using System.Security.Cryptography;
using System.Text;
using BalanceSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.Database;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing database...");

        _db.Orm.CodeFirst.SyncStructure<User>();
        _db.Orm.CodeFirst.SyncStructure<Recipe>();
        _db.Orm.CodeFirst.SyncStructure<TestRecord>();

        if (!_db.Orm.Select<User>().Any())
        {
            _db.Orm.Insert(new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Role = "Admin",
                IsActive = true
            }).ExecuteAffrows();
            _logger.LogInformation("Seeded default admin user");
        }

        // Seed a sample recipe for testing
        if (!_db.Orm.Select<Recipe>().Any())
        {
            _db.Orm.Insert(new Recipe
            {
                Name = "演示转子",
                RatedSpeed = 1500,
                AllowUnbalanceLeft = 1.0,
                AllowUnbalanceRight = 1.0,
                TrialMass1 = 50,
                TrialAngle1 = 0,
                TrialMass2 = 50,
                TrialAngle2 = 0
            }).ExecuteAffrows();
            _logger.LogInformation("Seeded default demo recipe");
        }

        _logger.LogInformation("Database initialization complete");
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
