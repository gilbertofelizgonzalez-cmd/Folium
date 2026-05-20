using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.BatchProtecting;
using PdfToolkit.Services.BatchProtecting;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class BatchFileItem : ObservableObject
{
    public required string SourcePath { get; init; }
    public string FileName => Path.GetFileName(SourcePath);

    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _statusIcon = "⏳";
    [ObservableProperty] private string _statusText = "Pendiente";
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private bool _isError;
}

public partial class BatchProtectViewModel : BaseToolViewModel
{
    private readonly IBatchProtector _batchProtector;
    private readonly IFileDialogService _fileDialog;
    private CancellationTokenSource? _cts;

    public ObservableCollection<BatchFileItem> Files { get; } = new();

    public BatchProtectViewModel(IBatchProtector batchProtector, IFileDialogService fileDialog)
    {
        _batchProtector = batchProtector;
        _fileDialog = fileDialog;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EncryptAllCommand))]
    private bool _isProcessing;
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private int _progressMax = 100;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private string _statusMessage = "Agrega los PDFs que quieres cifrar.";
    [ObservableProperty] private string? _lastOutputDirectory;

    [ObservableProperty] private bool _modeUniquePerFile = true;
    [ObservableProperty] private bool _modeSameForAll;
    [ObservableProperty] private string _commonPassword = string.Empty;
    [ObservableProperty] private int _pwLength = 12;
    [ObservableProperty] private bool _pwUpper = true;
    [ObservableProperty] private bool _pwLower = true;
    [ObservableProperty] private bool _pwDigits = true;
    [ObservableProperty] private bool _pwSymbols;
    [ObservableProperty] private string _passwordPreview = string.Empty;

    public bool HasFiles   => Files.Count > 0;
    public bool HasNoFiles => Files.Count == 0;
    public bool CanEncrypt => HasFiles && !IsProcessing;
    public bool HasNoPasswordPreview => string.IsNullOrEmpty(PasswordPreview);

    partial void OnModeUniquePerFileChanged(bool value) { if (value) ModeSameForAll = false; }
    partial void OnModeSameForAllChanged(bool value)    { if (value) ModeUniquePerFile = false; }
    partial void OnPasswordPreviewChanged(string value) { OnPropertyChanged(nameof(HasNoPasswordPreview)); }

    public void DropFiles(string[] paths)
    {
        foreach (var f in paths.Where(p => p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            if (Files.All(x => x.SourcePath != f))
                Files.Add(new BatchFileItem { SourcePath = f });
        RefreshState();
    }

    [RelayCommand]
    private void AddFiles()
    {
        var files = _fileDialog.OpenMultipleFiles("Seleccionar PDFs", "Archivos PDF|*.pdf");
        if (files is null) return;
        foreach (var f in files)
            if (Files.All(x => x.SourcePath != f))
                Files.Add(new BatchFileItem { SourcePath = f });
        RefreshState();
    }

    [RelayCommand]
    private void AddFolder()
    {
        var folder = _fileDialog.OpenFolder("Seleccionar carpeta con PDFs");
        if (folder is null) return;
        foreach (var f in Directory.GetFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly))
            if (Files.All(x => x.SourcePath != f))
                Files.Add(new BatchFileItem { SourcePath = f });
        RefreshState();
    }

    [RelayCommand]
    private void RemoveFile(BatchFileItem item) { Files.Remove(item); RefreshState(); }

    [RelayCommand]
    private void ClearList() { Files.Clear(); RefreshState(); }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasNoFiles));
        OnPropertyChanged(nameof(CanEncrypt));
        EncryptAllCommand.NotifyCanExecuteChanged();
        StatusMessage = Files.Count > 0
            ? $"{Files.Count} archivo(s) listo(s). Genera contraseñas y cifra."
            : "Agrega los PDFs que quieres cifrar.";
    }

    [RelayCommand]
    private void GeneratePreview()
    {
        PasswordPreview = PasswordGenerator.Generate(PwLength, PwLower, PwUpper, PwDigits, PwSymbols);
    }

    [RelayCommand]
    private void AssignPasswords()
    {
        if (ModeSameForAll)
        {
            var pw = string.IsNullOrWhiteSpace(CommonPassword)
                ? PasswordGenerator.Generate(PwLength, PwLower, PwUpper, PwDigits, PwSymbols)
                : CommonPassword;
            foreach (var f in Files) f.Password = pw;
            if (string.IsNullOrWhiteSpace(CommonPassword)) CommonPassword = pw;
        }
        else
        {
            foreach (var f in Files)
                f.Password = PasswordGenerator.Generate(PwLength, PwLower, PwUpper, PwDigits, PwSymbols);
        }
        PasswordPreview = Files.FirstOrDefault()?.Password ?? string.Empty;
    }

    [RelayCommand]
    private void DecreaseLength() { if (PwLength > 6) PwLength--; }

    [RelayCommand]
    private void IncreaseLength() { if (PwLength < 32) PwLength++; }

    [RelayCommand(CanExecute = nameof(CanEncrypt))]
    private async Task EncryptAllAsync()
    {
        if (Files.Any(f => string.IsNullOrWhiteSpace(f.Password)))
            AssignPasswords();

        var outputDir = _fileDialog.OpenFolder("Carpeta donde guardar los PDFs cifrados");
        if (outputDir is null) return;

        var items = Files.Select(f => new BatchProtectItem
        {
            SourcePath = f.SourcePath,
            Password = f.Password
        }).ToList();

        foreach (var f in Files)
        {
            f.StatusIcon = "⏳"; f.StatusText = "Procesando...";
            f.IsSuccess  = false; f.IsError    = false;
        }

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;
        ProgressMax = items.Count;
        ProgressValue = 0;

        try
        {
            var progress = new Progress<(int done, int total, string file)>(p =>
            {
                ProgressValue = p.done;
                ProgressText = p.file.Length > 0 ? $"Cifrando: {p.file}" : "Completado";
            });

            var result = await _batchProtector.ProtectBatchAsync(items, outputDir, progress, _cts.Token);

            foreach (var r in result.Results)
            {
                var item = Files.FirstOrDefault(f => f.SourcePath == r.SourcePath);
                if (item is null) continue;
                if (r.Success)
                {
                    item.StatusIcon = "✅"; item.StatusText = "Cifrado";
                    item.IsSuccess  = true;
                }
                else
                {
                    item.StatusIcon = "❌"; item.StatusText = r.ErrorMessage ?? "Error";
                    item.IsError    = true;
                }
            }

            LastOutputDirectory = outputDir;
            HasResults = true;
            OnPropertyChanged(nameof(CanEncrypt));

            ShowInfoBar(
                result.ErrorCount == 0 ? "Cifrado completado" : "Completado con errores",
                $"{result.SuccessCount} archivo(s) cifrado(s)" +
                (result.ErrorCount > 0 ? $", {result.ErrorCount} con error." : "."),
                result.ErrorCount == 0 ? "Success" : "Warning");
        }
        catch (OperationCanceledException) { ShowInfoBar("Cancelado", "Operación cancelada.", "Warning"); }
        catch (Exception ex)               { ShowInfoBar("Error", ex.Message, "Error"); }
        finally
        {
            IsProcessing = false;
            ProgressValue = 0;
            ProgressText = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void OpenResultFolder()
    {
        if (LastOutputDirectory is not null && Directory.Exists(LastOutputDirectory))
            Process.Start(new ProcessStartInfo(LastOutputDirectory) { UseShellExecute = true });
    }

    [RelayCommand]
    private void ExportPasswordList()
    {
        var savePath = _fileDialog.SaveFile(
            "Exportar lista de contraseñas", "CSV|*.csv|Texto|*.txt", "contraseñas.csv");
        if (savePath is null) return;

        var sb    = new StringBuilder();
        bool isCsv = savePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            sb.AppendLine("Archivo,Contraseña,Estado");
            foreach (var f in Files)
                sb.AppendLine($"\"{f.FileName}\",\"{f.Password}\",\"{f.StatusText}\"");
        }
        else
        {
            sb.AppendLine("LISTA DE CONTRASEÑAS");
            sb.AppendLine(new string('-', 60));
            foreach (var f in Files)
                sb.AppendLine($"{f.FileName,-45}  {f.Password}");
        }

        File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
        Process.Start(new ProcessStartInfo(savePath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void CopyPasswordList()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Archivo\tContraseña");
        foreach (var f in Files) sb.AppendLine($"{f.FileName}\t{f.Password}");
        Clipboard.SetText(sb.ToString());
        ShowInfoBar("Copiado", "Lista de contraseñas copiada al portapapeles.", "Success");
    }

}
