# Phase 2: Web App Completion Status

> **Last updated**: 2026-08-08
> **Status**: Repository implementation complete. This is the authoritative Phase 2 tracker.

## Completion Snapshot

| Area | Status |
|---|---|
| Core library | Complete; 29 tests and 95.34% line coverage |
| Web API | Complete; 139 tests and 96.03% application line coverage |
| React application | Complete; 9 component tests and 76.28% line coverage |
| Browser smoke coverage | Complete; 2 Playwright tests |
| Database model | Complete; migrations match the EF model and an idempotent script is generated in CI |
| Delivery | Complete; CI builds and tests both applications and publishes one combined App Service artifact |
| External release activities | Azure provisioning, production migration, secrets, and real OAuth-provider verification remain |

API coverage excludes generated EF migrations and the separately tested `Rcv.Core` assembly. Frontend coverage is reported but does not have a numeric CI threshold.

## 1. Application Foundation - COMPLETE

- [x] ASP.NET Core 9 API references `Rcv.Core`.
- [x] React, TypeScript, Vite, Tailwind CSS, React Router, React Query, Axios, dnd-kit, and Recharts are configured.
- [x] Development uses Vite at `http://localhost:5173` with same-browser `/api` proxying to `http://localhost:5041`.
- [x] Production serves the compiled SPA and API from one ASP.NET Core application.
- [x] Root and frontend READMEs document development and validation commands.

## 2. Persistence and Poll Lifecycle - COMPLETE

- [x] EF Core entities, relationships, indexes, SQL Server provider, and JSON-ranked ballots are implemented.
- [x] `IsVotingPublic` was removed from the model and contract by the `RemoveIsVotingPublic` migration.
- [x] Poll status uses the typed `PollStatus` enum.
- [x] A centralized lifecycle service lazily persists deadline closure.
- [x] Creator dashboard closure, filtering, counting, and pagination are database-side.
- [x] Vote writes serialize against closure and concurrent upserts.
- [x] Migrations match the current model.
- [x] CI generates an idempotent migration script.

## 3. Authentication and API Security - COMPLETE

- [x] Google and Microsoft OAuth providers are configured.
- [x] Authentication uses an HTTP-only JWT cookie and a temporary external OAuth cookie.
- [x] OAuth return destinations reject untrusted external origins.
- [x] Local OAuth callbacks and the Vite return destination are configured consistently.
- [x] `GET /api/auth/me` and antiforgery-protected `POST /api/auth/logout` are implemented.
- [x] `GET /api/auth/csrf` supplies the SPA antiforgery token.
- [x] State-changing controller actions require antiforgery validation.
- [x] Authentication and voting endpoints have rate limits.
- [x] RFC 7807 ProblemDetails, structured Serilog logging, production HTTPS/HSTS, forwarded headers, CORS restrictions, and secure cookie defaults are configured.
- [x] `/health` provides liveness and `/ready` checks database readiness.
- [x] JWT signing-key configuration is validated at startup.

## 4. Poll Management - COMPLETE

- [x] Authenticated users can create polls with 2-50 unique options.
- [x] Polls are unlisted and accessible publicly only by direct link.
- [x] `GET /api/polls` lists only the authenticated creator's polls.
- [x] Dashboard filtering supports active/closed status and bounded pagination.
- [x] Public responses expose creator ID/display name but not email.
- [x] Creators can edit polls before voting begins, including clearing a deadline.
- [x] Creators can close or soft-delete polls.
- [x] Scheduled deadlines are enforced on reads and writes without a background scheduler.
- [x] Input lengths, option uniqueness, deadlines, status, and pagination are validated.

## 5. Voting - COMPLETE

- [x] Authenticated users can submit partial or complete ranked ballots.
- [x] Ranked option IDs must be non-empty, unique, and belong to the poll.
- [x] One ballot per user/poll is enforced by the database.
- [x] Voters can revise their ballot while the poll remains effectively active.
- [x] `VoteStatusResponse` reports vote presence, change permission, timestamps, and current rankings.
- [x] Manual and scheduled closure reject new or revised votes.
- [x] Individual ballots and voter identities are never exposed publicly.

