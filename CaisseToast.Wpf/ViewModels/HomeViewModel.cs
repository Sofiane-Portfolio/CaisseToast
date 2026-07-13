using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _openOrders = "0";
    [ObservableProperty] private string _paidToday = "0";
    [ObservableProperty] private string _revenue = "0,00 €";
    [ObservableProperty] private string _tablesInfo = "42";
    [ObservableProperty] private string _tablesSub = "32 libres";

    public HomeViewModel(IAuthService auth, INavigationService navigation)
    {
        _auth = auth;
        _navigation = navigation;
        Refresh();
    }

    public void Refresh()
    {
        UserName = _auth.CurrentEmployee?.Name ?? "—";
        // Demo values — brancher sur OrderService plus tard
        OpenOrders = "1";
        PaidToday = "11";
        Revenue = "171,52 €";
        TablesInfo = "42";
        TablesSub = "32 libres · 6 occupées";
    }

    [RelayCommand]
    private void OpenPos() => _navigation.ShowPlaceholder("Quick Order", "Prise de commande rapide — Phase 2");

    [RelayCommand]
    private void OpenTerminal() => _navigation.ShowPlaceholder("Payment Terminal", "Open · Paid · Closed — Phase 2");

    [RelayCommand]
    private void OpenTables() => _navigation.ShowPlaceholder("Table Service", "Plan de salle interactif — Phase 2");

    [RelayCommand]
    private void OpenKitchen() => _navigation.ShowPlaceholder("Kitchen Display", "Tickets cuisine — Phase 2");

    [RelayCommand]
    private void OpenKiosk() => _navigation.ShowPlaceholder("Kiosk", "Borne client — Phase 2");

    [RelayCommand]
    private void OpenHub() => _navigation.ShowPlaceholder("Orders Hub", "Vue centralisée — Phase 2");

    [RelayCommand]
    private void SwitchUser()
    {
        _auth.Logout();
        _navigation.NavigateTo(AppScreen.Login);
    }
}

public partial class PlaceholderViewModel : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _subtitle;

    public event Action? BackRequested;

    public PlaceholderViewModel(string title, string subtitle)
    {
        _title = title;
        _subtitle = subtitle;
    }

    [RelayCommand]
    private void GoBack() => BackRequested?.Invoke();
}
