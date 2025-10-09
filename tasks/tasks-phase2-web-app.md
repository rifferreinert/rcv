# Phase 2: Web App Implementation Tasks

> **Reference**: See `phase2-web-app.md` for architecture, API specification, and data models

## Task Overview

- **Total Tasks**: 14 major sections, ~90 subtasks
- **Timeline**: 7 days
- **Sequence**: Backend (days 1-4) → Frontend (days 5-6) → Testing & Deployment (day 7+)

## 1.0 Project Setup and Infrastructure ✅

- [x] 1.1 Create ASP.NET Core Web API project (`Rcv.Web.Api`)
  - [x] 1.1.1 Run `dotnet new webapi` with .NET 9.0 target
  - [x] 1.1.2 Add reference to `Rcv.Core` NuGet package
  - [x] 1.1.3 Configure `appsettings.json` with connection strings and auth providers
  - [x] 1.1.4 Set up Swagger/OpenAPI for API documentation
- [x] 1.2 Create React frontend project (`rcv-web-ui`)
  - [x] 1.2.1 Initialize with `npm create vite@latest` (React + TypeScript template)
  - [x] 1.2.2 Install dependencies (React Router, Axios, Tailwind CSS, @dnd-kit, Recharts)
  - [x] 1.2.3 Configure Tailwind CSS with custom theme (responsive breakpoints)
  - [x] 1.2.4 Set up ESLint and Prettier
- [x] 1.3 Update solution file to include new projects
- [x] 1.4 Create test project (`Rcv.Web.Api.Tests`) with xUnit, Moq, FluentAssertions
- [x] 1.5 Configure CORS on backend to allow frontend origin (development and production URLs)

## 2.0 Database Design and Entity Framework Setup ✅

- [x] 2.1 Install EF Core packages (SqlServer, Tools, Design)
- [x] 2.2 Create entity classes with data annotations
  - [x] 2.2.1 `User` entity with provider mapping
  - [x] 2.2.2 `Poll` entity with creator relationship
  - [x] 2.2.3 `PollOption` entity with poll relationship
  - [x] 2.2.4 `Vote` entity with `List<Guid>` property for ranked choices (mapped to JSON column)
- [x] 2.3 Create `RcvDbContext` with DbSets and relationships
  - [x] 2.3.1 Configure entity relationships (one-to-many, unique constraints)
  - [x] 2.3.2 Add indexes for common queries
  - [x] 2.3.3 Configure JSON column type for `Vote.RankedChoices` using `HasColumnType("json")`
- [x] 2.4 Generate initial migration (`dotnet ef migrations add InitialCreate`)
- [x] 2.5 Apply migration to Azure SQL Database (skipped - no DB instance yet)
- [x] 2.6 Seed sample data for development (skipped for MVP)

## 3.0 Authentication & Authorization

- [ ] 3.1 Configure authentication services in `Program.cs`
  - [ ] 3.1.1 Add JWT Bearer authentication scheme
  - [ ] 3.1.2 Configure OAuth2 providers (Slack, Teams, Google, Apple, Microsoft)
  - [ ] 3.1.3 Set up cookie-based sessions for web flows (optional hybrid approach)
- [ ] 3.2 Create `AuthController` with login/logout endpoints
  - [ ] 3.2.1 `GET /api/auth/login/{provider}` - Initiate OAuth flow
  - [ ] 3.2.2 `GET /api/auth/callback/{provider}` - Handle OAuth callback
  - [ ] 3.2.3 `POST /api/auth/logout` - Clear session/token
  - [ ] 3.2.4 `GET /api/auth/me` - Get current user info
- [ ] 3.3 Create `AuthService` for user creation/lookup
  - [ ] 3.3.1 Implement `GetOrCreateUserAsync(externalId, provider, profile)`
  - [ ] 3.3.2 Implement JWT token generation with user claims
  - [ ] 3.3.3 Update `LastLoginAt` timestamp on authentication
- [ ] 3.4 Add authorization policies (poll creator, authenticated user)
- [ ] 3.5 Write unit tests for authentication flows and token generation

