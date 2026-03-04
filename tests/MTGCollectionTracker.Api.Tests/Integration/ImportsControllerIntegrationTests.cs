using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Controllers;
using MTGCollectionTracker.Api.Services;
using MTGCollectionTracker.Api.Tests.Infrastructure;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;
using Shouldly;
using Testcontainers.PostgreSql;
using DataPlatform = MTGCollectionTracker.Data.Entities.Platform;

namespace MTGCollectionTracker.Api.Tests.Integration;

/// <summary>
/// Integration tests for the full Manabox import background-job pipeline:
///   ImportsController (POST/GET) → ImportJobQueue → ImportWorkerService → PostgreSQL
///
/// The worker is instantiated directly and driven via a short-lived cancellation token
/// so each test fully exercises the real processing path without needing a hosted environment.
///
/// Why integration tests?
/// The core logic uses raw PostgreSQL UNNEST + INSERT … ON CONFLICT SQL, which cannot be
/// validated by the in-memory EF provider. TestContainers provides a throw-away real database.
/// </summary>
[DoNotParallelize]
[TestClass]
public class ImportsControllerIntegrationTests
{
    private static PostgreSqlContainer _container = null!;
    private static string _connectionString = null!;

    private AppDbContext _db = null!;
    private ImportsController _controller = null!;
    private ImportJobQueue _queue = null!;
    private ImportWorkerService _worker = null!;
    private IServiceScopeFactory _scopeFactory = null!;

    private const string TestUserId = "test-user-imports-integration";

    // ── Test lifecycle ────────────────────────────────────────────────────────

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        await using var ctx = IntegrationTestSetup.CreateDbContext(_connectionString);
        await ctx.Database.EnsureCreatedAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup() => await _container.DisposeAsync();

