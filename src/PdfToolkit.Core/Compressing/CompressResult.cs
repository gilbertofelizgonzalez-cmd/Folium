namespace PdfToolkit.Core.Compressing;
public record CompressResult(string OutputPath, long OriginalBytes, long CompressedBytes)
{
    public double SavedPercent => OriginalBytes > 0
        ? (1.0 - (double)CompressedBytes / OriginalBytes) * 100.0
        : 0;
}
