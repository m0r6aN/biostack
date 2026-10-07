# BIO-LOCAL-001 Configuration Review Note

**Parcel:** BIO-LOCAL-001 — Local dev-stack boot proof  
**Review Date:** 2026-10-07  
**Reviewer:** bio_local_001_builder  
**Scope:** `docker-compose.dev.yml`, `.env.example`, production compose gap

## Development Compose Configuration

### Services Defined

`docker-compose.dev.yml` declares two services:

1. **biostack-api-dev**
   - Image: `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
   - Ports: `5000:5000`, `5001:5001`
   - Volumes: `./backend:/app` (live code), `biostack-dev-data:/app/data` (SQLite persistence)
   - Environment: `ASPNETCORE_ENVIRONMENT=Development`, SQLite connection string
   - Command: `dotnet watch run` (hot-reload enabled)
   - Healthcheck: `curl -f http://localhost:5000/health` (10s interval, 10 retries, 30s start)

2. **biostack-ui-dev**
   - Image: `node:22-alpine`
   - Port: `3043:3043`
   - Volumes: `./frontend:/app` (live code), anonymous volumes for `node_modules` and `.next`
   - Environment: `NODE_ENV=development`, `NEXT_PUBLIC_API_URL=http://localhost:5000`
   - Command: `npm install && npm run dev`
   - Healthcheck: `wget -q --spider http://localhost:3043` (10s interval, 10 retries, 60s start)
   - Depends on: API service healthy

### Persistent Storage

Volume `biostack-dev-data`:
- Driver: `local`
- Purpose: SQLite database persistence across container restarts
- Mount point: `/app/data` inside `biostack-api-dev`
- Database files: `biostack.db`, `biostack.db-shm`, `biostack.db-wal` (WAL mode)
- Behavior: Persists unless explicitly removed with `docker compose down -v`

### Environment Variables

The dev compose reads from `.env` file (created from `.env.example`) but overrides critical paths for local development:

**Development Overrides:**
- `ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db` (SQLite, not Postgres)
- `ASPNETCORE_ENVIRONMENT=Development` (enables dev-specific configuration in `appsettings.Development.json`)
- `NODE_ENV=development` (Next.js dev mode)
- `NEXT_PUBLIC_API_URL=http://localhost:5000` (local API, not production URL)

**File-Watcher Flags (Docker hot-reload compatibility):**
- `DOTNET_USE_POLLING_FILE_WATCHER=1`
- `CHOKIDAR_USEPOLLING=true`
- `WATCHPACK_POLLING=true`

## .env.example Review

`.env.example` is a 230-line reference file documenting all configurable environment variables for both development and production. Key sections:

### Database
- `DB_PASSWORD`: Required for Postgres in production; dev uses in-memory default

### Required Production Secrets
- `Jwt__Secret`: Min 32 chars, signs JWT tokens
- `Auth__CallbackSecret`: Shared secret API ↔ frontend, must match `AUTH_CALLBACK_SECRET`
- `AUTH_SECRET`: Frontend NextAuth secret (min 32 chars)
- All three are **blank placeholders** in `.env.example`; never committed with real values

### Stripe (Required for Production Billing)
- `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__OperatorPriceId`, `Stripe__CommanderPriceId`
- All blank in `.env.example`; must be filled for production with live-mode values

### Email Delivery (Optional)
- SMTP or Azure Communication Services Email options
- Blank in `.env.example`; leaving blank uses in-memory magic-link inbox in dev

### OAuth Providers (Optional)
- Google, GitHub, Discord, Apple, Facebook, Instagram client IDs/secrets
- All blank; disabled unless configured

### Passkeys / WebAuthn (Fail-Closed by Default)
- `Auth__Passkeys__Enabled=false` in `.env.example`
- Requires HTTPS, public origin, and finalized RP ID before enabling

### Keon Runtime (Not Present)
**No `KeonRuntime__*` variables** are declared in `.env.example`. This means:
- Development runs with `KeonRuntimeOptions` defaults: `LiveMode=false`, `BaseUrl` empty
- The application uses `KeonRuntimeClientStub` (fail-closed stub mode)
- The `/health/keon` endpoint returns 503 with `"mode":"Offline"` and `"message":"Keon Runtime not configured — running in stub mode"`

## Production Compose Gap — Known Limitation (Not Fixed in This Parcel)

### Issue

