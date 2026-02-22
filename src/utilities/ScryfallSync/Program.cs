using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Cards;

namespace ScryfallSync;

/// <summary>
/// Console application to download Scryfall bulk card data and populate the database.
///
/// Usage:
///   ScryfallSync                                    - Sync all cards (default)
///   ScryfallSync --force-refresh                    - Force re-download even if cached
///   ScryfallSync --dry-run                          - Download and parse but don't insert
///   ScryfallSync --connection-string "Host=..."     - Override connection string
/// </summary>
class Program
{
    // Scryfall API requires rate limiting: 10 requests per second (100ms delay)
    // Bulk data downloads are exempt from rate limiting
    private const int ApiDelayMs = 100;

    static async Task<int> Main(string[] args)
    {
        // Define command-line options
        var forceRefreshOption = new Option<bool>(
            aliases: new[] { "--force-refresh", "-f" },
            description: "Force re-download of bulk data even if cached locally");

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-d" },
            description: "Download and parse data but don't insert into database");

        var connectionStringOption = new Option<string?>(
            aliases: new[] { "--connection-string", "-c" },
            description: "PostgreSQL connection string (overrides appsettings.json)");

        var bulkTypeOption = new Option<string>(
            aliases: new[] { "--bulk-type", "-t" },
            getDefaultValue: () => "default_cards",
            description: "Bulk data type: default_cards, oracle_cards, unique_artwork, all_cards, rulings");

        var listTypesOption = new Option<bool>(
            aliases: new[] { "--list-types", "-l" },
            description: "List available bulk data types and exit");

        var rootCommand = new RootCommand("Download Scryfall bulk card data and sync to database")
        {
            forceRefreshOption,
            dryRunOption,
            connectionStringOption,
            bulkTypeOption,
            listTypesOption
        };

