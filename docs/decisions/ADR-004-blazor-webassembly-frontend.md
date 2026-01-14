# ADR-004: Blazor WebAssembly for Frontend

**Date**: January 13, 2026
**Status**: Accepted

## Context

Frontend framework choice. Options:

1. **Blazor WebAssembly** (C#)
2. React + TypeScript
3. Vue 3 + TypeScript
4. Angular 17
5. Svelte + TypeScript

## Decision

Use Blazor WebAssembly (.NET 10) with MudBlazor or Blazorise component library.

## Consequences

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
