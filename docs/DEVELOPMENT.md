# Development Setup

> **Note**: This is a personal learning project. Not actively seeking contributions at this time.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for PostgreSQL)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) — required for deployments
- [GitHub CLI (`gh`)](https://cli.github.com/) — required for the bootstrap script
- Git

## Quick Start

### 1. Start PostgreSQL Database

```bash
# From repository root
docker compose up -d

# Verify it's running
docker ps --filter "name=mtg-postgres"
```

### 2. Trust the Development Certificate

ASP.NET Core uses a self-signed certificate for HTTPS during development. Trust it to avoid browser warnings and CORS issues:

```bash
dotnet dev-certs https --trust
```

This only needs to be done once per machine. You may be prompted to confirm.

### 3. Create Development Configuration

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
    "AllowedOrigins": [
      "http://localhost:5002",
      "https://localhost:5002",
      "http://localhost:5003",
      "https://localhost:5003"
    ]
  }
}
```

> **Note**: The password `LocalDev123!` matches the Docker Compose configuration. This is only for local development.

### 4. Run the Backend API

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

### 5. Run the Frontend

```bash
cd src/frontend/MTGCollectionTracker.Client
dotnet run --launch-profile https
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
dotnet test tests/MTGCollectionTracker.Api.Tests
```

## Building

```bash
# Build entire solution
dotnet build MTGCollectionTracker.slnx

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

## Azure Deployment

All Azure infrastructure is managed through GitHub Actions and a one-time bootstrap script. **No manual portal steps are needed after the first bootstrap.**

### Prerequisites

1. Log in to both CLIs:

   ```powershell
   az login
   gh auth login
   ```

2. Ensure you have an active Azure subscription and the target resource group or permissions to create one.

### First-Time Bootstrap

Run the bootstrap script once to create the Azure App Registration, OIDC federated credentials, and populate GitHub secrets:

```powershell
.\scripts\bootstrap-azure.ps1
```

Optional parameters (defaults shown):

```powershell
.\scripts\bootstrap-azure.ps1 `
    -ResourceGroup "mtg-tracker-rg" `
    -Location     "eastus2" `
    -AppName      "mtg-collection-tracker"
```

The script will:

- Create the resource group
- Create an Azure App Registration with OIDC federated credentials for GitHub Actions
- Assign the `Contributor` role on the resource group
- Set `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_STATIC_WEB_APPS_API_TOKEN` as GitHub secrets

### Required GitHub Secrets (Manual)

After the bootstrap, add these secrets manually in your GitHub repo settings (Settings → Secrets → Actions):

| Secret                 | Description                                         |
| ---------------------- | --------------------------------------------------- |
| `DB_ADMIN_PASSWORD`    | PostgreSQL administrator password (16+ chars)       |
| `JWT_SECRET`           | JWT signing key for the API (32+ chars)             |
| `DB_CONNECTION_STRING` | Full PostgreSQL connection string for EF migrations |

### Deploy Infrastructure

Trigger the infrastructure workflow manually from GitHub Actions:

```
GitHub → Actions → Infrastructure CI → Run workflow → Branch: main
```

Or push a change under `infrastructure/**`. The workflow will:

1. **Validate** — lint the Bicep files
2. **What-If** — preview changes (runs on PRs)
3. **Deploy** — apply to Azure (requires `production` environment approval)

After the first successful deploy, update the frontend production config with the real API hostname:

```json
// src/frontend/MTGCollectionTracker.Client/wwwroot/appsettings.Production.json
{
  "ApiBaseUrl": "https://<your-api-hostname>.azurewebsites.net"
}
```

### Subsequent Deployments

- **Backend**: Merge to `main` — `backend-ci.yml` builds, runs migrations, and deploys automatically
- **Frontend**: Merge to `main` — `frontend-ci.yml` builds and deploys automatically
- **Infrastructure changes**: Merge `infrastructure/**` changes to `main`

## Next Steps

- See [copilot-instructions.md](../.github/copilot-instructions.md) for architecture details
- See [DECISIONS.md](DECISIONS.md) for technology choices
