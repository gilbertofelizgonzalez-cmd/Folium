namespace PdfToolkit.Core.Exporting;
public record ExportRequest(
    string SourcePath,
    string OutputDirectory,
    string Format,          // "jpg" | "png"
    int Dpi,
    IReadOnlyList<int>? PageNumbers = null); // null = todas, 1-based
