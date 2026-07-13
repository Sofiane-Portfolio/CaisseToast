using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _openOrders = "0";
    [ObservableProperty] private string _paidToday = "0";
    [ObservableProperty] private string _revenue = "0,00 €";
    [ObservableProperty] private string _tablesInfo = "42";
    [ObservableProperty] private string _tablesSub = "32 libres";

    public HomeViewModel(IAuthService auth, IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
        _pos.StateChanged += () => Refresh();
        Refresh();
    }

    public void Refresh()
    {
        UserName = _auth.CurrentEmployee?.Name ?? "—";
        OpenOrders = _pos.OpenOrderCount.ToString();
        PaidToday = _pos.PaidTodayCount.ToString();
        Revenue = $"{_pos.RevenueToday:N2} €";
        TablesInfo = _pos.Tables.Count.ToString();
        var c = _pos.GetTableCounts();
        TablesSub = $"{c.free} libres · {c.occupied} occupées";
    }

    [RelayCommand]
    private void OpenPos()
    {
        _launch.Clear();
        _navigation.NavigateTo(AppScreen.Pos);
    }

    [RelayCommand]
    private void OpenTerminal() => _navigation.NavigateTo(AppScreen.Terminal);

    [RelayCommand]
    private void OpenTables() => _navigation.NavigateTo(AppScreen.Tables);

    [RelayCommand]
    private void OpenKitchen() => _navigation.NavigateTo(AppScreen.Kitchen);

    [RelayCommand]
    private void OpenKiosk() => _navigation.NavigateTo(AppScreen.Kiosk);

    [RelayCommand]
    private void OpenHub() => _navigation.NavigateTo(AppScreen.OrdersHub);

    [RelayCommand]
    private void OpenOnline() => _navigation.NavigateTo(AppScreen.Online);

    [RelayCommand]
    private void SwitchUser()
    {
        _auth.Logout();
        _pos.ClockOut();
        _navigation.NavigateTo(AppScreen.Login);
    }
}
