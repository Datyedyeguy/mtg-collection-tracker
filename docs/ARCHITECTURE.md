# Architecture Overview

> **Note**: This is a learning project focused on Azure services and GitHub CI/CD workflows.

## High-Level Architecture

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

## Components

### Frontend (Blazor WebAssembly)

- Responsive web application (C#)
- .NET 10 build system (no Node.js required)
- MudBlazor or Blazorise component library
- Shared C# DTOs with backend (no type generation)
- Hosted on Azure Static Web Apps

### Backend (ASP.NET Core 10)

- RESTful Web API
- JWT authentication
- EF Core + PostgreSQL
- Hosted on Azure App Service (Linux)

### Desktop Client (WPF)

- MTGALogParser: Reads MTGA log files (ToS-compliant)
- Uploads collection data to backend API
- Auto-update via Squirrel.Windows
- Code-signed for Windows

### Database (PostgreSQL)

- User accounts
- Card metadata (from Scryfall)
- Collection entries (user cards)
- Decklists

## Data Flow

1. **MTGA Collection Sync**:

   - User enables "Detailed Logs" in MTGA settings
   - Desktop client monitors log files
   - Parses collection JSON
   - Uploads to backend API

2. **Manual Import**:

   - User uploads CSV (Moxfield, Manabox, etc.)
   - Frontend sends to API
   - API parses and stores in database

3. **Collection Viewing**:
   - Frontend requests collections from API
   - API queries PostgreSQL
   - Returns card data with quantities
   - Frontend displays with filters/search

## Infrastructure (Azure)

- **Frontend**: Static Web Apps (Free tier)
- **Backend**: App Service Linux B1 (~$13/mo)
- **Database**: PostgreSQL Flexible Server B1ms (~$12/mo)
- **Storage**: Blob storage for desktop client releases (~$1/mo)
- **Monitoring**: Application Insights
- **Secrets**: Key Vault

**Total Cost**: ~$26/month

## CI/CD (GitHub Actions)

- Backend: Build, test, deploy to App Service
- Frontend: Build Blazor, deploy to Static Web Apps
- Desktop: Build, sign, publish to Blob Storage
- Infrastructure: Deploy Bicep templates to Azure

## Security

- HTTPS everywhere
- JWT authentication (15 min access + 7 day refresh tokens)
- Passwords hashed with BCrypt
- Azure Key Vault for secrets
- CORS restricted to frontend domain
- Rate limiting on API endpoints

## Scalability Considerations

This is a **learning project** targeting 10-100 users, not production scale.

For growth beyond that:

- Move to Azure Container Apps or AKS
- Add Redis caching layer
- Implement CDN for static assets
- Database read replicas
- Horizontal API scaling
