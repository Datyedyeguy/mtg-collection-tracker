# Architecture Decision Records (ADR)

This document records key architectural and technology decisions for the MTG Collection Tracker project.

## Format

Each decision follows this structure:

- **Date**: When the decision was made
- **Status**: Proposed, Accepted, Superseded, Deprecated
- **Context**: What problem are we solving?
- **Decision**: What did we decide to do?
- **Consequences**: What are the trade-offs?

---

## ADR-001: Monorepo Structure

**Date**: January 12, 2026
**Status**: Accepted

### Context

We need to manage multiple related projects: backend API, frontend Blazor app, WPF desktop client, shared libraries, and infrastructure code. Options:

1. Separate repositories for each project
2. Monorepo with all projects together

### Decision

Use a monorepo with organized folders for each component.

### Consequences

**Pros**:

- Single version history for all components
- Easier to coordinate breaking changes across frontend/backend
- Simplified CI/CD (one set of GitHub Actions)
- Shared libraries automatically stay in sync
- Single issue tracker for all bugs/features

**Cons**:

- Larger repository size
- Need to carefully scope builds (don't rebuild frontend when backend changes)
- All developers have access to all code (not an issue for small team)

---

## ADR-002: ASP.NET Core 10 for Backend

**Date**: January 13, 2026
**Status**: Accepted

### Context

Backend API framework choice. Options evaluated:

1. **ASP.NET Core 10** (C#)
2. Node.js + Express (JavaScript/TypeScript)
3. Python + FastAPI
4. Go + Gin

### Decision

Use ASP.NET Core 10 with C#.

### Consequences

**Pros**:

- Natural extension of existing C# codebase (MTGA injector)
- Excellent performance (top 5 in TechEmpower benchmarks)
- First-class Azure integration
- Built-in dependency injection, configuration, middleware
- EF Core provides type-safe database access
- Strong typing prevents runtime errors
- Mature ecosystem with extensive libraries

**Cons**:

- Less trendy than Node.js/Python in web dev community
- Requires .NET runtime (not an issue with containers)
- Smaller talent pool than JavaScript (but team already knows C#)

---

## ADR-003: PostgreSQL over SQL Server

**Date**: January 12, 2026
**Status**: Accepted

### Context

Database choice for Azure deployment. Options:

1. **Azure SQL Database** (SQL Server)
2. **PostgreSQL** (Azure Database for PostgreSQL)
3. **Cosmos DB** (NoSQL)
4. **MySQL** (Azure Database for MySQL)

### Decision

Use PostgreSQL Flexible Server.

### Consequences

**Pros**:

- **Cost**: $12/month (B1ms) vs $15/month for SQL Server (Basic)
- Open source with excellent Azure support
- Superior JSON support (JSONB type) for flexible card data
- EF Core has first-class PostgreSQL support via Npgsql
- Better full-text search capabilities
- Point-in-time restore (35 days retention)
- Active development and community

**Cons**:

- Team more familiar with SQL Server syntax
- Azure SQL has tighter integration with some Azure services
- No spatial data types (not needed for this project)

**Why not Cosmos DB?**

- Free tier is limited (1000 RU/s, 25 GB)
- Learning curve for NoSQL
- Overkill for structured card data
- More expensive at scale

---

## ADR-004: Blazor WebAssembly for Frontend

**Date**: January 13, 2026
**Status**: Accepted

### Context

Frontend framework choice. Options:

1. **Blazor WebAssembly** (C#)
2. React + TypeScript
3. Vue 3 + TypeScript
4. Angular 17
5. Svelte + TypeScript

### Decision

Use Blazor WebAssembly (.NET 10) with MudBlazor or Blazorise component library.

### Consequences

**Pros**:

- **Full C# Stack**: Share DTOs and code between backend, frontend, and desktop client
- **Type Safety**: Compile-time type checking across entire application
- **No TypeScript Generation**: Use C# models directly in frontend without code generation
- **Single Language**: No context switching between C# and JavaScript
- **Learning Focus**: Deep dive into .NET ecosystem (project goal)
- **Azure Integration**: First-class support in Azure Static Web Apps
- **Component Libraries**: MudBlazor/Blazorise provide Material Design/Bootstrap components
- **Debugging**: Full C# debugging experience in browser

**Cons**:

- **Bundle Size**: Larger initial download (~2-3 MB vs React's ~200KB)
- **Load Time**: Slower initial load while downloading .NET runtime
- **Ecosystem**: Smaller component library ecosystem than React
- **Talent Pool**: Fewer frontend developers familiar with Blazor
- **SEO**: Limited (not an issue for authenticated app)

**Why not React?**

- Requires maintaining TypeScript definitions separate from C# DTOs
- Context switching between languages
- Project goal is to learn .NET stack, not JavaScript ecosystem
- Can always migrate to React later if needed (frontend is separate project)

---

## ADR-005: Azure App Service over Azure Functions

**Date**: January 12, 2026
**Status**: Accepted

### Context

Backend hosting choice. Options:

1. **Azure App Service** (Linux, B1 tier)
2. **Azure Functions** (Consumption plan)
3. **Azure Container Apps** (Consumption)
4. **Azure Kubernetes Service** (AKS)

### Decision

Use Azure App Service with Linux B1 tier (~$13/month).

### Consequences

**Pros**:

- Always-on (no cold starts like Functions)
- Predictable pricing
- Easy deployment from GitHub Actions
- Deployment slots for blue-green deployments
- Built-in SSL, custom domains, auto-scaling
- Good for traditional REST APIs

**Cons**:

- More expensive than Functions Consumption ($13/mo vs ~$0)
- Still pays when idle (but <$13/mo with VS credits)

**Why not Azure Functions?**

- Cold starts (2-5 seconds) hurt user experience
- Consumption plan limits: 5-10 minute timeout, 1.5 GB memory
- Harder to debug complex applications

**Why not Container Apps?**

- More complex (need to manage containers)
- Consumption pricing is unpredictable for this workload
- Overkill for a simple API

**Alternative (if cost becomes issue)**:

- Azure Functions + Durable Functions for background jobs
- Static Web Apps backend (built on Functions)

---

## ADR-006: Avalonia UI for Desktop Client

**Date**: January 13, 2026
**Status**: Accepted
**Revision**: Changed from WPF after recognizing MTGA supports macOS

### Context

Desktop client framework for MTGA integration. **Critical requirement**: MTGA runs on both Windows AND macOS, so cross-platform support is essential, not optional.

Options:

1. **Avalonia UI** (cross-platform XAML)
2. **WPF** (Windows-only)
3. **.NET MAUI** (mobile-first)
4. **Electron** (web technologies)
5. **WinForms** (legacy Windows-only)

### Decision

Use Avalonia UI with .NET 10 for Windows, macOS, and Linux desktop support.

### Consequences

**Pros**:

- ✅ **Cross-platform**: Single C# codebase for Windows, macOS, Linux
- ✅ **MTGA coverage**: Supports both Windows and macOS where MTGA runs
- ✅ **XAML/MVVM**: Same patterns as WPF, familiar to .NET developers
- ✅ **Native C#**: Full .NET 10 support, shares code with backend/Blazor
- ✅ **Log parsing works**: FileSystemWatcher compatible on all platforms (ADR-015)
- ✅ **Modern UI**: FluentAvalonia, Material.Avalonia, Semi.Avalonia themes available
- ✅ **Active development**: Growing ecosystem, backed by JetBrains
- ✅ **Linux bonus**: Free Linux support for users who want it

**Cons**:

- ⚠️ **Less mature than WPF**: Some rough edges, smaller ecosystem
- ⚠️ **Fewer third-party controls**: Library selection smaller than WPF
- ⚠️ **Learning curve**: Similar to WPF but with platform differences
- ⚠️ **Designer tooling**: Not as polished as Visual Studio WPF designer

**Why not WPF?**

- ❌ **Windows-only**: Excludes macOS users (MTGA supports macOS)
- Would require separate macOS app or abandon macOS users entirely

**Why not .NET MAUI?**

- ❌ **Mobile-first design**: Desktop feels like afterthought
- Missing traditional desktop controls and patterns
- Better suited for Phase 8 mobile apps

**Why not Electron?**

- ❌ **100-200 MB bundle**: Too heavy for utility app
- ❌ **Memory hungry**: 100+ MB RAM baseline
- Need to bridge web → C# log parser

**Implementation Notes**:

- Use FluentAvalonia for modern Windows 11-style UI
- Platform-specific code via conditional compilation when needed
- Share 90%+ of code across platforms

---

## ADR-007: Velopack for Cross-Platform Auto-Update

**Date**: January 13, 2026
**Status**: Accepted
**Revision**: Changed from Squirrel.Windows after adopting Avalonia (cross-platform)

### Context

Desktop client needs auto-update mechanism for Windows, macOS, and Linux. Options:

1. **Velopack** (cross-platform, Squirrel successor)
2. **Squirrel.Windows** (Windows-only)
3. **Sparkle** (macOS-only)
4. **Custom solution** (download + replace)
5. **ClickOnce** (doesn't support .NET 10)

### Decision

Use Velopack with Azure Blob Storage for release hosting.

### Consequences

**Pros**:

- ✅ **Cross-platform**: Windows, macOS, Linux support
- ✅ **Delta updates**: Only download changed files
- ✅ **Silent background updates**: No user interruption
- ✅ **Modern**: Actively maintained successor to Squirrel
- ✅ **.NET 10 compatible**: Works with latest .NET
- ✅ **Simple integration**: NuGet package + minimal code
- ✅ **Free hosting**: Azure Blob Storage ~$1/month
- ✅ **Code signing support**: Per-platform signing

**Cons**:

- ⚠️ **Newer than Squirrel**: Less battle-tested (but growing)
- ⚠️ **Requires package creation**: Need to build releases for each platform
- ⚠️ **Code signing needed**: Windows (Authenticode), macOS (Apple Developer cert)
- ⚠️ **Platform-specific builds**: Separate CI jobs for each OS

**Why not Squirrel.Windows?**

- Windows-only, doesn't support macOS/Linux
- Velopack is the official cross-platform successor

**Why not platform-specific solutions?**

- Sparkle (macOS) + Squirrel (Windows) = maintain two systems
- Velopack unifies update logic across all platforms

---

## ADR-008: Bicep over Terraform for Infrastructure

**Date**: January 12, 2026
**Status**: Accepted

### Context

Infrastructure as Code (IaC) tool choice. Options:

1. **Azure Bicep**
2. **Terraform**
3. **ARM Templates** (JSON)
4. **Pulumi**

### Decision

Use Azure Bicep with parameterized modules.

### Consequences

**Pros**:

- **Native Azure support**: Best integration with Azure Resource Manager
- **Simpler syntax**: Easier than ARM JSON, less verbose than Terraform
- **Type safety**: Validates resource properties at compile time
- **No state file**: ARM is source of truth (unlike Terraform)
- **Free**: No Terraform Cloud costs
- **Automatic dependency resolution**: No manual `depends_on` needed
- Transpiles to ARM templates (visible in Azure Portal)

**Cons**:

- Azure-only (not multi-cloud like Terraform)
- Smaller community than Terraform
- Fewer modules/examples available

**Why not Terraform?**

- State file management complexity
- Need to lock state in Azure Storage ($)
- More verbose for Azure-specific features
- Team unfamiliar with HCL syntax

**Why not ARM Templates?**

- JSON is verbose and hard to read
- No variables/loops/functions (Bicep has them)

---

## ADR-009: JWT + Refresh Tokens for Authentication

**Date**: January 12, 2026
**Status**: Accepted

### Context

Authentication mechanism for API. Options:

1. **JWT tokens** with refresh tokens
2. **Session-based** authentication (cookies)
3. **OAuth 2.0** with external provider (Azure AD B2C)

### Decision

Use JWT access tokens (15 min expiry) + refresh tokens (7 days expiry) with ASP.NET Core Identity for user management.

### Consequences

**Pros**:

- **Stateless**: No server-side session storage needed
- **Mobile-friendly**: Tokens work in native apps
- **Scalable**: Can add multiple API servers without session sharing
- **Standard**: Industry-standard approach (RFC 7519)
- Short-lived access tokens limit exposure if compromised
- Refresh tokens allow long-term login without re-entering password

**Cons**:

- Cannot revoke access tokens before expiry (mitigated by short expiry)
- Need to store refresh tokens in database
- More complex than session cookies
- CORS configuration needed for cross-origin requests

**Why not session cookies?**

- Harder to use with mobile apps
- Requires sticky sessions or Redis for multi-server
- CSRF protection needed

**Why not Azure AD B2C?**

- Costs money after 50,000 MAU (monthly active users)
- Adds external dependency
- More complex to set up
- Can add later if needed

---

## ADR-010: Scryfall as Card Data Source

**Date**: January 13, 2026
**Status**: Accepted

### Context

Need comprehensive MTG card data including paper cards, Arena-exclusive sets (Alchemy, Through the Omenpaths), and rebalanced cards. Options:

1. **Scryfall API** (free bulk data)
2. **MTGJSON** (free JSON files)
3. **17Lands** (Arena-focused CSV)
4. **Manual database** (scrape Gatherer)

### Decision

Use Scryfall bulk data API exclusively with daily sync to PostgreSQL.

### Consequences

**Pros**:

- **Comprehensive**: 111,000+ cards with all printings including Alchemy and Arena-exclusive sets
- **Well-maintained**: Updated within hours of new sets
- **Bulk data endpoint**: No rate limits (respects their guidelines)
- **Rich metadata**: Prices, legalities, rulings, images, artist attribution
- **Arena/MTGO IDs**: Includes platform-specific identifiers (arena_id GrpIds)
- **Free**: No API key or cost
- **JSON format**: Easy to parse
- **Excellent documentation**: Clear API docs and usage guidelines
- **All images**: High-quality card images with proper copyright attribution

**Cons**:

- **Large files**: 600 MB uncompressed (80 MB gzipped)
- **Daily sync needed**: Can't rely on real-time API for every request

**Why not MTGJSON?**

- Less frequently updated
- More complex JSON structure
- Larger file size

**Scryfall Usage Compliance**:

Per Scryfall Fan Content Policy, we comply with:

- ✅ No paywall for card data access
- ✅ Adding value (collection tracking, deck building, MTGA sync)
- ✅ Proper image attribution (artist/copyright visible)
- ✅ Using bulk data endpoint (no rate limit abuse)
- ✅ Caching locally in database (24+ hour cache)
- ✅ Attribution: "Card data provided by Scryfall"

**Implementation Strategy**:

1. Download bulk data daily at 3 AM UTC via scheduled job
2. Store all cards in PostgreSQL for fast queries
3. Cache downloaded file locally for 24 hours (optional optimization)
4. Use `arena_id` when available, fallback to name+set matching
5. Display full card images with artist attribution
6. Include Scryfall credit in app footer

**Future Consideration**: If Scryfall proves insufficient for Arena-specific data, evaluate supplementing with 17Lands or MTGA log parsing

---

## ADR-011: HttpClient + Built-in State Management for Blazor

**Date**: January 13, 2026
**Status**: Accepted

### Context

Frontend state management for API data in Blazor WebAssembly. Options:

1. **Built-in HttpClient** + component state
2. **Fluxor** (Redux pattern for Blazor)
3. **Blazor.State** (third-party state library)
4. **Custom service layer** with dependency injection

### Decision

Use built-in HttpClient with dependency injection and component-level state management.

### Consequences

**Pros**:

- **No external dependencies**: Built into Blazor WebAssembly
- **Simple**: Easy to understand and maintain
- **Type-safe**: Share C# DTOs directly between backend and frontend
- **Dependency injection**: Services injected via `@inject` directive
- **Component state**: `StateHasChanged()` for reactivity
- **Learning focus**: Understand Blazor fundamentals before adding libraries

**Cons**:

- Manual cache management (no automatic caching like React Query)
- Need to implement loading/error states manually
- No built-in request deduplication

**Why not Fluxor?**

- Adds complexity for simple CRUD operations
- Can add later if global state becomes unwieldy
- Redux pattern has learning curve

**Strategy**:

1. Create API service classes (e.g., `CollectionService`, `CardService`)
2. Inject HttpClient into services
3. Use component state for loading/error handling
4. Share service instances via DI for caching when needed

---

## ADR-012: Code Signing for Desktop Client

**Date**: January 12, 2026
**Status**: Proposed (Not Yet Implemented)

### Context

Windows desktop client needs to be trusted by SmartScreen and antivirus. Options:

1. **Purchase code signing certificate** ($100-400/year)
2. **Self-signed certificate** (users see warnings)
3. **No signing** (definite AV flags)

### Decision

Purchase code signing certificate from DigiCert or Sectigo after beta phase.

### Consequences

**Pros**:

- No SmartScreen warnings
- Fewer antivirus false positives
- Users trust the application more
- Required for Squirrel auto-update integrity

**Cons**:

- **Cost**: $100-400/year
- **Validation process**: 3-7 days for OV certificate
- **Hardware token**: EV certificates require USB token (more expensive)

**Implementation Plan**:

1. **Beta phase**: Use self-signed cert, document warnings in setup guide
2. **Public release**: Purchase OV certificate from Sectigo (~$100/year)
3. **Future**: Consider EV certificate if budget allows (~$400/year, no warnings at all)

---

## ADR-013: GitHub Actions for CI/CD

**Date**: January 12, 2026
**Status**: Accepted

### Context

CI/CD platform choice. Options:

1. **GitHub Actions**
2. **Azure Pipelines**
3. **GitLab CI**
4. **Jenkins**

### Decision

Use GitHub Actions with separate workflows for backend, frontend, desktop, and infrastructure.

### Consequences

**Pros**:

- **Native integration**: Code and CI in same place
- **Free**: 2,000 minutes/month for private repos (GitHub Free)
- **Extensive marketplace**: Thousands of pre-built actions
- **Easy secrets management**: GitHub Secrets encrypted by default
- **Matrix builds**: Test multiple .NET/Node versions
- **Self-documenting**: Workflows visible in `.github/workflows/`

**Cons**:

- Slower than Azure Pipelines for .NET builds (no cache optimization)
- 6-hour job timeout (not an issue for us)

**Why not Azure Pipelines?**

- More complex YAML syntax
- Requires separate Azure DevOps project
- Similar free tier (1,800 minutes/month)

---

## ADR-014: Cost Alerts at 50%, 83%, 100%

**Date**: January 13, 2026
**Status**: Accepted

### Context

Need to monitor Azure costs to stay under $150/month budget. Options:

1. **Azure Cost Management** budgets with email alerts
2. **Azure Monitor** action groups with Logic Apps
3. **Third-party tools** (CloudHealth, CloudCheckr)
4. **Manual checks** (Azure Portal daily review)

### Decision

Use Azure Cost Management budgets with tiered email alerts at $75, $125, $150/month thresholds (50%, 83%, 100% of budget).

### Consequences

**Pros**:

- **Free**: Built into Azure
- **Proactive**: Alerts before exceeding budget
- **Multiple warning levels**: 50%, 83%, 100% provide escalating awareness
- **Configurable**: Can adjust thresholds anytime
- **Integrated**: Same portal as other Azure resources
- **Bicep deployable**: Can define budgets in infrastructure code

**Cons**:

- Alerts are reactive (can't automatically shut down resources)
- 8-24 hour delay in cost reporting
- Email fatigue if costs spike repeatedly

**Alert Configuration**:

- **50% ($75)**: Info alert, review spending patterns
- **83% ($125)**: Warning alert, investigate high costs
- **100% ($150)**: Critical alert, immediate action required

**Action Items on Alerts**:

1. Check Azure Cost Analysis for top consumers
2. Review Application Insights for unusual traffic
3. Scale down App Service tier if needed (B1 → Free)
4. Pause PostgreSQL database during low-usage periods

**Future Enhancement**:

- Consider adding Logic App (~$0.50/month) in Phase 7+ to automatically deallocate resources if projected costs exceed $150/month
- Requires manual approval to restart services
- Provides safety net against runaway costs

---

## ADR-015: MTGA Integration via Log Parsing

**Date**: January 13, 2026
**Status**: Accepted

### Context

Need to sync MTGA collection to backend. MTGA doesn't have official API. Options:

1. **Log file parsing** (authorized via "Detailed Logs" setting)
2. **Memory reading/DLL injection** (risky, ToS gray area)
3. **Screen scraping/OCR** (unreliable)
4. **Manual export** (MTGA has no export feature)

### Decision

Use log file parsing with FileSystemWatcher, reading from Player.log file when user enables "Detailed Logs (Plugin Support)" setting.

### Consequences

**Pros**:

- **✅ ToS Compliant**: Explicitly authorized by MTGA's "Detailed Logs (Plugin Support)" setting
- **Safe for Users**: No account termination risk (17Lands, MTGA Assistant use this for years)
- **Cross-Platform**: Works on Windows and macOS (same log format)
- **No Admin Rights**: Standard file read permissions
- **Stable Format**: JSON payloads in logs rarely change
- **Real-Time Updates**: FileSystemWatcher for live monitoring
- **Industry Standard**: Proven approach used by 17Lands (500k+ users)

**Cons**:

- **User Must Enable**: Requires toggling "Detailed Logs (Plugin Support)" in MTGA options
- **Log File Location**: Must detect MTGA install directory (varies by user)
- **Parsing Complexity**: Extract JSON from mixed log output
- **Initial Collection**: Full collection only available after opening collection screen in MTGA
- **No Historical Data**: Can't see what you owned last month

**Implementation Strategy**:

```csharp
// 1. FileSystemWatcher on Player.log
// 2. Parse lines like: [UnityCrossThreadLogger]PlayerInventory.GetPlayerCardsV3
// 3. Extract JSON: {"InventoryInfo":[{"grpId":12345,"quantity":4},...]}
// 4. Map arena_id (grpId) to Scryfall cards via lookup table
// 5. POST to API: /api/collections/sync with diff
```

**Log Locations**:

- Windows: `%APPDATA%\..\LocalLow\Wizards Of The Coast\MTGA\Player.log`
- macOS: `~/Library/Logs/Wizards Of The Coast/MTGA/Player.log`

**Desktop Client Workflow**:

1. Detect MTGA installation (check common paths)
2. Check if "Detailed Logs" enabled (parse settings or instruct user)
3. Monitor Player.log with FileSystemWatcher
4. Parse collection updates from JSON payloads
5. Calculate diff from last known state
6. Upload changes to API endpoint

**Why not memory reading?**

- Gray area in ToS, potential ban risk
- Requires admin rights, triggers antivirus
- Breaks with every MTGA update
- Not cross-platform (Windows-only techniques)

---

## Future Decisions to Make

### FD-001: Mobile App Framework

**Status**: Decided - .NET MAUI (Deferred to Phase 8)
**Decision Date**: January 13, 2026

**Decision**: Use .NET MAUI for iOS and Android mobile apps when Phase 8 begins.

**Rationale**:

- ✅ **Native C#**: Shares code with backend, Blazor, and Avalonia desktop
- ✅ **Production-ready**: Mature mobile support (iOS/Android)
- ✅ **Microsoft-backed**: Official cross-platform mobile solution
- ✅ **Touch-optimized**: Mobile-first design
- ✅ **Code sharing**: Share DTOs, API clients, business logic (~70%)

**Architecture**:

- Desktop (Avalonia): Windows/macOS/Linux desktop UI
- Mobile (MAUI): iOS/Android mobile UI
- Shared: API client, DTOs, services, business logic

**Why not single codebase for desktop + mobile?**

- Desktop and mobile need fundamentally different UX
- Desktop: File pickers, system tray, log file watching
- Mobile: Touch gestures, swipe navigation, mobile-specific features
- Separate UIs, shared backend logic is best of both worlds

**Deferred decisions**:

- Budget for app store fees ($99/year iOS, $25 one-time Android)
- App store approval strategy
- Mobile-specific features beyond collection viewing

### FD-002: Real-Time Features

**Status**: Deferred to Phase 9

If we add real-time deck sharing or collaborative editing:

- **SignalR** (ASP.NET Core, WebSockets)
- **Socket.IO** (if we migrate to Node.js)
- **Azure Web PubSub** (managed service, more expensive)

### FD-003: Search Engine

**Status**: Deferred to Phase 9

For advanced card search (Scryfall-style query language):

- **PostgreSQL full-text search** (built-in, good enough)
- **Azure AI Search** (formerly Cognitive Search, powerful but $$$)
- **Elasticsearch** (self-hosted, complex)
- **Meilisearch** (modern, fast, OSS)

---

## ADR-015: Log Parsing for MTGA Integration (REVISED)

**Date**: January 12, 2026
**Status**: Accepted
**Revision**: Updated after ToS review

### Context

Initial implementation used DLL injection to extract MTGA collection data. After reviewing WotC's Terms of Service, we discovered that **both injection and memory scanning violate ToS Section 2.2(iii)(d)** which prohibits "third-party programs or tools not expressly authorized by Wizards."

**Critical Discovery**: MTGA includes a setting called **"Detailed Logs (Plugin Support)"** which explicitly authorizes third-party tools to read log files.

### Decision

Adopt **log file parsing** as the primary MTGA integration method:

- Read Player.log file that MTGA writes locally
- Parse JSON payloads containing collection data
- Monitor file for changes using FileSystemWatcher
- No game modification, memory access, or reverse engineering

### Consequences

**Pros**:

- ✅ **ToS Compliant**: Explicitly authorized via "Plugin Support" setting
- ✅ **Industry Precedent**: 17Lands, MTGA Assistant operate successfully
- ✅ **Cross-Platform**: Works on Windows and macOS
- ✅ **Reliable**: Stable JSON format, doesn't break with updates
- ✅ **No Admin Required**: Standard file read permissions
- ✅ **Safe for Users**: Zero account termination risk
- ✅ **Simple Implementation**: No reverse engineering needed

**Cons**:

- ⚠️ **User Must Enable Setting**: "Detailed Logs" not enabled by default
- ⚠️ **Delayed Updates**: Collection only written when user opens collection screen
- ⚠️ **Log File Size**: Player.log can grow large (100+ MB)
- ⚠️ **Requires MTGA Running**: Must parse while game is running or immediately after

### Implementation Approach

**Technology**: C# FileSystemWatcher + JSON parsing
**Location**: MTGALogParser project (src/desktop/MTGALogParser/)
**Architecture**:

```
MTGALogParser
  ├── FileWatcher - Monitors Player.log for changes
  ├── JsonExtractor - Parses collection JSON payloads
  ├── CollectionBuilder - Maintains current state
  └── ApiClient - Uploads to backend
```

### Alternative Considered: Memory Scanning

- Untapped.gg approach using ReadProcessMemory()
- **Rejected**: Still violates ToS (unauthorized tool)
- No explicit authorization from Wizards
- Higher legal risk despite being "read-only"

### Alternative Considered: DLL Injection

- Original implementation
- **Rejected**: Clear ToS violation
- Modifies game process
- Highest risk approach

### Evidence of Authorization

1. Setting name explicitly mentions "Plugin Support"
2. 17Lands.com has operated since 2020 using this method
3. WotC employees publicly endorse 17Lands
4. MTGA Assistant (open source) uses log parsing without issues

---

## Decision Review Process

These decisions should be reviewed:

- **Quarterly**: Check if assumptions still hold (costs, performance, security)
- **Before major features**: Ensure decision supports new requirements
- **After production incidents**: Did architectural choice contribute to issue?

Document any changes as new ADRs (don't edit existing ones - mark as "Superseded" instead).

---

**Last Updated**: January 13, 2026
**Next Review**: April 13, 2026
