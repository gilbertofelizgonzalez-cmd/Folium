using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Settings;
using PdfToolkit.Core.Splitting;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class SplitViewModel : BaseToolViewModel
{
    private readonly ISplitter _splitter;
    private readonly IFileDialogService _dialogService;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    public SplitViewModel(ISplitter splitter, IFileDialogService dialogService, ISettingsService settings)
    {
        _splitter = splitter;
        _dialogService = dialogService;
        _settings = settings;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string? _outputDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectSourceCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectOutputCommand))]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelSplitCommand))]
    private bool _isProcessing;

    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string? _currentOutputFile;
    [ObservableProperty] private string _statusMessage = "Seleccioná un PDF y una carpeta de salida.";

    // --- Selección de páginas ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageCountLabel))]
    [NotifyPropertyChangedFor(nameof(HasPageCount))]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private int _totalPageCount;

    public bool HasPageCount => TotalPageCount > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomRangeEnabled))]
    [NotifyPropertyChangedFor(nameof(SplitSpecificPages))]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private bool _splitAllPages = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RangeValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private string _pageRangeText = string.Empty;

    public bool IsCustomRangeEnabled => !SplitAllPages;

    public bool SplitSpecificPages
    {
        get => !SplitAllPages;
        set => SplitAllPages = !value;
    }

    public string PageCountLabel => TotalPageCount > 0
        ? $"Todas las páginas ({TotalPageCount} páginas)"
        : "Todas las páginas";

    public string RangeValidationMessage
    {
        get
        {
            if (SplitAllPages || string.IsNullOrWhiteSpace(PageRangeText)) return string.Empty;
            var (pages, error) = ParsePageRange(PageRangeText, TotalPageCount);
            if (error is not null) return $"⚠ {error}";
            return $"✔ {pages!.Count} página(s) seleccionada(s)";
        }
    }

    partial void OnSourceFileChanged(string? value)
    {
        TotalPageCount = 0;
        PageRangeText = string.Empty;
        SplitAllPages = true;

        if (!string.IsNullOrWhiteSpace(value))
        {
            try { TotalPageCount = _splitter.GetPageCount(value); }
            catch { TotalPageCount = 0; }
        }

        UpdateStatusMessage();
    }

    partial void OnOutputDirectoryChanged(string? value) => UpdateStatusMessage();

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void SelectSource()
    {
        var path = _dialogService.OpenFile(
            title: "Seleccionar PDF para dividir",
            filter: "Archivos PDF (*.pdf)|*.pdf");

        if (path is null) return;
        LoadFile(path);
    }

    public void DropFile(string path) => LoadFile(path);

    private void LoadFile(string path) => SourceFile = path;

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void SelectOutput()
    {
        var s = _settings.Current;
        if (s.OutputMode == "Fixed" && Directory.Exists(s.OutputPath))
        {
            OutputDirectory = s.OutputPath;
            return;
        }
        var dir = _dialogService.OpenFolder("Carpeta de salida para los PDFs divididos");
        if (dir is null) return;
        OutputDirectory = dir;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelSplit() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private async Task SplitAsync()
    {
        IReadOnlyList<int>? pageNumbers = null;

        if (!SplitAllPages)
        {
            var (pages, error) = ParsePageRange(PageRangeText, TotalPageCount);
            if (error is not null)
            {
                ShowInfoBar("Rango inválido", error, "Error");
                return;
            }
            pageNumbers = pages;
        }

        IsInfoBarOpen = false;
        IsProcessing = true;
        ProgressPercentage = 0;
        StatusMessage = "Dividiendo PDF...";
        _cts = new CancellationTokenSource();

        try
        {
            var request = new SplitRequest(SourceFile!, OutputDirectory!, pageNumbers);

            var progress = new Progress<SplitProgress>(p =>
            {
                ProgressPercentage = p.Percentage;
                CurrentOutputFile = p.CurrentOutputFile;
            });

            var result = await _splitter.SplitAsync(request, progress, _cts.Token);

            ShowInfoBar(
                title: "PDF dividido correctamente",
                message: $"{result.OutputFiles.Count} archivos generados en {OutputDirectory}",
                severity: "Success");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar(
                title: "Operación cancelada",
                message: "La división fue interrumpida por el usuario.",
                severity: "Info");
        }
        catch (PdfSplitException ex)
        {
            ShowInfoBar("Error al dividir", ex.Message, "Error");
        }
        catch (Exception ex)
        {
            ShowInfoBar("Error inesperado", ex.Message, "Error");
        }
        finally
        {
            IsProcessing = false;
            CurrentOutputFile = null;
            _cts?.Dispose();
            _cts = null;
            UpdateStatusMessage();
        }
    }

    // Parsea "1, 3-5, 7, 10-15" en lista de páginas validadas
    private static (List<int>? Pages, string? Error) ParsePageRange(string text, int maxPage)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, "Ingresá al menos una página o rango.");

        var pages = new SortedSet<int>();

        foreach (var segment in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var part = segment.Trim();
            if (part.Contains('-'))
            {
                var sides = part.Split('-', 2);
                if (!int.TryParse(sides[0].Trim(), out int from) ||
                    !int.TryParse(sides[1].Trim(), out int to))
                    return (null, $"Rango inválido: '{part}'.");

                if (from < 1 || to < from)
                    return (null, $"Rango inválido: '{part}'. El inicio debe ser ≥ 1 y menor que el fin.");

                if (maxPage > 0 && to > maxPage)
                    return (null, $"La página {to} supera el total ({maxPage}).");

                for (int p = from; p <= to; p++) pages.Add(p);
            }
            else
            {
                if (!int.TryParse(part, out int page))
                    return (null, $"Valor inválido: '{part}'.");

                if (page < 1)
                    return (null, $"El número de página debe ser ≥ 1.");

                if (maxPage > 0 && page > maxPage)
                    return (null, $"La página {page} supera el total ({maxPage}).");

                pages.Add(page);
            }
        }

        return pages.Count == 0
            ? (null, "No se encontraron páginas válidas.")
            : (pages.ToList(), null);
    }

    private bool CanInteract() => !IsProcessing;

    private bool CanSplit()
    {
        if (string.IsNullOrWhiteSpace(SourceFile) ||
            string.IsNullOrWhiteSpace(OutputDirectory) ||
            IsProcessing) return false;

        if (!SplitAllPages)
        {
            var (pages, _) = ParsePageRange(PageRangeText, TotalPageCount);
            return pages is { Count: > 0 };
        }

        return true;
    }

    private bool CanCancel() => IsProcessing;

    private void UpdateStatusMessage()
    {
        if (IsProcessing) return;

        StatusMessage = (string.IsNullOrEmpty(SourceFile), string.IsNullOrEmpty(OutputDirectory)) switch
        {
            (true, true)   => "Seleccioná un PDF y una carpeta de salida.",
            (false, true)  => "Falta seleccionar la carpeta de salida.",
            (true, false)  => "Falta seleccionar el PDF a dividir.",
            _              => "Listo para dividir."
        };
    }
}
