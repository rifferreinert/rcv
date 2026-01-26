# Technology Research: Web Application

**Feature**: Ranked Choice Voting Web Application
**Date**: 2025-10-26
**Purpose**: Resolve open technology decisions from Technical Context

## Research Questions

1. **Blazor Hosting Model**: Should we use Blazor Server or Blazor WebAssembly?
2. **UI Testing Framework**: Should we use bUnit or Playwright for component testing?
3. **OAuth Implementation**: What's the best approach for implementing 5 OAuth providers?
4. **Database Schema**: Review and validate proposed schema design
5. **EF Core Patterns**: What patterns should we use for this application?

---

## Decision 1: Blazor Hosting Model

### Question
Should the frontend use Blazor Server (SSR with SignalR) or Blazor WebAssembly (client-side SPA)?

### Research Findings

**Blazor Server**:
- ✅ Smaller initial download (no .NET runtime download)
- ✅ Faster initial load time
- ✅ Full .NET API access on server
- ✅ Better SEO (server-side rendering)
- ✅ Works on older/low-end devices
- ❌ Requires constant SignalR connection
- ❌ Higher server resource usage (stateful connections)
- ❌ Latency on every UI interaction (roundtrip to server)
- ❌ Doesn't work offline

**Blazor WebAssembly**:
- ✅ Runs entirely in browser after initial load
- ✅ No server resources needed for UI (stateless API)
- ✅ Can work offline (with PWA)
- ✅ Lower latency for UI interactions (no roundtrips)
- ❌ Large initial download (~2-3 MB .NET runtime)
- ❌ Slower initial load time
- ❌ Limited .NET API access (browser sandbox)
- ❌ Requires modern browsers with WebAssembly support

### Decision: **Blazor WebAssembly (WASM)**

**Rationale**:

1. **Aligns with RESTful API Architecture**: The spec requires "a great set of REST endpoints that can be used independently of the website itself" (user input). Blazor WASM naturally treats the API as a separate service, making it easier for future clients (mobile apps, Slack/Teams integrations) to consume the same API.

2. **Scalability**: With Blazor Server, each user requires a persistent SignalR connection, increasing server costs. Blazor WASM offloads UI processing to the client, allowing the backend to focus on API requests only. For 1000 concurrent users, this is more cost-effective.

3. **Performance After Initial Load**: Spec requires page load time <2 seconds (SC-008). While Blazor WASM has a slower first load, subsequent navigation is instant (no server roundtrips). For users who create multiple polls or vote frequently, this provides a better experience.

4. **Offline Capability (Future)**: While not required in Phase 1, Blazor WASM can be enhanced to work offline as a PWA, which could be valuable for mobile users with intermittent connectivity.

5. **Modern Browser Support**: Spec assumption #5 states "Users have modern browsers (latest 2 versions of major browsers)," which all support WebAssembly.

**Tradeoffs Accepted**:
- Slightly slower initial load (~3-5 seconds vs <1 second for Blazor Server)
- Still meets <2 second page load requirement with optimization (lazy loading, compression, CDN)
- Will use .NET 9 AOT compilation for WASM to reduce bundle size and improve performance

**Alternatives Considered**:
- **Blazor Server**: Rejected due to scalability concerns and API independence requirement
- **Blazor Hybrid (Auto)**: Rejected due to added complexity; not needed for this use case

---

## Decision 2: UI Testing Framework

### Question
Should we use bUnit (Blazor-specific component testing) or Playwright (E2E browser automation)?

### Research Findings

**bUnit**:
- ✅ Blazor-specific testing library
- ✅ Fast unit tests (no browser required)
- ✅ Renders components in memory
- ✅ Great for testing component logic and rendering
- ✅ Integrates with xUnit (already used for backend)
- ❌ Doesn't test real browser behavior
- ❌ Can't test JavaScript interop
- ❌ No visual regression testing

**Playwright**:
- ✅ Real browser automation (Chromium, Firefox, WebKit)
- ✅ Tests actual user experience
- ✅ Can test JavaScript interop and OAuth redirects
- ✅ Screenshot/video capture for debugging
- ✅ Cross-browser testing
- ❌ Slower than bUnit (launches real browsers)
- ❌ More complex setup
- ❌ Can be flaky with timing issues

### Decision: **Use Both (bUnit for Components + Playwright for E2E)**

**Rationale**:

