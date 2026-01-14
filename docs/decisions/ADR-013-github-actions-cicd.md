# ADR-013: GitHub Actions for CI/CD

**Date**: January 12, 2026
**Status**: Accepted

## Context

CI/CD platform choice. Options:

1. **GitHub Actions**
2. **Azure Pipelines**
3. **GitLab CI**
4. **Jenkins**

## Decision

Use GitHub Actions with separate workflows for backend, frontend, desktop, and infrastructure.

## Consequences

**Pros**:

- **Native integration**: Code and CI in same place
- **Free**: 2,000 minutes/month for private repos (GitHub Free)
- **Extensive marketplace**: Thousands of pre-built actions
- **Easy secrets management**: GitHub Secrets encrypted by default
- **Matrix builds**: Test multiple .NET/Node versions
- **Self-documenting**: Workflows visible in `.github/workflows/`

**Cons**:

- Slower than Azure Pipelines for .NET builds (no cache optimization)
- 6-hour job timeout (not an issue for us)

**Why not Azure Pipelines?**

- More complex YAML syntax
- Requires separate Azure DevOps project
- Similar free tier (1,800 minutes/month)
