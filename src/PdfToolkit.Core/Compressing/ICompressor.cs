namespace PdfToolkit.Core.Compressing;
public interface ICompressor
{
    Task<CompressResult> CompressAsync(CompressRequest request, IProgress<int>? progress = null, CancellationToken ct = default);
}
