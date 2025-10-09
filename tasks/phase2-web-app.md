# Phase 2: Basic Web App Implementation Plan

## Quick Reference

- **Timeline**: 1 week (7 days)
- **Stack**: ASP.NET Core 9.0 + React 18 + TypeScript + Azure SQL Database
- **Auth**: OAuth2/OIDC (Slack, Teams, Google, Apple, Microsoft) → JWT tokens
- **Core Dependency**: Rcv.Core NuGet (1.0.0) for all voting calculations
- **Database**: Azure SQL Database (Serverless) with native JSON type
- **Architecture**: Clean Architecture (Controllers → Services → Data Layer)

## What We're Building

A responsive web application that enables:
- Poll creation with 2-50 ranked choice options
- Drag-and-drop voting interface (mobile-friendly)
- Round-by-round results visualization with live updates (optional)
- Multi-provider SSO authentication (no password management)
- Poll management dashboard for creators (close, delete polls)

## Dependencies

### Backend (NuGet Packages)
- Rcv.Core (1.0.0)
- Microsoft.EntityFrameworkCore (9.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (9.0.0)
- Microsoft.EntityFrameworkCore.Tools (9.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (9.0.0)
- Microsoft.AspNetCore.Authentication.OpenIdConnect (9.0.0)
- Microsoft.AspNetCore.Authentication.Google (9.0.0)
- Microsoft.AspNetCore.Authentication.MicrosoftAccount (9.0.0)
- AspNet.Security.OAuth.Slack (9.0.0)
- AspNet.Security.OAuth.Apple (9.0.0)
- Swashbuckle.AspNetCore (6.6.0)
- FluentValidation.AspNetCore (11.3.0)
- Serilog.AspNetCore (8.0.0)
- xUnit (2.9.0)
- Moq (4.20.0)
- FluentAssertions (6.12.0)
- Microsoft.AspNetCore.Mvc.Testing (9.0.0)

### Frontend (NPM Packages)
- react (^18.3.0)
- react-dom (^18.3.0)
- react-router-dom (^6.26.0)
- @tanstack/react-query (^5.51.0)
- axios (^1.7.0)
- tailwindcss (^3.4.0)
- @headlessui/react (^2.1.0)
- @heroicons/react (^2.1.0)
- @dnd-kit/core (^6.1.0)
- @dnd-kit/sortable (^8.0.0)
- recharts (^2.12.0)
- react-hook-form (^7.52.0)
- zod (^3.23.0)
- date-fns (^3.6.0)
- typescript (^5.5.0)
- vite (^5.4.0)
- @vitejs/plugin-react (^4.3.0)
- eslint (^9.9.0)
- prettier (^3.3.0)

## Project Structure

```
src/
├── Rcv.Core/                           [existing - Phase 1 NuGet library]
├── Rcv.Web.Api/                        [new - ASP.NET Core Web API]
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── AuthController.cs          (4 endpoints: login, callback, logout, me)
│   │   ├── PollsController.cs         (6 endpoints: CRUD + close)
│   │   ├── VotesController.cs         (3 endpoints: cast, get mine, count)
│   │   └── ResultsController.cs       (2 endpoints: results, live results)
│   ├── Services/
│   │   ├── AuthService.cs             (user lookup/create, JWT generation)
│   │   ├── PollService.cs             (poll business logic)
│   │   ├── VotingService.cs           (vote validation & persistence)
│   │   └── ResultsService.cs          (Rcv.Core integration + caching)
│   ├── Data/
│   │   ├── RcvDbContext.cs
│   │   └── Entities/
│   │       ├── User.cs                (provider-based identity)
│   │       ├── Poll.cs                (title, settings, status)
│   │       ├── PollOption.cs          (option text, display order)
│   │       └── Vote.cs                (JSON column: ranked choices)
│   └── Models/
│       ├── Requests/                  (CreatePollRequest, CastVoteRequest, UpdatePollRequest)
│       └── Responses/                 (PollResponse, VoteResponse, ResultResponse, PollListResponse)
└── rcv-web-ui/                         [new - React SPA]
    ├── package.json
    ├── vite.config.ts
    └── src/
        ├── App.tsx                     (routing setup)
        ├── pages/
        │   ├── Home.tsx               (landing page)
        │   ├── Login.tsx              (SSO buttons)
        │   ├── Dashboard.tsx          (user's polls list)
        │   ├── CreatePoll.tsx         (poll form)
        │   ├── PollDetail.tsx         (view poll + vote/results)
        │   └── Results.tsx            (results visualization)
        ├── components/
        │   ├── Layout.tsx             (nav header)
        │   ├── PollForm.tsx           (create/edit form)
        │   ├── PollCard.tsx           (dashboard card)
        │   ├── VotingInterface.tsx    (drag-and-drop ranking)
        │   ├── ResultsChart.tsx       (round-by-round viz)
        │   └── AuthButton.tsx         (login/logout button)
        ├── api/
        │   ├── client.ts              (Axios instance + interceptors)
        │   ├── auth.ts                (auth API calls)
        │   ├── polls.ts               (poll API calls)
        │   └── votes.ts               (voting API calls)
        ├── hooks/
        │   ├── useAuth.tsx            (auth context/hook)
        │   └── usePoll.tsx            (poll data fetching)
        └── types/                      (TypeScript type definitions)

tests/
├── Rcv.Core.Tests/                     [existing]
└── Rcv.Web.Api.Tests/                  [new - xUnit tests]
    ├── Controllers/                    (PollsControllerTests, VotesControllerTests)
    └── Services/                       (PollServiceTests, VotingServiceTests, ResultsServiceTests)

docs/
├── DEPLOYMENT.md                       [new - Azure deployment guide]
└── API.md                              [new - API documentation]
```

## Key Principles & Constraints

### ✅ Do This
- **Use Rcv.Core for ALL voting calculations** (never reimplement IRV algorithm)
- **Immutable domain models** (Rcv.Core) vs mutable entities (EF Core)
- **One vote per user per poll** (enforced at database level with unique constraint)
- **Soft delete** (set `Status = 'Deleted'` instead of hard delete)
- **Validate all inputs** (DTOs with FluentValidation + data annotations)
- **Cache results** (invalidate on new vote)

### ❌ Don't Do This
- Never expose internal database IDs in API responses (use GUIDs)
- Never calculate results in frontend (always call backend API)
- Never allow poll edits after votes are cast
- Never hard delete polls or votes (audit trail required)
- Never skip authorization checks (creator-only actions must verify ownership)

### Data Flow Pattern
1. **User Input** (Frontend) → DTO validation
2. **API Controller** → Service layer (business logic)
3. **Service** → EF Core entities (persistence)
4. **Service** → Rcv.Core domain models (calculations)
5. **Service** → Response DTOs
6. **API Controller** → JSON response

## API Specification

### Authentication Endpoints

```
GET  /api/auth/login/{provider}           Initiate OAuth flow (provider: slack, google, microsoft, apple)
GET  /api/auth/callback/{provider}        Handle OAuth callback, return JWT
POST /api/auth/logout                     Clear session/token
GET  /api/auth/me                         Get current user info
```

### Poll Management Endpoints

```
POST   /api/polls                         Create poll (auth required)
       Request: CreatePollRequest
       Response: PollResponse

GET    /api/polls/{id}                    Get poll details
       Response: PollResponse

GET    /api/polls                         List polls (query: creatorId, status, page, pageSize)
       Response: PollListResponse

PUT    /api/polls/{id}                    Update poll (creator only, before votes)
       Request: UpdatePollRequest
       Response: PollResponse

DELETE /api/polls/{id}                    Soft delete poll (creator only)
       Response: 204 No Content

POST   /api/polls/{id}/close              Close poll early (creator only)
       Response: PollResponse
```

### Voting Endpoints

```
POST /api/polls/{pollId}/votes            Cast or update vote (auth required)
     Request: CastVoteRequest
     Response: VoteResponse

GET  /api/polls/{pollId}/votes/me         Get current user's vote
     Response: VoteResponse | 404

GET  /api/polls/{pollId}/votes/count      Get participation stats
     Response: { totalVotes: number, uniqueVoters: number }
```

### Results Endpoints

```
GET /api/polls/{pollId}/results           Get final results (visibility rules apply)
    Response: ResultResponse

GET /api/polls/{pollId}/results/live      Get live results (if enabled)
    Response: ResultResponse
```

### Authorization Rules

| Action | Allowed Users |
|--------|---------------|
| Create poll | Any authenticated user |
| View poll details | Anyone (public) |
| Vote in poll | Any authenticated user (once per poll) |
| View live results | Poll creator OR anyone if `IsResultsPublic = true` |
| View final results | Anyone after poll closes OR poll creator anytime |
| Close poll | Poll creator only |
| Delete poll | Poll creator only |
| Update poll | Poll creator only (before votes cast) |

## Data Models

### Backend DTOs (C#)

```csharp
// Request DTOs
public class CreatePollRequest
{
    public string Title { get; set; }                    // Required, max 500 chars
    public string? Description { get; set; }
    public List<string> Options { get; set; }            // 2-50 options, each max 500 chars
    public DateTime? ClosesAt { get; set; }
    public bool IsResultsPublic { get; set; } = true;    // Live results visible?
    public bool IsVotingPublic { get; set; } = false;    // Votes anonymous or public?
}

public class CastVoteRequest
{
    public List<Guid> RankedOptionIds { get; set; }      // Ordered list, no duplicates
}

// Response DTOs
public class PollResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public UserSummary Creator { get; set; }
    public List<PollOptionDto> Options { get; set; }
    public string Status { get; set; }                   // "Active", "Closed", "Deleted"
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsResultsPublic { get; set; }
    public bool IsVotingPublic { get; set; }
    public int VoteCount { get; set; }
}

public class ResultResponse
{
    public Guid PollId { get; set; }
    public PollOptionDto? Winner { get; set; }           // null if tie
    public bool IsTie { get; set; }
    public List<PollOptionDto> TiedOptions { get; set; }
    public List<RoundSummaryDto> Rounds { get; set; }
    public Dictionary<Guid, int> FinalVoteTotals { get; set; }
    public int TotalVotes { get; set; }
}

public class RoundSummaryDto
{
    public int RoundNumber { get; set; }
    public Dictionary<Guid, int> VoteCounts { get; set; }
    public PollOptionDto? EliminatedOption { get; set; }
}
```

### Frontend Types (TypeScript)

```typescript
// Request types
interface CreatePollRequest {
  title: string;
  description?: string;
  options: string[];
  closesAt?: string;  // ISO 8601 date string
  isResultsPublic: boolean;
  isVotingPublic: boolean;
}

interface CastVoteRequest {
  rankedOptionIds: string[];  // GUIDs
}

// Response types
interface PollResponse {
  id: string;
  title: string;
  description?: string;
  creator: { id: string; name: string; email: string };
  options: PollOption[];
  status: 'Active' | 'Closed' | 'Deleted';
  createdAt: string;
  closesAt?: string;
  closedAt?: string;
  isResultsPublic: boolean;
  isVotingPublic: boolean;
  voteCount: number;
}

interface PollOption {
  id: string;
  text: string;
  displayOrder: number;
}

interface ResultResponse {
  pollId: string;
  winner?: PollOption;
  isTie: boolean;
  tiedOptions: PollOption[];
  rounds: RoundSummary[];
  finalVoteTotals: Record<string, number>;
  totalVotes: number;
}

interface RoundSummary {
  roundNumber: number;
  voteCounts: Record<string, number>;
  eliminatedOption?: PollOption;
}
```

## Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExternalId NVARCHAR(255) NOT NULL,        -- Provider's user ID
    Provider NVARCHAR(50) NOT NULL,           -- 'Slack', 'Google', etc.
    Email NVARCHAR(255),
    DisplayName NVARCHAR(255),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2,
    CONSTRAINT UQ_Users_ExternalId_Provider UNIQUE (ExternalId, Provider)
);
CREATE INDEX IX_Users_ExternalId_Provider ON Users(ExternalId, Provider);
```

### Polls Table
```sql
CREATE TABLE Polls (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    CreatorId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ClosesAt DATETIME2,                       -- NULL = no deadline
    ClosedAt DATETIME2,                       -- Actual close timestamp
    IsResultsPublic BIT NOT NULL DEFAULT 1,   -- 0 = private until close, 1 = live results
    IsVotingPublic BIT NOT NULL DEFAULT 0,    -- 0 = anonymous, 1 = public votes
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active', -- 'Active', 'Closed', 'Deleted'
    CONSTRAINT FK_Polls_Creator FOREIGN KEY (CreatorId) REFERENCES Users(Id)
);
CREATE INDEX IX_Polls_CreatorId ON Polls(CreatorId);
CREATE INDEX IX_Polls_Status ON Polls(Status);
CREATE INDEX IX_Polls_CreatedAt ON Polls(CreatedAt DESC);
```

### PollOptions Table
```sql
CREATE TABLE PollOptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PollId UNIQUEIDENTIFIER NOT NULL,
    OptionText NVARCHAR(500) NOT NULL,
    DisplayOrder INT NOT NULL,               -- Order shown to users
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PollOptions_Poll FOREIGN KEY (PollId) REFERENCES Polls(Id) ON DELETE CASCADE
);
CREATE INDEX IX_PollOptions_PollId ON PollOptions(PollId);
```

### Votes Table
```sql
CREATE TABLE Votes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PollId UNIQUEIDENTIFIER NOT NULL,
    VoterId UNIQUEIDENTIFIER NOT NULL,
    RankedChoices JSON NOT NULL,             -- Azure SQL native JSON: ["guid1", "guid2", ...]
    CastAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    CONSTRAINT FK_Votes_Poll FOREIGN KEY (PollId) REFERENCES Polls(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Votes_Voter FOREIGN KEY (VoterId) REFERENCES Users(Id),
    CONSTRAINT UQ_Votes_PollId_VoterId UNIQUE (PollId, VoterId)  -- One vote per user per poll
);
CREATE INDEX IX_Votes_PollId ON Votes(PollId);
CREATE INDEX IX_Votes_VoterId ON Votes(VoterId);
```

### EF Core JSON Configuration

```csharp
// In RcvDbContext.OnModelCreating
modelBuilder.Entity<Vote>()
    .Property(v => v.RankedChoices)
    .HasColumnType("json")
    .IsRequired();

