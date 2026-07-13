using CaisseToast.Wpf.Models;

namespace CaisseToast.Wpf.Services;

public interface INavigationService
{
    AppScreen CurrentScreen { get; }
    event Action? ScreenChanged;
    event Action<string, string>? PlaceholderRequested;
    void NavigateTo(AppScreen screen);
    void ShowPlaceholder(string title, string subtitle);
}

public sealed class NavigationService : INavigationService
{
    public AppScreen CurrentScreen { get; private set; } = AppScreen.Login;
    public event Action? ScreenChanged;
    public event Action<string, string>? PlaceholderRequested;

    public void NavigateTo(AppScreen screen)
    {
        CurrentScreen = screen;
        ScreenChanged?.Invoke();
    }

    public void ShowPlaceholder(string title, string subtitle)
        => PlaceholderRequested?.Invoke(title, subtitle);
}
