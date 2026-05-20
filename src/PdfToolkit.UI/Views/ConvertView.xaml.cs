using System.Windows;
using System.Windows.Controls;
using PdfToolkit.UI.ViewModels;

namespace PdfToolkit.UI.Views;

public partial class ConvertView : UserControl
{
    public ConvertView()
    {
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (DataContext is ConvertViewModel vm) vm.DropFiles(files);
    }
}
