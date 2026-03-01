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

    [TestMethod]
    public async Task GetCollection_WithImageUris_ReturnsNormalImageUri()
    {
        // Arrange — card with normal image URL in its ImageUris JSON
        const string imageUrl = "https://cards.scryfall.io/normal/front/test.jpg";
        var card = CreateCard("Lightning Bolt", "M21", "123",
            imageUrisJson: $"{{\"small\":\"https://small.jpg\",\"normal\":\"{imageUrl}\"}}");
        await _dbContext.Cards.AddAsync(card);
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
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        var entry = response.Entries.ShouldHaveSingleItem();
        entry.ImageUri.ShouldBe(imageUrl);
    }

    [TestMethod]
    public async Task GetCollection_WithNoImageUris_ReturnsNullImageUri()
    {
        // Arrange — card without any image data
        var card = CreateCard("Lightning Bolt", "M21", "123", imageUrisJson: null);
        await _dbContext.Cards.AddAsync(card);
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
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCollection();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<CollectionResponseDto>();

        var entry = response.Entries.ShouldHaveSingleItem();
        entry.ImageUri.ShouldBeNull();
    }

    // ── AddToCollection tests ─────────────────────────────────────────────────

    [TestMethod]
    public async Task AddToCollection_NewCard_Returns201WithCorrectEntry()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = 4,
            FoilQuantity = 1
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var entry = createdResult.Value.ShouldBeOfType<CollectionEntryDto>();

        entry.CardId.ShouldBe(card.Id);
        entry.CardName.ShouldBe("Lightning Bolt");
        entry.SetCode.ShouldBe("M21");
        entry.Platform.ShouldBe(MTGCollectionTracker.Shared.Enums.Platform.Paper);
        entry.Quantity.ShouldBe(4);
        entry.FoilQuantity.ShouldBe(1);

        // Verify persisted to database
        var dbEntry = await _dbContext.CollectionEntries.SingleAsync();
        dbEntry.Quantity.ShouldBe(4);
        dbEntry.FoilQuantity.ShouldBe(1);
    }

    [TestMethod]
    public async Task AddToCollection_ExistingCardSamePlatform_AccumulatesQuantityAndReturns200()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var existing = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 2,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(existing);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = 3,
            FoilQuantity = 1
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var entry = okResult.Value.ShouldBeOfType<CollectionEntryDto>();

        entry.Quantity.ShouldBe(5);   // 2 + 3
        entry.FoilQuantity.ShouldBe(1); // 0 + 1

        // Verify only one entry in database (upsert, not insert)
        var dbEntries = await _dbContext.CollectionEntries.ToListAsync();
        dbEntries.Count.ShouldBe(1);
        dbEntries[0].Quantity.ShouldBe(5);
        dbEntries[0].FoilQuantity.ShouldBe(1);
    }

    [TestMethod]
    public async Task AddToCollection_SameCardDifferentPlatform_CreatesSeparateEntry()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var existing = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(existing);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Arena,
            Quantity = 4,
            FoilQuantity = 0
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        result.Result.ShouldBeOfType<CreatedAtActionResult>();

        // Two separate entries — one Paper, one Arena
        var dbEntries = await _dbContext.CollectionEntries.ToListAsync();
        dbEntries.Count.ShouldBe(2);
        dbEntries.ShouldContain(e => e.Platform == Platform.Paper && e.Quantity == 4);
        dbEntries.ShouldContain(e => e.Platform == Platform.Arena && e.Quantity == 4);
    }

    [TestMethod]
    public async Task AddToCollection_NonexistentCardId_Returns404()
    {
        // Arrange
        var request = new AddToCollectionRequest
        {
            CardId = Guid.NewGuid(), // Does not exist in DB
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = 1,
            FoilQuantity = 0
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task AddToCollection_BothQuantitiesZero_Returns400()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = 0,
            FoilQuantity = 0
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task AddToCollection_NegativeQuantity_Returns400()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = -1,
            FoilQuantity = 0
        };

        // Act
        var result = await _controller.AddToCollection(request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task AddToCollection_NoUserClaim_ReturnsUnauthorized()
    {
        // Arrange - controller without user identity
        var controllerWithoutUser = new CollectionsController(_dbContext);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        var request = new AddToCollectionRequest
        {
            CardId = card.Id,
            Platform = MTGCollectionTracker.Shared.Enums.Platform.Paper,
            Quantity = 1,
            FoilQuantity = 0
        };

        // Act
        var result = await controllerWithoutUser.AddToCollection(request);

        // Assert
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    // ── GetCardOwnership tests ──────────────────────────────────────────────

    [TestMethod]
    public async Task GetCardOwnership_NotOwnedAnywhere_ReturnsEmptyList()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCardOwnership(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var entries = okResult.Value.ShouldBeOfType<List<CollectionEntryDto>>();
        entries.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetCardOwnership_OwnedOnOnePlatform_ReturnsSingleEntry()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.CollectionEntries.AddAsync(new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCardOwnership(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var entries = okResult.Value.ShouldBeOfType<List<CollectionEntryDto>>();
        entries.Count.ShouldBe(1);
        entries[0].Quantity.ShouldBe(4);
        entries[0].FoilQuantity.ShouldBe(1);
        entries[0].Platform.ShouldBe(MTGCollectionTracker.Shared.Enums.Platform.Paper);
    }

    [TestMethod]
    public async Task GetCardOwnership_OwnedOnMultiplePlatforms_ReturnsAllEntries()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.CollectionEntries.AddRangeAsync(
            new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = card.Id,
                Platform = Platform.Paper,
                Quantity = 4,
                FoilQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CollectionEntry
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                CardId = card.Id,
                Platform = Platform.Arena,
                Quantity = 4,
                FoilQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCardOwnership(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var entries = okResult.Value.ShouldBeOfType<List<CollectionEntryDto>>();
        entries.Count.ShouldBe(2);
        entries.ShouldContain(e => e.Platform == MTGCollectionTracker.Shared.Enums.Platform.Paper);
        entries.ShouldContain(e => e.Platform == MTGCollectionTracker.Shared.Enums.Platform.Arena);
    }

    [TestMethod]
    public async Task GetCardOwnership_OtherUsersCards_AreNotReturned()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        // Another user owns 4 copies — should not appear in test user's results
        await _dbContext.CollectionEntries.AddAsync(new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = "other-user-456",
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCardOwnership(card.Id);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var entries = okResult.Value.ShouldBeOfType<List<CollectionEntryDto>>();
        entries.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetCardOwnership_NonexistentCardId_Returns404()
    {
        // Act
        var result = await _controller.GetCardOwnership(Guid.NewGuid());

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task GetCardOwnership_NoUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controllerWithoutUser = new CollectionsController(_dbContext);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await controllerWithoutUser.GetCardOwnership(card.Id);

        // Assert
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    // ── UpdateCollectionEntry tests ─────────────────────────────────────────

    [TestMethod]
    public async Task UpdateCollectionEntry_ValidRequest_Returns200WithUpdatedEntry()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest
        {
            Quantity = 2,
            FoilQuantity = 3
        };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<CollectionEntryDto>();

        dto.Quantity.ShouldBe(2);
        dto.FoilQuantity.ShouldBe(3);
        dto.CardName.ShouldBe("Lightning Bolt");
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_ValidRequest_UpdatesDatabase()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var originalUpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 1,
            CreatedAt = originalUpdatedAt,
            UpdatedAt = originalUpdatedAt
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest
        {
            Quantity = 10,
            FoilQuantity = 0
        };

        // Act
        await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        var dbEntry = await _dbContext.CollectionEntries.SingleAsync();
        dbEntry.Quantity.ShouldBe(10);
        dbEntry.FoilQuantity.ShouldBe(0);
        dbEntry.UpdatedAt.ShouldBeGreaterThan(originalUpdatedAt);
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_NonexistentId_Returns404()
    {
        // Act
        var request = new UpdateCollectionEntryRequest { Quantity = 1, FoilQuantity = 0 };
        var result = await _controller.UpdateCollectionEntry(Guid.NewGuid(), request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_OtherUsersEntry_Returns404()
    {
        // Arrange — entry belongs to a different user
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = "other-user-456",
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest { Quantity = 1, FoilQuantity = 0 };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_BothQuantitiesZero_Returns400()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest { Quantity = 0, FoilQuantity = 0 };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_NegativeQuantity_Returns400()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest { Quantity = -1, FoilQuantity = 0 };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_NegativeFoilQuantity_Returns400()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest { Quantity = 1, FoilQuantity = -5 };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_NoUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controllerWithoutUser = new CollectionsController(_dbContext);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new UpdateCollectionEntryRequest { Quantity = 1, FoilQuantity = 0 };

        // Act
        var result = await controllerWithoutUser.UpdateCollectionEntry(Guid.NewGuid(), request);

        // Assert
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_SetQuantityToZeroWithFoil_Succeeds()
    {
        // Arrange — user wants 0 nonfoil but 2 foil (valid: at least one > 0)
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateCollectionEntryRequest { Quantity = 0, FoilQuantity = 2 };

        // Act
        var result = await _controller.UpdateCollectionEntry(entry.Id, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<CollectionEntryDto>();
        dto.Quantity.ShouldBe(0);
        dto.FoilQuantity.ShouldBe(2);
    }

    // ── DeleteCollectionEntry tests ─────────────────────────────────────────

    [TestMethod]
    public async Task DeleteCollectionEntry_ValidEntry_Returns204()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCollectionEntry(entry.Id);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_ValidEntry_RemovesFromDatabase()
    {
        // Arrange
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        await _controller.DeleteCollectionEntry(entry.Id);

        // Assert
        var dbEntries = await _dbContext.CollectionEntries.ToListAsync();
        dbEntries.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_NonexistentId_Returns404()
    {
        // Act
        var result = await _controller.DeleteCollectionEntry(Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_OtherUsersEntry_Returns404()
    {
        // Arrange — entry belongs to a different user
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = "other-user-456",
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteCollectionEntry(entry.Id);

        // Assert — returns 404 (not 403) to avoid leaking existence of other users' entries
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_OtherUsersEntry_DoesNotDeleteFromDatabase()
    {
        // Arrange — entry belongs to a different user
        var card = CreateCard("Lightning Bolt", "M21", "123");
        await _dbContext.Cards.AddAsync(card);
        var entry = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = "other-user-456",
            CardId = card.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();

        // Act
        await _controller.DeleteCollectionEntry(entry.Id);

        // Assert — other user's entry should still exist
        var dbEntries = await _dbContext.CollectionEntries.ToListAsync();
        dbEntries.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_NoUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controllerWithoutUser = new CollectionsController(_dbContext);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controllerWithoutUser.DeleteCollectionEntry(Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task DeleteCollectionEntry_DoesNotAffectOtherEntries()
    {
        // Arrange — user has two entries, delete one
        var card1 = CreateCard("Lightning Bolt", "M21", "123");
        var card2 = CreateCard("Counterspell", "MH2", "56");
        await _dbContext.Cards.AddRangeAsync(card1, card2);

        var entry1 = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card1.Id,
            Platform = Platform.Paper,
            Quantity = 4,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var entry2 = new CollectionEntry
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            CardId = card2.Id,
            Platform = Platform.Paper,
            Quantity = 2,
            FoilQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _dbContext.CollectionEntries.AddRangeAsync(entry1, entry2);
        await _dbContext.SaveChangesAsync();

        // Act — delete only entry1
        await _controller.DeleteCollectionEntry(entry1.Id);

        // Assert — entry2 still exists
        var dbEntries = await _dbContext.CollectionEntries.ToListAsync();
        dbEntries.Count.ShouldBe(1);
        dbEntries[0].Id.ShouldBe(entry2.Id);
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

    private Card CreateCard(string name, string setCode, string collectorNumber, string? imageUrisJson = null)
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
            ImageUris = imageUrisJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