// Vote entity property
public class Vote
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public Guid VoterId { get; set; }
    public List<Guid> RankedChoices { get; set; } = new();  // EF maps to/from JSON column
    public DateTime CastAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Poll Poll { get; set; } = null!;
    public User Voter { get; set; } = null!;
}
```

## Example Data Flows

### Flow 1: User Casts Vote

```
1. Frontend: User drags options to rank [Alice, Bob, Charlie]
2. Frontend: Validates (no duplicates, at least 1 choice)
3. Frontend: POST /api/polls/abc123/votes
   Body: { "rankedOptionIds": ["guid-alice", "guid-bob", "guid-charlie"] }

4. API: VotesController.CastVote()
   - Extracts user ID from JWT token
   - Calls VotingService.CastVoteAsync()

5. Service: VotingService
   - Validates poll exists and is open (Status = 'Active', ClosedAt = null)
   - Validates all option IDs belong to this poll
   - Checks for existing vote (unique constraint)
   - Creates/updates Vote entity with JSON: ["guid-alice", "guid-bob", "guid-charlie"]
   - Saves to database
   - Invalidates results cache for this poll

6. API: Returns VoteResponse { voteId, pollId, castAt }

7. Frontend: Shows success message, redirects to results (if visible)
```

### Flow 2: Calculate Results

```
1. Frontend: GET /api/polls/abc123/results

