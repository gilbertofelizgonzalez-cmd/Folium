namespace PdfToolkit.Services.Converting;

public static class DocumentConverterDetector
{
    private static readonly string[] LibreOfficePaths =
    [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
        @"C:\Program Files\LibreOffice 7\program\soffice.exe",
        @"C:\Program Files\LibreOffice 24\program\soffice.exe",
        @"C:\Program Files\LibreOffice 25\program\soffice.exe",
    ];

    public static string? FindLibreOffice()
    {
        // Buscar en rutas conocidas
        var found = LibreOfficePaths.FirstOrDefault(File.Exists);
        if (found is not null) return found;

        // Buscar en PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
        foreach (var dir in pathDirs)
        {
            var candidate = Path.Combine(dir.Trim(), "soffice.exe");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static bool IsLibreOfficeAvailable() => FindLibreOffice() is not null;
}
