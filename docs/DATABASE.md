# Database Schema Documentation

This document describes the database schema for the MTG Collection Tracker application.

**Database**: PostgreSQL (Azure Database for PostgreSQL - Flexible Server in production)
**ORM**: Entity Framework Core 10 with Npgsql provider
**Migrations**: Located in `src/backend/MTGCollectionTracker.Data/Migrations/`

---

## Conventions

- **Timestamps**: All timestamps are stored as `timestamp with time zone` (PostgreSQL's `timestamptz`), which stores values internally as UTC. Always use `DateTime.UtcNow` in application code.
- **Primary Keys**: String GUIDs for user-related tables (Identity convention), UUID for application entities.
- **Naming**: Tables use PascalCase (EF Core default). Consider switching to snake_case for PostgreSQL convention in future.
- **Soft Deletes**: Not implemented initially. May add `DeletedAt` columns later if needed.

---

## Identity Tables (ASP.NET Core Identity)

These tables are managed by ASP.NET Core Identity and provide authentication/authorization infrastructure.

### AspNetUsers

The main user account table. Extends `IdentityUser` with custom application fields.

| Column                 | Type         | Nullable | Description                                                                   |
| ---------------------- | ------------ | -------- | ----------------------------------------------------------------------------- |
| `Id`                   | text         | NO       | Primary key. GUID as string (Identity convention).                            |
| `UserName`             | varchar(256) | YES      | Login identifier. Must be unique. Used for authentication.                    |
| `NormalizedUserName`   | varchar(256) | YES      | Uppercase version of UserName for case-insensitive lookups.                   |
| `Email`                | varchar(256) | YES      | User's email address. Must be unique per our configuration.                   |
| `NormalizedEmail`      | varchar(256) | YES      | Uppercase version of Email for case-insensitive lookups.                      |
| `EmailConfirmed`       | boolean      | NO       | Whether email has been verified. Default: false.                              |
| `PasswordHash`         | text         | YES      | BCrypt hash of user's password. Null for external login only users.           |
| `SecurityStamp`        | text         | YES      | Random value that changes when credentials change. Used to invalidate tokens. |
| `ConcurrencyStamp`     | text         | YES      | Used for optimistic concurrency. Changes on each update.                      |
| `PhoneNumber`          | text         | YES      | Optional phone number. Not used in MVP.                                       |
| `PhoneNumberConfirmed` | boolean      | NO       | Whether phone has been verified. Default: false.                              |
| `TwoFactorEnabled`     | boolean      | NO       | Whether 2FA is enabled. Default: false. Not used in MVP.                      |
| `LockoutEnd`           | timestamptz  | YES      | When lockout expires. Null if not locked out.                                 |
| `LockoutEnabled`       | boolean      | NO       | Whether account can be locked out. Default: true.                             |
| `AccessFailedCount`    | integer      | NO       | Number of failed login attempts. Resets on successful login.                  |
| `DisplayName`          | text         | YES      | **Custom field.** Friendly name shown in UI (can differ from UserName).       |
| `CreatedAt`            | timestamptz  | NO       | **Custom field.** When account was created. Default: NOW().                   |
| `UpdatedAt`            | timestamptz  | NO       | **Custom field.** When profile was last modified. Default: NOW().             |

**Indexes:**

- `UserNameIndex` - Unique index on `NormalizedUserName` for fast login lookups
- `EmailIndex` - Index on `NormalizedEmail` for email lookups
- `IX_AspNetUsers_DisplayName` - Index on `DisplayName` for potential search

---

### AspNetRoles

Role definitions for role-based access control (RBAC).

| Column             | Type         | Nullable | Description                                     |
| ------------------ | ------------ | -------- | ----------------------------------------------- |
| `Id`               | text         | NO       | Primary key. GUID as string.                    |
| `Name`             | varchar(256) | YES      | Role name (e.g., "Admin", "User").              |
| `NormalizedName`   | varchar(256) | YES      | Uppercase version for case-insensitive lookups. |
| `ConcurrencyStamp` | text         | YES      | Optimistic concurrency token.                   |

**Indexes:**

- `RoleNameIndex` - Unique index on `NormalizedName`

**Note**: We may not use roles in MVP. Simple user/admin distinction can use claims instead.

---

### AspNetUserRoles

Junction table linking users to roles (many-to-many).

| Column   | Type | Nullable | Description          |
| -------- | ---- | -------- | -------------------- |
| `UserId` | text | NO       | FK to AspNetUsers.Id |
| `RoleId` | text | NO       | FK to AspNetRoles.Id |

**Primary Key**: Composite (`UserId`, `RoleId`)

---

### AspNetUserClaims

Custom claims attached to individual users. Claims are key-value pairs that represent user attributes or permissions.

| Column       | Type    | Nullable | Description                                                 |
| ------------ | ------- | -------- | ----------------------------------------------------------- |
| `Id`         | integer | NO       | Auto-incrementing primary key.                              |
| `UserId`     | text    | NO       | FK to AspNetUsers.Id                                        |
| `ClaimType`  | text    | YES      | Claim identifier (e.g., "subscription_tier", "can_export"). |
| `ClaimValue` | text    | YES      | Claim value (e.g., "premium", "true").                      |

**Example Uses**:

- Premium subscription status
- Feature flags per user
- Custom permissions not tied to roles

---

### AspNetRoleClaims

Claims attached to roles. All users in a role inherit these claims.

| Column       | Type    | Nullable | Description                    |
| ------------ | ------- | -------- | ------------------------------ |
| `Id`         | integer | NO       | Auto-incrementing primary key. |
| `RoleId`     | text    | NO       | FK to AspNetRoles.Id           |
| `ClaimType`  | text    | YES      | Claim identifier.              |
| `ClaimValue` | text    | YES      | Claim value.                   |

---

### AspNetUserLogins

External login provider associations (Google, Microsoft, etc.). Not used in MVP.

| Column                | Type | Nullable | Description                                  |
| --------------------- | ---- | -------- | -------------------------------------------- |
| `LoginProvider`       | text | NO       | Provider name (e.g., "Google", "Microsoft"). |
| `ProviderKey`         | text | NO       | User's unique ID from the provider.          |
| `ProviderDisplayName` | text | YES      | Friendly name for the provider.              |
| `UserId`              | text | NO       | FK to AspNetUsers.Id                         |

**Primary Key**: Composite (`LoginProvider`, `ProviderKey`)

---

### AspNetUserTokens

Stores tokens for various purposes (password reset, 2FA, refresh tokens if stored in DB).

| Column          | Type | Nullable | Description                                            |
| --------------- | ---- | -------- | ------------------------------------------------------ |
| `UserId`        | text | NO       | FK to AspNetUsers.Id                                   |
| `LoginProvider` | text | NO       | Token provider (e.g., "[AspNetUserStore]").            |
| `Name`          | text | NO       | Token name (e.g., "RefreshToken", "AuthenticatorKey"). |
| `Value`         | text | YES      | The token value.                                       |

**Primary Key**: Composite (`UserId`, `LoginProvider`, `Name`)

---

## Application Tables (Future)

These tables will be added as we build features.

### Cards (Phase 3)

Card metadata synced from Scryfall. ~111,000 rows.

```
Planned columns: scryfall_id, oracle_id, name, set_code, collector_number,
rarity, mana_cost, type_line, oracle_text, colors (JSONB), cmc, image_uris (JSONB),
prices (JSONB), legalities (JSONB), arena_id, mtgo_id, created_at, updated_at
```

### CollectionEntries (Phase 3)

User's card collection entries.

```
Planned columns: user_id, card_id, platform (paper/arena/mtgo), quantity,
foil_quantity, location, notes, acquired_date, created_at, updated_at
```

### Decklists (Phase 6)

User's saved decklists.

```
Planned columns: user_id, name, format, platform, description, is_public,
created_at, updated_at
```

### DecklistEntries (Phase 6)

Cards in a decklist.

```
Planned columns: decklist_id, card_id, quantity, is_sideboard, is_commander,
category, created_at
```

### ImportHistory (Phase 3)

Track collection import operations.

```
Planned columns: user_id, platform, import_type, source, cards_added,
cards_removed, cards_updated, file_name, imported_at
```

---

## Migration History

| Migration         | Date       | Description                                       |
| ----------------- | ---------- | ------------------------------------------------- |
| `InitialIdentity` | 2026-01-19 | ASP.NET Core Identity tables + custom user fields |

---

## Common Queries

### Find user by username (login)

```sql
SELECT * FROM "AspNetUsers"
WHERE "NormalizedUserName" = UPPER('username');
```

### Check if user is locked out

```sql
SELECT "LockoutEnd" > NOW() AS is_locked
FROM "AspNetUsers"
WHERE "Id" = 'user-guid';
```

### Get user with roles

```sql
SELECT u."UserName", r."Name" as "Role"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Id" = 'user-guid';
```

---

**Last Updated**: January 19, 2026
**Next Update**: When Cards/Collections tables are added (Phase 3)
