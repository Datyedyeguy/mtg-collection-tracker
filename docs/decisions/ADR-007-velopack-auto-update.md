# ADR-007: Velopack for Cross-Platform Auto-Update

**Date**: January 13, 2026
**Status**: Accepted
**Revision**: Changed from Squirrel.Windows after adopting Avalonia (cross-platform)

## Context

Desktop client needs auto-update mechanism for Windows, macOS, and Linux. Options:

1. **Velopack** (cross-platform, Squirrel successor)
2. **Squirrel.Windows** (Windows-only)
3. **Sparkle** (macOS-only)
4. **Custom solution** (download + replace)
5. **ClickOnce** (doesn't support .NET 10)

## Decision

Use Velopack with Azure Blob Storage for release hosting.

## Consequences

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
