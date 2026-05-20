using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Protecting;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class ProtectViewModel : BaseToolViewModel
{
    private readonly IProtector _protector;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public ProtectViewModel(IProtector protector, IFileDialogService fileDialog, ISettingsService settings)
    {
        _protector = protector;
        _fileDialog = fileDialog;
        _settings = settings;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExecute))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProtectMode))]
    [NotifyPropertyChangedFor(nameof(IsUnprotectMode))]
    private bool _modeProtect = true;

    [ObservableProperty] private bool _modeUnprotect;
    [ObservableProperty] private string _userPassword = string.Empty;
    [ObservableProperty] private string _ownerPassword = string.Empty;
    [ObservableProperty] private string _removePassword = string.Empty;
    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para proteger o desbloquear.";
    [ObservableProperty] private string? _lastOutputPath;

    public bool IsProtectMode => ModeProtect;
    public bool IsUnprotectMode => !ModeProtect;
    public bool HasResult => LastOutputPath is not null;
    public bool CanExecute => !IsProcessing && SourceFile is not null;

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
        ExecuteCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync()
    {
        var suffix = ModeProtect ? "_protegido" : "_desbloqueado";
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}{suffix}.pdf");
        if (outputPath is null) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;

        try
        {
            ProtectResult result;
            if (ModeProtect)
            {
                result = await _protector.ProtectAsync(
                    new ProtectRequest(SourceFile!, outputPath, UserPassword, OwnerPassword), _cts.Token);
                ShowInfoBar("PDF protegido", "Se aplicó cifrado de 128 bits al documento.", "Success");
            }
            else
            {
                result = await _protector.UnprotectAsync(
                    new UnprotectRequest(SourceFile!, outputPath, RemovePassword), _cts.Token);
                ShowInfoBar("PDF desbloqueado", "Se eliminó la protección del documento.", "Success");
            }

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));
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

    partial void OnModeProtectChanged(bool value)
    {
        if (value) ModeUnprotect = false;
        OnPropertyChanged(nameof(IsProtectMode));
        OnPropertyChanged(nameof(IsUnprotectMode));
    }

    partial void OnModeUnprotectChanged(bool value)
    {
        if (value) ModeProtect = false;
        OnPropertyChanged(nameof(IsProtectMode));
        OnPropertyChanged(nameof(IsUnprotectMode));
    }

    private string? ResolveOutputPath(string defaultFileName)
    {
        var s = _settings.Current;
        if (s.OutputMode == "Fixed" && Directory.Exists(s.OutputPath))
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(s.OutputPath, Path.GetFileNameWithoutExtension(defaultFileName) + $"_{stamp}.pdf");
        }
        return _fileDialog.SaveFile("Guardar PDF", "Archivos PDF|*.pdf", defaultFileName);
    }

}
