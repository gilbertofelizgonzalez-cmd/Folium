using PDFtoImage;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Exporting;
using SkiaSharp;

namespace PdfToolkit.Services.Exporting;

public class PdfPageExporter : IPageExporter
{
    public int GetPageCount(string sourcePath)
    {
        using var doc = PdfReader.Open(sourcePath, PdfDocumentOpenMode.InformationOnly);
        return doc.PageCount;
    }

    public async Task<ExportResult> ExportAsync(
        ExportRequest request,
        IProgress<ExportProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Archivo no encontrado.", request.SourcePath);

        Directory.CreateDirectory(request.OutputDirectory);

        var filePaths = new List<string>();
        var baseName = Path.GetFileNameWithoutExtension(request.SourcePath);
        var format = request.Format.ToLowerInvariant() == "png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        var ext = format == SKEncodedImageFormat.Png ? "png" : "jpg";

        await Task.Run(() =>
        {
            using var stream = File.OpenRead(request.SourcePath);
            var options = new RenderOptions(Dpi: request.Dpi);
            var pages = Conversion.ToImages(stream, options: options).ToList();
            var total = request.PageNumbers?.Count ?? pages.Count;
            var indices = request.PageNumbers?.Select(p => p - 1).ToList()
                          ?? Enumerable.Range(0, pages.Count).ToList();

            int processed = 0;
            foreach (var idx in indices)
            {
                ct.ThrowIfCancellationRequested();
                if (idx < 0 || idx >= pages.Count) continue;

                var fileName = $"{baseName}_pagina{idx + 1:D3}.{ext}";
                var filePath = Path.Combine(request.OutputDirectory, fileName);

                using var bitmap = pages[idx];
                using var data = bitmap.Encode(format, 95);
                File.WriteAllBytes(filePath, data.ToArray());
                filePaths.Add(filePath);

                processed++;
                progress?.Report(new ExportProgress(processed, total, fileName));
            }
        }, ct);

        return new ExportResult(request.OutputDirectory, filePaths.Count, filePaths);
    }
}
