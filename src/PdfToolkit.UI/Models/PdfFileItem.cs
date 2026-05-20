using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfToolkit.UI.Models;

public partial class PdfFileItem : ObservableObject
{
    public PdfFileItem(string filePath)
    {
        FilePath = filePath;
        IsLoadingThumbnail = true;
    }

    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);

    [ObservableProperty]
    private BitmapImage? _thumbnail;

    [ObservableProperty]
    private bool _isLoadingThumbnail;
}
