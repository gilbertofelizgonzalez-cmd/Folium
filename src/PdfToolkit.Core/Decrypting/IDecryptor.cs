namespace PdfToolkit.Core.Decrypting;
public interface IDecryptor
{
    Task<DecryptResult> DecryptAsync(DecryptRequest request, CancellationToken ct = default);
}