## 4.0 Poll Management API

- [ ] 4.1 Create `PollService` with business logic
  - [ ] 4.1.1 `CreatePollAsync(userId, title, description, options, settings)` - Returns Poll entity
  - [ ] 4.1.2 `GetPollByIdAsync(pollId)` - Returns Poll with options
  - [ ] 4.1.3 `GetPollsByCreatorAsync(userId)` - Returns creator's polls
  - [ ] 4.1.4 `GetActivePollsAsync()` - Returns all active polls (for discovery)
  - [ ] 4.1.5 `ClosePollAsync(pollId, userId)` - Validates creator, sets `ClosedAt`
  - [ ] 4.1.6 `DeletePollAsync(pollId, userId)` - Soft delete (sets Status = 'Deleted')
  - [ ] 4.1.7 `UpdatePollAsync(pollId, userId, updates)` - Allow editing before votes cast (optional)
- [ ] 4.2 Create DTOs for poll operations
  - [ ] 4.2.1 `CreatePollRequest` (title, description, options[], closesAt?, isResultsPublic, isVotingPublic)
  - [ ] 4.2.2 `PollResponse` (id, title, description, creator, options, stats, status, settings)
  - [ ] 4.2.3 `PollListResponse` (pagination metadata, poll summaries)
- [ ] 4.3 Create `PollsController` with CRUD endpoints
  - [ ] 4.3.1 `POST /api/polls` - Create poll (requires auth)
  - [ ] 4.3.2 `GET /api/polls/{id}` - Get poll details
  - [ ] 4.3.3 `GET /api/polls` - List polls (with filters: creator, status, pagination)
  - [ ] 4.3.4 `PUT /api/polls/{id}` - Update poll (creator only)
  - [ ] 4.3.5 `DELETE /api/polls/{id}` - Delete poll (creator only)
  - [ ] 4.3.6 `POST /api/polls/{id}/close` - Close poll early (creator only)
- [ ] 4.4 Add validation rules (minimum 2 options, required fields)
- [ ] 4.5 Write unit tests for `PollService` and integration tests for `PollsController`

## 5.0 Voting API

- [ ] 5.1 Create `VotingService` with business logic
  - [ ] 5.1.1 `CastVoteAsync(pollId, userId, rankedOptionIds)` - Validates and saves vote
  - [ ] 5.1.2 `GetUserVoteAsync(pollId, userId)` - Returns user's vote if exists
  - [ ] 5.1.3 `GetVoteCountAsync(pollId)` - Returns participation count
  - [ ] 5.1.4 Validate poll is open (not closed)
  - [ ] 5.1.5 Validate all option IDs belong to poll
  - [ ] 5.1.6 Handle duplicate vote attempts (update existing or return error)
- [ ] 5.2 Create DTOs for voting operations
  - [ ] 5.2.1 `CastVoteRequest` (rankedOptionIds[])
  - [ ] 5.2.2 `VoteResponse` (voteId, pollId, castAt, updatedAt)
  - [ ] 5.2.3 `VoteStatusResponse` (hasVoted, canChange, votedAt)
- [ ] 5.3 Create `VotesController` with voting endpoints
  - [ ] 5.3.1 `POST /api/polls/{pollId}/votes` - Cast/update vote (requires auth)
  - [ ] 5.3.2 `GET /api/polls/{pollId}/votes/me` - Get current user's vote
  - [ ] 5.3.3 `GET /api/polls/{pollId}/votes/count` - Get participation stats
- [ ] 5.4 Add validation for ranked choices (no duplicates, valid option IDs)
- [ ] 5.5 Write unit tests for `VotingService` and integration tests for `VotesController`

## 6.0 Results Calculation and Visualization API

