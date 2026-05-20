namespace PdfToolkit.UI.Services;

public interface IFileDialogService
{
    string? OpenFile(string title, string filter);
    IReadOnlyList<string>? OpenMultipleFiles(string title, string filter);
    string? SaveFile(string title, string filter, string defaultFileName);
    string? OpenFolder(string title);
}
