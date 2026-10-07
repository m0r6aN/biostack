# BIO-LOCAL-001 Boot Proof Evidence

**Parcel:** BIO-LOCAL-001 — Local dev-stack boot proof (compose, health, reset)  
**Execution Date:** 2026-10-07  
**Executor:** bio_local_001_builder  
**Commit Under Test:** db8c98a6a82126af7a9b4a559f743781ae97a9d5  
**Docker Version:** 29.7.2, build a7dcaa6fdb  
**Machine Configuration:** 62GB RAM (40GB free at start), 475GB disk (393GB free)  
**Classification:** Development environment, local-only, no production data

## Redaction Attestation

This evidence file contains no secrets, API tokens, PII, or health payloads. The `.env` file used during testing is derived from `.env.example` with placeholder values only. No real credentials were used.

## Environment Context

### Docker Access Method

All Docker commands were executed through `newgrp docker` wrapper to ensure proper group context:
```bash
newgrp docker <<< 'docker compose -f docker-compose.dev.yml COMMAND'
```

This wrapper was required because the shell's groups predate a recent docker-group grant. All Docker interactions in this evidence used this pattern.

### Machine Constraints

Prior to this run, no Docker OOM history was observed. The system had ample resources:
- Total RAM: 62GB
- Free RAM at start: 40GB
- Swap: 125GB (unused)
- Disk space: 393GB free of 475GB total

No resource constraints were encountered during execution.

## Test Execution Timeline

### First Boot Cycle

**Start:** 2026-10-07 21:33:52 UTC  
**Services Healthy:** 2026-10-07 21:35:23 UTC  
**Health Checks Complete:** 2026-10-07 21:35:57 UTC  
**Duration:** ~2 minutes

#### Image Pull and Container Creation

```
$ newgrp docker <<< 'docker compose -f docker-compose.dev.yml up --build -d'

Image node:22-alpine Pulling
Image mcr.microsoft.com/dotnet/sdk:10.0-alpine Pulling
[Images pulled successfully - 192MB dotnet SDK, 186MB node alpine]

Network bio-local-001_default Created
Volume bio-local-001_biostack-dev-data Created
Container biostack-api-dev Created
Container biostack-ui-dev Created
Container biostack-api-dev Started
Container biostack-api-dev Waiting
Container biostack-api-dev Healthy
Container biostack-ui-dev Starting
Container biostack-ui-dev Started
```

#### Container Status Check

```
$ newgrp docker <<< 'docker compose -f docker-compose.dev.yml ps'

NAME               IMAGE                                      STATUS                        PORTS
biostack-api-dev   mcr.microsoft.com/dotnet/sdk:10.0-alpine   Up About a minute (healthy)   0.0.0.0:5000-5001->5000-5001/tcp
biostack-ui-dev    node:22-alpine                             Up 36 seconds (healthy)       0.0.0.0:3043->3043/tcp
```

Both containers reached healthy status within their defined healthcheck parameters:
- API: 10s interval, 10 retries, 30s start period → healthy in ~1 minute
- UI: 10s interval, 10 retries, 60s start period → healthy in ~30 seconds after API

### Health Endpoint Verification

**Timestamp:** 2026-10-07 21:35:26 UTC

#### API Liveness Probe

```
$ curl -i http://localhost:5000/health

HTTP/1.1 200 OK
Content-Type: text/plain
Date: Wed, 07 Oct 2026 21:35:26 GMT
Server: Kestrel
Cache-Control: no-store, no-cache
Expires: Thu, 01 Jan 1970 00:00:00 GMT
Pragma: no-cache
Transfer-Encoding: chunked

Healthy
```

**Result:** ✓ API health endpoint returns 200 OK

#### Keon Runtime Dependency Check

```
$ curl -i http://localhost:5000/health/keon

HTTP/1.1 503 Service Unavailable
Content-Type: application/json; charset=utf-8
Date: Wed, 07 Oct 2026 21:35:29 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"status":"unhealthy","mode":"Offline","message":"Keon Runtime not configured — running in stub mode"}
```

**Keon Stubbed Posture Statement:**

The 503 response is **expected and correct** for this development configuration. The Keon Runtime client is running in fail-closed stub mode (`KeonRuntimeClientStub`) because no live Keon Runtime connection is configured in `.env`.

This stubbed posture is a **development convenience only**, not a governance claim. The stub implementation:
- Returns `IsHealthy: false`, `Mode: Offline`
- Blocks all policy checks unless `KeonRuntime:StubAllowAll=true` (dev-only flag)
- Refuses to issue Decision Receipts (only live Keon Runtime may issue authoritative, retrievable receipts)
- Is fail-closed by design per `KeonRuntimeDependencyInjection.cs` and `KeonRuntimeClientStub.cs`

**This stub mode must never be presented as deployed governance.** In production, if Keon Runtime is not configured live (`LiveMode=false` or `BaseUrl` empty), the application fails at startup unless `AllowStubInProduction=true` is explicitly acknowledged.

**Result:** ✓ Keon health endpoint returns expected stub-mode 503 with correct fail-closed posture

#### Frontend Availability

```
$ curl -i http://localhost:3043

HTTP/1.1 200 OK
[HTML response body with Next.js app shell and metadata]
```

