using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Security;
using PdfToolkit.Core.BatchProtecting;

namespace PdfToolkit.Services.BatchProtecting;

public class BatchProtector : IBatchProtector
{
    public async Task<BatchProtectResult> ProtectBatchAsync(
        IReadOnlyList<BatchProtectItem> items,
        string outputDirectory,
        IProgress<(int done, int total, string currentFile)>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var results = new List<EncryptedFileResult>();

        for (int i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = items[i];
            var fileName = Path.GetFileNameWithoutExtension(item.SourcePath) + "_cifrado.pdf";
            var outputPath = Path.Combine(outputDirectory, fileName);
            progress?.Report((i, items.Count, Path.GetFileName(item.SourcePath)));

            try
            {
                await Task.Run(() =>
                {
                    using var doc = PdfReader.Open(item.SourcePath, PdfDocumentOpenMode.Modify);
                    var sec = doc.SecuritySettings;
                    sec.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted128Bit;
                    sec.UserPassword  = item.Password;
                    sec.OwnerPassword = item.Password + "_owner";
                    doc.Save(outputPath);
                }, ct);

                results.Add(new EncryptedFileResult(item.SourcePath, outputPath, item.Password, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new EncryptedFileResult(item.SourcePath, outputPath, item.Password, false, ex.Message));
            }
        }

        progress?.Report((items.Count, items.Count, string.Empty));
        return new BatchProtectResult(results,
            results.Count(r => r.Success),
            results.Count(r => !r.Success));
    }
}
