# ADR-016: Authentication Implementation Strategy

**Date**: January 14, 2026  
**Status**: Accepted  
**Deciders**: Development Team

## Context

Need to implement user authentication for the MTG Collection Tracker. Must decide on authentication mechanism, identity storage, login identifiers, and external authentication provider support.

## Decision

### Core Authentication

**Use ASP.NET Core Identity** for user management and authentication.

- **Why Identity?**
  - Battle-tested, secure by default
  - Built-in password hashing, token management
  - Integrates with Entity Framework Core
  - Stores users in our PostgreSQL database
  - No external Azure services required
  - Well-documented and widely used

- **Why NOT Azure Entra ID?**
  - Entra ID is for enterprise SSO scenarios
  - Overkill for consumer application
  - Adds Azure dependency and cost
  - Identity gives us full control

### User Identification

**Username-based authentication** with email as separate account field.

- **Login Identifier**: Username (unique, immutable)
- **Email**: Separate field for notifications, password resets
- **Benefits**:
  - Users can change email without affecting login
  - More traditional gaming/collection app UX
  - Supports "display name" separate from login username

### Token Strategy

**JWT access tokens + refresh tokens**

- **Access Token**: Short-lived (15 minutes), stateless JWT
- **Refresh Token**: Long-lived (7 days), stored in database
- **Why?**: Balance between security and UX

### External Authentication (OAuth)

**Defer to Phase 3+** (not in MVP)

- **MVP**: Username/password only
- **Future**: Google, Microsoft OAuth providers
- **Reasoning**: 
  - Focus on core collection features first
  - ASP.NET Core Identity supports OAuth out-of-box when ready
  - Can add without breaking existing auth

## Consequences

### Positive

- Full control over user data and authentication flow
- No dependency on Azure authentication services
- Predictable cost (database storage only)
- Username flexibility (email changes don't affect login)
- OAuth support available when needed

### Negative

- Responsible for security updates and password policy
- No built-in "Sign in with Google" in MVP
- Must implement password reset mechanism ourselves

### Neutral

- Users must create account (can't use existing Google/Microsoft)
- Need to manage user credentials in our database

## Implementation Notes

- Use Entity Framework Core with ASP.NET Core Identity
- Store IdentityUser and related tables in PostgreSQL
- Implement custom IdentityUser with additional fields as needed
- Use BCrypt for password hashing (Identity default)
- JWT signing key stored in Azure Key Vault (production)

## Related Decisions

- ADR-002: ASP.NET Core 10 backend
- ADR-003: PostgreSQL database
- ADR-009: JWT + refresh tokens authentication
