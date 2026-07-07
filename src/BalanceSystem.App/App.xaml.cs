using System.Windows;
using BalanceSystem.App.DependencyInjection;
using BalanceSystem.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BalanceSystem.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddBalanceSystemServices(configuration);

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var initializer = Services.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();
    }
}
