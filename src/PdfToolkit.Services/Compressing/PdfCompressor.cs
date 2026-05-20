using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Compressing;

namespace PdfToolkit.Services.Compressing;

public class PdfCompressor : ICompressor
{
    public async Task<CompressResult> CompressAsync(
        CompressRequest request,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Archivo no encontrado.", request.SourcePath);

        var originalBytes = new FileInfo(request.SourcePath).Length;
        var tempPath = Path.Combine(Path.GetTempPath(), $"pdfcompress_{Guid.NewGuid():N}.pdf");

        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(10);

                using var doc = PdfReader.Open(request.SourcePath, PdfDocumentOpenMode.Modify);
                progress?.Report(40);

                ct.ThrowIfCancellationRequested();

                doc.Options.CompressContentStreams = true;
                doc.Options.NoCompression = false;

                progress?.Report(75);
                doc.Save(tempPath);
                progress?.Report(100);

            }, ct);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw new PdfToolkit.Core.Compressing.PdfCompressException($"Error al comprimir: {ex.Message}", ex);
        }

        var compressedBytes = new FileInfo(tempPath).Length;

        // If compression made it larger or equal, use the original — never inflate the file
        if (compressedBytes >= originalBytes)
        {
            File.Delete(tempPath);
            File.Copy(request.SourcePath, request.OutputPath, overwrite: true);
            return new CompressResult(request.OutputPath, originalBytes, originalBytes);
        }

        File.Move(tempPath, request.OutputPath, overwrite: true);
        return new CompressResult(request.OutputPath, originalBytes, compressedBytes);
    }
}
