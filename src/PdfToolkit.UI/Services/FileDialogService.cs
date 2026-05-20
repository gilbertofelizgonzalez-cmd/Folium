using Microsoft.Win32;

namespace PdfToolkit.UI.Services;

public class FileDialogService : IFileDialogService
{
    public string? OpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public IReadOnlyList<string>? OpenMultipleFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFolder(string title)
    {
        // OpenFolderDialog está disponible en .NET 8+ (Microsoft.Win32 en WindowsBase)
        var dialog = new OpenFolderDialog
        {
            Title = title
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
