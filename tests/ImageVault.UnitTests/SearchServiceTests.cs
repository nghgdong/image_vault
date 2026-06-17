using ImageVault.Application.Services;
using ImageVault.Domain.Entities;

namespace ImageVault.UnitTests;

public sealed class SearchServiceTests
{
    private readonly InMemoryFolderRepository _folders = new();
    private readonly InMemoryImageRepository _images = new();
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _sut = new SearchService(_folders, _images);
        _folders.Store["f1"] = new Folder { Id = "f1", Name = "Hạ Long", Path = "/f1/", IsDeleted = false };
        _folders.Store["f2"] = new Folder { Id = "f2", Name = "Đà Lạt", Path = "/f2/", IsDeleted = false };
        _folders.Store["f3"] = new Folder { Id = "f3", Name = "Long An", Path = "/f3/", IsDeleted = true }; // đã xóa
        _images.Store.Add(new ImageItem { Id = "i1", FolderId = "f1", Name = "long-bien.jpg", Url = "u", ThumbUrl = "t" });
        _images.Store.Add(new ImageItem { Id = "i2", FolderId = "f2", Name = "ho-xuan-huong.jpg", Url = "u", ThumbUrl = "t" });
    }

    [Fact]
    public async Task Search_matches_folders_and_images_case_insensitive()
    {
        var res = await _sut.SearchAsync("long", null);

        Assert.Contains(res.Folders, f => f.Id == "f1"); // "Hạ Long"
        Assert.DoesNotContain(res.Folders, f => f.Id == "f3"); // đã xóa, không trả
        Assert.Contains(res.Images, i => i.Id == "i1"); // "long-bien.jpg"
        Assert.DoesNotContain(res.Images, i => i.Id == "i2");
    }

    [Fact]
    public async Task Search_empty_query_returns_nothing()
    {
        var res = await _sut.SearchAsync("   ", null);
        Assert.Empty(res.Folders);
        Assert.Empty(res.Images);
    }
}
