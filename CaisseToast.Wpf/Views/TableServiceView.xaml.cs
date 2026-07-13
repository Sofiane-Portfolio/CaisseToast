using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CaisseToast.Wpf.ViewModels;

namespace CaisseToast.Wpf.Views;

public partial class TableServiceView : UserControl
{
    private TableSeatViewModel? _dragSeat;
    private Point _dragStart;
    private bool _isDragging;

    public TableServiceView() => InitializeComponent();

    private TableServiceViewModel? Vm => DataContext as TableServiceViewModel;

    private void Seat_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm is null || !Vm.IsEditMode) return;
        if (sender is not FrameworkElement el) return;
        _dragSeat = el.DataContext as TableSeatViewModel;
        if (_dragSeat is null) return;
        _dragStart = e.GetPosition(SalleCanvas);
        _isDragging = true;
        el.CaptureMouse();
        e.Handled = true;
    }

    private void Seat_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _dragSeat is null || Vm is null) return;
        var pos = e.GetPosition(SalleCanvas);
        Vm.MoveTable(_dragSeat, pos.X - _dragStart.X, pos.Y - _dragStart.Y);
        _dragStart = pos;
    }

    private void Seat_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        _dragSeat = null;
        if (sender is UIElement el) el.ReleaseMouseCapture();
    }
}
