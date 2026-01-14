# ADR-010: Scryfall as Card Data Source

**Date**: January 13, 2026
**Status**: Accepted

## Context

Need comprehensive MTG card data including paper cards, Arena-exclusive sets (Alchemy, Through the Omenpaths), and rebalanced cards. Options:

1. **Scryfall API** (free bulk data)
2. **MTGJSON** (free JSON files)
3. **17Lands** (Arena-focused CSV)
4. **Manual database** (scrape Gatherer)

## Decision

Use Scryfall bulk data API exclusively with daily sync to PostgreSQL.

## Consequences

**Pros**:

- **Comprehensive**: 111,000+ cards with all printings including Alchemy and Arena-exclusive sets
- **Well-maintained**: Updated within hours of new sets
- **Bulk data endpoint**: No rate limits (respects their guidelines)
- **Rich metadata**: Prices, legalities, rulings, images, artist attribution
- **Arena/MTGO IDs**: Includes platform-specific identifiers (arena_id GrpIds)
- **Free**: No API key or cost
- **JSON format**: Easy to parse
- **Excellent documentation**: Clear API docs and usage guidelines
- **All images**: High-quality card images with proper copyright attribution

**Cons**:

- **Large files**: 600 MB uncompressed (80 MB gzipped)
- **Daily sync needed**: Can't rely on real-time API for every request

**Why not MTGJSON?**

- Less frequently updated
- More complex JSON structure
- Larger file size

**Scryfall Usage Compliance**:

Per Scryfall Fan Content Policy, we comply with:

- ✅ No paywall for card data access
- ✅ Adding value (collection tracking, deck building, MTGA sync)
- ✅ Proper image attribution (artist/copyright visible)
- ✅ Using bulk data endpoint (no rate limit abuse)
- ✅ Caching locally in database (24+ hour cache)
- ✅ Attribution: "Card data provided by Scryfall"

**Implementation Strategy**:

1. Download bulk data daily at 3 AM UTC via scheduled job
2. Store all cards in PostgreSQL for fast queries
3. Cache downloaded file locally for 24 hours (optional optimization)
4. Use `arena_id` when available, fallback to name+set matching
5. Display full card images with artist attribution
6. Include Scryfall credit in app footer

**Future Consideration**: If Scryfall proves insufficient for Arena-specific data, evaluate supplementing with 17Lands or MTGA log parsing
