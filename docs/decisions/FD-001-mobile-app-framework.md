# FD-001: Mobile App Framework

**Status**: Decided - .NET MAUI (Deferred to Phase 8)
**Decision Date**: January 13, 2026

## Decision

Use .NET MAUI for iOS and Android mobile apps when Phase 8 begins.

## Rationale

- ✅ **Native C#**: Shares code with backend, Blazor, and Avalonia desktop
- ✅ **Production-ready**: Mature mobile support (iOS/Android)
- ✅ **Microsoft-backed**: Official cross-platform mobile solution
- ✅ **Touch-optimized**: Mobile-first design
- ✅ **Code sharing**: Share DTOs, API clients, business logic (~70%)

## Architecture

- Desktop (Avalonia): Windows/macOS/Linux desktop UI
- Mobile (MAUI): iOS/Android mobile UI
- Shared: API client, DTOs, services, business logic

## Why not single codebase for desktop + mobile?

- Desktop and mobile need fundamentally different UX
- Desktop: File pickers, system tray, log file watching
- Mobile: Touch gestures, swipe navigation, mobile-specific features
- Separate UIs, shared backend logic is best of both worlds

## Deferred decisions

- Budget for app store fees ($99/year iOS, $25 one-time Android)
- App store approval strategy
- Mobile-specific features beyond collection viewing
