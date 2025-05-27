# CrowdQR API Tests

This project contains comprehensive unit and integration tests for the CrowdQR API.

## Test Structure

### Unit Tests
- **Controllers**: Test API controllers with mocked dependencies
- **Services**: Test business logic and service classes
- **Validation**: Test business rules and data validation constraints

### Integration Tests
- **End-to-End**: Test complete API workflows with in-memory database

## Test Framework & Tools

- **xUnit**: Primary testing framework
- **FluentAssertions**: Fluent assertion library for better readability
- **Moq**: Mocking framework for dependencies
- **Entity Framework InMemory**: In-memory database for isolated testing
- **Coverlet**: Code coverage collection
- **ReportGenerator**: Coverage report generation

## Running Tests

### Local Development

Run all tests:
```bash
dotnet test
```

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run specific test class:
```bash
dotnet test --filter "ClassName=VoteControllerTests"
```

Run tests matching pattern:
```bash
dotnet test --filter "Name~Vote"
```

### Using PowerShell Script

The repository includes a PowerShell script for comprehensive test runs:

```powershell
# Run tests with default settings
./scripts/run-tests.ps1

# Run with specific configuration
./scripts/run-tests.ps1 -Configuration Release -Coverage $true

# Specify output directory
./scripts/run-tests.ps1 -Output "./my-test-results"
```

## Code Coverage

The tests are configured to achieve **70-80% code coverage** on core logic.

### Coverage Exclusions
- Test assemblies
- Program.cs and Startup.cs
- Auto-generated code
- Database migrations
- Static files (wwwroot)

### Coverage Reports
Coverage reports are generated in HTML format and include:
- Line coverage
- Branch coverage  
- Method coverage
- Summary statistics

## Test Categories

### Controller Tests
Test API endpoints with focus on:
- **Authorization**: Role-based access control
- **Validation**: Input validation and model binding
- **Business Logic**: Core functionality
- **Error Handling**: Exception scenarios
- **HTTP Status Codes**: Correct response codes

### Service Tests  
Test business services with focus on:
- **Authentication**: User login and registration
- **Data Operations**: CRUD operations
- **Business Rules**: Domain-specific logic
- **External Dependencies**: Mocked integrations

### Validation Tests
Test data integrity with focus on:
- **Unique Constraints**: Username, email, event slug uniqueness
- **Vote Limits**: One vote per user per request
- **Foreign Keys**: Referential integrity
- **Required Fields**: Non-null constraints
- **String Lengths**: Maximum length validation
- **Cascade Behavior**: Delete cascades and restrictions

### Integration Tests
Test complete workflows with focus on:
- **End-to-End Scenarios**: Complete user journeys
- **Database Integration**: Real database operations
- **Authentication Flow**: Token-based authentication
- **API Contracts**: Request/response validation

## Test Data Management

### In-Memory Database
Tests use Entity Framework's in-memory database provider for:
- **Isolation**: Each test gets a fresh database
- **Speed**: No external database dependencies
- **Reliability**: Consistent test environment

### Test Data Factory
The `TestDbContextFactory` provides:
- Clean database contexts
- Seeded test data
- Helper methods for common scenarios

### Sample Test Data
Standard test data includes:
- 1 DJ user (ID: 1)
- 2 Audience users (IDs: 2, 3)  
- 1 Test event (ID: 1)
- 2 Sample requests (IDs: 1, 2)

## Best Practices Followed

### Test Organization
- **Arrange-Act-Assert**: Clear test structure
- **Single Responsibility**: One concern per test
- **Descriptive Names**: Clear test intentions
- **Fast Execution**: Quick feedback loop

### Test Isolation
- **Independent Tests**: No shared state
- **Fresh Context**: New database per test
- **Mocked Dependencies**: Controlled external calls
- **Deterministic**: Reliable and repeatable

### Error Testing
- **Exception Scenarios**: Invalid inputs and edge cases  
- **Authorization**: Unauthorized and forbidden access
- **Validation**: Model validation failures
- **Business Rules**: Domain constraint violations

## Test Debugging

### Running Individual Tests
Use Test Explorer in Visual Studio or:
```bash
dotnet test --filter "FullyQualifiedName~VoteControllerTests.CreateVote_ValidRequest_ReturnsCreatedResult"
```

### Test Output
Enable detailed logging:
```bash
dotnet test --verbosity detailed
```

### Coverage Analysis
View coverage reports at:
```
TestResults/coverage-report/index.html
```

## Continuous Integration

The project includes GitHub Actions workflow for:
- **Automated Testing**: Run on push/PR
- **Code Coverage**: Collect and report coverage
- **Multi-Platform**: Test on different OS versions
- **Dependency Caching**: Faster build times

See `.github/workflows/test.yml` for configuration.

## Coverage Goals

Target coverage levels:
- **Line Coverage**: 70-80%
- **Branch Coverage**: 65-75%  
- **Method Coverage**: 80-90%

Current focus areas for coverage:
- Controller action methods
- Service business logic
- Data validation rules
- Authentication/authorization flows