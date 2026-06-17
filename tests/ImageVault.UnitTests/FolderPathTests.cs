using ImageVault.Application.Common;
using ImageVault.Application.Services;
using ImageVault.Domain.Entities;

namespace ImageVault.UnitTests;

/// <summary>
/// Test logic materialized path (SPEC §3.1) — phần dễ sai nhất: tạo/move/đệ quy.
/// </summary>
public sealed class FolderPathTests
{
    private readonly InMemoryFolderRepository _folders = new();
    private readonly InMemoryImageRepository _images = new();
    private readonly FakeClock _clock = new();
    private readonly FolderService _sut;

    public FolderPathTests()
    {
        _sut = new FolderService(_folders, _images, _clock, new SequentialIdGenerator());
    }

    [Fact]
    public async Task Create_root_folder_sets_path_depth_parent()
    {
        var root = await _sut.CreateFolderAsync("Du lịch", parentId: null);

        Assert.Null(root.ParentId);
        Assert.Equal(0, root.Depth);
        Assert.Equal($"/{root.Id}/", root.Path);
    }

    [Fact]
    public async Task Create_child_folder_extends_parent_path_and_depth()
    {
        var root = await _sut.CreateFolderAsync("Root", null);
        var child = await _sut.CreateFolderAsync("2024", root.Id);

        Assert.Equal(root.Id, child.ParentId);
        Assert.Equal(1, child.Depth);
        Assert.Equal($"{root.Path}{child.Id}/", child.Path);
        Assert.Equal($"/{root.Id}/{child.Id}/", child.Path);
    }

    [Fact]
    public async Task Create_duplicate_name_in_same_parent_throws_conflict()
    {
        var root = await _sut.CreateFolderAsync("Root", null);
        await _sut.CreateFolderAsync("A", root.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateFolderAsync("A", root.Id));
    }

    [Fact]
    public async Task Same_name_allowed_in_different_parents()
    {
        var a = await _sut.CreateFolderAsync("A", null);
        var b = await _sut.CreateFolderAsync("B", null);

        await _sut.CreateFolderAsync("Sub", a.Id);
        var ok = await _sut.CreateFolderAsync("Sub", b.Id); // không xung đột

        Assert.Equal(b.Id, ok.ParentId);
    }

    [Fact]
    public async Task Move_folder_updates_self_and_all_descendants_path_and_depth()
    {
        // src/ (depth0) -> child/ (1) -> grand/ (2)
        var src = await _sut.CreateFolderAsync("src", null);
        var child = await _sut.CreateFolderAsync("child", src.Id);
        var grand = await _sut.CreateFolderAsync("grand", child.Id);

        // dest/ (depth0)
        var dest = await _sut.CreateFolderAsync("dest", null);

        // Move child (cùng cây con grand) sang dưới dest.
        await _sut.MoveFolderAsync(child.Id, dest.Id);

        var movedChild = _folders.Store[child.Id];
        var movedGrand = _folders.Store[grand.Id];

        Assert.Equal(dest.Id, movedChild.ParentId);
        Assert.Equal(1, movedChild.Depth);
        Assert.Equal($"/{dest.Id}/{child.Id}/", movedChild.Path);

        // Con cháu phải được rebase theo.
        Assert.Equal(2, movedGrand.Depth);
        Assert.Equal($"/{dest.Id}/{child.Id}/{grand.Id}/", movedGrand.Path);

        // src không còn chứa child.
        Assert.Equal(src.Id, _folders.Store[src.Id].Id);
        Assert.DoesNotContain(_folders.Store.Values, f => f.ParentId == src.Id && !f.IsDeleted);
    }

    [Fact]
    public async Task Move_folder_to_root_sets_depth_zero_and_rebases_descendants()
    {
        var root = await _sut.CreateFolderAsync("root", null);
        var child = await _sut.CreateFolderAsync("child", root.Id);
        var grand = await _sut.CreateFolderAsync("grand", child.Id);

        await _sut.MoveFolderAsync(child.Id, newParentId: null);

        var mc = _folders.Store[child.Id];
        var mg = _folders.Store[grand.Id];
        Assert.Null(mc.ParentId);
        Assert.Equal(0, mc.Depth);
        Assert.Equal($"/{child.Id}/", mc.Path);
        Assert.Equal(1, mg.Depth);
        Assert.Equal($"/{child.Id}/{grand.Id}/", mg.Path);
    }

