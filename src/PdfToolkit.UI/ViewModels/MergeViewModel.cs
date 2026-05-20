using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Merging;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Models;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public enum NotificationSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

public partial class MergeViewModel : BaseToolViewModel
{
    private readonly IPdfMerger _pdfMerger;
    private readonly IFileDialogService _dialogService;
    private readonly IThumbnailService _thumbnailService;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    private const int ThumbnailWidth = 80;

    public MergeViewModel(
        IPdfMerger pdfMerger,
        IFileDialogService dialogService,
        IThumbnailService thumbnailService,
        ISettingsService settings)
    {
        _pdfMerger = pdfMerger;
        _dialogService = dialogService;
        _thumbnailService = thumbnailService;
        _settings = settings;

        SourceFiles.CollectionChanged += (_, _) =>
        {
            UpdateStatusMessage();
            MergeCommand.NotifyCanExecuteChanged();
            ClearAllCommand.NotifyCanExecuteChanged();
        };
    }

    public ObservableCollection<PdfFileItem> SourceFiles { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Selecciona al menos 2 archivos PDF para unir.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelMergeCommand))]
    private bool _isProcessing;

    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string? _currentFileName;

    public void DropFiles(string[] paths)
    {
        foreach (var file in paths.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
        {
            if (SourceFiles.Any(item => item.FilePath == file)) continue;
            var item = new PdfFileItem(file);
            SourceFiles.Add(item);
            _ = LoadThumbnailAsync(item);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void SelectFiles()
    {
        var files = _dialogService.OpenMultipleFiles(
            title: "Seleccionar PDFs para unir",
            filter: "Archivos PDF (*.pdf)|*.pdf");

        if (files is null || files.Count == 0) return;

        foreach (var file in files)
        {
            if (SourceFiles.Any(item => item.FilePath == file)) continue;

            var item = new PdfFileItem(file);
            SourceFiles.Add(item);

            _ = LoadThumbnailAsync(item);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void Remove(PdfFileItem? item)
    {
        if (item is null) return;
        SourceFiles.Remove(item);
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void MoveUp(PdfFileItem? item)
    {
        if (item is null) return;
        var index = SourceFiles.IndexOf(item);
        if (index > 0)
            SourceFiles.Move(index, index - 1);
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void MoveDown(PdfFileItem? item)
    {
        if (item is null) return;
        var index = SourceFiles.IndexOf(item);
        if (index >= 0 && index < SourceFiles.Count - 1)
            SourceFiles.Move(index, index + 1);
    }

    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private void ClearAll() => SourceFiles.Clear();

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelMerge() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private async Task MergeAsync()
    {
        var outputPath = ResolveOutputPath("PDFs_unidos.pdf");
        if (outputPath is null) return;

        IsInfoBarOpen = false;
        IsProcessing = true;
        ProgressPercentage = 0;
        StatusMessage = "Uniendo archivos...";

        _cts = new CancellationTokenSource();

        try
        {
            var paths = SourceFiles.Select(item => item.FilePath).ToList();
            var request = new MergeRequest(paths, outputPath);

            var progress = new Progress<MergeProgress>(p =>
            {
                ProgressPercentage = p.Percentage;
                CurrentFileName = p.CurrentFileName;
            });

            var result = await _pdfMerger.MergeAsync(request, progress, _cts.Token);

            var sizeKb = result.OutputSizeBytes / 1024.0;
            ShowInfoBar(
                title: "PDF creado correctamente",
                message: $"{result.TotalPages} páginas · {sizeKb:F1} KB · {outputPath}",
                severity: "Success");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar(
                title: "Operación cancelada",
                message: "El merge fue interrumpido por el usuario.",
                severity: "Info");
        }
        catch (PdfMergeException ex)
        {
            ShowInfoBar(
                title: "Error al unir PDFs",
                message: ex.Message,
                severity: "Error");
        }
        catch (Exception ex)
        {
            ShowInfoBar(
                title: "Error inesperado",
                message: ex.Message,
                severity: "Error");
        }
        finally
        {
            IsProcessing = false;
            CurrentFileName = null;
            _cts?.Dispose();
            _cts = null;
            UpdateStatusMessage();
        }
    }

    private async Task LoadThumbnailAsync(PdfFileItem item)
    {
        try
        {
            item.Thumbnail = await _thumbnailService.GenerateAsync(
                item.FilePath,
                ThumbnailWidth);
        }
        catch
        {
            // thumbnail failed, placeholder stays visible
        }
        finally
        {
            item.IsLoadingThumbnail = false;
        }
    }

    private bool CanInteract() => !IsProcessing;
    private bool CanMerge() => SourceFiles.Count >= 2 && !IsProcessing;
    private bool CanClearAll() => SourceFiles.Count > 0 && !IsProcessing;
    private bool CanCancel() => IsProcessing;

    private void UpdateStatusMessage()
    {
        if (IsProcessing) return;

        StatusMessage = SourceFiles.Count switch
        {
            0 => "Selecciona al menos 2 archivos PDF para unir.",
            1 => "1 archivo seleccionado. Necesitas al menos 1 más.",
            _ => $"{SourceFiles.Count} archivos listos para unir."
        };
    }

    private string? ResolveOutputPath(string defaultFileName)
    {
        var s = _settings.Current;
        if (s.OutputMode == "Fixed" && Directory.Exists(s.OutputPath))
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var name  = Path.GetFileNameWithoutExtension(defaultFileName) + $"_{stamp}.pdf";
            return Path.Combine(s.OutputPath, name);
        }
        return _dialogService.SaveFile("Guardar PDF unido", "Archivos PDF (*.pdf)|*.pdf", defaultFileName);
    }
}
