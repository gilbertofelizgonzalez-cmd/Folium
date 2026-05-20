namespace PdfToolkit.Core.Splitting;

public record SplitResult(IReadOnlyList<string> OutputFiles, int TotalPages);
