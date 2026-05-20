using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Rotating;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class PageRotateItem : ObservableObject
{
    public int PageNumber { get; init; }  // 1-based display
    public int PageIndex { get; init; }   // 0-based internal

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _rotationDegrees; // 0, 90, 180, 270

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RotationArrow))]
    private int _previewRotation;

    [ObservableProperty] private BitmapImage? _thumbnail;

    public string RotationLabel => RotationDegrees switch
    {
        90  => "90° →",
        180 => "180°",
        270 => "← 270°",
        _   => "Sin rotación"
    };

    public string RotationArrow => PreviewRotation switch
    {
        90  => "↻",
        180 => "↕",
        270 => "↺",
        _   => ""
    };
}

public partial class RotateViewModel : BaseToolViewModel
{
    private readonly IRotator _rotator;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private readonly IThumbnailService _thumbnailService;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _thumbCts;

    public ObservableCollection<PageRotateItem> Pages { get; } = new();

    public RotateViewModel(IRotator rotator, IFileDialogService fileDialog, ISettingsService settings, IThumbnailService thumbnailService)
    {
        _rotator = rotator;
        _fileDialog = fileDialog;
        _settings = settings;
        _thumbnailService = thumbnailService;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRotate))]
    [NotifyCanExecuteChangedFor(nameof(RotateCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRotate))]
    [NotifyCanExecuteChangedFor(nameof(RotateCommand))]
    private bool _isProcessing;

    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para rotar páginas.";
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private int _selectedRotation = 90;

    // Rotation RadioButton bindings
    [ObservableProperty] private bool _rotation90 = true;
    [ObservableProperty] private bool _rotation180;
    [ObservableProperty] private bool _rotation270;

    partial void OnRotation90Changed(bool value) { if (value) { SelectedRotation = 90; _rotation180 = false; _rotation270 = false; } }
    partial void OnRotation180Changed(bool value) { if (value) { SelectedRotation = 180; _rotation90 = false; _rotation270 = false; } }
    partial void OnRotation270Changed(bool value) { if (value) { SelectedRotation = 270; _rotation90 = false; _rotation180 = false; } }

    partial void OnSelectedRotationChanged(int value) => UpdatePreviews();

    public bool CanRotate => !IsProcessing && SourceFile is not null && Pages.Any(p => p.IsSelected);
    public bool HasResult => LastOutputPath is not null;
    public bool HasPages => Pages.Count > 0;
    public bool HasNoPages => Pages.Count == 0;

    [RelayCommand]
    private void SelectSource()
    {
        _thumbCts?.Cancel();
        _thumbCts = null;

        var file = _fileDialog.OpenFile("Seleccionar PDF", "Archivos PDF|*.pdf");
        if (file is null) return;
        LoadFile(file);
    }

    public void DropFile(string path) => LoadFile(path);

    private void LoadFile(string file)
    {
        _thumbCts?.Cancel();
        _thumbCts = null;

        SourceFile = file;
        Pages.Clear();
        LastOutputPath = null;
        OnPropertyChanged(nameof(HasResult));

        var count = _rotator.GetPageCount(file);
        for (int i = 0; i < count; i++)
            Pages.Add(new PageRotateItem { PageNumber = i + 1, PageIndex = i });

        StatusMessage = $"{count} página(s) — selecciona las que quieres rotar.";
        OnPropertyChanged(nameof(HasPages));
        OnPropertyChanged(nameof(HasNoPages));
        RotateCommand.NotifyCanExecuteChanged();

        _ = LoadThumbnailsAsync(file);
    }

    private async Task LoadThumbnailsAsync(string sourcePath)
    {
        _thumbCts?.Cancel();
        _thumbCts = new CancellationTokenSource();
        var ct = _thumbCts.Token;
        foreach (var page in Pages.ToList())
        {
            if (ct.IsCancellationRequested) break;
            try { page.Thumbnail = await _thumbnailService.GenerateAsync(sourcePath, 400, page.PageIndex, ct); }
            catch { /* ignore */ }
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var p in Pages) p.IsSelected = true;
        RotateCommand.NotifyCanExecuteChanged();
        UpdatePreviews();
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var p in Pages) p.IsSelected = false;
        RotateCommand.NotifyCanExecuteChanged();
        UpdatePreviews();
    }

    [RelayCommand]
    private void PageSelectionChanged()
    {
        RotateCommand.NotifyCanExecuteChanged();
        UpdatePreviews();
    }

    private void UpdatePreviews()
    {
        foreach (var page in Pages)
            page.PreviewRotation = page.IsSelected ? SelectedRotation : 0;
    }

    [RelayCommand(CanExecute = nameof(CanRotate))]
    private async Task RotateAsync()
    {
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}_rotado.pdf");
        if (outputPath is null) return;

        var rotations = Pages
            .Where(p => p.IsSelected)
            .ToDictionary(p => p.PageIndex, _ => SelectedRotation);

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;

        try
        {
            var result = await _rotator.RotateAsync(
                new RotateRequest(SourceFile!, outputPath, rotations), _cts.Token);

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));
            ShowInfoBar("Rotación completada",
                $"{rotations.Count} página(s) rotadas {SelectedRotation}° — {result.PageCount} páginas en total.", "Success");
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
        return _fileDialog.SaveFile("Guardar PDF rotado", "Archivos PDF|*.pdf", defaultFileName);
    }

}
