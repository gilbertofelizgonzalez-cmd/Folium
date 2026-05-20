namespace PdfToolkit.Core.Exporting;
public interface IPageExporter
{
    int GetPageCount(string sourcePath);
    Task<ExportResult> ExportAsync(ExportRequest request, IProgress<ExportProgress>? progress = null, CancellationToken ct = default);
}
