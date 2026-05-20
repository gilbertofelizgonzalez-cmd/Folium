namespace PdfToolkit.Core.Converting;

public record ConvertRequest(IReadOnlyList<string> SourceFiles, string OutputPath);
