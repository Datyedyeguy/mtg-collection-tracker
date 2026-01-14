# API Documentation

> **Status**: Not yet implemented. This is a placeholder for future API documentation.

Base URL: `https://api.example.com` (TBD)

## Authentication

All endpoints except `/auth/register` and `/auth/login` require JWT authentication.

```http
Authorization: Bearer <jwt_token>
```

## Endpoints

### Authentication

#### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "displayName": "John Doe"
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

Response: {
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 900
}
```

### Collections

#### Get All Collections

```http
GET /api/collections
```

#### Get Platform Collection

```http
GET /api/collections/{platform}
# platform: paper, arena, mtgo
```

#### Import Collection

```http
POST /api/collections/import
Content-Type: multipart/form-data

file: <csv_file>
platform: paper
source: moxfield
```

#### Sync MTGA Collection

```http
POST /api/collections/sync
Content-Type: application/json

{
  "platform": "arena",
  "cards": [
    { "grpId": 12345, "quantity": 4 },
    { "grpId": 67890, "quantity": 2 }
  ]
}
```

### Cards

#### Search Cards

```http
GET /api/cards/search?q=lightning+bolt&set=lea
```

#### Get Card Details

```http
GET /api/cards/{id}
```

### Decklists

#### Get User Decklists

```http
GET /api/decklists
```

#### Create Decklist

```http
POST /api/decklists
Content-Type: application/json

{
  "name": "Mono Red Aggro",
  "format": "standard",
  "platform": "paper",
  "cards": [
    { "cardId": "...", "quantity": 4, "isSideboard": false }
  ]
}
```

## Error Responses

```json
{
  "error": "Invalid credentials",
  "statusCode": 401
}
```

## Rate Limiting

- 1000 requests per hour per user
- 100 requests per minute per IP

## OpenAPI/Swagger

When the API is running locally, Swagger UI is available at:

- `https://localhost:5001/swagger`
