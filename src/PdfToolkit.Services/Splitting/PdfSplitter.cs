using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Splitting;

namespace PdfToolkit.Services.Splitting;

public class PdfSplitter : ISplitter
{
    public int GetPageCount(string sourceFile)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException($"Archivo no encontrado: {sourceFile}", sourceFile);

        using var doc = PdfReader.Open(sourceFile, PdfDocumentOpenMode.InformationOnly);
        return doc.PageCount;
    }

    public async Task<SplitResult> SplitAsync(
        SplitRequest request,
        IProgress<SplitProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        return await Task.Run(() =>
        {
            PdfDocument sourceDoc;
            try
            {
                sourceDoc = PdfReader.Open(request.SourceFile, PdfDocumentOpenMode.Import);
            }
            catch (Exception ex)
            {
                throw new PdfSplitException(
                    $"No se pudo abrir el PDF de entrada: {ex.Message}", ex);
            }

            using (sourceDoc)
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(request.SourceFile);

                IReadOnlyList<int> pageIndices = request.PageNumbers is { Count: > 0 }
                    ? request.PageNumbers.Select(p => p - 1).ToList()
                    : Enumerable.Range(0, sourceDoc.PageCount).ToList();

                var outputFiles = new List<string>(pageIndices.Count);
                int total = pageIndices.Count;

                for (int i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int pageIndex = pageIndices[i];
                    int pageNumber = pageIndex + 1;

                    var outputPath = Path.Combine(
                        request.OutputDirectory,
                        $"{nameWithoutExt}_pagina_{pageNumber}.pdf");

                    progress?.Report(new SplitProgress(i, total, Path.GetFileName(outputPath)));

                    try
                    {
                        using var pageDoc = new PdfDocument();
                        pageDoc.AddPage(sourceDoc.Pages[pageIndex]);
                        pageDoc.Save(outputPath);
                        outputFiles.Add(outputPath);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        throw new PdfSplitException(
                            $"Error al guardar página {pageNumber}: {ex.Message}", ex);
                    }

                    progress?.Report(new SplitProgress(i + 1, total, Path.GetFileName(outputPath)));
                }

                return new SplitResult(outputFiles, sourceDoc.PageCount);
            }
        }, cancellationToken);
    }

    private static void ValidateRequest(SplitRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.SourceFile))
            throw new ArgumentException("El archivo fuente no puede estar vacío.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            throw new ArgumentException("El directorio de salida no puede estar vacío.", nameof(request));

        if (!File.Exists(request.SourceFile))
            throw new FileNotFoundException(
                $"Archivo no encontrado: {request.SourceFile}", request.SourceFile);

        if (!Directory.Exists(request.OutputDirectory))
            throw new DirectoryNotFoundException(
                $"Directorio no encontrado: {request.OutputDirectory}");
    }
}
