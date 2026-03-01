using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTGCollectionTracker.Api.Helpers;
using MTGCollectionTracker.Shared.DTOs.Cards;
using Shouldly;

namespace MTGCollectionTracker.Api.Tests.Helpers;

[TestClass]
public class CardImageHelperTests
{
    // Lightning Bolt (Double Masters 2022) — single-faced card
    private const string NormalImageUrl = "https://cards.scryfall.io/normal/front/f/2/f29ba16f-c8fb-42fe-aabf-87089cb214a7.jpg";
    private const string SmallImageUrl = "https://cards.scryfall.io/small/front/f/2/f29ba16f-c8fb-42fe-aabf-87089cb214a7.jpg";

    // Delver of Secrets // Insectile Aberration (Innistrad) — double-faced card
    private const string FrontFaceImageUrl = "https://cards.scryfall.io/normal/front/1/1/11bf83bb-c95b-4b4f-9a56-ce7a1816e5db.jpg";
    private const string BackFaceImageUrl = "https://cards.scryfall.io/normal/back/1/1/11bf83bb-c95b-4b4f-9a56-ce7a1816e5db.jpg";

    #region Single-Faced Card (ImageUris JSON)

    [TestMethod]
    public void ExtractImageUri_WithNormalKey_ReturnsNormalUrl()
    {
        var imageUrisJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["small"] = SmallImageUrl,
            ["normal"] = NormalImageUrl
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson: null);

        result.ShouldBe(NormalImageUrl);
    }

    [TestMethod]
    public void ExtractImageUri_WithoutNormalKey_FallsThrough()
    {
        // ImageUris JSON exists but has no "normal" key — should return null (no faces fallback)
        var imageUrisJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["small"] = SmallImageUrl,
            ["large"] = "https://cards.scryfall.io/large/front/f/2/f29ba16f-c8fb-42fe-aabf-87089cb214a7.jpg"
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson: null);

        result.ShouldBeNull();
    }

    [TestMethod]
    public void ExtractImageUri_ImageUrisTakesPriorityOverFaces()
    {
        var imageUrisJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["normal"] = NormalImageUrl
        });
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>
        {
            new() { Name = "Front Face", TypeLine = "Creature", ImageUri = FrontFaceImageUrl }
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson);

        result.ShouldBe(NormalImageUrl);
    }

    #endregion

    #region Multi-Faced Card (Faces JSON Fallback)

    [TestMethod]
    public void ExtractImageUri_NullImageUris_UsesFirstFace()
    {
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>
        {
            new() { Name = "Delver of Secrets", TypeLine = "Creature — Human Wizard", ImageUri = FrontFaceImageUrl },
            new() { Name = "Insectile Aberration", TypeLine = "Creature — Human Insect", ImageUri = BackFaceImageUrl }
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson: null, facesJson);

        result.ShouldBe(FrontFaceImageUrl);
    }

    [TestMethod]
    public void ExtractImageUri_EmptyImageUris_UsesFirstFace()
    {
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>
        {
            new() { Name = "Front", TypeLine = "Creature", ImageUri = FrontFaceImageUrl }
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson: "", facesJson);

        result.ShouldBe(FrontFaceImageUrl);
    }

    [TestMethod]
    public void ExtractImageUri_EmptyFacesList_ReturnsNull()
    {
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>());

        var result = CardImageHelper.ExtractImageUri(imageUrisJson: null, facesJson);

        result.ShouldBeNull();
    }

    #endregion

    #region Both Null/Empty

    [TestMethod]
    [DataRow(null, null, DisplayName = "Both null")]
    [DataRow("", null, DisplayName = "ImageUris empty, Faces null")]
    [DataRow(null, "", DisplayName = "ImageUris null, Faces empty")]
    [DataRow("", "", DisplayName = "Both empty")]
    public void ExtractImageUri_NullOrEmptyInputs_ReturnsNull(string? imageUrisJson, string? facesJson)
    {
        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson);

        result.ShouldBeNull();
    }

    #endregion

    #region Malformed JSON

    [TestMethod]
    public void ExtractImageUri_MalformedImageUrisJson_FallsToFaces()
    {
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>
        {
            new() { Name = "Front", TypeLine = "Creature", ImageUri = FrontFaceImageUrl }
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson: "{ not valid json", facesJson);

        result.ShouldBe(FrontFaceImageUrl);
    }

    [TestMethod]
    public void ExtractImageUri_MalformedFacesJson_ReturnsNull()
    {
        var result = CardImageHelper.ExtractImageUri(imageUrisJson: null, facesJson: "[ broken");

        result.ShouldBeNull();
    }

    [TestMethod]
    public void ExtractImageUri_BothMalformed_ReturnsNull()
    {
        var result = CardImageHelper.ExtractImageUri(imageUrisJson: "{ bad", facesJson: "[ bad");

        result.ShouldBeNull();
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ExtractImageUri_ImageUrisWithNullNormalValue_ReturnsNull()
    {
        // "normal" key exists but value is null
        var imageUrisJson = """{"normal": null, "small": "https://cards.scryfall.io/small/front/f/2/f29ba16f-c8fb-42fe-aabf-87089cb214a7.jpg"}""";

        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson: null);

        result.ShouldBeNull();
    }

    [TestMethod]
    public void ExtractImageUri_ImageUrisNoNormalKey_FallsToFaces()
    {
        // ImageUris has keys but not "normal" — should fall through to faces
        var imageUrisJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["small"] = SmallImageUrl
        });
        var facesJson = JsonSerializer.Serialize(new List<CardFaceDto>
        {
            new() { Name = "Front", TypeLine = "Creature", ImageUri = FrontFaceImageUrl }
        });

        var result = CardImageHelper.ExtractImageUri(imageUrisJson, facesJson);

        result.ShouldBe(FrontFaceImageUrl);
    }

    #endregion
}
