# ADR-002: ASP.NET Core 10 for Backend

**Date**: January 13, 2026
**Status**: Accepted

## Context

Backend API framework choice. Options evaluated:

1. **ASP.NET Core 10** (C#)
2. Node.js + Express (JavaScript/TypeScript)
3. Python + FastAPI
4. Go + Gin

## Decision

Use ASP.NET Core 10 with C#.

## Consequences

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
