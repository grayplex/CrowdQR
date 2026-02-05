# Testing Patterns

**Analysis Date:** 2026-02-05

## Test Framework

**Runner:**
- xunit 2.9.3
- Config: No xunit.runner.json file detected; uses defaults
- Test projects: `CrowdQR.Api.Tests` in `tests/CrowdQR.API.Tests/`

**Assertion Library:**
- FluentAssertions 8.8.0
- Global using in `CrowdQR.Api.Tests.csproj`: `<Using Include="FluentAssertions" />`
- Enables fluent assertions like `.Should().BeTrue()`, `.Should().BeOfType<OkObjectResult>()`

**Mocking Framework:**
- Moq 4.20.72
- Global using in `CrowdQR.Api.Tests.csproj`: `<Using Include="Moq" />`
- Mock patterns: `new Mock<IAuthService>()`, `.Setup()`, `.ReturnsAsync()`, `.Verify()`

**Code Coverage:**
- coverlet.msbuild 6.0.4 - coverage collection during build
- coverlet.collector 6.0.4 - coverage collection via test runner
- View coverage via standard coverlet commands

**Run Commands:**
```bash
dotnet test                                    # Run all tests
dotnet test --watch                           # Watch mode (not standard xunit)
dotnet test /p:CollectCoverage=true          # Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover  # OpenCover format
```

## Test File Organization

**Location:**
- Tests are separate from source code (not co-located)
- Located in `tests/CrowdQR.API.Tests/`
- Mirror source structure: Controllers tests in `Controllers/`, Integration tests in `Integration/`, etc.

**Naming:**
- `*Tests.cs` suffix: `AuthControllerTests.cs`, `ExceptionHandlingMiddlewareTests.cs`, `VoteIntegrationTests.cs`
- Test class names match what they test: `AuthController` → `AuthControllerTests`
- Test methods use `MethodName_Scenario_ExpectedResult` pattern:
  - `Login_ValidCredentials_ReturnsOkWithToken()`
  - `InvokeAsync_ExceptionThrown_Returns500()`
  - `Register_DuplicateUsername_ReturnsBadRequest()`

**Structure:**
```
tests/CrowdQR.API.Tests/
├── Controllers/          # Controller unit tests
│   ├── AuthControllerTests.cs
│   ├── DashboardControllerTests.cs
│   └── ...
├── Integration/         # Integration tests with test server
│   ├── BaseIntegrationTest.cs  # Base class for integration tests
│   ├── AuthIntegrationTests.cs
│   └── VoteIntegrationTests.cs
├── Middleware/          # Middleware unit tests
│   └── ExceptionHandlingMiddlewareTests.cs
└── Helpers/            # Test utilities and factories
    ├── NullLogger.cs
    ├── TestDbContextFactory.cs
    └── TestLoggerFactory.cs
```

## Test Structure

**Suite Organization:**

Unit tests use `IDisposable` for cleanup in controller tests:
```csharp
public class AuthControllerTests : IDisposable
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Arrange - Create test doubles and SUT
        _mockAuthService = new Mock<IAuthService>();
        var logger = TestLoggerFactory.CreateNullLogger<AuthController>();
        _controller = new AuthController(_mockAuthService.Object, logger);
    }

    // Test methods here

    public void Dispose() { }
}
```

Integration tests inherit from `BaseIntegrationTest`:
```csharp
public class AuthIntegrationTests : BaseIntegrationTest
{
    [Fact]
    public async Task Login_NewAudienceUser_CreatesUserAndReturnsToken()
    {
        // Arrange
        await ClearDatabaseAsync();
        var loginDto = new AuthLoginDto { ... };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // ... more assertions
    }
}
```

**Patterns:**

**Arrange-Act-Assert:**
```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsOkWithToken()
{
    // Arrange
    var loginDto = new AuthLoginDto
    {
        UsernameOrEmail = "test_dj",
        Password = "password123"
    };

    var expectedResult = new AuthResultDto
    {
        Success = true,
        Token = "jwt-token",
        User = new UserDto { ... }
    };

    _mockAuthService
        .Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
        .ReturnsAsync(expectedResult);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    result.Result.Should().BeOfType<OkObjectResult>();
    var okResult = result.Result as OkObjectResult;
    okResult?.Value.Should().BeEquivalentTo(expectedResult);
}
```

**Setup and Teardown:**
- Unit tests use constructor for setup
- Integration tests use `ClearDatabaseAsync()` to reset data between tests
- `SeedDatabaseAsync(Action<CrowdQRContext>)` to populate test data
- `SeedStandardTestDataAsync()` for standard test scenario setup

**Assertion Patterns:**
- FluentAssertions chaining: `.Should().BeTrue()`, `.Should().NotBeNull()`, `.Should().BeEquivalentTo()`
- Type assertions: `.Should().BeOfType<OkObjectResult>()`
- Collection assertions: `.Should().NotBeNullOrEmpty()`
- String assertions: `.Should().Be()`, `.Should().Contain()`
- Numeric assertions: `.Should().Be(200)` for status codes

## Mocking

**Framework:** Moq

**Patterns:**

Create mocks in constructor:
```csharp
private readonly Mock<IAuthService> _mockAuthService;

public AuthControllerTests()
{
    _mockAuthService = new Mock<IAuthService>();
}
```

Setup return values:
```csharp
_mockAuthService
    .Setup(x => x.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(expectedResult);
```

Verify method calls:
```csharp
_mockLogger.Verify(
    x => x.Log(
        LogLevel.Error,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
        testException,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Once);
```

