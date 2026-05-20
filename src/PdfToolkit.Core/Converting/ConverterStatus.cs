namespace PdfToolkit.Core.Converting;

public record ConverterStatus(
    bool ImageConversionAvailable,
    bool DocumentConversionAvailable,
    string? DocumentConverterName,
    string StatusMessage);
