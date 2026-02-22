using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Data;
using Shouldly;

namespace MTGCollectionTracker.Api.Tests.Infrastructure;

/// <summary>
/// Tests that the EF Core migration snapshot matches the current model.
///
/// Why this matters:
/// When you change an entity (add a property, change a column type, add an index)
/// you MUST also create a new migration with 'dotnet ef migrations add'.
/// If you forget, the app crashes on startup when it detects the mismatch.
///
/// How it works:
/// EF Core maintains a "snapshot" file (AppDbContextModelSnapshot.cs) that records
/// what the database schema looked like after the last migration. When you call
/// HasPendingModelChanges(), EF diffs your current model against that snapshot.
/// No database connection is needed — it's a pure in-memory comparison.
/// </summary>
[TestClass]
public class MigrationTests
{
    [TestMethod]
    [Description("Fails if an entity change is missing a corresponding migration.")]
    public void Model_HasNoPendingModelChanges()
    {
        // Use a fake connection string — HasPendingModelChanges() never opens a connection.
        // It only needs the Npgsql provider registered so it can do relational type mapping
        // when comparing the compiled model against the migration snapshot.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test-model-check")
            .Options;

        using var context = new AppDbContext(options);

        var hasPendingChanges = context.Database.HasPendingModelChanges();

        hasPendingChanges.ShouldBeFalse(
            "The EF Core model has changes that don't have a corresponding migration. " +
            "Run: dotnet ef migrations add <MigrationName> " +
            "--project src/backend/MTGCollectionTracker.Data " +
            "--startup-project src/backend/MTGCollectionTracker.Api");
    }
}
