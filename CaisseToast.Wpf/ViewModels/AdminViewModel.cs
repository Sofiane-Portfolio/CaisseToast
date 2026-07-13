using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class AdminProductViewModel
{
    public string Name { get; init; } = "";
    public string PriceLabel { get; init; } = "";
    public string StockLabel { get; init; } = "";
    public string Category { get; init; } = "";
}

public partial class AdminEmployeeViewModel
{
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string Pin { get; init; } = "";
}

public partial class AdminViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _activeTab = "dashboard";
    [ObservableProperty] private string _restaurantName = "";
    [ObservableProperty] private string _registerName = "";
    [ObservableProperty] private string _openOrders = "0";
    [ObservableProperty] private string _paidToday = "0";
    [ObservableProperty] private string _revenue = "0,00 €";
    [ObservableProperty] private string _kitchenTickets = "0";

    public ObservableCollection<AdminProductViewModel> Products { get; } = [];
    public ObservableCollection<AdminEmployeeViewModel> Employees { get; } = [];

    public bool CanAccess => _auth.CurrentEmployee?.Role is UserRole.Manager or UserRole.Admin;

    public AdminViewModel(IAuthService auth, IPosStateService pos, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        RestaurantName = _auth.Config.Name;
        RegisterName = _auth.Config.Register;
        OpenOrders = _pos.OpenOrderCount.ToString();
        PaidToday = _pos.PaidTodayCount.ToString();
        Revenue = $"{_pos.RevenueToday:N2} €";
        KitchenTickets = _pos.KitchenTickets.Count(t => t.Status != KitchenTicketStatus.Done).ToString();

        Products.Clear();
        foreach (var p in _pos.Products)
        {
            Products.Add(new AdminProductViewModel
            {
                Name = p.Name,
                PriceLabel = $"{p.Price:N2} €",
                StockLabel = p.Stock <= 5 ? $"Stock bas ({p.Stock})" : $"{p.Stock} en stock",
                Category = p.CategoryLabel
            });
        }

        Employees.Clear();
        foreach (var e in _auth.Employees)
        {
            Employees.Add(new AdminEmployeeViewModel
            {
                Name = e.Name,
                Role = e.Job,
                Pin = "****"
            });
        }

        OnPropertyChanged(nameof(CanAccess));
    }

    [RelayCommand]
    private void SelectTab(string tab) => ActiveTab = tab;

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);
}
