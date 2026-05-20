using System.IO;
using System.Windows.Media.Imaging;
using PDFtoImage;
using SkiaSharp;

namespace PdfToolkit.UI.Services;

public class PdfThumbnailService : IThumbnailService
{
    public async Task<BitmapImage> GenerateAsync(
        string pdfPath,
        int width,
        int pageIndex = 0,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pdfStream = File.OpenRead(pdfPath);

            using var pageBitmap = Conversion.ToImage(
                pdfStream,
                page: pageIndex,
                options: new RenderOptions(Dpi: 150, BackgroundColor: SKColors.White));

            cancellationToken.ThrowIfCancellationRequested();

            int srcW = pageBitmap.Width;
            int srcH = pageBitmap.Height;
            int dstW = width;
            int dstH = srcW > 0 ? (int)Math.Round((double)srcH * dstW / srcW) : width;

            var info = new SKImageInfo(dstW, dstH, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var resized = new SKBitmap(info);
            pageBitmap.ScalePixels(resized, SKFilterQuality.High);

            using var skImage = SKImage.FromBitmap(resized);
            using var data = skImage.Encode(SKEncodedImageFormat.Png, 90);
            using var ms = new MemoryStream();
            data.SaveTo(ms);
            ms.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }, cancellationToken);
    }
}