**What to Mock:**
- External service dependencies: `IAuthService`, `IEmailService`, `IPasswordService`
- Logger interfaces: `ILogger<T>`
- Any service with external I/O (email, external APIs)
- Configuration: `IConfiguration`

**What NOT to Mock:**
- Domain models: `User`, `Event`, `Request` - use real instances
- DTOs - construct real instances for assertion comparison
- Database in integration tests - use in-memory database instead of mocking
- SignalR Hub - tested via integration tests with real Hub

## Fixtures and Factories

**Test Data:**

`TestDbContextFactory.cs` provides static factory methods:
```csharp
public static CrowdQRContext CreateInMemoryContext(string? databaseName = null)
{
    var dbName = databaseName ?? $"TestDb_{Guid.NewGuid()}";
    var options = new DbContextOptionsBuilder<CrowdQRContext>()
        .UseInMemoryDatabase(dbName)
        .EnableSensitiveDataLogging()
        .Options;
    return new CrowdQRContext(options);
}

public static CrowdQRContext CreateInMemoryContextWithData(string? databaseName = null)
{
    var context = CreateInMemoryContext(databaseName);
    SeedTestData(context);
    return context;
}

public static void SeedTestData(CrowdQRContext context)
{
    // Creates standard test users, events, requests
    var djUser = new User { UserId = 1, Username = "test_dj", ... };
    var audienceUser1 = new User { UserId = 2, Username = "audience1", ... };
    var testEvent = new Event { EventId = 1, DjUserId = 1, ... };
    // ... adds to context
}
```

**Integration Test Base Class Helpers:**
```csharp
protected async Task ClearDatabaseAsync()  // Clear all entities in dependency order
protected async Task SeedDatabaseAsync(Action<CrowdQRContext> seedAction)  // Custom seeding
protected async Task SeedStandardTestDataAsync()  // Use standard test data
protected CrowdQRContext GetDbContext()  // Get fresh context
protected async Task<User?> GetUserFromDatabaseAsync(string username)  // Query helpers
protected async Task VerifyUserInDatabaseAsync(string username, string because)  // Assertions
```

**Location:**
- Factory classes in `CrowdQR.Api.Tests/Helpers/`
- Base class for integration tests: `CrowdQR.Api.Tests/Integration/BaseIntegrationTest.cs`

## Coverage

**Requirements:** No enforced coverage target detected in configuration

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
# View reports in generated coverage files (typically in TestResults/ directory)
```

## Test Types

**Unit Tests:**
- Scope: Single method/class in isolation with mocked dependencies
- Location: `Controllers/`, `Middleware/` directories
- Approach: Mock all external dependencies, focus on logic branches
- Example: `AuthControllerTests.Login_ValidCredentials_ReturnsOkWithToken()` mocks `IAuthService`
- No database access - use test doubles

**Integration Tests:**
- Scope: Full request/response cycle through HTTP with real database
- Location: `Integration/` directory inheriting from `BaseIntegrationTest`
- Approach: Use real `WebApplicationFactory<Program>` with in-memory database
- Setup: `ConfigureServices()` removes real PostgreSQL and adds in-memory EF Core
- Example: `AuthIntegrationTests.Login_NewAudienceUser_CreatesUserAndReturnsToken()` sends actual HTTP POST request
- Database verification: Query database via `GetUserFromDatabaseAsync()` after HTTP requests

**E2E Tests:**
- Framework: Not detected; no Playwright, Selenium, or similar
- Not currently implemented

## Common Patterns

**Async Testing:**

All async methods use `async Task` test methods:
```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsOkWithToken()
{
    // Arrange
    var loginDto = new AuthLoginDto { ... };
    var expectedResult = new AuthResultDto { ... };
    _mockAuthService
        .Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
        .ReturnsAsync(expectedResult);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    result.Result.Should().BeOfType<OkObjectResult>();
}
```

**Error Testing:**

Return failure results from service mocks and verify error responses:
```csharp
[Fact]
public async Task Login_InvalidCredentials_ReturnsUnauthorized()
{
    // Arrange
    var loginDto = new AuthLoginDto { ... };
    var expectedResult = new AuthResultDto
    {
        Success = false,
        ErrorMessage = "Invalid credentials"
    };
    _mockAuthService
        .Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
        .ReturnsAsync(expectedResult);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    result.Result.Should().BeOfType<UnauthorizedObjectResult>();
}
```

**Exception Testing with Theory Tests:**

```csharp
[Theory]
[InlineData(typeof(ArgumentException), "Argument exception")]
[InlineData(typeof(InvalidOperationException), "Invalid operation")]
[InlineData(typeof(NotSupportedException), "Not supported")]
public async Task InvokeAsync_DifferentExceptionTypes_HandlesCorrectly(Type exceptionType, string message)
{
    // Arrange
    var context = new DefaultHttpContext();
    context.Response.Body = new MemoryStream();
    var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

    Task next(HttpContext ctx) { throw exception; }
    var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    context.Response.StatusCode.Should().Be(500);
}
```

**Database Assertions:**

Verify database state after HTTP operations:
```csharp
[Fact]
public async Task Login_NewAudienceUser_CreatesUserAndReturnsToken()
{
    // Arrange
    await ClearDatabaseAsync();
    var loginDto = new AuthLoginDto { UsernameOrEmail = "new_audience_user" };

    // Act
    var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

    // Assert HTTP response
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Assert database state
    await VerifyUserInDatabaseAsync("new_audience_user", "User should be created in database");
}
```

---

*Testing analysis: 2026-02-05*
