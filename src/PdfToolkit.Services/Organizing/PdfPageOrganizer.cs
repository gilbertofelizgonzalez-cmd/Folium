using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Organizing;

namespace PdfToolkit.Services.Organizing;

public class PdfPageOrganizer : IPageOrganizer
{
    public int GetPageCount(string pdfPath)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.InformationOnly);
        return doc.PageCount;
    }

    public Task<OrganizeResult> OrganizeAsync(OrganizeRequest request, CancellationToken ct = default)
        => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var src = PdfReader.Open(request.SourcePath, PdfDocumentOpenMode.Import);
            var dst = new PdfDocument();
            foreach (var idx in request.PageOrder)
            {
                ct.ThrowIfCancellationRequested();
                dst.AddPage(src.Pages[idx]);
            }
            dst.Save(request.OutputPath);
            return new OrganizeResult(request.OutputPath, dst.PageCount);
        }, ct);
}
