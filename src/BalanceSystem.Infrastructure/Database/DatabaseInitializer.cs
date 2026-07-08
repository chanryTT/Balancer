using System.Security.Cryptography;
using System.Text;
using BalanceSystem.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BalanceSystem.Infrastructure.Database;

public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _connectionString = db.Orm.Ado.ConnectionString;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing database...");

        EnsureDatabaseExists();

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

    private void EnsureDatabaseExists()
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        string targetDb = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using var conn = new SqlConnection(builder.ConnectionString);
        conn.Open();

        var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
        checkCmd.Parameters.AddWithValue("@name", targetDb);
        int exists = (int)checkCmd.ExecuteScalar()!;

        if (exists == 0)
        {
            var createCmd = conn.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE [{targetDb}]";
            createCmd.ExecuteNonQuery();
            _logger.LogInformation("Created database '{DbName}'", targetDb);
        }
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
