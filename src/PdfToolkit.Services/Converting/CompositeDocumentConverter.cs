using PdfToolkit.Core.Converting;

namespace PdfToolkit.Services.Converting;

/// <summary>
/// Usa Syncfusion si está disponible; si no, cae en LibreOffice.
/// </summary>
public class CompositeDocumentConverter : IDocumentConverter
{
    private readonly IDocumentConverter? _primary;
    private readonly IDocumentConverter? _fallback;
    private readonly IDocumentConverter? _active;

    public static readonly IReadOnlyList<string> AllDocumentExtensions;

    static CompositeDocumentConverter()
    {
        var combined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in SyncfusionDocumentConverter.AllExtensions) combined.Add(ext);
        foreach (var ext in LibreOfficeConverter.Extensions) combined.Add(ext);
        AllDocumentExtensions = combined.ToList();
    }

    public CompositeDocumentConverter(
        SyncfusionDocumentConverter syncfusion,
        LibreOfficeConverter libreOffice)
    {
        _primary = syncfusion.IsAvailable ? syncfusion : null;
        _fallback = libreOffice.IsAvailable ? libreOffice : null;
        _active = _primary ?? _fallback;
    }

    public bool IsAvailable => _active is not null;

    public string ConverterName => _active switch
    {
        { ConverterName: var name } => name,
        _ => "Ninguno disponible"
    };

    public IReadOnlyList<string> SupportedExtensions =>
        _active?.SupportedExtensions ?? Array.Empty<string>();

    public Task<string> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (_active is null)
            throw new PdfConvertException(
                "No hay conversor de documentos disponible. " +
                "Instala LibreOffice (gratis) o configura una clave de Syncfusion Community.");

        return _active.ConvertToPdfAsync(sourcePath, outputDirectory, cancellationToken);
    }
}
