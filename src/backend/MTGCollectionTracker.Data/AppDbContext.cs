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
    /// Refresh tokens for JWT authentication.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        // Configure RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            // Index on TokenHash for fast lookups
            entity.HasIndex(rt => rt.TokenHash);

            // Index on UserId for finding user's tokens
            entity.HasIndex(rt => rt.UserId);

            // Relationship: RefreshToken -> User
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(rt => rt.CreatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // Future entities (Cards, Collections, Decklists) will be configured here
        // as we build out those features
    }
}
