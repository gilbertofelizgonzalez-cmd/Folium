namespace PdfToolkit.Core.Protecting;
public record ProtectRequest(string SourcePath, string OutputPath, string UserPassword, string OwnerPassword);
