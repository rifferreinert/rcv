# Repository Guide

## Project

RCV is a ranked-choice voting platform built with .NET 9 and React 19.

- `src/Rcv.Core`: reusable instant-runoff voting library
- `src/Rcv.Web.Api`: ASP.NET Core API using EF Core and SQL Server
- `src/rcv-web-ui`: React, TypeScript, and Vite SPA
- `tests`: core and API test projects

The Phase 2 web MVP includes Google/Microsoft OAuth, poll management, ranked
ballots, aggregate results, and combined API/SPA publishing.

## Run Locally

The API requires a SQL Server connection string, a JWT secret of at least 32
bytes, and Google/Microsoft OAuth client credentials. Configure them with
environment variables or .NET user secrets; see `.env.example`.

Run the API:

```bash
dotnet run --project src/Rcv.Web.Api
```

The API listens on `http://localhost:5041`; Swagger is available at that URL in
Development.

In another terminal, run the SPA:

```bash
npm ci --prefix src/rcv-web-ui
npm run dev --prefix src/rcv-web-ui
```

The SPA listens on `http://localhost:5173` and proxies `/api` to port 5041.

## Test

```bash
dotnet test
npm run lint --prefix src/rcv-web-ui
npm run typecheck --prefix src/rcv-web-ui
npm test --prefix src/rcv-web-ui
npm run test:e2e --prefix src/rcv-web-ui
```

Install the Playwright browser once with
`npm exec --prefix src/rcv-web-ui -- playwright install chromium`.

## Build

```bash
dotnet build
npm run build --prefix src/rcv-web-ui
```

Follow existing project patterns, validate inputs, preserve API/frontend
contracts, and add focused tests for behavior changes.
