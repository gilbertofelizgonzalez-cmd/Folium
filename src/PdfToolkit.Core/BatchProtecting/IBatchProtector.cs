namespace PdfToolkit.Core.BatchProtecting;

public interface IBatchProtector
{
    Task<BatchProtectResult> ProtectBatchAsync(
        IReadOnlyList<BatchProtectItem> items,
        string outputDirectory,
        IProgress<(int done, int total, string currentFile)>? progress = null,
        CancellationToken ct = default);
}
