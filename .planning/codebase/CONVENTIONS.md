# Coding Conventions

**Analysis Date:** 2026-02-05

## Naming Patterns

**Files:**
- PascalCase for all C# files: `AuthController.cs`, `AuthService.cs`, `User.cs`
- File names match class names exactly
- DTO files use Dto suffix: `AuthLoginDto.cs`, `AuthDjRegisterDto.cs`
- Interface files use I prefix: `IAuthService.cs`, `IPasswordService.cs`
- Test files use Tests suffix: `AuthControllerTests.cs`, `ExceptionHandlingMiddlewareTests.cs`

**Classes and Types:**
- PascalCase for all class names: `AuthController`, `AuthService`, `User`, `Event`
- PascalCase for enum types: `UserRole`, `RequestStatus`
- PascalCase for DTO classes: `AuthLoginDto`, `UserDto`, `EventCreateDto`

**Methods:**
- PascalCase for public methods: `AuthenticateUser`, `RegisterDj`, `VerifyEmail`
- PascalCase for private methods: `GenerateJwtToken`, `BuildConnectionString`
- Async methods end with `Async`: `AuthenticateUserAsync`, `CanAccessEventAsync`, `ValidateTokenAsync` (note: not all async methods follow this strictly in AuthService)
- Action/return intent clear in method name: `CanAccessEvent`, `CanModifyRequest`, `IsDj`

**Properties:**
- PascalCase for public properties: `UserId`, `Username`, `Email`, `Role`, `IsEmailVerified`
- Private backing fields use underscore prefix with camelCase: `_authService`, `_context`, `_logger`, `_configuration`

**Variables:**
- camelCase for local variables: `loginDto`, `authResult`, `userId`, `testException`
- camelCase for parameters: `usernameOrEmail`, `password`, `registerDto`, `email`

**Constants:**
- PascalCase for constants: `DatabaseGeneratedOption.Identity`
- String literals for routes: `"api/[controller]"`, `"login"`, `"register"`

## Code Style

**Formatting:**
- No explicit formatter configuration file detected (no .editorconfig, .prettierrc, or StyleCop config)
- Consistent with C# .NET conventions
- 4-space indentation used throughout
- Opening braces on same line (Allman style not used)
- Space before opening brace: `public class AuthController(...) : ControllerBase`

**Linting:**
- `TreatWarningsAsErrors: true` in `Directory.Build.props` - warnings are treated as errors
- `Nullable: enable` enforced in `Directory.Build.props` - nullable reference types required
- `ImplicitUsings: enable` in `Directory.Build.props` - system namespaces auto-imported
- `LangVersion: latest` in `Directory.Build.props` - uses latest C# features
- Individual pragma disables used for specific warnings: `#pragma warning disable IDE0057` in `AuthController.cs` (line 70-72)

## Import Organization

**Order:**
1. System namespaces (System, System.Collections, System.Text, etc.)
2. Microsoft namespaces (Microsoft.AspNetCore, Microsoft.EntityFrameworkCore, Microsoft.Extensions, etc.)
3. Third-party packages (Newtonsoft.Json, etc.)
4. Project-local namespaces (CrowdQR.Api, CrowdQR.Shared)

**Example from `AuthService.cs`:**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
```

**Path Aliases:**
- No alias patterns detected; full namespace paths used throughout
- Root namespaces clearly hierarchical: `CrowdQR.Api`, `CrowdQR.Api.Services`, `CrowdQR.Shared.Models.DTOs`

## Error Handling

**Patterns:**
- Try-catch blocks in service methods to catch and log exceptions
- Services return DTOs with `Success` boolean flag instead of throwing exceptions: `AuthResultDto.Success`, `AuthResultDto.ErrorMessage`
- Controllers return appropriate HTTP status codes based on service response: `BadRequest()`, `Unauthorized()`, `Ok()`, `CreatedAtAction()`
- Exception logging via injected `ILogger<T>`: `_logger.LogError(ex, "Error message with params")`
- Sanitization of sensitive data in logs: `var sanitizedUsername = registerDto.Username.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");` (AuthService.cs line 227)
- Middleware catches unhandled exceptions globally via `ExceptionHandlingMiddleware`