- [ ] 6.1 Create `ResultsService` to orchestrate Rcv.Core integration
  - [ ] 6.1.1 `CalculateResultsAsync(pollId)` - Fetches votes, maps to `RankedBallot[]`, calls `RankedChoicePoll.CalculateResult()`
  - [ ] 6.1.2 Map `RcvResult` domain model to `ResultResponse` DTO
  - [ ] 6.1.3 Include round-by-round data for visualization
  - [ ] 6.1.4 Implement caching (in-memory for MVP; invalidate on new vote)
  - [ ] 6.1.5 Handle polls with no votes (return appropriate message)
- [ ] 6.2 Create `ResultResponse` DTO
  - [ ] 6.2.1 Include winner (or tie status), tied options
  - [ ] 6.2.2 Include rounds[] with vote counts and eliminated candidates
  - [ ] 6.2.3 Include final vote totals by option
  - [ ] 6.2.4 Include participation stats (total votes, turnout if applicable)
- [ ] 6.3 Create `ResultsController` with results endpoints
  - [ ] 6.3.1 `GET /api/polls/{pollId}/results` - Get results (enforce visibility rules)
  - [ ] 6.3.2 `GET /api/polls/{pollId}/results/live` - Real-time results (if enabled)
- [ ] 6.4 Add authorization checks (creator always sees results; others based on poll settings)
- [ ] 6.5 Write unit tests for `ResultsService` with various vote scenarios (tie, majority, elimination rounds)

## 7.0 Frontend Core Infrastructure

- [ ] 7.1 Set up routing with React Router
  - [ ] 7.1.1 Configure routes: `/`, `/login`, `/dashboard`, `/polls/new`, `/polls/:id`, `/polls/:id/results`
  - [ ] 7.1.2 Create protected route wrapper (requires authentication)
  - [ ] 7.1.3 Create `Layout` component with navigation header
- [ ] 7.2 Create authentication context and hooks
  - [ ] 7.2.1 `AuthContext` with user state and login/logout functions
  - [ ] 7.2.2 `useAuth()` hook to consume auth context
  - [ ] 7.2.3 Implement token storage and automatic inclusion in API requests
  - [ ] 7.2.4 Handle token expiration and refresh (if using refresh tokens)
- [ ] 7.3 Set up API client
  - [ ] 7.3.1 Create Axios instance with base URL and interceptors
  - [ ] 7.3.2 Add request interceptor to include JWT token in headers
  - [ ] 7.3.3 Add response interceptor for error handling (401 → redirect to login)
- [ ] 7.4 Create TypeScript types mirroring backend DTOs
  - [ ] 7.4.1 `Poll`, `PollOption`, `Vote`, `Result`, `RoundSummary` types
  - [ ] 7.4.2 Request/response types for API calls
- [ ] 7.5 Set up React Query for server state management (queries and mutations)

## 8.0 Frontend Authentication UI

- [ ] 8.1 Create `Login` page with SSO provider buttons
  - [ ] 8.1.1 Display buttons for each provider (Slack, Teams, Google, Apple, Microsoft)
  - [ ] 8.1.2 Style buttons with provider branding (official icons and colors)
  - [ ] 8.1.3 Initiate OAuth flow by redirecting to `/api/auth/login/{provider}`
- [ ] 8.2 Handle OAuth callback and token storage
  - [ ] 8.2.1 Parse JWT from callback URL or cookie
  - [ ] 8.2.2 Store token in localStorage (or secure cookie)
  - [ ] 8.2.3 Redirect to dashboard on successful login
- [ ] 8.3 Create `AuthButton` component for header (shows user name, logout button when authenticated)
- [ ] 8.4 Implement logout functionality (clear token, redirect to home)

## 9.0 Frontend Poll Management UI

- [ ] 9.1 Create `Home` page (landing page with CTA and product overview)
- [ ] 9.2 Create `Dashboard` page showing user's polls
  - [ ] 9.2.1 Fetch polls via `GET /api/polls?creatorId={userId}`
  - [ ] 9.2.2 Display poll cards with title, status, vote count, actions
  - [ ] 9.2.3 Include "Create Poll" button navigating to `/polls/new`
  - [ ] 9.2.4 Filter/sort options (active/closed, date created)
