# ADR-005: Azure App Service over Azure Functions

**Date**: January 12, 2026
**Status**: Accepted

## Context

Backend hosting choice. Options:

1. **Azure App Service** (Linux, B1 tier)
2. **Azure Functions** (Consumption plan)
3. **Azure Container Apps** (Consumption)
4. **Azure Kubernetes Service** (AKS)

## Decision

Use Azure App Service with Linux B1 tier (~$13/month).

## Consequences

**Pros**:

- Always-on (no cold starts like Functions)
- Predictable pricing
- Easy deployment from GitHub Actions
- Deployment slots for blue-green deployments
- Built-in SSL, custom domains, auto-scaling
- Good for traditional REST APIs

**Cons**:

- More expensive than Functions Consumption ($13/mo vs ~$0)
- Still pays when idle (but <$13/mo with VS credits)

**Why not Azure Functions?**

- Cold starts (2-5 seconds) hurt user experience
- Consumption plan limits: 5-10 minute timeout, 1.5 GB memory
- Harder to debug complex applications

**Why not Container Apps?**

- More complex (need to manage containers)
- Consumption pricing is unpredictable for this workload
- Overkill for a simple API

**Alternative (if cost becomes issue)**:

- Azure Functions + Durable Functions for background jobs
- Static Web Apps backend (built on Functions)
