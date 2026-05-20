namespace PdfToolkit.Core.BatchProtecting;

public record EncryptedFileResult(
    string SourcePath,
    string OutputPath,
    string Password,
    bool   Success,
    string? ErrorMessage);

public record BatchProtectResult(
    IReadOnlyList<EncryptedFileResult> Results,
    int SuccessCount,
    int ErrorCount);
