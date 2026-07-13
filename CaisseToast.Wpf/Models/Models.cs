namespace CaisseToast.Wpf.Models;

public enum UserRole
{
    Server,
    Cashier,
    Manager,
    Admin
}

public sealed class Employee
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Pin { get; init; } = "";
    public UserRole Role { get; init; }
    public string Job => Role switch
    {
        UserRole.Manager => "Manager",
        UserRole.Admin => "Administrateur",
        UserRole.Cashier => "Caissier",
        _ => "Serveur"
    };
}

public sealed class RestaurantConfig
{
    public string Name { get; set; } = "Mon Restaurant";
    public string Register { get; set; } = "Caisse 1";
    public decimal TvaRate { get; set; } = 10m;
}

public enum AppScreen
{
    Login,
    Home,
    Pos,
    Tables,
    Terminal,
    Kitchen,
    Kiosk,
    Admin
}
