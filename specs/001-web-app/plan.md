# Implementation Plan: Ranked Choice Voting Web Application

**Branch**: `001-web-app` | **Date**: 2025-10-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-web-app/spec.md`

## Summary

Build a full-stack web application for creating, voting in, and viewing results of ranked choice voting polls. The application consists of a Blazor frontend, .NET 9 REST API backend, and Azure SQL Database. The backend integrates with the existing Rcv.Core NuGet package for vote calculation logic and implements OAuth 2.0 authentication with five providers (Slack, Teams, Google, Apple, Microsoft).

## Technical Context

**Language/Version**: C# 14 / .NET 9.0
**Primary Dependencies**:
- **Frontend**: Blazor Server or WebAssembly (NEEDS CLARIFICATION - see research.md)
- **Backend**: ASP.NET Core 9.0 Web API, Entity Framework Core 9.0, Rcv.Core (existing NuGet package)
- **Authentication**: ASP.NET Core Identity with OAuth 2.0 (Microsoft.AspNetCore.Authentication.OAuth)
- **Database**: Azure SQL Database (accessed via EF Core)

**Storage**: Azure SQL Database with Entity Framework Core 9.0
**Testing**: xUnit (existing), Playwright or bUnit for UI tests (NEEDS CLARIFICATION - see research.md)
**Target Platform**: Web browsers (Chrome, Firefox, Safari, Edge - latest 2 versions), Azure App Service for hosting
**Project Type**: Web (Blazor frontend + ASP.NET Core backend)
**Performance Goals**:
- API response time: <200ms p95 for CRUD operations, <500ms for vote calculation
- Page load time: <2 seconds on 10 Mbps connection
- Support 1000 concurrent users
- Vote calculation for 100 ballots with 10 options: <100ms

**Constraints**:
- Mobile responsive (min width 320px)
- WCAG 2.2 Level AA accessibility
- Must integrate with existing Rcv.Core NuGet package (no reimplementation of voting logic)
- One vote per user per poll enforcement
- OAuth tokens expire after 7 days requiring re-authentication

**Scale/Scope**:
- Expected: 1000+ users, 100+ polls per day, 100 votes per poll average
- Maximum: 20 options per poll, 1000 concurrent voters
- Database: ~10k users, ~100k polls, ~1M votes (first year projection)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Library-First Architecture ✅

**Status**: PASS

The existing Rcv.Core NuGet package already implements the core voting logic (instant-runoff algorithm, ballot validation, results calculation). The web application will **consume** this library without reimplementing any voting logic. The library is:
- Self-contained and stateless (no DB/UI logic)
- Independently tested (90%+ coverage verified in existing tests)
- Well-documented with XML comments
- Reusable (can be used by future Slack/Teams integrations)

**Application Layer Responsibilities** (this feature):
- Poll CRUD operations (create, read, update, delete polls)
- User authentication and session management
- Ballot persistence (storing votes in database)
- Results caching and presentation
- API endpoints for frontend consumption

**No Violations**: The web app does not reimplement voting logic; it delegates to Rcv.Core.

### II. Test-First Development ✅

**Status**: PASS (with implementation requirement)

**Commitment**:
- All new backend services (PollService, VoteService, UserService) will be developed using TDD
- API endpoints will have integration tests written first
- Blazor components will have tests written first using bUnit or Playwright
- Database operations will have integration tests with in-memory or test database

**Process**:
1. Write failing test for each user story acceptance scenario
2. Get user approval on test suite before implementation
3. Implement minimum code to pass tests
4. Refactor for readability while keeping tests green

**No Violations**: TDD process will be followed for all new code in tasks.md.

### III. Clear Separation of Concerns ✅

**Status**: PASS

**Layer Separation**:

```
┌─────────────────────────────────────────────────────────┐
│ Presentation Layer (Blazor)                            │
│ - Components, Pages, UI State Management               │
└─────────────────────────────────────────────────────────┘
                        ↓ HTTP (REST API)
┌─────────────────────────────────────────────────────────┐
│ API Layer (ASP.NET Core Controllers)                   │
│ - Request validation, Response mapping, Auth filters   │
└─────────────────────────────────────────────────────────┘
                        ↓ DTOs
┌─────────────────────────────────────────────────────────┐
│ Application Layer (Services)                           │
│ - PollService, VoteService, UserService                │
│ - Business logic, orchestration                        │
└─────────────────────────────────────────────────────────┘
                        ↓ Domain Models ↓ Entities
