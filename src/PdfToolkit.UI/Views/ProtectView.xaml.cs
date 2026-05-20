using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PdfToolkit.UI.ViewModels;

namespace PdfToolkit.UI.Views;

public partial class ProtectView : UserControl
{
    public ProtectView()
    {
        InitializeComponent();
        UserPwBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is ProtectViewModel vm) vm.UserPassword = UserPwBox.Password;
        };
        OwnerPwBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is ProtectViewModel vm) vm.OwnerPassword = OwnerPwBox.Password;
        };
        RemovePwBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is ProtectViewModel vm) vm.RemovePassword = RemovePwBox.Password;
        };
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
        var pdf = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        if (pdf is null) return;
        if (DataContext is ProtectViewModel vm) vm.DropFile(pdf);
    }
}
