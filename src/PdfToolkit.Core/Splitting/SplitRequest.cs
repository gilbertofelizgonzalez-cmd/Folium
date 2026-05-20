namespace PdfToolkit.Core.Splitting;

/// <param name="PageNumbers">Páginas a extraer (1-based). null = todas.</param>
public record SplitRequest(
    string SourceFile,
    string OutputDirectory,
    IReadOnlyList<int>? PageNumbers = null);
