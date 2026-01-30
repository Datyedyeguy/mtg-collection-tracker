# MTG Collection Tracker - Roadmap

**Last Updated**: January 13, 2026
**Current Phase**: Phase 2 - Backend Foundation & Frontend Development

---

## Project Status

### ✅ Completed

#### Phase 1: Initial Setup (Completed)

- [x] Create monorepo directory structure
- [x] Initialize Git repository with .gitignore
- [x] Create copilot-instructions.md
- [x] Create architecture documentation (ARCHITECTURE.md, DECISIONS.md, etc.)
- [x] Add EditorConfig for code formatting standards
- [x] Set up solution file with utilities folder structure
- [x] **Technology Stack Decisions (All 15 ADRs confirmed)**
  - [x] ADR-001: Monorepo structure
  - [x] ADR-002: ASP.NET Core 10 backend
  - [x] ADR-003: PostgreSQL database
  - [x] ADR-004: Blazor WebAssembly frontend
  - [x] ADR-005: Azure App Service hosting
  - [x] ADR-006: Avalonia UI for desktop (cross-platform)
  - [x] ADR-007: Velopack for auto-updates
  - [x] ADR-008: Bicep for infrastructure
  - [x] ADR-009: JWT + Refresh Tokens authentication
  - [x] ADR-010: Scryfall-only data source
  - [x] ADR-011: Built-in HttpClient
  - [x] ADR-012: Self-signed certificates initially
  - [x] ADR-013: GitHub Actions for CI/CD
  - [x] ADR-014: Cost alerts at $75/$125/$150
  - [x] ADR-015: MTGA log parsing approach

---

## 🚧 Current Focus

### Phase 2: Backend Foundation & Frontend Development (In Progress)

**Development Strategy**: Build API and web frontend together locally with tests, then deploy to Azure once MVP is functional.

**NEXT IMMEDIATE TASKS:**

- [x] **Set up GitHub repository** ✅
  - [x] Create remote repository on GitHub
  - [x] Configure repository settings (branch protection, etc.)
  - [x] Push initial codebase
  - [ ] Set up GitHub Secrets for Azure (defer until Phase 4)

**Priority Tasks:**

- [x] **Create Shared DTOs project** ✅
  - [x] MTGCollectionTracker.Shared (.NET Standard 2.1)
  - [x] Define DTOs for API contracts (Auth DTOs)
  - [ ] Validation attributes and models (deferred - using Data Annotations)
- [x] **Create ASP.NET Core 10 Web API project** ✅
  - [x] Project structure (Controllers, Services, Middleware)
  - [x] Configure Kestrel and HTTPS
  - [ ] ~~Add Swagger/OpenAPI documentation~~ (skipped - using .http files)
  - [x] Configure dependency injection
- [x] **Set up Entity Framework Core with PostgreSQL** ✅
  - [x] Create MTGCollectionTracker.Data project
  - [x] Install Npgsql.EntityFrameworkCore.PostgreSQL
  - [x] Create DbContext (AppDbContext.cs)
  - [x] Define entity models (User, RefreshToken - Card, CollectionEntry, Decklist pending)
  - [x] Configure relationships and indexes
  - [x] Generate and apply first migration
- [x] **Implement authentication system** ✅
  - [x] ASP.NET Core Identity setup
  - [x] JWT token generation and validation middleware
  - [x] Refresh token mechanism (with SHA256 hashing)
  - [x] Auth endpoints (/api/auth/register, /api/auth/login, /api/auth/refresh, /api/auth/logout)
  - [x] Password hashing (Identity uses PBKDF2 by default)
- [ ] **Build core API endpoints**
  - [ ] Collections endpoints (GET, POST, DELETE)
  - [ ] Cards search endpoint
  - [ ] User profile endpoints
  - [x] Health check endpoint ✅
