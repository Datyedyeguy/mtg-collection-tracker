# Development Setup

> **Note**: This is a personal learning project. Not actively seeking contributions at this time.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL)
- Azure CLI (for deployments - optional for local dev)
- Git

## Quick Start

### 1. Start PostgreSQL Database

```bash
# From repository root
docker compose up -d

# Verify it's running
docker ps --filter "name=mtg-postgres"
```

### 2. Create Development Configuration

The `appsettings.Development.json` file is gitignored (contains local credentials). Create it manually:

**File**: `src/backend/MTGCollectionTracker.Api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mtgtracker;Username=mtgadmin;Password=LocalDev123!"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5002", "https://localhost:5002"]
  }
}
```

> **Note**: The password `LocalDev123!` matches the Docker Compose configuration. This is only for local development.

### 3. Run the Backend API

```bash
cd src/backend/MTGCollectionTracker.Api
dotnet run --launch-profile https
```

API will be available at:

- `https://localhost:5001` (HTTPS)
- `http://localhost:5000` (HTTP)

Test the health endpoint:

```bash
curl -k https://localhost:5001/api/health
```

### 4. Run the Frontend (Coming Soon)

```bash
cd src/frontend/MTGCollectionTracker.Client
dotnet run
```

Frontend will be available at `https://localhost:5002`

## Database

### Connection Details (Local Docker)

| Setting  | Value          |
| -------- | -------------- |
| Host     | `localhost`    |
| Port     | `5432`         |
| Database | `mtgtracker`   |
| Username | `mtgadmin`     |
| Password | `LocalDev123!` |

### Docker Commands

```bash
# Start database
docker compose up -d

# Stop database (keeps data)
docker compose down

# Stop and DELETE all data (fresh start)
docker compose down -v

# View database logs
docker compose logs -f postgres

# Connect via psql
docker exec -it mtg-postgres psql -U mtgadmin -d mtgtracker
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
# Build entire solution
dotnet build MTGCollectionTracker.sln

# Build specific project
dotnet build src/backend/MTGCollectionTracker.Api/MTGCollectionTracker.Api.csproj
```

## Project Ports

| Service             | Port | URL                      |
| ------------------- | ---- | ------------------------ |
| Backend API (HTTPS) | 5001 | `https://localhost:5001` |
| Backend API (HTTP)  | 5000 | `http://localhost:5000`  |
| Frontend (Blazor)   | 5002 | `https://localhost:5002` |
| PostgreSQL          | 5432 | `localhost:5432`         |

## Troubleshooting

### HTTPS Certificate Warning

The ASP.NET Core development certificate is self-signed. To trust it:

```bash
dotnet dev-certs https --trust
```

### Port Already in Use

- Backend: Change ports in `src/backend/MTGCollectionTracker.Api/Properties/launchSettings.json`
- Frontend: Change ports in `src/frontend/MTGCollectionTracker.Client/Properties/launchSettings.json`

### Database Connection Fails

1. Ensure Docker is running: `docker ps`
2. Ensure PostgreSQL container is healthy: `docker ps --filter "name=mtg-postgres"`
3. Check connection string in `appsettings.Development.json`
4. Verify no other service is using port 5432

### Container Won't Start

```bash
# Check for port conflicts
netstat -ano | findstr :5432

# Remove old container and recreate
docker compose down
docker compose up -d
```

## Next Steps

- See [copilot-instructions.md](../.github/copilot-instructions.md) for architecture details
- See [DECISIONS.md](DECISIONS.md) for technology choices
