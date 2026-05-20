using System.Windows.Media.Imaging;

namespace PdfToolkit.UI.Services;

public interface IThumbnailService
{
    Task<BitmapImage> GenerateAsync(string pdfPath, int width, int pageIndex = 0, CancellationToken cancellationToken = default);
}
