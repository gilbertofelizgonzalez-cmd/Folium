using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfToolkit.Core.Converting;
using PdfToolkit.Core.Settings;
using PdfToolkit.Services.Converting;
using PdfToolkit.UI.Models;
using PdfToolkit.UI.Services;

namespace PdfToolkit.UI.ViewModels;

public partial class ConvertViewModel : BaseToolViewModel
{
    private readonly IConverter _converter;
    private readonly IDocumentConverter _documentConverter;
    private readonly IFileDialogService _fileDialog;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _cts;

    private static string BuildFileFilter()
    {
        var imgExts = string.Join(";", PdfConverter.ImageExtensions.Select(e => $"*{e}"));
        var docExts = string.Join(";", CompositeDocumentConverter.AllDocumentExtensions.Select(e => $"*{e}"));
        return $"Todos los formatos admitidos|{imgExts};{docExts}|" +
               $"Imágenes|{imgExts}|" +
               $"Documentos (Word/Excel/PPT)|{docExts}";
    }

    public ObservableCollection<PdfFileItem> SourceFiles { get; } = new();

    public string ConverterStatusMessage { get; }
    public bool HasDocumentConverter { get; }
    public bool IsLibreOfficeAvailable { get; }

    public ConvertViewModel(IConverter converter, IDocumentConverter documentConverter, IFileDialogService fileDialog, ISettingsService settings)
    {
        _converter = converter;
        _documentConverter = documentConverter;
        _fileDialog = fileDialog;
        _settings = settings;

        HasDocumentConverter = documentConverter.IsAvailable;
        IsLibreOfficeAvailable = DocumentConverterDetector.IsLibreOfficeAvailable();
        ConverterStatusMessage = documentConverter.IsAvailable
            ? $"Documentos: {documentConverter.ConverterName} activo"
            : "Documentos: instala LibreOffice para convertir Word/Excel/PPT";

        if (!IsLibreOfficeAvailable)
        {
            ShowInfoBar(
                "LibreOffice no detectado",
                "Instala LibreOffice para convertir Word, Excel y PowerPoint a PDF. La conversión de imágenes sigue disponible.",
                "Warning");
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    [NotifyCanExecuteChangedFor(nameof(ConvertToPdfCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = "Agrega archivos para convertir a PDF.";

    [ObservableProperty]
    private string _currentFileName = string.Empty;

    [ObservableProperty]
    private double _progressPercentage;

    public bool CanConvert => !IsProcessing && SourceFiles.Count > 0;

    private static readonly HashSet<string> _supportedExtensions = new(
        PdfConverter.ImageExtensions.Concat(CompositeDocumentConverter.AllDocumentExtensions),
        StringComparer.OrdinalIgnoreCase);

    public void DropFiles(string[] paths)
    {
        foreach (var f in paths)
        {
            var ext = Path.GetExtension(f);
            if (!_supportedExtensions.Contains(ext)) continue;
            if (!SourceFiles.Any(x => x.FilePath == f))
                SourceFiles.Add(new PdfFileItem(f));
        }
        UpdateStatus();
    }

    [RelayCommand]
    private void SelectFiles()
    {
        var files = _fileDialog.OpenMultipleFiles("Seleccionar archivos", BuildFileFilter());
        if (files is null) return;

        foreach (var f in files)
            if (!SourceFiles.Any(x => x.FilePath == f))
                SourceFiles.Add(new PdfFileItem(f));

        UpdateStatus();
    }

    [RelayCommand]
    private void Remove(PdfFileItem item)
    {
        SourceFiles.Remove(item);
        UpdateStatus();
    }

    [RelayCommand]
    private void MoveUp(PdfFileItem item)
    {
        var idx = SourceFiles.IndexOf(item);
        if (idx > 0) SourceFiles.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown(PdfFileItem item)
    {
        var idx = SourceFiles.IndexOf(item);
        if (idx >= 0 && idx < SourceFiles.Count - 1) SourceFiles.Move(idx, idx + 1);
    }

    [RelayCommand]
    private void ClearAll()
    {
        SourceFiles.Clear();
        UpdateStatus();
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertToPdfAsync()
    {
        var outputPath = ResolveOutputPath("Archivos_convertidos.pdf");
        if (outputPath is null) return;

        _cts = new CancellationTokenSource();
        IsProcessing = true;
        IsInfoBarOpen = false;
        OnPropertyChanged(nameof(CanConvert));

        try
        {
            var request = new ConvertRequest(
                SourceFiles.Select(f => f.FilePath).ToList(),
                outputPath);

            var progress = new Progress<ConvertProgress>(p =>
            {
                CurrentFileName = p.CurrentFileName;
                ProgressPercentage = p.TotalFiles > 0
                    ? (double)p.ProcessedFiles / p.TotalFiles * 100
                    : 0;
            });

            var result = await _converter.ConvertAsync(request, progress, _cts.Token);

            ShowInfoBar("Conversión completada",
                $"PDF generado con {result.PageCount} página(s) — {result.FileSizeBytes / 1024.0:F1} KB",
                "Success");
        }
        catch (OperationCanceledException)
        {
            ShowInfoBar("Cancelado", "La conversión fue cancelada.", "Warning");
        }
        catch (Exception ex)
        {
            ShowInfoBar("Error", ex.Message, "Error");
        }
        finally
        {
            IsProcessing = false;
            ProgressPercentage = 0;
            CurrentFileName = string.Empty;
            OnPropertyChanged(nameof(CanConvert));
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelConvert() => _cts?.Cancel();

    private void UpdateStatus()
    {
        StatusMessage = SourceFiles.Count == 0
            ? "Agrega archivos para convertir a PDF."
            : $"{SourceFiles.Count} archivo(s) listo(s) para convertir.";
        OnPropertyChanged(nameof(CanConvert));
        ConvertToPdfCommand.NotifyCanExecuteChanged();
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
        return _fileDialog.SaveFile("Guardar PDF convertido", "Archivos PDF|*.pdf", defaultFileName);
    }
}