2. API: ResultsController.GetResults()
   - Extracts user ID from JWT (if authenticated)
   - Calls ResultsService.CalculateResultsAsync()

3. Service: ResultsService
   - Checks authorization (creator OR public results OR poll closed)
   - Checks cache for pollId
   - If cache miss:
     a. Fetch Poll with Options from database
     b. Fetch all Votes for poll, deserialize JSON to List<Guid>
     c. Map to Rcv.Core types:
        - PollOption entities → Option[] (Guid Id, string Label)
        - Vote.RankedChoices → RankedBallot[] objects
     d. Call: new RankedChoicePoll(options).CalculateResult(ballots, new InstantRunoffCalculator())
     e. Map RcvResult → ResultResponse DTO
     f. Cache result (invalidate on new vote)

4. API: Returns ResultResponse (winner, rounds, stats)

5. Frontend: Displays winner + round-by-round chart
```

### Flow 3: OAuth Authentication

```
1. Frontend: User clicks "Sign in with Google"
2. Frontend: window.location = "/api/auth/login/google"

3. API: AuthController.Login("google")
   - Initiates OAuth flow via Google provider
   - Redirects to Google consent screen

4. Google: User approves, redirects to /api/auth/callback/google?code=xyz

5. API: AuthController.Callback("google", code)
   - Exchanges code for access token
   - Fetches user profile (id, email, name)
   - Calls AuthService.GetOrCreateUserAsync(externalId: "google-123", provider: "Google", profile)