┌──────────────────────┐              ┌──────────────────┐
│ Core Library         │              │ Data Layer       │
│ (Rcv.Core NuGet)     │              │ (EF Core)        │
│ - Voting algorithm   │              │ - DbContext      │
│ - RcvResult          │              │ - Entities       │
│ - RankedBallot       │              │ - Migrations     │
└──────────────────────┘              └──────────────────┘
```

**No Cross-Layer Pollution**:
- Rcv.Core has zero dependencies on EF Core, ASP.NET, or Blazor
- Data layer only contains EF entities and DbContext (no business logic)
- Application services orchestrate but don't know about HTTP or UI concerns
- Controllers handle HTTP but don't perform business logic

**No Violations**: Architecture maintains strict layer boundaries.

### IV. Simplicity & YAGNI ✅

**Status**: PASS

**What We're Building** (from spec requirements):
- OAuth authentication (required by spec FR-001 to FR-007)
- Poll CRUD with validation (required by spec FR-008 to FR-015)
- Vote submission with one-per-user enforcement (required by spec FR-016 to FR-025)
- Results display with Rcv.Core integration (required by spec FR-026 to FR-035)
- Basic dashboard (required by spec FR-043 to FR-048)

**What We're NOT Building** (YAGNI):
- ❌ Email notifications (spec assumption #8: no notifications)
- ❌ Multi-tenancy/organizations (spec assumption #9: shared environment)
- ❌ Advanced analytics (spec assumption #14: basic stats only)
- ❌ Poll templates (spec assumption #13: create from scratch)
- ❌ Repository pattern (EF Core DbContext IS the repository; adding abstraction is premature)
- ❌ CQRS/Event Sourcing (simple CRUD is sufficient)
- ❌ Message queues (synchronous processing is adequate for 1000 concurrent users)

**Simplicity Choices**:
- Direct DbContext usage in services (no repository abstraction)
- Single Web API project (not splitting into multiple microservices)
- JSON column for ranked choices (simpler than junction table)
- Built-in ASP.NET Core auth middleware (no custom auth framework)

**No Violations**: Only building what spec requires; avoiding premature complexity.

### V. API-First Design ✅

**Status**: PASS

**Immutable Models**:
- All DTOs will be C# records (immutable by default)
- Use `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>` for collections
- Validation at construction/binding time using FluentValidation or Data Annotations

**API Contracts** (see `/contracts/openapi.yaml`):
- RESTful design: `/api/polls`, `/api/polls/{id}/votes`, `/api/polls/{id}/results`
- OpenAPI 3.0 specification with request/response schemas
- Consistent error responses with ProblemDetails (RFC 7807)
- JSON serialization using System.Text.Json (not Newtonsoft.Json)

**Integration with Rcv.Core**:
- Rcv.Core models (Option, RankedBallot, RcvResult) already support System.Text.Json
- API DTOs map cleanly to/from Rcv.Core domain models
- No impedance mismatch between layers

**No Violations**: API follows immutable, validated, JSON-serializable design.

---

## Constitution Check Summary

**Overall Status**: ✅ PASS - All 5 principles satisfied

No complexity violations to justify. All architectural choices align with constitutional principles and spec requirements.

## Project Structure

### Documentation (this feature)

```text
specs/001-web-app/
├── spec.md              # Feature specification (created by /speckit.specify)
├── plan.md              # This file (created by /speckit.plan)
├── research.md          # Phase 0 output (technology research)
├── data-model.md        # Phase 1 output (entities, DTOs, mappings)
├── quickstart.md        # Phase 1 output (local setup guide)
├── contracts/           # Phase 1 output (OpenAPI spec)
│   └── openapi.yaml
├── checklists/
│   └── requirements.md  # Spec quality checklist (created by /speckit.specify)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan, use /speckit.tasks)
```

### Source Code (repository root)

```text
# Web application structure (Blazor frontend + ASP.NET Core backend)

