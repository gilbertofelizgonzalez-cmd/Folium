using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Exporting;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class ExportViewModel : BaseToolViewModel
{
    private readonly IPageExporter _exporter;
    private readonly IFileDialogService _fileDialog;
    private CancellationTokenSource? _cts;

    public ExportViewModel(IPageExporter exporter, IFileDialogService fileDialog)
    {
        _exporter = exporter;
        _fileDialog = fileDialog;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _isProcessing;

    [ObservableProperty] private int _totalPageCount;
    [ObservableProperty] private bool _hasPageCount;
    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para exportar sus páginas como imágenes.";
    [ObservableProperty] private string _currentFileName = string.Empty;
    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string? _lastOutputDirectory;

    // Opciones
    [ObservableProperty] private bool _formatJpg = true;
    [ObservableProperty] private bool _formatPng;
    [ObservableProperty] private int _selectedDpi = 150;
    [ObservableProperty] private bool _dpi72;
    [ObservableProperty] private bool _dpi150 = true;
    [ObservableProperty] private bool _dpi300;
    [ObservableProperty] private bool _dpi600;
    [ObservableProperty] private bool _exportAllPages = true;
    [ObservableProperty] private bool _exportSpecificPages;
    [ObservableProperty] private string _pageRangeText = string.Empty;

    partial void OnDpi72Changed(bool value)  { if (value) { SelectedDpi = 72;  _dpi150 = false; _dpi300 = false; _dpi600 = false; } }
    partial void OnDpi150Changed(bool value) { if (value) { SelectedDpi = 150; _dpi72  = false; _dpi300 = false; _dpi600 = false; } }
    partial void OnDpi300Changed(bool value) { if (value) { SelectedDpi = 300; _dpi72  = false; _dpi150 = false; _dpi600 = false; } }
    partial void OnDpi600Changed(bool value) { if (value) { SelectedDpi = 600; _dpi72  = false; _dpi150 = false; _dpi300 = false; } }

    public bool HasResult => LastOutputDirectory is not null;
    public bool CanExport => !IsProcessing && SourceFile is not null;

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
        LastOutputDirectory = null;
        OnPropertyChanged(nameof(HasResult));
        var count = _exporter.GetPageCount(file);
        TotalPageCount = count;
        HasPageCount = count > 0;
        StatusMessage = $"{count} página(s) — elige el formato y la resolución.";
        ExportCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        var folder = _fileDialog.OpenFolder("Carpeta de destino para las imágenes");
        if (folder is null) return;

        IReadOnlyList<int>? pageNumbers = null;
        if (ExportSpecificPages && !string.IsNullOrWhiteSpace(PageRangeText))
        {
            var (pages, _) = ParseRange(PageRangeText, TotalPageCount);
            pageNumbers = pages;
        }

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;
        ProgressPercentage = 0;

        try
        {
            var fmt = FormatPng ? "png" : "jpg";
            var request = new ExportRequest(SourceFile!, folder, fmt, SelectedDpi, pageNumbers);
            var progress = new Progress<ExportProgress>(p =>
            {
                CurrentFileName = p.CurrentFile;
                ProgressPercentage = p.TotalPages > 0 ? (double)p.ProcessedPages / p.TotalPages * 100 : 0;
            });

            var result = await _exporter.ExportAsync(request, progress, _cts.Token);
            LastOutputDirectory = result.OutputDirectory;
            OnPropertyChanged(nameof(HasResult));
            ShowInfoBar("Exportación completada",
                $"{result.ExportedPages} imagen(es) guardadas en {folder}", "Success");
        }
        catch (OperationCanceledException) { ShowInfoBar("Cancelado", "Operación cancelada.", "Warning"); }
        catch (Exception ex) { ShowInfoBar("Error", ex.Message, "Error"); }
        finally
        {
            IsProcessing = false;
            ProgressPercentage = 0;
            CurrentFileName = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void OpenResult()
    {
        if (LastOutputDirectory is not null && Directory.Exists(LastOutputDirectory))
            Process.Start(new ProcessStartInfo(LastOutputDirectory) { UseShellExecute = true });
    }

    private static (IReadOnlyList<int>? pages, string? error) ParseRange(string text, int maxPage)
    {
        var result = new SortedSet<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains('-'))
            {
                var bounds = part.Split('-');
                if (bounds.Length == 2 && int.TryParse(bounds[0], out var a) && int.TryParse(bounds[1], out var b))
                    for (int i = a; i <= b && i <= maxPage; i++) result.Add(i);
            }
            else if (int.TryParse(part, out var n) && n >= 1 && n <= maxPage)
                result.Add(n);
        }
        return result.Count > 0 ? (result.ToList(), null) : (null, "Rango inválido.");
    }

}