6. Service: AuthService
   - Queries Users table for (ExternalId = "google-123", Provider = "Google")
   - If not found: creates new User
   - Updates LastLoginAt
   - Generates JWT with claims: { userId, email, name }

7. API: Sets JWT in cookie or returns in response body

8. Frontend: Stores JWT in localStorage, redirects to /dashboard
```

## Implementation Considerations

### Authentication Flow
- Use `Microsoft.AspNetCore.Authentication.OpenIdConnect` for OAuth2/OIDC
- Store JWT in frontend localStorage (or httpOnly cookie for better security)
- Include JWT in `Authorization: Bearer <token>` header for all API calls
- Token expiration: 7 days (configurable)

### Data Mapping: EF Entities ↔ Rcv.Core Domain Models

```csharp
// When calculating results
var poll = await _context.Polls
    .Include(p => p.Options)
    .FirstAsync(p => p.Id == pollId);

var votes = await _context.Votes
    .Where(v => v.PollId == pollId)
    .ToListAsync();

// Map to Rcv.Core types
var options = poll.Options
    .Select(o => new Option(o.Id, o.OptionText))
    .ToList();

var ballots = votes
    .Select(v => new RankedBallot(v.RankedChoices))  // v.RankedChoices is List<Guid>
    .ToList();

// Calculate
var rcvPoll = new RankedChoicePoll(options);
var result = rcvPoll.CalculateResult(ballots, new InstantRunoffCalculator());

