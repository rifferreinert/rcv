# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET ranked choice voting (RCV) platform consisting of:
- **Rcv.Core**: A NuGet library implementing instant-runoff voting (IRV) algorithm with round-by-round elimination and statistics
- **Rcv.Web.Api**: ASP.NET Core 9 Web API — poll management, voting, results, and OAuth2 authentication
- **rcv-web-ui**: React 18 + TypeScript SPA (Vite) — frontend for creating polls, voting, and viewing results
- **Future**: Slack/Teams integrations (see tasks/prd-v1.md for full roadmap)

The core library is stateless and independent of UI, storage, or user management concerns. The web API delegates all vote calculation to `Rcv.Core`.

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

### NuGet Package
```bash
dotnet pack -c Release                          # Create NuGet package
dotnet pack -c Release --output ./artifacts     # Output to specific directory
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

tests/
├── Rcv.Core.Tests/        # xUnit tests for core library
└── Rcv.Web.Api.Tests/     # xUnit tests for web API (unit + integration)
```

### Core Library Design

**Public API** (`RankedChoicePoll`):
- Facade exposing poll creation, ballot addition, and result calculation
- Immutable domain models: `Option`, `RankedBallot`, `RcvResult`, `RoundSummary`
- All models support `System.Text.Json` serialization

**Internal Implementation** (`RcvCalculator`):
- Instant-runoff voting algorithm with round-by-round elimination
- Handles partial ballots, immediate majority winners, tie detection
- Maintains SOLID principles: calculation logic isolated from public API

**Key Principles**:
- No database, session, or UI logic in the library
- Option ID and ballot ID management is external (consuming app's responsibility)
- Throw descriptive exceptions for invalid input (unknown option IDs, duplicate rankings)
- Comprehensive XML documentation for IntelliSense

### Web API Design (Rcv.Web.Api)

**Architecture**: Clean Architecture — Controllers → Services → EF Core Data Layer

**Authentication**: OAuth2 (Google, Microsoft) → JWT stored as httpOnly cookie (`rcv_jwt`).

**Key patterns**:
- Services throw typed exceptions (`KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`); controllers map them to HTTP status codes (404, 403, 409)
- Soft delete only — polls are never hard-deleted (`Status = "Deleted"`)
- All voting calculations delegated to `Rcv.Core` (never re-implemented in the API)
- FluentValidation for all request DTOs; validated automatically before controller actions run
- Integration tests use `WebApplicationFactory<Program>` with in-memory EF Core (no SQL Server required)

### Test Organization
- `Rcv.Core.Tests/`: `RankedChoicePollTests.cs` (public API), `RcvCalculatorEdgeCaseTests.cs` (edge cases). Target: ≥90% coverage
- `Rcv.Web.Api.Tests/Services/`: Unit tests using in-memory EF Core (no HTTP)
- `Rcv.Web.Api.Tests/Controllers/`: Integration tests using `WebApplicationFactory` + in-memory EF Core

## Code Style & Practices

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

**Important**: After writing tests, wait for user confirmation before implementing. The user is learning C# (coming from Python background), so explain concepts as you go and summarize what was done and why after each feature.

### Visual Studio Tasks
XML files typically modified through Visual Studio UI (`.csproj`, `.sln`) should be edited in Visual Studio when possible. Instruct the user to open Visual Studio for these tasks, then verify completion.

## CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`):
1. Restore dependencies
2. Build (Release configuration)
3. Run tests
4. Pack NuGet package
5. Upload artifacts

Triggers: pushes to `main`/`develop`, PRs to `main`
- When working through a task list, go in order and step by step when at all possible