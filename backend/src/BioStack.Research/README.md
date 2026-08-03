# BioStack.Research — Scientific Research Sidecar

Bounded Python sidecar for BioStack compound research. Exposes **BioStack-owned** research operations only. Does **not** expose unrestricted ToolUniverse tool execution.

## Status

Foundation scaffold (Phase 0–2 boundary). ToolUniverse is **not** installed by default.

See:

- `docs/kickstarters/kickoff.md` — full program
- `docs/PHASE0-REPOSITORY-TRUTH.md` — inspection record
- `docs/adr/ADR-001-scientific-research-sidecar.md` — architecture decisions
- `docs/guidance/biostack-guidance-content-contract.v1.md` — output classes A–D

## Run locally

```bash
cd backend/src/BioStack.Research
uv sync --all-extras
uv run python -m biostack_research_sidecar
```

Health:

```text
GET http://127.0.0.1:8080/health
GET http://127.0.0.1:8080/internal/v1/capabilities/gpu
GET http://127.0.0.1:8080/internal/v1/capabilities/inference
```

## Docker

```bash
docker build -t biostack-research-sidecar .
docker run --rm -p 8080:8080 biostack-research-sidecar
```

## Security defaults (fail closed)

| Setting | Default | Notes |
|---|---|---|
| `BIOSTACK_RESEARCH_HOST` | `127.0.0.1` | Loopback only. Non-loopback **requires** a service token. |
| `BIOSTACK_RESEARCH_SERVICE_TOKEN` | empty | Empty token is **not** open-auth; protected routes return 401 unless insecure dev auth is enabled on loopback. |
| `BIOSTACK_RESEARCH_ALLOW_INSECURE_DEV_AUTH` | `false` | Local test escape hatch only; forbidden with non-loopback host. |
| `BIOSTACK_RESEARCH_HOSTED_FALLBACK_ENABLED` | `false` | Hosted inference requires this **and** full request authorization flags. |

## Kill switches

| Env | Effect |
|---|---|
| `BIOSTACK_RESEARCH_GLOBAL_KILL_SWITCH=true` | Reject new research jobs |
| `BIOSTACK_RESEARCH_WORKFLOW_KILLS=resolve_compound_identity,research_adverse_events` | Per-workflow disable |
| `BIOSTACK_RESEARCH_GPU_ENABLED=false` | Force CPU-only execution mode reporting |
| `BIOSTACK_RESEARCH_OLLAMA_BASE_URL` | Ollama base URL (default `http://127.0.0.1:11434`) |
| `BIOSTACK_RESEARCH_SERVICE_TOKEN` | Required bearer token for protected routes |

## Privacy

Sidecar accepts public compound/research parameters only. No user health profiles or personal protocols.

| Control | Rule |
|---|---|
| `subject_name` | Compound-identifier shape (≤128 chars, chemical-name charset, ≤8 tokens) |
| `known_identifiers` | Whitelisted registry keys only (`cid`, `chembl_id`, `uniprot`, `pmid`, …) |
| Request body | Top-level field allowlist + nested health-key denylist + free-text value scan |
| `data_classification` | Only `public_scientific` / `public_metadata` |

## Jobs

`POST /internal/v1/research/jobs` returns **202** with `status=queued` immediately. Work runs off the event loop.

| Setting | Default | Effect |
|---|---|---|
| `BIOSTACK_RESEARCH_MAX_CONCURRENT_RESEARCH_JOBS` | `4` | Excess submits → `429 max_concurrent_jobs` |
| `BIOSTACK_RESEARCH_JOB_TTL_SECONDS` | `86400` | Terminal jobs purged from the in-memory store |
| Request `maximum_execution_time_seconds` / `execution.maximum_execution_duration_seconds` | `600` | Tighter of the two is the worker timeout |

Poll `GET .../jobs/{id}` / `.../result`. `/health` stays responsive and reports `jobs_in_flight`.

## ToolUniverse pin

| Item | Value |
|---|---|
| Package | `tooluniverse==1.4.0` |
| Install | `uv sync --extra tooluniverse` (base only, **not** `[all]`) |
| Receipt | `docs/pins/TOOLUNIVERSE-PIN.md` |
| Allowlist | `src/biostack_research_sidecar/data/tooluniverse_allowlist.v1.json` (single canonical copy; override only via `BIOSTACK_RESEARCH_TOOLUNIVERSE_ALLOWLIST_PATH`) |
| Enable at runtime | `BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=true` |

Capability probe:

```text
GET /internal/v1/capabilities/tooluniverse
```

## .NET provider

Register via `AddScientificResearchProvider` (Api Program). Default:

```json
"ScientificResearchSidecar": {
  "Enabled": false,
  "BaseUrl": "http://127.0.0.1:8080",
  "ServiceToken": "",
  "TimeoutMs": 30000
}
```

## Next (human-gated)

1. Complete guidance contract ratification sign-offs (`docs/guidance/RATIFICATION.md`).
2. Deploy sidecar + set `ScientificResearchSidecar:Enabled=true` where intended.
3. Benchmark `qwen3.5:9b` for approved extraction tasks.
4. Kompress research-path tenant/job isolation.
