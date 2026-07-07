using BalanceSystem.Core.Interfaces;
using BalanceSystem.Core.Services;
using BalanceSystem.Infrastructure.DataAcquisition;
using BalanceSystem.Infrastructure.Database;
using BalanceSystem.Infrastructure.Logging;
using BalanceSystem.Infrastructure.Reporting;
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

        // TODO: Phase 2 — register real hardware speed measurement service
        // services.AddSingleton<ISpeedMeasurementService, HardwareSpeedMeasurementService>();

        // Business services
        services.AddSingleton<BalanceSystem.Core.Services.IBalancingTestService,
                               BalanceSystem.Core.Services.BalancingTestService>();

        // Phase 2 — Data management services
        services.AddSingleton<IRecipeService, RecipeService>();
        services.AddSingleton<ITestRecordService, TestRecordService>();
        services.AddSingleton<TestReportService>();

        // ViewModels
        services.AddSingleton<ViewModels.MonitoringViewModel>();
        services.AddSingleton<ViewModels.BalancingTestViewModel>();
        services.AddSingleton<ViewModels.RecipeManagementViewModel>();
        services.AddSingleton<ViewModels.HistoryViewModel>();
        services.AddSingleton<ViewModels.MainViewModel>();

        return services;
    }
}
