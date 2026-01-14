# ADR-003: PostgreSQL over SQL Server

**Date**: January 12, 2026
**Status**: Accepted

## Context

Database choice for Azure deployment. Options:

1. **Azure SQL Database** (SQL Server)
2. **PostgreSQL** (Azure Database for PostgreSQL)
3. **Cosmos DB** (NoSQL)
4. **MySQL** (Azure Database for MySQL)

## Decision

Use PostgreSQL Flexible Server.

## Consequences

**Pros**:

- **Cost**: $12/month (B1ms) vs $15/month for SQL Server (Basic)
- Open source with excellent Azure support
- Superior JSON support (JSONB type) for flexible card data
- EF Core has first-class PostgreSQL support via Npgsql
- Better full-text search capabilities
- Point-in-time restore (35 days retention)
- Active development and community

**Cons**:

- Team more familiar with SQL Server syntax
- Azure SQL has tighter integration with some Azure services
- No spatial data types (not needed for this project)

**Why not Cosmos DB?**

- Free tier is limited (1000 RU/s, 25 GB)
- Learning curve for NoSQL
- Overkill for structured card data
- More expensive at scale
