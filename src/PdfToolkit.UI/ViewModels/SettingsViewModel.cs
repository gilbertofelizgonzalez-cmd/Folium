using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModernWpf;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IFileDialogService _fileDialog;

    public SettingsViewModel(ISettingsService settings, IFileDialogService fileDialog)
    {
        _settings = settings;
        _fileDialog = fileDialog;

        _themeSystem = settings.Current.Theme == "System";
        _themeLight = settings.Current.Theme == "Light";
        _themeDark = settings.Current.Theme == "Dark";
        _useFixedPath = settings.Current.OutputMode == "Fixed";
        _outputPath = settings.Current.OutputPath;
    }

    [ObservableProperty] private bool _themeSystem;
    [ObservableProperty] private bool _themeLight;
    [ObservableProperty] private bool _themeDark;

    partial void OnThemeSystemChanged(bool value)  { if (value) ApplyTheme("System"); }
    partial void OnThemeLightChanged(bool value)   { if (value) ApplyTheme("Light"); }
    partial void OnThemeDarkChanged(bool value)    { if (value) ApplyTheme("Dark"); }

    private void ApplyTheme(string theme)
    {
        _settings.Current.Theme = theme;
        ThemeManager.Current.ApplicationTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark" => ApplicationTheme.Dark,
            _ => null
        };
        _settings.Save();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedPathEnabled))]
    private bool _useFixedPath;

    [ObservableProperty] private string _outputPath;

    public bool IsFixedPathEnabled => UseFixedPath;

    partial void OnUseFixedPathChanged(bool value)
    {
        _settings.Current.OutputMode = value ? "Fixed" : "Ask";
        _settings.Save();
    }

    partial void OnOutputPathChanged(string value)
    {
        _settings.Current.OutputPath = value;
        _settings.Save();
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var folder = _fileDialog.OpenFolder("Seleccionar carpeta de salida predefinida");
        if (folder is not null)
            OutputPath = folder;
    }

    [RelayCommand]
    private void ResetOutputPath()
    {
        OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
