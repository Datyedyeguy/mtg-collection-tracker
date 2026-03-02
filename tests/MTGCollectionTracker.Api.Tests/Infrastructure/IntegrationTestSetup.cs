using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Data;

namespace MTGCollectionTracker.Api.Tests.Infrastructure;

/// <summary>
/// Static factory for creating DbContext instances connected to a test container.
///
/// Integration test classes manage their own container lifecycle via [ClassInitialize] /
/// [ClassCleanup] (not [AssemblyInitialize]) so that Docker unavailability only fails the
/// affected integration test class — leaving all unit tests unaffected.
///
/// Typical usage in an integration test class:
/// <code>
///   private static PostgreSqlContainer _container = null!;
///
///   [ClassInitialize]
///   public static async Task ClassInitialize(TestContext _)
///   {
///       _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
///       await _container.StartAsync();
///       await using var ctx = IntegrationTestSetup.CreateDbContext(_container.GetConnectionString());
///       await ctx.Database.EnsureCreatedAsync();
///   }
///
///   [ClassCleanup]
///   public static async Task ClassCleanup() => await _container.DisposeAsync();
/// </code>
/// </summary>
public static class IntegrationTestSetup
{
    /// <summary>
    /// Creates a new DbContext connected to the provided connection string.
    /// Callers are responsible for disposing it.
    /// </summary>
    public static AppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
