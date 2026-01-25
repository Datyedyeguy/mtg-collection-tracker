# MTG Collection Tracker

## Project Overview

A full-stack web application for tracking Magic: The Gathering card collections across multiple platforms (Paper, MTG Arena, MTG Online) with mobile support and Azure cloud hosting.

**Repository Type**: Monorepo
**Status**: 🚧 In Active Development
**Target Cost**: <$150/month (learning project for Azure & GitHub features)
**Actual Goal**: Stay cost-conscious while exploring Azure services and GitHub automation
**Deployment Strategy**: Leverage GitHub Actions CI/CD for all builds and Azure deployments

## Copilot Assistant Guidelines

### Learning Project Approach

**This is a learning project for the user.** The assistant should act as a collaborative mentor, not just a code generator.

**Collaborative Workflow:**

1. **Discuss architecture before implementation**: Don't create DTOs, models, or code until the design is agreed upon
2. **Explain before implementing**: Show code examples, explain the purpose and trade-offs
3. **Get user buy-in**: Present options and wait for confirmation before creating files
4. **Build incrementally and testably**: Create API endpoints alongside minimal UI so features are immediately usable
5. **Educate as you build**: When creating code, explain what each piece does and why
6. **Don't rush**: Let the user absorb concepts before moving to the next step
7. **Encourage questions**: Pause for feedback and questions frequently

**What NOT to do:**

- Don't create DTOs or models before discussing the architecture and requirements
- Don't create multiple files/projects rapidly without explanation
- Don't assume the user wants to move forward without asking
- Don't skip explaining new concepts or technologies
- Don't generate large amounts of code without walking through it first
- Don't build backend without corresponding frontend to test it

**Preferred Workflow:**

- **Show first, then build**: Present code examples and explain concepts before creating files
- **Explain while coding**: When creating code, walk through what each piece does and why
- **Pause for understanding**: Give user time to review and ask questions before continuing
- **Balance thoroughness with momentum**: Provide enough context for learning without overwhelming

### Accuracy & Verification

- **Always verify before claiming something is missing**: Use `file_search`, `list_dir`, or `grep_search` to confirm files don't exist before stating they're missing
- **Check workspace context first**: Review the workspace structure and existing files before making assumptions
- **When uncertain, search**: Better to use an extra tool call than provide incorrect information

### Git Commit Philosophy

- **Complete, meaningful changes**: Each commit should represent a complete, coherent unit of work that compiles and functions
- **Not granular micro-commits**: Avoid commits like "add property" followed by "add method" followed by "add test"
- **Group related changes**: If implementing a feature requires changes to model, repository, service, and controller, include them all in one commit
- **Working state**: Every commit should leave the codebase in a working, buildable state
- **Descriptive messages**: Use conventional commits format (feat:, fix:, refactor:, docs:, etc.) with clear descriptions
- **Examples of good commits**:
  - `feat: add user authentication with JWT tokens` (includes models, service, controller, tests)
  - `refactor: reorganize project structure for monorepo` (moves all files at once)
  - `fix: resolve collection import validation issues` (includes fix, test, and documentation update)
- **Examples of bad commits**:
  - `add User class` (too granular)
  - `update` (not descriptive)
  - `WIP` (not complete)

When working on tasks, aim to complete all related changes before suggesting a commit.

## Business Requirements

### Core Features

1. **Multi-Platform Collection Tracking**

   - Paper collections with physical location tracking (decks, binders, storage boxes)
   - MTG Arena integration via Windows desktop client (DLL injection)
   - MTG Online via CSV/dat file exports
   - Support for full imports and delta/diff updates

2. **Import/Export Formats**

   - Moxfield CSV
   - Manabox CSV
   - Generic CSV formats
   - MTGO collection exports
   - Future: Archidekt, Deckbox, TCGPlayer

3. **Decklist Management**

   - Store multiple decklists per user
   - Track which physical cards are allocated to which deck
   - Validate deck legality (format, banned cards)

4. **User Accounts & Authentication**

   - Email/password registration
   - JWT-based authentication
   - Multi-device support

5. **Platform Support**
   - Responsive web application (primary)
   - Future: Native mobile apps (iOS/Android)
   - Desktop client for MTGA integration (Windows only)

### Non-Functional Requirements

- **Cost**: Stay under $150/month (primary goal is learning, not production scale)
- **Security**: Secure authentication, HTTPS only, SQL injection prevention
- **Performance**: Sub-second API responses, efficient card searches
- **Scalability**: Support 10-100 users (learning environment, not production)
- **Maintainability**: Well-documented, automated CI/CD, infrastructure as code

