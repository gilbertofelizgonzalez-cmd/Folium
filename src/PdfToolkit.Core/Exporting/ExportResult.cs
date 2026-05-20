namespace PdfToolkit.Core.Exporting;
public record ExportResult(string OutputDirectory, int ExportedPages, IReadOnlyList<string> FilePaths);
