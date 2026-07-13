using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class ShellHeaderViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly DispatcherTimer _clock;

    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private string _restaurantName = "Mon Restaurant";
    [ObservableProperty] private string _registerName = "Caisse 1";
    [ObservableProperty] private string _userName = "—";
    [ObservableProperty] private string _userRole = "—";
    [ObservableProperty] private string _employeeCode = "—";
    [ObservableProperty] private string _ticketNumber = "#0001";
    [ObservableProperty] private string _dateText = "";
    [ObservableProperty] private string _timeText = "";

    public event Action? LogoutRequested;
    public event Action? HomeRequested;
    public event Action? AdminRequested;

    public ShellHeaderViewModel(IAuthService auth)
    {
        _auth = auth;
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();
        Refresh();
    }

    public void Refresh()
    {
        IsAuthenticated = _auth.IsAuthenticated;
        RestaurantName = _auth.Config.Name;
        RegisterName = _auth.Config.Register;

        if (_auth.CurrentEmployee is { } emp)
        {
            UserName = emp.Name;
            UserRole = emp.Job;
            EmployeeCode = emp.Pin.PadLeft(4, '0');
        }
        else
        {
            UserName = "—";
            UserRole = "—";
            EmployeeCode = "—";
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        DateText = now.ToString("ddd d MMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
        TimeText = now.ToString("HH:mm:ss");
    }

    [RelayCommand]
    private void GoHome() => HomeRequested?.Invoke();

    [RelayCommand]
    private void OpenAdmin() => AdminRequested?.Invoke();

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke();
}
