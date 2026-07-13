using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class KioskProductViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string CategoryLabel { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string PriceLabel => $"{Price:N2} €";
    public bool IsUnavailable => Stock <= 0;
    public bool IsLowStock => Stock > 0 && Stock <= 5;
}

public partial class KioskCartLineViewModel : ObservableObject
{
    public int ProductId { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }

    [ObservableProperty] private int _quantity;

    public string PriceLabel => $"{Price:N2} €";
    public string LineTotalLabel => $"{Price * Quantity:N2} €";

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotalLabel));
}

public partial class KioskViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly INavigationService _navigation;

    [ObservableProperty] private bool _showWelcome = true;
    [ObservableProperty] private string _restaurantName = "Mon Restaurant";
    [ObservableProperty] private string _diningBadge = "Sur place";
    [ObservableProperty] private string _cartSummary = "Panier vide";
    [ObservableProperty] private string _subtotalLabel = "0,00 €";
    [ObservableProperty] private string _taxLabel = "0,00 €";
    [ObservableProperty] private string _totalLabel = "0,00 €";
    [ObservableProperty] private string _confirmTotalLabel = "0,00 €";
    [ObservableProperty] private string _confirmItemsText = "";
    [ObservableProperty] private bool _showConfirm;
    [ObservableProperty] private bool _canOrder;
    [ObservableProperty] private string _selectedCategory = "all";

    public ObservableCollection<KioskProductViewModel> Products { get; } = [];
    public ObservableCollection<KioskCartLineViewModel> CartLines { get; } = [];

    public KioskViewModel(IAuthService auth, IPosStateService pos, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _navigation = navigation;
        RestaurantName = auth.Config.Name;
    }

    public void Reset()
    {
        ShowWelcome = true;
        ShowConfirm = false;
        SelectedCategory = "all";
        CartLines.Clear();
        LoadProducts();
        UpdateTotals();
    }

    [RelayCommand]
    private void StartDineIn() => StartOrdering(KioskDiningMode.DineIn);

    [RelayCommand]
    private void StartTakeOut() => StartOrdering(KioskDiningMode.TakeOut);

    private KioskDiningMode _diningMode = KioskDiningMode.DineIn;

    private void StartOrdering(KioskDiningMode mode)
    {
        _diningMode = mode;
        DiningBadge = mode == KioskDiningMode.DineIn ? "Sur place" : "À emporter";
        ShowWelcome = false;
        LoadProducts();
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
        LoadProducts();
    }

    [RelayCommand]
    private void AddProduct(KioskProductViewModel? product)
    {
        if (product is null || product.IsUnavailable) return;
        var existing = CartLines.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing is not null) existing.Quantity++;
        else CartLines.Add(new KioskCartLineViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            Quantity = 1
        });
        UpdateTotals();
    }

    [RelayCommand]
    private void IncreaseQty(KioskCartLineViewModel? line)
    {
        if (line is null) return;
        line.Quantity++;
        UpdateTotals();
    }

    [RelayCommand]
    private void DecreaseQty(KioskCartLineViewModel? line)
    {
        if (line is null) return;
        line.Quantity--;
        if (line.Quantity <= 0) CartLines.Remove(line);
        UpdateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartLines.Clear();
        UpdateTotals();
    }

    [RelayCommand]
    private void SubmitOrder()
    {
        if (!CartLines.Any()) return;
        ConfirmTotalLabel = TotalLabel;
        ConfirmItemsText = string.Join(Environment.NewLine,
            CartLines.Select(c => $"{c.Quantity}× {c.Name} — {c.LineTotalLabel}"));
        ShowConfirm = true;
    }

    [RelayCommand]
    private void CancelConfirm() => ShowConfirm = false;

    [RelayCommand]
    private void ConfirmOrder()
    {
        var lines = CartLines.Select(c => new KioskCartLine
        {
            ProductId = c.ProductId,
            Name = c.Name,
            Price = c.Price,
            Quantity = c.Quantity
        });
        var order = _pos.SubmitKioskOrder(lines, _diningMode);
        ShowConfirm = false;
        CartLines.Clear();
        UpdateTotals();
        _navigation.NavigateTo(AppScreen.Kitchen);
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);

    private void LoadProducts()
    {
        Products.Clear();
        if (ShowWelcome) return;

        var query = _pos.Products.AsEnumerable();
        if (SelectedCategory != "all")
        {
            query = SelectedCategory switch
            {
                "food" => query.Where(p => p.Category == ProductCategory.Food),
                "drinks" => query.Where(p => p.Category == ProductCategory.Drinks),
                "desserts" => query.Where(p => p.Category == ProductCategory.Desserts),
                _ => query
            };
        }

        foreach (var p in query)
        {
            Products.Add(new KioskProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                CategoryLabel = p.CategoryLabel,
                ImageUrl = p.ImageUrl
            });
        }
    }

    private void UpdateTotals()
    {
        var count = CartLines.Sum(c => c.Quantity);
        CartSummary = count == 0 ? "Panier vide" : $"{count} article{(count > 1 ? "s" : "")}";
        var sub = CartLines.Sum(c => c.LineTotal);
        var tax = sub * 0.10m;
        var total = sub + tax;
        SubtotalLabel = $"{sub:N2} €";
        TaxLabel = $"{tax:N2} €";
        TotalLabel = $"{total:N2} €";
        CanOrder = count > 0;
    }
}
