using FreeSql;
using BalanceSystem.Core.Models;

namespace BalanceSystem.Infrastructure.Database;

public class AppDbContext
{
    public IFreeSql Orm { get; }

    public AppDbContext(string connectionString)
    {
        Orm = new FreeSqlBuilder()
            .UseConnectionString(DataType.SqlServer, connectionString)
            .UseAutoSyncStructure(true)
            .UseMonitorCommand(cmd => System.Diagnostics.Debug.WriteLine(cmd.CommandText))
            .Build();
    }
}
