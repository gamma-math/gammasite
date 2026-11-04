using System.Threading;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IReceiptPdfService
    {
        Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken);
    }
}
