using System.Windows;
using CaisseToast.Wpf.Services;
using CaisseToast.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CaisseToast.Wpf;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ShellHeaderViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
