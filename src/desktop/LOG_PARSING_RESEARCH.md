# MTGA Log Parsing - ToS-Compliant Approach

**Status**: 🔬 Research Phase - **PREFERRED METHOD**  
**Risk Level**: ✅ LOW - Explicitly authorized by Wizards via "Detailed Logs (Plugin Support)" setting  

---

## Why Log Parsing is ToS-Compliant

### Evidence of Authorization

1. **"Detailed Logs (Plugin Support)" Setting**
   - MTGA includes an option to enable detailed logging explicitly for plugin support
   - This demonstrates Wizards' **explicit authorization** for third-party tools
   - Found in: Settings → Account → Detailed Logs

2. **ToS Section 2.2(iii)(d) Exception**
   - Prohibits: "third-party programs or tools **not expressly authorized** by Wizards"
   - The existence of "Plugin Support" logs = express authorization
   - We're not modifying the game, just reading files it writes

3. **Industry Precedent**
   - 17Lands.com - Uses MTGA logs for draft analytics (operated for years)
   - MTGA Assistant - Log-based tracker (available on GitHub)
   - Untapped.gg - Started with log parsing before moving to memory scanning

### Comparison to Other Methods

| Method | ToS Compliance | Complexity | Reliability |
|--------|---------------|------------|-------------|
| **Log Parsing** | ✅ Authorized | Low | High |
| Memory Scanning | ❌ Unauthorized | High | Medium |
| DLL Injection | ❌ Unauthorized | Medium | Medium |
| Screen Scraping | ❌ Unauthorized | High | Low |

---

## MTGA Log File Locations

### Windows
```
%APPDATA%\..\LocalLow\Wizards Of The Coast\MTGA\Player.log
%APPDATA%\..\LocalLow\Wizards Of The Coast\MTGA\Logs\
```

**Typical Full Path:**
```
C:\Users\[USERNAME]\AppData\LocalLow\Wizards Of The Coast\MTGA\Player.log
```

### macOS
```
~/Library/Logs/Wizards Of The Coast/MTGA/Player.log
```

---

## What Data is Available in Logs?

### Confirmed Available (From 17Lands and MTGA Assistant)

1. **Collection Inventory** ✅
   - Card GrpIds and quantities
   - Format: JSON payloads in log entries
   - Triggered by: Opening collection screen, game startup

2. **Deck Lists** ✅
   - Complete deck lists with GrpIds
   - Sideboard information
   - Format information

3. **Match Results** ✅
   - Game outcomes (win/loss)
   - Opponent's deck (cards played)
   - Turn-by-turn plays

4. **Draft Picks** ✅
   - Cards seen in draft packs
   - Cards picked
   - Pack/pick number

5. **Inventory Changes** ✅
   - Cards acquired (packs opened, rewards)
   - Wildcards earned
   - Gold/Gem transactions

### Example Log Entries

**Collection Update:**
```json
[UnityCrossThreadLogger]<== PlayerInventory.GetPlayerCardsV3 
{
  "payload": {
    "InventoryUpdates": {
      "49": 4,  // GrpId 49 (Lightning Bolt) x4
      "1234": 2,
      // ... more cards
    }
  }
}
```

**Deck List:**
```json
[UnityCrossThreadLogger]PlayerInventory.GetDecklists
{
  "name": "Goblins Aggro",
  "mainDeck": [
    {"cardId": 49, "quantity": 4},
    {"cardId": 123, "quantity": 4}
  ],
  "sideboard": [
    {"cardId": 789, "quantity": 3}
  ]
}
```

---

## Implementation Approach

### Architecture

```
MTGALogParser (C# Console/Service)
  ├── FileWatcher
  │   └── Monitors Player.log for changes
  ├── JsonExtractor
  │   └── Parses JSON payloads from log lines
  ├── CollectionTracker
  │   └── Builds collection state from events
  └── ApiClient
      └── Uploads to backend API
```

