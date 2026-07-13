using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class ServerReportViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _serverName = "";
    [ObservableProperty] private string _shiftDuration = "";
    [ObservableProperty] private string _ordersCount = "0";
    [ObservableProperty] private string _revenue = "0,00 €";
    [ObservableProperty] private string _tips = "0,00 €";
    [ObservableProperty] private string _tablesServed = "0";

    public ServerReportViewModel(IAuthService auth, IPosStateService pos, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _navigation = navigation;
    }

    public void Refresh()
    {
        ServerName = _auth.CurrentEmployee?.Name ?? "Serveur";
        var report = _pos.GetServerReport(ServerName);
        ShiftDuration = report.ShiftDuration;
        OrdersCount = report.OrdersCount.ToString();
        Revenue = $"{report.Revenue:N2} €";
        Tips = $"{report.Tips:N2} €";
        TablesServed = report.TablesServed.ToString();
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Tables);
}
