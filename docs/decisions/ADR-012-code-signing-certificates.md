# ADR-012: Code Signing for Desktop Client

**Date**: January 12, 2026
**Status**: Proposed (Not Yet Implemented)

## Context

Windows desktop client needs to be trusted by SmartScreen and antivirus. Options:

1. **Purchase code signing certificate** ($100-400/year)
2. **Self-signed certificate** (users see warnings)
3. **No signing** (definite AV flags)

## Decision

Purchase code signing certificate from DigiCert or Sectigo after beta phase.

## Consequences

**Pros**:

- No SmartScreen warnings
- Fewer antivirus false positives
- Users trust the application more
- Required for Squirrel auto-update integrity

**Cons**:

- **Cost**: $100-400/year
- **Validation process**: 3-7 days for OV certificate
- **Hardware token**: EV certificates require USB token (more expensive)

**Implementation Plan**:

1. **Beta phase**: Use self-signed cert, document warnings in setup guide
2. **Public release**: Purchase OV certificate from Sectigo (~$100/year)
3. **Future**: Consider EV certificate if budget allows (~$400/year, no warnings at all)