- [ ] 9.3 Create `CreatePoll` page with form
  - [ ] 9.3.1 Form fields: title, description, options (dynamic list with add/remove)
  - [ ] 9.3.2 Settings: close date (optional), results visibility, vote visibility
  - [ ] 9.3.3 Client-side validation (minimum 2 options, required title)
  - [ ] 9.3.4 Submit form via `POST /api/polls`, redirect to poll detail on success
- [ ] 9.4 Create `PollDetail` page (view poll, cast vote, or see results)
  - [ ] 9.4.1 Fetch poll data via `GET /api/polls/{id}`
  - [ ] 9.4.2 Show poll metadata (title, description, creator, deadline, status)
  - [ ] 9.4.3 Conditionally render: voting interface (if open and user hasn't voted), "Already voted" message, or results
  - [ ] 9.4.4 Poll creator actions: "Close Poll", "Delete Poll" buttons (with confirmation modals)
- [ ] 9.5 Create `PollCard` component for dashboard list items

## 10.0 Frontend Voting Interface

- [ ] 10.1 Create `VotingInterface` component with drag-and-drop ranking
  - [ ] 10.1.1 Display all poll options as draggable cards
  - [ ] 10.1.2 Use @dnd-kit for drag-and-drop reordering
  - [ ] 10.1.3 Show visual feedback (highlighted drop zones, numbered ranks)
  - [ ] 10.1.4 Allow partial ballots (users can rank subset of options)
  - [ ] 10.1.5 Validate no duplicates, clear error messages
- [ ] 10.2 Add "Submit Vote" button with loading state
- [ ] 10.3 Submit vote via `POST /api/polls/{pollId}/votes`
- [ ] 10.4 Show success confirmation and automatically navigate to results (if visible)
- [ ] 10.5 Handle errors gracefully (poll closed, duplicate vote, invalid options)
- [ ] 10.6 Make interface accessible (keyboard navigation, screen reader support)
- [ ] 10.7 Ensure responsive design (touch-friendly on mobile)

## 11.0 Frontend Results Visualization

- [ ] 11.1 Create `Results` page fetching results via `GET /api/polls/{pollId}/results`
- [ ] 11.2 Display winner or tie announcement prominently
- [ ] 11.3 Create `ResultsChart` component for round-by-round visualization
  - [ ] 11.3.1 Use Recharts bar chart showing vote distribution per round
  - [ ] 11.3.2 Animate transitions between rounds (optional, enhance UX)
  - [ ] 11.3.3 Highlight eliminated candidate in each round
  - [ ] 11.3.4 Show final round with winner
- [ ] 11.4 Display participation statistics (total votes, percentage if applicable)
- [ ] 11.5 Add option to view detailed round data (table format)
- [ ] 11.6 Implement live results (polling or WebSocket) if `isResultsPublic = true`
  - [ ] 11.6.1 Use polling (fetch every N seconds) for MVP
  - [ ] 11.6.2 Add visual indicator when results are live/updating
- [ ] 11.7 Ensure responsive design for charts (mobile-friendly, readable on small screens)

## 12.0 Testing and Quality Assurance

- [ ] 12.1 Write unit tests for backend services
  - [ ] 12.1.1 `PollService` tests (create, close, delete, authorization)
  - [ ] 12.1.2 `VotingService` tests (cast vote, duplicate handling, validation)
  - [ ] 12.1.3 `ResultsService` tests (integration with Rcv.Core, caching)
  - [ ] 12.1.4 `AuthService` tests (user creation, token generation)
- [ ] 12.2 Write integration tests for API controllers
  - [ ] 12.2.1 `PollsController` (CRUD operations, authorization)
  - [ ] 12.2.2 `VotesController` (voting flow, edge cases)
  - [ ] 12.2.3 `ResultsController` (results visibility rules)
- [ ] 12.3 Manual testing checklist
  - [ ] 12.3.1 Authentication flow with each SSO provider
  - [ ] 12.3.2 Poll creation and management (create, close, delete)
  - [ ] 12.3.3 Voting flow (cast vote, update vote, validation errors)
  - [ ] 12.3.4 Results visualization (winner, tie, round-by-round)
  - [ ] 12.3.5 Authorization (creator-only actions, results visibility)
  - [ ] 12.3.6 Responsive design (mobile, tablet, desktop)
  - [ ] 12.3.7 Accessibility (keyboard navigation, screen reader)
- [ ] 12.4 Cross-browser testing (Chrome, Firefox, Safari, Edge)
- [ ] 12.5 Performance testing (database query optimization, caching effectiveness)

## 13.0 Documentation and Deployment Preparation

- [ ] 13.1 Write API documentation (Swagger/OpenAPI annotations on controllers)
- [ ] 13.2 Create deployment guide (`docs/DEPLOYMENT.md`)
  - [ ] 13.2.1 Azure SQL Database setup instructions
  - [ ] 13.2.2 Azure App Service configuration (backend)
  - [ ] 13.2.3 Static web app hosting for frontend (Azure, Vercel, or Netlify)
  - [ ] 13.2.4 OAuth provider registration and configuration
  - [ ] 13.2.5 Environment variables and secrets management
- [ ] 13.3 Update root `README.md` with Phase 2 overview and quickstart
- [ ] 13.4 Create `.env.example` files for both backend and frontend
- [ ] 13.5 Update CI/CD pipeline (`.github/workflows/ci.yml`)
  - [ ] 13.5.1 Add build steps for web API
  - [ ] 13.5.2 Add build steps for React frontend
  - [ ] 13.5.3 Run all tests (Rcv.Core + Rcv.Web.Api)
  - [ ] 13.5.4 Generate deployment artifacts
- [ ] 13.6 Write user-facing documentation
  - [ ] 13.6.1 How to create a poll
  - [ ] 13.6.2 How to vote in a poll
  - [ ] 13.6.3 Understanding results and statistics

## 14.0 Security Hardening and Polish

- [ ] 14.1 Add rate limiting to authentication and voting endpoints
- [ ] 14.2 Implement CSRF protection for state-changing operations
- [ ] 14.3 Validate and sanitize all user inputs on backend
- [ ] 14.4 Add comprehensive error handling and logging (structured logging with Serilog)
- [ ] 14.5 Configure HTTPS redirect and HSTS headers
- [ ] 14.6 Review and test CORS configuration (restrict to known origins)
- [ ] 14.7 Add health check endpoints (`/health`, `/ready`)
- [ ] 14.8 Implement graceful error messages on frontend (user-friendly, actionable)
- [ ] 14.9 Add loading states and skeleton screens for better perceived performance
- [ ] 14.10 Final accessibility audit (WCAG 2.1 Level AA)

## Recommended Development Sequence

### Days 1-2: Backend Foundation
**Tasks**: 1.0, 2.0, 3.0

- Set up projects, database, authentication
- Get basic API endpoints working with auth
- Test with Postman/Swagger

### Days 3-4: Core API Functionality
**Tasks**: 4.0, 5.0, 6.0

- Implement poll management, voting, results
- Write comprehensive tests
- Validate integration with Rcv.Core

### Days 5-6: Frontend Development
**Tasks**: 7.0, 8.0, 9.0, 10.0, 11.0

- Build UI components and pages
- Connect to backend API
- Polish UX and responsive design

### Day 7+: Testing and Deployment
**Tasks**: 12.0, 13.0, 14.0

- QA and bug fixes
- Documentation
- Deploy to Azure

## Success Criteria

- [ ] Users can create polls with multiple options via web UI
- [ ] Users can authenticate with at least 3 SSO providers (Slack, Google, Microsoft)
- [ ] Users can vote using intuitive drag-and-drop interface
- [ ] Users can view live or final results with round-by-round breakdown
- [ ] Poll creators can manage their polls (close, delete)
- [ ] App is responsive and accessible (mobile-friendly, WCAG AA)
- [ ] Backend API is fully tested (≥80% coverage)
- [ ] App is deployed to Azure with production-ready configuration
