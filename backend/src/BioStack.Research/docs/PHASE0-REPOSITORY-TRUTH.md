# Phase 0: Repository Truth — BioStack Scientific Research Sidecar

**Date:** 2026-08-02  
**Status:** Complete for foundation planning  
**Kickoff:** `docs/kickstarters/kickoff.md`  
**Inspection host:** Windows development workstation  

---

## Skills loaded

| Skill / plugin | Path | Why loaded |
|---|---|---|
| foreman-line plugin README | `D:\Repos\agent-skills\plugins\foreman-line\README.md` | Guardrail / coordination pipeline requirement |
| parcel-driven-development | `foreman-line/skills/parcel-driven-development/SKILL.md` | Multi-session contract-first delivery |
| goal skill | `foreman-line/skills/goal/SKILL.md` | Not fully executed; available for charter shaping |
| foreman-shaping | `foreman-line/skills/foreman-shaping/SKILL.md` | Available for Stage A shaping |
| Existing BioStack ADRs / canon | `docs/architecture/`, `docs/canon/`, `docs/biostack/` | Product and evidence boundary truth |

**Foreman runtime status:** CLI tools `foreman` / `foreman-agent` are **not installed** on PATH. The foreman-line plugin source exists locally under `agent-skills/plugins/foreman-line` but is not mediating shell/MCP calls in this session. Fallback: explicit human approval for sensitive actions; manual session log (see `IMPLEMENTATION-SESSION-NOTES.md`).

---

## 1. Solution and project structure

Repository root: `D:\Repos\BioStack`

### Backend (.NET 10)

| Project | Role |
|---|---|
| `BioStack.Api` | HTTP API, auth, Kompress admin endpoints, DI composition |
| `BioStack.Application` | Services, governance gates, review/promotion lifecycle, product contract |
| `BioStack.Domain` | Entities, enums, value objects |
| `BioStack.Contracts` | Request/response DTOs |
| `BioStack.Infrastructure` | EF Core persistence, `IKnowledgeSource`, Keon runtime clients |
| `BioStack.KnowledgeWorker` | Offline/online knowledge ingestion, source acquisition, trust gate, promotion |
| `BioStack.Cognition` / `CollectiveAdapter` | Stack review / collective orchestration |
| `BioStack.Research` | **New** — Python scientific research sidecar home (this work) |

Target framework: **net10.0** across backend projects.

### Layering

```text
Api -> Application -> Domain
Api -> Infrastructure -> Domain
KnowledgeWorker -> Domain + Infrastructure patterns
```

Domain does not depend on Ollama, ToolUniverse, CUDA, or Kompress types. Application depends on abstractions; Infrastructure/Api host adapters.

---

## 2. Database and persistence

- **EF Core 10** with dual providers:
  - SQLite for local/dev (`biostack.db`)
  - PostgreSQL via Npgsql for production-oriented runs
- Canonical knowledge store: `KnowledgeEntry` via `IKnowledgeSource` (`DatabaseKnowledgeSource`, `LocalKnowledgeSource`)
- Upsert dispositions: `Created` | `Updated` | `Unchanged`
- Graph store: `CompoundGraphStore` / relationship graph entities

### Core knowledge entity gaps (relevant to sidecar)

`KnowledgeEntry` today holds narrative dosing strings (`RecommendedDosage`, `StandardDosageRange`, `MaxReportedDose`, escalation lists) rather than first-class typed:

- published exposure regimens
- study records with population/endpoints
- adverse-event evidence with source class separation
- mechanism/pathway claims with species and evidence class

Phase 5 of the kickoff must **add** typed scientific entities rather than stuffing structured research into free-text fields only.

---

## 3. Knowledge ingestion and review lifecycle

### KnowledgeWorker pipeline

```text
load → schema validate → deserialize → normalize → TrustGate → canonicalize → (caller persists)
```

- **TrustGate:** Class A may set regulatory/safety/product-specific dosing; Class B enriches only; Class B-only strips Class A fields and forces `needsReview`.
- Source acquisition adapters already exist for:
  - PubChem PUG REST
  - PubMed E-utilities
  - ClinicalTrials.gov v2
  - FDA openFDA drug labels
  - DailyMed SPL list JSON
  - NIH ODS fact sheets
  - NCCIH manual review candidate workflow
- Promotion path: promotion exporters, import preview, manifest builders, research review queue.

### Application review/promotion (transcript candidates — reusable pattern)

