using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Merging;

namespace PdfToolkit.Services.Merging;

public class PdfMerger : IPdfMerger
{
    public async Task<MergeResult> MergeAsync(
        MergeRequest request,
        IProgress<MergeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        return await Task.Run(() =>
        {
            using var outputDocument = new PdfDocument();
            int totalFiles = request.SourceFiles.Count;
            int processed = 0;

            foreach (var sourceFile in request.SourceFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(sourceFile);
                progress?.Report(new MergeProgress(processed, totalFiles, fileName));

                try
                {
                    using var sourceDoc = PdfReader.Open(sourceFile, PdfDocumentOpenMode.Import);
                    foreach (var page in sourceDoc.Pages)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        outputDocument.AddPage(page);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new PdfMergeException(
                        $"Error al procesar el archivo '{fileName}': {ex.Message}", ex);
                }

                processed++;
                progress?.Report(new MergeProgress(processed, totalFiles, fileName));
            }

            try
            {
                outputDocument.Save(request.OutputPath);
            }
            catch (Exception ex)
            {
                throw new PdfMergeException(
                    $"Error al guardar el PDF de salida: {ex.Message}", ex);
            }

            var info = new FileInfo(request.OutputPath);
            return new MergeResult(request.OutputPath, outputDocument.PageCount, info.Length);
        }, cancellationToken);
    }

    private static void ValidateRequest(MergeRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ArgumentException("La ruta de salida no puede estar vacía.", nameof(request));

        if (request.SourceFiles is null || request.SourceFiles.Count < 2)
            throw new ArgumentException(
                "Se requieren al menos 2 archivos para unir.", nameof(request));

        foreach (var file in request.SourceFiles)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentException(
                    "Las rutas de archivo no pueden estar vacías.", nameof(request));
        }

        foreach (var file in request.SourceFiles)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(
                    $"Archivo no encontrado: {file}", file);
        }
    }
}
