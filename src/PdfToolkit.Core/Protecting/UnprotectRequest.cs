namespace PdfToolkit.Core.Protecting;
public record UnprotectRequest(string SourcePath, string OutputPath, string Password);
