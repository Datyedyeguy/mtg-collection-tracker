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
    /// Card metadata synced from Scryfall.
    /// </summary>
    public DbSet<Card> Cards => Set<Card>();

    /// <summary>
    /// User collection entries (cards owned by users).
    /// </summary>
    public DbSet<CollectionEntry> CollectionEntries => Set<CollectionEntry>();

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

        // Configure Card
        builder.Entity<Card>(entity =>
        {
            entity.HasKey(c => c.Id);

            // Scryfall ID must be unique
            entity.HasIndex(c => c.ScryfallId).IsUnique();

            // Index on OracleId for finding all printings of a card
            entity.HasIndex(c => c.OracleId);

            // Index on card name for search
            entity.HasIndex(c => c.Name);

            // Composite index on set + collector number (unique constraint)
            entity.HasIndex(c => new { c.SetCode, c.CollectorNumber }).IsUnique();

            // Index on Arena ID for Arena imports
            entity.HasIndex(c => c.ArenaId).HasFilter("\"ArenaId\" IS NOT NULL");

            // Index on MTGO ID for MTGO imports
            entity.HasIndex(c => c.MtgoId).HasFilter("\"MtgoId\" IS NOT NULL");

            // Configure JSONB columns (stored as string in C# but jsonb in PostgreSQL)
            entity.Property(c => c.Colors).HasColumnType("jsonb");
            entity.Property(c => c.ImageUris).HasColumnType("jsonb");
            entity.Property(c => c.Legalities).HasColumnType("jsonb");
            entity.Property(c => c.Faces).HasColumnType("jsonb");
            entity.Property(c => c.Finishes).HasColumnType("jsonb");

            // Store Platform enum as string in database
            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(c => c.UpdatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // Configure CollectionEntry
        builder.Entity<CollectionEntry>(entity =>
        {
            entity.HasKey(ce => ce.Id);

            // Index on UserId for finding user's collection
            entity.HasIndex(ce => ce.UserId);

            // Index on Platform for filtering by platform
            entity.HasIndex(ce => ce.Platform);

            // Composite index for common query patterns (user + card + platform)
            entity.HasIndex(ce => new { ce.UserId, ce.CardId, ce.Platform });

            // Composite index for user + platform queries (optimizes filtered collection views)
            entity.HasIndex(ce => new { ce.UserId, ce.Platform });

            // Store Platform enum as string in database
            entity.Property(ce => ce.Platform)
                .HasConversion<string>();

            // Relationship: CollectionEntry -> User
            entity.HasOne(ce => ce.User)
                .WithMany()
                .HasForeignKey(ce => ce.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: CollectionEntry -> Card
            entity.HasOne(ce => ce.Card)
                .WithMany(c => c.CollectionEntries)
                .HasForeignKey(ce => ce.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ce => ce.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(ce => ce.UpdatedAt)
                .HasDefaultValueSql("NOW()");
        });

        // Future entities (Decklists, etc.) will be configured here
    }
}
