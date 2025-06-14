# Ranked Choice Voting Platform PRD

### TL;DR

The Ranked Choice Voting Platform helps business users create, run, and
analyze fair ranked choice polls directly from Slack, Teams, or a simple
website. By integrating with workplace chat and using streamlined
authentication, it enables decision-making for any group, avoiding the
hassle of managing complex user accounts. Core voting logic and
analytics are encapsulated in a reusable .NET NuGet module for easy use
in other apps.

------------------------------------------------------------------------

## Goals

### Business Goals

- Enable seamless ranked choice voting for teams and organizations via
  Slack, Teams, and the web.

- Drive adoption within business groups through easy integration and
  intuitive UI.

- Encourage reuse of the voting logic through a distributable .NET NuGet
  library.

- Collect actionable feedback to iterate rapidly on core features.

### User Goals

- Allow users to quickly launch and participate in ranked choice polls
  without leaving their preferred platform.

- Offer transparent, real-time results and clear statistics to inform
  group decisions.

- Minimize friction with simple onboarding and authentication via Slack,
  Teams, or social login.

- Make poll creation and participation accessible, with no technical
  knowledge required.

### Non-Goals

- Managing advanced user account features (profiles, permissions,
  settings, etc.) within the app.

- Supporting platforms beyond Slack, Teams, and a basic web interface at
  launch.

- Implementing non-RCV voting methods or in-depth analytics beyond
  essential poll statistics.

------------------------------------------------------------------------

## User Stories

**Persona 1: Poll Creator (Business User, Team Lead)**

- As a Poll Creator, I want to create a new ranked choice poll within
  Slack/Teams/web so that my team can collaboratively make decisions.

- As a Poll Creator, I want to easily add poll options and set voting
  rules so that the poll fits my group’s needs.

- As a Poll Creator, I want to monitor participation and view live
  results so that I can encourage engagement and transparency.

- As a Poll Creator, I want special privileges to manage my poll, like
  ending it early or deleting it.

**Persona 2: Voter (General Group Member)**

- As a Voter, I want to receive a direct invite or notification to
  participate in a poll within the platform I use most.

- As a Voter, I want to rank my choices in order of preference with
  minimal clicks or taps.

- As a Voter, I want to view results and understand how my vote
  contributed once the poll closes.

------------------------------------------------------------------------

## Functional Requirements

- **Poll Management (Priority: High)**

  - Poll creation (Slack, Teams, Web): Launch new RCV polls; set
    description, options, duration.

  - Poll creator privileges:

    - End a poll early (close voting at any time).

    - Decide if poll results are available live (visible during voting)
      or only after poll closes.

    - Choose if votes are public (visible) or private (anonymous).

    - Delete a poll.

  - Management dashboard (Web): View all active/past polls, basic
    statistics.

- **Voting (Priority: High)**

  - Secure, authenticated voting via SSO with Slack and Teams; no
    password-based accounts.

  - Easy ranking UI (drag-and-drop or selection matrix).

  - Single vote per user, enforced by platform identity.

- **Live Results & Statistics (Priority: Medium)**

  - Real-time results visualizations (round-by-round elimination and
    winner selection).

  - Display of essential statistics: participation rate, option
    performance, tie handling.

- **Integrations (Priority: High)**

  - Slack app: Slash commands, interactive messages for creating/voting
    in polls.

  - Teams app: Similar features adapted to Teams workflow.

  - Simple, responsive web app for universal access.

- **User Authentication (Priority: High)**

  - Delegate authentication to Slack and Teams as Identity Providers for
    security; no standalone accounts.

  - Web users will use SSO via "Sign in with Slack" or "Sign in with
    Teams."

  - The basic web app will support standard social logins via Google,
    Apple, and Microsoft to allow users who do not use Slack or Teams to
    participate.

  - Identity mapped via workspace/user IDs.

- **Data Storage (Priority: High)**

  - Persist polls, votes, and minimal user info in CosmosDB.

  - Ensure accessibility by integration type.

- **RCV Module (Priority: High)**

  - Isolate all RCV tallying logic/statistics in a reusable .NET NuGet
    package.

  - Ensure robust testing, open API, and clear documentation.

------------------------------------------------------------------------

## User Experience

**Entry Point & First-Time User Experience**

- Users discover the app through Slack/Teams app directories or links
  shared by colleagues, or via the product website.

- First-time access triggers a clear welcome and brief intro (modal or
  message), highlighting poll creation and participation.

- Slack/Teams users authenticate via their existing org login; web users
  can authenticate using SSO via existing accounts (Google, Apple,
  Microsoft in Phase 2).

- No lengthy onboarding flows or unnecessary profile creation.

**Core Experience**

- **Step 1:** Organizers start a poll (via slash command in Slack/Teams
  or “New Poll” button on web).

  - UI prompts them for poll title, options to rank, duration, and
    group/channel to share with.

  - UI ensures clear required fields, concise help text, and validation
    feedback.

- **Step 2:** Poll is posted in selected channel (Slack/Teams) or
  linked/viewable on the web.

  - Interactive message/component encourages participation; direct link
    provided.

- **Step 3:** Voters click the poll, authenticate if prompted, and are
  presented with a clean, minimal ranking UI.

  - Drag-and-drop or radio matrix; error checks for duplicate/empty
    rankings.

  - On submit, visual confirmation of successful vote.