        rootCommand.SetHandler(async (forceRefresh, dryRun, connectionString, bulkType, listTypes) =>
        {
            try
            {
                if (listTypes)
                {
                    await ListBulkDataTypes();
                    return;
                }

                await RunSync(forceRefresh, dryRun, connectionString, bulkType);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }, forceRefreshOption, dryRunOption, connectionStringOption, bulkTypeOption, listTypesOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunSync(bool forceRefresh, bool dryRun, string? connectionStringOverride, string bulkType)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           MTG Collection Tracker - Scryfall Sync              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 1. Get connection string
        var connectionString = connectionStringOverride ?? GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string provided. Use --connection-string or set in appsettings.json");
        }

        // 2. Get bulk data (download or use cache)
        Console.WriteLine($"📥 Getting Scryfall bulk data ({bulkType})...");
        var bulkDataInfo = await GetBulkDataDownloadUrl(bulkType);
        Console.WriteLine($"   Type: {bulkDataInfo.Type}");
        Console.WriteLine($"   Description: {bulkDataInfo.Description}");
        Console.WriteLine($"   Updated: {bulkDataInfo.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"   Size: {bulkDataInfo.Size / (1024.0 * 1024.0):F1} MB");
        Console.WriteLine($"   URL: {bulkDataInfo.DownloadUri}");

        var jsonData = await DownloadBulkData(bulkDataInfo.DownloadUri, bulkType, forceRefresh);
        Console.WriteLine();

        // 3. Parse JSON
        Console.WriteLine("🔍 Parsing card data...");
        var cards = ParseCards(jsonData);
        Console.WriteLine($"   Total cards: {cards.Count:N0}");
        Console.WriteLine();

        // 4. Insert into database
        if (dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  DRY RUN MODE - Skipping database insert");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("💾 Syncing to database...");
            await SyncToDatabase(connectionString, cards);
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Sync complete!");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("📊 Data provided by Scryfall (https://scryfall.com)");
    }

    static string? GetConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("DefaultConnection");
    }

    static async Task ListBulkDataTypes()
    {
        Console.WriteLine("Available Scryfall bulk data types:");
        Console.WriteLine();

        using var client = CreateHttpClient();
        var response = await client.GetStringAsync("https://api.scryfall.com/bulk-data");
        await Task.Delay(ApiDelayMs); // Rate limiting

        using var doc = JsonDocument.Parse(response);
        var data = doc.RootElement.GetProperty("data");

        foreach (var item in data.EnumerateArray())
        {
            var type = item.GetProperty("type").GetString();
            var name = item.GetProperty("name").GetString();
            var description = item.GetProperty("description").GetString();
            var sizeMB = item.GetProperty("size").GetInt64() / (1024.0 * 1024.0);
            var updatedAt = item.GetProperty("updated_at").GetDateTime();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {type}");
            Console.ResetColor();
            Console.WriteLine($"    Name: {name}");
            Console.WriteLine($"    Description: {description}");
            Console.WriteLine($"    Size: {sizeMB:F1} MB");
            Console.WriteLine($"    Updated: {updatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();
        }
    }

    static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();

        // Required by Scryfall API documentation
        client.DefaultRequestHeaders.Add("User-Agent", "MTGCollectionTracker/1.0 (https://github.com/datyedyeguy/mtg-collection-tracker)");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }

    static async Task<BulkDataInfo> GetBulkDataDownloadUrl(string bulkType)
    {
        using var client = CreateHttpClient();
        var response = await client.GetStringAsync("https://api.scryfall.com/bulk-data");
        await Task.Delay(ApiDelayMs); // Rate limiting

        using var doc = JsonDocument.Parse(response);
        var data = doc.RootElement.GetProperty("data");

        foreach (var item in data.EnumerateArray())
        {
            var type = item.GetProperty("type").GetString();
            if (type == bulkType)
            {
                return new BulkDataInfo
                {
                    Type = type ?? bulkType,
                    Name = item.GetProperty("name").GetString() ?? string.Empty,
                    Description = item.GetProperty("description").GetString() ?? string.Empty,
                    DownloadUri = item.GetProperty("download_uri").GetString() ?? throw new InvalidOperationException($"No download URI for {bulkType}"),
                    UpdatedAt = item.GetProperty("updated_at").GetDateTime(),
                    Size = item.GetProperty("size").GetInt64()
                };
            }
        }

        throw new InvalidOperationException($"Bulk data type '{bulkType}' not found. Use --list-types to see available types.");
    }

    record BulkDataInfo
    {
        public string Type { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string DownloadUri { get; init; } = string.Empty;
        public DateTime UpdatedAt { get; init; }
        public long Size { get; init; }
    }

    static async Task<string> DownloadBulkData(string url, string bulkType, bool forceRefresh)
    {
        // Use local data directory for persistent caching (not system temp)
        var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(cacheDir);
        var cacheFile = Path.Combine(cacheDir, $"scryfall-{bulkType}.json");

        // Use cached file if it exists (no time limit - user controls with --force-refresh)
        if (!forceRefresh && File.Exists(cacheFile))
        {
            var fileInfo = new FileInfo(cacheFile);
            var fileAge = DateTime.Now - fileInfo.LastWriteTime;
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   Using cached data from: {cacheFile}");
            Console.WriteLine($"   Cache age: {fileAge.TotalDays:F1} days ({fileAge.TotalHours:F1} hours)");
            Console.WriteLine($"   File size: {fileSizeMB:F1} MB");
            Console.ResetColor();
            Console.WriteLine($"   Tip: Use --force-refresh to download latest data");

            return await File.ReadAllTextAsync(cacheFile);
        }

        using var client = CreateHttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        Console.WriteLine($"   Downloading to: {cacheFile}");
        Console.WriteLine("   This may take several minutes depending on connection speed...");
        Console.Write("   Progress: ");

        // Download with progress reporting
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[8192];
        var totalRead = 0L;
        var lastPercent = 0;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(cacheFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)((totalRead * 100) / totalBytes);
                if (percent != lastPercent && percent % 5 == 0) // Update every 5%
                {
                    Console.Write($"{percent}% ");
                    lastPercent = percent;
                }
            }
        }

        Console.WriteLine("100%");
        Console.WriteLine($"   Saved {totalRead / (1024.0 * 1024.0):F1} MB to cache");

        // Read the file back for parsing
        return await File.ReadAllTextAsync(cacheFile);
    }

    static List<Card> ParseCards(string jsonData)
    {
        using var doc = JsonDocument.Parse(jsonData);
        var cards = new List<Card>();
        var skippedCards = new List<(string Name, string ScryfallId, string Reason, JsonElement Element)>();
        var array = doc.RootElement;

        int processed = 0;
        int total = array.GetArrayLength();

        foreach (var element in array.EnumerateArray())
        {
            processed++;
            if (processed % 5000 == 0) // Update every 5,000 cards
            {
                var percent = (100 * processed / total);
                Console.Write($"\r   Progress: {processed:N0} / {total:N0} ({percent}%)    ");
                Console.Out.Flush(); // Force console update
            }

            try
            {
                // Include everything - tokens, emblems, art cards, etc.
                // People collect all of these, especially in paper

                // === ORACLE ID EXTRACTION ===
                // Oracle ID is required by our database schema (NOT NULL constraint).
                // It links all printings of the same card together.
                //
                // SCRYFALL STRUCTURE:
                // - Most cards: oracle_id is at the top level
                //   Example: { "oracle_id": "abc123", "name": "Lightning Bolt", ... }
                //
                // - Multi-faced cards (transform, modal, split, etc.): oracle_id is at top level
                //   Example: { "oracle_id": "abc123", "card_faces": [...], ... }
                //
                // - Reversible cards (special Secret Lair promos): oracle_id ONLY in card_faces
                //   Example: { "card_faces": [{ "oracle_id": "abc123", ... }, ...], ... }
                //   These are showcase treatments where both sides show the same card with different art
                //
                // Strategy: Check top level first, fall back to card_faces[0] if not found

                Guid oracleId;
                if (element.TryGetProperty("oracle_id", out var oracleIdProp))
                {
                    // Found at top level (99%+ of cards take this path)
                    oracleId = oracleIdProp.GetGuid();
                }
                else if (element.TryGetProperty("card_faces", out var faces) && faces.GetArrayLength() > 0)
                {
                    // Top level oracle_id missing, check first card face
                    // This handles reversible_card layout and other edge cases
                    var firstFace = faces[0];
                    if (firstFace.TryGetProperty("oracle_id", out var faceOracleId))
                    {
                        oracleId = faceOracleId.GetGuid();
                    }
                    else
                    {
                        // Card has faces but no oracle_id anywhere - very rare edge case
                        var name = element.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                        var id = element.TryGetProperty("id", out var i) ? i.GetString() : "Unknown";
                        var layout = element.TryGetProperty("layout", out var l) ? l.GetString() : "Unknown";
                        skippedCards.Add((name ?? "Unknown", id ?? "Unknown", $"No oracle_id in card or faces (layout: {layout})", element.Clone()));
                        continue;
                    }
                }
                else
                {
                    // No oracle_id and no card_faces - likely a malformed entry
                    var name = element.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                    var id = element.TryGetProperty("id", out var i) ? i.GetString() : "Unknown";
                    var layout = element.TryGetProperty("layout", out var l) ? l.GetString() : "Unknown";
                    skippedCards.Add((name ?? "Unknown", id ?? "Unknown", $"Missing oracle_id and card_faces (layout: {layout})", element.Clone()));
                    continue;
                }

                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    ScryfallId = element.GetProperty("id").GetGuid(),
                    OracleId = oracleId,

                    // These fields are always at top level (guaranteed by Scryfall API)
                    Name = element.GetProperty("name").GetString() ?? string.Empty,
                    FlavorName = element.TryGetProperty("flavor_name", out var flavorName)
                        ? flavorName.GetString()
                        : null,
                    SetCode = element.GetProperty("set").GetString() ?? string.Empty,
                    CollectorNumber = element.GetProperty("collector_number").GetString() ?? string.Empty,
                    Rarity = element.GetProperty("rarity").GetString() ?? string.Empty,

                    // === GAMEPLAY FIELDS WITH FALLBACK ===
                    // These fields may be at the top level OR in card_faces[0]:
                    // - Single-faced cards: Top level (e.g., Llanowar Elves)
                    // - Transform cards: Top level usually exists (aggregate data)
                    // - Modal DFCs: Top level usually exists
                    // - Reversible cards: ONLY in card_faces[0]
                    //
                    // TryGetPropertyWithFallback checks top level first, then card_faces[0]
                    ManaCost = TryGetPropertyWithFallback(element, "mana_cost")?.GetString(),
                    Cmc = TryGetPropertyWithFallback(element, "cmc")?.GetDecimal() ?? 0,
                    TypeLine = TryGetPropertyWithFallback(element, "type_line")?.GetString() ?? string.Empty,
                    OracleText = TryGetPropertyWithFallback(element, "oracle_text")?.GetString(),
                    Power = TryGetPropertyWithFallback(element, "power")?.GetString(),
                    Toughness = TryGetPropertyWithFallback(element, "toughness")?.GetString(),

                    // Colors: Prefer "colors" (actual mana colors), fallback to "color_identity"
                    // color_identity includes colors from mana symbols in text (e.g., Kenrith)
                    Colors = element.TryGetProperty("colors", out var colors)
                        ? JsonSerializer.Serialize(colors)
                        : (element.TryGetProperty("color_identity", out var colorId)
                            ? JsonSerializer.Serialize(colorId)
                            : null),

                    // Image URIs: Single-faced cards have top-level, multi-faced in card_faces
                    ImageUris = TryGetImageUris(element),

                    // Legalities and platform IDs always at top level
                    Legalities = element.TryGetProperty("legalities", out var legal)
                        ? JsonSerializer.Serialize(legal)
                        : null,
                    ArenaId = element.TryGetProperty("arena_id", out var arenaId)
                        ? arenaId.GetInt32()
                        : null,
                    MtgoId = element.TryGetProperty("mtgo_id", out var mtgoId)
                        ? mtgoId.GetInt32()
                        : null,

                    // Finishes: Array of available finishes (nonfoil, foil, etched)
                    Finishes = element.TryGetProperty("finishes", out var finishes)
                        ? JsonSerializer.Serialize(finishes)
                        : null,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    // Parse and transform card_faces into our simplified CardFaceDto structure
                    // Only populated for multi-faced cards (transform, modal, reversible, etc.)
                    CardFaces = ParseCardFaces(element)
                };

                cards.Add(card);
            }
            catch (Exception ex)
            {
                var cardName = element.TryGetProperty("name", out var name) ? name.GetString() : "Unknown";
                var scryfallId = element.TryGetProperty("id", out var id) ? id.GetString() : "Unknown";
                skippedCards.Add((cardName ?? "Unknown", scryfallId ?? "Unknown", $"Parse error: {ex.Message}", element.Clone()));
                continue;
            }
        }

        Console.WriteLine();

        // Display skipped cards if any
        if (skippedCards.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Skipped {skippedCards.Count} card(s) due to missing/invalid data:");
            Console.ResetColor();

            foreach (var (name, id, reason, _) in skippedCards.Take(10)) // Show first 10 summary
            {
                Console.WriteLine($"   - {name} [{id}]: {reason}");
            }

            if (skippedCards.Count > 10)
            {
                Console.WriteLine($"   ... and {skippedCards.Count - 10} more");
            }

            // Show full JSON for first 3 cards to analyze
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("📋 Sample card entries (first 3 for analysis):");
            Console.ResetColor();

            foreach (var (name, id, reason, element) in skippedCards.Take(3))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Card: {name} [{id}]");
                Console.ResetColor();
                Console.WriteLine(JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = true }));
            }

            Console.WriteLine();
        }

        return cards;
    }

    static async Task SyncToDatabase(string connectionString, List<Card> cards)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        Console.WriteLine("   Checking database connection...");
        await context.Database.EnsureCreatedAsync();

        Console.WriteLine("   Loading existing cards...");
        var existingCardData = await context.Cards
            .Select(c => new { c.ScryfallId, c.SetCode, c.CollectorNumber, c.Id, c.CreatedAt })
            .ToListAsync();

        // Primary lookup: by ScryfallId (fast path, covers 99%+ of cards)
        var existingByScryfallId = existingCardData.ToDictionary(c => c.ScryfallId);

        // Secondary lookup: by SetCode+CollectorNumber — handles cases where Scryfall
        // rotates a card's UUID (the card is the same printing but gets a new ScryfallId).
        // Without this, these cards would try to INSERT and hit the unique constraint.
        var existingBySetAndNumber = existingCardData.ToDictionary(c => $"{c.SetCode}|{c.CollectorNumber}");

        Console.WriteLine($"   Existing cards in database: {existingCardData.Count:N0}");

        var toInsert = new List<Card>();
        var toUpdate = new List<Card>();

        foreach (var card in cards)
        {
            if (existingByScryfallId.TryGetValue(card.ScryfallId, out var existing))
            {
                // ScryfallId match — standard update path
                card.Id = existing.Id;
                card.CreatedAt = existing.CreatedAt;
                toUpdate.Add(card);
            }
            else if (existingBySetAndNumber.TryGetValue($"{card.SetCode}|{card.CollectorNumber}", out var existingByKey))
            {
                // Same set+number, different ScryfallId — Scryfall rotated the card's UUID.
                // Treat as an update so we don't violate the unique constraint.
                card.Id = existingByKey.Id;
                card.CreatedAt = existingByKey.CreatedAt;
                toUpdate.Add(card);
            }
            else
            {
                // Genuinely new card
                toInsert.Add(card);
            }
        }

        Console.WriteLine($"   Cards to insert: {toInsert.Count:N0}");
        Console.WriteLine($"   Cards to update: {toUpdate.Count:N0}");

        // Insert new cards in batches
        if (toInsert.Count > 0)
        {
            Console.Write("   Inserting new cards: ");
            int batchSize = 1000;
            for (int i = 0; i < toInsert.Count; i += batchSize)
            {
                var batch = toInsert.Skip(i).Take(batchSize).ToList();
                await context.Cards.AddRangeAsync(batch);
                try
                {
                    await context.SaveChangesAsync();
                    Console.Write($"{Math.Min(i + batchSize, toInsert.Count):N0}... ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"   ❌ Error inserting batch {i}-{i + batch.Count}:");
                    Console.WriteLine($"   {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                    }
                    Console.ResetColor();
                    throw;
                }
            }
            Console.WriteLine("Done!");
        }

        // Update existing cards in batches
        if (toUpdate.Count > 0)
        {
            Console.Write("   Updating existing cards: ");
            int batchSize = 1000;
            for (int i = 0; i < toUpdate.Count; i += batchSize)
            {
                var batch = toUpdate.Skip(i).Take(batchSize).ToList();
                context.Cards.UpdateRange(batch);
                await context.SaveChangesAsync();
                Console.Write($"{Math.Min(i + batchSize, toUpdate.Count):N0}... ");
            }
            Console.WriteLine("Done!");
        }

        // Display statistics
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📊 Database Statistics:");
        Console.ResetColor();

        var totalCards = await context.Cards.CountAsync();
        var uniqueOracles = await context.Cards.Select(c => c.OracleId).Distinct().CountAsync();
        var multiFacedCards = await context.Cards.CountAsync(c => c.Faces != null);
        var cardsWithFinishes = await context.Cards.CountAsync(c => c.Finishes != null);
        var arenaCards = await context.Cards.CountAsync(c => c.ArenaId != null);
        var mtgoCards = await context.Cards.CountAsync(c => c.MtgoId != null);

        Console.WriteLine($"   Total cards: {totalCards:N0}");
        Console.WriteLine($"   Unique Oracle IDs: {uniqueOracles:N0}");
        Console.WriteLine($"   Multi-faced cards: {multiFacedCards:N0}");
        Console.WriteLine($"   Cards with finishes data: {cardsWithFinishes:N0}");
        Console.WriteLine($"   Arena cards: {arenaCards:N0}");
        Console.WriteLine($"   MTGO cards: {mtgoCards:N0}");
    }

    /// <summary>
    /// Try to get a property from the element, falling back to card_faces[0] if not found.
    ///
    /// WHY THIS IS NEEDED:
    /// Scryfall's JSON structure varies by card layout:
    ///
    /// 1. Single-faced cards (normal, split, flip, etc.):
    ///    - All properties at top level
    ///    - Example: {"name": "Lightning Bolt", "mana_cost": "{R}", ...}
    ///
    /// 2. Most transform/modal cards (transform, modal_dfc):
    ///    - Properties at BOTH top level (aggregate) AND in card_faces
    ///    - Example: {"name": "Front // Back", "mana_cost": "{1}{U}", card_faces: [{...}, {...}]}
    ///    - Top level has front face data or combined data
    ///
    /// 3. Reversible cards (reversible_card layout):
    ///    - Properties ONLY in card_faces, NOT at top level
    ///    - Example: {"name": "Card // Card", card_faces: [{"mana_cost": "{R}"}, {...}]}
    ///    - These are Secret Lair promos with same card, different art on each side
    ///
    /// This method checks top level first (fast path for 99% of cards), then falls back
    /// to card_faces[0] to handle reversible cards and ensure we capture all data.
    /// </summary>
    static JsonElement? TryGetPropertyWithFallback(JsonElement element, string propertyName)
    {
        // Fast path: Check top level first (works for most cards)
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop;
        }

        // Slow path: Check first card face (needed for reversible_card layout)
        if (element.TryGetProperty("card_faces", out var faces) && faces.GetArrayLength() > 0)
        {
            var firstFace = faces[0];
            if (firstFace.TryGetProperty(propertyName, out var faceProp))
            {
                return faceProp;
            }
        }

        // Property doesn't exist anywhere (e.g., lands don't have power/toughness)

        return null;
    }

    /// <summary>
    /// Try to get image URIs, preferring top-level but falling back to card_faces[0].
    ///
    /// WHY THIS IS NEEDED:
    /// - Single-faced cards: image_uris at top level
    ///   Example: {"image_uris": {"small": "...", "normal": "...", ...}}
    ///
    /// - Multi-faced cards: image_uris in card_faces[0], NOT at top level
    ///   Example: {"card_faces": [{"image_uris": {...}}, {"image_uris": {...}}]}
    ///
    /// We store the front face image for display in collection views.
    /// The image_uris object contains URLs to Scryfall's CDN for different sizes:
    /// small (146x204), normal (488x680), large (672x936), png (745x1040), etc.
    /// </summary>
    static string? TryGetImageUris(JsonElement element)
    {
        // Single-faced cards have top-level image_uris
        if (element.TryGetProperty("image_uris", out var imgUris))
        {
            return JsonSerializer.Serialize(imgUris);
        }

        // Multi-faced cards have image_uris in each face - use front face
        if (element.TryGetProperty("card_faces", out var faces) && faces.GetArrayLength() > 0)
        {
            var firstFace = faces[0];
            if (firstFace.TryGetProperty("image_uris", out var faceImgUris))
            {
                return JsonSerializer.Serialize(faceImgUris);
            }
        }

        return null;
    }

    /// <summary>
    /// Parse Scryfall's card_faces array into our simplified CardFaceDto structure.
    /// Returns null for single-faced cards (no card_faces array).
    ///
    /// TRANSFORMATION STRATEGY:
    /// We extract only the data we need from Scryfall's verbose card_faces structure.
    /// This decouples us from Scryfall's schema changes and reduces storage.
    ///
    /// Example: Transform cards like "Delver of Secrets // Insectile Aberration"
    /// have 2 faces, modal DFCs have 2 faces, reversible cards have 2+ faces.
    /// </summary>
    static List<CardFaceDto>? ParseCardFaces(JsonElement element)
    {
        // Single-faced cards don't have card_faces array
        if (!element.TryGetProperty("card_faces", out var facesArray) || facesArray.GetArrayLength() == 0)
        {
            return null;
        }

        var faces = new List<CardFaceDto>();

        foreach (var faceElement in facesArray.EnumerateArray())
        {
            // Extract image URI (prefer "normal" size, fallback to first available)
            string? imageUri = null;
            if (faceElement.TryGetProperty("image_uris", out var imageUris))
            {
                if (imageUris.TryGetProperty("normal", out var normalImage))
                {
                    imageUri = normalImage.GetString();
                }
                else if (imageUris.TryGetProperty("large", out var largeImage))
                {
                    imageUri = largeImage.GetString();
                }
                else if (imageUris.TryGetProperty("small", out var smallImage))
                {
                    imageUri = smallImage.GetString();
                }
            }

            // Skip faces without required data (name, type_line, image)
            if (!faceElement.TryGetProperty("name", out var nameElement) ||
                !faceElement.TryGetProperty("type_line", out var typeLineElement) ||
                string.IsNullOrEmpty(imageUri))
            {
                continue;
            }

            // Extract colors array
            string[]? colors = null;
            if (faceElement.TryGetProperty("colors", out var colorsElement) && colorsElement.ValueKind == JsonValueKind.Array)
            {
                colors = colorsElement.EnumerateArray()
                    .Select(c => c.GetString())
                    .Where(c => c != null)
                    .ToArray()!;
            }

            var face = new CardFaceDto
            {
                Name = nameElement.GetString() ?? string.Empty,
                ManaCost = faceElement.TryGetProperty("mana_cost", out var mc) ? mc.GetString() : null,
                TypeLine = typeLineElement.GetString() ?? string.Empty,
                OracleText = faceElement.TryGetProperty("oracle_text", out var ot) ? ot.GetString() : null,
                Power = faceElement.TryGetProperty("power", out var pow) ? pow.GetString() : null,
                Toughness = faceElement.TryGetProperty("toughness", out var tou) ? tou.GetString() : null,
                ImageUri = imageUri,
                Colors = colors
            };

            faces.Add(face);
        }

        // Return null if no valid faces were parsed (instead of empty list)
        return faces.Count > 0 ? faces : null;
    }
}
