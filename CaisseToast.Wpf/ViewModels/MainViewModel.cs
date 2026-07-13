using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IAuthService _auth;
    private readonly LoginViewModel _loginViewModel;
    private readonly HomeViewModel _homeViewModel;

    public ShellHeaderViewModel Header { get; }

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel(
        INavigationService navigation,
        IAuthService auth,
        ShellHeaderViewModel header,
        LoginViewModel loginViewModel,
        HomeViewModel homeViewModel)
    {
        _navigation = navigation;
        _auth = auth;
        Header = header;
        _loginViewModel = loginViewModel;
        _homeViewModel = homeViewModel;

        _navigation.ScreenChanged += OnScreenChanged;
        _navigation.PlaceholderRequested += OnPlaceholderRequested;
        _loginViewModel.LoginSucceeded += OnLoginSucceeded;
        Header.LogoutRequested += OnLogoutRequested;
        Header.HomeRequested += () => _navigation.NavigateTo(AppScreen.Home);
        Header.AdminRequested += () => _navigation.ShowPlaceholder("Admin", "Back-office Light Banana");

        CurrentView = _loginViewModel;
        Header.Refresh();
    }

    private void OnLoginSucceeded()
    {
        _navigation.NavigateTo(AppScreen.Home);
        _homeViewModel.Refresh();
        Header.Refresh();
    }

    private void OnLogoutRequested()
    {
        _auth.Logout();
        _navigation.NavigateTo(AppScreen.Login);
        Header.Refresh();
        CurrentView = _loginViewModel;
    }

    private void OnScreenChanged()
    {
        CurrentView = _navigation.CurrentScreen switch
        {
            AppScreen.Login => _loginViewModel,
            AppScreen.Home => _homeViewModel,
            _ => CurrentView
        };
        if (CurrentView is HomeViewModel home) home.Refresh();
        Header.Refresh();
    }

    private void OnPlaceholderRequested(string title, string subtitle)
    {
        var vm = new PlaceholderViewModel(title, subtitle);
        vm.BackRequested += () => _navigation.NavigateTo(AppScreen.Home);
        CurrentView = vm;
    }
}