States:

| State | Meaning |
|---|---|
| `pending_review` | Awaiting human action |
| `review_deferred` | Deferred |
| `review_rejected` | Rejected |
| `review_approved_for_promotion` | Eligible for promotion path |

Actions: `defer_review`, `reject_review`, `approve_for_promotion`.

Gates:

- `IEvidenceGate` — fail-closed evidence metadata checks + banned recommendation language
- `IProtocolIntelligenceGate` / promotion targets — human-review signals
- `UserFacingIntelligenceGate` / `DoctrineSanitizer` / `HighRiskCategoryGate` / `PolicyGate`
- Knowledge-ingest fences (KEO-74 legacy canonical ingest fence parcels)

**Integration decision (see ADR):** Sidecar output must stage as **candidate evidence packets** and enter the **existing review/promotion lifecycle**. Sidecar never writes canonical knowledge tables.

---

## 4. Provider and intelligence seams

| Seam | Status |
|---|---|
| `IKnowledgeSource` | Production knowledge read/write |
| KnowledgeWorker source acquisition adapters | Class A/B source lanes |
| `IYouTubeTranscriptMcpClient` | MCP-style intake (null client default) |
| Keon Runtime client | Receipts / runtime orchestration |
| Keon.Kompress 0.1.0 | In-process .NET package + admin HTTP |
| Collective / Cognition | Stack deliberation (not scientific retrieval) |
| Ollama | Used operationally on host; **no BioStack-owned `IInferenceProvider` abstraction yet** |
| ToolUniverse | **Not integrated** |

### Correct ToolUniverse integration seam (decision)

**Combination:**

1. New **BioStack-owned** `IScientificResearchProvider` (Application abstraction)
2. Infrastructure HTTP client → Python sidecar
3. Results stage into existing knowledge-intake / review controls (extend promotion candidate pattern; do **not** invent a second lifecycle)
4. KnowledgeWorker remains owner of Class A official-source acquisition where it already exists; sidecar augments deep literature, multi-tool research, and model-assisted extraction

Prefer reuse of:

- TrustGate field authority
- EvidenceGate + review states
- Source registry / provenance receipts
- Knowledge-ingest fences

---

## 5. Kompress (Keon)

| Item | Observed |
|---|---|
| Package | `Keon.Kompress` **0.1.0** on `BioStack.Api` |
| Surface | In-process `CompressionPipeline` + admin endpoints |
| Admin routes | `POST /api/v1/admin/kompress/compress`, `POST /api/v1/admin/kompress/retrieve` |
| Auth | `AdminOnly` |
| Content types | auto, text, json, log, diff, search-results, conversation |
| Retrieval | Hash-based via `ICompressionStore` + tenant context |
| System-message rule | **Not yet enforced as BioStack research policy** — must be explicit for research path |
| Sidecar access | Sidecar should **not** call admin browser APIs as its primary contract; needs service-auth internal contract (see ADR) |

---

## 6. Configuration, secrets, deployment

- API Docker image: `mcr.microsoft.com/dotnet/aspnet:10.0`, non-root `app` user, port 5000, healthcheck `/health`
- KnowledgeWorker has separate Dockerfiles
- SQLite data under `/app/data` (Kompress-writable path noted in SEC-CONTAINER-001)
- Secrets: configuration via appsettings / env (no secrets in this inspection)

---

## 7. GPU / host capability discovery

| Field | Value |
|---|---|
| Exact GPU model | **NVIDIA RTX 3500 Ada Generation Laptop GPU** |
| Architecture | Ada Lovelace (laptop) |
| Compute capability | **8.9** |
| VRAM | **12282 MiB** (~12 GB) |
| Driver | **596.41** |
| Reported CUDA (driver) | **13.2** |
| Power profile (observed) | Cap **62 W**, idle ~4 W |
| Host OS | Windows + WDDM |
| WSL | Default distro **Ubuntu-22.04**, WSL 2 |
| Docker Desktop | **4.84.0** / Engine **29.6.2** |
| Docker GPU runtime | **nvidia** runtime present; CDI `docker.com/gpu=webgpu` |
| Docker Desktop memory | **~3.8 GiB total** (severe container RAM limit; must raise for GPU worker PoC) |
| Docker Desktop CPUs | 20 |

### GPU decision implication

