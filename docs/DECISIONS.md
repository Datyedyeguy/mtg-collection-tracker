# Architecture Decision Records (ADR)

This document serves as an index to all architectural and technology decisions for the MTG Collection Tracker project.

## Format

Each decision is documented in a separate file following this structure:

- **Date**: When the decision was made
- **Status**: Proposed, Accepted, Superseded, Deprecated
- **Context**: What problem are we solving?
- **Decision**: What did we decide to do?
- **Consequences**: What are the trade-offs?

## Current Decisions

### Project Structure & Organization

- [ADR-001: Monorepo Structure](decisions/ADR-001-monorepo-structure.md)

### Backend & API

- [ADR-002: ASP.NET Core 10 for Backend](decisions/ADR-002-aspnet-core-backend.md)
- [ADR-003: PostgreSQL over SQL Server](decisions/ADR-003-postgresql-database.md)
- [ADR-009: JWT + Refresh Tokens for Authentication](decisions/ADR-009-jwt-refresh-tokens.md)
- [ADR-016: Authentication Implementation Strategy](decisions/ADR-016-authentication-implementation.md)
- [ADR-017: Testing Libraries (MSTest, NSubstitute, Shouldly)](decisions/ADR-017-testing-libraries.md)
- [ADR-019: Shared API Route Constants](decisions/ADR-019-shared-api-routes.md)

### Pending Decisions

- [ADR-018: HTTP Mocking for Frontend Tests](decisions/ADR-018-http-mocking-frontend-tests.md) - **Status: Proposed**

### Frontend

- [ADR-004: Blazor WebAssembly for Frontend](decisions/ADR-004-blazor-webassembly-frontend.md)
- [ADR-011: HttpClient + Built-in State Management for Blazor](decisions/ADR-011-httpclient-state-management.md)

### Desktop Client

- [ADR-006: Avalonia UI for Desktop Client](decisions/ADR-006-avalonia-ui-desktop.md)
- [ADR-007: Velopack for Cross-Platform Auto-Update](decisions/ADR-007-velopack-auto-update.md)
- [ADR-012: Code Signing for Desktop Client](decisions/ADR-012-code-signing-certificates.md)
- [ADR-015: MTGA Integration via Log Parsing](decisions/ADR-015-mtga-log-parsing.md)

### Infrastructure & Deployment

- [ADR-005: Azure App Service over Azure Functions](decisions/ADR-005-azure-app-service-hosting.md)
- [ADR-008: Bicep over Terraform for Infrastructure](decisions/ADR-008-bicep-infrastructure.md)
- [ADR-013: GitHub Actions for CI/CD](decisions/ADR-013-github-actions-cicd.md)
- [ADR-014: Cost Alerts at 50%, 83%, 100%](decisions/ADR-014-cost-alerts-monitoring.md)

### Data & External Services

- [ADR-010: Scryfall as Card Data Source](decisions/ADR-010-scryfall-card-data.md)

## Future Decisions

These are decisions that have been made but are deferred to later phases:

- [FD-001: Mobile App Framework (.NET MAUI)](decisions/FD-001-mobile-app-framework.md) - Phase 8
- [FD-002: Real-Time Features (SignalR)](decisions/FD-002-realtime-features.md) - Phase 9
- [FD-003: Search Engine (PostgreSQL Full-Text)](decisions/FD-003-search-engine.md) - Phase 9

## Decision Review Process

These decisions should be reviewed:

- **Quarterly**: Check if assumptions still hold (costs, performance, security)
- **Before major features**: Ensure decision supports new requirements
- **After production incidents**: Did architectural choice contribute to issue?

Document any changes as new ADRs (don't edit existing ones - mark as "Superseded" instead).

---

**Last Updated**: January 14, 2026
**Next Review**: April 14, 2026
