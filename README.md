# MTG Collection Tracker

A comprehensive web-based collection management system for Magic: The Gathering cards across multiple platforms.

## Features

- 📦 **Multi-Platform Support**: Track collections in Paper, MTG Arena, and MTG Online
- 🎯 **Smart Imports**: Support for Moxfield, Manabox, CSV, and direct MTGA/MTGO integration
- 📍 **Physical Location Tracking**: Know exactly where your paper cards are (decks, binders, storage)
- 🎴 **Decklist Management**: Store and organize your decklists
- 🔄 **Delta Imports**: Efficiently update collections with only changes
- 🌐 **Web & Mobile**: Responsive design with future native mobile app support
- 🔐 **Secure Authentication**: Your collection data is private and secure
- 💰 **Learning Project**: Azure-hosted with <$150/month budget for exploring cloud services

## Quick Start

### For Users

1. **Visit the website**: https://example.com (domain TBD - coming soon)
2. **Create an account**: Sign up with your email
3. **Import your collection**:
   - **Paper**: Upload CSV from Moxfield, Manabox, or other platforms
   - **MTG Arena**: Download and run our [Windows desktop client](#mtga-client)
   - **MTG Online**: Export collection and upload the file

### MTGA Client

The MTG Arena desktop client automatically extracts your collection from **MTGA log files**:

1. Download: [Latest Release](https://github.com/YOUR_USERNAME/mtg-collection-tracker/releases/latest)
2. Run the installer
3. **Enable in MTGA**: Settings → Account → "Detailed Logs (Plugin Support)"
4. Launch the MTG Collection Tracker client
5. Click "Sync Collection" - logs are read and uploaded automatically

**How it works**: The client reads log files that MTGA writes locally using the official "Plugin Support" feature. Same method used by 17Lands and other popular trackers.

**Requirements**: Windows 10/11 or macOS, MTG Arena installed

**Safety**:

- ✅ ToS-compliant (explicitly authorized by Wizards)
- ✅ No code injection or memory modification
- ✅ Zero account termination risk
- ✅ Used by 17Lands, MTGA Assistant, and other endorsed tools

## Technology Stack

- **Frontend**: Blazor WebAssembly (.NET 10), Bootstrap CSS
- **Backend**: ASP.NET Core 10 Web API, PostgreSQL
- **Desktop Client**: WPF (.NET 10) with auto-update (future)
- **Hosting**: Azure (Static Web Apps + App Service)
- **Infrastructure**: Azure Bicep (Infrastructure as Code)

## Project Status

🚧 **In Active Development** - Not yet publicly available

Current phase: Phase 2 - Backend Foundation & Frontend Development

**Completed:**

- ✅ Authentication (Register, Login, JWT + Refresh Tokens)
- ✅ Collections viewing (with pagination and filtering)
- ✅ Database schema (Users, Cards, Collections)
- ✅ 80 passing tests

**Next:**

- 🔄 Scryfall data integration (Phase 3)
- 🔄 Card search functionality
- 🔄 Add/edit/remove cards from collection

See [ROADMAP.md](ROADMAP.md) for detailed progress.

## Development

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+ (or Docker for local development)
- Azure CLI (for deployments - optional for local dev)
- Visual Studio 2022 or VS Code

### Local Setup

```bash
# Clone repository
git clone https://github.com/YOUR_USERNAME/mtg-collection-tracker.git
cd mtg-collection-tracker

# Start PostgreSQL (Docker)
docker compose up -d

# Backend API
cd src/backend/MTGCollectionTracker.Api
dotnet restore
dotnet run --launch-profile https

# Frontend (in new terminal)
cd src/frontend/MTGCollectionTracker.Client
dotnet restore
dotnet run --launch-profile https

# Desktop Client (future - Phase 4)
cd src/desktop/MTGALogParser
dotnet restore
dotnet run
```

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for detailed setup instructions.

## Architecture

```
┌─────────────┐
│   Browser   │
│  (Blazor)   │
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────┐         ┌──────────────┐
│ Static Web App  │         │ Desktop      │
│   (Frontend)    │         │ Client (WPF) │
└─────────────────┘         └──────┬───────┘
       │                            │
       │ REST API                   │ MTGA Log Parsing
       ▼                            ▼
┌─────────────────┐         ┌──────────────┐
│   App Service   │         │  MTG Arena   │
│  (.NET 10 API)  │         │  Log Files   │
└────────┬────────┘         └──────────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │
│   (Card Data +  │
│   Collections)  │
└─────────────────┘
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed system design.

## Contributing

This is a personal learning project. See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## Cost Transparency

Monthly Azure costs (with optimizations):

- Azure App Service (Linux B1): ~$13/month
- PostgreSQL Flexible Server (B1ms): ~$12/month
- Blob Storage: ~$1/month
- **Total**: ~$26/month ($312/year)

**With Visual Studio Enterprise subscription**: Effectively FREE (includes $150/month Azure credits)

## Security

- ✅ Passwords hashed with BCrypt
- ✅ JWT-based authentication
- ✅ HTTPS enforced everywhere
- ✅ SQL injection prevention (EF Core parameterization)
- ✅ Rate limiting on API endpoints
- ✅ Secrets stored in Azure Key Vault

Found a security issue? Please report it responsibly via GitHub Security Advisories (do not open public issues for security vulnerabilities).

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [Scryfall](https://scryfall.com) for comprehensive MTG card data API
- [Wizards of the Coast](https://company.wizards.com/) for Magic: The Gathering
- Original MTGA collection exporter code from [MTGArenaCollectionExporter](../MTGArenaCollectionExporter)

## Roadmap

- [x] Phase 1: Project setup and architecture
- [ ] Phase 2: Backend API with authentication
- [ ] Phase 3: Frontend collection viewer
- [ ] Phase 4: Desktop MTGA client
- [ ] Phase 5: Import/export features
- [ ] Phase 6: Decklist management
- [ ] Phase 7: Physical location tracking
- [ ] Phase 8: Mobile apps
- [ ] Phase 9: Advanced search and statistics
- [ ] Phase 10: Public beta launch

## Support

- **Documentation**: [docs/](docs/)
- **Issues**: [GitHub Issues](https://github.com/YOUR_USERNAME/mtg-collection-tracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YOUR_USERNAME/mtg-collection-tracker/discussions)

---

Made with ❤️ for the MTG community
