using BalanceSystem.Core.Interfaces;
using BalanceSystem.Infrastructure.DataAcquisition;
using BalanceSystem.Infrastructure.Database;
using BalanceSystem.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BalanceSystem.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBalanceSystemServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var logger = LogConfig.CreateLogger();
        services.AddLogging(builder => builder.AddSerilog(logger));

        var connStr = configuration["AppSettings:ConnectionString"]
            ?? "Server=localhost;Database=BalanceSystem;Trusted_Connection=true;TrustServerCertificate=true;";
        var dbContext = new AppDbContext(connStr);
        services.AddSingleton(dbContext);
        services.AddTransient<DatabaseInitializer>();

        services.AddSingleton<IDataAcquisitionService, CsvSimulationService>();

        return services;
    }
}
