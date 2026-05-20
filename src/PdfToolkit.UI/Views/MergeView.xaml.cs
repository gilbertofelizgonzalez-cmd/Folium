using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PdfToolkit.UI.Models;

namespace PdfToolkit.UI.Views;

public partial class MergeView : UserControl
{
    private Point _dragStartPoint;

    public MergeView()
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
        if (DataContext is ViewModels.MergeViewModel vm) vm.DropFiles(files);
    }

    private void FilesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void FilesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPoint = e.GetPosition(null);
        var diff = _dragStartPoint - currentPoint;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var listBox = sender as ListBox;
        var listBoxItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);

        if (listBoxItem is null) return;

        var item = listBox?.ItemContainerGenerator.ItemFromContainer(listBoxItem) as PdfFileItem;
        if (item is null) return;

        DragDrop.DoDragDrop(listBoxItem, item, DragDropEffects.Move);
    }

    private void FilesListBox_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(PdfFileItem))
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void FilesListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(PdfFileItem))) return;

        var draggedItem = (PdfFileItem)e.Data.GetData(typeof(PdfFileItem));
        if (draggedItem is null) return;

        var listBox = sender as ListBox;
        if (listBox?.DataContext is not ViewModels.MergeViewModel vm) return;

        var targetItemContainer = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);

        int newIndex;
        if (targetItemContainer is not null)
        {
            var targetItem = listBox.ItemContainerGenerator.ItemFromContainer(targetItemContainer) as PdfFileItem;
            if (targetItem is null || targetItem == draggedItem) return;
            newIndex = vm.SourceFiles.IndexOf(targetItem);
        }
        else
        {
            newIndex = vm.SourceFiles.Count - 1;
        }

        var oldIndex = vm.SourceFiles.IndexOf(draggedItem);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex) return;

        vm.SourceFiles.Move(oldIndex, newIndex);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