`docker-compose.yml` (production compose) **does not pass through any `KeonRuntime__*` environment variables** to the `biostack-api` service.

### Impact

Without explicit configuration, production deployments using `docker-compose.yml` would:
1. Run with `KeonRuntimeOptions` defaults (`LiveMode=false`, `BaseUrl` empty)
2. Use `KeonRuntimeClientStub` instead of the live `KeonRuntimeClient`
3. Trigger the fail-closed startup check in `KeonRuntimeDependencyInjection.cs`:

   ```csharp
   if (isProduction && !isLive && !options.AllowStubInProduction)
   {
       throw new InvalidOperationException(
           "KeonRuntime is not in live mode (LiveMode=false or BaseUrl empty) in a Production "
           + "environment. Governance receipts cannot be anchored. Configure "
           + "KeonRuntime:BaseUrl + KeonRuntime:LiveMode=true, or set "
           + "KeonRuntime:AllowStubInProduction=true to acknowledge running ungoverned.");
   }
   ```

4. **Fail at startup** unless `KeonRuntime:AllowStubInProduction=true` is set (which explicitly acknowledges ungoverned operation)

### Required Variables (Missing from docker-compose.yml)

To run with live Keon Runtime in production, `docker-compose.yml` should pass:

```yaml
environment:
  - KeonRuntime__LiveMode=true
  - KeonRuntime__BaseUrl=${KEON_RUNTIME_BASE_URL:?KeonRuntime base URL must be set}
  - KeonRuntime__BearerToken=${KEON_RUNTIME_BEARER_TOKEN:-}
  - KeonRuntime__TimeoutMs=${KEON_RUNTIME_TIMEOUT_MS:-10000}
```

And `.env.example` should document:

```bash
# ── Keon Runtime (required for governance receipts in production) ────────────
KEON_RUNTIME_BASE_URL=https://keon-runtime.example.com
KEON_RUNTIME_BEARER_TOKEN=
KEON_RUNTIME_TIMEOUT_MS=10000
```

### Status

**This gap is recorded as a known limitation and is not fixed in this parcel.** Per the spec's stop-and-report rule and the scope constraint ("Read-only against product code"), this config gap is documented here for remediation in a future parcel or configuration update.

The governed-delivery mandate for BIO-LOCAL-001 prohibits modifying product code, compose files, Dockerfiles, or `.env` structure. The proof demonstrates the **current state** of the stack; configuration improvements are out of scope.

### Recommended Next Action

A follow-on parcel (or direct coordinator action) should:
1. Add `KeonRuntime__*` variables to `docker-compose.yml`
2. Document them in `.env.example`
3. Update deployment runbooks to require these values before production launch
4. Verify the fail-closed boot check triggers correctly when they are missing

## Development vs. Production Comparison

| Aspect | `docker-compose.dev.yml` | `docker-compose.yml` |
|--------|--------------------------|----------------------|
| **Database** | SQLite in volume | Postgres service |
| **API Image** | `dotnet/sdk:10.0-alpine` (SDK, watch mode) | Built from `backend/Dockerfile` (runtime-only) |
| **UI Image** | `node:22-alpine` (dev server) | Built from `frontend/Dockerfile` (standalone Next.js) |
| **Hot Reload** | Enabled (file watchers) | No (static builds) |
| **Keon Runtime** | Stub (offline) | Stub (offline) — **gap** |
| **Secrets Validation** | Minimal (dev defaults) | Fail-fast on missing required secrets |
| **Environment** | `ASPNETCORE_ENVIRONMENT=Development` | `ASPNETCORE_ENVIRONMENT=Production` |

Both configurations currently run Keon Runtime in stub mode. The production compose lacks the configuration to enable live mode.

## Review Conclusion

- `docker-compose.dev.yml` is correctly configured for local development with SQLite, hot-reload, and stubbed dependencies.
- `.env.example` comprehensively documents all application settings but **does not include `KeonRuntime__*` variables**.
- `docker-compose.yml` (production) **does not pass through Keon Runtime configuration**, which would cause a fail-closed boot failure in production unless `AllowStubInProduction=true` is explicitly set.
- This configuration gap is **recorded here as a known limitation**, not fixed in this parcel per governed-delivery scope constraints.

**Configuration class verified:** Development, local-only, SQLite backend, stubbed Keon Runtime (fail-closed).
