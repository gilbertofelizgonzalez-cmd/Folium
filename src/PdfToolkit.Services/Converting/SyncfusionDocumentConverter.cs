using PdfToolkit.Core.Converting;

namespace PdfToolkit.Services.Converting;

/// <summary>
/// Stub de Syncfusion — desactivado hasta que los paquetes sean compatibles con net10.0.
/// Para activar: descomenta los PackageReference en PdfToolkit.Services.csproj e implementa los métodos ConvertWord/Presentation/Excel.
/// </summary>
public class SyncfusionDocumentConverter : IDocumentConverter
{
    public static readonly IReadOnlyList<string> AllExtensions =
    [
        ".docx", ".doc", ".rtf", ".odt", ".txt", ".html", ".htm",
        ".pptx", ".ppt", ".odp",
        ".xlsx", ".xls", ".ods", ".csv"
    ];

    public bool IsAvailable => false;
    public string ConverterName => "Syncfusion";
    public IReadOnlyList<string> SupportedExtensions => AllExtensions;

    public Task<string> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
        => throw new PdfConvertException("Syncfusion no está disponible en esta versión.");
}
