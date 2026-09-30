# BIO-LOCAL-001 — Config note (dev compose + `.env.example` review)

- Ticket: BIO-LOCAL-001 / Commit SHA (pinned): `e5b75e072c7f99b14ba658ec12ef6004e48a0ca4` (`e5b75e0`) / Config class: Development / Date (UTC): 2026-09-19
- Scope: review ONLY. No compose, Dockerfile, `.env`, migration, or contract change made here (stop-and-report applies; none fired).

## 1. Dev compose review (`docker-compose.dev.yml`, 60 lines)

- Services: `biostack-api-dev` (image `mcr.microsoft.com/dotnet/sdk:10.0-alpine`, ports `5000:5000` + `5001:5001`, `command: dotnet watch run … --urls http://+:5000`, `ASPNETCORE_ENVIRONMENT=Development`, `ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db`) and `biostack-ui-dev` (image `node:22-alpine`, port `3043:3043`, `command: npm install && npm run dev`, `NEXT_PUBLIC_API_URL=http://localhost:5000`).
- `env_file: [.env]` on BOTH services; `.env` was copied verbatim from `.env.example` with blank secrets (gitignored, never committed). Dev boot succeeds with blanks because Development defaults come from `appsettings.Development.json` + compose environment.
- Volumes: host `./backend` → `/app`, named `biostack-dev-data` → `/app/data` (SQLite file lives here, not host `backend/data/`); host `./frontend` → `/app` with anonymous `/app/node_modules` and `/app/.next` (reinstalled/rebuilt per container recreation — observed UI warmup ~60 s on fresh volume, curl exit 52 empty-reply while warming, then 200).
- Healthchecks: API `curl -f http://localhost:5000/health` (start_period 30 s); UI `wget --spider http://localhost:3043` (start_period 60 s); UI `depends_on: biostack-api-dev condition: service_healthy` — observed ordering held (UI started only after API healthy, both boots).
- Local-only posture: no Postgres, no Redis requirement (in-memory fallback), no SMTP/Azure (in-memory magic-link inbox when hosts blank), no Stripe live, no image pushes. Matches the directive's local-only constraint.

## 2. `.env.example` review (107 lines)

- Secret placeholders are BLANK by default and stay blank for dev: `DB_PASSWORD=`, `Jwt__Secret=`, `Auth__CallbackSecret=`, `AUTH_SECRET=`, `AUTH_CALLBACK_SECRET=`, `Smtp__Username/Password=`, `Stripe__SecretKey/WebhookSecret/OperatorPriceId/CommanderPriceId=`. Generation guidance (`openssl rand -hex 32`) is production-only.
- Dev-relevant fields: `Smtp__Host=` blank → in-memory inbox; commented dev URLs (`PublicApiUrl=http://localhost:5000`, `FrontendUrl=http://localhost:3043`); OAuth/Stripe/Redis sections blank/commented (disabled). Nothing real was filled in for this proof; no credential material appears in any artifact.
- SG-L5 (partial, local config/secrets posture): PASS for this parcel — blank/placeholder-only local config, `.env` gitignored (`.gitignore:55`), never committed, never pasted. SG-L6 (partial, no payload in logs/artifacts): PASS — transcripts contain only `Healthy` / stub-status JSON / framework log lines / UI shell structure.

## 3. Prod-compose gap — RECORDED as known limitation, NEVER fixed here

- As documented in `README.md` ("Run the production-shaped local stack"): the production-shaped composition (`docker-compose.yml`, `biostack-api` service) does NOT pass `KeonRuntime__*` variables through from `.env`, and `.env.example` does not list them — so a Production-environment API cannot reach live Keon (`KeonRuntime__BaseUrl` + `KeonRuntime__LiveMode=true`) without manually adding them to the `biostack-api` `environment` block (or explicitly acknowledging ungoverned via `KeonRuntime__AllowStubInProduction=true`).
- Consequence (by design, fail-closed in `KeonRuntimeDependencyInjection.cs`): Production startup THROWS when stubbed/unconfigured — silently serving production traffic without a governance runtime becomes a boot failure. `StubAllowAll=true` is likewise rejected in Production.
- This gap is INTENTIONALLY left open in BIO-LOCAL-001 (out-of-scope: prod compose, image pushes, Azure/billing/email/Postgres drills; forbidden: compose/contract edits). A later parcel owns any production-compose remediation. The dev-stack claim proven here (`local` boot + stubbed-dev posture) stands independent of it.

## 4. References asserted (all read, none modified)

- `docker-compose.dev.yml` — services/healthchecks/ports/volumes as above.
- `README.md` § "Local development" → "Run the development stack" (line 124) — procedure proven; § "Run the production-shaped local stack" — gap source quoted in §3.
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs` (71 lines) — fail-closed wording quoted correctly in the boot-proof evidence file.

## Redaction attestation

This note contains NO secrets, tokens, credentials, PII, or health payloads — only file/line references, blank-key names, and structural config description.
