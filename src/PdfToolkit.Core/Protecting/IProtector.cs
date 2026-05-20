namespace PdfToolkit.Core.Protecting;
public interface IProtector
{
    Task<ProtectResult> ProtectAsync(ProtectRequest request, CancellationToken ct = default);
    Task<ProtectResult> UnprotectAsync(UnprotectRequest request, CancellationToken ct = default);
}
