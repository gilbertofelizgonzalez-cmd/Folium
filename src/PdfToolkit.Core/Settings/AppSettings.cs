namespace PdfToolkit.Core.Settings;

public class AppSettings
{
    public string Theme { get; set; } = "System";   // "System" | "Light" | "Dark"
    public string OutputMode { get; set; } = "Ask"; // "Ask" | "Fixed"
    public string OutputPath { get; set; } = "";
}
