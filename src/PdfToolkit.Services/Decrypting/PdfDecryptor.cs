using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Security;
using PdfToolkit.Core.Decrypting;

namespace PdfToolkit.Services.Decrypting;

public class PdfDecryptor : IDecryptor
{
    public Task<DecryptResult> DecryptAsync(DecryptRequest request, CancellationToken ct = default)
        => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(request.SourcePath, request.Password, PdfDocumentOpenMode.Modify);
            doc.SecuritySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.None;
            doc.Save(request.OutputPath);
            return new DecryptResult(request.OutputPath, doc.PageCount);
        }, ct);
}
