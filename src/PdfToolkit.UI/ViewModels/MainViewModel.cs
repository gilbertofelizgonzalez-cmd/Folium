using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdfToolkit.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MergeViewModel         Merge         { get; }
    public SplitViewModel         Split         { get; }
    public ConvertViewModel       Convert       { get; }
    public CompressViewModel      Compress      { get; }
    public RotateViewModel        Rotate        { get; }
    public ExportViewModel        Export        { get; }
    public BatchProtectViewModel  BatchProtect  { get; }
    public OrganizeViewModel      Organize      { get; }
    public WatermarkViewModel     Watermark     { get; }
    public DecryptViewModel       Decrypt       { get; }
    public SettingsViewModel      Settings      { get; }

    public MainViewModel(
        MergeViewModel        mergeViewModel,
        SplitViewModel        splitViewModel,
        ConvertViewModel      convertViewModel,
        CompressViewModel     compressViewModel,
        RotateViewModel       rotateViewModel,
        ExportViewModel       exportViewModel,
        BatchProtectViewModel batchProtectViewModel,
        OrganizeViewModel     organizeViewModel,
        WatermarkViewModel    watermarkViewModel,
        DecryptViewModel      decryptViewModel,
        SettingsViewModel     settingsViewModel)
    {
        Merge        = mergeViewModel;
        Split        = splitViewModel;
        Convert      = convertViewModel;
        Compress     = compressViewModel;
        Rotate       = rotateViewModel;
        Export       = exportViewModel;
        BatchProtect = batchProtectViewModel;
        Organize     = organizeViewModel;
        Watermark    = watermarkViewModel;
        Decrypt      = decryptViewModel;
        Settings     = settingsViewModel;
        _currentViewModel = Merge;
        _activePage = "Merge";
    }

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _title = "Folium";

    [ObservableProperty]
    private string _activePage;

    [RelayCommand]
    private void NavigateMerge()    { CurrentViewModel = Merge;    ActivePage = "Merge"; }

    [RelayCommand]
    private void NavigateSplit()    { CurrentViewModel = Split;    ActivePage = "Split"; }

    [RelayCommand]
    private void NavigateConvert()  { CurrentViewModel = Convert;  ActivePage = "Convert"; }

    [RelayCommand]
    private void NavigateCompress() { CurrentViewModel = Compress; ActivePage = "Compress"; }

    [RelayCommand]
    private void NavigateRotate()   { CurrentViewModel = Rotate;   ActivePage = "Rotate"; }

    [RelayCommand]
    private void NavigateExport()   { CurrentViewModel = Export;   ActivePage = "Export"; }

    [RelayCommand]
    private void NavigateBatchProtect()  { CurrentViewModel = BatchProtect; ActivePage = "BatchProtect"; }

    [RelayCommand]
    private void NavigateOrganize()  { CurrentViewModel = Organize;  ActivePage = "Organize"; }

    [RelayCommand]
    private void NavigateWatermark() { CurrentViewModel = Watermark; ActivePage = "Watermark"; }

    [RelayCommand]
    private void NavigateDecrypt()   { CurrentViewModel = Decrypt;   ActivePage = "Decrypt"; }

    [RelayCommand]
    private void NavigateSettings() { CurrentViewModel = Settings; ActivePage = "Settings"; }
}
