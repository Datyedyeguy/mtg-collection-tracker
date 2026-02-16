# MTG Collection Tracker - Roadmap

**Last Updated**: February 16, 2026
**Current Phase**: Phase 3 - Scryfall Integration & Card Management (Scryfall Data Sync Complete)

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

#### Phase 2: Backend Foundation & Frontend Development (Major Progress - Feb 14, 2026)

- [x] **Collections Feature - Viewing Implementation** ✅ (Feb 14, 2026)
  - [x] Card and CollectionEntry entities with full EF configuration
  - [x] Database migration with optimized indexes
  - [x] Composite indexes for performance (UserId + Platform queries)
  - [x] Collections GET API with pagination (GET /api/collections?platform=&page=&pageSize=)
  - [x] Pagination with server-side aggregation (efficient for large datasets)
  - [x] Platform filtering (Paper/Arena/MTGO)
  - [x] Input validation (page/pageSize limits)
  - [x] Collections UI with platform dropdown and pagination controls
  - [x] Empty state handling with helpful messaging
  - [x] Authorization header integration (JWT tokens in requests)
  - [x] Comprehensive tests (12 controller tests, all passing)
  - [x] Performance optimizations (SQL projections, database aggregation)
  - [x] Shared DTOs (CollectionEntryDto, CollectionResponseDto)
  - [x] ApiRoutes updated for collections endpoints
  - [ ] **Collections Management (Phase 3)**: Add/edit/remove cards, search, import/export

#### Phase 2: Infrastructure & Code Quality (Completed Feb 2, 2026)

- [x] **Upgrade to .NET 10**
  - [x] Upgraded MTGCollectionTracker.Shared from netstandard2.1 to net10.0
  - [x] Eliminated Blazor hot reload warnings
  - [x] Consistent .NET 10 across entire solution
- [x] **Pre-Commit Validation**
  - [x] Created `validate-solutions.ps1` script to check solution consistency
  - [x] Created `pre-commit.ps1` hook for Release build validation
  - [x] Created `setup-hooks.ps1` for easy installation
  - [x] Updated CONTRIBUTING.md with developer notes
  - [x] Cross-platform support (Windows batch wrapper, Unix shell script)

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
- [x] **Build core API endpoints** (Partial ✅)
  - [x] Collections GET endpoint with pagination and filtering ✅
  - [ ] Collections POST endpoint (add cards to collection - Phase 3)
  - [ ] Collections PUT endpoint (update quantities - Phase 3)
  - [ ] Collections DELETE endpoint (remove cards - Phase 3)
  - [ ] Cards search endpoint (Phase 3 - needs Scryfall data)ts (add/remove cards - Phase 3)
  - [ ] Cards search endpoint (Phase 3)
  - [ ] User profile endpoints (deferred)
  - [x] Health check endpoint ✅
- [x] **Create test projects** ✅
  - [x] MTGCollectionTracker.Api.Tests (unit tests with MSTest, NSubstitute, Shouldly)
  - [x] MTGCollectionTracker.Client.Tests (frontend service tests)
  - [x] CollectionsController tests (12 tests covering pagination, filtering, auth)
  - [ ] Integration tests infrastructure (TestContainers for PostgreSQL) - deferred
  - [x] Test JwtService (28 tests)
  - [x] Test CustomAuthStateProvider (token parsing, auth state management)
  - [x] Test TokenStorageService (localStorage interactions)
  - [x] AuthServiceTests created (documented test requirements, HTTP mocking deferred)
  - [x] All 80 tests passing ✅ster, /api/auth/login, /api/auth/refresh, /api/auth/logout)
  - [x] Password hashing (Identity uses PBKDF2 by default)
  - [x] Authentication UI (Login, Register, Logout pages with validation)
  - [x] AuthService with error handling and loading states
  - [x] Shared ApiRoutes for compile-time route safety (ADR-019)
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
  - [x] Protected routes with [Authorize] attribute ✅
