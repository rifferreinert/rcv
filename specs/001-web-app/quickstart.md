# Quickstart Guide: RCV Web Application

**Feature**: 001-web-app
**Date**: 2025-10-26
**Purpose**: Local development setup instructions

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 9 SDK** (version 9.0.100 or later)
  ```bash
  dotnet --version  # Should output 9.0.x
  ```

- **Azure SQL Database** (or SQL Server LocalDB for local development)
  - **Option A**: Azure SQL Database (requires Azure subscription)
  - **Option B**: SQL Server LocalDB (installed with Visual Studio or standalone)
  - **Option C**: SQL Server in Docker (cross-platform)

- **Azure Account** (for OAuth app registrations and database)
  - Free tier is sufficient for development

- **Git** (for version control)
  ```bash
  git --version
  ```

- **IDE** (recommended):
  - Visual Studio 2022 (17.8 or later) with ASP.NET and web development workload
  - OR Visual Studio Code with C# Dev Kit extension
  - OR JetBrains Rider

---

## Step 1: Clone the Repository

```bash
git clone https://github.com/rifferreinert/rcv.git
cd rcv
git checkout 001-web-app
```

---

## Step 2: Database Setup

### Option A: Azure SQL Database

1. **Create Azure SQL Database**:
   ```bash
   # Login to Azure
   az login

   # Create resource group
   az group create --name rcv-dev --location eastus

   # Create SQL server
   az sql server create \
     --name rcv-sql-server-dev \
     --resource-group rcv-dev \
     --location eastus \
     --admin-user rcvadmin \
     --admin-password 'YourSecurePassword123!'

   # Create database (serverless tier for dev)
   az sql db create \
     --name rcv-dev-db \
     --resource-group rcv-dev \
     --server rcv-sql-server-dev \
     --edition GeneralPurpose \
     --family Gen5 \
     --capacity 1 \
     --compute-model Serverless \
     --auto-pause-delay 60

   # Allow local IP to connect
   az sql server firewall-rule create \
     --resource-group rcv-dev \
     --server rcv-sql-server-dev \
     --name AllowMyIP \
     --start-ip-address $(curl -s ifconfig.me) \
     --end-ip-address $(curl -s ifconfig.me)
   ```

2. **Connection String**:
   ```
   Server=tcp:rcv-sql-server-dev.database.windows.net,1433;
   Database=rcv-dev-db;
   User ID=rcvadmin;
   Password=YourSecurePassword123!;
   Encrypt=True;
   TrustServerCertificate=False;
   Connection Timeout=30;
   ```

### Option B: SQL Server LocalDB (Windows)

1. **Install LocalDB** (comes with Visual Studio or standalone):
   ```bash
   sqllocaldb create rcv-dev
   sqllocaldb start rcv-dev
   sqllocaldb info rcv-dev
   ```

2. **Connection String**:
   ```
   Server=(localdb)\\rcv-dev;
   Database=RcvDb;
   Integrated Security=True;
   ```

### Option C: SQL Server in Docker (Cross-platform)

1. **Run SQL Server container**:
   ```bash
   docker run -e "ACCEPT_EULA=Y" \
     -e "MSSQL_SA_PASSWORD=YourSecurePassword123!" \
     -p 1433:1433 \
     --name rcv-sql \
     -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. **Connection String**:
   ```
   Server=localhost,1433;
   Database=RcvDb;
   User ID=sa;
   Password=YourSecurePassword123!;
   Encrypt=False;
   ```

---

## Step 3: OAuth Provider Configuration

You need to register OAuth applications with each provider to get client IDs and secrets.

### 3.1 Google OAuth

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project: "RCV Web App Dev"
3. Enable Google+ API
4. Go to **Credentials** → **Create Credentials** → **OAuth client ID**
5. Application type: **Web application**
6. Authorized redirect URIs:
   - `https://localhost:5001/api/auth/callback/google`
   - `http://localhost:5000/api/auth/callback/google`
7. Copy **Client ID** and **Client secret**

### 3.2 Microsoft Account OAuth

