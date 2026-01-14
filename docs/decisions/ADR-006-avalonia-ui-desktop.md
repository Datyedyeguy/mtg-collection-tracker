# ADR-006: Avalonia UI for Desktop Client

**Date**: January 13, 2026
**Status**: Accepted
**Revision**: Changed from WPF after recognizing MTGA supports macOS

## Context

Desktop client framework for MTGA integration. **Critical requirement**: MTGA runs on both Windows AND macOS, so cross-platform support is essential, not optional.

Options:

1. **Avalonia UI** (cross-platform XAML)
2. **WPF** (Windows-only)
3. **.NET MAUI** (mobile-first)
4. **Electron** (web technologies)
5. **WinForms** (legacy Windows-only)

## Decision

Use Avalonia UI with .NET 10 for Windows, macOS, and Linux desktop support.

## Consequences

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