1. **Testing Pyramid Approach**:
   - **Base**: xUnit unit tests for services and mappers (fast, numerous)
   - **Middle**: bUnit component tests for Blazor components (medium speed, focused)
   - **Top**: Playwright E2E tests for critical user journeys (slow, few)

2. **bUnit for Component Logic**:
   - Test individual components: `VotingInterface.razor`, `ResultsDisplay.razor`, `PollCard.razor`
   - Validate component state management, event handling, conditional rendering
   - Fast feedback during development (runs in <1 second)

3. **Playwright for Critical Paths**:
   - Test acceptance scenarios from spec (e.g., FR-001: OAuth sign-in flow)
   - Validate browser-specific behavior (OAuth redirects, session persistence)
   - Accessibility testing (WCAG 2.1 Level AA requirement)
   - One E2E test per user story (7 tests total)

**Test Distribution**:
- **bUnit**: ~30-40 component tests (one test per component behavior)
- **Playwright**: ~7-10 E2E tests (one per user story + critical edge cases)
- **xUnit**: ~50-60 service/integration tests (existing pattern from Rcv.Core.Tests)

**Tradeoffs Accepted**:
- Slightly more complex test setup (two frameworks instead of one)
- Worth it for comprehensive coverage: fast feedback (bUnit) + real-world validation (Playwright)

**Alternatives Considered**:
- **bUnit only**: Rejected because we need to test OAuth flows and browser-specific behavior
- **Playwright only**: Rejected because E2E tests are too slow for component-level TDD

---

## Decision 3: OAuth Implementation

### Question
How should we implement OAuth 2.0 authentication with 5 providers (Slack, Teams, Google, Apple, Microsoft)?

### Research Findings

**ASP.NET Core Built-in OAuth**:
- ✅ Microsoft.AspNetCore.Authentication.OAuth package
- ✅ Built-in support for Google, Microsoft, Apple (via official providers)
- ✅ Generic OAuth handler for Slack, Teams
- ✅ Automatic token refresh, cookie-based sessions
- ✅ Integrates with ASP.NET Core Identity
- ❌ Requires manual provider configuration (client IDs, secrets)
- ❌ No built-in Slack/Teams providers (must use generic OAuth)

**Third-Party Libraries (e.g., Auth0, IdentityServer)**:
- ✅ Unified auth experience across providers
- ✅ User management dashboard
- ✅ Advanced features (MFA, passwordless)
- ❌ External dependency and potential cost
- ❌ Data residency concerns (user data on third-party servers)
- ❌ Overkill for simple OAuth requirements

### Decision: **ASP.NET Core Built-in OAuth with Provider-Specific Handlers**

**Rationale**:

1. **Simplicity & YAGNI**: Spec requirements (FR-001 to FR-007) only need OAuth authentication and session management. Built-in middleware provides exactly this without extra features we don't need.

2. **No External Dependencies**: Keeps user data in our database; no third-party auth service required. Aligns with spec assumption #9 (shared environment, no org isolation).

3. **.NET Ecosystem Alignment**: Uses official Microsoft packages, well-documented and maintained:
   - `Microsoft.AspNetCore.Authentication.Google`
   - `Microsoft.AspNetCore.Authentication.MicrosoftAccount`
   - `AspNet.Security.OAuth.Apple`
   - `AspNet.Security.OAuth.Slack` (community package)
   - Generic OAuth handler for Teams (uses Microsoft OAuth)

4. **Token Management**: Built-in token refresh and cookie-based sessions match spec requirement (FR-004: 7-day session duration).

**Implementation Approach**:

```csharp
// Program.cs configuration
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => {
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // FR-004
    options.SlidingExpiration = true;
})
.AddGoogle(options => { /* client ID/secret from config */ })
.AddMicrosoftAccount(options => { /* client ID/secret */ })
.AddApple(options => { /* client ID/secret */ })
.AddSlack(options => { /* client ID/secret */ })
.AddOAuth("Teams", options => { /* Microsoft OAuth with Teams scope */ });
```

**Configuration**:
- Client IDs and secrets stored in Azure Key Vault (not appsettings.json)
- Each provider configured separately in `Authentication/OAuthProviders.cs`
- User created on first sign-in via `OnCreatingTicket` callback (FR-003)

**Tradeoffs Accepted**:
- Manual configuration for each provider (not a big issue - one-time setup)
- No built-in Slack/Teams providers (but community packages are stable and widely used)

