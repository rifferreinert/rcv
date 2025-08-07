# Ranked Choice Voting Platform PRD

### TL;DR

The Ranked Choice Voting Platform helps business users create, run, and
analyze fair ranked choice polls through a responsive web application that
integrates seamlessly with Slack and Teams via deep linking. By focusing on
a great web experience with simple built-in authentication, it enables
decision-making for any group without the complexity of managing native
platform apps. Core voting logic and analytics are encapsulated in a
reusable .NET NuGet module for easy use in other apps.

------------------------------------------------------------------------

## Goals

### Business Goals

- Enable seamless ranked choice voting for teams and organizations via a
  responsive web application accessible from any platform.

- Drive adoption within business groups through deep linking integration
  with Slack and Teams.

- Encourage reuse of the voting logic through a distributable .NET NuGet
  library.

- Collect actionable feedback to iterate rapidly on core features.

### User Goals

- Allow users to quickly launch and participate in ranked choice polls
  through a web interface that works seamlessly when linked from Slack or Teams.

- Offer transparent, real-time results and clear statistics to inform
  group decisions.

- Minimize friction with simple registration and authentication using
  ASP.NET Core Identity.

- Make poll creation and participation accessible, with no technical
  knowledge required.

### Non-Goals

- Building and maintaining native Slack and Teams applications at launch.

- Supporting platforms beyond a responsive web interface with deep linking
  capabilities.

- Implementing non-RCV voting methods or in-depth analytics beyond
  essential poll statistics.

------------------------------------------------------------------------

## User Stories

**Persona 1: Poll Creator (Business User, Team Lead)**

- As a Poll Creator, I want to create a new ranked choice poll on the web
  and easily share it via Slack/Teams so that my team can collaboratively
  make decisions.

- As a Poll Creator, I want to easily add poll options and set voting
  rules so that the poll fits my group's needs.

- As a Poll Creator, I want to monitor participation and view live
  results so that I can encourage engagement and transparency.

- As a Poll Creator, I want special privileges to manage my poll, like
  ending it early or deleting it.

**Persona 2: Voter (General Group Member)**

- As a Voter, I want to receive a direct link to participate in a poll
  within the platform I use most (Slack/Teams).

- As a Voter, I want to rank my choices in order of preference with
  minimal clicks or taps.

- As a Voter, I want to view results and understand how my vote
  contributed once the poll closes.

------------------------------------------------------------------------

## Functional Requirements

- **Poll Management (Priority: High)**

  - Poll creation (Web): Launch new RCV polls; set description, options,
    duration.

  - Poll creator privileges:

    - End a poll early (close voting at any time).

    - Decide if poll results are available live (visible during voting)
      or only after poll closes.

    - Choose if votes are public (visible) or private (anonymous).

    - Delete a poll.

  - Management dashboard (Web): View all active/past polls, basic
    statistics.

- **Voting (Priority: High)**

  - Secure, authenticated voting via ASP.NET Core Identity with email
    verification.

  - Easy ranking UI (drag-and-drop or selection matrix).

  - Single vote per user, enforced by user account.

- **Live Results & Statistics (Priority: Medium)**

  - Real-time results visualizations (round-by-round elimination and
    winner selection).

  - Display of essential statistics: participation rate, option
    performance, tie handling.

- **Integrations (Priority: High)**

  - Deep linking support: Generate shareable links that work seamlessly
    when posted in Slack, Teams, or any messaging platform.

  - Open Graph metadata for rich link previews in chat applications.

  - Simple, responsive web app optimized for mobile and desktop browsers.

- **User Authentication (Priority: High)**

  - Built-in authentication using ASP.NET Core Identity with email/password.

  - Email verification for account activation.

  - Password reset functionality.

  - Optional: Magic link authentication for passwordless login in future phases.

  - Identity mapped via user accounts in the system.

- **Data Storage (Priority: High)**

  - Persist polls, votes, and user info in Azure SQL Database serverless
    using Entity Framework Core.

  - Ensure efficient querying and data integrity with proper indexing.

- **RCV Module (Priority: High)**

  - Isolate all RCV tallying logic/statistics in a reusable .NET NuGet
    package.

  - Ensure robust testing, open API, and clear documentation.

------------------------------------------------------------------------

## User Experience

**Entry Point & First-Time User Experience**