// Map back to DTO
var response = new ResultResponse
{
    Winner = result.Winner != null ? MapToDto(result.Winner) : null,
    IsTie = result.IsTie,
    Rounds = result.Rounds.Select(r => MapToDto(r, poll.Options)).ToList(),
    // ... etc
};
```

### Validation Strategy
- **Client-side**: Zod schemas for forms (immediate feedback)
- **Server-side**: FluentValidation for DTOs (security boundary)
- **Database**: Constraints for data integrity (unique votes, foreign keys)

### Caching Strategy
```csharp
// In ResultsService
private readonly IMemoryCache _cache;

public async Task<ResultResponse> CalculateResultsAsync(Guid pollId)
{
    var cacheKey = $"results:{pollId}";

    if (_cache.TryGetValue(cacheKey, out ResultResponse? cached))
        return cached!;

    var result = await ComputeResultsAsync(pollId);

    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

    return result;
}

// Invalidate on new vote
public async Task CastVoteAsync(Guid pollId, Guid userId, List<Guid> rankedOptionIds)
{
    // ... save vote ...

    _cache.Remove($"results:{pollId}");
}
```

### Performance Optimizations
- **Eager loading**: Use `.Include()` for related entities (avoid N+1 queries)
- **Pagination**: Limit poll lists to 20 per page
- **JSON querying**: Native JSON type is pre-parsed (faster than NVARCHAR with JSON functions)
- **Indexes**: Covered indexes on common queries (polls by creator, votes by poll)

### Security Checklist
- [ ] Validate all DTOs with FluentValidation
- [ ] Sanitize HTML in poll titles/descriptions (prevent XSS)
- [ ] Rate limit authentication endpoints (prevent brute force)
- [ ] Rate limit voting endpoint (prevent spam)
- [ ] Use HTTPS only in production
- [ ] Set CORS to allow only known origins
- [ ] Never expose internal user IDs (use GUIDs)
- [ ] Log failed authentication attempts
- [ ] Implement CSRF protection for cookie-based auth

### Accessibility Requirements (WCAG 2.1 Level AA)
- Keyboard navigation for all interactive elements
- ARIA labels on drag-and-drop interface
- Color contrast ratio ≥ 4.5:1
- Screen reader support (semantic HTML, alt text)
- Focus indicators visible on all focusable elements
- Touch targets ≥ 44px on mobile

## Implementation Tasks

> **See detailed task breakdown**: `tasks-phase2-web-app.md`

The implementation is organized into 14 task sections with ~90 subtasks total:

1. Project Setup and Infrastructure
2. Database Design and Entity Framework Setup
3. Authentication & Authorization
4. Poll Management API
5. Voting API
6. Results Calculation and Visualization API
7. Frontend Core Infrastructure
8. Frontend Authentication UI
9. Frontend Poll Management UI
10. Frontend Voting Interface
11. Frontend Results Visualization
12. Testing and Quality Assurance
13. Documentation and Deployment Preparation
14. Security Hardening and Polish

### Recommended Development Sequence

**Days 1-2**: Backend foundation (tasks 1.0, 2.0, 3.0)
- Set up projects, database, authentication
- Get basic API endpoints working with auth
- Test with Postman/Swagger

**Days 3-4**: Core API functionality (tasks 4.0, 5.0, 6.0)
- Implement poll management, voting, results
- Write comprehensive tests
- Validate integration with Rcv.Core

**Days 5-6**: Frontend development (tasks 7.0, 8.0, 9.0, 10.0, 11.0)
- Build UI components and pages
- Connect to backend API
- Polish UX and responsive design

**Day 7+**: Testing and deployment (tasks 12.0, 13.0, 14.0)
- QA and bug fixes
- Documentation
- Deploy to Azure

### Success Criteria

- [ ] Users can create polls with multiple options via web UI
- [ ] Users can authenticate with at least 3 SSO providers (Slack, Google, Microsoft)
- [ ] Users can vote using intuitive drag-and-drop interface
- [ ] Users can view live or final results with round-by-round breakdown
- [ ] Poll creators can manage their polls (close, delete)
- [ ] App is responsive and accessible (mobile-friendly, WCAG AA)
- [ ] Backend API is fully tested (≥80% coverage)
- [ ] App is deployed to Azure with production-ready configuration
