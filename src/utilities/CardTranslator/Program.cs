using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length == 0)
{
    Console.WriteLine("MTG Arena Collection Translator");
    Console.WriteLine("================================");
    Console.WriteLine();
    Console.WriteLine("Usage: CardTranslator <input.csv> [output.csv]");
    Console.WriteLine();
    Console.WriteLine("Converts Arena GrpId collection CSV to MoxField-compatible format.");
    Console.WriteLine("Input CSV format: GrpId,Count");
    Console.WriteLine("Output CSV format: Count,Name,Edition,Collector Number");
    return;
}

string inputFile = args[0];
string outputFile = args.Length > 1 ? args[1] : Path.ChangeExtension(inputFile, ".moxfield.csv");

if (!File.Exists(inputFile))
{
    Console.WriteLine($"ERROR: Input file not found: {inputFile}");
    return;
}

var startTime = DateTime.Now;

Console.WriteLine($"Reading collection from: {inputFile}");
var lines = File.ReadAllLines(inputFile);

if (lines.Length < 2)
{
    Console.WriteLine("ERROR: Input file is empty or has no data rows");
    return;
}

// Parse input CSV (skip header)
var collection = new List<(int grpId, int count)>();
for (int i = 1; i < lines.Length; i++)
{
    var parts = lines[i].Split(',');
    if (parts.Length >= 2 && int.TryParse(parts[0], out int grpId) && int.TryParse(parts[1], out int count))
    {
        collection.Add((grpId, count));
    }
}

Console.WriteLine($"Found {collection.Count} cards in collection");
Console.WriteLine();

// Download and cache Scryfall bulk data
var cacheDir = Path.Combine(Path.GetTempPath(), "MTGArenaCollectionExporter");
Directory.CreateDirectory(cacheDir);
var cacheFile = Path.Combine(cacheDir, "scryfall-default-cards.json");

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "MTGArenaCollectionExporter/1.0");

// Check if cache is less than 24 hours old
bool needsDownload = true;
if (File.Exists(cacheFile))
{
    var cacheAge = DateTime.Now - File.GetLastWriteTime(cacheFile);
    if (cacheAge.TotalHours < 24)
    {
        Console.WriteLine($"Using cached Scryfall data (age: {cacheAge.TotalHours:F1} hours)");
        needsDownload = false;
    }
    else
    {
        Console.WriteLine($"Cache is {cacheAge.TotalHours:F1} hours old, refreshing...");
    }
}

if (needsDownload)
{
    Console.WriteLine("Fetching Scryfall bulk data metadata...");
    var bulkDataResponse = await httpClient.GetFromJsonAsync<BulkDataResponse>("https://api.scryfall.com/bulk-data");
    var defaultCards = bulkDataResponse?.Data?.FirstOrDefault(x => x.Type == "default_cards");
    
    if (defaultCards == null)
    {
        Console.WriteLine("ERROR: Could not find default_cards bulk data");
        return;
    }
    
    Console.WriteLine($"Downloading Scryfall bulk data (~{defaultCards.Size / 1024 / 1024}MB)...");
    Console.WriteLine("This may take a minute...");
    
    var bulkJson = await httpClient.GetStringAsync(defaultCards.DownloadUri);
    File.WriteAllText(cacheFile, bulkJson);
    Console.WriteLine("Download complete, cached for 24 hours");
}

Console.WriteLine();
Console.WriteLine("Loading card database...");
var jsonText = File.ReadAllText(cacheFile);
var allCards = JsonSerializer.Deserialize<List<ScryfallCard>>(jsonText);

if (allCards == null || allCards.Count == 0)
{
    Console.WriteLine("ERROR: Failed to parse card database");
    return;
}

// Build arena_id lookup
Console.WriteLine($"Building Arena ID lookup from {allCards.Count} cards...");
var arenaCards = new Dictionary<int, ScryfallCard>();

foreach (var card in allCards)
{
    if (card.ArenaId.HasValue)
    {
        // If duplicate, prefer the one without "a" suffix in collector number (main printing)
        if (!arenaCards.ContainsKey(card.ArenaId.Value))
        {
            arenaCards[card.ArenaId.Value] = card;
        }
        else if (!card.CollectorNumber.Contains('a') && arenaCards[card.ArenaId.Value].CollectorNumber.Contains('a'))
        {
            arenaCards[card.ArenaId.Value] = card;
        }
    }
}

Console.WriteLine($"Found {arenaCards.Count} cards with Arena IDs");
Console.WriteLine();

// Translate collection
var csvOutput = new StringBuilder();
csvOutput.AppendLine("Count,Name,Edition,Collector Number");

int found = 0;
int notFound = 0;

foreach (var (grpId, count) in collection)
{
    if (arenaCards.TryGetValue(grpId, out var card))
    {
        string name = EscapeCsvField(card.Name);
        string edition = card.Set.ToUpper();
        string collectorNumber = EscapeCsvField(card.CollectorNumber);
        
        csvOutput.AppendLine($"{count},{name},{edition},{collectorNumber}");
        found++;
    }
    else
    {
        Console.WriteLine($"WARNING: Arena ID {grpId} not found in Scryfall database");
        notFound++;
    }
}

File.WriteAllText(outputFile, csvOutput.ToString());

var elapsed = DateTime.Now - startTime;
Console.WriteLine();
Console.WriteLine($"Completed in {elapsed.TotalSeconds:F1} seconds");
Console.WriteLine($"Output written to: {outputFile}");
Console.WriteLine($"Successfully translated {found} cards");
if (notFound > 0)
{
    Console.WriteLine($"WARNING: {notFound} cards not found in database");
}

static string EscapeCsvField(string field)
{
    if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
    {
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
    return field;
}

class BulkDataResponse
{
    [JsonPropertyName("data")]
    public List<BulkDataInfo>? Data { get; set; }
}

class BulkDataInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("download_uri")]
    public string DownloadUri { get; set; } = "";
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
}

class ScryfallCard
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("set")]
    public string Set { get; set; } = "";
    
    [JsonPropertyName("collector_number")]
    public string CollectorNumber { get; set; } = "";
    
    [JsonPropertyName("arena_id")]
    public int? ArenaId { get; set; }
}
