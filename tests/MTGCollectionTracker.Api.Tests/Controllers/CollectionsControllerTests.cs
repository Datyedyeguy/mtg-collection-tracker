using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Controllers;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;
using MTGCollectionTracker.Shared.DTOs.Collections;
using Shouldly;

namespace MTGCollectionTracker.Api.Tests.Controllers;

[TestClass]
public class CollectionsControllerTests
{
    private AppDbContext _dbContext = null!;
    private CollectionsController _controller = null!;
    private const string TestUserId = "test-user-123";

    [TestInitialize]
    public void SetUp()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        // Create controller with mocked user context
        _controller = new CollectionsController(_dbContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [TestCleanup]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetCollection_WithNoCards_ReturnsEmptyCollection()
    {
        // Act
        var result = await _controller.GetCollection();

        // Assert
        result.ShouldBeOfType<ActionResult<CollectionResponseDto>>();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.Entries.ShouldBeEmpty();
        response.TotalCards.ShouldBe(0);
        response.TotalUniqueCards.ShouldBe(0);
        response.CurrentPage.ShouldBe(1);
        response.PageSize.ShouldBe(50);
        response.TotalPages.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetCollection_WithCards_ReturnsPaginatedResults()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 100);

        // Act
        var result = await _controller.GetCollection(page: 1, pageSize: 20);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.Entries.Count.ShouldBe(20); // First page
        response.TotalUniqueCards.ShouldBe(100);
        response.TotalCards.ShouldBe(100); // Each card has quantity 1
        response.CurrentPage.ShouldBe(1);
        response.PageSize.ShouldBe(20);
        response.TotalPages.ShouldBe(5); // 100 / 20
        response.HasNextPage.ShouldBeTrue();
        response.HasPreviousPage.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetCollection_WithPage2_ReturnsSecondPageResults()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 100);

