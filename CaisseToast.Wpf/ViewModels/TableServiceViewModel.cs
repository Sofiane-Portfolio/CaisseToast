using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class TableSeatViewModel : ObservableObject
{
    public int Number { get; init; }
    public int Seats { get; init; }
    public string Zone { get; init; } = "";
    public bool IsVip { get; init; }
    public bool IsBar { get; init; }

    [ObservableProperty] private TableStatus _status;
    [ObservableProperty] private string? _guestName;
    [ObservableProperty] private decimal _orderTotal;
    [ObservableProperty] private double _posX;
    [ObservableProperty] private double _posY;

    public string StatusLabel => TableStatusLabels.Get(Status);
    public string DisplayAmount => OrderTotal > 0 ? $"{OrderTotal:N2} €" : "";
    public bool IsBlocked => Status is TableStatus.Dirty or TableStatus.Unavailable;

    partial void OnStatusChanged(TableStatus value)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(IsBlocked));
    }

    partial void OnOrderTotalChanged(decimal value) => OnPropertyChanged(nameof(DisplayAmount));
}

public partial class TableServiceViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;

    [ObservableProperty] private bool _isServerMode;
    [ObservableProperty] private bool _showServerStrip;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _useCanvasLayout;
    [ObservableProperty] private string _title = "Table Service";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _serverName = "";
    [ObservableProperty] private string _shiftInfo = "";
    [ObservableProperty] private string _clockButtonLabel = "Clock In";
    [ObservableProperty] private string _shiftPill = "Hors service";
    [ObservableProperty] private bool _isClockedIn;
    [ObservableProperty] private string _editModeLabel = "Positionner tables";

    [ObservableProperty] private string _statFree = "";
    [ObservableProperty] private string _statOccupied = "";
    [ObservableProperty] private string _statBill = "";
    [ObservableProperty] private string _statPaying = "";
    [ObservableProperty] private string _statReserved = "";
    [ObservableProperty] private string _statDirty = "";
    [ObservableProperty] private string _statUnavailable = "";

    public ObservableCollection<TableSeatViewModel> SalleTables { get; } = [];
    public ObservableCollection<TableSeatViewModel> TerrasseTables { get; } = [];
    public ObservableCollection<TableSeatViewModel> BarTables { get; } = [];
    public ObservableCollection<TableSeatViewModel> VipTables { get; } = [];

    public bool CanManageTables => _auth.CurrentEmployee?.Role is UserRole.Manager or UserRole.Admin;
    public bool ShowResetGrid => CanManageTables && _pos.TablePositionsCustom;

    public TableServiceViewModel(IAuthService auth, IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
        BuildSeats();
        Refresh();
    }

    public void Refresh()
    {
        IsServerMode = _auth.CurrentEmployee?.Role == UserRole.Server;
        ShowServerStrip = IsServerMode;
        ServerName = _auth.CurrentEmployee?.Name ?? "Serveur";
        Title = IsServerMode ? "Plan de salle" : "Table Service";
        Subtitle = IsServerMode
            ? $"Touchez une table pour commander · {ServerName}"
            : "Sélectionnez une table ou lancez une commande rapide";

        IsClockedIn = _pos.IsClockedIn;
        ClockButtonLabel = IsClockedIn ? "Clock Out" : "Clock In";
        ShiftPill = IsClockedIn ? "En service" : "Hors service";
        ShiftInfo = IsClockedIn
            ? $"Shift · {_pos.FormatShiftDuration()}"
            : "Connecté — démarrez votre shift";

        UseCanvasLayout = _pos.TablePositionsCustom || IsEditMode;
        EditModeLabel = IsEditMode ? "Terminer positionnement" : "Positionner tables";

        foreach (var seat in AllSeats())
        {
            var st = _pos.GetTableState(seat.Number);
            seat.Status = st.Status;
            seat.GuestName = st.GuestName;
            seat.OrderTotal = st.OrderTotal;
            var pos = _pos.GetTablePosition(seat.Number);
            seat.PosX = pos.X;
            seat.PosY = pos.Y;
        }

        var c = _pos.GetTableCounts();
        StatFree = $"{c.free} libre{(c.free > 1 ? "s" : "")}";
        StatOccupied = $"{c.occupied} occupée{(c.occupied > 1 ? "s" : "")}";
        StatBill = $"{c.bill} addition";
        StatPaying = $"{c.paying} en paiement";
        StatReserved = $"{c.reserved} réservée{(c.reserved > 1 ? "s" : "")}";
        StatDirty = $"{c.dirty} sale{(c.dirty > 1 ? "s" : "")}";
        StatUnavailable = $"{c.unavailable} indisponible{(c.unavailable > 1 ? "s" : "")}";
        OnPropertyChanged(nameof(CanManageTables));
        OnPropertyChanged(nameof(ShowResetGrid));
    }

    public void MoveTable(TableSeatViewModel seat, double deltaX, double deltaY)
    {
        if (!IsEditMode) return;
        _pos.SetTablePosition(seat.Number, seat.PosX + deltaX, seat.PosY + deltaY);
        seat.PosX += deltaX;
        seat.PosY += deltaY;
    }

    [RelayCommand]
    private void OpenTable(TableSeatViewModel? seat)
    {
        if (seat is null || seat.IsBlocked || IsEditMode) return;
        if (seat.Status == TableStatus.Free)
            _pos.SetTableStatus(seat.Number, TableStatus.Occupied);

        _launch.TableNumber = seat.Number;
        _launch.TabName = seat.GuestName ?? "";
        _launch.ResumeOrderId = _pos.FindOpenOrderForTable(seat.Number)?.Id;
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void CycleStatus(TableSeatViewModel? seat)
    {
        if (seat is null || !CanManageTables) return;
        _pos.CycleTableStatus(seat.Number);
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        if (!CanManageTables) return;
        IsEditMode = !IsEditMode;
        if (!IsEditMode && _pos.TablePositionsCustom)
            Refresh();
        else
        {
            UseCanvasLayout = _pos.TablePositionsCustom || IsEditMode;
            EditModeLabel = IsEditMode ? "Terminer positionnement" : "Positionner tables";
        }
    }

    [RelayCommand]
    private void ResetGrid()
    {
        if (!CanManageTables) return;
        _pos.ResetTablePositions();
        IsEditMode = false;
        Refresh();
    }

    [RelayCommand]
    private void QuickOrder()
    {
        _launch.Clear();
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void ToggleClock()
    {
        if (_pos.IsClockedIn) _pos.ClockOut();
        else _pos.ClockIn();
        Refresh();
    }

    [RelayCommand]
    private void OpenOrders() => _navigation.NavigateTo(AppScreen.ServerOrders);

    [RelayCommand]
    private void OpenReport() => _navigation.NavigateTo(AppScreen.ServerReport);

    [RelayCommand]
    private void GoBack()
    {
        if (IsServerMode) OpenOrders();
        else _navigation.NavigateTo(AppScreen.Home);
    }

    private void BuildSeats()
    {
        SalleTables.Clear();
        TerrasseTables.Clear();
        BarTables.Clear();
        VipTables.Clear();

        foreach (var t in _pos.Tables)
        {
            var pos = _pos.GetTablePosition(t.Number);
            var vm = new TableSeatViewModel
            {
                Number = t.Number,
                Seats = t.Seats,
                Zone = t.Zone,
                IsVip = t.IsVip,
                IsBar = t.IsBar,
                Status = _pos.GetTableState(t.Number).Status,
                PosX = pos.X,
                PosY = pos.Y
            };
            switch (t.Zone)
            {
                case "terrasse": TerrasseTables.Add(vm); break;
                case "bar": BarTables.Add(vm); break;
                case "vip": VipTables.Add(vm); break;
                default: SalleTables.Add(vm); break;
            }
        }
    }

    private IEnumerable<TableSeatViewModel> AllSeats()
        => SalleTables.Concat(TerrasseTables).Concat(BarTables).Concat(VipTables);
}