- [x] **Create test projects** ✅
  - [x] MTGCollectionTracker.Api.Tests (unit tests with MSTest, NSubstitute, Shouldly)
  - [x] MTGCollectionTracker.Client.Tests (frontend service tests)
  - [ ] Integration tests infrastructure (TestContainers for PostgreSQL) - deferred
  - [x] Test JwtService (28 tests)
  - [x] Test CustomAuthStateProvider (token parsing, auth state management)
  - [x] Test TokenStorageService (localStorage interactions)
  - [x] AuthServiceTests created (documented test requirements, HTTP mocking deferred)
  - [ ] Test auth flows, API endpoints

  **Frontend Testing Strategy (Deferred):**
  - Unit tests for services implemented (CustomAuthStateProvider, TokenStorageService)
  - AuthService tests documented but require HTTP mocking library (RichardSzalay.MockHttp)
  - bUnit component tests deferred to Phase 3+ (requires separate GitHub Actions workflow)
  - Focus on backend service tests for MVP, expand frontend testing after Azure deployment
  - Integration tests with TestContainers also deferred (GitHub Actions resource considerations)

- [x] **Create Blazor WebAssembly project** ✅
  - [x] Initial project created with Bootstrap CSS
  - [ ] Configure MudBlazor or Blazorise UI components (using vanilla Bootstrap for now)
  - [x] Set up routing and navigation
  - [x] Configure HttpClient with base URL
  - [x] API client service classes (AuthService)
- [x] **Implement authentication UI** ✅
  - [x] Login page with form validation and password visibility toggle
  - [x] Registration page with password requirements (12 char minimum)
  - [x] Token storage (localStorage via TokenStorageService)
  - [x] Auth state management (CustomAuthStateProvider)
  - [x] Navigation menu with AuthorizeView (Login/Register when anonymous, Logout when authenticated)
  - [ ] Protected routes (can add [Authorize] to pages as needed)
- [ ] **Build basic collection view**
  - [ ] Display user's collection
  - [ ] Card search and filtering
  - [ ] Basic pagination
- [x] **Local development setup** ✅
  - [x] Docker Compose for PostgreSQL (local dev database)
  - [x] Connection string configuration
  - [ ] Run API and frontend together locally
  - [ ] Test end-to-end user flows

**Decisions Needed:**

- [ ] **API Versioning Strategy** - Decide whether/when to add versioning
  - Current: `/api/auth/login` (no version)
  - Option 1: Add `/api/v1/` now for practice
  - Option 2: Defer until desktop client ships (Phase 5)
  - Option 3: Defer until breaking changes needed
  - Consider: URL versioning vs header versioning vs query string
  - Note: ApiRoutes.cs structure already supports easy migration to versioned routes

**Technical Debt / Warnings:**

- [ ] **Blazor WebAssembly dotnet watch warning** - .NET Standard 2.1 Shared project causes hot reload warnings
  - Warning: "Found project reference without a matching metadata reference"
  - Root cause: Mixing .NET Standard 2.1 (Shared) with .NET 10 (Blazor WebAssembly)
  - Impact: Cosmetic warning, hot reload doesn't work for Shared project changes (requires manual rebuild)
  - Solution options:
    - Option 1: Upgrade MTGCollectionTracker.Shared from netstandard2.1 to net10.0
    - Option 2: Multi-target Shared project: `<TargetFrameworks>netstandard2.1;net10.0</TargetFrameworks>`
    - Option 3: Accept warning (not recommended - warnings should be treated as errors)
  - Decision: Should be resolved before Phase 4 (Azure deployment) - build warnings in CI/CD pipelines
  - Recommendation: Option 1 (upgrade to net10.0) - simplest, no desktop client constraints yet

- [ ] **Pre-commit validation enforcement** - Prevent broken builds from reaching CI/CD
  - Problem: Debug builds pass locally but Release builds fail in GitHub Actions (e.g., unused field warnings)
  - Current: Manual validation required (`dotnet build --configuration Release`)
  - Solution options:
    - Option 1: Git pre-commit hooks (runs Release build before commit)
    - Option 2: GitHub Actions status check required before merge (already in place, but reactive)
    - Option 3: VS Code task with Release build + test (`Ctrl+Shift+B` or command palette)
    - Option 4: Document best practice in CONTRIBUTING.md ("always test Release build before push")
  - Recommendation: Combination of Option 1 (pre-commit hook) + Option 4 (documentation)
  - Benefits: Catches compiler warnings treated as errors, ensures CI/CD parity locally
  - Note: Pre-commit hooks can be bypassed with `git commit --no-verify` if needed

**Nice to Have (Defer to Later):**

- [ ] **HTTP mocking strategy for frontend tests (Decision pending - see ADR-018)**
  - [ ] Review ADR-018 and decide on approach (RichardSzalay.MockHttp vs alternatives)
  - [ ] Once decided, add package to MTGCollectionTracker.Client.Tests
  - [ ] Implement AuthService tests (currently documented with [Ignore] attributes)
  - [ ] Verify API endpoint paths match controller routes (catches refactoring issues)