- [x] **Build basic collection view** ✅ (Feb 14, 2026)
  - [x] Display user's collection with pagination (50 cards/page)
  - [x] Platform filtering dropdown (All/Paper/Arena/MTGO)
  - [x] Pagination controls with smart page number display
  - [x] Empty state with helpful messaging
  - [x] **Advanced collection features (Phase 3)**:
    - [ ] Add cards to collection (needs card search first)
    - [ ] Edit card quantities
    - [ ] Remove cards from collection
    - [ ] Card search and filtering (needs Scryfall data)
    - [ ] Import from CSV/Moxfield/Manabox
    - [ ] Export to various formats
  - [x] Card count display (unique cards vs total copies)
  - [ ] Card search and filtering (Phase 3 - needs Scryfall data)
- [x] **Local development setup** ✅
  - [x] Docker Compose for PostgreSQL (local dev database)
  - [x] Connection string configuration
  - [x] Run API and frontend together locally (dotnet watch)
  - [x] End-to-end user flows working (register → login → view collection)ation
  - [ ] Run API and frontend together locally
  - [ ] Test end-to-end user flows

**Decisions Needed:**

- [ ] **API Versioning Strategy** - Decide whether/when to add versioning
  - Current: `/api/auth/login` (no version)
  - Option 1: Add `/api/v1/` now for practice
  - Option 2: Defer until desktop client ships (Phase 5)
  - Option 3: Defer until breaking changes needed
  - Upgraded MTGCollectionTracker.Shared from netstandard2.1 to net10.0
  - Removed System.ComponentModel.Annotations package (included in .NET 10)
  - Hot reload warnings eliminated

- [x] **Pre-commit validation enforcement** - ✅ IMPLEMENTED (Feb 2, 2026)
  - Implemented Git pre-commit hook (`scripts/pre-commit.ps1`)
  - Validates Release builds before commit
  - Validates solution file consistency (all projects in main solution)
  - Setup script (`scripts/setup-hooks.ps1`) for easy installation
  - Documented in CONTRIBUTING.md
  - Can be bypassed with `git commit --no-verify` when needed

- [x] **Authorization header not sent with API requests** - ✅ RESOLVED (Feb 14, 2026)
  - CollectionService now directly adds JWT token to requests
  - Simpler approach than DelegatingHandler pattern
  - Works reliably in Blazor WebAssembly

**Performance Considerations (Addressed - Feb 14, 2026):**

- [x] **Pagination implemented** - Essential for large collections (10k+ cards)
  - Server-side pagination with LIMIT/OFFSET
  - Configurable page size (default: 50, max: 100)
  - Smart page navigation UI (shows up to 5 pages at once)

### Phase 3: Scryfall Integration & Card Management (In Progress)

**Strategy**: First populate the database with card data, then build search/add features on top of it.

**Key Deliverables:**

- [x] **Build ScryfallSync utility** ✅ (Feb 16, 2026)
  - [x] Create console application in src/utilities/ScryfallSync
  - [x] Download Scryfall bulk data (Default Cards endpoint - ~501 MB)
  - [x] Parse JSON and map to Card entities
  - [x] Bulk insert to PostgreSQL (112,145 cards)
  - [x] Handle card images (store Scryfall CDN URLs)
  - [x] Add command-line arguments (--force-refresh, --dry-run, --bulk-type, --list-types)
  - [x] Progress reporting (parsing + batch insert)
  - [x] Local caching (data/ directory, 24-hour cache)
  - [x] Multi-faced card support (transform, modal DFC, reversible cards)
  - [x] Finishes tracking (nonfoil/foil/etched)
  - [x] Error handling and statistics display
  - [x] ADR-020: Multi-faced card storage architecture
  - [x] User-Agent header compliance with Scryfall API requirements
  - [x] Add Scryfall attribution/credit in UI footer ✅ (Feb 16, 2026)

- [ ] **Collections Management Features** (After Scryfall data loaded)
  - [ ] Card search API endpoint (search by name, set, type, etc.)
  - [ ] Card search UI with autocomplete
  - [ ] Add card to collection (POST /api/collections)
  - [ ] Edit card quantity (PUT /api/collections/{id})
  - [ ] Remove card from collection (DELETE /api/collections/{id})
  - [ ] Display card images in collection view
  - [ ] Card details modal/page