### How It Works

1. **File Watching**
   - Use `FileSystemWatcher` to monitor Player.log
   - New lines are appended as game runs
   - No need to restart MTGA

2. **Event Detection**
   - Search for specific log markers:
     - `PlayerInventory.GetPlayerCardsV3` - Full collection
     - `Event_SetDeck` - Deck changes
     - `Event_InventoryUpdated` - Card acquisitions

3. **JSON Extraction**
   - Log lines contain JSON payloads
   - Extract and parse JSON
   - Map GrpIds to Scryfall cards

4. **State Building**
   - Maintain current collection state
   - Detect deltas (new cards acquired)
   - Handle edge cases (arena-only cards, Alchemy rebalances)

### Sample Code

```csharp
using System.IO;
using System.Text.Json;

public class MTGALogParser
{
    private readonly string _logPath;
    private FileSystemWatcher _watcher;
    
    public MTGALogParser()
    {
        _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "..",
            "LocalLow",
            "Wizards Of The Coast",
            "MTGA",
            "Player.log"
        );
    }
    
    public void StartWatching()
    {
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_logPath))
        {
            Filter = "Player.log",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        
        _watcher.Changed += OnLogFileChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        // Read new lines from log
        // Parse JSON payloads
        // Extract collection data
    }
    
    public Dictionary<int, int> ParseCollection(string logContent)
    {
        var collection = new Dictionary<int, int>();
        
        // Find collection JSON in logs
        var matches = Regex.Matches(logContent, 
            @"PlayerInventory\.GetPlayerCardsV3.*?\n.*?({.*?})", 
            RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            var json = match.Groups[1].Value;
            var payload = JsonSerializer.Deserialize<InventoryPayload>(json);
            
            foreach (var card in payload.InventoryUpdates)
            {
                collection[card.Key] = card.Value;
            }
        }
        
        return collection;
    }
}
```

---

## Known Tools Using This Approach

### 17Lands (https://www.17lands.com/)
- **Purpose**: Draft analytics and win rate tracking
- **Method**: Reads MTGA logs
- **Status**: Operational since 2020, endorsed by WotC employees on Twitter
- **Data**: Draft picks, match results, deck performance

### MTGA Assistant (GitHub)
- **Purpose**: Collection tracking and draft helper
- **Method**: Log parsing
- **Status**: Open source, actively maintained
- **Repo**: https://github.com/Razviar/mtgap

### Untapped.gg (Original Implementation)
- **Started**: Log parsing approach
- **Migrated**: Later moved to memory scanning for additional features
- **Still Uses Logs**: For match history and some events

---

## Advantages of Log Parsing

### 1. **ToS Compliant** ✅
- Explicitly authorized via "Plugin Support" setting
- No process modification
- No reverse engineering required
- Reading public files the game writes

### 2. **Cross-Platform** ✅
- Works on Windows and macOS
- Same log format on both platforms
- No platform-specific APIs needed

### 3. **Reliable** ✅
- Less fragile than memory scanning
- Doesn't break with every game update
- JSON format is stable

### 4. **No Admin Required** ✅
- Standard file read permissions
- Doesn't require elevated privileges
- Better user experience

### 5. **Safe for Users** ✅
- No account termination risk
- Wizards explicitly allows it
- Used by popular, endorsed tools

---

## Limitations

### 1. **User Must Enable Setting**
- Detailed Logs are not enabled by default
- Users must: Settings → Account → Enable "Detailed Logs (Plugin Support)"
- Solution: Clear instructions in our app

### 2. **Delayed Updates**
- Collection only written to logs when:
  - Opening collection screen
  - Game startup
  - After opening packs/rewards
- Not real-time like memory scanning
- Solution: Instruct users to open collection screen

### 3. **Log File Size**
- Player.log can grow to 100+ MB
- Older entries may be rotated
- Solution: Process incrementally, watch for new lines

