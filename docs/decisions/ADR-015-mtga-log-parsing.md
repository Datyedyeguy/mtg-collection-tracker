# ADR-015: MTGA Integration via Log Parsing

**Date**: January 13, 2026
**Status**: Accepted

## Context

Need to sync MTGA collection to backend. MTGA doesn't have official API. Options:

1. **Log file parsing** (authorized via "Detailed Logs" setting)
2. **Memory reading/DLL injection** (risky, ToS gray area)
3. **Screen scraping/OCR** (unreliable)
4. **Manual export** (MTGA has no export feature)

## Decision

Use log file parsing with FileSystemWatcher, reading from Player.log file when user enables "Detailed Logs (Plugin Support)" setting.

## Consequences

**Pros**:

- **✅ ToS Compliant**: Explicitly authorized by MTGA's "Detailed Logs (Plugin Support)" setting
- **Safe for Users**: No account termination risk (17Lands, MTGA Assistant use this for years)
- **Cross-Platform**: Works on Windows and macOS (same log format)
- **No Admin Rights**: Standard file read permissions
- **Stable Format**: JSON payloads in logs rarely change
- **Real-Time Updates**: FileSystemWatcher for live monitoring
- **Industry Standard**: Proven approach used by 17Lands (500k+ users)

**Cons**:

- **User Must Enable**: Requires toggling "Detailed Logs (Plugin Support)" in MTGA options
- **Log File Location**: Must detect MTGA install directory (varies by user)
- **Parsing Complexity**: Extract JSON from mixed log output
- **Initial Collection**: Full collection only available after opening collection screen in MTGA
- **No Historical Data**: Can't see what you owned last month

**Implementation Strategy**:

```csharp
// 1. FileSystemWatcher on Player.log
// 2. Parse lines like: [UnityCrossThreadLogger]PlayerInventory.GetPlayerCardsV3
// 3. Extract JSON: {"InventoryInfo":[{"grpId":12345,"quantity":4},...]}
// 4. Map arena_id (grpId) to Scryfall cards via lookup table
// 5. POST to API: /api/collections/sync with diff
```

**Log Locations**:

- Windows: `%APPDATA%\..\LocalLow\Wizards Of The Coast\MTGA\Player.log`
- macOS: `~/Library/Logs/Wizards Of The Coast/MTGA/Player.log`

**Desktop Client Workflow**:

1. Detect MTGA installation (check common paths)
2. Check if "Detailed Logs" enabled (parse settings or instruct user)
3. Monitor Player.log with FileSystemWatcher
4. Parse collection updates from JSON payloads
5. Calculate diff from last known state
6. Upload changes to API endpoint

**Why not memory reading?**

- Gray area in ToS, potential ban risk
- Requires admin rights, triggers antivirus
- Breaks with every MTGA update
- Not cross-platform (Windows-only techniques)
