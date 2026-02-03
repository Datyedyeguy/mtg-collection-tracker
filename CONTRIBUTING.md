# Contributing to MTG Collection Tracker

> **Note**: This is currently a personal learning project focused on exploring Azure services and GitHub CI/CD workflows. I'm not actively seeking contributions at this time while I learn and experiment.
>
> Feel free to fork the project for your own learning, but pull requests may not be reviewed or accepted. This may change once the project reaches a more stable state.

## Learning Goals

This project exists primarily to:

- Learn Azure cloud services (App Service, Static Web Apps, PostgreSQL, etc.)
- Practice GitHub Actions CI/CD workflows
- Explore Infrastructure as Code with Bicep
- Build a full-stack application with modern technologies

If you're interested in similar learning goals, feel free to fork and adapt!

## Developer Notes

> **This section is for the primary developer (me!) - not for external contributors yet.**

### Pre-Commit Hook Setup

To catch build issues before commits, install the pre-commit hook:

```bash
.\scripts\setup-hooks.ps1
```

This automatically validates before each commit:

- ✅ Release build succeeds (catches warnings-as-errors)
- ✅ All projects are in `MTGCollectionTracker.slnx`

**Bypass when needed:** `git commit --no-verify`

### Quick Reference

**Build in Release mode** (do this before pushing):

```bash
dotnet build MTGCollectionTracker.slnx --configuration Release
```

**Run all tests**:

```bash
dotnet test MTGCollectionTracker.slnx
```

**Validate solution consistency**:

```bash
.\scripts\validate-solutions.ps1
```