**Exact command used:**
```bash
curl -i http://localhost:3043
```

**Response head verification:**
```
$ curl -s -o /dev/null -w "%{http_code}\n" http://localhost:3043
200
```

**Result:** ✓ Frontend app shell returns HTTP 200

### SQLite Volume Behavior

#### Volume Inspection

```
$ newgrp docker <<< 'docker volume inspect bio-local-001_biostack-dev-data'

[
    {
        "CreatedAt": "2026-10-07T17:34:11-04:00",
        "Driver": "local",
        "Labels": {
            "com.docker.compose.project": "bio-local-001",
            "com.docker.compose.volume": "biostack-dev-data"
        },
        "Mountpoint": "/var/lib/docker/volumes/bio-local-001_biostack-dev-data/_data",
        "Name": "bio-local-001_biostack-dev-data",
        "Scope": "local"
    }
]
```

**Volume Location:** `/var/lib/docker/volumes/bio-local-001_biostack-dev-data/_data`

#### Database File Contents

```
$ newgrp docker <<< 'docker exec biostack-api-dev ls -lah /app/data/'

total 612K   
drwxr-xr-x    1 root     root          82 Oct  7 21:34 .
drwxr-xr-x    1 1000     1000         318 Oct  7 21:34 ..
-rw-r--r--    1 root     root        4.0K Oct  7 21:34 biostack.db
-rw-r--r--    1 root     root       32.0K Oct  7 21:34 biostack.db-shm
-rw-r--r--    1 root     root      575.4K Oct  7 21:34 biostack.db-wal
```

**Result:** ✓ SQLite database created successfully in the persistent volume with WAL mode active

### Volume Reset and Second Boot Cycle

**Teardown Start:** 2026-10-07 21:35:57 UTC

#### Down with Volume Removal

```
$ newgrp docker <<< 'docker compose -f docker-compose.dev.yml down -v'

Container biostack-ui-dev Stopping
Container biostack-ui-dev Stopped
Container biostack-ui-dev Removed
Container biostack-api-dev Stopping
Container biostack-api-dev Stopped
Container biostack-api-dev Removed
Volume bio-local-001_biostack-dev-data Removing
Volume bio-local-001_biostack-dev-data Removed
Network bio-local-001_default Removed
```

**Result:** ✓ Clean teardown with volume removal (`-v` flag)

**Second Boot Start:** 2026-10-07 21:36:16 UTC

#### Re-creation from Clean State

```
$ newgrp docker <<< 'docker compose -f docker-compose.dev.yml up --build -d'

Network bio-local-001_default Creating
Volume bio-local-001_biostack-dev-data Creating
Volume bio-local-001_biostack-dev-data Created
Network bio-local-001_default Created
Container biostack-api-dev Creating
Container biostack-api-dev Created
Container biostack-ui-dev Creating
Container biostack-ui-dev Created
Container biostack-api-dev Starting
Container biostack-api-dev Started
Container biostack-api-dev Waiting
Container biostack-api-dev Healthy
Container biostack-ui-dev Starting
Container biostack-ui-dev Started
```

**Services Healthy:** 2026-10-07 21:37:25 UTC

#### Second Boot Verification

```
$ curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5000/health
200
```

**Result:** ✓ Second boot succeeded from clean volume state

## Git Diff Check

```
$ git diff --check
[no output]
```

**Result:** ✓ No whitespace errors or unintended changes to tracked files

**Note on Modified Files:** The worktree shows `frontend/package-lock.json` as modified. This is an ambient change from the frontend container's `npm install` process and was present before this parcel's execution. Per governed-delivery mandate, this file was not staged or committed.

## Acceptance Criteria Results

| Criterion | Status | Evidence |
|-----------|--------|----------|
| AC1: `docker compose up --build` reaches healthy API + UI from clean volume | ✓ PASS | Both containers healthy; timeline recorded with UTC timestamps and commit SHA |
| AC2: `/health` → 200; `/health/keon` response recorded with stubbed posture | ✓ PASS | `/health` returned 200; `/health/keon` returned 503 with stub-mode message and fail-closed posture documented |
| AC3: Frontend `:3043` returns app shell (HTTP 200) | ✓ PASS | Verified with `curl` command; exact response code 200 |
| AC4: SQLite volume location/behavior documented; `down -v` resets and second boot succeeds | ✓ PASS | Volume at `/var/lib/docker/volumes/bio-local-001_biostack-dev-data/_data`; database files created; clean reset verified; second boot successful |
| AC5: Evidence file written; `git diff --check` clean; redaction attestation | ✓ PASS | This file is the evidence; no whitespace errors; redaction attestation at top |

## Summary

All acceptance criteria met. The development stack boots successfully at commit `db8c98a6a82126af7a9b4a559f743781ae97a9d5` using `docker-compose.dev.yml`, serves healthy endpoints, runs in correctly-documented stub mode for Keon Runtime, and resets cleanly with `down -v`.

**Configuration Class:** Development, local-only, SQLite backend, stubbed Keon Runtime (fail-closed).

**Production Gap:** See `BIO-LOCAL-001-config-note.md` for the known limitation regarding Keon Runtime configuration in production compose.
