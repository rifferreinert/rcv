
## Project Overview

This is a .NET ranked choice voting (RCV) platform c

## Development Commands

### Build
```bash
dotnet build                                    # Build entire solution
dotnet build -c Release                         # Release build
cd src/rcv-web-ui && npm install                # Install frontend dependencies
cd src/rcv-web-ui && npm run build              # Build frontend for production
```

### Running Locally

**Backend API** (requires OAuth credentials in `appsettings.json` or user-secrets):
```bash
dotnet run --project src/Rcv.Web.Api            # http://localhost:5041
dotnet run --project src/Rcv.Web.Api --launch-profile https  # https://localhost:7188
```
Swagger UI is available at `http://localhost:5041` in Development mode.

> **Prerequisites**: The API requires `Authentication:Google:ClientId/ClientSecret` and
> `Authentication:Microsoft:ClientId/ClientSecret` to start. Set them via
> `dotnet user-secrets` or environment variables. The database connection string
> (`ConnectionStrings:DefaultConnection`) must also point to a valid SQL Server instance
> (or be overridden for local dev). The JWT `SecretKey` must be set.

**Frontend** (in a separate terminal):
```bash
cd src/rcv-web-ui
npm install          # first time only
npm run dev          # http://localhost:5173
```
The frontend expects the API at `http://localhost:5041` (CORS is pre-configured for port 5173).

### Testing
```bash
dotnet test                                     # Run all tests (Core + Web API)
dotnet test --verbosity detailed                # Verbose test output
dotnet test --filter "FullyQualifiedName~PollServiceTests"      # Run specific test class
dotnet test --filter "FullyQualifiedName~RcvCalculatorEdgeCaseTests"  # Core edge cases
```

## Architecture

### Solution Structure
```
src/
├── Rcv.Core/              # Core RCV library (NuGet package)
├── Rcv.Web.Api/           # ASP.NET Core 9 Web API
│   ├── Controllers/       # AuthController, PollsController (more coming)
│   ├── Services/          # AuthService, PollService (interfaces + implementations)
│   ├── Validators/        # FluentValidation validators for request DTOs
│   ├── Data/              # EF Core DbContext + entity classes
│   └── Models/            # Request/Response DTOs
└── rcv-web-ui/            # React 18 + TypeScript SPA (Vite)
```



### C# Conventions
- Use records for immutable data models
- XML doc comments on all public members
- Validate all inputs; throw exceptions if invalid
- Follow SOLID principles (single responsibility per class)
- Prefer clear, concise variable names over abbreviations

### Test-Driven Development (TDD)
When implementing features:
- Write clear, focused tests that verify one behavior at a time
- Use descriptive test names that explain what is being tested and the expected outcome
- Follow Arrange-Act-Assert (AAA) pattern: set up test data, execute the code under test, verify results
- Keep tests independent - each test should run in isolation without depending on other tests
- Start with the simplest test case, then add edge cases and error conditions
- Tests should fail for the right reason - verify they catch the bugs they're meant to catch
- Mock external dependencies to keep tests fast and reliable

### Git Practices
- Use feature branches for new work
- Write clear, concise commit messages
- Open PRs for code review before merging to `main`
- Commit often with small, focused changes