- [ ] Build Scryfall sync utility (ScryfallSync console app) - defer until API is stable
- [ ] Structured logging (Serilog) - can add later
  - [ ] Add logging to frontend (Blazor console logging or remote logging)
  - [ ] Improve exception handling in CustomAuthStateProvider (specific exception types, TryParse for token claims)
- [ ] Advanced error handling - start simple

---

## 📋 Upcoming Phases

### Phase 3: Scryfall Integration & Card Data

**Key Deliverables:**

- [ ] Build ScryfallSync console app
  - [ ] Download bulk data from Scryfall API
  - [ ] Parse JSON and map to Card entities
  - [ ] Bulk insert to PostgreSQL (~111k cards)
  - [ ] Handle updates and new sets
- [ ] Optimize card search queries
  - [ ] Add full-text search indexes
  - [ ] Implement autocomplete endpoint
  - [ ] Cache frequent searches
- [ ] Test with real card data
  - [ ] Verify all Arena cards mapped correctly
  - [ ] Test collection imports with large datasets
  - [ ] Performance testing with 10k+ card collections

---

### Phase 4: Azure Deployment & CI/CD

**Infrastructure as Code:**

- [ ] Write Bicep templates
  - [ ] main.bicep (entry point)
  - [ ] modules/database.bicep (PostgreSQL Flexible Server)
  - [ ] modules/web-app.bicep (App Service + Static Web Apps)
  - [ ] modules/storage.bicep (Blob storage for desktop client)
  - [ ] modules/monitoring.bicep (Application Insights, cost alerts)
- [ ] Deploy infrastructure to Azure
  - [ ] Create resource group
  - [ ] Deploy dev environment
  - [ ] Deploy staging environment
  - [ ] Deploy production environment
- [ ] Configure GitHub Actions CI/CD
  - [ ] backend-ci.yml (build, test, deploy API)
  - [ ] **Run EF migrations before deployment** (`dotnet ef database update` in pipeline)
  - [ ] frontend-ci.yml (build, deploy Blazor app)
  - [ ] desktop-ci.yml (build, sign, publish WPF client)
  - [ ] infrastructure-ci.yml (deploy Bicep templates)
  - [ ] **Code style enforcement** (`dotnet format --verify-no-changes` to enforce EditorConfig rules)
  - [ ] Consider StyleCop.Analyzers or Roslyn analyzers for additional compile-time checks
- [ ] Set up cost management
  - [ ] Budget alerts ($75, $125, $150)
  - [ ] Cost anomaly detection
  - [ ] Weekly cost review reminders
- [ ] Deploy applications
  - [ ] Backend API to App Service
  - [ ] Frontend to Static Web Apps
  - [ ] Connection string configuration
  - [ ] Environment variables

**Learning Goals:**

- Hands-on with Azure PaaS services
- GitHub Actions workflow authoring
- OIDC authentication for Azure
- Multi-environment deployment strategies

---

### Phase 5: Desktop Client (MTGA Integration)

**Desktop Client:**

- [ ] Create Avalonia UI application (MTGADesktopClient)
  - [ ] MVVM architecture with ViewModels
  - [ ] Modern UI styling
  - [ ] Cross-platform support (Windows/macOS/Linux)
  - [ ] Settings page (API endpoint, auth)
- [ ] Build MTGALogParser library
  - [ ] FileSystemWatcher for real-time log monitoring
  - [ ] JSON parsing for collection data
  - [ ] Error handling and logging
  - [ ] Cross-platform log file detection (Windows/macOS)
- [ ] Implement auto-update with Velopack
  - [ ] Create installer project
  - [ ] Version checking on startup
  - [ ] Delta update downloads
  - [ ] Restart after update
  - [ ] Cross-platform update support
- [ ] Add API upload functionality
  - [ ] Authenticate with backend API
  - [ ] Upload collection data (JSON)
  - [ ] Display sync status and errors
- [ ] Testing
  - [ ] Test with real MTGA logs
  - [ ] Verify collection accuracy
  - [ ] End-to-end sync workflow

**Hosting:**

