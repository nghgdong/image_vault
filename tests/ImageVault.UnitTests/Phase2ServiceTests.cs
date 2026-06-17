using System.Text;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using ImageVault.Domain.Entities;

namespace ImageVault.UnitTests;

/// <summary>
/// Integration test tầng service (service + repo in-memory + fake freeimage) cho Phase 2:
/// CRUD folder, soft-delete không gọi freeimage, upload đơn & batch (207 logic).
/// </summary>
public sealed class Phase2ServiceTests
{
    private readonly InMemoryFolderRepository _folders = new();
    private readonly InMemoryImageRepository _images = new();
    private readonly FakeFreeImageClient _freeImage = new();
    private readonly FakeClock _clock = new();
    private readonly FolderService _folderSvc;
    private readonly ImageService _imageSvc;

    public Phase2ServiceTests()
    {
        var ids = new SequentialIdGenerator();
        _folderSvc = new FolderService(_folders, _images, _clock, ids);
        _imageSvc = new ImageService(_images, _folders, _freeImage, _clock, ids);
    }

    // ---------- P2-16: CRUD folder + move cập nhật con cháu ----------

    [Fact]
    public async Task Folder_crud_move_updates_descendants_then_soft_delete_counts()
    {
        var b = await _folderSvc.CreateFolderAsync("B", null);
        var c = await _folderSvc.CreateFolderAsync("C", b.Id);
        var d = await _folderSvc.CreateFolderAsync("D", c.Id);
        var dest = await _folderSvc.CreateFolderAsync("dest", null);

        // Move C (kèm D) vào dest.
        await _folderSvc.MoveFolderAsync(c.Id, dest.Id);

        var crumbsOfD = await _folderSvc.GetBreadcrumbAsync(d.Id);
        Assert.Equal(new[] { dest.Id, c.Id, d.Id }, crumbsOfD.Select(x => x.Id).ToArray());
        Assert.Equal(2, _folders.Store[d.Id].Depth);

        // Soft delete dest đệ quy → dest + C + D = 3 folder.
        var res = await _folderSvc.SoftDeleteFolderAsync(dest.Id, cascade: true);
        Assert.Equal(3, res.DeletedFolders);
        Assert.True(_folders.Store[c.Id].IsDeleted);
        Assert.True(_folders.Store[d.Id].IsDeleted);
        // B không bị ảnh hưởng.
        Assert.False(_folders.Store[b.Id].IsDeleted);
    }

    // ---------- P2-17: upload đơn & batch (mock freeimage) ----------

    [Fact]
    public async Task Upload_single_saves_metadata_from_freeimage()
    {
        var f = await _folderSvc.CreateFolderAsync("imgs", null);

        var dto = await _imageSvc.UploadAsync(f.Id, Png("a.png"), displayName: null);

        Assert.Equal(1, _freeImage.UploadCount);
        Assert.Equal("a.png", dto.Name);
        Assert.StartsWith("https://iili.io/", dto.Url);
        Assert.Single(_images.Store);
        Assert.Equal(f.Id, _images.Store[0].FolderId);
    }

    [Fact]
    public async Task Upload_to_missing_folder_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _imageSvc.UploadAsync("idXX", Png("a.png"), null));
    }

    [Fact]
    public async Task Upload_batch_one_failure_does_not_block_others()
    {
        var f = await _folderSvc.CreateFolderAsync("imgs", null);

        var files = new List<UploadFile>
        {
            Png("ok1.png"),       // success
            Text("bad.txt"),      // lỗi validate (không phải ảnh) → không gọi freeimage
            Png("boom.png"),      // qua validate nhưng freeimage giả lập lỗi
            Png("ok2.png"),       // success
        };

        var resp = await _imageSvc.UploadBatchAsync(f.Id, files);

        Assert.Equal(4, resp.Total);
        Assert.Equal(2, resp.Succeeded);
        Assert.Equal(2, resp.Failed);
        Assert.Equal("success", resp.Results[0].Status);
        Assert.Equal("error", resp.Results[1].Status);
        Assert.Equal("error", resp.Results[2].Status);
        Assert.Equal("success", resp.Results[3].Status);

        // Chỉ 2 ảnh hợp lệ được lưu; file text không hề gọi freeimage.
        Assert.Equal(2, _images.Store.Count);
        Assert.DoesNotContain("bad.txt", _freeImage.UploadedFiles);
    }

    [Fact]
    public async Task Upload_batch_over_limit_throws_validation()
    {
        var f = await _folderSvc.CreateFolderAsync("imgs", null);
        var many = Enumerable.Range(0, ImageFileValidator.MaxBatchFiles + 1)
            .Select(i => Png($"f{i}.png")).ToList();

        await Assert.ThrowsAsync<ValidationAppException>(() => _imageSvc.UploadBatchAsync(f.Id, many));
    }

    [Fact]
    public async Task Delete_image_soft_deletes_without_calling_freeimage()
    {
        var f = await _folderSvc.CreateFolderAsync("imgs", null);
        var dto = await _imageSvc.UploadAsync(f.Id, Png("a.png"), null);
        var before = _freeImage.UploadCount;

        await _imageSvc.DeleteAsync(dto.Id);

        Assert.True(_images.Store.Single().IsDeleted);
        Assert.Equal(before, _freeImage.UploadCount); // không có lệnh gọi freeimage nào thêm (và không hề có Delete)
        await Assert.ThrowsAsync<NotFoundException>(() => _imageSvc.GetDetailAsync(dto.Id));
    }

    // ---------- helpers ----------

    private static UploadFile Png(string name)
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR...
        };
        return new UploadFile(name, new MemoryStream(bytes), bytes.Length);
    }

    private static UploadFile Text(string name)
    {
        var bytes = Encoding.ASCII.GetBytes("this is definitely not an image file");
        return new UploadFile(name, new MemoryStream(bytes), bytes.Length);
    }
}
