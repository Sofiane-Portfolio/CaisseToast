using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class HubOrderViewModel
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public string Title { get; init; } = "";
    public string Meta { get; init; } = "";
    public string TypeLabel { get; init; } = "";
    public string TotalLabel { get; init; } = "";
    public string ActionLabel { get; init; } = "";
    public string Channel { get; init; } = "open";
}

public partial class OrdersHubViewModel : ObservableObject
{
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _activeChannel = "all";
    [ObservableProperty] private string _bannerCount = "0 tickets";

    public ObservableCollection<HubOrderViewModel> Orders { get; } = [];

    public OrdersHubViewModel(IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        Orders.Clear();
        var items = new List<HubOrderViewModel>();

        if (ActiveChannel is "all" or "open")
        {
            foreach (var o in _pos.OpenOrders)
            {
                items.Add(new HubOrderViewModel
                {
                    Id = o.Id,
                    Number = o.Number,
                    Title = o.DisplayName,
                    Meta = $"{o.TimeLabel} · {o.ItemCount} art.",
                    TypeLabel = "Ouverte",
                    TotalLabel = $"{o.Total:N2} €",
                    ActionLabel = "Reprendre",
                    Channel = "open"
                });
            }
        }

        if (ActiveChannel is "all" or "paid")
        {
            foreach (var o in _pos.PaidOrders)
            {
                items.Add(new HubOrderViewModel
                {
                    Id = o.Id,
                    Number = o.Number,
                    Title = o.DisplayName,
                    Meta = $"{o.TimeLabel} · Payée",
                    TypeLabel = "Payée",
                    TotalLabel = $"{o.Total:N2} €",
                    ActionLabel = "Voir",
                    Channel = "paid"
                });
            }
        }

        if (ActiveChannel is "all" or "online")
        {
            foreach (var o in _pos.OnlineOrders.Where(x => x.Status == "pending"))
            {
                items.Add(new HubOrderViewModel
                {
                    Id = o.Id,
                    Number = $"#{o.Id}",
                    Title = o.Customer,
                    Meta = $"{o.Channel} · {o.Time}",
                    TypeLabel = "Online",
                    TotalLabel = $"{o.Total:N2} €",
                    ActionLabel = "Accepter",
                    Channel = "online"
                });
            }
        }

        foreach (var item in items)
            Orders.Add(item);

        BannerCount = $"{items.Count} ticket{(items.Count != 1 ? "s" : "")}";
    }

    [RelayCommand]
    private void SelectChannel(string channel)
    {
        ActiveChannel = channel;
        Refresh();
    }

    [RelayCommand]
    private void ActOnOrder(HubOrderViewModel? order)
    {
        if (order is null) return;
        if (order.Channel == "online")
        {
            _pos.AcceptOnlineOrder(order.Id);
            Refresh();
            return;
        }
        if (order.Channel == "open")
        {
            _launch.ResumeOrderId = order.Id;
            _navigation.NavigateTo(AppScreen.Pos);
            return;
        }
        _navigation.NavigateTo(AppScreen.Terminal);
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);
}
