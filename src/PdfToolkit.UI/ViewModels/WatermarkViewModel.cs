using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Settings;
using PdfToolkit.Core.Watermarking;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class WatermarkViewModel : BaseToolViewModel
{
    private readonly IWatermarker _watermarker;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public WatermarkViewModel(IWatermarker watermarker, IFileDialogService fileDialog, ISettingsService settings)
    {
        _watermarker = watermarker;
        _fileDialog = fileDialog;
        _settings = settings;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanWatermark))]
    [NotifyCanExecuteChangedFor(nameof(WatermarkCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanWatermark))]
    [NotifyCanExecuteChangedFor(nameof(WatermarkCommand))]
    private bool _isProcessing;

    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para añadir marca de agua.";
    [ObservableProperty] private string? _lastOutputPath;

    // Watermark options
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanWatermark))]
    [NotifyCanExecuteChangedFor(nameof(WatermarkCommand))]
    private string _watermarkText = "CONFIDENCIAL";

    [ObservableProperty] private double _fontSize = 48;
    [ObservableProperty] private int _opacity = 40; // 0-100 slider, convert to 0-255
    [ObservableProperty] private double _angleDegrees = -45;
    [ObservableProperty] private string _colorHex = "#808080";

    public bool CanWatermark => !IsProcessing && SourceFile is not null && !string.IsNullOrWhiteSpace(WatermarkText);
    public bool HasResult => LastOutputPath is not null;

    [RelayCommand]
    private void SelectSource()
    {
        var file = _fileDialog.OpenFile("Seleccionar PDF", "Archivos PDF|*.pdf");
        if (file is null) return;
        LoadFile(file);
    }

    public void DropFile(string path) => LoadFile(path);

    private void LoadFile(string file)
    {
        SourceFile = file;
        LastOutputPath = null;
        OnPropertyChanged(nameof(HasResult));
        StatusMessage = $"Archivo: {Path.GetFileName(file)}";
        WatermarkCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SetPreset(string text)
    {
        WatermarkText = text;
    }

    [RelayCommand(CanExecute = nameof(CanWatermark))]
    private async Task WatermarkAsync()
    {
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}_marca_agua.pdf");
        if (outputPath is null) return;

        int opacityByte = (int)Math.Round(Opacity / 100.0 * 255);

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;

        try
        {
            var result = await _watermarker.AddWatermarkAsync(
                new WatermarkRequest(SourceFile!, outputPath, WatermarkText, FontSize, opacityByte, AngleDegrees, ColorHex, true),
                _cts.Token);

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));
            ShowInfoBar("Marca de agua aplicada",
                $"PDF guardado con marca de agua en {result.PageCount} página(s).", "Success");
        }
        catch (OperationCanceledException) { ShowInfoBar("Cancelado", "Operación cancelada.", "Warning"); }
        catch (Exception ex) { ShowInfoBar("Error", ex.Message, "Error"); }
        finally { IsProcessing = false; _cts?.Dispose(); _cts = null; }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void OpenResult()
    {
        if (LastOutputPath is not null && File.Exists(LastOutputPath))
            Process.Start(new ProcessStartInfo(LastOutputPath) { UseShellExecute = true });
    }

    private string? ResolveOutputPath(string defaultFileName)
    {
        var s = _settings.Current;
        if (s.OutputMode == "Fixed" && Directory.Exists(s.OutputPath))
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(s.OutputPath, Path.GetFileNameWithoutExtension(defaultFileName) + $"_{stamp}.pdf");
        }
        return _fileDialog.SaveFile("Guardar PDF con marca de agua", "Archivos PDF|*.pdf", defaultFileName);
    }
}
