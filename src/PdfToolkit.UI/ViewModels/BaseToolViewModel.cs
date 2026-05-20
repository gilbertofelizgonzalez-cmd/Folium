using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdfToolkit.UI.ViewModels;

public partial class BaseToolViewModel : ObservableObject
{
    [ObservableProperty] private bool _isInfoBarOpen;
    [ObservableProperty] private string _infoBarTitle = string.Empty;
    [ObservableProperty] private string _infoBarMessage = string.Empty;
    [ObservableProperty] private string _infoBarSeverity = "Info";

    protected void ShowInfoBar(string title, string message, string severity = "Info")
    {
        InfoBarTitle = title;
        InfoBarMessage = message;
        InfoBarSeverity = severity;
        IsInfoBarOpen = true;
    }

    [RelayCommand]
    protected virtual void CloseInfoBar() => IsInfoBarOpen = false;
}
