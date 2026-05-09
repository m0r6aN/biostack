# BioStack Go-Live Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden BioStack for production launch by closing the critical security, reliability, and operational gaps identified in the pre-launch audit.

**Architecture:** Fixes are surgical — no new layers or abstractions. Changes touch: docker-compose secrets hygiene, backend startup (Program.cs migration + DevAuth guard), frontend research admin auth, and CI pipeline test gates.

**Tech Stack:** .NET 10 (C#), Next.js 16 (TypeScript), Docker Compose, GitHub Actions

---

## Pre-existing baseline

- Backend: 18/42 tests failing pre-existing (EF dual-provider issue in test setup — not introduced here)
- Frontend: node_modules rebuild required in worktree before vitest runs

---

## Task 1: Create .env.example

**Files:**
- Create: `frontend/.env.example`
- Create: `.env.example` (root — for compose vars)

- [ ] **Step 1: Write root .env.example**

```bash
# BioStack — Production Environment Variables
# Copy to .env, fill in real values, never commit .env

# ── Database ────────────────────────────────────────────────────────────────
DB_PASSWORD=                        # Required. Strong random password for PostgreSQL.

# ── Backend secrets ─────────────────────────────────────────────────────────
Jwt__Secret=                        # Required. Min 32 random chars. Signs JWT bearer tokens.
Auth__CallbackSecret=               # Required. Shared secret between API and frontend for magic link callbacks.

# ── Frontend (NextAuth) ──────────────────────────────────────────────────────
AUTH_SECRET=                        # Required. Min 32 random chars. openssl rand -hex 32
AUTH_URL=https://yourdomain.com     # Required. Public base URL of the frontend.
AUTH_CALLBACK_SECRET=               # Required. Must match Auth__CallbackSecret above.

# ── URLs ─────────────────────────────────────────────────────────────────────
NEXT_PUBLIC_API_URL=https://api.yourdomain.com
API_INTERNAL_URL=http://biostack-api:5000
PublicApiUrl=https://api.yourdomain.com
FrontendUrl=https://yourdomain.com

# ── Email delivery (choose one) ─────────────────────────────────────────────
# Option A: SMTP
Smtp__Host=smtp.yourdomain.com
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__Username=
Smtp__Password=
Smtp__FromEmail=noreply@yourdomain.com
Smtp__FromName=BioStack

# Option B: Azure Communication Services
# AzureCommunicationEmail__ConnectionString=

# ── Stripe ───────────────────────────────────────────────────────────────────
# Stripe__SecretKey=
# Stripe__WebhookSecret=

# ── Redis (optional — falls back to in-memory) ───────────────────────────────
# Redis__Configuration=your-redis-host:6379
# Redis__InstanceName=biostack:

# ── OAuth providers (all optional) ──────────────────────────────────────────
# GOOGLE_CLIENT_ID=
# GOOGLE_CLIENT_SECRET=
# GITHUB_CLIENT_ID=
# GITHUB_CLIENT_SECRET=

# ── Knowledge worker ─────────────────────────────────────────────────────────
Worker__RunMode=Seed
Worker__DryRun=false
Worker__MaxBatchSize=50
```

- [ ] **Step 2: Write frontend/.env.example**

```bash
# BioStack Frontend — Environment Variables
# Copy to .env.local, fill in real values, never commit .env.local

# ── API ───────────────────────────────────────────────────────────────────────
NEXT_PUBLIC_API_URL=http://localhost:5000
API_INTERNAL_URL=http://localhost:5000

# ── Auth (NextAuth v5) ────────────────────────────────────────────────────────
# Generate: openssl rand -hex 32
AUTH_SECRET=
AUTH_URL=http://localhost:3043
AUTH_TRUST_HOST=true
# Must match backend Auth__CallbackSecret
AUTH_CALLBACK_SECRET=

# ── Research data source ──────────────────────────────────────────────────────
# "fixtures" = local JSON files (dev), "api" = real backend
RESEARCH_DATA_SOURCE=fixtures
# Only used when RESEARCH_DATA_SOURCE=api:
# RESEARCH_ARTIFACTS_PATH=research/output/<run-id>
```

- [ ] **Step 3: Commit**

```bash
git add .env.example frontend/.env.example
git commit -m "docs: add .env.example files documenting all required production vars"
```

---

## Task 2: Fail-fast on missing production secrets in docker-compose.yml

**Files:**
- Modify: `docker-compose.yml`

The `${VAR:-fallback}` syntax silently accepts `CHANGE_ME_*` strings.
Use `${VAR:?Error message}` so compose refuses to start if a secret is unset.

- [ ] **Step 1: Replace CHANGE_ME fallbacks for secret vars**

In `docker-compose.yml`, replace these 4 lines:

```yaml
      - Jwt__Secret=${Jwt__Secret:-CHANGE_ME_MINIMUM_32_CHARACTER_RANDOM_STRING}
      - Auth__CallbackSecret=${Auth__CallbackSecret:-CHANGE_ME_SHARED_SECRET}
```
```yaml
      - AUTH_SECRET=${AUTH_SECRET:-CHANGE_ME_NEXTAUTH_SECRET_MIN_32_CHARS}
      - AUTH_CALLBACK_SECRET=${AUTH_CALLBACK_SECRET:-CHANGE_ME_SHARED_SECRET}
```

With fail-fast versions:

```yaml
      - Jwt__Secret=${Jwt__Secret:?Jwt__Secret must be set (min 32 random chars). See .env.example}
      - Auth__CallbackSecret=${Auth__CallbackSecret:?Auth__CallbackSecret must be set. See .env.example}
```
```yaml
      - AUTH_SECRET=${AUTH_SECRET:?AUTH_SECRET must be set (min 32 random chars). See .env.example}
      - AUTH_CALLBACK_SECRET=${AUTH_CALLBACK_SECRET:?AUTH_CALLBACK_SECRET must be set. See .env.example}
```

- [ ] **Step 2: Verify compose parses correctly**

```bash
docker compose config --quiet 2>&1 | head -5
# Expected: error about missing vars (since .env is not set) — that's correct behaviour
```

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "fix(infra): fail-fast on missing production secrets in docker-compose"
```

---

## Task 3: Add EF Core Migrate() for PostgreSQL on production startup

**Files:**
- Modify: `backend/src/BioStack.Api/Program.cs:342-372`

In production, `db.Database.EnsureCreated()` is never called and no migration runs automatically.
Add `db.Database.Migrate()` for PostgreSQL so a fresh deployment self-migrates.

- [ ] **Step 1: Locate the startup DB block** (already read — lines 342-372)

- [ ] **Step 2: Replace the startup block**

Current code (lines 342-372):
```csharp
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
    await InteractionSchemaBootstrapper.EnsureCompoundInteractionHintsTableAsync(db);

    if (!app.Environment.IsProduction())
    {
        db.Database.EnsureCreated();
        // ... sqlite-only code
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL] Database initialization failed: {ex.Message}");
}
```

Replace with:
```csharp
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
    await InteractionSchemaBootstrapper.EnsureCompoundInteractionHintsTableAsync(db);

    if (app.Environment.IsProduction())
    {
        // Apply pending EF migrations on startup so fresh deployments self-migrate.
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();

        if (db.Database.IsSqlite())
        {
            var createScript = DatabaseSchemaBootstrapper.MakeSqliteCreateScriptIdempotent(
                db.Database.GenerateCreateScript());

            if (!string.IsNullOrWhiteSpace(createScript))
            {
                db.Database.ExecuteSqlRaw(createScript);
            }

            DatabaseSchemaBootstrapper.BackfillMissingSqliteColumns(db);
        }

        var hintRepository = scope.ServiceProvider.GetRequiredService<ICompoundInteractionHintRepository>();
        await CompoundInteractionHintCatalog.SeedDefaultsAsync(hintRepository);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[FATAL] Database initialization failed: {ex.Message}");
    throw; // Re-throw so the container exits with non-zero code and orchestrator restarts it
}
```

- [ ] **Step 3: Build to verify no compile errors**

```bash
cd backend && dotnet build --no-restore -v quiet 2>&1 | tail -5
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/BioStack.Api/Program.cs
git commit -m "fix(backend): run EF migrations on production startup, re-throw fatal DB errors"
```

---

## Task 4: Harden DevAuthEndpoints — IsDevelopment() only

**Files:**
- Modify: `backend/src/BioStack.Api/Program.cs:339-341`

Current: DevAuthEndpoints registers when `useInMemoryMagicLinks` is true, which is also true when
`FrontendUrl` points to localhost — even if `ASPNETCORE_ENVIRONMENT=Production`.
Fix: only register in `IsDevelopment()`.

- [ ] **Step 1: Locate registration line** (line 339-341 in Program.cs)

```csharp
if (useInMemoryMagicLinks)
    app.MapDevAuthEndpoints();
