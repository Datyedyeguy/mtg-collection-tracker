namespace MTGCollectionTracker.Shared.Enums;

/// <summary>
/// Represents the platform where a Magic: The Gathering card exists.
/// </summary>
/// <remarks>
/// This enum is stored as a string in the database (via EF Core HasConversion)
/// to maintain readability in PostgreSQL.
/// Database values: "Paper", "Arena", "Mtgo"
/// </remarks>
public enum Platform
{
    /// <summary>
    /// Physical paper cards that you can hold.
    /// </summary>
    Paper,

    /// <summary>
    /// Magic: The Gathering Arena (digital platform).
    /// </summary>
    Arena,

    /// <summary>
    /// Magic Online (MTGO) - older digital platform.
    /// </summary>
    Mtgo
}
