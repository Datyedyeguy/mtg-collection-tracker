using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Controllers;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Cards;
using Shouldly;

namespace MTGCollectionTracker.Api.Tests.Controllers;

[TestClass]
public class CardsControllerTests
{
    private AppDbContext _dbContext = null!;
    private CardsController _controller = null!;

    [TestInitialize]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _controller = new CardsController(_dbContext);
    }

    [TestCleanup]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // -------------------------------------------------------------------------
    // Validation tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_WithNoParameters_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchCards();

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchCards_WithOnlyWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchCards(q: "   ");

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchCards_WithInvalidPageNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchCards(q: "bolt", page: 0);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchCards_WithPageSizeTooSmall_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchCards(q: "bolt", pageSize: 0);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task SearchCards_WithPageSizeTooLarge_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchCards(q: "bolt", pageSize: 101);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    // -------------------------------------------------------------------------
    // Name search tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_ByName_ReturnsMatchingCards()
    {
        // Arrange
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "M21", "123"),
            CreateCard("Lightning Strike", "M21", "124"),
            CreateCard("Counterspell", "MH2", "56"));

        // Act
        var result = await _controller.SearchCards(q: "Lightning");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2);
        response.Cards.Count.ShouldBe(2);
        response.Cards.ShouldAllBe(c => c.Name.Contains("Lightning"));
    }

    [TestMethod]
    public async Task SearchCards_ByName_IsPartialMatch()
    {
        // Arrange — searching "bolt" should match "Lightning Bolt"
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "M21", "123"),
            CreateCard("Fireball", "M21", "140"));

        // Act
        var result = await _controller.SearchCards(q: "bolt");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Lightning Bolt");
    }

    [TestMethod]
    public async Task SearchCards_ByName_IsCaseInsensitive()
    {
        // Arrange
        await SeedCardsAsync(CreateCard("Lightning Bolt", "M21", "123"));

        // Act — search with all caps
        var result = await _controller.SearchCards(q: "LIGHTNING BOLT");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Lightning Bolt");
    }

    [TestMethod]
    public async Task SearchCards_ByName_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await SeedCardsAsync(CreateCard("Lightning Bolt", "M21", "123"));

        // Act
        var result = await _controller.SearchCards(q: "xyzzy");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(0);
        response.Cards.ShouldBeEmpty();
        response.TotalPages.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // Set filter tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_BySet_ReturnsOnlyCardsFromThatSet()
    {
        // Arrange
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "m21", "123"),
            CreateCard("Counterspell", "mh2", "56"),
            CreateCard("Dark Ritual", "mh2", "78"));

        // Act
        var result = await _controller.SearchCards(set: "mh2");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2);
        response.Cards.ShouldAllBe(c => c.SetCode == "mh2");
    }

    [TestMethod]
    public async Task SearchCards_BySet_IsCaseInsensitive()
    {
        // Arrange
        await SeedCardsAsync(CreateCard("Lightning Bolt", "m21", "123"));

        // Act — pass uppercase set code
        var result = await _controller.SearchCards(set: "M21");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
    }

    // -------------------------------------------------------------------------
    // Type filter tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_ByType_ReturnsMatchingCards()
    {
        // Arrange
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "M21", "123", typeLine: "Instant"),
            CreateCard("Grizzly Bears", "M21", "180", typeLine: "Creature — Bear"),
            CreateCard("Giant Growth", "M21", "181", typeLine: "Instant"));

        // Act
        var result = await _controller.SearchCards(type: "instant");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2);
        response.Cards.ShouldAllBe(c => c.TypeLine.ToLower().Contains("instant"));
    }

    [TestMethod]
    public async Task SearchCards_ByType_MatchesSubtypes()
    {
        // Arrange — "Bear" is a subtype so searching "bear" should work
        await SeedCardsAsync(
            CreateCard("Grizzly Bears", "M21", "180", typeLine: "Creature — Bear"),
            CreateCard("Lightning Bolt", "M21", "123", typeLine: "Instant"));

        // Act
        var result = await _controller.SearchCards(type: "bear");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Grizzly Bears");
    }

    // -------------------------------------------------------------------------
    // Combined filter tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_WithNameAndSet_AppliesBothFilters()
    {
        // Arrange — two Lightning Bolts in different sets, only one creature in M21
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "m21", "123"),
            CreateCard("Lightning Bolt", "lea", "161"),
            CreateCard("Lightning Strike", "m21", "156"));

        // Act — name contains "bolt" AND set is "m21"
        var result = await _controller.SearchCards(q: "bolt", set: "m21");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Lightning Bolt");
        response.Cards[0].SetCode.ShouldBe("m21");
    }

    // -------------------------------------------------------------------------
    // Pagination tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_WithPagination_ReturnsCorrectPage()
    {
        // Arrange — 25 creatures
        var cards = Enumerable.Range(1, 25)
            .Select(i => CreateCard($"Creature {i:D3}", "TST", i.ToString(), typeLine: "Creature"))
            .ToArray();
        await SeedCardsAsync(cards);

        // Act — page 2, 10 per page
        var result = await _controller.SearchCards(type: "creature", page: 2, pageSize: 10);

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(25);
        response.Page.ShouldBe(2);
        response.PageSize.ShouldBe(10);
        response.TotalPages.ShouldBe(3); // ceil(25/10) = 3
        response.Cards.Count.ShouldBe(10);
    }

    [TestMethod]
    public async Task SearchCards_LastPage_ReturnsRemainingItems()
    {
        // Arrange — 25 cards, 10 per page → last page has 5
        var cards = Enumerable.Range(1, 25)
            .Select(i => CreateCard($"Card {i:D3}", "TST", i.ToString()))
            .ToArray();
        await SeedCardsAsync(cards);

        // Act — page 3 of 3
        var result = await _controller.SearchCards(q: "card", page: 3, pageSize: 10);

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.Cards.Count.ShouldBe(5);
    }

    [TestMethod]
    public async Task SearchCards_ReturnsCorrectPaginationMetadata()
    {
        // Arrange — 7 cards
        var cards = Enumerable.Range(1, 7)
            .Select(i => CreateCard($"Spell {i}", "TST", i.ToString()))
            .ToArray();
        await SeedCardsAsync(cards);

        // Act
        var result = await _controller.SearchCards(q: "spell", page: 1, pageSize: 3);

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(7);
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(3);
        response.TotalPages.ShouldBe(3); // ceil(7/3) = 3
        response.Cards.Count.ShouldBe(3);
    }

    // -------------------------------------------------------------------------
    // Sorting tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_ResultsAreSortedByNameThenSetThenCollectorNumber()
    {
        // Arrange — same name, different sets and collector numbers
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "znr", "45"),
            CreateCard("Lightning Bolt", "lea", "161"),
            CreateCard("Counterspell", "mh2", "56"),
            CreateCard("Lightning Bolt", "m21", "123"));

        // Act
        var result = await _controller.SearchCards(q: "lightning bolt");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.Cards.Count.ShouldBe(3);
        // Ordered by name (all "Lightning Bolt"), then by set code alphabetically
        response.Cards[0].SetCode.ShouldBe("lea");
        response.Cards[1].SetCode.ShouldBe("m21");
        response.Cards[2].SetCode.ShouldBe("znr");
    }

    // -------------------------------------------------------------------------
    // Response field tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_ResponseContainsCoreCardFields()
    {
        // Arrange
        var card = new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = Guid.NewGuid(),
            Name = "Counterspell",
            SetCode = "mh2",
            CollectorNumber = "56",
            Rarity = "uncommon",
            ManaCost = "{U}{U}",
            Cmc = 2m,
            TypeLine = "Instant",
            OracleText = "Counter target spell.",
            Colors = JsonSerializer.Serialize(new[] { "U" }),
            Finishes = JsonSerializer.Serialize(new[] { "nonfoil", "foil" }),
            ArenaId = 99999,
            MtgoId = 12345,
            ImageUris = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["small"] = "https://cards.scryfall.io/small/front/example.jpg",
                ["normal"] = "https://cards.scryfall.io/normal/front/example.jpg",
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await SeedCardsAsync(card);

        // Act
        var result = await _controller.SearchCards(q: "counterspell");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.Cards.Count.ShouldBe(1);
        var dto = response.Cards[0];
        dto.Name.ShouldBe("Counterspell");
        dto.SetCode.ShouldBe("mh2");
        dto.CollectorNumber.ShouldBe("56");
        dto.Rarity.ShouldBe("uncommon");
        dto.ManaCost.ShouldBe("{U}{U}");
        dto.Cmc.ShouldBe(2m);
        dto.TypeLine.ShouldBe("Instant");
        dto.OracleText.ShouldBe("Counter target spell.");
        dto.Colors.ShouldNotBeNull();
        dto.Colors!.ShouldContain("U");
        dto.Finishes.ShouldNotBeNull();
        dto.Finishes!.ShouldContain("nonfoil");
        dto.Finishes!.ShouldContain("foil");
        dto.ArenaId.ShouldBe(99999);
        dto.MtgoId.ShouldBe(12345);
        dto.ImageUri.ShouldBe("https://cards.scryfall.io/normal/front/example.jpg");
        dto.IsMultiFaced.ShouldBeFalse();
        dto.Faces.ShouldBeNull();
    }

    [TestMethod]
    public async Task SearchCards_QueryStringIsEchoedInResponse()
    {
        // Arrange
        await SeedCardsAsync(CreateCard("Lightning Bolt", "M21", "123"));

        // Act
        var result = await _controller.SearchCards(q: "  Lightning  ");

        // Assert — query should be trimmed in the response
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.Query.ShouldBe("Lightning");
    }

    [TestMethod]
    public async Task SearchCards_MultiFacedCard_HasIsMultiFacedTrue()
    {
        // Arrange — simulate a transform card with Faces JSON
        var faces = new List<CardFaceDto>
        {
            new()
            {
                Name = "Delver of Secrets",
                TypeLine = "Creature — Human Wizard",
                ImageUri = "https://cards.scryfall.io/normal/front/delver.jpg",
            },
            new()
            {
                Name = "Insectile Aberration",
                TypeLine = "Creature — Human Insect",
                ImageUri = "https://cards.scryfall.io/normal/back/delver.jpg",
            },
        };

        var card = new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = Guid.NewGuid(),
            Name = "Delver of Secrets // Insectile Aberration",
            SetCode = "isd",
            CollectorNumber = "51",
            Rarity = "common",
            TypeLine = "Creature — Human Wizard // Creature — Human Insect",
            Cmc = 1m,
            Faces = JsonSerializer.Serialize(faces),
            // Multi-faced cards have no top-level ImageUris
            ImageUris = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await SeedCardsAsync(card);

        // Act
        var result = await _controller.SearchCards(q: "Delver");

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.Cards.Count.ShouldBe(1);
        var dto = response.Cards[0];
        dto.IsMultiFaced.ShouldBeTrue();
        dto.Faces.ShouldNotBeNull();
        dto.Faces!.Count.ShouldBe(2);
        // Image URI should be extracted from the front face
        dto.ImageUri.ShouldBe("https://cards.scryfall.io/normal/front/delver.jpg");
    }

    // -------------------------------------------------------------------------
    // Deduplication tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SearchCards_ByDefault_ReturnsOneResultPerOracleId()
    {
        // Arrange — 3 printings of "Lightning Bolt", each with the same OracleId
        var sharedOracleId = Guid.NewGuid();
        await SeedCardsAsync(
            CreateCardWithOracle("Lightning Bolt", "lea", "161", sharedOracleId),
            CreateCardWithOracle("Lightning Bolt", "m21", "123", sharedOracleId),
            CreateCardWithOracle("Lightning Bolt", "znr", "45", sharedOracleId));

        // Act — default allPrintings=false
        var result = await _controller.SearchCards(q: "Lightning Bolt");

        // Assert — should collapse to 1 result
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards.Count.ShouldBe(1);
        response.AllPrintings.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SearchCards_WithAllPrintings_ReturnsEveryPrinting()
    {
        // Arrange — same 3 printings
        var sharedOracleId = Guid.NewGuid();
        await SeedCardsAsync(
            CreateCardWithOracle("Lightning Bolt", "lea", "161", sharedOracleId),
            CreateCardWithOracle("Lightning Bolt", "m21", "123", sharedOracleId),
            CreateCardWithOracle("Lightning Bolt", "znr", "45", sharedOracleId));

        // Act — explicitly request all printings
        var result = await _controller.SearchCards(q: "Lightning Bolt", allPrintings: true);

        // Assert — all 3 printings returned
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(3);
        response.Cards.Count.ShouldBe(3);
        response.AllPrintings.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SearchCards_Deduplication_KeepsCardsWithDifferentOracleIds()
    {
        // Arrange — 2 different cards, 1 printing each
        await SeedCardsAsync(
            CreateCard("Lightning Bolt", "m21", "123"),
            CreateCard("Counterspell", "mh2", "56"));

        // Act
        var result = await _controller.SearchCards(q: "l");

        // Assert — both cards appear since they have different OracleIds
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(2);
    }

    [TestMethod]
    public async Task SearchCards_FlavorName_MatchesSearchQuery()
    {
        // Arrange — "Isshin, Two Heavens as One" with a Final Fantasy showcase printing
        // whose flavor name is "Lightning, Lone Commando".
        // Searching "lightnin" should find the card via its flavor name.
        var sharedOracleId = Guid.NewGuid();
        await SeedCardsAsync(
            CreateCardWithOracle("Isshin, Two Heavens as One", "neo", "229", sharedOracleId),
            CreateCardWithOracle("Isshin, Two Heavens as One", "fca", "12", sharedOracleId,
                flavorName: "Lightning, Lone Commando"));

        // Act — search by the flavor name, not the canonical name
        var result = await _controller.SearchCards(q: "lightnin");

        // Assert — 1 result (deduplicated), and it uses the canonical name
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards.Count.ShouldBe(1);
        response.Cards[0].Name.ShouldBe("Isshin, Two Heavens as One");
        response.Cards[0].FlavorName.ShouldBeNull(); // canonical printing has no flavor name
        response.Cards[0].MatchedFlavorName.ShouldBe("Lightning, Lone Commando"); // explains why it appeared
    }

    [TestMethod]
    public async Task SearchCards_FlavorName_CanonicalNamePrefersNoFlavorName()
    {
        // Arrange — only flavor-name printings exist (no canonical in DB).
        // Should fall back to first in group.
        var sharedOracleId = Guid.NewGuid();
        await SeedCardsAsync(
            CreateCardWithOracle("Isshin, Two Heavens as One", "fca", "12", sharedOracleId,
                flavorName: "Lightning, Lone Commando"));

        var result = await _controller.SearchCards(q: "lightnin");

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<CardSearchResponseDto>();

        response.TotalCards.ShouldBe(1);
        response.Cards[0].FlavorName.ShouldBe("Lightning, Lone Commando");
    }

    // -------------------------------------------------------------------------
    // Helper methods
    // -------------------------------------------------------------------------

    private async Task SeedCardsAsync(params Card[] cards)
    {
        await _dbContext.Cards.AddRangeAsync(cards);
        await _dbContext.SaveChangesAsync();
    }

    private static Card CreateCard(
        string name,
        string setCode,
        string collectorNumber,
        string typeLine = "Instant")
    {
        return new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = Guid.NewGuid(), // unique oracle per call — for non-dedup tests
            Name = name,
            SetCode = setCode.ToLower(),
            CollectorNumber = collectorNumber,
            Rarity = "common",
            TypeLine = typeLine,
            Cmc = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Creates a card with an explicit OracleId, for deduplication tests.</summary>
    private static Card CreateCardWithOracle(
        string name,
        string setCode,
        string collectorNumber,
        Guid oracleId,
        string? flavorName = null)
    {
        return new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = oracleId,
            Name = name,
            FlavorName = flavorName,
            SetCode = setCode.ToLower(),
            CollectorNumber = collectorNumber,
            Rarity = "common",
            TypeLine = "Instant",
            Cmc = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
