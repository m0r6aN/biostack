# BIO-LOCAL-001 — Local dev-stack boot proof

- Ticket: BIO-LOCAL-001 (parcel directive: `docs/specs/active/BIO-LOCAL-001-local-dev-boot-proof.md`, coordinator copy — read-only, NOT committed)
- Commit SHA (pinned): `e5b75e072c7f99b14ba658ec12ef6004e48a0ca4` (short `e5b75e0`)
- Branch: `proof/bio-local-001-local-dev-boot`
- Worktree: `D:\Repos\BioStack.BIO-LOCAL-001`
- Config class: Development (`ASPNETCORE_ENVIRONMENT=Development` in `docker-compose.dev.yml`; API runs Development per README)
- Docker: `Docker version 29.8.0, build 88096ef` / `Docker Compose version v5.5.1`
- Machine constraints: Windows 11 Pro, 13th Gen Intel i7-13800H, 31.6 GB host RAM; Docker VM `CPUs=20 Mem=4103249920` (~3.9 GiB for the Docker engine — prior OOM history noted, no OOM observed this run)
- `.env`: copied verbatim from `.env.example` (`Copy-Item .env.example .env`); all secret values blank as checked in; `.env` is gitignored (`.gitignore:55:.env*`) and NOT committed, NOT pasted anywhere in this record
- OQ assumptions: OQ2 docker-compose boot REQUIRED; OQ1 zero automated tests (manual boot verification only); OQ3 no INDEX work

## Per-AC verdicts

- AC1 — `docker compose -f docker-compose.dev.yml up --build` reaches healthy API + UI from clean volume state: HOLD (two boots, both reached `healthy`/`healthy`)
- AC2 — `curl http://localhost:5000/health` → 200; `/health/keon` body recorded verbatim with stubbed posture noted: HOLD (`/health` 200 `Healthy`; `/health/keon` HTTP 503 with stub JSON below — stubbed-dev posture, never governance)
- AC3 — Frontend `http://localhost:3043` returns app shell HTTP 200: HOLD (200, 56357-byte Next.js shell, both boots)
- AC4 — SQLite volume `biostack-dev-data` documented; `down -v` resets and second boot succeeds: HOLD
- AC5 — Evidence file written; `git diff --check` clean; redaction attestation: HOLD (see bottom)

## UTC windows (all UTC)

- Pre-clean `down -v`: 2026-09-19T13:44:07Z, exit 0 (nothing to remove — clean starting state confirmed)
- Boot 1 `up --build -d`: start 2026-09-19T13:44:30Z, end 2026-09-19T13:45:33Z (~63 s), exit 0
- Probes boot 1: 2026-09-19T13:45:43Z (`/health`), 13:45:48Z (`/health/keon`), 13:47:35–36Z (clean header captures), UI 200 confirmed after ~60 s warmup (first probe during `npm install` returned curl exit 52 empty-reply — warmup, not a defect)
- Reset `down -v`: start 2026-09-19T13:47:52Z, end 2026-09-19T13:47:59Z, exit 0 (volume removed, verified absent via `docker volume ls`)
- Boot 2 `up --build -d`: start 2026-09-19T13:48:03Z, end 2026-09-19T13:48:56Z (~53 s), exit 0
- Probes boot 2 (~13:50:30Z): `/health` 200, `/health/keon` identical stub body, UI 200 (56357 bytes), both containers `(healthy)`

Note on `-d`: the directive verification lists `up --build` (attached). I ran `up --build -d` (detached) so the probes below could run against the live stack, then asserted health via `docker compose ps` (API `healthy`, UI `healthy`) plus the endpoint transcripts. Build semantics are identical.

## Commands run (exact, with exit codes)