    [Fact]
    public async Task Move_folder_into_itself_throws_validation()
    {
        var a = await _sut.CreateFolderAsync("a", null);
        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.MoveFolderAsync(a.Id, a.Id));
    }

    [Fact]
    public async Task Move_folder_into_own_descendant_throws_validation()
    {
        var a = await _sut.CreateFolderAsync("a", null);
        var b = await _sut.CreateFolderAsync("b", a.Id);
        var c = await _sut.CreateFolderAsync("c", b.Id);

        // Move a vào c (con cháu của a) → vòng lặp → 400.
        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.MoveFolderAsync(a.Id, c.Id));
    }

    [Fact]
    public async Task Move_to_destination_with_duplicate_name_throws_conflict()
    {
        var dest = await _sut.CreateFolderAsync("dest", null);
        await _sut.CreateFolderAsync("X", dest.Id);   // dest đã có "X"
        var x2 = await _sut.CreateFolderAsync("X", null); // top-level "X"

        await Assert.ThrowsAsync<ConflictException>(() => _sut.MoveFolderAsync(x2.Id, dest.Id));
    }

    [Fact]
    public async Task SoftDelete_recursively_marks_folder_descendants_and_images()
    {
        var root = await _sut.CreateFolderAsync("root", null);
        var child = await _sut.CreateFolderAsync("child", root.Id);
        var grand = await _sut.CreateFolderAsync("grand", child.Id);
        var sibling = await _sut.CreateFolderAsync("sibling", null); // ngoài cây bị xóa

        AddImage("img-root", root.Id);
        AddImage("img-grand", grand.Id);
        AddImage("img-sibling", sibling.Id);

        var result = await _sut.SoftDeleteFolderAsync(root.Id, cascade: true);

        Assert.Equal(3, result.DeletedFolders); // root + child + grand
        Assert.Equal(2, result.DeletedImages);  // img-root + img-grand

        Assert.True(_folders.Store[root.Id].IsDeleted);
        Assert.True(_folders.Store[child.Id].IsDeleted);
        Assert.True(_folders.Store[grand.Id].IsDeleted);
        Assert.False(_folders.Store[sibling.Id].IsDeleted); // không đụng cây khác

        Assert.True(_images.Store.Single(i => i.Id == "img-root").IsDeleted);
        Assert.True(_images.Store.Single(i => i.Id == "img-grand").IsDeleted);
        Assert.False(_images.Store.Single(i => i.Id == "img-sibling").IsDeleted);
    }

    [Fact]
    public async Task SoftDelete_non_cascade_with_children_throws_conflict()
    {
        var root = await _sut.CreateFolderAsync("root", null);
        await _sut.CreateFolderAsync("child", root.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.SoftDeleteFolderAsync(root.Id, cascade: false));
    }

    [Fact]
    public async Task Breadcrumb_returns_path_from_top_to_current()
    {
        var root = await _sut.CreateFolderAsync("Root", null);
        var mid = await _sut.CreateFolderAsync("Mid", root.Id);
        var leaf = await _sut.CreateFolderAsync("Leaf", mid.Id);

        var crumbs = await _sut.GetBreadcrumbAsync(leaf.Id);

        Assert.Equal(new[] { root.Id, mid.Id, leaf.Id }, crumbs.Select(c => c.Id).ToArray());
        Assert.Equal(new[] { "Root", "Mid", "Leaf" }, crumbs.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task GetChildren_reports_image_and_subfolder_counts()
    {
        var root = await _sut.CreateFolderAsync("root", null);
        var a = await _sut.CreateFolderAsync("a", root.Id);
        await _sut.CreateFolderAsync("a-sub", a.Id);
        AddImage("i1", a.Id);
        AddImage("i2", a.Id);

        var children = await _sut.GetChildrenAsync(root.Id, new ContentQuery());

        var dto = Assert.Single(children.SubFolders);
        Assert.Equal(a.Id, dto.Id);
        Assert.Equal(2, dto.ImageCount);
        Assert.Equal(1, dto.SubFolderCount);
    }

    private void AddImage(string id, string folderId)
        => _images.Store.Add(new ImageItem
        {
            Id = id,
            FolderId = folderId,
            Name = id,
            Url = "u",
            ThumbUrl = "t",
            IsDeleted = false,
            UploadedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        });
}
