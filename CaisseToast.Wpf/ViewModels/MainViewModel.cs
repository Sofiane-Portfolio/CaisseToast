using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IAuthService _auth;
    private readonly IPosStateService _pos;
    private readonly LoginViewModel _loginViewModel;
    private readonly HomeViewModel _homeViewModel;
    private readonly PosViewModel _posViewModel;
    private readonly TableServiceViewModel _tableViewModel;
    private readonly TerminalViewModel _terminalViewModel;
    private readonly KitchenViewModel _kitchenViewModel;
    private readonly KioskViewModel _kioskViewModel;
    private readonly OrdersHubViewModel _ordersHubViewModel;
    private readonly AdminViewModel _adminViewModel;
    private readonly ServerOrdersViewModel _serverOrdersViewModel;
    private readonly ServerReportViewModel _serverReportViewModel;
    private readonly OnlineOrderingViewModel _onlineOrderingViewModel;

    public ShellHeaderViewModel Header { get; }

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private bool _showShellHeader;

    public MainViewModel(
        INavigationService navigation,
        IAuthService auth,
        IPosStateService pos,
        ShellHeaderViewModel header,
        LoginViewModel loginViewModel,
        HomeViewModel homeViewModel,
        PosViewModel posViewModel,
        TableServiceViewModel tableViewModel,
        TerminalViewModel terminalViewModel,
        KitchenViewModel kitchenViewModel,
        KioskViewModel kioskViewModel,
        OrdersHubViewModel ordersHubViewModel,
        AdminViewModel adminViewModel,
        ServerOrdersViewModel serverOrdersViewModel,
        ServerReportViewModel serverReportViewModel,
        OnlineOrderingViewModel onlineOrderingViewModel)
    {
        _navigation = navigation;
        _auth = auth;
        _pos = pos;
        Header = header;
        _loginViewModel = loginViewModel;
        _homeViewModel = homeViewModel;
        _posViewModel = posViewModel;
        _tableViewModel = tableViewModel;
        _terminalViewModel = terminalViewModel;
        _kitchenViewModel = kitchenViewModel;
        _kioskViewModel = kioskViewModel;
        _ordersHubViewModel = ordersHubViewModel;
        _adminViewModel = adminViewModel;
        _serverOrdersViewModel = serverOrdersViewModel;
        _serverReportViewModel = serverReportViewModel;
        _onlineOrderingViewModel = onlineOrderingViewModel;

        _navigation.ScreenChanged += OnScreenChanged;
        _navigation.PlaceholderRequested += OnPlaceholderRequested;
        _loginViewModel.LoginSucceeded += OnLoginSucceeded;
        Header.LogoutRequested += OnLogoutRequested;
        Header.HomeRequested += OnHomeRequested;
        Header.AdminRequested += OnAdminRequested;

        CurrentView = _loginViewModel;
        ShowShellHeader = false;
        Header.Refresh();
    }

    private void OnLoginSucceeded()
    {
        var emp = _auth.CurrentEmployee;
        if (emp?.Role == UserRole.Server)
        {
            _pos.ClockIn();
            _navigation.NavigateTo(AppScreen.Tables);
            _tableViewModel.Refresh();
        }
        else
        {
            _navigation.NavigateTo(AppScreen.Home);
            _homeViewModel.Refresh();
        }
        Header.Refresh();
    }

    private void OnHomeRequested()
    {
        if (_auth.CurrentEmployee?.Role == UserRole.Server)
            _navigation.NavigateTo(AppScreen.Tables);
        else
            _navigation.NavigateTo(AppScreen.Home);
    }

    private void OnAdminRequested()
    {
        if (_auth.CurrentEmployee?.Role is UserRole.Manager or UserRole.Admin)
            _navigation.NavigateTo(AppScreen.Admin);
        else
            _navigation.ShowPlaceholder("Admin", "Accès réservé aux managers");
    }

    private void OnLogoutRequested()
    {
        _auth.Logout();
        _pos.ClockOut();
        _navigation.NavigateTo(AppScreen.Login);
        Header.Refresh();
        CurrentView = _loginViewModel;
        ShowShellHeader = false;
    }

    private void OnScreenChanged()
    {
        CurrentView = _navigation.CurrentScreen switch
        {
            AppScreen.Login => _loginViewModel,
            AppScreen.Home => _homeViewModel,
            AppScreen.Pos => _posViewModel,
            AppScreen.Tables => _tableViewModel,
            AppScreen.Terminal => _terminalViewModel,
            AppScreen.Kitchen => _kitchenViewModel,
            AppScreen.Kiosk => _kioskViewModel,
            AppScreen.Admin => _adminViewModel,
            AppScreen.OrdersHub => _ordersHubViewModel,
            AppScreen.ServerOrders => _serverOrdersViewModel,
            AppScreen.ServerReport => _serverReportViewModel,
            AppScreen.Online => _onlineOrderingViewModel,
            _ => CurrentView
        };

        ShowShellHeader = _navigation.CurrentScreen is not AppScreen.Login and not AppScreen.Kiosk;

        switch (CurrentView)
        {
            case HomeViewModel home: home.Refresh(); break;
            case TableServiceViewModel tables: tables.Refresh(); break;
            case KioskViewModel kiosk: kiosk.Reset(); break;
            case PosViewModel pos: pos.Initialize(); break;
            case TerminalViewModel terminal: terminal.Refresh(); break;
            case KitchenViewModel kitchen: kitchen.Refresh(); break;
            case OrdersHubViewModel hub: hub.Refresh(); break;
            case AdminViewModel admin: admin.Refresh(); break;
            case ServerOrdersViewModel serverOrders: serverOrders.Refresh(); break;
            case ServerReportViewModel serverReport: serverReport.Refresh(); break;
            case OnlineOrderingViewModel online: online.Refresh(); break;
        }
        Header.Refresh();
    }

    private void OnPlaceholderRequested(string title, string subtitle)
    {
        var vm = new PlaceholderViewModel(title, subtitle);
        vm.BackRequested += () =>
        {
            if (_auth.CurrentEmployee?.Role == UserRole.Server)
                _navigation.NavigateTo(AppScreen.Tables);
            else
                _navigation.NavigateTo(AppScreen.Home);
        };
        CurrentView = vm;
        ShowShellHeader = true;
    }
}