src/
├── Rcv.Web.Api/                    # ASP.NET Core Web API project
│   ├── Controllers/                # API endpoints
│   │   ├── PollsController.cs
│   │   ├── VotesController.cs
│   │   ├── UsersController.cs
│   │   └── ResultsController.cs
│   ├── Services/                   # Application services
│   │   ├── IPollService.cs
│   │   ├── PollService.cs
│   │   ├── IVoteService.cs
│   │   ├── VoteService.cs
│   │   ├── IUserService.cs
│   │   └── UserService.cs
│   ├── Data/                       # EF Core data layer
│   │   ├── RcvDbContext.cs
│   │   ├── Entities/               # EF entities (map to database)
│   │   │   ├── User.cs
│   │   │   ├── Poll.cs
│   │   │   ├── PollOption.cs
│   │   │   └── Vote.cs
│   │   └── Migrations/             # EF migrations
│   ├── Models/                     # DTOs for API requests/responses
│   │   ├── Requests/
│   │   │   ├── CreatePollRequest.cs
│   │   │   ├── CastVoteRequest.cs
│   │   │   └── UpdatePollSettingsRequest.cs
│   │   └── Responses/
│   │       ├── PollResponse.cs
│   │       ├── VoteResponse.cs
│   │       ├── ResultsResponse.cs
│   │       └── UserResponse.cs
│   ├── Mapping/                    # Entity ↔ DTO ↔ Domain model mappings
│   │   ├── PollMapper.cs
│   │   └── VoteMapper.cs
│   ├── Authentication/             # OAuth configuration
│   │   ├── OAuthProviders.cs
│   │   └── AuthenticationExtensions.cs
│   ├── Program.cs                  # Application entry point
│   ├── appsettings.json
│   └── Rcv.Web.Api.csproj
│
├── Rcv.Web.Client/                 # Blazor project (Server or WASM - see research.md)
│   ├── Pages/                      # Blazor pages/routes
│   │   ├── Index.razor             # Home page / poll list
│   │   ├── CreatePoll.razor        # Poll creation form
│   │   ├── PollDetails.razor       # Vote + view results
│   │   ├── Dashboard.razor         # User dashboard
│   │   └── Login.razor             # OAuth sign-in page
│   ├── Components/                 # Reusable Blazor components
│   │   ├── PollCard.razor
│   │   ├── VotingInterface.razor
│   │   ├── ResultsDisplay.razor
│   │   └── Layout/
│   │       ├── MainLayout.razor
│   │       └── NavMenu.razor
│   ├── Services/                   # Frontend services (HTTP clients)
│   │   ├── PollApiClient.cs
│   │   ├── VoteApiClient.cs
│   │   └── AuthService.cs
│   ├── Models/                     # Frontend view models (may differ from API DTOs)
│   │   ├── PollViewModel.cs
│   │   ├── VoteViewModel.cs
│   │   └── ResultsViewModel.cs
│   ├── wwwroot/                    # Static assets
│   │   ├── css/
│   │   ├── js/
│   │   └── favicon.ico
│   ├── Program.cs
│   ├── App.razor
│   └── Rcv.Web.Client.csproj
│
└── Rcv.Core/                       # Existing core library (DO NOT MODIFY)
    ├── Domain/
    │   ├── Option.cs
    │   ├── RankedBallot.cs
    │   ├── RcvResult.cs
    │   └── RoundSummary.cs
    ├── Services/
    │   └── RcvCalculator.cs
    └── RankedChoicePoll.cs

tests/
├── Rcv.Web.Api.Tests/              # Backend API tests
│   ├── Controllers/                # Controller integration tests
│   ├── Services/                   # Service unit tests
│   ├── Data/                       # EF integration tests
│   └── Rcv.Web.Api.Tests.csproj
│
├── Rcv.Web.Client.Tests/           # Frontend tests
│   ├── Pages/                      # Page component tests (bUnit or Playwright)
│   ├── Components/                 # Component unit tests
│   └── Rcv.Web.Client.Tests.csproj
│
└── Rcv.Core.Tests/                 # Existing core library tests (DO NOT MODIFY)

.github/
└── workflows/
    ├── ci.yml                      # Existing CI (extends to build/test web projects)
    └── web-app-deploy.yml          # New: Deploy to Azure App Service (optional)
```

**Structure Decision**:

We're using **Option 2: Web Application** structure with a clear frontend/backend split:

1. **src/Rcv.Web.Api**: ASP.NET Core Web API backend
   - RESTful API that can be consumed by any client (not just Blazor)
   - Clean separation: Controllers → Services → Data/Core
   - Integrates with existing Rcv.Core NuGet package

2. **src/Rcv.Web.Client**: Blazor frontend
   - Hosting model TBD in research.md (Server vs WebAssembly)
   - Communicates with backend via HTTP API calls
   - Implements all UI requirements from spec

3. **src/Rcv.Core**: Existing library (unchanged)
   - Contains voting algorithm, domain models
   - Consumed by Rcv.Web.Api services

This structure supports:
- Independent API testing without UI
- Future clients (mobile apps, Slack/Teams bots) can use same API
- Clear separation of concerns per Constitution Principle III
- Blazor framework decision deferred to research phase

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations to track.** All architectural choices comply with constitutional principles.
