using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Compressing;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class CompressViewModel : BaseToolViewModel
{
    private readonly ICompressor _compressor;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public CompressViewModel(ICompressor compressor, IFileDialogService fileDialog, ISettingsService settings)
    {
        _compressor = compressor;
        _fileDialog = fileDialog;
        _settings = settings;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompress))]
    [NotifyCanExecuteChangedFor(nameof(CompressCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompress))]
    [NotifyCanExecuteChangedFor(nameof(CompressCommand))]
    private bool _isProcessing;

    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para comprimir.";
    [ObservableProperty] private string? _lastOutputPath;

    public bool CanCompress => !IsProcessing && SourceFile is not null;
    public bool HasResult => LastOutputPath is not null;

    [RelayCommand]
    private void SelectSource()
    {
        var file = _fileDialog.OpenFile("Seleccionar PDF", "Archivos PDF|*.pdf");
        if (file is null) return;
        LoadFile(file);
    }

    public void DropFile(string path) => LoadFile(path);

    private void LoadFile(string path)
    {
        SourceFile = path;
        StatusMessage = $"Listo para comprimir: {Path.GetFileName(path)}";
        LastOutputPath = null;
        OnPropertyChanged(nameof(HasResult));
    }

    [RelayCommand(CanExecute = nameof(CanCompress))]
    private async Task CompressAsync()
    {
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}_comprimido.pdf");
        if (outputPath is null) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;
        ProgressPercentage = 0;

        try
        {
            var progress = new Progress<int>(p => ProgressPercentage = p);
            var result = await _compressor.CompressAsync(
                new CompressRequest(SourceFile!, outputPath), progress, _cts.Token);

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));

            var saved = result.SavedPercent;
            var msg = saved > 0
                ? $"Reducido un {saved:F1}% — de {result.OriginalBytes / 1024.0:F0} KB a {result.CompressedBytes / 1024.0:F0} KB"
                : $"Guardado ({result.CompressedBytes / 1024.0:F0} KB). El PDF ya estaba optimizado.";

            ShowInfoBar("Compresión completada", msg, "Success");
        }
        catch (OperationCanceledException) { ShowInfoBar("Cancelado", "Operación cancelada.", "Warning"); }
        catch (Exception ex) { ShowInfoBar("Error", ex.Message, "Error"); }
        finally
        {
            IsProcessing = false;
            ProgressPercentage = 0;
            _cts?.Dispose();
            _cts = null;
        }
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
        return _fileDialog.SaveFile("Guardar PDF comprimido", "Archivos PDF|*.pdf", defaultFileName);
    }

}
