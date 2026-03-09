
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

**Zero-config local dev** — no database, OAuth credentials, or secrets required:

**Terminal 1 — Backend API:**
```bash
dotnet run --project src/Rcv.Web.Api            # http://localhost:5041
```
- A SQLite database (`src/Rcv.Web.Api/rcv-dev.db`) is created automatically on first run.
- Swagger UI is available at `http://localhost:5041` in Development mode.

**Terminal 2 — Frontend:**
```bash
cd src/rcv-web-ui
npm install          # first time only
npm run dev          # http://localhost:5173
```

**Logging in during local dev:**
Open `http://localhost:5173/login` and click the **"Dev Login (local only)"** button. This calls
`GET /api/auth/dev-login`, creates a fixed test user (`dev@localhost`), and issues a JWT cookie —
no OAuth provider needed. The button only appears when `import.meta.env.DEV` is `true`.

**Using real OAuth providers (optional):**
Register OAuth app credentials via `dotnet user-secrets`:
```bash
cd src/Rcv.Web.Api
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-secret>"
dotnet user-secrets set "Authentication:Microsoft:ClientId"     "<your-id>"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<your-secret>"
```
The Google and Microsoft sign-in buttons on the login page will then work end-to-end.

> **HTTPS local dev**: `dotnet run --project src/Rcv.Web.Api --launch-profile https` runs on
> `https://localhost:7188`. Real OAuth providers require HTTPS callback URLs.

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