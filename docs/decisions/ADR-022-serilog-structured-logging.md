# ADR-022: Serilog for Structured Logging

**Date**: March 24, 2026
**Status**: Accepted

## Context

The API needed a structured logging solution that works both locally (developer-friendly console output) and in production (Application Insights telemetry). Options:

1. **Built-in `Microsoft.Extensions.Logging`** — already present via ASP.NET Core
2. **Serilog** — popular .NET structured logging library with rich sink ecosystem
3. **NLog** — mature alternative logging framework
4. **Log4Net** — legacy option with .NET support

## Decision

Use **Serilog** with:

- `Serilog.AspNetCore` — host integration, request logging middleware
- `Serilog.Sinks.ApplicationInsights` — production telemetry sink
- `Microsoft.ApplicationInsights.AspNetCore` — Application Insights SDK

Configuration is read from `appsettings.json` / `appsettings.Production.json` so no code changes are needed to add or remove sinks.

## Consequences

**Pros**:

- **Structured logging**: Every log has queryable properties (not just text)
- **Bootstrap logger**: Captures startup errors before DI is ready (critical for diagnosing config issues)
- **Flush on shutdown**: `try/finally Log.CloseAndFlush()` guarantees buffered telemetry reaches Application Insights
- **Request logging**: `UseSerilogRequestLogging()` replaces verbose ASP.NET Core request logs with a single clean summary line per request
- **Sink ecosystem**: Easy to add file, Seq, Datadog, or other sinks without code changes
- **Configuration-driven**: Log levels and sinks controlled entirely in `appsettings.json`

**Cons**:

- Additional NuGet packages (3 packages)
- Serilog has its own property syntax (`{@object}`) that differs slightly from MEL
- Application Insights sink adds ~5 ms of buffering overhead on startup

**Why not built-in MEL only?**

- No built-in Application Insights sink via configuration
- Structured properties harder to add without custom enrichers
- Less control over output templates

**NuGet Packages Added**:

| Package                                    | Version | Purpose                               |
| ------------------------------------------ | ------- | ------------------------------------- |
| `Serilog.AspNetCore`                       | 9.0.0   | Host integration + request middleware |
| `Serilog.Sinks.ApplicationInsights`        | 4.0.0   | Production telemetry                  |
| `Microsoft.ApplicationInsights.AspNetCore` | 2.23.0  | App Insights SDK                      |

**Log Sink Configuration**:

| Environment | Sinks                          |
| ----------- | ------------------------------ |
| Development | Console (human-readable)       |
| Production  | Console + Application Insights |
