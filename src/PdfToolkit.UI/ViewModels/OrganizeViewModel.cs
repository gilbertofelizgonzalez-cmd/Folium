using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Organizing;
using PdfToolkit.Core.Settings;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class OrganizePageItem : ObservableObject
{
    public int PageIndex { get; init; }            // original 0-based
    [ObservableProperty] private int _displayNumber; // 1-based, updated on reorder
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private BitmapImage? _thumbnail;
}

public partial class OrganizeViewModel : BaseToolViewModel
{
    private readonly IPageOrganizer _organizer;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private readonly IThumbnailService _thumbnailService;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _thumbCts;

    public ObservableCollection<OrganizePageItem> Pages { get; } = new();

    public OrganizeViewModel(IPageOrganizer organizer, IFileDialogService fileDialog, ISettingsService settings, IThumbnailService thumbnailService)
    {
        _organizer = organizer;
        _fileDialog = fileDialog;
        _settings = settings;
        _thumbnailService = thumbnailService;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOrganize))]
    [NotifyCanExecuteChangedFor(nameof(OrganizeCommand))]
    private string? _sourceFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOrganize))]
    [NotifyCanExecuteChangedFor(nameof(OrganizeCommand))]
    private bool _isProcessing;

    [ObservableProperty] private string _statusMessage = "Selecciona un PDF para reorganizar sus páginas.";
    [ObservableProperty] private string? _lastOutputPath;

    public bool CanOrganize => !IsProcessing && SourceFile is not null && Pages.Count > 0;
    public bool HasResult => LastOutputPath is not null;
    public bool HasPages => Pages.Count > 0;
    public bool HasNoPages => Pages.Count == 0;

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
        _thumbCts?.Cancel();
        _thumbCts = null;

        SourceFile = file;
        Pages.Clear();
        LastOutputPath = null;
        OnPropertyChanged(nameof(HasResult));

        var count = _organizer.GetPageCount(file);
        for (int i = 0; i < count; i++)
            Pages.Add(new OrganizePageItem { PageIndex = i, DisplayNumber = i + 1 });

        StatusMessage = $"{count} página(s) — reordena o elimina páginas.";
        OnPropertyChanged(nameof(HasPages));
        OnPropertyChanged(nameof(HasNoPages));
        OnPropertyChanged(nameof(CanOrganize));
        OrganizeCommand.NotifyCanExecuteChanged();

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
    private void MoveUp(OrganizePageItem? item)
    {
        if (item is null) return;
        var idx = Pages.IndexOf(item);
        if (idx <= 0) return;
        Pages.Move(idx, idx - 1);
        RenumberPages();
    }

    [RelayCommand]
    private void MoveDown(OrganizePageItem? item)
    {
        if (item is null) return;
        var idx = Pages.IndexOf(item);
        if (idx < 0 || idx >= Pages.Count - 1) return;
        Pages.Move(idx, idx + 1);
        RenumberPages();
    }

    [RelayCommand]
    private void DeletePage(OrganizePageItem? item)
    {
        if (item is null) return;
        Pages.Remove(item);
        RenumberPages();
        OnPropertyChanged(nameof(HasPages));
        OnPropertyChanged(nameof(HasNoPages));
        OnPropertyChanged(nameof(CanOrganize));
        OrganizeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var p in Pages) p.IsSelected = true;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        var toDelete = Pages.Where(p => p.IsSelected).ToList();
        foreach (var p in toDelete) Pages.Remove(p);
        RenumberPages();
        OnPropertyChanged(nameof(HasPages));
        OnPropertyChanged(nameof(HasNoPages));
        OnPropertyChanged(nameof(CanOrganize));
        OrganizeCommand.NotifyCanExecuteChanged();
    }

    private void RenumberPages()
    {
        for (int i = 0; i < Pages.Count; i++)
            Pages[i].DisplayNumber = i + 1;
    }

    [RelayCommand(CanExecute = nameof(CanOrganize))]
    private async Task OrganizeAsync()
    {
        var outputPath = ResolveOutputPath($"{Path.GetFileNameWithoutExtension(SourceFile!)}_reorganizado.pdf");
        if (outputPath is null) return;

        var pageOrder = Pages.Select(p => p.PageIndex).ToList();

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;

        try
        {
            var result = await _organizer.OrganizeAsync(
                new OrganizeRequest(SourceFile!, outputPath, pageOrder), _cts.Token);

            LastOutputPath = result.OutputPath;
            OnPropertyChanged(nameof(HasResult));
            ShowInfoBar("PDF reorganizado",
                $"PDF guardado con {result.PageCount} página(s).", "Success");
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
        return _fileDialog.SaveFile("Guardar PDF reorganizado", "Archivos PDF|*.pdf", defaultFileName);
    }
}
