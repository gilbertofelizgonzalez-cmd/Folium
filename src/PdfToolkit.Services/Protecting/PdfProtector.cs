using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Security;
using PdfToolkit.Core.Protecting;

namespace PdfToolkit.Services.Protecting;

public class PdfProtector : IProtector
{
    public async Task<ProtectResult> ProtectAsync(
        ProtectRequest request,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Archivo no encontrado.", request.SourcePath);

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(request.SourcePath, PdfDocumentOpenMode.Modify);
            var security = doc.SecuritySettings;
            security.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted128Bit;
            if (!string.IsNullOrEmpty(request.UserPassword))
                security.UserPassword = request.UserPassword;
            if (!string.IsNullOrEmpty(request.OwnerPassword))
                security.OwnerPassword = request.OwnerPassword;
            doc.Save(request.OutputPath);
        }, ct);

        return new ProtectResult(request.OutputPath, true);
    }

    public async Task<ProtectResult> UnprotectAsync(
        UnprotectRequest request,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Archivo no encontrado.", request.SourcePath);

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(request.SourcePath, request.Password, PdfDocumentOpenMode.Modify);
            doc.SecuritySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.None;
            doc.Save(request.OutputPath);
        }, ct);

        return new ProtectResult(request.OutputPath, false);
    }
}
