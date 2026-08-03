# Implementation Session Notes — Scientific Research Sidecar

**Date:** 2026-08-02  
**Operator mode:** Human-in-the-loop (Warp / agent worker)  
**Kickoff:** `docs/kickstarters/kickoff.md`

---

## Skills loaded

| Skill | Why |
|---|---|
| foreman-line README | Guardrail discovery |
| parcel-driven-development | Contract-first multi-parcel delivery |
| Existing BioStack ADRs / canon / evidence methodology | Product boundary truth |

---

## Foreman-line / Foreman audit

| Field | Value |
|---|---|
| Foreman-line or Foreman status | **Plugin source present; CLI unavailable** |
| Version | Plugin alpha 0.1.0 (source tree); no `foreman` binary on PATH |
| Configuration used | None (not mediating) |
| Approval mode | Human / session manual approval |
| Sensitive actions requested | Dependency install via `uv` (declared minimal set only); no ToolUniverse |
| Sensitive actions approved | Scaffold + docs within research-sidecar; Application abstraction stubs |
| Sensitive actions denied | n/a |
| Tool calls mediated | 0 (Foreman not active) |
| Known gaps | No MCP mediation, no SQLite audit log from Foreman, no command risk scoring |
| Fallback approval process | Kickoff rules + manual session log; ask before ToolUniverse pin, model downloads, infra changes |

---

## What was implemented this session

1. Phase 0 repository truth document  
2. ADR-001 architecture decisions  
3. Guidance Content Contract v1 (draft for ratification)  
4. Python sidecar scaffold (`biostack_research_sidecar`)  
   - Health, capabilities, allowlisted workflows  
   - Job submit/status/result/cancel  
   - Privacy field rejection  
   - Global/per-workflow kill switches  
   - ToolUniverse **disabled** by default  
5. JSON Schema for research request  
6. GPU path validation script  
7. .NET Application abstractions (`IScientificResearchProvider`, model/compression interfaces)  
8. Parcel index for follow-on work  

---

## ToolUniverse pin follow-up (same initiative)

| Item | Value |
|---|---|
| Pin | `tooluniverse==1.4.0` (base only, not `[all]`) |
| Wheel SHA256 | `506c3b3112714df38fcfcb2a7abe29318f53a55749fde4f87a5f75b54ebe5148` |
| Allowlist | `config/tooluniverse_allowlist.v1.json` |
| Adapter | allowlist-bound `ToolUniverseAdapter` |
| Runtime enable | `BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=true` (default off) |

## Explicitly not done (by design)

- Full live literature / AE / pathway workflow sequences  
- Ollama inference execution (probe only)  
- Kompress research-path integration  
- EF entities for published regimens  
- Public UX for dosing-context comparison  
- Docker image production publish  
- Foreman mediation  

---

## Verification commands

```bash
cd backend/research-sidecar
uv sync --extra dev --extra tooluniverse
uv run pytest
uv run --extra tooluniverse python scripts/smoke_tooluniverse_pin.py
# optional live PubChem:
# uv run --extra tooluniverse python scripts/smoke_tooluniverse_pin.py --live
```

```bash
# optional
pwsh scripts/validate_gpu_path.ps1
```

---

## Next safe actions

1. Human ratify guidance contract (or amend).  
2. Human approve ToolUniverse exact version for allowlisted skills only.  
3. Implement Infrastructure HTTP client for `IScientificResearchProvider`.  
4. Add durable job store + async worker.  
5. Benchmark `qwen3.5:9b` (digest `6488c96fa5fa`) for structured extraction.  
6. Increase Docker Desktop RAM before container GPU experiments.
