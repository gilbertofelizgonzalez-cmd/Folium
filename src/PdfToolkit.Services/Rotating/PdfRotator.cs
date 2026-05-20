using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Rotating;

namespace PdfToolkit.Services.Rotating;

public class PdfRotator : IRotator
{
    public int GetPageCount(string sourcePath)
    {
        using var doc = PdfReader.Open(sourcePath, PdfDocumentOpenMode.InformationOnly);
        return doc.PageCount;
    }

    public async Task<RotateResult> RotateAsync(
        RotateRequest request,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.SourcePath))
            throw new FileNotFoundException("Archivo no encontrado.", request.SourcePath);

        int pageCount = await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            using var src = PdfReader.Open(request.SourcePath, PdfDocumentOpenMode.Import);
            var dst = new PdfDocument();

            for (int i = 0; i < src.PageCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                var page = dst.AddPage(src.Pages[i]);
                if (request.PageRotations.TryGetValue(i, out var degrees))
                    page.Rotate = NormalizeRotation(page.Rotate + degrees);
            }

            dst.Save(request.OutputPath);
            return dst.PageCount;
        }, ct);

        return new RotateResult(request.OutputPath, pageCount);
    }

    private static int NormalizeRotation(int degrees) => ((degrees % 360) + 360) % 360;
}