**Alternatives Considered**:
- **Auth0/IdentityServer**: Rejected due to unnecessary complexity and external dependency
- **Custom OAuth implementation**: Rejected - reinventing the wheel when built-in solution exists

---

## Decision 4: Database Schema Review

### Question
Review and validate the proposed database schema for Users, Polls, PollOptions, Votes tables.

### Schema Analysis

**Provided Schema**:

```sql
-- Users: ExternalId + Provider uniqueness, basic profile
-- Polls: Poll metadata, settings, creator FK
-- PollOptions: Poll choices, display order
-- Votes: Ranked choices as JSON, poll + voter uniqueness
```

### Validation

**✅ Strengths**:

1. **Proper Normalization**:
   - Users, Polls, PollOptions, Votes are correctly separated
   - Foreign keys maintain referential integrity
   - Unique constraints enforce business rules (one vote per user per poll)

2. **Aligns with Rcv.Core Domain**:
   - `PollOptions.Id` maps to `Option.Id` (Guid)
   - `Votes.RankedChoices` (JSON array of GUIDs) maps to `RankedBallot.RankedOptionIds`
   - `RcvResult` is calculated, not stored (correct - always computed fresh from votes)

3. **Performance Considerations**:
   - Proper indexes: `IX_Users_ExternalId_Provider` for auth lookups
   - `IX_Polls_CreatorId` for dashboard queries
   - `IX_Votes_PollId` for vote retrieval
   - `IX_Polls_CreatedAt DESC` for recent polls

4. **Flexibility**:
   - `ClosesAt` NULL allows polls without deadlines
   - `Status` enum ('Active', 'Closed', 'Deleted') enables soft deletes
   - `IsResultsPublic` and `IsVotingPublic` support both public and private polls

**🔧 Recommended Refinements**:

1. **Add DisplayOrder to Votes** (Optional):
   - Current schema assumes votes are stored in ranked order in JSON
   - Consider adding `DisplayOrder` column if we need to query "all first-choice votes" efficiently
   - **Decision**: Not needed - JSON queries in Azure SQL are efficient for our scale

2. **Consider Soft Deletes for Votes** (Optional):
   - Current schema uses `CASCADE DELETE` on polls → options/votes
   - If we want vote audit history, add `IsDeleted` flag instead of hard delete
   - **Decision**: Not needed - spec says "permanently removed" (FR-037), hard delete is correct

3. **Add CreatedAt to PollOptions** (Already included):
   - ✅ Good for audit trail

4. **Add UpdatedAt to Votes** (Already included):
   - ✅ Tracks vote changes (though spec says one vote per user, no edits allowed)
   - Useful if we add "edit vote" feature in future

5. **JSON Column for RankedChoices**:
   - ✅ Azure SQL supports native JSON (SQL Server 2016+)
   - Use `NVARCHAR(MAX)` with JSON constraints:
     ```sql
     CONSTRAINT CK_Votes_RankedChoices_IsJson CHECK (ISJSON(RankedChoices) = 1)
     ```
   - Enables JSON queries: `JSON_VALUE(RankedChoices, '$[0]')` for first choice

### Decision: **Approve Schema with Minor Additions**

**Approved Schema**:

The provided schema is excellent and aligns well with domain models. Only minor additions:

```sql
-- Add JSON constraint to Votes.RankedChoices
ALTER TABLE Votes ADD CONSTRAINT CK_Votes_RankedChoices_IsJson
    CHECK (ISJSON(RankedChoices) = 1);

-- Add index for vote counting queries
CREATE INDEX IX_Votes_CastAt ON Votes(CastAt DESC);

-- Add index for poll status queries (active vs closed)
CREATE INDEX IX_Polls_Status_ClosesAt ON Polls(Status, ClosesAt);
```

**EF Core Entities**:

Entities will map 1:1 to tables:
- `User` entity → `Users` table
- `Poll` entity → `Polls` table (with `List<PollOption>` navigation property)
- `PollOption` entity → `PollOptions` table
- `Vote` entity → `Votes` table (with `List<Guid>` for RankedChoices)

**JSON Mapping in EF Core**:

