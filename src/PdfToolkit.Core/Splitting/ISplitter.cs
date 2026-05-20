namespace PdfToolkit.Core.Splitting;

public interface ISplitter
{
    int GetPageCount(string sourceFile);

    Task<SplitResult> SplitAsync(
        SplitRequest request,
        IProgress<SplitProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
