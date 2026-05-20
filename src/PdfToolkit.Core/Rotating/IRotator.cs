namespace PdfToolkit.Core.Rotating;
public interface IRotator
{
    int GetPageCount(string sourcePath);
    Task<RotateResult> RotateAsync(RotateRequest request, CancellationToken ct = default);
}
