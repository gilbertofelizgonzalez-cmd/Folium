namespace PdfToolkit.Core.Converting;

public interface IConverter
{
    Task<ConvertResult> ConvertAsync(
        ConvertRequest request,
        IProgress<ConvertProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
