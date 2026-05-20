namespace PdfToolkit.Core.Converting;

public interface IDocumentConverter
{
    bool IsAvailable { get; }
    string ConverterName { get; }
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>Convierte un documento a PDF y devuelve la ruta del PDF generado.</summary>
    Task<string> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