| # | Command | Exit | Key output |
|---|---------|------|------------|
| 1 | `docker compose -f docker-compose.dev.yml down -v` (pre-clean) | 0 | clean state |
| 2 | `docker compose -f docker-compose.dev.yml up --build -d` (boot 1) | 0 | `Network … Created`, `Volume biostackbio-local-001_biostack-dev-data Created`, `Container biostack-api-dev … Healthy`, `Container biostack-ui-dev Started` |
| 3 | `curl.exe -i --max-time 10 http://localhost:5000/health` | 0 | `HTTP/1.1 200 OK`, body `Healthy` (full transcript below) |
| 4 | `curl.exe -i --max-time 10 http://localhost:5000/health/keon` | 0 | `HTTP/1.1 503 Service Unavailable`, stub JSON body (full transcript below) |
| 5 | `curl.exe -i --max-time 10 http://localhost:3043` (early, UI warming) | 52 | `Empty reply from server` — UI still running `npm install` / Next dev startup; NOT a product defect |
| 6 | `curl.exe -s -o NUL -w "HTTP_CODE:%{http_code} SIZE:%{size_download} …" http://localhost:3043` (after warmup) | 0 | `HTTP_CODE:200 SIZE:56357` |
| 7 | `docker compose -f docker-compose.dev.yml ps` | 0 | both `Up … (healthy)` (boot 1 and boot 2) |
| 8 | `docker volume inspect biostackbio-local-001_biostack-dev-data` | 0 | `Driver=local Mountpoint=/var/lib/docker/volumes/biostackbio-local-001_biostack-dev-data/_data` |
| 9 | `docker exec biostack-api-dev sh -c "ls -la /app/data && du -h /app/data/*"` | 0 | `biostack.db` (4.0K) + `biostack.db-shm` (32K) + `biostack.db-wal` (~568K) — SQLite WAL mode |
| 10 | `docker compose -f docker-compose.dev.yml down -v` (reset) | 0 | containers, volume, network `Removed`; volume absent from `docker volume ls` afterwards |
| 11 | `docker compose -f docker-compose.dev.yml up --build -d` (boot 2) | 0 | same healthy sequence as boot 1 |
| 12 | Re-probes boot 2 (`/health`, `/health/keon`, `:3043`) | 0 | `health:200`, identical stub body, `ui:200 size:56357` |

No product-code, compose, Dockerfile, `.env`, migration, or contract edits were made (read-only against product code per directive; no stop condition fired).

## Transcripts (verbatim headers + bodies)

### `GET http://localhost:5000/health` → 200

Request: `curl.exe -s -D - -o NUL --max-time 10 http://localhost:5000/health`

```http
HTTP/1.1 200 OK
Content-Type: text/plain
Date: Sat, 19 Sep 2026 13:47:35 GMT
Server: Kestrel
Cache-Control: no-store, no-cache
Expires: Thu, 01 Jan 1970 00:00:00 GMT
Pragma: no-cache
Transfer-Encoding: chunked
```

Body (`curl.exe -s http://localhost:5000/health`), verbatim:

```text
Healthy
```

### `GET http://localhost:5000/health/keon` → 503 stub (verbatim)

Request: `curl.exe -s -D - -o NUL --max-time 10 http://localhost:5000/health/keon`

```http
HTTP/1.1 503 Service Unavailable
Content-Type: application/json; charset=utf-8
Date: Sat, 19 Sep 2026 13:47:35 GMT
Server: Kestrel
Transfer-Encoding: chunked
```

Body (`curl.exe -s http://localhost:5000/health/keon`), verbatim (104 bytes):

```json
{"status":"unhealthy","mode":"Offline","message":"Keon Runtime not configured — running in stub mode"}
```

Stubbed-Keon posture statement (exact, never oversold): Development convenience only — the API runs in Development with no live Keon runtime, so the stub (`KeonRuntimeClientStub`) serves `/health/keon` as `Offline`/stub mode. This is NOT a governance claim; it says nothing about deployed governance. Fail-closed wording lives in `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs` (Production refuses stubbed start unless explicitly acknowledged).

### `GET http://localhost:3043` → 200 app shell

Request: `curl.exe -s -D - -o NUL --max-time 15 http://localhost:3043`

