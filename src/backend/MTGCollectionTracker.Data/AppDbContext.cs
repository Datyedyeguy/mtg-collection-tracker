using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MTGCollectionTracker.Data.Entities;

namespace MTGCollectionTracker.Data;

/// <summary>
/// The main database context for the MTG Collection Tracker application.
/// </summary>
/// <remarks>
/// Inherits from IdentityDbContext which provides:
/// - Users table (AspNetUsers)
/// - Roles table (AspNetRoles)
/// - UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims tables
///
/// We specify our custom ApplicationUser as the user type.
/// The other Identity types (IdentityRole, etc.) use defaults.
/// </remarks>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configure entity relationships, indexes, and constraints.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Must call base.OnModelCreating for Identity tables to be configured
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            // Add index on DisplayName for potential future searches
            entity.HasIndex(u => u.DisplayName);

            // Ensure CreatedAt has a default value at the database level
            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // Future entities (Cards, Collections, Decklists) will be configured here
        // as we build out those features
    }
}
