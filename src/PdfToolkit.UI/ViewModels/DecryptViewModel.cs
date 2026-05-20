using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Decrypting;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class DecryptViewModel : BaseToolViewModel
{
    private readonly IDecryptor _decryptor;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public DecryptViewModel(IDecryptor decryptor, IFileDialogService fileDialog, ISettingsService settings)
    {
        _decryptor = decryptor;
        _fileDialog = fileDialog;
        _settings = settings;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDecrypt))]
    [NotifyCanExecuteChangedFor(nameof(DecryptCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDecrypt))]
    [NotifyCanExecuteChangedFor(nameof(DecryptCommand))]
    private bool _isProcessing;

    [ObservableProperty] private string _statusMessage = "Selecciona un PDF protegido para quitar la contraseña.";
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _lastOutputPath;

    public bool CanDecrypt => !IsProcessing && SourceFile is not null;
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
        DecryptCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDecrypt))]
    private async Task DecryptAsync()
    {
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}_sin_contraseña.pdf");
        if (outputPath is null) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;

        try
        {
            var result = await _decryptor.DecryptAsync(
                new DecryptRequest(SourceFile!, outputPath, Password), _cts.Token);

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));
            ShowInfoBar("Contraseña eliminada",
                $"PDF sin protección guardado: {result.PageCount} página(s).", "Success");
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
        return _fileDialog.SaveFile("Guardar PDF sin contraseña", "Archivos PDF|*.pdf", defaultFileName);
    }
}
