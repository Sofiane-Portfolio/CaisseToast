using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class TerminalOrderViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string TotalLabel { get; init; } = "";
    public string Meta { get; init; } = "";
    public string Badge { get; init; } = "";
    public OrderStatus Status { get; init; }
    public bool IsSelected { get; set; }
}

public partial class TerminalLineViewModel
{
    public string Name { get; init; } = "";
    public string TotalLabel { get; init; } = "";
}

public partial class TerminalViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _activeTab = "open";
    [ObservableProperty] private string _openCount = "0";
    [ObservableProperty] private string _paidCount = "0";
    [ObservableProperty] private string _closedCount = "0";
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string _detailTitle = "";
    [ObservableProperty] private string _detailMeta = "";
    [ObservableProperty] private string _detailBanner = "";
    [ObservableProperty] private string _detailSubtotal = "";
    [ObservableProperty] private string _detailTax = "";
    [ObservableProperty] private string _detailTotal = "";
    [ObservableProperty] private bool _canPay;
    [ObservableProperty] private bool _canClose;
    [ObservableProperty] private bool _canRefund;
    [ObservableProperty] private bool _canReopen;

    public ObservableCollection<TerminalOrderViewModel> Orders { get; } = [];
    public ObservableCollection<TerminalLineViewModel> DetailLines { get; } = [];

    private PosOrder? _selected;

    public TerminalViewModel(IAuthService auth, IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
    }

    public void Refresh()
    {
        OpenCount = _pos.OpenOrders.Count.ToString();
        PaidCount = _pos.PaidOrders.Count.ToString();
        ClosedCount = _pos.ClosedOrders.Count.ToString();

        var source = ActiveTab switch
        {
            "paid" => _pos.PaidOrders,
            "closed" => _pos.ClosedOrders,
            _ => _pos.OpenOrders
        };

        Orders.Clear();
        foreach (var o in source)
        {
            Orders.Add(new TerminalOrderViewModel
            {
                Id = o.Id,
                Number = o.Number,
                DisplayName = o.DisplayName,
                TotalLabel = ActiveTab == "closed" ? "0,00 €" : $"{o.Total:N2} €",
                Meta = $"{o.TimeLabel} · {o.ItemCount} art.",
                Badge = ActiveTab == "open" ? "Ouverte" : ActiveTab == "paid" ? "Payée" : "Closed",
                Status = o.Status,
                IsSelected = _selected?.Id == o.Id
            });
        }

        if (_selected is not null && !source.Any(o => o.Id == _selected.Id))
            _selected = source.FirstOrDefault();

        if (_selected is null && source.Any())
            _selected = source.First();

        UpdateDetail();
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        ActiveTab = tab;
        _selected = null;
        Refresh();
    }

    [RelayCommand]
    private void SelectOrder(TerminalOrderViewModel? order)
    {
        if (order is null) return;
        _selected = ActiveTab switch
        {
            "paid" => _pos.PaidOrders.FirstOrDefault(o => o.Id == order.Id),
            "closed" => _pos.ClosedOrders.FirstOrDefault(o => o.Id == order.Id),
            _ => _pos.OpenOrders.FirstOrDefault(o => o.Id == order.Id)
        };
        Refresh();
    }

    [RelayCommand]
    private void UpdateOrder()
    {
        if (_selected is null) return;
        _launch.ResumeOrderId = _selected.Id;
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void PayOrder()
    {
        if (_selected is null || ActiveTab != "open") return;
        _launch.ResumeOrderId = _selected.Id;
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void CloseOrder()
    {
        if (_selected is null || ActiveTab != "paid") return;
        _pos.CloseOrder(_selected.Id);
        ActiveTab = "closed";
        _selected = null;
        Refresh();
    }

    [RelayCommand]
    private void RefundOrder()
    {
        if (_selected is null || ActiveTab != "paid") return;
        _pos.RefundOrder(_selected.Id);
        _selected = null;
        Refresh();
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);

    private void UpdateDetail()
    {
        HasSelection = _selected is not null;
        DetailLines.Clear();

        if (_selected is null)
        {
            DetailTitle = "";
            CanPay = CanClose = CanRefund = CanReopen = false;
            return;
        }

        DetailTitle = $"{_selected.Number} · {_selected.DisplayName}";
        DetailMeta = $"{_selected.Source} · {_selected.ServerName}";
        DetailBanner = ActiveTab switch
        {
            "open" => "Active",
            "paid" => $"Payé · {_selected.PaymentMethod}",
            _ => $"Closed · {_selected.ClosedAt?.ToString("HH:mm") ?? "—"}"
        };
        DetailSubtotal = $"{_selected.Subtotal:N2} €";
        DetailTax = $"{_selected.Tax:N2} €";
        DetailTotal = ActiveTab == "closed" ? "0,00 €" : $"{_selected.Total:N2} €";

        foreach (var line in _selected.Lines)
        {
            DetailLines.Add(new TerminalLineViewModel
            {
                Name = $"{line.Quantity}× {line.Name}",
                TotalLabel = $"{line.LineTotal:N2} €"
            });
        }

        CanPay = ActiveTab == "open";
        CanClose = ActiveTab == "paid";
        CanRefund = ActiveTab == "paid";
        CanReopen = false;
    }
}
