namespace PdfToolkit.Core.Splitting;

public record SplitProgress(
    int PagesProcessed,
    int TotalPages,
    string? CurrentOutputFile)
{
    public double Percentage => TotalPages == 0 ? 0 : (PagesProcessed * 100.0) / TotalPages;
}
