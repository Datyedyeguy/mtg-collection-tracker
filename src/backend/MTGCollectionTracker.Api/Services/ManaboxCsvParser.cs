using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;

namespace MTGCollectionTracker.Api.Services;

/// <summary>
/// A single parsed and de-duplicated row from a Manabox CSV export, ready for DB lookup.
/// Rows for the same Scryfall ID are aggregated before being emitted.
/// </summary>
public sealed record ManaboxRow(
    Guid ScryfallId,
    string CardName,
    int Quantity,
    int FoilQuantity);

/// <summary>
/// Parses a Manabox CSV stream and aggregates rows by Scryfall ID.
/// Injected into <see cref="ImportWorkerService"/> and unit-tested independently.
/// </summary>
public interface IManaboxCsvParser
{
    /// <summary>
    /// Streams a Manabox CSV, filters to the selected binders, and yields one aggregated
    /// <see cref="ManaboxRow"/> per unique Scryfall ID. Rows with missing or unparseable
    /// Scryfall IDs are collected in <paramref name="invalidNames"/> rather than thrown.
    /// </summary>
    IAsyncEnumerable<ManaboxRow> ParseAsync(
        Stream stream,
        HashSet<string> includedBinders,
        List<string> invalidNames,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class ManaboxCsvParser : IManaboxCsvParser
{
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        MissingFieldFound = null, // tolerate missing optional columns
        BadDataFound = null       // skip malformed rows silently
    };

    /// <inheritdoc />
    public async IAsyncEnumerable<ManaboxRow> ParseAsync(
        Stream stream,
        HashSet<string> includedBinders,
        List<string> invalidNames,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Aggregate in memory by ScryfallId to merge normal + foil rows for the same card
        // and to handle duplicate binder entries for the same printing.
        // Memory is bounded by unique ScryfallIds (~30k max for a full MTGA export).
        var aggregated = new Dictionary<Guid, (string Name, int Qty, int FoilQty)>();

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, CsvConfig);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var binderName = csv.GetField("Binder Name") ?? string.Empty;
            if (!includedBinders.Contains(binderName))
                continue;

            var cardName = csv.GetField("Name") ?? "Unknown";
            var scryfallIdStr = csv.GetField("Scryfall ID") ?? string.Empty;

            if (!Guid.TryParse(scryfallIdStr, out var scryfallId))
            {
                invalidNames.Add(cardName);
                continue;
            }

            var foilStr = csv.GetField("Foil") ?? "normal";
            _ = int.TryParse(csv.GetField("Quantity") ?? "1", out var qty);
            if (qty <= 0) qty = 1;

            var isFoil = foilStr.Equals("foil", StringComparison.OrdinalIgnoreCase);

            if (aggregated.TryGetValue(scryfallId, out var existing))
            {
                aggregated[scryfallId] = isFoil
                    ? (existing.Name, existing.Qty, existing.FoilQty + qty)
                    : (existing.Name, existing.Qty + qty, existing.FoilQty);
            }
            else
            {
                aggregated[scryfallId] = isFoil
                    ? (cardName, 0, qty)
                    : (cardName, qty, 0);
            }
        }

        foreach (var (scryfallId, (name, qty, foilQty)) in aggregated)
        {
            ct.ThrowIfCancellationRequested();
            yield return new ManaboxRow(scryfallId, name, qty, foilQty);
        }
    }
}