- Users discover the app through links shared in Slack/Teams channels or
  direct invitations via email.

- First-time access triggers a simple registration flow using email and
  password.

- Clear welcome page highlighting poll creation and participation features.

- Email verification required before first poll creation (but not for voting).

**Core Experience**

- **Step 1:** Organizers create a poll on the web platform.

  - UI prompts them for poll title, options to rank, duration, and
    optional settings.

  - UI ensures clear required fields, concise help text, and validation
    feedback.

- **Step 2:** Poll generates a shareable link with Open Graph metadata.

  - Organizer copies link and posts in Slack/Teams channel or shares via
    email.

  - Link preview shows poll title, description, and "Vote Now" call-to-action.

- **Step 3:** Voters click the link, register/login if needed, and are
  presented with a clean, minimal ranking UI.

  - Drag-and-drop or radio matrix; error checks for duplicate/empty
    rankings.

  - On submit, visual confirmation of successful vote.

- **Step 4:** Organizer and voters can view live results (if enabled) or
  wait for poll closure for a rich breakdown.

  - Results page animates round-by-round elimination and final winner;
    participation stats shown clearly.

  - UI is fully responsive and visually consistent across all devices.

**Advanced Features & Edge Cases**

- Poll creators can edit or close an active poll.

- Voters who attempt to vote multiple times are gently notified.

- Edge case: In case of database connection failure, users receive
  actionable error messages and retry options.

**UI/UX Highlights**

- High contrast colors, large clickable areas, and WCAG-compliant text.

- Accessible keyboard navigation; mobile-first responsive design.

- Consistent branding, friendly tone, and clear microcopy throughout.

------------------------------------------------------------------------

## Narrative

A mid-sized company's marketing team often struggles to agree on
campaign priorities in endless Slack threads. Needing a better way to
make collaborative decisions, the team lead opens the Ranked Choice
Voting Platform website and creates a poll for which campaign to tackle
next—adding options and setting a deadline in seconds.

She copies the generated link and posts it in their Slack channel with
a message: "Let's vote on our next campaign!" The link unfurls with a
rich preview showing the poll title and a "Vote Now" button. Team
members click through, quickly register with their work email (or login
if returning), and use an intuitive drag-and-drop interface to rank
their preferences.

Within minutes, everyone submits their votes. As soon as the poll
closes, the live result animation reveals the winning campaign,
accompanied by transparent stats—no ambiguity, no drama. The entire
team feels heard and confident in the group's decision.

Meanwhile, the company's tech lead, intrigued by the seamless voting
logic, pulls the underlying NuGet package to reuse the robust RCV
tallying in an internal .NET application. The platform's simplicity and
flexibility delight both business users and developers, amplifying its
reach across the organization.

------------------------------------------------------------------------

## Success Metrics

### User-Centric Metrics

- Number of polls created per week

- Percentage of invited users who complete registration and vote

- User satisfaction (in-app survey score ≥ 80%)

- Time from poll link click to vote submission

### Business Metrics

- Growth in weekly active users

- NuGet package downloads and reuses by other development teams

- Retention: repeat usage by users/organizations

### Technical Metrics

- Platform uptime ≥ 99.9%

- Average page load time < 2 seconds

- Error rate < 1% for voting and poll result calculations

### Tracking Plan

- Poll creation events (with context: options, poll size, settings)

- Vote submission events

- Poll result views (by user/source)

- User registration and login events

- Link click-through rates from various sources

- NuGet package download and versioning analytics

------------------------------------------------------------------------

## Technical Considerations

### Technical Needs

- Single responsive web application built with ASP.NET Core MVC or Blazor.

- API server for poll orchestration, state management, and business logic.

- Dedicated .NET NuGet package for all RCV tallying/statistics,
  independently testable and documented.

- Azure SQL Database serverless with Entity Framework Core for persistent
  storage of polls, votes, and user accounts.

### Integration Points

- Deep linking with Open Graph metadata for rich previews in Slack, Teams,
  and other platforms.

- ASP.NET Core Identity for user authentication and authorization.

- Entity Framework Core for database operations and migrations.

### Data Storage & Privacy

- User data: email, hashed passwords, display name stored securely.

- Data encrypted at rest and in transit.

- Compliance with organizational security standards and privacy
  best-practices.

- Azure SQL Database serverless for cost-effective scaling based on usage.

