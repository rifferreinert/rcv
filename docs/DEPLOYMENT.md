# Azure Deployment Guide

This guide deploys the Phase 2 web MVP as one ASP.NET Core 9 App Service containing both the API and compiled React application, backed by Azure SQL Database.

Actual Azure resource creation, DNS, and OAuth credentials are release operations. Keep all secrets in App Service settings or Key Vault; never commit them.

## Architecture

```text
Browser
  |
  | HTTPS (same origin)
  v
Azure App Service
  |- ASP.NET Core API (/api)
  |- React static application
  |- Health endpoints
  |
  v
Azure SQL Database
```

Using one origin keeps the HTTP-only authentication cookie, antiforgery cookie/header, OAuth redirects, and SPA requests on the same site.

## Prerequisites

- An Azure subscription
- Azure CLI authenticated with `az login`
- .NET 9 SDK
- Node.js version used by CI
- Google and Microsoft OAuth applications
- An Azure SQL logical server and database
- An Azure App Service running Linux with the .NET 9 runtime

## Required Configuration

Set nested ASP.NET Core keys with double underscores in App Service:

| App Service setting | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | Enables production middleware |
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string |
| `Authentication__Jwt__SecretKey` | Random signing secret, at least 32 bytes |
| `Authentication__Jwt__Issuer` | JWT issuer, normally the public application URL |
| `Authentication__Jwt__Audience` | JWT audience, normally the public application URL |
| `Authentication__Jwt__ExpirationDays` | Authentication lifetime |
| `Authentication__Google__ClientId` | Google OAuth client ID |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |
| `Authentication__Microsoft__ClientId` | Microsoft OAuth client ID |
| `Authentication__Microsoft__ClientSecret` | Microsoft OAuth client secret |
| `Authentication__OAuth__ReturnUrl` | Trusted frontend destination after login (`/dashboard` for same-origin production) |

Do not configure Slack, Teams, or Apple credentials for Phase 2.

Prefer managed identity for Azure SQL when the hosting environment supports it. Otherwise store the SQL password in Key Vault and expose it through a Key Vault reference.

## OAuth Registration

Register these production callback URLs, replacing the example host:

```text
https://rcv.example.com/api/auth/signin/google
https://rcv.example.com/api/auth/signin/microsoft
```

For local development through the Vite proxy, register:

```text
http://localhost:5041/api/auth/signin/google
http://localhost:5041/api/auth/signin/microsoft
```

The proxy rewrites the request origin to the API origin so the generated callback matches these URLs. Redirect destinations accepted by the API must remain local application paths; do not add arbitrary external return URLs.
Development configuration redirects the completed login to `http://localhost:5173/dashboard`; production uses the same-origin `/dashboard` path.

## Build the Deployment Artifact

From the repository root:

```bash
dotnet restore
npm ci --prefix src/rcv-web-ui
npm run lint --prefix src/rcv-web-ui
npm run test --prefix src/rcv-web-ui
npm run build --prefix src/rcv-web-ui
dotnet test -c Release --no-restore
dotnet publish src/Rcv.Web.Api/Rcv.Web.Api.csproj \
  -c Release \
  --no-restore \
  -o artifacts/app
```

The publish output must contain the compiled frontend under `wwwroot`.

## Database Migration

Do not run migrations automatically during application startup. Generate and review an idempotent script:

```bash
dotnet ef migrations script \
  --project src/Rcv.Web.Api \
  --startup-project src/Rcv.Web.Api \
  --idempotent \
  --output artifacts/migrate.sql
```

Apply `artifacts/migrate.sql` using a deployment identity with schema-change permissions. The runtime application identity should use only the permissions needed by the application.

Back up the database or verify point-in-time restore before applying a production migration.

## Deploy

Zip the publish directory and deploy it:

```bash
cd artifacts/app
zip -r ../app.zip .
cd ../..

az webapp deploy \
  --resource-group <resource-group> \
  --name <app-name> \
  --src-path artifacts/app.zip \
  --type zip
```

Use a deployment slot for production releases:

1. Deploy the artifact to the staging slot.
2. Apply the reviewed database migration.
3. Verify liveness, readiness, login redirects, poll creation, voting, and results.
4. Swap staging into production.

## Verification

Check:

```text
GET /health
GET /ready
GET /
GET /api/auth/me
```

Expected behavior:

- `/health` returns success without requiring the database.
- `/ready` succeeds only when required dependencies are available.
- `/` serves the React application.
- `/api/auth/me` returns 401 before login.
- Google and Microsoft login return to the same application origin.
- State-changing calls without a valid antiforgery token are rejected.

Review App Service logs for startup failures without logging cookies, JWTs, OAuth tokens, or ranked ballots.

## Rollback

1. Swap back to the previous App Service slot or redeploy the previous artifact.
2. Prefer forward-compatible database migrations so the previous application remains usable.
3. If a schema rollback is unavoidable, restore Azure SQL to a new database and repoint the application after validating data.
4. Re-run health and critical user-flow checks.
