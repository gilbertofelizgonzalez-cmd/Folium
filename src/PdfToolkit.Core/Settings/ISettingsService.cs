namespace PdfToolkit.Core.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
}
