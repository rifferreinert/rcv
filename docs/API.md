# API Overview

The API is served under `/api` from the same origin as the React application. Swagger UI is available at the application root in Development.

Authentication uses an HTTP-only JWT cookie. The SPA obtains an antiforgery token before sending state-changing requests.

## Authentication

| Method | Route | Access |
|---|---|---|
| `GET` | `/api/auth/login/{provider}` | Public; Google or Microsoft |
| `GET` | `/api/auth/callback/{provider}` | OAuth callback |
| `GET` | `/api/auth/me` | Authenticated |
| `GET` | `/api/auth/csrf` | Public token bootstrap |
| `GET` | `/api/auth/dev-login` | Development only; fixed local test user (404 in production) |
| `POST` | `/api/auth/logout` | Authenticated and antiforgery-protected |

## Polls

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/polls` | Authenticated and antiforgery-protected |
| `GET` | `/api/polls` | Authenticated; current user's unlisted polls |
| `GET` | `/api/polls/{id}` | Public by direct link |
| `PUT` | `/api/polls/{id}` | Creator and antiforgery-protected |
| `DELETE` | `/api/polls/{id}` | Creator and antiforgery-protected |
| `POST` | `/api/polls/{id}/close` | Creator and antiforgery-protected |

The list endpoint supports `status`, `page`, and `pageSize`. Poll updates are rejected after voting begins. `ClosesAt` is enforced as an effective closure deadline; updates use `removeClosesAt: true` to clear an existing deadline.

## Votes

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/polls/{pollId}/votes` | Authenticated and antiforgery-protected |
| `GET` | `/api/polls/{pollId}/votes/me` | Authenticated |
| `GET` | `/api/polls/{pollId}/votes/count` | Public by direct link |

Posting a vote creates or revises the authenticated user's ballot while the poll remains open. Individual ballots and voter identities are never exposed publicly.

## Results

| Method | Route | Access |
|---|---|---|
| `GET` | `/api/polls/{pollId}/results` | Visibility depends on poll state |

The creator may always view results. Other users may view results while voting only when live results are enabled. Final aggregate results are public after closure.

Result states:

- `NoVotes`: zero participation, no winner, and no rounds
- `InProgress`: a live snapshot of an active poll
- `Final`: results from a closed poll

Errors use RFC 7807 `application/problem+json`.
