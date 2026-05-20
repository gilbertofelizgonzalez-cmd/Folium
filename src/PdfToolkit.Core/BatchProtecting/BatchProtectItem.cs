namespace PdfToolkit.Core.BatchProtecting;

public class BatchProtectItem
{
    public required string SourcePath { get; init; }
    public required string Password   { get; init; }
}