**Example from `AuthService.cs`:**
```csharp
try
{
    // Logic here
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error authenticating user during authentication process.");
    return new AuthResultDto
    {
        Success = false,
        ErrorMessage = "Authentication failed"
    };
}
```

## Logging

**Framework:** `Microsoft.Extensions.Logging`

**Injection Pattern:**
- Constructor injection of `ILogger<T>`: `ILogger<AuthController> logger`
- Stored as private readonly field: `private readonly ILogger<AuthController> _logger = logger;`

**Patterns:**
- Log level selection matches severity:
  - `LogError()` for exceptions and failures
  - `LogInformation()` for important business events: "Created a new user with Audience role", "Token validation successful for user ID"
  - `LogWarning()` for validation failures and security concerns
- Structured logging with parameters: `_logger.LogError(ex, "Error registering DJ user {Username}", sanitizedUsername);`
- Null logger test helper available: `TestLoggerFactory.CreateNullLogger<T>()` in tests

**When to Log:**
- Service entry/exit for important operations
- Authentication successes and failures
- Exceptions and error conditions
- Unusual application state (token validation failures, etc.)

## Comments

**When to Comment:**
- Temporary workarounds and disabled code clearly marked:
  ```csharp
  // TEMPORARILY DISABLED: Email Verification check
  /*
  if (!user.IsEmailVerified)
  {
      return new AuthResultDto
      {
          Success = false,
          ErrorMessage = "Please verify your email before logging in",
          EmailVerificationRequired = true
      };
  }
  */
  ```
- Complex logic requiring explanation (e.g., JWT token generation, password verification)
- Business logic that differs from typical implementations (audience auto-creation on login)

**JSDoc/TSDoc (XML Documentation):**
- Comprehensive `<summary>` tags on all public classes and methods
- `<remarks>` for constructor initialization details
- `<param>` tags for all parameters with descriptions
- `<returns>` tags for return values including nullable indicators
- Examples from `AuthController.cs`:
  ```csharp
  /// <summary>
  /// Authenticates a user and returns a JWT token.
  /// </summary>
  /// <param name="request">The login request.</param>
  /// <returns>Authentication result with JWT token if successful.</returns>
  [HttpPost("login")]
  public async Task<ActionResult<AuthResultDto>> Login([FromBody] AuthLoginDto request)
  ```

## Function Design

**Size:**
- Methods generally 20-100 lines
- Service methods can exceed 100 lines when handling multiple validation/data steps (AuthService.AuthenticateUser: 102 lines)
- Well-defined single responsibility where methods handle one business operation

**Parameters:**
- Use DTOs for complex parameter sets: `AuthDjRegisterDto registerDto` instead of multiple primitives
- Optional parameters with null coalescing: `string? password = null` in `AuthenticateUser`
- Tuple returns for related values: `(hash, salt)` from `_passwordService.HashPassword(password)`

**Return Values:**
- Controllers return `ActionResult<T>` for API responses with proper HTTP status codes
- Services return DTOs with embedded success/error information: `AuthResultDto` with `Success`, `ErrorMessage`, `Token`, `User`
- Nullable returns when object may not exist: `User?` from `ValidateToken()`, `FirstOrDefaultAsync()`
- Boolean for simple success indicators: `VerifyEmail()`, `CanAccessEventAsync()`

## Module Design

**Exports:**
- Public interface definitions in `Services/I*.cs` files
- DTO classes are public and export-ready
- Model classes marked as public
- Internal implementation details marked private

**Barrel Files:**
- No barrel files (index.ts equivalent) detected
- Direct imports from specific files required
- Examples: `using CrowdQR.Api.Services;`, `using CrowdQR.Shared.Models.DTOs;`

**Organization:**
- Controllers in `Controllers/` directory
- Services and interfaces in `Services/` directory
- Models in `Models/` directory
- DTOs in `CrowdQR.Shared/Models/DTOs/`
- Middleware in `Middleware/` directory
- Data access in `Data/` directory with DbContext and DbSeeder

---

*Convention analysis: 2026-02-05*
