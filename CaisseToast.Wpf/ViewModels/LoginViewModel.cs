using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly DispatcherTimer _clock;
    private string _pinBuffer = "";

    [ObservableProperty] private int _filledDots;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _restaurantName = "Mon Restaurant";
    [ObservableProperty] private string _dateText = "";
    [ObservableProperty] private string _timeText = "";

    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        RestaurantName = auth.Config.Name;
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        DateText = now.ToString("ddd d MMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
        TimeText = now.ToString("HH:mm:ss");
    }

    [RelayCommand]
    private void Digit(string digit)
    {
        ErrorMessage = "";
        if (_pinBuffer.Length >= 4) return;
        _pinBuffer += digit;
        FilledDots = _pinBuffer.Length;
        if (_pinBuffer.Length == 4) TryLogin();
    }

    [RelayCommand]
    private void Clear()
    {
        _pinBuffer = "";
        FilledDots = 0;
        ErrorMessage = "";
    }

    [RelayCommand]
    private void Delete()
    {
        if (_pinBuffer.Length == 0) return;
        _pinBuffer = _pinBuffer[..^1];
        FilledDots = _pinBuffer.Length;
        ErrorMessage = "";
    }

    [RelayCommand]
    private void Submit() => TryLogin();

    private void TryLogin()
    {
        if (_pinBuffer.Length < 4)
        {
            ErrorMessage = "Entrez 4 chiffres";
            return;
        }

        if (_auth.Authenticate(_pinBuffer) is null)
        {
            ErrorMessage = "Code incorrect — réessayez";
            _pinBuffer = "";
            FilledDots = 0;
            return;
        }

        ErrorMessage = "";
        LoginSucceeded?.Invoke();
    }
}