```csharp
public class Vote {
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public Guid VoterId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public List<Guid> RankedChoices { get; set; } // EF Core 8 auto-maps to JSON

    public DateTime CastAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// OnModelCreating
modelBuilder.Entity<Vote>()
    .Property(v => v.RankedChoices)
    .HasColumnType("nvarchar(max)")
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
        v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null)
    );
```

**Alternatives Considered**:
- **Junction Table for Ranked Choices**: Rejected - more complex, harder to maintain rank order
- **Separate RcvResults Table**: Rejected - results should be computed on-demand, not cached (ensures freshness)

---

## Decision 5: EF Core Patterns

### Question
What EF Core patterns should we use for this application?

### Research Findings

**Repository Pattern**:
- ✅ Abstracts data access
- ✅ Easier to mock for unit tests
- ❌ Adds extra layer of abstraction
- ❌ DbContext already implements repository and unit of work patterns

**Direct DbContext Usage**:
- ✅ Simpler, less boilerplate
- ✅ DbContext is already abstracted (can mock `DbContext` in tests)
- ✅ Recommended by Microsoft for most applications
- ❌ Services have direct dependency on EF Core

**Service Layer with DbContext**:
- ✅ Business logic in services (PollService, VoteService)
- ✅ Services injected with DbContext
- ✅ Clean separation: Controllers → Services → DbContext
- ✅ Aligns with Constitution Principle III (separation of concerns)

### Decision: **Direct DbContext Injection into Services (No Repository Pattern)**

**Rationale**:

1. **YAGNI (Constitution Principle IV)**: Repository pattern adds complexity without benefit for our use case. DbContext is already a repository.

2. **Simplicity**: Services directly use DbContext for queries and updates:
   ```csharp
   public class PollService : IPollService {
       private readonly RcvDbContext _context;

       public PollService(RcvDbContext context) {
           _context = context;
       }

       public async Task<Poll> GetPollAsync(Guid id) {
           return await _context.Polls
               .Include(p => p.Options)
               .FirstOrDefaultAsync(p => p.Id == id);
       }
   }
   ```

3. **Testability**: Use in-memory or SQLite database for integration tests:
   ```csharp
   var options = new DbContextOptionsBuilder<RcvDbContext>()
       .UseInMemoryDatabase("TestDb")
       .Options;
   var context = new RcvDbContext(options);
   var service = new PollService(context);
   ```

4. **Performance**: Direct queries, no unnecessary abstractions or mapping overhead.

**Patterns We WILL Use**:

1. **Service Layer**:
   - `IPollService`, `IVoteService`, `IUserService` interfaces
   - Concrete implementations with DbContext dependency
   - Controllers depend on interfaces (for testability)

2. **Async/Await**:
   - All database operations async: `ToListAsync()`, `SaveChangesAsync()`
   - Matches ASP.NET Core async controller pattern

3. **Include() for Eager Loading**:
   - Load related data explicitly: `_context.Polls.Include(p => p.Options)`
   - Avoid N+1 query problems

4. **Transactions for Multi-Step Operations**:
   - Use `using var transaction = await _context.Database.BeginTransactionAsync();` for vote submission
   - Ensures atomicity per spec FR-062

5. **Migrations**:
   - Code-first migrations for schema changes
   - Keep migrations in source control
   - Apply via `dotnet ef database update` or startup code

**Patterns We WILL NOT Use** (YAGNI):

- ❌ Repository Pattern: DbContext is already a repository
- ❌ Unit of Work Pattern: DbContext is already a unit of work
- ❌ Specification Pattern: LINQ queries are sufficient
- ❌ CQRS: Simple CRUD doesn't warrant read/write separation

**Alternatives Considered**:
- **Generic Repository Pattern**: Rejected - over-abstraction for simple CRUD
- **Dapper (micro-ORM)**: Rejected - EF Core provides type safety and migrations

---

## Summary of Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| **Blazor Hosting** | Blazor WebAssembly | Scalability, API independence, better UX after initial load |
| **UI Testing** | bUnit + Playwright | Testing pyramid: fast component tests + real E2E tests |
| **OAuth** | ASP.NET Core Built-in OAuth | Simplicity, no external dependencies, meets all spec requirements |
| **Database Schema** | Approve with minor index additions | Well-designed, aligns with domain models, proper normalization |
| **EF Core Patterns** | Direct DbContext in Services | YAGNI, simplicity, DbContext is already a repository |

All open questions resolved. Ready to proceed to Phase 1: Design & Contracts.
