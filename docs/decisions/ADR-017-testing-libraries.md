# ADR-017: Testing Libraries

**Date**: January 24, 2026
**Status**: Accepted
**Deciders**: Development Team

## Context

Need to establish a testing strategy for the MTG Collection Tracker. This includes selecting:

1. A testing framework for organizing and running tests
2. A mocking library for isolating dependencies
3. An assertion library for readable test validations

## Decision

### Testing Framework: MSTest

**Use MSTest** as the primary testing framework.

- **Why MSTest?**
  - Microsoft official, first-party support
  - Best Visual Studio integration
  - Improved significantly in .NET Core era
  - Consistent with Microsoft-stack philosophy of the project
  - `[TestMethod]`, `[DataRow]` syntax is familiar
  - No third-party dependency concerns

- **Why NOT xUnit?**
  - Third-party library (though popular)
  - Constructor-based setup pattern less intuitive for beginners
  - No significant advantage for our use case

- **Why NOT NUnit?**
  - Third-party, older project
  - Less common in new .NET projects
  - No significant advantage over MSTest

### Mocking Library: NSubstitute

**Use NSubstitute** for creating test doubles.

- **Why NSubstitute?**
  - Clean, natural syntax without lambda boilerplate
  - Easy to read and write
  - No controversy (unlike Moq's SponsorLink incident)
  - Well-maintained and actively developed

- **Syntax comparison:**

  ```csharp
  // NSubstitute (cleaner)
  var userManager = Substitute.For<IUserManager>();
  userManager.FindByEmailAsync(Arg.Any<string>()).Returns(user);

  // Moq (more verbose)
  var mock = new Mock<IUserManager>();
  mock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
  ```

- **Why NOT Moq?**
  - More verbose lambda-based syntax
  - 2023 SponsorLink controversy damaged trust (telemetry added without disclosure)
  - Issue resolved, but demonstrated governance concerns

### Assertion Library: Shouldly

**Use Shouldly** for test assertions.

- **Why Shouldly?**
  - Highly readable, natural language syntax
  - MIT licensed (truly free, no commercial restrictions)
  - Excellent error messages when tests fail
  - Works with any testing framework

- **Syntax comparison:**

  ```csharp
  // Shouldly (readable, MIT licensed)
  result.ShouldNotBeNull();
  result.Email.ShouldBe("test@example.com");
  tokens.Count.ShouldBe(3);
  Should.Throw<InvalidOperationException>(() => service.DoSomething());

  // Built-in Assert (basic)
  Assert.IsNotNull(result);
  Assert.AreEqual("test@example.com", result.Email);
  Assert.AreEqual(3, tokens.Count);
  ```

- **Why NOT FluentAssertions?**
  - FluentAssertions 8.x changed to commercial licensing (Xceed acquired it)
  - Requires paid license for commercial use
  - Shouldly provides similar readability without restrictions

- **Why NOT built-in assertions?**
  - Less readable
  - Worse error messages
  - Limited collection/object assertions

## Package References

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="MSTest.TestAdapter" Version="3.*" />
<PackageReference Include="MSTest.TestFramework" Version="3.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="Shouldly" Version="4.*" />
<PackageReference Include="coverlet.collector" Version="6.*" />
```

## Consequences

### Positive

- **Consistency**: Microsoft-official framework aligns with .NET stack
- **Readability**: Shouldly makes tests self-documenting
- **Maintainability**: NSubstitute's clean syntax reduces test code complexity
- **Tooling**: Best-in-class VS Test Explorer integration with MSTest
- **No licensing concerns**: All libraries are MIT licensed or Microsoft-supported
- **Trust**: No controversial third-party governance issues

### Negative

- **Learning curve**: Team members familiar with xUnit will need adjustment
- **Community resources**: More xUnit examples online (but MSTest is well-documented)
- **Shouldly vs FluentAssertions**: Slightly less feature-rich, but sufficient for most cases

### Neutral

- **Performance**: All three frameworks perform similarly for our scale
- **CI/CD**: All work well with GitHub Actions `dotnet test`

## Test Project Structure

```
tests/
├── MTGCollectionTracker.Api.Tests/           # Unit tests
│   ├── Services/
│   │   └── JwtServiceTests.cs
│   └── Controllers/
│       └── AuthControllerTests.cs
└── MTGCollectionTracker.Api.IntegrationTests/ # Integration tests (future)
    └── Auth/
        └── AuthFlowTests.cs
```

## References

- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [Shouldly Documentation](https://docs.shouldly.org/)