- [ ] **Import/Export Features**
  - [ ] Import from Moxfield CSV
  - [ ] Import from Manabox CSV
  - [ ] Import from generic CSV format
  - [ ] Export collection to CSV
  - [ ] Validation and error reporting for imports
  - [ ] Create console application in src/utilities/ScryfallSync
  - [ ] Download Scryfall bulk data (Default Cards endpoint - ~501 MB)
  - [ ] Parse JSON and map to Card entities
  - [ ] Bulk insert to PostgreSQL (~111,000 cards)
  - [ ] Handle card images (store URLs, not download files)
  - [ ] Add command-line arguments (--force-refresh, --dry-run)
  - [ ] Progress reporting and error handling
  - [ ] Add Scryfall attribution/credit in UI footer
    - Index on (UserId, CardId, Platform) for uniqueness
  - Partial indexes on ArenaId/MtgoId (only where NOT NULL)

**Nice to Have (Defer to Later):**

- Consider: URL versioning vs header versioning vs query string
- Note: ApiRoutes.cs structure already supports easy migration to versioned routes

**Technical Debt / Warnings:**

- [x] **Blazor WebAssembly dotnet watch warning** - ✅ RESOLVED (Feb 2, 2026)
  - Upgraded MTGCollectionTracker.Shared from netstandard2.1 to net10.0
  - Removed System.ComponentModel.Annotations package (included in .NET 10)
  - Hot reload warnings eliminated

- [x] **Pre-commit validation enforcement** - ✅ IMPLEMENTED (Feb 2, 2026)
  - Implemented Git pre-commit hook (`scripts/pre-commit.ps1`)
  - Validates Release builds before commit
  - Validates solution file consistency (all projects in main solution)
  - Setup script (`scripts/setup-hooks.ps1`) for easy installation
  - Documented in CONTRIBUTING.md
  - Can be bypassed with `git commit --no-verify` when needed

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

- [ ] **Research foil/finish tracking strategy**
  - [ ] Analyze Scryfall finishes array: ["nonfoil", "foil", "etched"]
  - [ ] Handle foil-only cards (finishes: ["foil"])
  - [ ] Decide: Separate columns (FoilQuantity, EtchedQuantity) vs JSONB field
  - [ ] Consider Arena style variants vs finishes
  - [ ] User expectations research (separate tracking or combined?)
  - [ ] Update Card and CollectionEntry entities based on findings
- [ ] Build ScryfallSync console app
  - [ ] Download bulk data from Scryfall API (Default Cards - 501 MB)
  - [ ] Parse JSON and map to Card entities
  - [ ] Bulk insert to PostgreSQL (~111k cards)
  - [ ] Handle updates and new sets (daily sync)
  - [ ] Store finishes array in Card entity
  - [ ] Add Scryfall attribution/credit in UI footer
- [ ] Optimize card search queries
  - [ ] Add full-text search indexes on card name
  - [ ] Implement autocomplete endpoint
  - [ ] Cache frequent searches
- [ ] Test with real card data
  - [ ] Verify all Arena cards mapped correctly (via arena_id)
  - [ ] Test collection imports with large datasets
  - [ ] Performance testing with 10k+ card collections
- [ ] Display card images from Scryfall
  - [ ] Use image_uris JSONB field (hotlink to Scryfall CDN)
  - [ ] Support multiple sizes (small, normal, large, png)
  - [ ] Handle missing images gracefully (placeholder)

---

### Phase 4: Azure Deployment & CI/CD

**Pre-Deployment Research:**

- [ ] **Research pricing strategy and sources**
  - [ ] Evaluate pricing sources: TCGPlayer, CardKingdom, Scryfall bulk data
  - [ ] Understand pricing per finish: usd, usd_foil, usd_etched
  - [ ] Update frequency requirements (daily? on-demand?)
  - [ ] Historical pricing data needs (price trends/charts?)
  - [ ] Consider card condition pricing (NM, LP, MP, HP) if applicable
  - [ ] Decide: Add Prices field to Card entity or separate PriceHistory table?

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
- [ ] **Implement Container/Location tracking system**
  - [ ] Design: Create Container and ContainerEntry entities
  - [ ] Container types: Deck, Binder, Box
  - [ ] Track unallocated vs allocated quantity per collection entry
  - [ ] Remember source container when moving cards
  - [ ] "Return to source" functionality when removing from deck
  - [ ] Filter/search by location
  - [ ] Location autocomplete and management UI
  - [ ] Replaces simple string Location field with proper entity relationships
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
