using System.Windows;
using PdfToolkit.UI.ViewModels;

namespace PdfToolkit.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