### Scalability & Performance

- Designed for spikes in voting activity (e.g., company-wide polls).

- Azure SQL Database serverless auto-scales based on demand.

- Efficient EF Core queries with proper indexing and eager loading where needed.

- NuGet package built for high concurrency and stateless usage.

### Potential Challenges

- Ensuring smooth user experience when transitioning from chat platforms to web.

- Ensuring poll integrity (no duplicate votes, secure tallying).

- Managing database migrations and schema evolution with EF Core.

- Maintaining modularity between app logic and the RCV core across environments.

------------------------------------------------------------------------

## Milestones & Sequencing

### Project Estimate

- Medium: 2–3 weeks for MVP; core team focused and lean.

### Team Size & Composition

- Small Team: 2–3 people

  - Product/Eng Lead (oversees product, backend, NuGet module)

  - Full-Stack Developer (web UI, authentication, database)

  - (Optional: Part-time designer or QA for usability testing)

### Suggested Phases

**Phase 1: RCV NuGet Module (1 week)**

- Deliverables: Well-tested, documented .NET library for RCV tallying,
  with core API and sample data.

- Dependencies: None.

**Phase 2: Core Web Application (1 week)**

- Deliverables: Responsive web UI; ASP.NET Core Identity authentication;
  poll creation, voting, live results; Azure SQL Database with EF Core
  integration; connects to RCV module; deep linking support with Open Graph
  metadata.

- Dependencies: Completed NuGet package.

**Phase 3: Polish & Optimization (3-4 days)**

- Deliverables: Enhanced mobile experience, performance optimizations,
  comprehensive error handling, basic analytics integration.

- Dependencies: Core Web Application complete.

**Phase 4: Testing, Feedback & Launch (2-3 days)**

- Deliverables: Usability polish, security review, database performance
  tuning, deployment to Azure, refine based on user feedback.

- Dependencies: Previous phases complete.

------------------------------------------------------------------------

## RCV Module API Specification

### API Overview

The NuGet package is a self-contained module for handling all ranked
choice voting logic and statistics. It ensures a clean separation of UI,
data storage, and user management from core RCV operations. The
consuming application is responsible for handling the management of
Option IDs.

### What The NuGet Package Should Do

**Inputs:**

- A list of options, each represented by an Option record.

- A list of ballots, where each ballot is an ordered list of option
  identifiers (GUIDs).

**Outputs:**

- Winning option(s)

- Round-by-round elimination tables (which option was eliminated in each
  round)

- Per-option statistics—first-choice votes, final round runoff, etc.

- Edge case markers: for example, incomplete/invalid ballots, ties, or
  ambiguous outcomes

**Public API Sketch (C#-ish):**

```csharp
public class RankedChoicePoll
{
    // Initialize with your poll options
    public RankedChoicePoll(IEnumerable<Option> options);

    // Add many ballots at once (could be called once at poll close)
    public void AddBallots(IEnumerable<RankedBallot> ballots);

    // Tally and return high-level results: winner, round summaries, etc.
    public RcvResult CalculateResult();
}

// Ballot structure
public class RankedBallot
{
    public List<Guid> RankedOptionIds { get; set; }
}

// Option representation
public record Option(Guid Id, string Label);

// What is returned as the result
public class RcvResult
{
    public Option Winner { get; set; }
    public List<RoundSummary> Rounds { get; set; }
    public bool IsTie { get; set; }
    public List<Option> TiedOptions { get; set; }
    public Dictionary<Option, int> FinalVoteTotals { get; set; }
    // Add other stats or edge cases as needed
}

public class RoundSummary
{
    public int RoundNumber { get; set; }
    public Dictionary<Option, int> VoteCounts { get; set; }
    public Option EliminatedOption { get; set; }
}
```

### What It Never Does:

- No DB or session access inside the package.

- No user/account logic.

- No notification sending/UI logic.

## Responsibilities

- Management of option IDs and unique IDs for ballots is handled
  externally by the consuming app.

- The package exposes a consistent and transparent API for RCV logic,
  which can be accessed by any front-end or other backend system.
- Management of option IDs and unique IDs for ballots is handled
  externally by the consuming app.

- The package exposes a consistent and transparent API for RCV logic,
  which can be accessed by any front-end or other backend system.

------------------------------------------------------------------------
