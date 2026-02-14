// Platform enum is defined in MTGCollectionTracker.Shared.Enums.Platform
// This file re-exports it in the Data.Entities namespace for backward compatibility
// with existing code that references MTGCollectionTracker.Data.Entities.Platform.

namespace MTGCollectionTracker.Data.Entities;

using SharedPlatform = MTGCollectionTracker.Shared.Enums.Platform;

/// <summary>
/// Platform enum - re-exported from Shared project.
/// The actual enum definition is in MTGCollectionTracker.Shared.Enums.Platform.
/// </summary>
public enum Platform
{
    /// <summary>
    /// Physical paper cards.
    /// </summary>
    Paper = SharedPlatform.Paper,

    /// <summary>
    /// MTG Arena digital platform.
    /// </summary>
    Arena = SharedPlatform.Arena,

    /// <summary>
    /// Magic Online digital platform.
    /// </summary>
    Mtgo = SharedPlatform.Mtgo
}
