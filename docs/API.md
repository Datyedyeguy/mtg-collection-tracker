# API Documentation

> **Status**: ✅ Core endpoints implemented. Authentication and Collections viewing functional.

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
      "rarity": "common",
      "platform": "Paper",
      "quantity": 4,
      "acquiredDate": "2024-03-15T00:00:00Z",
      "imageUrl": "https://cards.scryfall.io/normal/front/..."
    }
  ],
  "totalEntries": 1523,
  "totalCards": 8476,
  "platform": "Paper",
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

### Cards (Future - Phase 3)

#### Search Cards

```http
GET /api/cards/search?q=lightning+bolt&set=m21&page=1&pageSize=20
```

#### Get Card Details

```http
GET /api/cards/{id}
```

#### Get Card by Scryfall ID

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

**Last Updated:** February 14, 2026
**API Version:** v1
**Next Update:** Phase 3 (Card search endpoints)
