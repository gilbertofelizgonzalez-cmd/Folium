using System.Diagnostics;
using PdfToolkit.Core.Converting;

namespace PdfToolkit.Services.Converting;

public class LibreOfficeConverter : IDocumentConverter
{
    private readonly string? _executablePath;

    public static readonly IReadOnlyList<string> Extensions =
    [
        ".docx", ".doc", ".rtf", ".odt", ".ott", ".txt", ".fodt",
        ".pptx", ".ppt", ".odp", ".otp", ".fodp",
        ".xlsx", ".xls", ".ods", ".ots", ".csv", ".fods",
        ".html", ".htm", ".xml"
    ];

    public LibreOfficeConverter()
    {
        _executablePath = DocumentConverterDetector.FindLibreOffice();
    }

    public bool IsAvailable => _executablePath is not null;
    public string ConverterName => "LibreOffice";
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public async Task<string> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new PdfConvertException("LibreOffice no está instalado.");

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = $"--headless --convert-to pdf --outdir \"{outputDirectory}\" \"{sourcePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Timeout de 5 minutos por documento
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new PdfConvertException(
                $"Tiempo de espera agotado al convertir '{Path.GetFileName(sourcePath)}'.");
        }

        if (process.ExitCode != 0)
        {
            var err = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new PdfConvertException(
                $"LibreOffice falló al convertir '{Path.GetFileName(sourcePath)}': {err}");
        }

        // LibreOffice nombra el output igual que el input pero con .pdf
        var outputFile = Path.Combine(
            outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + ".pdf");

        if (!File.Exists(outputFile))
            throw new PdfConvertException(
                $"LibreOffice no generó el archivo esperado: {outputFile}");

        return outputFile;
    }
}
