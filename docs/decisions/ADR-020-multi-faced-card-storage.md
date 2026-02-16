# ADR-020: Multi-Faced Card Storage and Search Strategy

**Status**: Accepted
**Date**: 2026-02-16
**Deciders**: Development Team
**Related**: ADR-003 (PostgreSQL), ADR-010 (Scryfall Card Data)

## Context

Magic: The Gathering has various multi-faced cards:

- **Transform cards**: "Delver of Secrets // Insectile Aberration" (two distinct faces, physical card flips)
- **Modal DFCs**: "Alrund, God of the Cosmos // Hakka, Whispering Raven" (choose which face to cast)
- **Reversible cards**: Secret Lair promos with same card, different artwork on each side
- **Split/Flip/Adventure**: Other multi-part layouts

**Key Requirements:**

1. Users must be able to view all faces of a card
2. Users must be able to search by any face name (e.g., search "Insectile Aberration" finds Delver)
3. System should be decoupled from Scryfall's schema (resilient to API changes)
4. Performance must be acceptable for ~112,000 cards
5. Storage should not be excessively redundant

**Problem**: How do we store and search multi-faced card data efficiently?

## Decision

We will store multi-faced card data as a **JSONB array** using our own simplified schema, with **materialized views** for search optimization.

### Storage Approach

1. **Single row per card** (not one row per face)
   - One database row = one physical card (identified by `scryfall_id`)
   - Multi-faced cards have a `faces` JSONB column

2. **Custom DTO schema** (not raw Scryfall data)
   - Define `CardFaceDto` with only fields we need
   - Transform Scryfall's `card_faces` array → our simplified structure
   - Decouple from Scryfall's schema evolution

3. **Application-level validation** (not database constraints)
   - Use strongly-typed `List<CardFaceDto>` in C# code
   - Validate before saving to database
   - `[NotMapped]` property for type-safe access

4. **Materialized view for search** (not real-time indexes)
   - Create `card_search` materialized view
   - Combines main card name + all face names
   - Full-text search with PostgreSQL `tsvector`
   - Refresh after bulk sync operations

### Schema

```sql
-- Main table
CREATE TABLE cards (
    id UUID PRIMARY KEY,
    name VARCHAR(255),  -- e.g., "Delver of Secrets // Insectile Aberration"
    -- ... other single-faced or front-face data
    faces JSONB,  -- NULL for single-faced, array for multi-faced
    CONSTRAINT faces_is_array CHECK (
        faces IS NULL OR jsonb_typeof(faces) = 'array'
    )
);

-- Materialized view for search
CREATE MATERIALIZED VIEW card_search AS
SELECT
    c.id,
    c.name,
    to_tsvector('english',
        c.name || ' ' ||
        COALESCE(
            (SELECT string_agg(f->>'name', ' ')
             FROM jsonb_array_elements(c.faces) f),
            ''
        )
    ) AS search_vector
FROM cards c;

CREATE INDEX idx_card_search_vector
ON card_search USING GIN (search_vector);
```

### CardFaceDto Structure

```json
[
  {
    "name": "Delver of Secrets",
    "mana_cost": "{U}",
    "type_line": "Creature — Human Wizard",
    "oracle_text": "At the beginning of your upkeep...",
    "power": "1",
    "toughness": "1",
    "image_uri": "https://cards.scryfall.io/normal/front/...",
    "colors": ["U"]
  },
  {
    "name": "Insectile Aberration",
    "type_line": "Creature — Human Insect",
    "power": "3",
    "toughness": "2",
    "image_uri": "https://cards.scryfall.io/normal/back/...",
    "colors": ["U"]
  }
]
```

## Alternatives Considered

### Alternative 1: Normalized `card_faces` Table

```sql
CREATE TABLE card_faces (
    id UUID PRIMARY KEY,
    card_id UUID REFERENCES cards(id),
    face_index INT,
    name VARCHAR(255),
    -- ... other columns
);
```

**Pros:**

- "Textbook" database normalization
- Can query faces independently
- Schema enforced by columns

**Cons:**

- More complex sync (multiple inserts/updates per card)
- JOIN required for every query (performance overhead)
- Schema rigidity (adding face fields requires migration)
- Face data never queried independently in our use case

**Why rejected**: Over-normalization for data that's always accessed together. The "is-a" relationship (card HAS faces, not faces ARE entities) suggests composition over normalization.

### Alternative 2: Store Raw Scryfall Data

```sql
CREATE TABLE cards (
    -- ...
    raw_scryfall_data JSONB  -- Store everything from Scryfall
);
```

**Pros:**

- Maximum future-proofing
- No transformation needed

**Cons:**

- **Excessive redundancy** (100+ fields we don't use)
- Tight coupling to Scryfall's schema
- Larger storage footprint (~5-10x)
- Harder to query specific fields

**Why rejected**: User identified this as wasteful storage. We only need ~10 fields per face, not 100+ Scryfall provides.

### Alternative 3: Computed/Generated Columns

```sql
ALTER TABLE cards ADD COLUMN face_names TEXT[] GENERATED ALWAYS AS (
    SELECT array_agg(value->>'name') FROM jsonb_array_elements(faces)
) STORED;
```

**Pros:**

- Best of both worlds (JSONB + indexed column)
- Automatic updates

**Cons:**

- Complex generated column expressions
- Limited to simple extractions
- Extra storage for generated data

**Why rejected**: Materialized views provide the same benefit with more flexibility and easier maintenance.

## Consequences

### Positive

- ✅ **Simple sync**: One row insert/update per card
- ✅ **Decoupled**: Resilient to Scryfall schema changes
- ✅ **Type-safe**: `CardFaceDto` provides compile-time checking
- ✅ **Efficient storage**: Only store fields we use
- ✅ **Fast searches**: Materialized view with GIN index
- ✅ **Maintenance**: Easy to add new face fields (just update DTO)

### Negative

- ❌ **Materialized view refresh**: Requires explicit `REFRESH MATERIALIZED VIEW` after bulk sync
- ❌ **JSONB learning curve**: Developers must understand JSONB query syntax
- ❌ **Manual validation**: Application must validate face structure before saving

### Neutral

- ⚠️ **PostgreSQL-specific**: JSONB is PostgreSQL feature (not portable to MySQL/SQL Server)
  - Mitigation: We already chose PostgreSQL (ADR-003), so this is acceptable

## Implementation Notes

1. **Sync process**:
   - Transform Scryfall's `card_faces` → `List<CardFaceDto>`
   - Serialize to JSON string for database storage
   - Store in `faces` column

2. **Search process**:
   - Query `card_search` materialized view
   - PostgreSQL full-text search on `search_vector`
   - Refresh view after daily Scryfall sync

3. **Display**:
   - Deserialize `faces` JSON → `List<CardFaceDto>`
   - Render each face in UI with images
   - Show flip animation or tabs for multi-faced cards

4. **Migration strategy**:
   - Add `faces` JSONB column to existing cards table
   - Create materialized view
   - Create GIN index
   - Populate during next Scryfall sync

## Related Decisions

- **ADR-003**: PostgreSQL chosen for JSONB support
- **ADR-010**: Scryfall as data source
- **ADR-020** (this): How to store multi-faced data from Scryfall

## References

- [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)
- [Scryfall API - Card Objects](https://scryfall.com/docs/api/cards)
- [PostgreSQL Materialized Views](https://www.postgresql.org/docs/current/rules-materializedviews.html)
- [Martin Fowler - When to Use NOSQL](https://martinfowler.com/articles/nosql-intro.html)

## Review History

- 2026-02-16: Initial decision during ScryfallSync implementation
