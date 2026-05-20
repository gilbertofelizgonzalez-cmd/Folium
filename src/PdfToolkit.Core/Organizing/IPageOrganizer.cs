namespace PdfToolkit.Core.Organizing;
public interface IPageOrganizer
{
    int GetPageCount(string pdfPath);
    Task<OrganizeResult> OrganizeAsync(OrganizeRequest request, CancellationToken ct = default);
}
