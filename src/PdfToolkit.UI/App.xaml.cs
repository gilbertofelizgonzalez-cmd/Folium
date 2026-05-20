using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using PdfToolkit.Core.Compressing;
using PdfToolkit.Core.Converting;
using PdfToolkit.Core.Decrypting;
using PdfToolkit.Core.Exporting;
using PdfToolkit.Core.Merging;
using PdfToolkit.Core.BatchProtecting;
using PdfToolkit.Core.Organizing;
using PdfToolkit.Core.Rotating;
using PdfToolkit.Core.Settings;
using PdfToolkit.Core.Splitting;
using PdfToolkit.Core.Watermarking;
using PdfToolkit.Services.Compressing;
using PdfToolkit.Services.Converting;
using PdfToolkit.Services.Decrypting;
using PdfToolkit.Services.Exporting;
using PdfToolkit.Services.Merging;
using PdfToolkit.Services.BatchProtecting;
using PdfToolkit.Services.Organizing;
using PdfToolkit.Services.Rotating;
using PdfToolkit.Services.Settings;
using PdfToolkit.Services.Splitting;
using PdfToolkit.Services.Watermarking;
using PdfToolkit.UI.Services;
using PdfToolkit.UI.ViewModels;
using PdfConverter = PdfToolkit.Services.Converting.PdfConverter;

namespace PdfToolkit.UI;

public partial class App : Application
{
    private IServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Aplicar tema guardado antes de mostrar la ventana
        var settings = _services.GetRequiredService<ISettingsService>();
        ApplyTheme(settings.Current.Theme);

        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    private static void ApplyTheme(string theme)
    {
        ThemeManager.Current.ApplicationTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark"  => ApplicationTheme.Dark,
            _       => null   // System
        };
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Settings
        services.AddSingleton<ISettingsService, SettingsService>();

        // Core services
        services.AddSingleton<IPdfMerger, PdfMerger>();
        services.AddSingleton<ISplitter, PdfSplitter>();

        // Document converters (LibreOffice con auto-detección)
        services.AddSingleton<SyncfusionDocumentConverter>();
        services.AddSingleton<LibreOfficeConverter>();
        services.AddSingleton<CompositeDocumentConverter>();
        services.AddSingleton<IDocumentConverter>(sp =>
            sp.GetRequiredService<CompositeDocumentConverter>());
        services.AddSingleton<IConverter>(sp =>
            new PdfConverter(sp.GetRequiredService<IDocumentConverter>()));

        // UI services
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IThumbnailService, PdfThumbnailService>();

        // New feature services
        services.AddSingleton<IBatchProtector, BatchProtector>();
        services.AddSingleton<ICompressor, PdfCompressor>();
        services.AddSingleton<IRotator, PdfRotator>();
        services.AddSingleton<IPageExporter, PdfPageExporter>();
        services.AddSingleton<IPageOrganizer, PdfPageOrganizer>();
        services.AddSingleton<IWatermarker, PdfWatermarker>();
        services.AddSingleton<IDecryptor, PdfDecryptor>();

        // ViewModels
        services.AddSingleton<MergeViewModel>();
        services.AddSingleton<SplitViewModel>();
        services.AddSingleton<ConvertViewModel>();
        services.AddSingleton<CompressViewModel>();
        services.AddSingleton<RotateViewModel>();
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<BatchProtectViewModel>();
        services.AddSingleton<OrganizeViewModel>();
        services.AddSingleton<WatermarkViewModel>();
        services.AddSingleton<DecryptViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Window
        services.AddTransient<MainWindow>();
    }
}
