using MBappe.Data;
using Microsoft.EntityFrameworkCore;

namespace MBappe.Tests.TestInfrastructure;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly string _databasePath;

    public SqliteTestDatabase()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"mbappe-tests-{Guid.NewGuid():N}.db");

        using var db = CreateContext();

        db.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }
        catch
        {
            // Ошибка удаления временной базы не должна ломать результат тестов.
        }
    }
}