1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to **Azure Active Directory** → **App registrations** → **New registration**
3. Name: "RCV Web App Dev"
4. Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
5. Redirect URI (Web): `https://localhost:5001/api/auth/callback/microsoft`
6. Go to **Certificates & secrets** → **New client secret**
7. Copy **Application (client) ID** and **Client secret value**

### 3.3 Slack OAuth

1. Go to [Slack API](https://api.slack.com/apps)
2. Click **Create New App** → **From scratch**
3. App name: "RCV Web App Dev"
4. Pick a workspace for development
5. Go to **OAuth & Permissions**
6. Add redirect URL: `https://localhost:5001/api/auth/callback/slack`
7. **Scopes** → **User Token Scopes**: Add `identity.basic`, `identity.email`
8. Copy **Client ID** and **Client Secret**

### 3.4 Apple Sign In

1. Go to [Apple Developer Portal](https://developer.apple.com/)
2. **Certificates, Identifiers & Profiles** → **Identifiers** → **App IDs** → **+**
3. Register **Services ID**
4. Identifier: `com.rcv.web.dev`
5. Configure **Sign In with Apple**
6. Return URLs: `https://localhost:5001/api/auth/callback/apple`
7. Download private key, copy **Team ID**, **Client ID (Services ID)**, **Key ID**

### 3.5 Microsoft Teams OAuth

Uses Microsoft OAuth (same as 3.2) with Teams-specific scope:
- Scope: `User.Read`, `offline_access`

---

## Step 4: Configure Application Secrets

**NEVER** commit secrets to source control. Use User Secrets for local development.

### 4.1 Initialize User Secrets

```bash
cd src/Rcv.Web.Api
dotnet user-secrets init
```

### 4.2 Set Database Connection String

```bash
# For Azure SQL
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:rcv-sql-server-dev.database.windows.net,1433;Database=rcv-dev-db;User ID=rcvadmin;Password=YourSecurePassword123!;Encrypt=True;"

# For LocalDB
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\rcv-dev;Database=RcvDb;Integrated Security=True;"

# For Docker
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=RcvDb;User ID=sa;Password=YourSecurePassword123!;Encrypt=False;"
```

### 4.3 Set OAuth Credentials

```bash
# Google
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"

# Microsoft
dotnet user-secrets set "Authentication:Microsoft:ClientId" "YOUR_MICROSOFT_CLIENT_ID"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "YOUR_MICROSOFT_CLIENT_SECRET"

# Slack
dotnet user-secrets set "Authentication:Slack:ClientId" "YOUR_SLACK_CLIENT_ID"
dotnet user-secrets set "Authentication:Slack:ClientSecret" "YOUR_SLACK_CLIENT_SECRET"

# Apple
dotnet user-secrets set "Authentication:Apple:ClientId" "com.rcv.web.dev"
dotnet user-secrets set "Authentication:Apple:TeamId" "YOUR_APPLE_TEAM_ID"
dotnet user-secrets set "Authentication:Apple:KeyId" "YOUR_APPLE_KEY_ID"
dotnet user-secrets set "Authentication:Apple:PrivateKey" "$(cat AuthKey_XXXXX.p8)"

# Teams (uses Microsoft OAuth with different scope)
dotnet user-secrets set "Authentication:Teams:ClientId" "YOUR_MICROSOFT_CLIENT_ID"
dotnet user-secrets set "Authentication:Teams:ClientSecret" "YOUR_MICROSOFT_CLIENT_SECRET"
```

### 4.4 Verify Secrets

```bash
dotnet user-secrets list
```

---

## Step 5: Create Database Schema

### 5.1 Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version  # Should output 9.0.x
```

### 5.2 Create Initial Migration

```bash
cd src/Rcv.Web.Api
dotnet ef migrations add InitialCreate --context RcvDbContext
```

### 5.3 Apply Migration to Database

```bash
dotnet ef database update --context RcvDbContext
```

This creates the following tables:
- Users
- Polls
- PollOptions
- Votes

### 5.4 Verify Schema

```bash
# Connect to database and verify tables exist
# For Azure SQL or Docker
sqlcmd -S localhost -U sa -P 'YourSecurePassword123!' -d RcvDb -Q "SELECT name FROM sys.tables"

# For LocalDB
sqlcmd -S "(localdb)\rcv-dev" -d RcvDb -E -Q "SELECT name FROM sys.tables"
```

Expected output:
```
name
--------------
Users
Polls
PollOptions
Votes
__EFMigrationsHistory
```

---

## Step 6: Run the Backend API

### 6.1 Build and Run

```bash
cd src/Rcv.Web.Api
dotnet restore
dotnet build
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 6.2 Test API

Open browser to https://localhost:5001/swagger to see OpenAPI documentation.

Test health endpoint:
```bash
curl https://localhost:5001/api/health
# Expected: {"status": "Healthy"}
```

---

## Step 7: Run the Blazor Frontend

### 7.1 Build and Run

```bash
# Open new terminal
cd src/Rcv.Web.Client
dotnet restore
dotnet build
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
```

### 7.2 Access Application

Open browser to https://localhost:7001

You should see:
- Home page with "Create New Poll" button
- Sign in options for all 5 OAuth providers

---

## Step 8: Development Workflow

### 8.1 Run Both Projects Concurrently

**Option A: Visual Studio**
1. Right-click solution → **Properties**
2. **Startup Project** → **Multiple startup projects**
3. Set both `Rcv.Web.Api` and `Rcv.Web.Client` to **Start**
4. Press F5

**Option B: Visual Studio Code**
1. Install "Compound" launch configuration (`.vscode/launch.json`)
2. Press F5 and select "API + Client"

**Option C: Command Line (using tmux or separate terminals)**
```bash
# Terminal 1: API
cd src/Rcv.Web.Api && dotnet watch run

# Terminal 2: Client
cd src/Rcv.Web.Client && dotnet watch run
```

### 8.2 Hot Reload

Use `dotnet watch run` for automatic recompilation on file changes:
```bash
# API with hot reload
cd src/Rcv.Web.Api
dotnet watch run

# Client with hot reload
cd src/Rcv.Web.Client
dotnet watch run
```

### 8.3 Run Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Rcv.Web.Api.Tests/Rcv.Web.Api.Tests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Step 9: Verify OAuth Flow

### 9.1 Test Google Sign-In

1. Navigate to https://localhost:7001
2. Click "Sign in with Google"
3. Authorize the app in Google OAuth consent screen
4. You should be redirected back to the app and see your name in the navbar

### 9.2 Verify User Record Created

```sql
-- Connect to database and check Users table
SELECT Id, ExternalId, Provider, DisplayName, Email, CreatedAt
FROM Users;
```

You should see a new user record with:
- `Provider` = "Google"
- `ExternalId` = your Google user ID
- `DisplayName` = your name from Google profile
- `Email` = your Gmail address

---

## Step 10: Create Your First Poll

### 10.1 Using the UI

1. Click "Create New Poll"
2. Enter:
   - Title: "Best Pizza Topping"
   - Description: "Vote for your favorite pizza topping"
   - Options: "Pepperoni", "Mushrooms", "Olives", "Pineapple"
   - Close date: 7 days from now
   - Settings: Live results enabled, anonymous votes
3. Click "Create Poll"
4. Copy the poll URL

### 10.2 Using the API

```bash
# Get auth cookie first (sign in via browser)
# Then make API call with cookie

curl -X POST https://localhost:5001/api/polls \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{
    "title": "Best Pizza Topping",
    "description": "Vote for your favorite pizza topping",
    "options": ["Pepperoni", "Mushrooms", "Olives", "Pineapple"],
    "closesAt": "2025-11-02T00:00:00Z",
    "isResultsPublic": true,
    "isVotingPublic": false
  }'
```

---

## Step 11: Troubleshooting

### Issue: Database Connection Fails

**Error**: `Microsoft.Data.SqlClient.SqlException: Cannot open server`

**Solution**:
1. Verify SQL Server is running:
   ```bash
   # For LocalDB
   sqllocaldb info rcv-dev

   # For Docker
   docker ps | grep rcv-sql
   ```

2. Check connection string in user secrets:
   ```bash
   cd src/Rcv.Web.Api
   dotnet user-secrets list | grep ConnectionStrings
   ```

3. Test connection with sqlcmd:
   ```bash
   sqlcmd -S localhost -U sa -P 'YourSecurePassword123!' -Q "SELECT @@VERSION"
   ```

### Issue: OAuth Provider Error

**Error**: `invalid_client` or `redirect_uri_mismatch`

**Solution**:
1. Verify redirect URI in OAuth provider console matches exactly:
   - Include protocol (https)
   - Include port (5001)
   - Include path (/api/auth/callback/{provider})

2. Check client ID and secret in user secrets:
   ```bash
   dotnet user-secrets list | grep Authentication
   ```

3. Ensure OAuth provider app is in "development" or "testing" mode

### Issue: EF Migration Fails

**Error**: `Build failed` when running `dotnet ef migrations add`

**Solution**:
1. Ensure project builds successfully:
   ```bash
   cd src/Rcv.Web.Api
   dotnet build
   ```

2. Ensure EF Core tools are installed:
   ```bash
   dotnet tool list --global | grep dotnet-ef
   ```

3. Specify DbContext explicitly:
   ```bash
   dotnet ef migrations add InitialCreate --context RcvDbContext
   ```

### Issue: Port Already in Use

**Error**: `Failed to bind to address https://localhost:5001: address already in use`

**Solution**:
1. Find process using port 5001:
   ```bash
   # macOS/Linux
   lsof -i :5001

   # Windows
   netstat -ano | findstr :5001
   ```

2. Kill the process or change port in `launchSettings.json`:
   ```json
   "applicationUrl": "https://localhost:5002;http://localhost:5003"
   ```

### Issue: HTTPS Certificate Error

**Error**: `The certificate chain was issued by an authority that is not trusted`

**Solution**:
1. Trust the development certificate:
   ```bash
   dotnet dev-certs https --trust
   ```

2. If still failing, clear and regenerate certificate:
   ```bash
   dotnet dev-certs https --clean
   dotnet dev-certs https --trust
   ```

---

## Step 12: Next Steps

### Development Tasks

1. **Implement TDD for User Stories**:
   - See `tasks.md` (generated by `/speckit.tasks`)
   - Write tests first for each acceptance scenario
   - Implement minimum code to pass tests

2. **Set Up CI/CD**:
   - Extend `.github/workflows/ci.yml` to build/test web projects
   - Add deployment workflow for Azure App Service

3. **Configure Logging**:
   - Set up Application Insights (Azure) or Serilog
   - Add structured logging to services

4. **Performance Testing**:
   - Use k6 or Apache Bench to test API under load
   - Verify 1000 concurrent users requirement (SC-005)

### Helpful Commands

```bash
# Watch API logs
cd src/Rcv.Web.Api && dotnet watch run --verbosity detailed

# Run tests in watch mode
dotnet watch test

# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage" && \
  reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage

# Format code
dotnet format

# Check for outdated packages
dotnet list package --outdated

# Publish for deployment
dotnet publish src/Rcv.Web.Api -c Release -o ./publish/api
dotnet publish src/Rcv.Web.Client -c Release -o ./publish/client
```

---

## Environment-Specific Configuration

### Development (`appsettings.Development.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["https://localhost:7001", "http://localhost:7000"]
  }
}
```

### Production (`appsettings.Production.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*.rcv.example.com",
  "Cors": {
    "AllowedOrigins": ["https://app.rcv.example.com"]
  }
}
```

---

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Blazor WebAssembly Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [OAuth 2.0 Specification](https://oauth.net/2/)

---

## Support

If you encounter issues not covered in this guide:

1. Check existing issues on GitHub
2. Review CLAUDE.md for development practices
3. Consult the project PRD: `tasks/prd-v1.md`
4. Ask on the project discussions board

Happy coding! 🚀
