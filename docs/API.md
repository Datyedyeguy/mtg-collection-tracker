# API Documentation

> **Status**: ✅ Core endpoints implemented. Authentication, Collections (full CRUD: view, add, edit, delete, ownership query), Cards (search + detail), and **Imports** (async background CSV import pipeline) are functional.

Base URL:

- **Local Development**: `https://localhost:5001`
- **Production**: `https://api.example.com` (TBD)

## Authentication

All endpoints except `/api/auth/register` and `/api/auth/login` require JWT authentication.

**Authorization Header:**

```http
Authorization: Bearer <jwt_token>
```

**Token Lifespan:**

- Access Token: 15 minutes
- Refresh Token: 7 days

---

## Endpoints

### Authentication

#### Register New User

**Endpoint:** `POST /api/auth/register`

**Request:**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "displayName": "John Doe"
}
```

**Success Response (200 OK):**

```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "email": "user@example.com",
  "displayName": "John Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454",
  "expiresIn": 900
}
```

**Error Response (400 Bad Request):**

```json
{
  "message": "Registration failed.",
  "errors": ["Password must be at least 12 characters"]
}
```

**Password Requirements:**

- Minimum 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

---

#### Login

**Endpoint:** `POST /api/auth/login`

**Request:**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (200 OK):**

```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "email": "user@example.com",
  "displayName": "John Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454",
  "expiresIn": 900
}
```

**Error Response (401 Unauthorized):**

```json
{
  "message": "Invalid email or password"
}
```

---

#### Refresh Access Token

**Endpoint:** `POST /api/auth/refresh`

**Request:**

```json
{
  "refreshToken": "f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"
}
```

**Success Response (200 OK):**

```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "email": "user@example.com",
  "displayName": "John Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "expiresIn": 900
}
```

**Notes:**

- Old refresh token is automatically revoked
- New refresh token is returned (token rotation for security)

**Error Response (401 Unauthorized):**

```json
{
  "message": "Invalid or expired refresh token"
}
```

---

#### Logout

**Endpoint:** `POST /api/auth/logout`

**Authorization:** Required (Bearer token)

**Request:**

```json
{
  "refreshToken": "f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"
}
```

**Success Response (200 OK):**

```json
{
  "message": "Logout successful"
}
```

**Notes:**

- Revokes the provided refresh token
- Access token remains valid until expiration (15 minutes)
- Client should discard tokens immediately

---

### Collections

#### Get User Collection

**Endpoint:** `GET /api/collections`

**Authorization:** Required (Bearer token)

**Query Parameters:**

- `platform` (optional): Filter by platform (`Paper`, `Arena`, `Mtgo`)
- `page` (optional): Page number (1-based, default: 1)
- `pageSize` (optional): Items per page (1-100, default: 50)

**Example Requests:**

```http
# Get all collections (first 50 entries)
GET /api/collections

# Get Arena collection only
GET /api/collections?platform=Arena

