using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class PosProductViewModel : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string CategoryLabel { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public bool HasModifiers { get; init; }
    public string[] ModifierOptions { get; init; } = [];
    public string PriceLabel => $"{Price:N2} €";
    public bool IsUnavailable => Stock <= 0;
}

public partial class PosCartLineViewModel : ObservableObject
{
    public int ProductId { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; set; }
    public List<string> Modifiers { get; init; } = [];

    [ObservableProperty] private int _quantity;
    [ObservableProperty] private bool _sentToKitchen;

    public string DisplayName => Modifiers.Count > 0 ? $"{Name} ({string.Join(" · ", Modifiers)})" : Name;
    public string PriceLabel => $"{Price:N2} €";
    public string LineTotalLabel => $"{Price * Quantity:N2} €";
    public string StatusLabel => SentToKitchen ? "Envoyé" : "À envoyer";

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotalLabel));
        OnPropertyChanged(nameof(StatusLabel));
    }

    partial void OnSentToKitchenChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));
}

public partial class PosViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly IPosLaunchService _launch;
    private readonly INavigationService _navigation;
    private PosOrder? _currentOrder;
    private PosProductViewModel? _pendingProduct;

    [ObservableProperty] private string _ticketBadge = "Quick Order";
    [ObservableProperty] private string _ticketNumber = "#0001";
    [ObservableProperty] private string _tabName = "";
    [ObservableProperty] private string _subtotalLabel = "0,00 €";
    [ObservableProperty] private string _taxLabel = "0,00 €";
    [ObservableProperty] private string _totalLabel = "0,00 €";
    [ObservableProperty] private string _selectedCategory = "all";
    [ObservableProperty] private bool _hasItems;
    [ObservableProperty] private bool _showPayment;
    [ObservableProperty] private bool _showModifier;
    [ObservableProperty] private string _modifierProductName = "";
    [ObservableProperty] private string _paymentMethod = "Carte";
    [ObservableProperty] private int? _tableNumber;

    public ObservableCollection<PosProductViewModel> Products { get; } = [];
    public ObservableCollection<PosCartLineViewModel> CartLines { get; } = [];
    public ObservableCollection<ModifierOptionViewModel> ModifierOptions { get; } = [];

    public PosViewModel(IAuthService auth, IPosStateService pos, IPosLaunchService launch, INavigationService navigation)
    {
        _auth = auth;
        _pos = pos;
        _launch = launch;
        _navigation = navigation;
    }

    public void Initialize()
    {
        ShowPayment = false;
        ShowModifier = false;
        CartLines.Clear();

        if (_launch.ResumeOrderId is int resumeId)
        {
            var existing = _pos.ResumeOrder(resumeId);
            if (existing is not null)
            {
                LoadOrder(existing);
                _launch.Clear();
                LoadProducts();
                UpdateTotals();
                return;
            }
        }

        if (_launch.TableNumber is int table)
        {
            TableNumber = table;
            var existing = _pos.FindOpenOrderForTable(table);
            if (existing is not null) LoadOrder(existing);
            else
            {
                _currentOrder = _pos.CreateOrder(table, _launch.TabName ?? "", "POS", _auth.CurrentEmployee?.Name ?? "Serveur");
                TabName = _launch.TabName ?? "";
                TicketNumber = _currentOrder.Number;
                TicketBadge = $"Table T{table}";
            }
        }
        else
        {
            TableNumber = null;
            _currentOrder = _pos.CreateOrder(null, "", "POS", _auth.CurrentEmployee?.Name ?? "Caissier");
            TicketNumber = _currentOrder.Number;
            TicketBadge = "Quick Order";
            TabName = "";
        }

        _launch.Clear();
        LoadProducts();
        UpdateTotals();
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
        LoadProducts();
    }

    [RelayCommand]
    private void AddProduct(PosProductViewModel? product)
    {
        if (product is null || product.IsUnavailable || _currentOrder is null) return;
        if (product.HasModifiers)
        {
            _pendingProduct = product;
            ModifierProductName = product.Name;
            ModifierOptions.Clear();
            foreach (var opt in product.ModifierOptions)
                ModifierOptions.Add(new ModifierOptionViewModel { Label = opt });
            ShowModifier = true;
            return;
        }
        AddLineToCart(product, []);
    }

    [RelayCommand]
    private void ConfirmModifier()
    {
        if (_pendingProduct is null) return;
        var selected = ModifierOptions.Where(o => o.IsSelected).Select(o => o.Label).ToList();
        AddLineToCart(_pendingProduct, selected);
        _pendingProduct = null;
        ShowModifier = false;
        ModifierOptions.Clear();
    }

    [RelayCommand]
    private void CancelModifier()
    {
        _pendingProduct = null;
        ShowModifier = false;
        ModifierOptions.Clear();
    }

    [RelayCommand]
    private void ToggleModifier(ModifierOptionViewModel? option)
    {
        if (option is null) return;
        option.IsSelected = !option.IsSelected;
    }

    [RelayCommand]
    private void IncreaseQty(PosCartLineViewModel? line)
    {
        if (line is null || line.SentToKitchen) return;
        line.Quantity++;
        SyncOrder();
        UpdateTotals();
    }

    [RelayCommand]
    private void DecreaseQty(PosCartLineViewModel? line)
    {
        if (line is null || line.SentToKitchen) return;
        line.Quantity--;
        if (line.Quantity <= 0) CartLines.Remove(line);
        SyncOrder();
        UpdateTotals();
    }

    [RelayCommand]
    private void SendKitchen()
    {
        if (_currentOrder is null || !CartLines.Any()) return;
        SyncOrder();
        _pos.SendToKitchen(_currentOrder);
        foreach (var line in CartLines) line.SentToKitchen = true;
        UpdateTotals();
    }

    [RelayCommand]
    private void HoldOrder()
    {
        if (_currentOrder is null || !CartLines.Any()) return;
        SyncOrder();
        _pos.HoldOrder(_currentOrder);
        GoBack();
    }

    [RelayCommand]
    private void RequestBill()
    {
        if (TableNumber is not int table) return;
        SyncOrder();
        _pos.HoldOrder(_currentOrder!);
        _pos.RequestBill(table);
        GoBack();
    }

    [RelayCommand]
    private void OpenPayment()
    {
        if (!CartLines.Any()) return;
        SyncOrder();
        ShowPayment = true;
    }

    [RelayCommand]
    private void CancelPayment() => ShowPayment = false;

    [RelayCommand]
    private void SelectPayment(string? method)
    {
        if (!string.IsNullOrWhiteSpace(method))
            PaymentMethod = method;
    }

    [RelayCommand]
    private void ConfirmPayment()
    {
        if (_currentOrder is null) return;
        SyncOrder();
        _pos.HoldOrder(_currentOrder);
        _pos.PayOrder(_currentOrder.Id, _auth.CurrentEmployee?.Name ?? "Caissier", PaymentMethod);
        ShowPayment = false;
        CartLines.Clear();
        GoBack();
    }

    [RelayCommand]
    private void CancelSale()
    {
        if (_currentOrder is not null && CartLines.Any())
            _pos.CancelOpenOrder(_currentOrder.Id);
        CartLines.Clear();
        GoBack();
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_auth.CurrentEmployee?.Role == UserRole.Server)
            _navigation.NavigateTo(AppScreen.Tables);
        else
            _navigation.NavigateTo(AppScreen.Home);
    }

    private void LoadOrder(PosOrder order)
    {
        _currentOrder = order;
        TableNumber = order.TableNumber;
        TabName = order.TabName;
        TicketNumber = order.Number;
        TicketBadge = order.DisplayName;
        foreach (var line in order.Lines)
        {
            CartLines.Add(new PosCartLineViewModel
            {
                ProductId = line.ProductId,
                Name = line.Name,
                Price = line.Price,
                Quantity = line.Quantity,
                SentToKitchen = line.SentToKitchen,
                Modifiers = [..line.Modifiers]
            });
        }
    }

    private void AddLineToCart(PosProductViewModel product, List<string> modifiers)
    {
        var existing = CartLines.FirstOrDefault(c =>
            !c.SentToKitchen &&
            c.ProductId == product.Id &&
            string.Join(",", c.Modifiers) == string.Join(",", modifiers));

        if (existing is not null) existing.Quantity++;
        else CartLines.Add(new PosCartLineViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            Quantity = 1,
            Modifiers = modifiers
        });
        SyncOrder();
        UpdateTotals();
    }

    private void SyncOrder()
    {
        if (_currentOrder is null) return;
        _currentOrder.TabName = TabName;
        _currentOrder.Lines = CartLines.Select(c => new OrderLine
        {
            ProductId = c.ProductId,
            Name = c.Name,
            Price = c.Price,
            Quantity = c.Quantity,
            SentToKitchen = c.SentToKitchen,
            Modifiers = [..c.Modifiers]
        }).ToList();
        _pos.SaveOrder(_currentOrder);
    }

    private void LoadProducts()
    {
        Products.Clear();
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
            Products.Add(new PosProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                CategoryLabel = p.CategoryLabel,
                ImageUrl = p.ImageUrl,
                HasModifiers = p.HasModifiers,
                ModifierOptions = p.ModifierOptions
            });
        }
    }

    private void UpdateTotals()
    {
        HasItems = CartLines.Any();
        var sub = CartLines.Sum(c => c.Price * c.Quantity);
        var tax = Math.Round(sub * 0.10m, 2);
        SubtotalLabel = $"{sub:N2} €";
        TaxLabel = $"{tax:N2} €";
        TotalLabel = $"{sub + tax:N2} €";
    }
}