    [TestInitialize]
    public async Task SetUp()
    {
        _db = IntegrationTestSetup.CreateDbContext(_connectionString);

        // Wipe all data between tests (cascade handles child tables)
        await _db.Database.ExecuteSqlRawAsync(@"TRUNCATE TABLE ""AspNetUsers"", ""Cards"" CASCADE");

        // Seed a test user (ImportJob.UserId is a FK to AspNetUsers)
        _db.Users.Add(new ApplicationUser
        {
            Id                 = TestUserId,
            UserName           = "imports_test_user",
            NormalizedUserName = "IMPORTS_TEST_USER",
            Email              = "imports@integration.test",
            NormalizedEmail    = "IMPORTS@INTEGRATION.TEST",
            SecurityStamp      = Guid.NewGuid().ToString(),
            ConcurrencyStamp   = Guid.NewGuid().ToString()
        });
        await _db.SaveChangesAsync();

        // Build a minimal DI container so the worker can resolve scoped AppDbContext
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_connectionString));
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _queue = new ImportJobQueue();
        _worker = new ImportWorkerService(
            _queue,
            _scopeFactory,
            new ManaboxCsvParser(),
            NullLogger<ImportWorkerService>.Instance);

        _controller = new ImportsController(_db, _queue);
        SetControllerUser(_controller, TestUserId);
    }

    [TestCleanup]
    public void TearDown() => _db.Dispose();

    // ── Accumulate tests ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task Accumulate_NewCards_InsertsAllMatchedRows()
    {
        var bolt  = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var count = await SeedCardAsync("Counterspell", "m21", "46");

        var csv = BuildCsv(
            Row("Main Deck", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 3),
            Row("Main Deck", "binder", "Counterspell",   count.ScryfallId, "normal", 2));

        var jobId = await SubmitAndWaitAsync(csv, ["Main Deck"], ImportMode.Accumulate);

        var entries = await _db.CollectionEntries
            .Where(e => e.UserId == TestUserId && e.Platform == DataPlatform.Paper)
            .ToListAsync();

        entries.Count.ShouldBe(2);
        entries.Single(e => e.CardId == bolt.Id).Quantity.ShouldBe(3);
        entries.Single(e => e.CardId == count.Id).Quantity.ShouldBe(2);

        var job = await GetJobAsync(jobId);
        job.Imported.ShouldBe(2);
        job.Updated.ShouldBe(0);
    }

    [TestMethod]
    public async Task Accumulate_ExistingEntry_AddsToQuantity()
    {
        var bolt = await SeedCardAsync("Lightning Bolt", "m21", "123");
        _db.CollectionEntries.Add(MakeEntry(bolt.Id, TestUserId, DataPlatform.Paper, quantity: 2));
        await _db.SaveChangesAsync();

        var csv = BuildCsv(
            Row("Main Deck", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 3));

        await SubmitAndWaitAsync(csv, ["Main Deck"], ImportMode.Accumulate);

        // AsNoTracking bypasses _db's identity map (which still holds qty=2 from seeding)
        // so we see the value the worker actually committed.
        var entry = await _db.CollectionEntries
            .AsNoTracking()
            .SingleAsync(e => e.UserId == TestUserId && e.CardId == bolt.Id);
        entry.Quantity.ShouldBe(5); // 2 existing + 3 imported
    }

    [TestMethod]
    public async Task Accumulate_FoilRows_SetsFoilQuantity()
    {
        var bolt = await SeedCardAsync("Lightning Bolt", "m21", "123");

        var csv = BuildCsv(
            Row("Foils", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 2),
            Row("Foils", "binder", "Lightning Bolt", bolt.ScryfallId, "foil",   1));

        // Both rows have the same ScryfallId; the parser aggregates: qty=2 + foil=1
        await SubmitAndWaitAsync(csv, ["Foils"], ImportMode.Accumulate);

        var entry = await _db.CollectionEntries
            .SingleAsync(e => e.UserId == TestUserId && e.CardId == bolt.Id);
        entry.Quantity.ShouldBe(2);
        entry.FoilQuantity.ShouldBe(1);
    }

    // ── Replace tests ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Replace_ClearsExistingPaperAndInsertsFresh()
    {
        var bolt  = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var count = await SeedCardAsync("Counterspell", "m21", "46");

        // Pre-existing entry that should be wiped
        _db.CollectionEntries.Add(MakeEntry(count.Id, TestUserId, DataPlatform.Paper, quantity: 5));
        await _db.SaveChangesAsync();

        var csv = BuildCsv(
            Row("New Deck", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 4));

        await SubmitAndWaitAsync(csv, ["New Deck"], ImportMode.Replace);

        var entries = await _db.CollectionEntries
            .Where(e => e.UserId == TestUserId && e.Platform == DataPlatform.Paper)
            .ToListAsync();

        entries.Count.ShouldBe(1);
        entries[0].CardId.ShouldBe(bolt.Id);
        entries[0].Quantity.ShouldBe(4);
    }

    [TestMethod]
    public async Task Replace_DoesNotAffectArenaEntries()
    {
        var bolt = await SeedCardAsync("Lightning Bolt", "m21", "123");
        _db.CollectionEntries.Add(MakeEntry(bolt.Id, TestUserId, DataPlatform.Arena, quantity: 10));
        await _db.SaveChangesAsync();

        var csv = BuildCsv(
            Row("New Deck", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 1));

        await SubmitAndWaitAsync(csv, ["New Deck"], ImportMode.Replace);

        // Arena entry should be untouched
        var arenaEntry = await _db.CollectionEntries
            .SingleAsync(e => e.UserId == TestUserId && e.Platform == DataPlatform.Arena);
        arenaEntry.Quantity.ShouldBe(10);
    }

    // ── Binder filtering ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task BinderFilter_ExcludedBinder_NotImported()
    {
        var bolt  = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var count = await SeedCardAsync("Counterspell", "m21", "46");

        var csv = BuildCsv(
            Row("Keep Binder",   "binder", "Lightning Bolt", bolt.ScryfallId,  "normal", 3),
            Row("Skip Binder",   "binder", "Counterspell",   count.ScryfallId, "normal", 2));

        // Only import "Keep Binder"
        await SubmitAndWaitAsync(csv, ["Keep Binder"], ImportMode.Accumulate);

        var entries = await _db.CollectionEntries
            .Where(e => e.UserId == TestUserId)
            .ToListAsync();

        entries.Count.ShouldBe(1);
        entries[0].CardId.ShouldBe(bolt.Id);
    }

    // ── Skipped cards ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task UnknownScryfallId_ReportedInSkippedCards()
    {
        var known = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var unknownId = Guid.NewGuid(); // not in Cards table

        var csv = BuildCsv(
            Row("Main Deck", "binder", "Lightning Bolt",  known.ScryfallId, "normal", 1),
            Row("Main Deck", "binder", "Fake Card",       unknownId,        "normal", 1));

        var jobId = await SubmitAndWaitAsync(csv, ["Main Deck"], ImportMode.Accumulate);
        var job = await GetJobAsync(jobId);

        job.Skipped.ShouldBe(1);
        var skipped = JsonSerializer.Deserialize<List<string>>(job.SkippedCardsJson!)!;
        skipped.ShouldContain("Fake Card");
    }

    // ── Status endpoint ───────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetStatus_CompletedJob_ReturnsResultDto()
    {
        var bolt = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var csv = BuildCsv(Row("Main", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 2));

        var jobId = await SubmitAndWaitAsync(csv, ["Main"], ImportMode.Accumulate);

        var result = await _controller.GetStatus(jobId);
        var dto = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ImportJobStatusDto>();

        dto.JobId.ShouldBe(jobId);
        dto.Status.ShouldBe("Completed");
        dto.Progress.ShouldBe(100);
        dto.Result.ShouldNotBeNull();
        dto.Result!.Imported.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetStatus_OtherUsersJob_Returns404()
    {
        var bolt = await SeedCardAsync("Lightning Bolt", "m21", "123");
        var csv = BuildCsv(Row("Main", "binder", "Lightning Bolt", bolt.ScryfallId, "normal", 1));
        var jobId = await SubmitAndWaitAsync(csv, ["Main"], ImportMode.Accumulate);

        // Request from a different user
        var otherController = new ImportsController(_db, _queue);
        SetControllerUser(otherController, "other-user-id");

        var result = await otherController.GetStatus(jobId);
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Submits the CSV via the controller and drives the worker until the job reaches a terminal state.
    /// Returns the job ID on success; throws if it times out or fails.
    /// </summary>
    private async Task<Guid> SubmitAndWaitAsync(
        string csv,
        List<string> binders,
        ImportMode mode,
        int timeoutSeconds = 30)
    {
        // POST via controller
        var file = MakeFormFile(csv);
        var bindersJson = JsonSerializer.Serialize(binders);
        var httpResult = await _controller.SubmitManaboxImport(file, mode.ToString(), bindersJson);

        var accepted = httpResult.ShouldBeOfType<AcceptedResult>()
            .Value.ShouldBeOfType<ImportJobAcceptedDto>();
        var jobId = accepted.JobId;

        // Drive the worker: start it, wait for the job to finish, then stop it
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await _worker.StartAsync(cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            await using var ctx = IntegrationTestSetup.CreateDbContext(_connectionString);
            var job = await ctx.ImportJobs.FindAsync(jobId);
            if (job?.Status is ImportJobStatus.Completed or ImportJobStatus.Failed)
            {
                await _worker.StopAsync(CancellationToken.None);

                if (job.Status == ImportJobStatus.Failed)
                    Assert.Fail($"Import job failed: {job.ErrorMessage}");

                return jobId;
            }

            await Task.Delay(200, cts.Token);
        }

        await _worker.StopAsync(CancellationToken.None);
        throw new TimeoutException($"Import job {jobId} did not complete within {timeoutSeconds}s.");
    }

    private async Task<ImportJob> GetJobAsync(Guid jobId)
    {
        await using var ctx = IntegrationTestSetup.CreateDbContext(_connectionString);
        return await ctx.ImportJobs.FindAsync(jobId)
               ?? throw new InvalidOperationException($"Job {jobId} not found");
    }

    private static readonly string CsvHeader =
        "Binder Name,Binder Type,Name,Set code,Set name,Collector number,Foil,Rarity," +
        "Quantity,ManaBox ID,Scryfall ID,Purchase price,Misprint,Altered,Condition,Language,Purchase price currency";

    private static string Row(string binderName, string binderType, string name,
        Guid scryfallId, string foil, int qty) =>
        $"\"{binderName}\",\"{binderType}\",\"{name}\",m21,\"Core Set 2021\",100," +
        $"\"{foil}\",common,{qty},12345,{scryfallId},0.50,False,False,NM,English,USD";

    private static string BuildCsv(params string[] rows) =>
        CsvHeader + "\n" + string.Join("\n", rows);

    private static IFormFile MakeFormFile(string csv)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return new MemoryFormFile(bytes, "collection.csv");
    }

    /// <summary>
    /// Concrete <see cref="IFormFile"/> backed by an in-memory byte array.
    /// More reliable than NSubstitute mocks for async copy operations.
    /// </summary>
    private sealed class MemoryFormFile : IFormFile
    {
        private readonly byte[] _bytes;

        public MemoryFormFile(byte[] bytes, string fileName)
        {
            _bytes = bytes;
            FileName = fileName;
            Length = bytes.Length;
        }

        public string ContentType { get; } = "text/csv";
        public string ContentDisposition { get; } = "form-data; name=\"file\"; filename=\"collection.csv\"";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length { get; }
        public string Name { get; } = "file";
        public string FileName { get; }

        public void CopyTo(Stream target) => target.Write(_bytes, 0, _bytes.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => target.WriteAsync(_bytes, 0, _bytes.Length, cancellationToken);

        public Stream OpenReadStream() => new MemoryStream(_bytes);
    }

    private static void SetControllerUser(ImportsController controller, string userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId)
        ]));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private async Task<Card> SeedCardAsync(string name, string setCode, string collectorNumber)
    {
        var card = new Card
        {
            Id               = Guid.NewGuid(),
            ScryfallId       = Guid.NewGuid(),
            OracleId         = Guid.NewGuid(),
            Name             = name,
            SetCode          = setCode,
            CollectorNumber  = collectorNumber,
            Rarity           = "common",
            TypeLine         = "Instant",
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync();
        return card;
    }

    private static CollectionEntry MakeEntry(Guid cardId, string userId, DataPlatform platform,
        int quantity = 1, int foilQuantity = 0) => new()
    {
        Id            = Guid.NewGuid(),
        UserId        = userId,
        CardId        = cardId,
        Platform      = platform,
        Quantity      = quantity,
        FoilQuantity  = foilQuantity,
        CreatedAt     = DateTime.UtcNow,
        UpdatedAt     = DateTime.UtcNow
    };
}