```

- [ ] **Step 2: Replace with IsDevelopment guard**

```csharp
if (app.Environment.IsDevelopment())
    app.MapDevAuthEndpoints();
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build --no-restore -v quiet 2>&1 | tail -3
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/BioStack.Api/Program.cs
git commit -m "fix(security): restrict DevAuthEndpoints to IsDevelopment() only"
```

---

## Task 5: Fix AdminEndpoints error leak

**Files:**
- Modify: `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs:34-36`

`Results.Problem(ex.Message)` sends the raw exception text to the client.

- [ ] **Step 1: Replace the catch block**

Current:
```csharp
catch (Exception ex)
{
    return Results.Problem(ex.Message);
}
```

Replace with:
```csharp
catch (Exception ex)
{
    // Log internally, return generic message to avoid leaking implementation details
    app.Logger.LogError(ex, "Knowledge ingest failed");
    return Results.Problem("Knowledge ingestion failed. Check server logs for details.");
}
```

Wait — `app` is not in scope inside `MapAdminEndpoints`. Use the injected `ILogger` pattern:

```csharp
group.MapPost("/knowledge/ingest", async (
    [FromBody] List<KnowledgeEntry> entries,
    [FromServices] IKnowledgeSource knowledgeSource,
    [FromServices] IMemoryCache memoryCache,
    [FromServices] ILogger<AdminEndpoints> logger,
    CancellationToken ct) =>
{
    if (entries == null || entries.Count == 0)
        return Results.BadRequest("No entries provided");

    try
    {
        var count = await knowledgeSource.IngestBulkAsync(entries, ct);
        memoryCache.Remove("analyzer:knowledge:aliases");
        return Results.Ok(new { Message = $"Successfully ingested {count} compounds", Count = count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Knowledge ingest failed");
        return Results.Problem("Knowledge ingestion failed. Check server logs for details.");
    }
});
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build --no-restore -v quiet 2>&1 | tail -3
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs
git commit -m "fix(security): don't leak exception message in admin knowledge ingest endpoint"
```

---

## Task 6: Fix research admin pages — remove dev-token call, add session auth guard

**Files:**
- Modify: `frontend/src/app/admin/research/page.tsx`
- Modify: `frontend/src/app/admin/research/pipeline/page.tsx`
- Create: `frontend/src/app/admin/research/layout.tsx`

Both research pages call `POST /api/v1/auth/dev-token` to get a JWT. In production that endpoint doesn't exist. The token stays null and all data fetches fail silently.

Fix: remove the dev-token call. The pages already use Bearer token in the loader — switch to the user's session cookie instead (the backend accepts both cookie and bearer). Add a layout.tsx that redirects unauthenticated users to sign-in.

- [ ] **Step 1: Create the admin/research layout with auth guard**

Create `frontend/src/app/admin/research/layout.tsx`:

```typescript
import { auth } from '@/auth';
import { redirect } from 'next/navigation';

export default async function ResearchLayout({ children }: { children: React.ReactNode }) {
  const session = await auth();
  if (!session?.user) {
    redirect('/auth/signin?callbackUrl=/admin/research');
  }
  return <>{children}</>;
}
```

- [ ] **Step 2: Remove acquireToken from research/page.tsx**

In `frontend/src/app/admin/research/page.tsx`, remove:
```typescript
const tokenRef = useRef<string | null>(null);
```
and:
```typescript
useEffect(() => {
  acquireToken().then(load);
}, []);

async function acquireToken() {
  try {
    const res = await fetch(`${getApiBaseUrl()}/api/v1/auth/dev-token`, { method: 'POST' });
    if (res.ok) tokenRef.current = (await res.json()).token;
  } catch { /* production — no-op */ }
}

async function load() {
  const t = tokenRef.current ?? '';
```

Replace with:
```typescript
useEffect(() => {
  load();
}, []);

async function load() {
  const t = '';  // cookie auth — no bearer token needed for server-side session
```

- [ ] **Step 3: Remove acquireToken from research/pipeline/page.tsx**

Same pattern — find and remove the `tokenRef`, `acquireToken`, and change `load` to not take token from dev-token.

- [ ] **Step 4: Build frontend to check for type errors**

```bash
cd frontend && npx tsc --noEmit 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/admin/research/
git commit -m "fix(security): add auth guard to research admin, remove dev-token dependency"
```

---

## Task 7: Add CI test steps before Docker build

**Files:**
- Modify: `.github/workflows/deploy.yml`

Tests never run before deployment. Add `dotnet test` and `npm run test` as required steps before building Docker images.

- [ ] **Step 1: Add test jobs to deploy.yml**

Add these steps after `Checkout` and before the Azure login / Docker build steps:

```yaml
      # ── Backend tests ─────────────────────────────────────────────────────────
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      - name: Run backend tests
        working-directory: backend
        run: dotnet test --no-build --verbosity minimal || dotnet test --verbosity minimal

      # ── Frontend tests ────────────────────────────────────────────────────────
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install frontend deps
        working-directory: frontend
        run: npm ci

      - name: Run frontend tests
        working-directory: frontend
        run: npm test
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/deploy.yml
git commit -m "ci: add dotnet test + npm test gates before Docker build in deploy workflow"
```

---

## Final: Tag, push, open PR

- [ ] **Step 1: Verify build still passes**

```bash
cd backend && dotnet build --no-restore -v quiet 2>&1 | tail -3
```

- [ ] **Step 2: Tag**

```bash
git tag v0.9.0-hardened
```

- [ ] **Step 3: Push branch and tags**

```bash
git push origin HEAD --tags
```

- [ ] **Step 4: Open PR**

```bash
gh pr create \
  --title "fix: go-live hardening — secrets, migrations, auth, CI tests" \
  --body "..."
```