# Get Paper collection with pagination
GET /api/collections?platform=Paper&page=2&pageSize=25
```

**Success Response (200 OK):**

```json
{
  "entries": [
    {
      "id": "c3d4e5f6-7890-1234-5678-90abcdef1234",
      "cardId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
      "cardName": "Lightning Bolt",
      "setCode": "m21",
      "collectorNumber": "123",
      "platform": "Paper",
      "quantity": 4,
      "foilQuantity": 0,
      "imageUri": "https://cards.scryfall.io/normal/front/...",
      "acquiredDate": null,
      "createdAt": "2026-02-26T12:00:00Z"
    }
  ],
  "totalUniqueCards": 1523,
  "totalCards": 8476,
  "cardsByPlatform": { "Paper": 6000, "Arena": 2000, "Mtgo": 476 },
  "currentPage": 1,
  "pageSize": 50,
  "totalPages": 31,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Error Responses:**

**400 Bad Request (invalid parameters):**

```json
"Page number must be at least 1"
```

**401 Unauthorized (missing/invalid token):**

```json
{
  "message": "Unauthorized"
}
```

---

#### Add Card to Collection

**Endpoint:** `POST /api/collections`

**Authorization:** Required (Bearer token)

**Upsert semantics:** If the user already owns this card on the same platform, quantities are accumulated onto the existing entry (200 OK). A new entry returns 201 Created.

**Request Body:**

```json
{
  "cardId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "platform": "Paper",
  "quantity": 4,
  "foilQuantity": 1
}
```

**Success Responses:**

- **201 Created** — new collection entry created
- **200 OK** — quantities added to an existing entry

Both return the updated `CollectionEntryDto` (same shape as entries in `GET /api/collections`).

**Error Responses:**

- **400 Bad Request** — both quantities are zero, or a quantity is negative
- **404 Not Found** — `cardId` does not exist in the cards table
- **401 Unauthorized** — missing/invalid token

---

#### Get Card Ownership

**Endpoint:** `GET /api/collections/card/{cardId}`

**Authorization:** Required (Bearer token)

Returns all collection entries for a specific card across all platforms. Used by the card detail page to show how many copies the user owns.

**Example Request:**

```http
GET /api/collections/card/a1b2c3d4-5678-90ab-cdef-1234567890ab
```

**Success Response (200 OK):**

```json
[
  { "platform": "Paper", "quantity": 4, "foilQuantity": 1, ... },
  { "platform": "Mtgo",  "quantity": 2, "foilQuantity": 0, ... }
]
```

Returns an empty array `[]` if the card is not in the user's collection.

**Error Responses:**

- **404 Not Found** — `cardId` does not exist in the cards table
- **401 Unauthorized** — missing/invalid token

---

#### Update Collection Entry

**Endpoint:** `PUT /api/collections/{id}`

**Authorization:** Required (Bearer token)

**Absolute value semantics:** The entry's quantities are set to the provided values, not incremented. To remove a card entirely, use `DELETE /api/collections/{id}` instead.

**Request Body:**

```json
{
  "quantity": 2,
  "foilQuantity": 1
}
```

**Success Response (200 OK):**

Returns the updated `CollectionEntryDto` (same shape as entries in `GET /api/collections`).

**Validation Rules:**

- `quantity` and `foilQuantity` must be >= 0
- At least one must be > 0 (use DELETE to remove entirely)

**Error Responses:**

- **400 Bad Request** — both quantities are zero, or a quantity is negative
- **404 Not Found** — entry does not exist or belongs to another user
- **401 Unauthorized** — missing/invalid token

---

#### Remove Card from Collection

**Endpoint:** `DELETE /api/collections/{id}`

**Authorization:** Required (Bearer token)

Permanently removes the collection entry. This is a hard delete — the entry cannot be recovered.

**Example Request:**

```http
DELETE /api/collections/c3d4e5f6-7890-1234-5678-90abcdef1234
```

**Success Response (204 No Content):**

Empty body.

**Error Responses:**

- **404 Not Found** — entry does not exist or belongs to another user
- **401 Unauthorized** — missing/invalid token

**Security Note:** Returns 404 (not 403) for entries belonging to other users to avoid leaking existence information.

---

#### Import Collection (Future - Phase 3)

```http
POST /api/collections/import
Content-Type: multipart/form-data

file: <csv_file>
platform: Paper
source: moxfield
```

---

#### Sync MTGA Collection (Future - Phase 4)

```http
POST /api/collections/sync
Content-Type: application/json

{
  "platform": "Arena",
  "cards": [
    { "arenaId": 12345, "quantity": 4 },
    { "arenaId": 67890, "quantity": 2 }
  ]
}
```

---

### Cards

#### Search Cards

**Endpoint:** `GET /api/cards`

**Authorization:** Required (Bearer token)

**Note on auth:** Card search hits the database with potentially expensive queries (112k+ cards,
full-text filtering, multi-step deduplication). Putting it behind auth limits abuse to registered
users only and gives us an identity to tie to any suspicious traffic patterns.

**Query Parameters:**

| Parameter      | Type    | Default | Description                                                                                              |
| -------------- | ------- | ------- | -------------------------------------------------------------------------------------------------------- |
| `q`            | string  | —       | Name search. Case-insensitive, partial match against card name and flavor name.                          |
| `set`          | string  | —       | Filter by set code (e.g., `m21`, `znr`). Case-insensitive.                                               |
| `type`         | string  | —       | Filter by type line (e.g., `Goblin`, `Instant`). Partial, case-insensitive.                              |
| `allPrintings` | boolean | `false` | When `false` (default), one result per oracle card (de-duplicated). When `true`, all printings returned. |
| `page`         | integer | `1`     | Page number (1-based).                                                                                   |
| `pageSize`     | integer | `20`    | Results per page (1-100).                                                                                |

**Deduplication (allPrintings=false):**

When de-duplication is active, the API:

1. Collects all oracle IDs from cards that match the filters (including flavor name matches)
2. Fetches ALL printings of those oracle IDs from the full card table
3. Picks a representative printing (preferring printings without a `FlavorName`)
4. Records `MatchedFlavorName` + `MatchedImageUri` when the search matched via a flavor name and the representative printing doesn't show it

**Example Requests:**

```http
# Search by name
GET /api/cards?q=lightning+bolt

# Search for a flavor name (showcase/Universe Beyond crossover)
GET /api/cards?q=lone+commando

# Filter by set
GET /api/cards?set=m21&page=1&pageSize=20

# Show all printings of Lightning Bolt
GET /api/cards?q=lightning+bolt&allPrintings=true
```

**Success Response (200 OK):**

```json
{
  "cards": [
    {
      "id": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
      "scryfallId": "e7c61540-8c83-4c4e-bc32-6f3a18f72f99",
      "name": "Lightning Bolt",
      "flavorName": null,
      "setCode": "m21",
      "collectorNumber": "152",
      "rarity": "common",
      "typeLine": "Instant",
      "manaCost": "{R}",
      "colors": ["R"],
      "imageUri": "https://cards.scryfall.io/normal/front/...",
      "matchedFlavorName": "Lightning, Lone Commando",
      "matchedImageUri": "https://cards.scryfall.io/normal/front/..."
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "allPrintings": false
}
```

**Notes:**

- `matchedFlavorName` is non-null when the search matched a showcase/Universe Beyond printing via its flavor name but the representative card shown is a standard printing. Displayed as "aka: [flavor name]" in the UI.
- `matchedImageUri` is non-null in the same scenario, used to display the art from the matched printing instead of the canonical one.
- `flavorName` on the card itself is the flavor name of that specific printing (will be non-null for showcase/crossover cards).

---

#### Get Card Details

**Endpoint:** `GET /api/cards/{id}`

**Authorization:** Required (Bearer token)

Returns full card detail for a given internal card ID, including oracle text, mana cost, type line, format legalities, all alternate printings, and image URI. Multi-faced cards include a `faces` array. Implemented Feb 24, 2026.

#### Get Card by Scryfall ID (Future)

```http
GET /api/cards/scryfall/{scryfallId}
```

---

### Decklists (Future - Phase 6)

#### Get User Decklists

```http
GET /api/decklists?page=1&pageSize=20
```

#### Get Decklist by ID

```http
GET /api/decklists/{id}
```

#### Create Decklist

```http
POST /api/decklists
Content-Type: application/json

{
  "name": "Mono Red Aggro",
  "format": "standard",
  "platform": "Paper",
  "description": "Fast aggro deck",
  "cards": [
    { "cardId": "a1b2c3d4-5678-90ab-cdef-1234567890ab", "quantity": 4, "isSideboard": false }
  ]
}
```

#### Update Decklist

```http
PUT /api/decklists/{id}
Content-Type: application/json

{
  "name": "Mono Red Aggro v2",
  "description": "Updated with new cards"
}
```

#### Delete Decklist

```http
DELETE /api/decklists/{id}
```

---

### Imports

Manabox CSV imports are processed **asynchronously** as background jobs. The client submits the file,
receives a job ID, then polls for progress until the job reaches a terminal state.

#### Submit Manabox CSV Import

**Endpoint:** `POST /api/imports/manabox`

**Authorization:** Required (Bearer token)

**Content-Type:** `multipart/form-data`

**Request fields:**

| Field | Type | Required | Description |
|---|---|---|---|
| `file` | File (.csv) | Yes | Manabox CSV export. Maximum 50 MB. |
| `mode` | string | Yes | `Accumulate` or `Replace`. See below. |
| `includedBinders` | JSON string | No | JSON array of binder names to import. Omit to import all. |

**Import modes:**

- `Accumulate` — Add imported quantities on top of existing Paper collection (ON CONFLICT DO UPDATE)
- `Replace` — Delete **all** existing Paper entries first, then insert fresh

**Example:**

```http
POST /api/imports/manabox
Content-Type: multipart/form-data

file=<csv bytes>
mode=Accumulate
includedBinders=["Main Deck","Sideboard"]
```

**Success Response (202 Accepted):**

```json
{
  "jobId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "statusUrl": "/api/imports/a1b2c3d4-5678-90ab-cdef-1234567890ab/status"
}
```

**Error Responses:**

- `400 Bad Request` — File missing, empty, not a .csv, or invalid mode
- `401 Unauthorized` — Missing or invalid token

---

#### Poll Import Job Status

**Endpoint:** `GET /api/imports/{jobId}/status`

**Authorization:** Required (Bearer token)

**Response while processing (200 OK):**

```json
{
  "jobId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "status": "Processing",
  "progress": 45
}
```

**Response when completed (200 OK):**

```json
{
  "jobId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "status": "Completed",
  "progress": 100,
  "result": {
    "imported": 4103,
    "updated": 0,
    "totalCopies": 8704,
    "skipped": 12,
    "skippedCards": ["Ragavan, Nimble Pilferer"]
  }
}
```

**Result fields:**

| Field | Description |
|---|---|
| `imported` | Unique card entries newly created (distinct ScryfallId slots) |
| `updated` | Existing entries whose quantities were incremented (Accumulate mode only) |
| `totalCopies` | Physical cards added: sum of `Quantity + FoilQuantity` across all matched rows |
| `skipped` | Rows whose ScryfallId was not found in the local card database |
| `skippedCards` | Names of skipped cards (run ScryfallSync to pull missing cards) |

**Response when failed (200 OK):**

```json
{
  "jobId": "a1b2c3d4-5678-90ab-cdef-1234567890ab",
  "status": "Failed",
  "progress": 30,
  "error": "No header record was found."
}
```

**Job status values:**

| Status | Description |
|---|---|
| `Pending` | Accepted, waiting to be picked up by the worker |
| `Processing` | Worker is actively processing rows (check `progress`) |
| `Completed` | All rows processed; `result` is populated |
| `Failed` | Unrecoverable error; `error` contains the message |

**Notes:**

- Returns `404 Not Found` if the jobId belongs to a different user
- Recommended polling interval: 2 seconds
- The job record is retained indefinitely for audit purposes
- `CsvBytes` are cleared from the database once the job completes (storage reclaim)

---

### Health Check

#### Health Status

**Endpoint:** `GET /api/health`

**Authorization:** None required

**Success Response (200 OK):**

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-14T12:34:56.789Z"
}
```

---

## Error Responses

### Standard Error Format

```json
{
  "message": "Error description",
  "errors": ["Details if applicable"]
}
```

### Common HTTP Status Codes

| Code | Meaning               | Description                             |
| ---- | --------------------- | --------------------------------------- |
| 200  | OK                    | Request succeeded                       |
| 400  | Bad Request           | Invalid request parameters              |
| 401  | Unauthorized          | Missing or invalid authentication       |
| 403  | Forbidden             | Valid auth but insufficient permissions |
| 404  | Not Found             | Resource doesn't exist                  |
| 500  | Internal Server Error | Server error (check logs)               |

---

## Rate Limiting (Future - Phase 5)

- 1000 requests per hour per user
- 100 requests per minute per IP

---

## OpenAPI/Swagger

When the API is running locally, Swagger UI is available at:

- `https://localhost:5001/swagger`

---

**Last Updated:** March 5, 2026
**API Version:** v1
**Next Update:** Phase 4 (Collection search, Azure deployment)
