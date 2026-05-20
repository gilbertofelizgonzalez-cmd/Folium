namespace PdfToolkit.Core.Merging;

public record MergeRequest(IReadOnlyList<string> SourceFiles, string OutputPath);
