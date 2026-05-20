using System.Drawing;
using System.Drawing.Imaging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Converting;

namespace PdfToolkit.Services.Converting;

public class PdfConverter : IConverter
{
    private readonly IDocumentConverter? _documentConverter;

    public static readonly IReadOnlyList<string> ImageExtensions =
    [
        ".jpg", ".jpeg", ".jpe", ".jfif",
        ".png",
        ".bmp", ".dib",
        ".gif",
        ".tiff", ".tif",
        ".ico", ".cur",
        ".webp",
        ".wmp", ".hdp", ".jxr",
        ".wmf", ".emf",
        ".heic", ".heif",
        ".pcx", ".tga", ".psd",
        ".pnm", ".pgm", ".ppm", ".pbm",
    ];

    public PdfConverter(IDocumentConverter? documentConverter = null)
    {
        _documentConverter = documentConverter;
    }

    public async Task<ConvertResult> ConvertAsync(
        ConvertRequest request,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        return await ConvertInternalAsync(request, progress, cancellationToken);
    }

    private async Task<ConvertResult> ConvertInternalAsync(
        ConvertRequest request,
        IProgress<ConvertProgress>? progress,
        CancellationToken ct)
    {
        using var outputDocument = new PdfDocument();
        int total = request.SourceFiles.Count;
        int processed = 0;

        foreach (var sourceFile in request.SourceFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(sourceFile);
            progress?.Report(new ConvertProgress(processed, total, fileName));

            try
            {
                var ext = Path.GetExtension(sourceFile).ToLowerInvariant();

                if (IsImageExtension(ext))
                    AddImagePages(outputDocument, sourceFile, ct);
                else
                    await AddDocumentPagesAsync(outputDocument, sourceFile, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new PdfConvertException(
                    $"Error al convertir '{fileName}': {ex.Message}", ex);
            }

            processed++;
            progress?.Report(new ConvertProgress(processed, total, fileName));
        }

        try { outputDocument.Save(request.OutputPath); }
        catch (Exception ex)
        {
            throw new PdfConvertException($"Error al guardar el PDF: {ex.Message}", ex);
        }

        var info = new FileInfo(request.OutputPath);
        return new ConvertResult(request.OutputPath, outputDocument.PageCount, info.Length);
    }

    private static void AddImagePages(PdfDocument doc, string filePath, CancellationToken ct)
    {
        using var sourceImage = Image.FromFile(filePath);

        var dimension = new FrameDimension(sourceImage.FrameDimensionsList[0]);
        int frameCount = sourceImage.GetFrameCount(dimension);

        for (int f = 0; f < frameCount; f++)
        {
            ct.ThrowIfCancellationRequested();
            sourceImage.SelectActiveFrame(dimension, f);

            byte[] jpegBytes = ToJpegBytes(sourceImage);
            var xImage = XImage.FromStream(() => new MemoryStream(jpegBytes));

            double dpiX = sourceImage.HorizontalResolution > 0 ? sourceImage.HorizontalResolution : 96;
            double dpiY = sourceImage.VerticalResolution > 0 ? sourceImage.VerticalResolution : 96;
            double wPt = sourceImage.Width / dpiX * 72.0;
            double hPt = sourceImage.Height / dpiY * 72.0;

            const double maxPt = 1684; // A3
            if (wPt > maxPt || hPt > maxPt)
            {
                double scale = Math.Min(maxPt / wPt, maxPt / hPt);
                wPt *= scale;
                hPt *= scale;
            }

            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(wPt);
            page.Height = XUnit.FromPoint(hPt);

            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);
        }
    }

    private static byte[] ToJpegBytes(Image source)
    {
        using var bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        bmp.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        using var ms = new MemoryStream();
        var encoder = ImageCodecInfo.GetImageEncoders()
            .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var encParams = new EncoderParameters(1);
        encParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
        bmp.Save(ms, encoder, encParams);
        return ms.ToArray();
    }

    private async Task AddDocumentPagesAsync(
        PdfDocument outputDoc, string sourcePath, CancellationToken ct)
    {
        if (_documentConverter is null || !_documentConverter.IsAvailable)
            throw new PdfConvertException(
                "No hay conversor de documentos disponible. " +
                "Instala LibreOffice (gratis) o configura Syncfusion Community.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"pdftoolkit_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tempPdf = await _documentConverter.ConvertToPdfAsync(sourcePath, tempDir, ct);

            using var tempDoc = PdfReader.Open(tempPdf, PdfDocumentOpenMode.Import);
            foreach (var page in tempDoc.Pages)
            {
                ct.ThrowIfCancellationRequested();
                outputDoc.AddPage(page);
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    public static bool IsImageExtension(string ext) =>
        ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);

    public static bool IsDocumentExtension(string ext) =>
        CompositeDocumentConverter.AllDocumentExtensions.Contains(
            ext, StringComparer.OrdinalIgnoreCase);

    private void ValidateRequest(ConvertRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ArgumentException("La ruta de salida no puede estar vacía.", nameof(request));

        if (request.SourceFiles is null || request.SourceFiles.Count == 0)
            throw new ArgumentException("Se requiere al menos un archivo.", nameof(request));

        foreach (var file in request.SourceFiles)
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException("Las rutas no pueden estar vacías.", nameof(request));

        foreach (var file in request.SourceFiles)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"Archivo no encontrado: {file}", file);

            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (!IsImageExtension(ext) && !IsDocumentExtension(ext))
                throw new ArgumentException(
                    $"Formato no admitido: '{ext}'.");
        }
    }
}