- GPU is an **optimization**, not a prerequisite.
- ~12 GB VRAM supports mid-size local models (e.g. Qwen 3.5 9B Q4) with careful single-job concurrency.
- Docker Desktop RAM at ~3.8 GB is **insufficient** for serious GPU containers until reconfigured.
- Prefer **Ollama on the Windows host** for local inference in PoC; GPU worker inside Linux container is secondary and must be validated with passthrough.

---

## 8. Ollama

| Field | Value |
|---|---|
| Version | **0.32.5** |
| Installed models of interest | **qwen3.5:9b** (`6488c96fa5fa`, 6.6 GB) |
| gemma4:12b | **Not installed** |
| Other local models | qwen2.5-coder:14b/3b, qwen3, deepseek-r1:8b, dolphin-llama3, etc. |
| Cloud-tagged models | kimi-k2.7-code:cloud, glm-5.2:cloud, nemotron-3-super:cloud, gemini-3-flash-preview — **must not be selected for local-first research without explicit approval** |

### qwen3.5:9b profile (from `ollama show`)

| Field | Value |
|---|---|
| Architecture | qwen35 |
| Parameters | 9.7B |
| Advertised context | 262144 |
| Embedding length | 4096 |
| Quantization | Q4_K_M |
| Capabilities | completion, vision, tools, thinking |
| License | Apache 2.0 |
| Runtime context allocated | **Not verified under load** — must not route long-context tasks from advertised max alone |
| Benchmark status | **Not yet BioStack-approved** |

---

## 9. Privacy boundary (current + required)

Product canon and evidence methodology prohibit personalized medical dosing authority.

Sidecar **must not** receive (initial implementation):

- User identity, account, age, sex, weight, symptoms, biomarkers, check-ins, personal protocols, notes, provider info, health documents

Sidecar **may** receive:

- Compound names / public identifiers
- Research questions
- Public disease/pathway names
- Citation IDs
- Evidence category filters
- Execution policy (GPU mode, compression mode, kill switches) without PII

User-vs-evidence comparison runs **inside BioStack** after promotion/review.

---

## 10. Product boundary tension (Phase 1 input)

Existing drafts:

- Canon: observational, no prescribe/dose/start/stop instructions
- Evidence grading: grades claims; does not tell users what to take
- ADR source-first engine: rejects individualized medical dosing
- Kickoff: permits **published evidence context** and **evidence comparison** language (Class A–C) while prohibiting personalized medical direction (Class D)

**Required:** formal Guidance Content Contract v1 before public dosing-context UX.

---

## 11. Development and production GPU modes

| Mode | Support intent |
|---|---|
| Dev workstation GPU | Available (RTX 3500 Ada 12GB) via host Ollama; container GPU optional after Docker RAM fix |
| Self-hosted prod GPU | Optional; not assumed |
| CPU-only prod | Required to work |
| Hosted model fallback | Explicit opt-in only; privacy-bounded payload |

---

## 12. Tests and architectural guards (observed)

- Test projects: Api, Domain, Application, KnowledgeWorker, ProtocolOperations verifier
- Forbidden-phrase / doctrine sanitizer patterns in governance
- Knowledge-ingest fence parcels under production-readiness initiative
- Offline boundary and receipt fail-closed work (BIO-RT-01)

---

## 13. Open items for later verification

- [ ] Container GPU passthrough end-to-end (Windows → WSL2 → Docker → CUDA framework)
- [ ] Actual Ollama runtime context / VRAM for qwen3.5:9b under BioStack prompts
- [ ] Whether gemma4:12b should be pulled after benchmark gap analysis
- [ ] Keon Kompress TTL, store path, multi-tenant isolation under research jobs
- [ ] ToolUniverse exact version pin after allowlisted skill smoke tests
- [ ] Production deployment GPU presence (unknown; design for absence)

---

## 14. Integration seam recommendation (summary)

```text
BioStack Application (IScientificResearchProvider)
        |
        | internal HTTP + service auth + async jobs
        v
Python Scientific Research Sidecar (BioStack.Research)
        |
        +--> CPU/Network: allowlisted ToolUniverse skills + official APIs
        +--> Local inference: BioStack router → Ollama adapter (host)
        +--> Kompress: BioStack-owned compression provider (service contract)
        |
        v
Candidate Evidence Packet (immutable raw + normalized claims)
        |
        v
Existing review / EvidenceGate / TrustGate / promotion lifecycle
```

See `docs/adr/ADR-001-scientific-research-sidecar.md` for full decisions.
