# ADR-001: Monorepo Structure

**Date**: January 12, 2026  
**Status**: Accepted

## Context

We need to manage multiple related projects: backend API, frontend Blazor app, WPF desktop client, shared libraries, and infrastructure code. Options:

1. Separate repositories for each project
2. Monorepo with all projects together

## Decision

Use a monorepo with organized folders for each component.

## Consequences

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
