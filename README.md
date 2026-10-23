# CompareHub Backend

.NET Web API that lets a user register product source links (e-commerce sites) and scrapes/extracts product data from them for comparison (via HTML selectors, JSON-LD, an API strategy, and Playwright browser automation).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 14+ (a local install, or run one via Docker: `docker run --name comparehub-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres`)
- [pwsh](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (needed once, to install the Playwright browser)

## Setup

1. Confirm Postgres is running and matches the connection string in `CompareHub.Backend/app/Host/API/appsettings.json` (default: `Host=localhost;Port=5432;Database=CompareHub;Username=postgres;Password=postgres`). Create the `CompareHub` database if it doesn't exist yet — the app applies EF Core migrations automatically on startup, so no manual `dotnet ef database update` is needed.
2. Install the Playwright Chromium browser (used by the browser-automation extraction strategy):
   ```
   cd CompareHub.Backend
   dotnet build
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```
3. Run the API:
   ```
   dotnet run --project CompareHub.Backend/CompareHub.Backend.csproj
   ```
   Available at `http://localhost:5000`. Swagger UI is enabled in development at `http://localhost:5000/swagger`.

## Configuration

All local config lives in `CompareHub.Backend/app/Host/API/appsettings.json` — connection string, JWT issuer/audience/secret, and `Cors:AllowedOrigins` (defaults to `http://localhost:3000` for the frontend dev server). In production these are overridden by environment variables (see `Program.cs`) rather than editing this file.

### Accounts, wishlist and alerts
Sign-up/sign-in, Google sign-in, the wishlist, price history and price-drop alerts need a Postgres database and these
settings (environment variables on the host; `__` separates levels):

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Postgres, as `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require` |
| `Jwt__SecretKey` | Signs login tokens (Render generates one) |
| `Cors__AllowedOrigins__0` | The website's address |
| `App__FrontendUrl` | Website address used in alert emails |
| `Email__User`, `Email__Password` | Gmail address and app password used to send alert emails |
| `Push__PublicKey`, `Push__PrivateKey`, `Push__Subject` | VAPID keys for web push (`Subject` is `mailto:you@example.com`) |
| `Cron__Key` | Shared secret for `POST /api/v1/internal/price-check` (header `X-Cron-Key`) |
| `Google__ClientId` | Optional: enables "Continue with Google" (a Web client id from Google Cloud Console) |

The free host sleeps when idle, so prices are rechecked by a scheduled GitHub Action
(`.github/workflows/price-check.yml`, every 6 hours) that calls the endpoint above; it needs the repository secrets
`BACKEND_URL` and `CRON_KEY`. A drop of at least 3% and ৳10 creates an in-app alert, an email and a web push.

## Tests

```
dotnet test tests/CompareHub.Backend.Tests/CompareHub.Backend.Tests.csproj
```

## Deployment

Deploys to [Render](https://render.com) as a Docker web service, defined in `render.yaml` and `Dockerfile` at the repo root. No database is provisioned: public search works without one, and `render.yaml` explains how to add a long-lived Postgres (e.g. Neon) when login is needed. Render is configured to build from the `master` branch — merging `develop` into `master` and pushing triggers an automatic redeploy. See `render.yaml` for the environment variables that need to be set (`Cors__AllowedOrigins__0` should point at the deployed frontend URL).
