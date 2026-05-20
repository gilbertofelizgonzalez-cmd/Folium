namespace PdfToolkit.Core.Merging;

public record MergeProgress(
    int FilesProcessed,
    int TotalFiles,
    string? CurrentFileName)
{
    public double Percentage => TotalFiles == 0 ? 0 : (FilesProcessed * 100.0) / TotalFiles;
}