## 6. Aggregate Results - COMPLETE

- [x] `GET /api/polls/{pollId}/results` serves live and final snapshots.
- [x] The creator can always view results.
- [x] Other users can view live results only when `IsResultsPublic` is enabled.
- [x] Final aggregate results are public after closure.
- [x] `NoVotes`, `InProgress`, and `Final` states are explicit.
- [x] Results include winner/tie details, ordered rounds, eliminations, final totals, and participation.
- [x] Persisted options and ballots are mapped to `Rcv.Core` for calculation.
- [x] Tie-breaking uses a deterministic seed derived from the poll GUID.
- [x] Only immutable final results are cached; live snapshots are recalculated.
- [x] No response exposes individual ranked ballots.

## 7. React Web Application - COMPLETE

- [x] Responsive application shell, navigation, error boundary, loading/error states, and not-found page.
- [x] Home, login, dashboard, create/edit poll, poll detail, and results routes.
- [x] Auth provider and protected routes backed by `GET /api/auth/me`.
- [x] Relative Axios API client with credentials, antiforgery bootstrap, typed errors, and 401 handling.
- [x] Creator dashboard with status filtering and pagination.
- [x] Poll forms with Zod validation, dynamic options, deadline controls, and live-results setting.
- [x] Direct-link poll page with share action and creator-only edit/close/delete controls.
- [x] Accessible dnd-kit ranking with keyboard sensors, touch support, visible ranks, and move buttons.
- [x] Existing ballots load for revision while changes remain allowed.
- [x] Results page handles no-vote, live, winner, and tie states.
- [x] Recharts visualization has an accessible tabular alternative.
- [x] Live results poll modestly and stop once final.

## 8. Automated Quality and Delivery - COMPLETE

- [x] Core tests enforce at least 80% line coverage.
- [x] API tests enforce at least 80% application line coverage.
- [x] Frontend lint, type-check, component tests, coverage report, and production build run in CI.
- [x] Playwright covers the application shell, protected routing, and keyboard ranking behavior.
- [x] EF migrations are validated and scripted.
- [x] `dotnet publish` fails if the SPA has not been built.
- [x] The publish output contains the compiled SPA in `wwwroot`.
- [x] Client-side routes fall back to `index.html` without capturing API, health, or Swagger routes.
- [x] CI uploads backend reports, frontend coverage, the migration script, the NuGet package, and the combined web artifact.

## 9. Documentation - COMPLETE

- [x] API overview documents authentication, polls, voting, results, and visibility.
- [x] Deployment guide documents Azure SQL, App Service, configuration, OAuth callbacks, migration, health, rollback, and secrets.
- [x] User guide covers poll creation, voting, revisions, closure, and result interpretation.
- [x] Safe example environment configuration contains placeholders only.
- [x] README and Phase 2 status documents match the delivered repository.

## External Release Checklist

These activities require infrastructure or credentials and are intentionally not marked as repository implementation:

- [ ] Provision Azure SQL and Azure App Service.
- [ ] Store production secrets in App Service/Key Vault.
- [ ] Register production Google and Microsoft callback URLs.
- [ ] Apply the generated migration script to the production database.
- [ ] Verify real Google and Microsoft login/logout flows.
- [ ] Verify `/health` and `/ready` in the deployed environment.
- [ ] Complete manual cross-browser, screen-reader, responsive, and load checks.
- [ ] Exercise rollback procedures against the deployed environment.

## Deferred Beyond Phase 2

- Slack, Teams, and Apple identity providers
- Public poll directory or discovery feed
- Public individual ballots or voter identities
- Refresh tokens
- Background deadline scheduler
- WebSocket result updates
- Production-only test authentication endpoints
