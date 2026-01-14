# ADR-009: JWT + Refresh Tokens for Authentication

**Date**: January 12, 2026
**Status**: Accepted

## Context

Authentication mechanism for API. Options:

1. **JWT tokens** with refresh tokens
2. **Session-based** authentication (cookies)
3. **OAuth 2.0** with external provider (Azure AD B2C)

## Decision

Use JWT access tokens (15 min expiry) + refresh tokens (7 days expiry) with ASP.NET Core Identity for user management.

## Consequences

**Pros**:

- **Stateless**: No server-side session storage needed
- **Mobile-friendly**: Tokens work in native apps
- **Scalable**: Can add multiple API servers without session sharing
- **Standard**: Industry-standard approach (RFC 7519)
- Short-lived access tokens limit exposure if compromised
- Refresh tokens allow long-term login without re-entering password

**Cons**:

- Cannot revoke access tokens before expiry (mitigated by short expiry)
- Need to store refresh tokens in database
- More complex than session cookies
- CORS configuration needed for cross-origin requests

**Why not session cookies?**

- Harder to use with mobile apps
- Requires sticky sessions or Redis for multi-server
- CSRF protection needed

**Why not Azure AD B2C?**

- Costs money after 50,000 MAU (monthly active users)
- Adds external dependency
- More complex to set up
- Can add later if needed
