using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GamMaSite.Services
{
    public interface IContentMediaService
    {
        Task<ContentMediaUploadResult> UploadAsync(int contentItemId, IFormFile file);

        Task<ContentMediaDeleteResult> DeleteAsync(int contentItemId);

        Task DeleteLocalFileIfUnreferencedAsync(string pictureUrl);
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
