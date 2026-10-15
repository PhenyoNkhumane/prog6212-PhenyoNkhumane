using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public static class TestDbFactory
{
    public static ApplicationDbContext Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection, contextOwnsConnection: true)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
