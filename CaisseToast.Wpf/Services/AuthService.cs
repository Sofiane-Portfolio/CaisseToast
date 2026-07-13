using CaisseToast.Wpf.Models;

namespace CaisseToast.Wpf.Services;

public interface IAuthService
{
    IReadOnlyList<Employee> Employees { get; }
    RestaurantConfig Config { get; }
    Employee? CurrentEmployee { get; }
    bool IsAuthenticated { get; }
    Employee? Authenticate(string pin);
    void Logout();
}

public sealed class AuthService : IAuthService
{
    public RestaurantConfig Config { get; } = new();

    public IReadOnlyList<Employee> Employees { get; } =
    [
        new() { Id = 1, Name = "Sofiane", Pin = "1234", Role = UserRole.Manager },
        new() { Id = 2, Name = "Admin", Pin = "0000", Role = UserRole.Admin },
        new() { Id = 3, Name = "Marie", Pin = "1111", Role = UserRole.Cashier },
        new() { Id = 4, Name = "Thomas", Pin = "2222", Role = UserRole.Server },
        new() { Id = 5, Name = "Julie", Pin = "3333", Role = UserRole.Cashier },
    ];

    public Employee? CurrentEmployee { get; private set; }
    public bool IsAuthenticated => CurrentEmployee is not null;

    public Employee? Authenticate(string pin)
    {
        CurrentEmployee = Employees.FirstOrDefault(e => e.Pin == pin);
        return CurrentEmployee;
    }

    public void Logout() => CurrentEmployee = null;
}
