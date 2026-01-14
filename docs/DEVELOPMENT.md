# Development Setup

> **Note**: This is a personal learning project. Not actively seeking contributions at this time.

## Prerequisites

- .NET 8 SDK
- PostgreSQL 14+ (or Docker)
- Azure CLI (for deployments)
- Git
- **Note**: No Node.js required (using Blazor WebAssembly)

## Quick Start

### Backend

```bash
cd src/backend/MTGCollectionTracker.Api
dotnet restore
dotnet ef database update
dotnet run
```

API will be available at `https://localhost:5001`

### Frontend

```bash
cd src/frontend/MTGCollectionTracker.Client
dotnet restore
dotnet run
```

Frontend will be available at `https://localhost:5001`

### Desktop Client

```bash
cd src/desktop/MTGALogParser
dotnet restore
dotnet run
```

## Database Setup

### Local PostgreSQL

```bash
# Create database
createdb mtg-collection-tracker

# Update connection string in appsettings.Development.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=mtg-collection-tracker;Username=postgres;Password=yourpassword"
}

# Run migrations
dotnet ef database update -p MTGCollectionTracker.Data -s MTGCollectionTracker.Api
```

### Docker PostgreSQL

```bash
docker run --name mtg-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:14
```

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test src/backend/MTGCollectionTracker.Tests
```

## Building

```bash
# Build entire solution (backend + frontend + desktop)
dotnet build MTGCollectionTracker.sln

# Build specific project
dotnet build src/backend/MTGCollectionTracker.Api
dotnet build src/frontend/MTGCollectionTracker.Client
```

## Troubleshooting

### Port already in use

- Backend: Change port in `launchSettings.json`
- Frontend: Change port with `npm run dev -- --port 3000`

### Database connection fails

- Ensure PostgreSQL is running
- Check connection string in `appsettings.Development.json`
- Verify firewall allows connection

## Next Steps

- See [copilot-instructions.md](../.github/copilot-instructions.md) for architecture details
- See [DECISIONS.md](DECISIONS.md) for technology choices