### Deployment Philosophy

**GitHub Actions First**: All deployments to Azure are triggered and managed through GitHub Actions workflows:

- **No manual deployments**: Azure Portal used only for monitoring, not deployment
- **Infrastructure as Code**: Bicep templates deployed via GitHub workflows
- **Automated testing**: All code tested before deployment
- **Environment promotion**: Dev → Staging → Production via GitHub
- **Rollback capability**: Git-based deployment history
- **Audit trail**: All changes tracked in GitHub Actions logs

**Benefits for Learning**:

- Hands-on experience with modern DevOps practices
- Understanding GitHub Actions workflow syntax
- Azure service principal configuration and OIDC
- Multi-environment management
- Automated testing integration
- Secret management best practices

**GitHub Actions Costs**:

- **Public Repository**: Unlimited minutes FREE
- **Private Repository**: 2,000 free minutes/month (GitHub Free tier)
  - Linux runners: 1x multiplier (~100 builds/month)
  - Windows runners: 2x multiplier (~50 builds/month for WPF)
  - Additional minutes: $0.008/minute (Linux), $0.016/minute (Windows)
- **Recommendation**: Use public repo for learning (unlimited free) or private with GitHub Pro ($4/month = 3,000 minutes)
- **Monitoring**: Track usage in GitHub Settings → Billing

**Estimated Usage** (learning project with daily commits):

- Backend builds: 5 min × 30/month = 150 minutes
- Frontend builds: 5 min × 30/month = 150 minutes
- Desktop builds: 5 min × 10/month = 50 minutes (100 counted on Windows)
- Infrastructure deploys: 5 min × 5/month = 25 minutes
- **Total**: ~425 minutes/month (well under 2,000 free limit)

## Architecture

### Technology Stack

#### Backend

- **Framework**: ASP.NET Core 10 Web API (.NET 10)
- **Database**: PostgreSQL (Azure Database for PostgreSQL - Flexible Server, Burstable B1ms tier)
- **ORM**: Entity Framework Core 10
- **Authentication**: ASP.NET Core Identity + JWT tokens
- **Hosting**: Azure App Service (Linux, B1 tier) OR Azure Container Apps (Consumption)

**Why ASP.NET Core?**

- Natural fit with existing C# codebase (MTGA injector)
- Excellent performance and cross-platform support
- Built-in dependency injection, configuration, and middleware
- EF Core provides type-safe database access with migrations
- Strong Azure integration

**Why PostgreSQL over SQL Server?**

- Cost: $12/month vs $15/month for basic tiers
- Open source with excellent Azure support
- EF Core has first-class PostgreSQL support (Npgsql)
- Better JSON support for flexible card data storage

#### Frontend

- **Framework**: Blazor WebAssembly (.NET 10)
- **Build Tool**: .NET SDK (no Node.js required)
- **UI Library**: MudBlazor or Blazorise (Bootstrap/Material Design components)
- **State Management**: Built-in Blazor state management + HTTP client
- **Routing**: Blazor Router
- **Hosting**: Azure Static Web Apps (Free tier)

**Why Blazor WebAssembly?**

- **Full C# Stack**: Share DTOs and code between backend, frontend, and desktop
- **Type Safety**: Compile-time type checking across entire application
- **No TypeScript Generation**: Use C# models directly in frontend
- **Single Language**: No context switching between C# and JavaScript
- **Learning Focus**: Deep dive into .NET ecosystem
- **Azure Integration**: First-class support in Static Web Apps

**Trade-offs Accepted**:

