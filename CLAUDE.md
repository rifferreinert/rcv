
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

```bash
dotnet run --project src/Rcv.Web.Api   # http://localhost:5041 (Swagger UI at root)
cd src/rcv-web-ui && npm install && npm run dev  # http://localhost:5173
```

Log in at `/login` using the **"Dev Login (local only)"** button — no OAuth credentials needed.
The SQLite database is created automatically on first run.

To use real Google/Microsoft OAuth, set credentials via `dotnet user-secrets` in `src/Rcv.Web.Api`.

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