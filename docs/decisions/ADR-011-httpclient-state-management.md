# ADR-011: HttpClient + Built-in State Management for Blazor

**Date**: January 13, 2026
**Status**: Accepted

## Context

Frontend state management for API data in Blazor WebAssembly. Options:

1. **Built-in HttpClient** + component state
2. **Fluxor** (Redux pattern for Blazor)
3. **Blazor.State** (third-party state library)
4. **Custom service layer** with dependency injection

## Decision

Use built-in HttpClient with dependency injection and component-level state management.

## Consequences

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