### 4. **Arena-Specific GrpIds**
- Logs use Arena's internal GrpIds
- Must map to Scryfall oracle IDs
- Solution: Use Scryfall's arena_id field (existing CardTranslator logic)

---

## Implementation Plan

### Phase 1: Proof of Concept (1 week)

**Goals:**
- [ ] Locate and read Player.log
- [ ] Extract collection JSON from logs
- [ ] Parse GrpIds and quantities
- [ ] Map to Scryfall cards

**Deliverables:**
- Console app that prints collection to terminal
- Validate against known collection (manual count)

### Phase 2: File Watching (1 week)

**Goals:**
- [ ] Implement FileSystemWatcher
- [ ] Detect new log entries in real-time
- [ ] Handle log rotation
- [ ] Maintain collection state

**Deliverables:**
- Service that runs in background
- Detects collection changes automatically

### Phase 3: Desktop Integration (2 weeks)

**Goals:**
- [ ] Integrate into WPF desktop app
- [ ] Add UI for sync status
- [ ] Upload to backend API
- [ ] Handle offline/online scenarios

**Deliverables:**
- Full desktop client with log parsing
- Automatic background sync

### Phase 4: Polish & Launch (1 week)

**Goals:**
- [ ] Add setup wizard (enable detailed logs)
- [ ] Error handling and logging
- [ ] User documentation
- [ ] Beta testing

---

## Testing Strategy

### Test Cases

1. **Fresh Install**
   - Install MTGA, enable detailed logs
   - Parse collection on first run
   - Verify accuracy

2. **Collection Changes**
   - Open packs, earn rewards
   - Verify new cards detected
   - Check delta accuracy

3. **Log Rotation**
   - Test with large log files
   - Verify handling when log is cleared/rotated

4. **Edge Cases**
   - Alchemy rebalanced cards (A-prefix)
   - Historic Anthology sets
   - Arena-exclusive cards

### Manual Validation

1. Export collection via MTGA (if available)
2. Compare with parsed collection
3. Verify 100% accuracy before production

---

## User Instructions (Draft)

### Enable Detailed Logs in MTGA

1. Launch MTG Arena
2. Click the gear icon (Settings)
3. Select "Account" tab
4. Enable "Detailed Logs (Plugin Support)"
5. Click "Done"

**Note:** This setting allows third-party tools like MTG Collection Tracker to read your collection data from log files. This is an officially supported feature by Wizards of the Coast.

---

## Legal Position

### Why This is Safe

1. **Express Authorization**: The "Plugin Support" setting explicitly authorizes this
2. **No Modification**: We only read files, never write or modify
3. **Industry Standard**: 17Lands, MTGA Assistant use this method
4. **WotC Endorsement**: Pro players and WotC employees use 17Lands publicly

### Disclaimer We Should Include

> **Data Source**: MTG Collection Tracker reads data from MTGA log files using the "Detailed Logs (Plugin Support)" feature. This is an officially supported method provided by Wizards of the Coast. We do not modify game files, inject code, or access game memory.

---

## Next Steps

1. **Verify Log Contents** - Install MTGA, enable detailed logs, examine Player.log
2. **Build Parser** - Create C# library to extract collection data
3. **Test Accuracy** - Validate parsed data matches actual collection
4. **Integrate** - Add to WPF desktop client
5. **Document** - Create user guide for enabling detailed logs

---

## Resources

- **17Lands API Documentation**: https://www.17lands.com/getting_started
- **MTGA Log Parser (Python)**: https://github.com/rconroy293/mtga-log-parser
- **MTGA Assistant (C#)**: https://github.com/Razviar/mtgap
- **Arena GrpId Database**: Scryfall has `arena_id` field for mapping

---

**Last Updated**: January 12, 2026  
**Status**: Recommended approach - ToS compliant  
**Estimated Implementation**: 4-5 weeks to production

