namespace PdfToolkit.Core.Watermarking;
public interface IWatermarker
{
    Task<WatermarkResult> AddWatermarkAsync(WatermarkRequest request, CancellationToken ct = default);
}
