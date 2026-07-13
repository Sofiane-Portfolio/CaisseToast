using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class ServerOrderViewModel
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string TotalLabel { get; init; } = "";
    public string Meta { get; init; } = "";
}

public partial class ServerOrdersViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _serverName = "";
    [ObservableProperty] private string _orderCount = "0";

    public ObservableCollection<ServerOrderViewModel> Orders { get; } = [];

    public ServerOrdersViewModel(IAuthService auth, IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
    }

    public void Refresh()
    {
        ServerName = _auth.CurrentEmployee?.Name ?? "Serveur";
        Orders.Clear();
        var list = _pos.GetOpenOrdersForServer(ServerName);
        foreach (var o in list)
        {
            Orders.Add(new ServerOrderViewModel
            {
                Id = o.Id,
                Number = o.Number,
                DisplayName = o.DisplayName,
                TotalLabel = $"{o.Total:N2} €",
                Meta = $"{o.TimeLabel} · {o.ItemCount} art."
            });
        }
        OrderCount = Orders.Count.ToString();
    }

    [RelayCommand]
    private void ResumeOrder(ServerOrderViewModel? order)
    {
        if (order is null) return;
        _launch.ResumeOrderId = order.Id;
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Tables);
}
