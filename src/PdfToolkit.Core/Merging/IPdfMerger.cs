namespace PdfToolkit.Core.Merging;

public interface IPdfMerger
{
    Task<MergeResult> MergeAsync(
        MergeRequest request,
        IProgress<MergeProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
