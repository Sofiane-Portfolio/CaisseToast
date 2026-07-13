using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class ModifierOptionViewModel : ObservableObject
{
    public string Label { get; init; } = "";

    [ObservableProperty] private bool _isSelected;
}

public partial class OnlineOrderViewModel
{
    public int Id { get; init; }
    public string Customer { get; init; } = "";
    public string Channel { get; init; } = "";
    public string TotalLabel { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string Time { get; init; } = "";
    public string ItemsSummary { get; init; } = "";
    public string Status { get; init; } = "";
    public bool CanAccept => Status == "pending";
    public bool CanMarkReady => Status == "accepted";
    public bool CanReject => Status == "pending";
}

public partial class OnlineOrderingViewModel : ObservableObject
{
    private readonly IPosStateService _pos;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _activeTab = "pending";
    [ObservableProperty] private string _pendingCount = "0";
    [ObservableProperty] private string _acceptedCount = "0";
    [ObservableProperty] private string _readyCount = "0";
    [ObservableProperty] private string _todayCount = "0";

    public ObservableCollection<OnlineOrderViewModel> Orders { get; } = [];

    public OnlineOrderingViewModel(IPosStateService pos, INavigationService navigation)
    {
        _pos = pos;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
    }

    public void Refresh()
    {
        PendingCount = _pos.OnlineOrders.Count(o => o.Status == "pending").ToString();
        AcceptedCount = _pos.OnlineOrders.Count(o => o.Status == "accepted").ToString();
        ReadyCount = _pos.OnlineOrders.Count(o => o.Status == "ready").ToString();
        TodayCount = _pos.OnlineOrders.Count.ToString();

        var source = ActiveTab switch
        {
            "accepted" => _pos.OnlineOrders.Where(o => o.Status == "accepted"),
            "ready" => _pos.OnlineOrders.Where(o => o.Status == "ready"),
            "all" => _pos.OnlineOrders,
            _ => _pos.OnlineOrders.Where(o => o.Status == "pending")
        };

        Orders.Clear();
        foreach (var o in source)
        {
            Orders.Add(new OnlineOrderViewModel
            {
                Id = o.Id,
                Customer = o.Customer,
                Channel = o.Channel,
                TotalLabel = $"{o.Total:N2} €",
                StatusLabel = o.Status switch
                {
                    "accepted" => "En cuisine",
                    "ready" => "Prête",
                    _ => "À traiter"
                },
                Time = o.Time,
                ItemsSummary = string.Join(", ", o.Items.Select(i => $"{i.Quantity}× {i.Name}")),
                Status = o.Status
            });
        }
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        ActiveTab = tab;
        Refresh();
    }

    [RelayCommand]
    private void AcceptOrder(OnlineOrderViewModel? order)
    {
        if (order is null) return;
        _pos.AcceptOnlineOrder(order.Id);
        Refresh();
    }

    [RelayCommand]
    private void MarkReady(OnlineOrderViewModel? order)
    {
        if (order is null) return;
        _pos.MarkOnlineReady(order.Id);
        Refresh();
    }

    [RelayCommand]
    private void RejectOrder(OnlineOrderViewModel? order)
    {
        if (order is null) return;
        _pos.RejectOnlineOrder(order.Id);
        Refresh();
    }

    [RelayCommand]
    private void SimulateNewOrder()
    {
        _pos.SeedOnlineDemo();
        ActiveTab = "pending";
        Refresh();
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);
}