        // Act
        var result = await _controller.GetCollection(page: 2, pageSize: 20);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.Entries.Count.ShouldBe(20);
        response.CurrentPage.ShouldBe(2);
        response.HasNextPage.ShouldBeTrue();
        response.HasPreviousPage.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetCollection_WithLastPage_ReturnsRemainingCards()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 55);

        // Act: Request page 2 with size 50 (should have 5 cards)
        var result = await _controller.GetCollection(page: 2, pageSize: 50);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.Entries.Count.ShouldBe(5);
        response.TotalUniqueCards.ShouldBe(55);
        response.CurrentPage.ShouldBe(2);
        response.TotalPages.ShouldBe(2);
        response.HasNextPage.ShouldBeFalse();
        response.HasPreviousPage.ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetCollection_WithPlatformFilter_ReturnsOnlyMatchingCards()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 30, platform: Platform.Paper);
        await SeedTestCollectionAsync(TestUserId, cardCount: 20, platform: Platform.Arena, startIndex: 30);

        // Act
        var result = await _controller.GetCollection(platform: MTGCollectionTracker.Shared.Enums.Platform.Paper);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.TotalUniqueCards.ShouldBe(30);
        response.Entries.ShouldAllBe(e => e.Platform == MTGCollectionTracker.Shared.Enums.Platform.Paper);
    }

    [TestMethod]
    public async Task GetCollection_WithMultiplePlatforms_ReturnsCardsByPlatform()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 10, platform: Platform.Paper);
        await SeedTestCollectionAsync(TestUserId, cardCount: 15, platform: Platform.Arena, startIndex: 10);
        await SeedTestCollectionAsync(TestUserId, cardCount: 5, platform: Platform.Mtgo, startIndex: 25);

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.CardsByPlatform.Count.ShouldBe(3);
        response.CardsByPlatform[MTGCollectionTracker.Shared.Enums.Platform.Paper].ShouldBe(10);
        response.CardsByPlatform[MTGCollectionTracker.Shared.Enums.Platform.Arena].ShouldBe(15);
        response.CardsByPlatform[MTGCollectionTracker.Shared.Enums.Platform.Mtgo].ShouldBe(5);
    }

    [TestMethod]
    public async Task GetCollection_WithQuantities_CalculatesTotalCorrectly()
    {
        // Arrange
        var cards = await SeedCardsAsync(3);
        await _dbContext.CollectionEntries.AddRangeAsync(
            new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = cards[0].Id,
                Platform = Platform.Paper,
                Quantity = 4, // 4x of first card
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = cards[1].Id,
                Platform = Platform.Paper,
                Quantity = 2, // 2x of second card
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = cards[2].Id,
                Platform = Platform.Arena,
                Quantity = 1, // 1x of third card
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.TotalUniqueCards.ShouldBe(3); // 3 unique cards
        response.TotalCards.ShouldBe(7); // 4 + 2 + 1 = 7 total copies
        response.CardsByPlatform[MTGCollectionTracker.Shared.Enums.Platform.Paper].ShouldBe(6); // 4 + 2
        response.CardsByPlatform[MTGCollectionTracker.Shared.Enums.Platform.Arena].ShouldBe(1);
    }

    [TestMethod]
    public async Task GetCollection_WithInvalidPageNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetCollection(page: 0);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task GetCollection_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Act - pageSize too small
        var result1 = await _controller.GetCollection(pageSize: 0);
        result1.Result.ShouldBeOfType<BadRequestObjectResult>();

        // Act - pageSize too large
        var result2 = await _controller.GetCollection(pageSize: 101);
        result2.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task GetCollection_WithNoUserClaim_ReturnsUnauthorized()
    {
        // Arrange - Create controller without user claims
        var controllerWithoutUser = new CollectionsController(_dbContext);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controllerWithoutUser.GetCollection();

        // Assert
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task GetCollection_WithDifferentUser_ReturnsOnlyTheirCards()
    {
        // Arrange
        await SeedTestCollectionAsync(TestUserId, cardCount: 10);
        await SeedTestCollectionAsync("other-user-456", cardCount: 5, startIndex: 10);

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.TotalUniqueCards.ShouldBe(10); // Only test user's cards
        response.Entries.ShouldAllBe(e => e.CardName.StartsWith("Test Card"));
    }

    [TestMethod]
    public async Task GetCollection_ResultsAreSortedByCardName()
    {
        // Arrange
        var cards = new[]
        {
            CreateCard("Zebra Strike", "ZNR", "100"),
            CreateCard("Ancestral Recall", "LEA", "1"),
            CreateCard("Lightning Bolt", "M21", "123")
        };
        await _dbContext.Cards.AddRangeAsync(cards);
        await _dbContext.SaveChangesAsync();

        foreach (var card in cards)
        {
            await _dbContext.CollectionEntries.AddAsync(new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = card.Id,
                Platform = Platform.Paper,
                Quantity = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        response.Entries[0].CardName.ShouldBe("Ancestral Recall");
        response.Entries[1].CardName.ShouldBe("Lightning Bolt");
        response.Entries[2].CardName.ShouldBe("Zebra Strike");
    }

    #region Helper Methods

    private async Task SeedTestCollectionAsync(
        string userId,
        int cardCount,
        Platform platform = Platform.Paper,
        int startIndex = 0)
    {
        var cards = await SeedCardsAsync(cardCount, startIndex);

        foreach (var card in cards)
        {
            await _dbContext.CollectionEntries.AddAsync(new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = card.Id,
                Platform = platform,
                Quantity = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<List<Card>> SeedCardsAsync(int count, int startIndex = 0)
    {
        var cards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            var index = startIndex + i;
            cards.Add(CreateCard($"Test Card {index:D3}", "TST", index.ToString()));
        }

        await _dbContext.Cards.AddRangeAsync(cards);
        await _dbContext.SaveChangesAsync();

        return cards;
    }

    private Card CreateCard(string name, string setCode, string collectorNumber)
    {
        return new Card
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid(),
            OracleId = Guid.NewGuid(),
            Name = name,
            SetCode = setCode,
            CollectorNumber = collectorNumber,
            Rarity = "common",
            TypeLine = "Creature",
            Cmc = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
