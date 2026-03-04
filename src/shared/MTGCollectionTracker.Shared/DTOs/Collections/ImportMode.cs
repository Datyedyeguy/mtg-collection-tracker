namespace MTGCollectionTracker.Shared.DTOs.Collections;

/// <summary>
/// Controls how an import merges with the user's existing collection entries.
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Add imported quantities on top of any existing entries.
    /// If the user already owns 2 copies and the import says 3, they end up with 5.
    /// </summary>
    Accumulate,

    /// <summary>
    /// Delete all existing Paper collection entries first, then insert the imported cards fresh.
    /// Use this for a clean re-import from a source of truth export.
    /// </summary>
    Replace
}
