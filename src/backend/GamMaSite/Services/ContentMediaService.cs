using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamMaSite.Data;
using GamMaSite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GamMaSite.Services
{
    /* Stores content images outside the deployment directory and cleans them up safely. */
    public sealed class ContentMediaService : IContentMediaService
    {
        public const long MaxImageSize = 2 * 1024 * 1024;
        public const string PublicPath = "/media";

        private readonly ApplicationDbContext _db;
        private readonly string _mediaRoot;
        private readonly ILogger<ContentMediaService> _logger;

        public ContentMediaService(ApplicationDbContext db, IConfiguration configuration, IWebHostEnvironment environment, ILogger<ContentMediaService> logger)
            : this(db, ResolveRootPath(configuration, environment.ContentRootPath), logger)
        {
        }

        public ContentMediaService(ApplicationDbContext db, string mediaRoot, ILogger<ContentMediaService> logger = null)
        {
            _db = db;
            _mediaRoot = Path.GetFullPath(mediaRoot ?? string.Empty);
            _logger = logger;
        }

        public static string ResolveRootPath(IConfiguration configuration, string contentRootPath)
        {
            var configuredPath = configuration["ContentMediaRootPath"];

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new ContentMediaStorageException("Miljøvariablen ContentMediaRootPath er ikke konfigureret.");
            }

            return Path.GetFullPath(Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(contentRootPath, configuredPath));
        }

        public async Task<ContentMediaUploadResult> UploadAsync(int contentItemId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ContentMediaValidationException("Vælg et billede først.");
            }

            if (file.Length > MaxImageSize)
            {
                throw new ContentMediaValidationException("Billedet må højst være 2 MiB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentMediaValidationException("Billedet skal være PNG eller JPEG.");
            }

            var item = await _db.ContentItems.FirstOrDefaultAsync(content => content.Id == contentItemId);
            if (item == null)
            {
                throw new ContentMediaValidationException("Indholdet findes ikke.");
            }

            if (!string.Equals(item.Type, ContentTypes.Event, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Type, ContentTypes.News, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentMediaValidationException("Billedet kan kun knyttes til events eller nyheder.");
            }

            var bytes = await ReadUploadAsync(file);
            var format = DetectImageFormat(bytes);
            var safeSlug = SafeFilePart(item.Slug);
            var contentType = item.Type.Equals(ContentTypes.Event, StringComparison.OrdinalIgnoreCase) ? "events" : "news";
            var relativeDirectory = Path.Combine("content", contentType);
            var directory = EnsureWritableDirectory(Path.Combine(_mediaRoot, relativeDirectory));
            var normalizedExtension = format == ImageFormat.Png ? ".png" : ".jpg";
            var fileName = $"{safeSlug}{normalizedExtension}";
            var path = EnsureUnderRoot(Path.Combine(directory, fileName));
            var temporaryPath = EnsureUnderRoot(Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp"));
            var publicUrl = $"{PublicPath}/{relativeDirectory.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";

            var usedByOtherContent = await _db.ContentItems
                .AsNoTracking()
                .AnyAsync(content => content.Id != item.Id && content.PictureUrl == publicUrl);
            if (usedByOtherContent)
            {
                throw new ContentMediaStorageException("Filnavnet er allerede knyttet til andet indhold. Ændr slug eller fjern den eksisterende billedreference først.");
            }

            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                TryDelete(temporaryPath);
                throw new ContentMediaStorageException("Billedet kunne ikke gemmes i mediemappen.", ex);
            }

            var oldPictureUrl = item.PictureUrl;
            var oldUpdated = item.Updated;
            item.PictureUrl = publicUrl;
            item.Updated = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch
            {
                TryDelete(temporaryPath);
                throw;
            }

            try
            {
                File.Move(temporaryPath, path, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                TryDelete(temporaryPath);
                await RestorePictureUrlAfterFailedPublishAsync(item, oldPictureUrl, oldUpdated, ex);
                throw new ContentMediaStorageException("Billedet blev gemt midlertidigt, men kunne ikke offentliggøres i mediemappen.", ex);
            }

            if (!string.Equals(oldPictureUrl, publicUrl, StringComparison.Ordinal))
            {
                await DeleteLocalFileBestEffortAsync(oldPictureUrl);
            }

            return new ContentMediaUploadResult { Url = publicUrl };
        }

        public async Task<ContentMediaDeleteResult> DeleteAsync(int contentItemId)
        {
            var item = await _db.ContentItems.FirstOrDefaultAsync(content => content.Id == contentItemId);
            if (item == null)
            {
                return null;
            }

            var oldPictureUrl = item.PictureUrl;
            item.PictureUrl = null;
            item.Updated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var isLocal = TryGetLocalPath(oldPictureUrl, out _);
            if (isLocal)
            {
                await DeleteLocalFileBestEffortAsync(oldPictureUrl);
            }

            return new ContentMediaDeleteResult { Url = oldPictureUrl, WasLocalFile = isLocal };
        }

        public async Task DeleteLocalFileIfUnreferencedAsync(string pictureUrl)
        {
            if (!TryGetLocalPath(pictureUrl, out var path))
            {
                return;
            }

            var stillReferenced = await _db.ContentItems
                .AsNoTracking()
                .AnyAsync(item => item.PictureUrl == pictureUrl);
            if (stillReferenced || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ContentMediaStorageException("Billedet blev fjernet fra indholdet, men kunne ikke slettes fra mediemappen.", ex);
            }
        }

        public async Task<ContentMediaSlugRename> PrepareSlugRenameAsync(ContentItem item, string previousSlug, string newSlug)
        {
            if (item == null || string.Equals(previousSlug, newSlug, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(item.PictureUrl))
            {
                return null;
            }

            var contentFolder = GetContentFolder(item.Type);
            if (contentFolder == null || !TryGetLocalPath(item.PictureUrl, out var sourcePath))
            {
                return null;
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg")
            {
                return null;
            }

            var relativeDirectory = Path.Combine("content", contentFolder);
            var expectedSourceUrl = BuildPublicUrl(relativeDirectory, $"{SafeFilePart(previousSlug)}{extension}");
            if (!string.Equals(item.PictureUrl, expectedSourceUrl, StringComparison.OrdinalIgnoreCase) || !File.Exists(sourcePath))
            {
                return null;
            }

            var newUrl = BuildPublicUrl(relativeDirectory, $"{SafeFilePart(newSlug)}{extension}");
            var destinationPath = EnsureUnderRoot(Path.Combine(_mediaRoot, relativeDirectory, Path.GetFileName(newUrl)));
            if (File.Exists(destinationPath))
            {
                throw new ContentMediaStorageException("Der findes allerede en billedfil med den nye slug.");
            }

            var referencedByOtherContent = await _db.ContentItems
                .AsNoTracking()
                .AnyAsync(content => content.Id != item.Id && content.PictureUrl == newUrl);
            if (referencedByOtherContent)
            {
                throw new ContentMediaStorageException("Den nye slug er allerede knyttet til et andet indholdsbillede.");
            }

            return new ContentMediaSlugRename
            {
                PreviousUrl = item.PictureUrl,
                NewUrl = newUrl
            };
        }

        public Task MoveLocalFileAsync(ContentMediaSlugRename rename)
        {
            if (rename == null ||
                !TryGetLocalPath(rename.PreviousUrl, out var sourcePath) ||
                !TryGetLocalPath(rename.NewUrl, out var destinationPath))
            {
                return Task.CompletedTask;
            }

            try
            {
                if (!File.Exists(sourcePath))
                {
                    throw new ContentMediaStorageException("Det eksisterende billede kunne ikke findes i mediemappen.");
                }

                if (File.Exists(destinationPath))
                {
                    throw new ContentMediaStorageException("Der findes allerede en billedfil med den nye slug.");
                }

                File.Move(sourcePath, destinationPath);
                return Task.CompletedTask;
            }
            catch (ContentMediaStorageException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ContentMediaStorageException("Billedfilen kunne ikke omdøbes til den nye slug.", ex);
            }
        }

        private async Task<byte[]> ReadUploadAsync(IFormFile file)
        {
            await using var input = file.OpenReadStream();
            await using var buffer = new MemoryStream((int)Math.Min(file.Length, MaxImageSize));
            await input.CopyToAsync(buffer);
            if (buffer.Length > MaxImageSize)
            {
                throw new ContentMediaValidationException("Billedet må højst være 2 MiB.");
            }

            var bytes = buffer.ToArray();
            if (DetectImageFormat(bytes) == ImageFormat.Unknown)
            {
                throw new ContentMediaValidationException("Filen er ikke et gyldigt PNG- eller JPEG-billede.");
            }

            return bytes;
        }

        private async Task RestorePictureUrlAfterFailedPublishAsync(ContentItem item, string oldPictureUrl, DateTime oldUpdated, Exception publishException)
        {
            item.PictureUrl = oldPictureUrl;
            item.Updated = oldUpdated;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception rollbackException)
            {
                _logger?.LogError(rollbackException,
                    "Could not restore content item {ContentItemId} after publishing its image failed.", item.Id);
            }

            _logger?.LogError(publishException,
                "Could not publish uploaded image for content item {ContentItemId}; restoration of the content record was attempted.", item.Id);
        }

        private async Task DeleteLocalFileBestEffortAsync(string pictureUrl)
        {
            const int attempts = 2;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    await DeleteLocalFileIfUnreferencedAsync(pictureUrl);
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex,
                        "Could not clean up unreferenced content image {PictureUrl} (attempt {Attempt}/{Attempts}).",
                        pictureUrl, attempt, attempts);

                    if (attempt < attempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                }
            }
        }

        private string EnsureWritableDirectory(string directory)
        {
            if (!Directory.Exists(_mediaRoot))
            {
                throw new ContentMediaStorageException($"Mediemappen mangler: {_mediaRoot}");
            }

            try
            {
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}");
                using (File.Create(probe)) { }
                File.Delete(probe);
                return directory;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new ContentMediaStorageException($"Mediemappen kan ikke skrives til: {_mediaRoot}", ex);
            }
        }

        private string EnsureUnderRoot(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var root = _mediaRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentMediaStorageException("Den beregnede mediefil ligger uden for mediemappen.");
            }

            return fullPath;
        }

        private bool TryGetLocalPath(string pictureUrl, out string path)
        {
            path = null;
            if (string.IsNullOrWhiteSpace(pictureUrl))
            {
                return false;
            }

            if (!Uri.TryCreate(pictureUrl, UriKind.RelativeOrAbsolute, out var uri))
            {
                return false;
            }

            if (uri.IsAbsoluteUri)
            {
                return false;
            }

            var publicPrefix = PublicPath.TrimEnd('/') + "/";
            if (!pictureUrl.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = Uri.UnescapeDataString(pictureUrl[publicPrefix.Length..])
                .Replace('/', Path.DirectorySeparatorChar);
            try
            {
                path = EnsureUnderRoot(Path.Combine(_mediaRoot, relativePath));
                return true;
            }
            catch (ContentMediaStorageException)
            {
                path = null;
                return false;
            }
        }

        private static string GetContentFolder(string contentType)
        {
            if (string.Equals(contentType, ContentTypes.Event, StringComparison.OrdinalIgnoreCase)) return "events";
            if (string.Equals(contentType, ContentTypes.News, StringComparison.OrdinalIgnoreCase)) return "news";
            return null;
        }

        private static string BuildPublicUrl(string relativeDirectory, string fileName) =>
            $"{PublicPath}/{relativeDirectory.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";

        private static string SafeFilePart(string value)
        {
            var source = string.IsNullOrWhiteSpace(value) ? "content" : value.Trim().ToLowerInvariant();
            var builder = new StringBuilder(source.Length);
            foreach (var character in source)
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-' || character == '_')
                {
                    builder.Append(character);
                }
                else if (char.IsWhiteSpace(character))
                {
                    builder.Append('-');
                }
            }

            var safe = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(safe) ? "content" : safe[..Math.Min(safe.Length, 80)];
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // The original database exception is more useful to the caller.
            }
        }

        private enum ImageFormat
        {
            Unknown,
            Png,
            Jpeg
        }

        private static ImageFormat DetectImageFormat(byte[] bytes)
        {
            if (IsPng(bytes)) return ImageFormat.Png;
            if (IsJpeg(bytes)) return ImageFormat.Jpeg;
            return ImageFormat.Unknown;
        }

        private static bool IsPng(byte[] bytes)
        {
            if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
                return false;
            }

            var offset = 8;
            var hasHeader = false;
            while (offset + 12 <= bytes.Length)
            {
                var length = ReadBigEndianInt(bytes, offset);
                if (length < 0 || offset + 12L + length > bytes.Length) return false;
                var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                if (type == "IHDR" && length == 13)
                {
                    hasHeader = ReadBigEndianInt(bytes, offset + 8) > 0 && ReadBigEndianInt(bytes, offset + 12) > 0;
                }
                if (type == "IEND") return hasHeader && length == 0;
                offset += 12 + length;
            }

            return false;
        }

        private static bool IsJpeg(byte[] bytes)
        {
            if (bytes.Length < 12 || bytes[0] != 0xff || bytes[1] != 0xd8)
            {
                return false;
            }

            var index = 2;
            var hasFrame = false;
            while (index < bytes.Length)
            {
                if (bytes[index++] != 0xff)
                {
                    return false;
                }

                while (index < bytes.Length && bytes[index] == 0xff) index++;
                if (index >= bytes.Length) return false;
                var marker = bytes[index++];

                if (marker == 0xd9) return hasFrame;
                if (marker == 0x01 || marker is >= 0xd0 and <= 0xd7) continue;
                if (index + 2 > bytes.Length) return false;

                var segmentLength = (bytes[index] << 8) | bytes[index + 1];
                if (segmentLength < 2 || index + segmentLength > bytes.Length) return false;

                if (marker == 0xda)
                {
                    if (!hasFrame) return false;
                    index += segmentLength;
                    while (index < bytes.Length - 1)
                    {
                        if (bytes[index] != 0xff)
                        {
                            index++;
                            continue;
                        }

                        var scanMarker = bytes[index + 1];
                        if (scanMarker == 0x00)
                        {
                            index += 2;
                            continue;
                        }

                        if (scanMarker is >= 0xd0 and <= 0xd7)
                        {
                            index += 2;
                            continue;
                        }

                        return scanMarker == 0xd9;
                    }

                    return false;
                }

                if (marker is >= 0xc0 and <= 0xc3 || marker is >= 0xc5 and <= 0xc7 || marker is >= 0xc9 and <= 0xcb || marker is >= 0xcd and <= 0xcf)
                {
                    if (segmentLength < 8 || index + 7 >= bytes.Length) return false;
                    var height = (bytes[index + 3] << 8) | bytes[index + 4];
                    var width = (bytes[index + 5] << 8) | bytes[index + 6];
                    hasFrame = width > 0 && height > 0 && bytes[index + 7] > 0;
                }

                index += segmentLength;
            }

            return false;
        }

        private static int ReadBigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