```http
HTTP/1.1 200 OK
Vary: rsc, next-router-state-tree, next-router-prefetch, next-router-segment-prefetch, Accept-Encoding
Cache-Control: no-cache, must-revalidate
X-Powered-By: Next.js
Content-Type: text/html; charset=utf-8
Date: Sat, 19 Sep 2026 13:47:36 GMT
Connection: keep-alive
Keep-Alive: timeout=5
Transfer-Encoding: chunked
```

Body fingerprint: `HTTP_CODE:200 SIZE:56357` (boot 1 and boot 2 identical size). Full 56 KB HTML omitted by size; verified content is the BioStack marketing/app shell (`<title>BioStack | Evidence-Graded Research on Peptides and Similar Compounds</title>`, `BioStack is not a doctor.` footer, nav/library/pricing links). No secrets, tokens, PII, or health payloads in headers or sampled body.

## Compose log excerpts (no secrets)

API (`docker logs biostack-api-dev --tail 8`):

```text
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 GET http://localhost:5000/health - - -
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'Health checks'
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[1]
      Executed endpoint 'Health checks'
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      Request finished HTTP/1.1 GET http://localhost:5000/health - 200 - text/plain 0.3469ms
```

UI (`docker logs biostack-ui-dev --tail 8`, representative):

```text
 GET / 200 in 135ms (next.js: 9ms, proxy.ts: 47ms, application-code: 79ms)
 GET / 200 in 73ms (next.js: 4ms, proxy.ts: 12ms, application-code: 56ms)
 GET / 200 in 115ms (next.js: 10ms, proxy.ts: 29ms, application-code: 76ms)
 GET / 200 in 82ms (next.js: 6ms, proxy.ts: 15ms, application-code: 61ms)
```

## SQLite volume behavior + reset note

- Compose declares named volume `biostack-dev-data` (local driver), mounted over `/app/data` in `biostack-api-dev`; connection string `Data Source=/app/data/biostack.db` (see config note). Effective runtime volume name is project-prefixed: `biostackbio-local-001_biostack-dev-data` (Mountpoint `/var/lib/docker/volumes/biostackbio-local-001_biostack-dev-data/_data`). A separate pre-existing `biostack_biostack-dev-data` volume from another compose project was observed and left untouched.
- Live behavior (boot 1): `/app/data` contained `biostack.db` + `-shm`/`-wal` (SQLite WAL mode), confirming file-backed persistence inside the named volume — NOT the host `backend/data/` directory (per README).
- Reset: `docker compose -f docker-compose.dev.yml down -v` (13:47:52–13:47:59Z, exit 0) removed both containers, the network, and `biostackbio-local-001_biostack-dev-data` (verified absent). Second `up --build -d` (13:48:03–13:48:56Z, exit 0) recreated everything from scratch and all probes passed identically — reset is clean and repeatable. Stack left RUNNING after boot-2 verification for reviewer replay; rerun `down -v` + `up --build` from a clean volume to reproduce.

## Tests

Zero automated tests added or run (OQ1 assumption: manual boot verification per spec). No test framework invoked.

## Redaction attestation

I attest this evidence file contains NO secrets, tokens, credentials, PII, or health (PHI) payloads: endpoint bodies are `Healthy` and the stub-status JSON above; the UI transcript is headers + size + structural excerpt only; logs are framework request lines; `.env` values are never pasted (secrets were blank placeholders per `.env.example`).

## Session handoff

- Starting commit: `e5b75e072c7f99b14ba658ec12ef6004e48a0ca4` / Ending commit: (evidence commit hash in completion claim) / Files changed: the two Allowed Files only / Commands run: table above / Tests passed: n/a (OQ1, zero automated) / Tests failed: none / Decisions needed: none / Blockers: none / Next safe action: coordinator Gate-3 review (replay from clean volume) / Do not touch: product code, compose, Dockerfiles, `.env`, migrations, contracts, `docs/specs/` directive copy.