- **Step 4:** Organizer and voters can view live results (if enabled) or
  wait for poll closure for a rich breakdown.

  - Results page animates round-by-round elimination and final winner;
    participation stats shown clearly.

  - UI is fully responsive and visually consistent across platforms.

**Advanced Features & Edge Cases**

- Poll creators can edit or close an active poll.

- Voters who attempt to vote multiple times are gently notified.

- Edge case: In case of API failure (Slack/Teams), users receive
  actionable error messages and retry options.

**UI/UX Highlights**

- High contrast colors, large clickable areas, and WCAG-compliant text.

- Accessible keyboard navigation; mobile-friendly layouts for web.

- Consistent branding, friendly tone, and clear microcopy on all
  platforms.

------------------------------------------------------------------------

## Narrative

A mid-sized company’s marketing team often struggles to agree on
campaign priorities in endless Slack threads. Needing a better way to
make collaborative decisions, the team lead searches the Slack App
Directory and finds the Ranked Choice Voting Platform. With a simple
“/rcv create” command, she launches a poll on which campaign to tackle
next—adding options and setting a deadline in seconds.

Team members are immediately pinged in their usual Slack channel. Each
is guided through a quick authentication—no new passwords, just Slack
login—and an intuitive drag-and-drop interface for ranking their
preferences. Within minutes, everyone submits their votes. As soon as
the poll closes, the live result animation reveals the winning campaign,
accompanied by transparent stats—no ambiguity, no drama.

The entire team feels heard and confident in the group’s decision.
Meanwhile, the company’s tech lead, intrigued by the seamless voting
logic, pulls the underlying NuGet package to reuse the robust RCV
tallying in an internal .NET application. The platform’s simplicity and
flexibility delight both business users and developers, amplifying its
reach across the organization.

------------------------------------------------------------------------

## Success Metrics

### User-Centric Metrics

- Number of polls created per integration (Slack, Teams, web)

- Percentage of invited users who participate in polls

- User satisfaction (in-app survey score ≥ 80%)

- Time from poll creation to first vote

### Business Metrics

- Growth in weekly active organizations using the integrations

- NuGet package downloads and reuses by other development teams

- Retention: repeat usage by organizations/groups

### Technical Metrics

- Platform uptime ≥ 99.9%

- Average API response time \< 500ms during peak load

- Error rate \< 1% for voting and poll result calculations

### Tracking Plan

- Poll creation events (with context: platform, options, poll size)

- Vote submission events

- Poll result views (by user/source)

- Integration installs/activations

- NuGet package download and versioning analytics

------------------------------------------------------------------------

## Technical Considerations

### Technical Needs

- Three front-end clients: Slack app (bot/interactive messages), Teams
  app (tab/bot), responsive web app.

- API server for poll orchestration, state management, and business
  logic.

- Dedicated .NET NuGet package for all RCV tallying/statistics,
  independently testable and documented.

- CosmosDB for persistent storage of poll, vote, and light user
  metadata.

### Integration Points

- Slack and Teams APIs for authentication, messaging, and real-time
  updates.

- In Phase 2, add support for standard social logins (Google, Apple,
  Microsoft) on the web for flexibility beyond Slack/Teams ecosystems.

- CosmosDB SDK for structured, scalable data access.

### Data Storage & Privacy

- Minimal user data: name, email, platform ID only for audit and
  participation.

- Data encrypted at rest and in transit.

- Compliance with organizational security standards and privacy
  best-practices.

### Scalability & Performance

- Designed for spikes in voting activity (e.g., company-wide polls).

- Distributed CosmosDB deployment for low-latency, cross-region access.

- NuGet package built for high concurrency and stateless usage.

### Potential Challenges

- Slack/Teams API changes or rate limits impacting real-time
  interaction.

- Ensuring poll integrity (no duplicate votes, secure tallying).

- Managing integration authentication consistency and edge-case errors.

- Maintaining modularity between app logic and the RCV core across
  environments.

------------------------------------------------------------------------

## Milestones & Sequencing

### Project Estimate

- Medium: 2–4 weeks for MVP; core team focused and lean.

### Team Size & Composition

- Small Team: 2–3 people

  - Product/Eng Lead (oversees product, backend, NuGet module)

  - Front-End/Integration Dev (web, Slack, Teams)

  - (Optional: Part-time designer or QA for usability testing)

### Suggested Phases

**Phase 1: RCV NuGet Module (1 week)**

- Deliverables: Well-tested, documented .NET library for RCV tallying,
  with core API and sample data.

- Dependencies: None.

**Phase 2: Basic Web App (1 week)**

- Deliverables: Responsive web UI; SSO with Slack and Teams; poll
  creation, voting, live results; CosmosDB integration; connects to RCV
  module.

  - Add support for standard social logins (Google, Apple, Microsoft) to
    enhance accessibility for non-Slack/Teams users.

- Dependencies: Completed NuGet package.

**Phase 3: Slack & Teams Integrations (1 week)**

- Deliverables: Slack bot/app (slash commands, voting UI,
  notifications); Teams bot/app; both integrate seamlessly with RCV
  backend and web.

- Dependencies: Basic Web App, CosmosDB.

**Phase 4: Testing, Feedback & Launch (up to 1 week)**

- Deliverables: Usability polish, robust error handling,
  analytics/events tracking, security review, refine based on user
  feedback.

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

------------------------------------------------------------------------
