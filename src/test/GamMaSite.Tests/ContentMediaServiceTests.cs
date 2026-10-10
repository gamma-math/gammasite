using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using GamMaSite.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GamMaSite.Tests;

public class ContentMediaServiceTests
{
    [Fact]
    public async Task UploadAsync_AcceptsValidPngAndReturnsPublicUrl()
    {
        var root = CreateTempDirectory();
        try
        {
            await using var db = CreateDb();
            db.ContentItems.Add(new ContentItem { Id = 12, Title = "Event", Slug = "sommer-event", Type = ContentTypes.Event, Status = ContentStatuses.Draft });
            await db.SaveChangesAsync();
            var service = new ContentMediaService(db, root);

            var result = await service.UploadAsync(12, File(PngBytes(), "photo.jpg", "application/octet-stream"));

            Assert.Equal("/media/content/events/sommer-event.png", result.Url);
            var savedItem = await db.ContentItems.FindAsync(12);
            Assert.NotNull(savedItem);
            Assert.Equal(result.Url, savedItem.PictureUrl);
            Assert.True(System.IO.File.Exists(Path.Combine(root, result.Url["/media/".Length..].Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task UploadAsync_AcceptsValidJpegAndRejectsOversizedOrInvalidFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            await using var db = CreateDb();
            db.ContentItems.Add(new ContentItem { Id = 13, Title = "News", Slug = "nyhed", Type = ContentTypes.News, Status = ContentStatuses.Draft });
            await db.SaveChangesAsync();
            var service = new ContentMediaService(db, root);

            var result = await service.UploadAsync(13, File(JpegBytes(), "photo.png", "image/png"));
            Assert.Equal("/media/content/news/nyhed.jpg", result.Url);

            await Assert.ThrowsAsync<ContentMediaValidationException>(() => service.UploadAsync(13, File(new byte[(int)ContentMediaService.MaxImageSize + 1], "large.jpg", "image/jpeg")));
            await Assert.ThrowsAsync<ContentMediaValidationException>(() => service.UploadAsync(13, File(new byte[] { 1, 2, 3, 4 }, "fake.jpg", "image/jpeg")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesLocalFileButNeverTreatsExternalUrlAsLocal()
    {
        var root = CreateTempDirectory();
        try
        {
            await using var db = CreateDb();
            var localUrl = "/media/content/news/old.jpg";
            var localPath = Path.Combine(root, "content", "news", "old.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await System.IO.File.WriteAllBytesAsync(localPath, new byte[] { 1 });
            db.ContentItems.AddRange(
                new ContentItem { Id = 14, Title = "Local", Slug = "local", Type = ContentTypes.News, Status = ContentStatuses.Draft, PictureUrl = localUrl },
                new ContentItem { Id = 15, Title = "External", Slug = "external", Type = ContentTypes.News, Status = ContentStatuses.Draft, PictureUrl = "https://images.example/photo.jpg" });
            await db.SaveChangesAsync();
            var service = new ContentMediaService(db, root);

            await service.DeleteAsync(14);
            await service.DeleteAsync(15);

            Assert.False(System.IO.File.Exists(localPath));
            var deletedLocal = await db.ContentItems.FindAsync(14);
            var deletedExternal = await db.ContentItems.FindAsync(15);
            Assert.NotNull(deletedLocal);
            Assert.NotNull(deletedExternal);
            Assert.Null(deletedLocal.PictureUrl);
            Assert.Null(deletedExternal.PictureUrl);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DeleteAsync_KeepsAFileReferencedByAnotherContentItem()
    {
        var root = CreateTempDirectory();
        try
        {
            await using var db = CreateDb();
            var localUrl = "/media/content/events/shared.jpg";
            var localPath = Path.Combine(root, "content", "events", "shared.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await System.IO.File.WriteAllBytesAsync(localPath, new byte[] { 1 });
            db.ContentItems.AddRange(
                new ContentItem { Id = 16, Title = "First", Slug = "first", Type = ContentTypes.Event, Status = ContentStatuses.Draft, PictureUrl = localUrl },
                new ContentItem { Id = 17, Title = "Second", Slug = "second", Type = ContentTypes.Event, Status = ContentStatuses.Draft, PictureUrl = localUrl });
            await db.SaveChangesAsync();
            var service = new ContentMediaService(db, root);

            await service.DeleteAsync(16);

            Assert.True(System.IO.File.Exists(localPath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DeletingContent_CleansUpItsUnreferencedLocalImage()
    {
        var root = CreateTempDirectory();
        try
        {
            await using var db = CreateDb();
            var localUrl = "/media/content/events/to-delete.jpg";
            var localPath = Path.Combine(root, "content", "events", "to-delete.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await System.IO.File.WriteAllBytesAsync(localPath, new byte[] { 1 });
            db.ContentItems.Add(new ContentItem { Id = 18, Title = "Delete", Slug = "delete", Type = ContentTypes.Event, Status = ContentStatuses.Draft, PictureUrl = localUrl });
            await db.SaveChangesAsync();
            var media = new ContentMediaService(db, root);

            Assert.True(await new ContentService(db, media).DeleteAsync(18));
            Assert.False(System.IO.File.Exists(localPath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static FormFile File(byte[] bytes, string name, string contentType)
    {
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return file;
    }

    private static byte[] PngBytes() => new byte[]
    {
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0,
        31, 21, 196, 137, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130
    };

    private static byte[] JpegBytes() => new byte[]
    {
        0xff, 0xd8,
        0xff, 0xe0, 0x00, 0x02,
        0xff, 0xc0, 0x00, 0x0b, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00,
        0xff, 0xda, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3f, 0x00,
        0x00, 0xff, 0xd9
    };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "gammasite-media-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
