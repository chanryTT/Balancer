using FreeSql;
using BalanceSystem.Core.Models;

namespace BalanceSystem.Infrastructure.Database;

public class AppDbContext
{
    public IFreeSql Orm { get; set; }
    public string ConnectionString { get; }

    public AppDbContext(string connectionString)
    {
        ConnectionString = connectionString;
        Orm = new FreeSqlBuilder()
            .UseConnectionString(DataType.SqlServer, connectionString)
            .UseAutoSyncStructure(true)
            .UseMonitorCommand(cmd => System.Diagnostics.Debug.WriteLine(cmd.CommandText))
            .Build();
    }
}
