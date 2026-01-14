# ADR-008: Bicep over Terraform for Infrastructure

**Date**: January 12, 2026
**Status**: Accepted

## Context

Infrastructure as Code (IaC) tool choice. Options:

1. **Azure Bicep**
2. **Terraform**
3. **ARM Templates** (JSON)
4. **Pulumi**

## Decision

Use Azure Bicep with parameterized modules.

## Consequences

**Pros**:

- **Native Azure support**: Best integration with Azure Resource Manager
- **Simpler syntax**: Easier than ARM JSON, less verbose than Terraform
- **Type safety**: Validates resource properties at compile time
- **No state file**: ARM is source of truth (unlike Terraform)
- **Free**: No Terraform Cloud costs
- **Automatic dependency resolution**: No manual `depends_on` needed
- Transpiles to ARM templates (visible in Azure Portal)

**Cons**:

- Azure-only (not multi-cloud like Terraform)
- Smaller community than Terraform
- Fewer modules/examples available

**Why not Terraform?**

- State file management complexity
- Need to lock state in Azure Storage ($)
- More verbose for Azure-specific features
- Team unfamiliar with HCL syntax

**Why not ARM Templates?**

- JSON is verbose and hard to read
- No variables/loops/functions (Bicep has them)
