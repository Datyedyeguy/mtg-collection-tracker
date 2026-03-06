# ADR-021: Background Job Pipeline for CSV Imports

**Date**: March 5, 2026
**Status**: Accepted

---

## Context

Manabox CSV exports can contain tens of thousands of rows (tested: 7,835 rows → 8,704 physical cards). Processing this synchronously inside an HTTP request is problematic:

- The HTTP request would block for several seconds (or time out entirely on large files)
- The client has no visibility into progress
- A server restart mid-request silently loses work
- IIS/Kestrel has a default request timeout that caps long-running operations

We needed an async job architecture that returns immediately, provides progress updates, and is resilient to restarts.

---

## Decision

Implement a **durable in-process background job pipeline** using:

- **`Channel<Guid>`** (System.Threading.Channels) as the in-process job queue
- **`BackgroundService`** (ASP.NET Core hosted service) as the worker
- **PostgreSQL `ImportJobs` table** as the durable job store

### Request/response flow

```
POST /api/imports/manabox
  → Buffer CSV bytes into ImportJob row (Status=Pending, CsvBytes stored as bytea)
  → Enqueue job ID into Channel<Guid>
  → Return 202 Accepted + { jobId, statusUrl }

ImportWorkerService (background loop)
  → Dequeue job ID from Channel
  → Load ImportJob from DB
  → Parse CSV, UNNEST-batch-upsert into CollectionEntries, update Progress (0–100)
  → Set Status=Completed or Status=Failed
  → Clear CsvBytes to reclaim storage

GET /api/imports/{jobId}/status
  → Return { status, progress, result? }
  → Client polls every 2 seconds until terminal state
```

### Crash durability

The ImportJob row is **written to PostgreSQL before** the job ID is enqueued into the Channel. On server restart:

```csharp
// ReEnqueueStaleJobsAsync called at worker startup
db.ImportJobs
  .Where(j => j.Status == ImportJobStatus.Pending
           || j.Status == ImportJobStatus.Processing)
  .ExecuteUpdateAsync(j => j.Status = Pending)  // reset Processing → Pending
```

This guarantees no jobs are silently lost across restarts.

### UNNEST batch upsert

Rather than one INSERT per card, the worker sends 1,000-row batches using PostgreSQL `unnest()`:

```sql
INSERT INTO "CollectionEntries" (...)
SELECT unnest(@ids::uuid[]), @userId, unnest(@cardIds::uuid[]), 'Paper',
       unnest(@quantities::int[]), unnest(@foilQty::int[]), 0, now(), now()
ON CONFLICT ("UserId", "CardId", "Platform") DO UPDATE
    SET "Quantity"     = "CollectionEntries"."Quantity"     + EXCLUDED."Quantity",
        "FoilQuantity" = "CollectionEntries"."FoilQuantity" + EXCLUDED."FoilQuantity",
        "UpdatedAt"    = now()
```

---

## Why Not Hangfire / Quartz.NET / Azure Service Bus?

| Option | Pro | Con |
|---|---|---|
| Hangfire | Dashboard, retry, scheduling | Extra dependency, SQL schema it controls |
| Quartz.NET | Robust scheduling | Complex setup, overkill for simple queue |
| Azure Service Bus | Multi-node safe, cloud-native | ~$10/month, operational overhead |
| **Channel\<T\> + BackgroundService** | Zero dependencies, fast, simple | In-process only (see limitation below) |

For a single-node learning project with 10–100 users, the in-memory Channel is the right complexity level. The limitation is documented deliberately.

---

## Known Limitation: Multi-Node Safety

`Channel<Guid>` lives in process memory. With two web nodes both running `ImportWorkerService`:

- The controller enqueues onto **one** node's Channel only
- On restart, **both** nodes re-scan for Pending jobs and double-enqueue

**Impact**: The UNNEST upsert is idempotent (`ON CONFLICT DO UPDATE`), so double-processing produces the correct result — but counts (Imported/Updated) may be wrong. Status will be correct once both runs complete.

**Fix path (if needed)**: Replace `Channel<Guid>` + startup re-scan with `SELECT ... FOR UPDATE SKIP LOCKED` polling against the `ImportJobs` table. PostgreSQL's `SKIP LOCKED` is what Hangfire, Quartz, and most job frameworks use under the hood for multi-node safety. No migration needed — the job table already has the right shape.

---

## Consequences

**Positive**:
- Zero new dependencies or infrastructure costs
- Immediate 202 response regardless of CSV size
- Progress polling gives real-time feedback in the UI
- Crash-safe: no jobs lost across server restarts
- UNNEST batching keeps 10,000-card imports under ~5 seconds

**Negative**:
- Multi-node deployments would double-process jobs on restart (acceptable for current scale)
- No built-in retry on failure (a failed job stays Failed; user must re-upload)
- CSV bytes stored in PostgreSQL `bytea` (max ~50 MB per job) — Blob Storage would be better at scale

---

## Related

- [ADR-003: PostgreSQL](ADR-003-postgresql-database.md) — job store database
- [ADR-013: GitHub Actions CI/CD](ADR-013-github-actions-cicd.md) — single-node App Service deployment (makes multi-node moot for now)