- [ ] Sign binaries with code signing certificate
- [ ] Upload releases to Azure Blob Storage
- [ ] Configure CDN for fast downloads
- [ ] Update version endpoint for auto-update checks

---

### Phase 6: Polish & Launch

**Feature Completion:**

- [ ] Add import/export for other formats
  - [ ] Moxfield CSV import
  - [ ] Manabox CSV import
  - [ ] Generic CSV format support
  - [ ] Export to various formats
- [ ] Implement decklist management
  - [ ] Create/edit/delete decklists
  - [ ] Validate deck legality by format
  - [ ] Track card allocation (which deck uses which cards)
  - [ ] Import from Moxfield/Archidekt URLs
- [ ] Add paper location tracking
  - [ ] Specify storage location per card
  - [ ] Location autocomplete (Deck, Binder 1, Box A, etc.)
  - [ ] Filter by location
- [ ] Write user documentation
  - [ ] Getting started guide
  - [ ] Import/export tutorials
  - [ ] FAQ section
  - [ ] Video walkthroughs (optional)
- [ ] Public beta launch
  - [ ] Set up domain and SSL
  - [ ] Create landing page
  - [ ] Announce on Reddit, Discord, social media
  - [ ] Collect feedback

---

## 💡 Future Ideas (Post-MVP)

### Advanced Features

- **Advanced Search**: Scryfall-style query language (color:R type:creature cmc<=3)
- **Price Tracking**: Historical price charts using Scryfall data
- **Deck Statistics**: Mana curve, color distribution, CMC average
- **Trade Finder**: Match collection with wanted lists
- **Deck Recommendations**: ML-based suggestions from collection
- **Social Features**: Share collections, public profiles, follow other users
- **Card Condition Tracking**: NM, LP, MP, HP for paper cards
- **Sealed Product Tracking**: Track unopened booster boxes, precons
- **Wishlist Management**: Track cards you want to acquire
- **Trade History**: Log trades with other players

### Platform Expansion

- **Native Mobile Apps**: iOS and Android apps (React Native or .NET MAUI)
- **OAuth Integration**: Google, Microsoft, Discord sign-in
- **MTGO Auto-Sync**: Monitor collection files and auto-import
- **TCGPlayer Integration**: Price tracking and purchase links
- **Card Market Integration**: European pricing data
- **Archidekt Import**: Support Archidekt deck imports

### Infrastructure Improvements

- **Caching**: Redis for frequently accessed data
- **CDN**: Azure Front Door for global performance
- **Search**: Azure Cognitive Search or Elasticsearch
- **Analytics**: User behavior tracking and insights
- **A/B Testing**: Feature experimentation framework

---

## 🐛 Known Issues & Technical Debt

### Privacy & Compliance

- **IP Address Storage (PII Concern)**: The `RefreshToken` entity stores client IP addresses (`CreatedByIp`, `RevokedByIp`) for security auditing. IP addresses are considered PII under GDPR and similar regulations. Before production use with real users, consider:
  - Documenting data collection in a privacy policy
  - Implementing IP anonymization (e.g., masking last octet: `192.168.1.xxx`)
  - Adding a data retention policy to delete old refresh tokens
  - Making IP logging configurable/optional
  - **Location**: [AuthController.cs](src/backend/MTGCollectionTracker.Api/Controllers/AuthController.cs) `GetClientIpAddress()` method

---

## 📊 Metrics & Goals

### Performance Targets

- API Response Time: <200ms (95th percentile)
- Frontend Load Time: <2 seconds (on cable/fiber)
- Collection Import: <5 seconds for 10,000 cards
- Card Search: <100ms for autocomplete
- Database Queries: <50ms for indexed lookups

### Cost Targets

- Monthly Azure spend: <$150
- GitHub Actions minutes: <2,000/month (free tier)
- Storage: <10 GB (well under limits)

### User Goals (Post-Launch)

- 10-100 users in first month (learning project scale)
- <5% error rate on collection imports
- 90% user satisfaction (based on feedback)

---

## 📝 Notes

- This is a **learning project** focused on Azure, GitHub Actions, and .NET 8
- Priority is learning and experimentation over production-scale features
- Cost consciousness is important but not restrictive
- Feedback and iteration expected throughout development

---

**Contributing**: This is a personal learning project. See [CONTRIBUTING.md](CONTRIBUTING.md) for more information.

**Documentation**: See [docs/](docs/) for architecture, API specs, and development guides.