- Larger initial download (~2-3 MB vs React's ~200KB)
- Smaller ecosystem than React
- Can be swapped for React/Vue later if needed (separate project)

#### Desktop Client (MTGA Integration)

- **Framework**: WPF (.NET 10) with modern UI (MaterialDesignInXAML or ModernWpf)
- **Auto-Update**: Squirrel.Windows OR ClickOnce deployment
- **Components**:
  - MTGALogParser (C# library)
    - Reads MTGA log files (explicitly authorized by "Plugin Support" setting)
    - Parses JSON payloads containing collection data
    - Uses FileSystemWatcher for real-time updates
    - ToS-compliant, same approach as 17Lands and MTGA Assistant
  - MTGADesktopClient (WPF application)
    - User interface for collection management
    - Integrates MTGALogParser for MTGA sync
    - API client for uploading collections to backend
    - Auto-update support via Squirrel.Windows

**Why WPF over WinForms?**

- Modern XAML-based UI with better styling options
- MVVM pattern for testability
- Better support for async operations
- Native Windows integration

**Log Parsing Advantages**:

- ✅ **ToS Compliant**: Explicitly authorized via "Detailed Logs (Plugin Support)" setting
- ✅ **Safe for Users**: No account termination risk
- ✅ **Industry Standard**: 17Lands, MTGA Assistant use this method
- ✅ **Cross-Platform**: Works on Windows and macOS
- ✅ **No Admin Required**: Standard file read permissions
- ✅ **Reliable**: Stable JSON format, doesn't break with updates

#### Shared Components

- **MTGCollectionTracker.Shared** (.NET Standard 2.1)
  - DTOs (Data Transfer Objects)
  - API contracts
  - Validation models
  - Used by backend, desktop client, and Blazor frontend

#### Utilities

- **ScryfallSync** (.NET 10 Console App)
  - Downloads Scryfall bulk data (https://api.scryfall.com/bulk-data)
  - Populates PostgreSQL database with card metadata
  - Runs daily via Azure Function OR scheduled job
  - Caches 111,000+ cards locally to avoid API rate limits

### Azure Infrastructure

#### Recommended Architecture (Cost-Optimized)

```
┌─────────────────────────────────────────────────────────┐
│                     Azure Front Door                     │
│                    (Optional - $35/mo)                   │
└─────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┴───────────────────┐
        │                                       │
┌───────▼──────────┐                 ┌─────────▼────────┐
│  Static Web Apps │                 │   App Service    │
│   (Free Tier)    │                 │  (Linux B1 tier) │
│  Blazor Frontend │                 │   .NET 10 API    │
│                  │                 │   $13/month      │
└──────────────────┘                 └─────────┬────────┘
                                               │
                            ┌──────────────────┼──────────────────┐
                            │                  │                  │
                 ┌──────────▼──────┐  ┌────────▼────────┐  ┌─────▼──────┐
                 │   PostgreSQL    │  │  Blob Storage   │  │ Key Vault  │
                 │ Flexible Server │  │  (Desktop       │  │ (Secrets)  │
                 │  (B1ms tier)    │  │   downloads)    │  │  $0.03/mo  │
                 │   $12/month     │  │   $1/month      │  └────────────┘
                 └─────────────────┘  └─────────────────┘
```

**Total Monthly Cost**: ~$26/month (well under $150/month budget)

**Learning Focus**: This architecture allows experimentation with:

- **GitHub Actions CI/CD**: Automated build, test, and deployment pipelines
- **Azure App Service**: PaaS hosting with GitHub integration
- **Azure Static Web Apps**: Native GitHub Actions deployment
- **PostgreSQL**: Managed database deployment via GitHub workflows
- **Azure Bicep**: Infrastructure as Code deployed from GitHub
- **Cost monitoring**: Automated alerts and optimization
- **Secrets Management**: GitHub secrets for Azure credentials
- **Multi-environment deployments**: Dev/staging/prod workflows

**Alternative Ultra-Cheap Architecture** (<$10/month for comparison):

```
- Azure Static Web Apps (Free tier) - Frontend
- Azure Functions (Consumption plan) - Backend API - $0 (1M free requests)
- Cosmos DB (Free tier: 1000 RU/s) - Database - $0
- Blob Storage (Standard LRS) - $1/month
Total: ~$1/month
```

**Trade-offs**: Cosmos DB has learning curve, no SQL compatibility, limited free tier storage (25 GB)

#### Infrastructure as Code

- **Tool**: Azure Bicep (preferred over ARM templates and Terraform)
- **Deployment**: GitHub Actions workflows (not manual Azure CLI)
- **Location**: `infrastructure/main.bicep`
- **Modules**:
  - `web-app.bicep` - Static Web Apps + App Service
  - `database.bicep` - PostgreSQL with firewall rules
  - `storage.bicep` - Blob storage for desktop client downloads
  - `monitoring.bicep` - Cost alerts, Application Insights

**GitHub Actions Integration**:

- Azure login via OIDC (no stored credentials)
- Bicep validation in PR builds
- Automated deployment on merge to main
- Separate workflows for dev/staging/prod
- Drift detection and remediation

**Why Bicep over Terraform?**

- Native Azure support with better type checking
- Simpler syntax than ARM templates
- Automatic dependency resolution
- Free Azure Policy integration
- No state file management needed
- Excellent GitHub Actions integration

#### Cost Management

- **Budget Alerts**: Set at $75, $125, $150/month via Azure Cost Management
- **Action Groups**: Email notifications to project owner
- **Monitoring**: Weekly cost reviews in Azure Portal
- **Philosophy**: Cost-conscious but not restrictive - prioritize learning over penny-pinching
- **Optimization**: Use appropriate tiers for learning (Burstable for dev, can scale up for testing)

### Database Schema (Initial Design)

```sql
-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Card metadata (synced from Scryfall)
CREATE TABLE cards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scryfall_id UUID UNIQUE NOT NULL,
    oracle_id UUID, -- Links card faces/printings
    name VARCHAR(255) NOT NULL,
    set_code VARCHAR(10) NOT NULL,
    collector_number VARCHAR(20) NOT NULL,
    rarity VARCHAR(20),
    mana_cost VARCHAR(100),
    type_line TEXT,
    oracle_text TEXT,
    colors JSONB, -- Array of color codes
    cmc DECIMAL(5,2), -- Converted mana cost
    image_uris JSONB, -- URLs for card images
    prices JSONB, -- USD, EUR, TIX prices
    legalities JSONB, -- Format legality (standard, modern, etc.)
    arena_id INT, -- MTG Arena GrpId
    mtgo_id INT, -- MTGO card ID
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(set_code, collector_number)
);

-- User collections (many-to-many with quantities)
CREATE TABLE collection_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    card_id UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    platform VARCHAR(20) NOT NULL, -- 'paper', 'arena', 'mtgo'
    quantity INT NOT NULL DEFAULT 1,
    foil_quantity INT DEFAULT 0, -- For paper/MTGO
    location VARCHAR(255), -- For paper: 'Deck: Goblins', 'Binder 1 Page 3', etc.
    notes TEXT,
    acquired_date DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, card_id, platform, location)
);

-- Decklists
CREATE TABLE decklists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    format VARCHAR(50), -- 'standard', 'modern', 'commander', etc.
    platform VARCHAR(20), -- 'paper', 'arena', 'mtgo'
    description TEXT,
    is_public BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Decklist entries
CREATE TABLE decklist_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    decklist_id UUID NOT NULL REFERENCES decklists(id) ON DELETE CASCADE,
    card_id UUID NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 1,
    is_sideboard BOOLEAN DEFAULT FALSE,
    is_commander BOOLEAN DEFAULT FALSE,
    category VARCHAR(50), -- 'land', 'creature', 'removal', etc. (optional)
    created_at TIMESTAMP DEFAULT NOW()
);

-- Import history (for tracking deltas)
CREATE TABLE import_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    platform VARCHAR(20) NOT NULL,
    import_type VARCHAR(20) NOT NULL, -- 'full', 'delta'
    source VARCHAR(50), -- 'moxfield', 'manabox', 'mtga_client', etc.
    cards_added INT DEFAULT 0,
    cards_removed INT DEFAULT 0,
    cards_updated INT DEFAULT 0,
    file_name VARCHAR(255),
    imported_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_collection_user ON collection_entries(user_id);
CREATE INDEX idx_collection_platform ON collection_entries(platform);
CREATE INDEX idx_cards_oracle ON cards(oracle_id);
CREATE INDEX idx_cards_name ON cards(name);
CREATE INDEX idx_cards_set ON cards(set_code);
CREATE INDEX idx_cards_arena ON cards(arena_id) WHERE arena_id IS NOT NULL;
CREATE INDEX idx_decklists_user ON decklists(user_id);
```

## Project Structure

```
mtg-collection-tracker/
├── .github/
│   ├── copilot-instructions.md (this file)
│   └── workflows/
│       ├── backend-ci.yml          # Build/test/deploy API
│       ├── frontend-ci.yml         # Build/deploy Blazor app
│       ├── desktop-ci.yml          # Build/sign/publish WPF client
│       ├── infrastructure-ci.yml   # Deploy Bicep templates
│       └── scryfall-sync.yml       # Scheduled card sync
│
├── docs/
│   ├── ARCHITECTURE.md            # System design diagrams
│   ├── DECISIONS.md               # Technology choices & rationale
│   ├── API.md                     # API endpoint documentation
│   ├── DATABASE.md                # Schema documentation
│   ├── DEPLOYMENT.md              # Azure deployment guide
│   └── DEVELOPMENT.md             # Local dev setup
│
├── src/
│   ├── backend/
│   │   ├── MTGCollectionTracker.Api/              # ASP.NET Core Web API
│   │   │   ├── Controllers/                       # API endpoints
│   │   │   ├── Services/                          # Business logic
│   │   │   ├── Middleware/                        # Auth, logging, etc.
│   │   │   ├── Program.cs
│   │   │   └── appsettings.json
│   │   ├── MTGCollectionTracker.Data/             # EF Core + repositories
│   │   │   ├── Entities/                          # Database models
│   │   │   ├── Migrations/                        # EF migrations
│   │   │   ├── Repositories/                      # Data access layer
│   │   │   └── AppDbContext.cs
│   │   └── MTGCollectionTracker.Tests/            # Unit tests
│   │
│   ├── frontend/
│   │   ├── wwwroot/                               # Static assets
│   │   ├── Pages/                                 # Blazor pages
│   │   ├── Shared/                                # Shared components
│   │   ├── Services/                              # API client services
│   │   ├── Models/                                # View models
│   │   ├── App.razor
│   │   ├── Program.cs
│   │   └── MTGCollectionTracker.Client.csproj
│   │
│   ├── desktop/
│   │   ├── MTGALogParser/                         # C# library for log parsing
│   │   │   ├── Models/                            # Collection data models
│   │   │   ├── Parsers/                           # JSON log parsers
│   │   │   ├── FileWatcher/                       # Real-time log monitoring
│   │   │   └── MTGALogParser.csproj
│   │   ├── MTGADesktopClient/                     # WPF app
│   │   │   ├── ViewModels/                        # MVVM view models
│   │   │   ├── Views/                             # XAML views
│   │   │   ├── Services/                          # Log parser integration
│   │   │   ├── App.xaml
│   │   │   └── MainWindow.xaml
│   │   ├── LOG_PARSING_RESEARCH.md                # Implementation guide
│   │   └── MTGADesktopClient.Installer/           # Squirrel installer
│   │
│   ├── shared/
│   │   └── MTGCollectionTracker.Shared/           # .NET Standard 2.1
│   │       ├── DTOs/                              # Request/response models
│   │       ├── Enums/                             # Platform, Rarity, etc.
│   │       └── Validators/                        # FluentValidation rules
│   │
│   └── utilities/
│       └── ScryfallSync/                          # Console app
│           ├── Program.cs                         # Download & parse Scryfall
│           └── CardImporter.cs                    # Bulk insert to DB
│
├── infrastructure/
│   ├── main.bicep                                 # Main deployment
│   ├── modules/
│   │   ├── web-app.bicep                          # App Service + Static Web
│   │   ├── database.bicep                         # PostgreSQL
│   │   ├── storage.bicep                          # Blob storage
│   │   └── monitoring.bicep                       # Alerts + App Insights
│   └── parameters/
│       ├── dev.json                               # Dev environment params
│       └── prod.json                              # Production params
│
├── tests/
│   ├── integration/                               # API integration tests
│   └── e2e/                                       # Playwright tests (future)
│
├── .gitignore
├── README.md                                      # User-facing documentation
└── MTGCollectionTracker.sln                       # Root solution file
```

## Development Guidelines

### Code Style

- **C#**: Follow Microsoft C# Coding Conventions

  - Use `var` for obvious types
  - Async methods end with `Async` suffix
  - Prefer dependency injection over static classes
  - Use records for DTOs
  - XML documentation for public APIs
  - **Type Safety**: Use specific types instead of strings (e.g., `Guid` for IDs, `DateTime` for timestamps, enums for known values)
  - Leverage the type system for compile-time safety rather than runtime string parsing
  - **Project Settings**: Common settings (`Nullable`, `ImplicitUsings`, `LangVersion`, `TreatWarningsAsErrors`) are configured in `Directory.Build.props` at the repository root and automatically apply to all projects. Individual `.csproj` files should not override these unless absolutely necessary.

- **Blazor/Razor**: Follow Blazor best practices

  - Use code-behind files for complex component logic
  - Inject services via `@inject` directive
  - Use `@bind` for two-way data binding
  - Implement `IDisposable` for components with subscriptions
  - Use `StateHasChanged()` when updating state outside event handlers

- **SQL**: Use snake_case for table/column names (PostgreSQL convention)

### Git Workflow

- **Branch Naming**: `feature/description`, `bugfix/description`, `docs/description`
- **Commits**: Conventional Commits (feat:, fix:, docs:, chore:)
- **Pull Requests**: Required for merging to `main`
- **Main Branch**: Always deployable, protected

### Testing Strategy

- **Backend**: Minimum 80% code coverage with MSTest, NSubstitute, and Shouldly

  - Unit tests for services and repositories
  - Integration tests for API endpoints
  - Use TestContainers for PostgreSQL in tests

- **Frontend**: bUnit for Blazor component testing

  - Component tests for critical UI
  - Mock services with test doubles

- **Desktop**: Manual testing initially (UI automation complex for WPF)

### Documentation

- **Architecture Decisions**: Create separate ADR files in docs/decisions/ following format ADR-XXX-title-in-kebab-case.md, update docs/DECISIONS.md index with link
- **API Changes**: Update docs/API.md and OpenAPI/Swagger spec
- **Database Changes**: Document migrations with comments
- **README**: Keep user-facing, include screenshots

## Security Considerations

### Authentication & Authorization

- **Password Storage**: BCrypt with work factor 12+
- **JWT Tokens**: Short-lived access tokens (15 min) + refresh tokens (7 days)
- **HTTPS Only**: Enforce in production via Azure Front Door or App Service
- **CORS**: Whitelist only frontend domain
- **Rate Limiting**: Implement in API middleware (1000 requests/hour per user)

### Data Protection

- **Secrets Management**: Azure Key Vault for connection strings, API keys
- **Connection Strings**: Never commit to Git, use environment variables
- **SQL Injection**: Use parameterized queries (EF Core does this by default)
- **XSS Protection**: Blazor/Razor escapes by default, validate user input

### Desktop Client Security

- **Code Signing**: Sign WPF application with authenticode certificate
- **Update Verification**: Validate update packages with digital signatures
- **DLL Injection**: Educational use disclosure, no anti-cheat bypass

## CI/CD Pipeline

**Primary Goal**: Leverage GitHub Actions for all build, test, and deployment automation to gain hands-on DevOps experience.

**Key Principles**:

- **Automation First**: Every deployment goes through GitHub Actions
- **Consistency**: Same process for all environments (dev/staging/prod)
- **Security**: Azure credentials managed via GitHub OIDC, secrets in Key Vault
- **Testing**: Automated tests gate all deployments
- **Observability**: All workflows logged and monitored

### GitHub Actions Workflows

#### Backend CI (`backend-ci.yml`)

```yaml
Trigger: Push to main, PRs to main, paths: src/backend/**
Steps:
  1. Restore dependencies
  2. Build projects
  3. Run unit tests
  4. Run integration tests (with PostgreSQL container)
  5. Publish artifacts
  6. Deploy to Azure App Service (main branch only)
     - Azure login via OIDC
     - Deploy using az webapp up or Web Deploy
     - Run smoke tests post-deployment
     - Rollback on failure
```

#### Frontend CI (`frontend-ci.yml`)

```yaml
Trigger: Push to main, PRs to main, paths: src/frontend/**
Steps:
  1. Setup .NET 10 SDK
  2. Restore dependencies
  3. Build Blazor WebAssembly project
  4. Run tests (if any)
  5. Publish for production
  6. Deploy to Azure Static Web Apps (main branch only)
     - Uses Azure/static-web-apps-deploy action
     - Automatic PR preview environments
     - Production deployment on merge
```

#### Desktop CI (`desktop-ci.yml`)

```yaml
Trigger: Push to main with tag v*, paths: src/desktop/**
Steps:
  1. Build MTGALogParser (.NET 10)
  2. Build MTGADesktopClient (WPF)
  3. Sign binaries with code signing certificate
  4. Create Squirrel release package
  5. Upload to Azure Blob Storage
  6. Update version endpoint for auto-update
```

#### Infrastructure CI (`infrastructure-ci.yml`)

```yaml
Trigger: Manual workflow_dispatch, paths: infrastructure/**
Steps:
  1. Azure login via OIDC (federated credentials)
  2. Validate Bicep syntax (az bicep build)
  3. Preview changes (what-if deployment)
  4. Deploy to dev environment
  5. Run smoke tests
  6. Deploy to staging (manual approval)
  7. Deploy to production (manual approval + change freeze checks)
  8. Tag release in Git
```

**GitHub Environments**:

- `development` - Auto-deploy on workflow_dispatch
- `staging` - Requires approval from 1 reviewer
- `production` - Requires approval from 2 reviewers + branch protection

## Auto-Update for Desktop Client

### Strategy: Squirrel.Windows

- **Why?**: ClickOnce doesn't support .NET 10, Squirrel is modern and .NET-friendly
- **How It Works**:

  1. Client checks `https://api.example.com/desktop/version` on startup
  2. If new version available, downloads `Releases` folder from blob storage
  3. Applies delta updates (only changed files)
  4. Restarts application with new version

- **Hosting**:
  - Store releases in Azure Blob Storage (`$web/desktop/releases/`)
  - Serve via Azure CDN for fast downloads
  - Version file updated by CI/CD after successful build

## Cost Monitoring Implementation

### Azure Budget Configuration (Bicep)

```bicep
resource budget 'Microsoft.Consumption/budgets@2023-05-01' = {
  name: 'monthly-budget'
  properties: {
    category: 'Cost'
    amount: 150 // $150/month - learning project budget
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '2026-01-01'
    }
    notifications: {
      threshold_50: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50  // $75/month
        contactEmails: ['admin@example.com']
      }
      threshold_83: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 83  // $125/month
        contactEmails: ['admin@example.com']
      }
      threshold_100: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100  // $150/month - AT LIMIT
        contactEmails: ['admin@example.com']
      }
    }
  }
}
```

## Known Limitations & Future Work

### MVP Limitations

1. **No OAuth**: Email/password only (no Google/Microsoft sign-in)
2. **No Mobile Apps**: Responsive web only initially
3. **Basic Search**: No advanced filtering (by color, CMC, format legality)
4. **No Deck Validation**: Can add any cards, no format checking yet
5. **MTGA Only on Windows**: DLL injection requires Windows

### Future Enhancements

- **Advanced Search**: Scryfall-style query language
- **Price Tracking**: Historical price charts using Scryfall data
- **Deck Statistics**: Mana curve, color distribution, CMC average
- **Trade Finder**: Match collection with wanted lists
- **Deck Recommendations**: ML-based suggestions from collection
- **Social Features**: Share collections, public profiles
- **Card Condition Tracking**: NM, LP, MP, HP for paper cards
- **Sealed Product Tracking**: Track unopened booster boxes, etc.

## API Endpoints (Initial Design)

### Authentication

- `POST /api/auth/register` - Create new account
- `POST /api/auth/login` - Get JWT tokens
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Invalidate refresh token

### Collections

- `GET /api/collections` - Get all user collections (grouped by platform)
- `GET /api/collections/{platform}` - Get specific platform collection
- `POST /api/collections/import` - Import collection (CSV, JSON)
- `POST /api/collections/sync` - Sync MTGA collection (from desktop client)
- `DELETE /api/collections/{platform}` - Clear platform collection
- `GET /api/collections/export?format=moxfield` - Export collection

### Cards

- `GET /api/cards/search?q=lightning+bolt` - Search cards
- `GET /api/cards/{id}` - Get card details
- `GET /api/cards/oracle/{oracleId}` - Get all printings of card

### Decklists

- `GET /api/decklists` - Get user's decklists
- `POST /api/decklists` - Create new decklist
- `GET /api/decklists/{id}` - Get decklist details
- `PUT /api/decklists/{id}` - Update decklist
- `DELETE /api/decklists/{id}` - Delete decklist
- `POST /api/decklists/{id}/validate` - Check format legality

### Desktop Client

- `GET /api/desktop/version` - Get latest client version
- `GET /api/desktop/download` - Redirect to installer download

### Admin (Future)

- `POST /api/admin/scryfall/sync` - Trigger Scryfall sync
- `GET /api/admin/stats` - Platform usage statistics

## Scryfall Integration

### Bulk Data Sync Process

1. **Download**: GET https://api.scryfall.com/bulk-data/default-cards
2. **Parse**: JSON file with ~111,000 cards (80 MB compressed, 600 MB uncompressed)
3. **Filter**: Only cards with `digital: true` or paper printings
4. **Transform**: Map Scryfall JSON to database schema
5. **Upsert**: Insert new cards, update existing (by scryfall_id)
6. **Schedule**: Run daily at 3 AM UTC via Azure Function Timer Trigger

### Rate Limiting

- Bulk data endpoint: No rate limit (encouraged by Scryfall)
- Search API: 10 requests/second (not used for bulk sync)
- Cache bulk data locally for 24 hours

### Data Mapping Challenges

- **Arena ID Mapping**: Only ~16,000 cards have `arena_id`, use name+set matching for others
- **MTGO ID Mapping**: Similar issue, fallback to name matching
- **Set Code Differences**: Arena uses different codes (e.g., `ANA` vs `oana`)
  - Solution: Store both Scryfall and Arena set codes in mapping table

## Environment Variables

### Backend (App Service)

```bash
ConnectionStrings__DefaultConnection="Host=xxx.postgres.database.azure.com;Database=mtgtracker;Username=xxx;Password=xxx"
JwtSettings__Secret="<generated-secret>"
JwtSettings__Issuer="https://api.example.com"
JwtSettings__Audience="https://example.com"
JwtSettings__ExpiryMinutes="15"
AzureStorage__ConnectionString="<blob-storage-connection>"
AzureStorage__DesktopContainer="desktop-releases"
Scryfall__BulkDataUrl="https://api.scryfall.com/bulk-data/default-cards"
```

### Frontend (Static Web Apps)

```bash
VITE_API_BASE_URL="https://api.example.com"
VITE_ENVIRONMENT="production"
```

### Desktop Client (app.config)

```xml
<appSettings>
  <add key="ApiBaseUrl" value="https://api.example.com" />
  <add key="UpdateUrl" value="https://example.com/desktop/releases" />
</appSettings>
```

## Performance Targets

- **API Response Time**: <200ms (95th percentile)
- **Frontend Load Time**: <2 seconds (on cable/fiber)
- **Collection Import**: <5 seconds for 10,000 cards
- **Card Search**: <100ms for autocomplete
- **Database Queries**: <50ms for indexed lookups
- **MTGA Extraction**: <10 seconds (existing tool baseline)

## Disaster Recovery

### Database Backups

- **Automated**: PostgreSQL point-in-time restore (35 days retention)
- **Manual**: Weekly full backup to Azure Blob Storage
- **Recovery Time Objective (RTO)**: 1 hour
- **Recovery Point Objective (RPO)**: 5 minutes

### Deployment Rollback

- **Strategy**: Blue-green deployment with Azure deployment slots
- **Rollback Time**: <5 minutes (swap slots)
- **Database Migrations**: Always backward-compatible for 1 release

## Support & Maintenance

### Monitoring

- **Application Insights**: Track API errors, performance, usage
- **Health Checks**: /health endpoint for uptime monitoring
- **Alerts**: Email on 5xx errors, high response times, cost thresholds

### Logging

- **Structured Logging**: Serilog with JSON output
- **Log Levels**: Debug (dev), Information (prod)
- **Sensitive Data**: Never log passwords, tokens, connection strings

### Versioning

- **API Versioning**: URL-based (`/api/v1/...`)
- **Desktop Client**: Semantic versioning (MAJOR.MINOR.PATCH)
- **Database Migrations**: Sequential numbering with timestamps

## Contributing Guidelines

### Before Starting Work

1. Check existing issues and PRs
2. Create issue for new features
3. Get approval for large changes
4. Create feature branch from `main`

### Pull Request Process

1. Write tests for new features
2. Update documentation
3. Run `dotnet format` for C# code
4. Ensure CI passes
5. Request review from maintainer

### Code Review Checklist

- [ ] Code follows style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No security vulnerabilities
- [ ] No performance regressions
- [ ] Breaking changes documented

---

## Quick Reference

### Local Development Setup

```bash
# Clone repository
git clone https://github.com/YOUR_USERNAME/mtg-collection-tracker.git
cd mtg-collection-tracker

# Backend
cd src/backend/MTGCollectionTracker.Api
dotnet restore
dotnet ef database update
dotnet run

# Frontend
cd src/frontend
dotnet restore
dotnet run

# Desktop Client (when available)
cd src/desktop/MTGALogParser
dotnet restore
dotnet run
```

### Useful Commands

```bash
# Create EF migration
dotnet ef migrations add MigrationName -p MTGCollectionTracker.Data -s MTGCollectionTracker.Api

# Apply migrations
dotnet ef database update -p MTGCollectionTracker.Data -s MTGCollectionTracker.Api

# Build all projects
dotnet build MTGCollectionTracker.sln

# Run tests
dotnet test

# Deploy infrastructure
az deployment group create --resource-group mtg-tracker-rg --template-file infrastructure/main.bicep --parameters infrastructure/parameters/prod.json
```

---

**Last Updated**: January 12, 2026
**Project Status**: Phase 1 - Initial Setup
**Next Milestone**: Phase 2 - Migrate Existing MTGA Code
