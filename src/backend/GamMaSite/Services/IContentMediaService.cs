using System.Threading.Tasks;
using GamMaSite.Models;
using Microsoft.AspNetCore.Http;

namespace GamMaSite.Services
{
    public interface IContentMediaService
    {
        Task<ContentMediaUploadResult> UploadAsync(int contentItemId, IFormFile file);

        Task<ContentMediaDeleteResult> DeleteAsync(int contentItemId);

        Task DeleteLocalFileIfUnreferencedAsync(string pictureUrl);

        Task<ContentMediaSlugRename> PrepareSlugRenameAsync(ContentItem item, string previousSlug, string newSlug);

        Task MoveLocalFileAsync(ContentMediaSlugRename rename);
    }

    public sealed class ContentMediaUploadResult
    {
        public string Url { get; set; }
    }

    public sealed class ContentMediaDeleteResult
    {
        public string Url { get; set; }

        public bool WasLocalFile { get; set; }
    }

    public sealed class ContentMediaSlugRename
    {
        public string PreviousUrl { get; init; }

        public string NewUrl { get; init; }
    }

    public class ContentMediaValidationException : System.Exception
    {
        public ContentMediaValidationException(string message) : base(message)
        {
        }
    }

    public class ContentMediaStorageException : System.Exception
    {
        public ContentMediaStorageException(string message, System.Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
