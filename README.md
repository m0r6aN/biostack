# BioStack

BioStack is a free, public library of what the research says about peptides and similar compounds — graded by evidence strength, for anyone deciding for themselves.

Tracking, analysis, and personalization sit downstream of that library as paid layers. The library itself is not the upsell.

## Why it exists

Most people meet this subject through a forum thread, a video, or someone who swears by it. The sourced literature exists, but it is scattered across abstracts, labels, regulatory notices, and paywalls, and it rarely arrives with its own uncertainty attached.

BioStack's thesis is that the useful unit is not another tracker — it is a public, source-graded record of what is actually known about a compound, including where the evidence is thin, presented so that the reader makes their own decision. The product answers:

- What does the reviewed literature say about this compound, and how strong is that evidence?
- What kind of source is this — controlled trial, observational study, label, regulatory action, mechanism?
- Where does the evidence stop, and what is simply unknown?
- What high-risk, regulatory, or sport-banned context applies?

The product favors traceability and `Unknown` over unsupported inference. It does not tell a user what to take, how to dose, how to inject, or what treatment decision to make, and it is not designed to become the next authority whose word is taken on trust — the citations are shown so they can be checked.

## What is free, and what is not

The upgrade boundary is a change in the question, not a withheld portion of the evidence:

> *"what is this compound"* → *"what about mine"* → *"what about across my runs."*

| Tier | Price | What it is for | Entitlements declared in the product contract |
| --- | --- | --- | --- |
| Observer | $0 | The evidence, open. The sourced compound library with evidence grades, source types, and mechanism context, plus the reconstitution, dose-volume, and unit-conversion calculators and the protocol analyzer. | Public evidence access, public tools, warning-first guardrails, basic tracking with an active-compound limit of 8 |
| Operator | $12/mo | Your protocol, rather than compounds in general. Check-ins, your own timeline, reviewed relationship and source-quality context for the compounds you are actually running. | Paid intelligence, reviewed relationship graph, source-quality tracker, GLP-1 observability pack |
| Commander | $29/mo | Every run, side by side. Past protocols and check-ins on one timeline, read across runs. | Everything in Operator, plus longitudinal comparison and side-effect ambiguity analysis |

Prices, plan codes, entitlements, and public route prefixes are declared in `contracts/product-contract.v1.json` (contract v1.0.0, effective 2026-07-13) and consumed by `BioStack.Application/Services/FeatureGate.cs`. That file is the source of truth; this table is a description of it. Safety warnings and high-risk guardrails are available on every tier and are never a paid feature.

Stripe checkout, webhook, subscription-state, and customer-portal integrations exist for Operator and Commander. Credentials and price identifiers are configuration-dependent and blank in checked-in settings. The repository demonstrates payment capability, **not evidence that production billing is configured, live, or revenue-generating**.

## Product surfaces

**Public, no account** — the evidence library (`/knowledge`) and per-compound dossiers (`/knowledge/[slug]`); the grading methodology (`/knowledge/methodology`); sourced insight articles (`/knowledge/insights/...`); the reconstitution, volume, and unit-conversion calculators (`/tools/reconstitution-calculator`, `/tools/volume-calculator`, `/tools/unit-converter`); the protocol analyzer (`/tools/analyzer`); onboarding (`/start`); and `/pricing`, `/how-it-works`, `/safety`, `/faq`, `/providers`. The contract also declares the aliases `/onboarding` → `/start` and `/map` → `/tools/analyzer`.

**With an account** — protocols and compound records, check-ins across subjective and operational measures, timeline and phase context for starts/stops/changes, profiles, the protocol console (`/protocol-console`, the post-sign-in default), and governance receipt views (`/governance/receipts`, `/receipts/[uri]`). Authentication is magic-link email plus passkeys (WebAuthn), with optional OAuth providers.

**Admin and operations** — research pipeline, staged reviews, taxonomy management, and compound curation under `/admin/research`.

## How knowledge gets in

The knowledge architecture is source-first. Approved sources are ingested and normalized deterministically, classified for evidence strength and risk, human-reviewed where the contract requires it, and only then promoted into canonical product knowledge. Evidence is graded and the grade is shown, including when it is weak — a compound whose support is mechanistic rather than clinical is labeled as such rather than rounded up.

A general-purpose model may assist only inside a constrained, cited, guardrailed workflow. It is not an autonomous authority, and it does not promote knowledge on its own.

The pipeline runs in `BioStack.KnowledgeWorker` (seed, refresh, and offline evaluation jobs) with a Python FastAPI research sidecar at `backend/research-sidecar/` (uv-managed, pytest, not part of `backend/BioStack.sln`) reached from admin-only endpoints via `IScientificResearchProvider`. The sidecar is disabled by default (`ScientificResearchSidecar:Enabled` is `false` in `appsettings.json`).

## Governance

BioStack's output is bounded by the Guidance Content Contract (`docs/guidance/biostack-guidance-content-contract.v1.md`), which classifies everything the product can say:

- **Class A** — published evidence context. Permitted, source-backed.
- **Class B** — evidence comparison. Reviewed templates and deterministic math only; no model-generated arithmetic.
- **Class C** — harm-reduction context. Approved templates, gated behind clinical safety copy review.
- **Class D** — personalized medical direction. **Prohibited.**

The contract is enforced in code, not by convention:

- `BioStack.Application/Governance/` — `DoctrineSanitizer`, `DoctrineRuleset`, `PolicyGate`, `UserFacingIntelligenceGate`, `HighRiskCategoryGate`, and output `Classification`.
- `BioStack.Domain/Governance/` and `BioStack.Infrastructure/Governance/` — the Governed Spine: an append-only hash-chained record (`SpineEntry`, `SpineChain`) with signed chain-head checkpoints (`SpineCheckpointService`). The checkpoint cadence worker, `SpineCheckpointCadenceHostedService`, is hosted in `BioStack.Api/Governance/`. The signing key does not live in the Spine database.
- `BioStack.Infrastructure/Keon/` — the Keon runtime adapter (`IKeonRuntimeClient`, `RuntimeReceiptFactory`, `ReceiptClass`) that anchors decision receipts.
- `BioStack.Cognition/` and `BioStack.Cognition.CollectiveAdapter/` — the deliberation plane (Stack Review Board, deliberation translator, collective orchestration), kept in separate assemblies from the effect path. Deliberation produces an envelope for downstream handling rather than text rendered straight to a user.

**Governance absence is a boot failure, not a silent degradation.** `KeonRuntimeDependencyInjection` refuses to start in a Production environment when the runtime is stubbed or has no base URL, unless `KeonRuntime:AllowStubInProduction` is explicitly set to acknowledge running ungoverned; `KeonRuntime:StubAllowAll`, which would bypass fail-closed policy checks, is rejected in Production outright. Runtime dependency health is exposed at `/health/keon`.

## Implemented vs. gated

The governance foundation is more built out than its user-facing exposure, and the two should not be conflated:

- Gates, the Spine, receipt anchoring, and the source-first pipeline are implemented and exercised in tests.
- Receipt views exist as authenticated in-product surfaces; they are not public routes.
- Outside Production, the Keon runtime may run stubbed, which is a development convenience and not a claim about deployed governance.
- Public Class B and Class C surfaces remain blocked behind human-only ratification gates recorded in `docs/guidance/RATIFICATION.md` (product canon reconciliation, legal/policy reconciliation, governance manual update, clinical safety copy review, public surface enablement).
- The offline protocol-intelligence evaluator is intentionally **not** a live, user-facing narrative or per-protocol recommendation service.
- `/privacy` and `/terms` are stubs pending approved policy. **No data-custody, storage-location, or privacy guarantee may be claimed on any public surface until they are approved.** BioStack's persistence is server-side (PostgreSQL or SQLite by configuration); it is not local-first, and nothing in this repository should be described that way.

This distinction is material: BioStack should not be valued or marketed as an autonomous medical-AI product. Its thesis is a governed, source-graded evidence surface with observational tooling downstream, and every expansion of runtime intelligence is subject to separate design, review, provenance, and safety gates.

## Safety and compliance boundary

BioStack is for educational and observational use only. It may organize user-recorded data, show evidence context and uncertainty, surface source-quality or regulatory warnings, and suggest that a user discuss relevant observations with a qualified clinician.

**Not Medical Advice.** BioStack does not provide clinical diagnosis, prescribing, medical dosing recommendations, individualized dosing, injection instructions, treatment plans, substance sourcing, cycle design, start/stop/taper/escalation advice, or claims that investigational substances are safe or effective for human use. High-risk categories — investigational peptides, compounded GLP-1s, prescription-only substances, SARMs/SERMs, gray-market products, and banned-in-sport substances — are handled warning-first.

**Mathematical Logic Only.** Calculator outputs use pure mathematical formulas so the arithmetic is transparent and checkable; they are not clinical direction. Users remain responsible for checking inputs and consulting an appropriate qualified professional.

## Architecture

```text
Next.js 16 / React 19 / TypeScript / Tailwind CSS 4 frontend (Recharts, React Flow)
        |
        v
.NET 10 Minimal API (Entity Framework Core)
  ├─ Application and domain services
  ├─ Governance: doctrine sanitizer, policy gate, user-facing intelligence gate,
  │              high-risk category gate, Governed Spine (+ signed checkpoints)
  ├─ Keon runtime adapter: decision receipts, fail-closed in Production
  ├─ Cognition plane (Stack Review Board) in separate assemblies
  ├─ Authentication: magic-link email, passkeys (WebAuthn), optional OAuth
  ├─ Billing: Stripe checkout, webhook, and customer portal
  └─ Caching: Redis when configured, in-memory otherwise
        |
        +-- PostgreSQL in the production-shaped Docker Compose
        +-- SQLite for local development
        +-- One-shot Knowledge Worker for seed, refresh, and offline evaluation
        +-- Python research sidecar (FastAPI, admin-only, out-of-solution)
```

Two persistence providers are supported and selected by `Database:Provider`. Anything hashed or serialized must round-trip identically through both — `DateTimeKind` does not survive SQLite, and PostgreSQL truncates .NET's 100-nanosecond ticks to microseconds. Both affect the Spine hash chain.

Migrations in this repository are **intentionally hand-written**; do not run `dotnet ef migrations add`. See `BioStack.Api/ProductionMigrationBaselineConfiguration.cs`.

## Local development

### Prerequisites

- Docker Desktop with Docker Compose (the development composition runs the API on the .NET 10 SDK image and the frontend on Node 22, so neither needs to be installed on the host)
- A copy of `.env.example` as `.env` for local environment values

### Run the development stack

```bash
docker compose -f docker-compose.dev.yml up --build
```

- API health endpoint: `http://localhost:5000/health`
- Governance runtime health: `http://localhost:5000/health/keon`
- Frontend: `http://localhost:3043`
- Development persistence: SQLite in the Docker named volume `biostack-dev-data` (mounted over `/app/data` inside the API container; the host `backend/data/` directory is not where the file lives)

To reset development data:

```bash
docker compose -f docker-compose.dev.yml down -v
```

The API runs in the Development environment here, so the Keon runtime is stubbed and the in-memory magic-link inbox is used. Outside Development, magic links are delivered by Azure Communication Email or SMTP when either is configured, and fall back to the in-memory inbox only when neither is configured and the frontend is local. Never use development defaults or placeholder secrets in production.

### Run the production-shaped local stack

```bash
docker compose up -d --build
```

This composition runs PostgreSQL, the API, the frontend, and a one-shot knowledge-worker service. Set the required secrets in `.env` first; the API service fails fast on any that are missing.

The API runs in the **Production** environment in this composition, so the governance boot check applies: it must reach a live Keon runtime (`KeonRuntime__BaseUrl` and `KeonRuntime__LiveMode=true`) or be explicitly told to run ungoverned (`KeonRuntime__AllowStubInProduction=true`), otherwise startup throws. These values must be present in the API container's environment; as of this writing `docker-compose.yml` does not pass `KeonRuntime__*` variables through from `.env` and `.env.example` does not list them, so add them to the `biostack-api` `environment` block before running this composition.

To stop it and remove persisted Docker volumes:

```bash
docker compose down -v
```

## Tests and CI

```bash
# Backend (.NET 10 SDK) — five test projects under backend/tests/
cd backend && dotnet test

# Frontend (Node 22)
cd frontend && npm ci --include=dev && npm run lint && npm run test

# Research sidecar (uv)
cd backend/research-sidecar && uv sync --all-extras && uv run pytest
```

GitHub Actions workflows live in `.github/workflows/`: `deploy.yml`, `secret-scan.yml`, `sonarcloud.yml`, `source-acquisition-worker.yml`, `protocol-operations-offline-verification-kit.yml`, and `structural-evaluation-report.yml`.

## Repository guide

- `frontend/` — Next.js product and marketing surfaces.
- `backend/src/` — .NET API, application/domain/infrastructure layers, cognition assemblies, and the knowledge worker (`backend/BioStack.sln`).
- `backend/tests/` — API, Application, Domain, KnowledgeWorker, and export-bundle-verifier test projects, plus shared fixtures.
- `backend/research-sidecar/` — Python FastAPI research sidecar (uv, pytest).
- `.github/workflows/` — CI, deployment, secret scanning, and offline verification workflows.
- `contracts/` — the product contract: plans, prices, entitlements, public routes.
- `docs/guidance/` — Guidance Content Contract, ratification record, enforcement findings.
- `docs/canon/` — product and safety canon for protocol intelligence.
- `docs/architecture/` and `docs/knowledge-engine/` — source-first knowledge-engine and integration decisions.
- `docs/billing/` and `docs/commercialization/` — tier enforcement and commercialization planning.
- `docs/legal/`, `docs/security/`, `docs/operations/` — policy, security, and operational records.
- `research/` — source artifacts, review decisions, and offline protocol-intelligence inputs.
- `infra/` — deployment infrastructure.

Public-surface copy is governed by `.audit/POSITIONING-ARTIFACTS-v2.md`. Copy constraints that apply repo-wide: no safety or outcome promise, no expertise-gating language, no data-custody claim until the privacy policy is approved, and no claim that any tier's evidence access is withheld.

## Investor and partner diligence

The repository demonstrates a coherent product thesis, a subscription model, a governed and source-first knowledge architecture, and enforcement of its own content boundary in code. It does not, by itself, establish live revenue, active subscribers, clinical validation, regulatory clearance, production Stripe configuration, an approved privacy posture, or a launched B2B provider offering. The provider path is discovery and lead generation, not booked B2B revenue.

Before a funding, partnership, or go-live decision, validate the deployment environment, the Keon runtime's live-mode configuration in the deployed environment, payment configuration and lifecycle, legal and privacy posture, content-review operations, evidence-source licensing, customer demand, retention, and the boundary between offline intelligence artifacts and user-facing functionality. The commands in [Tests and CI](#tests-and-ci) are the fastest way to check the claims in this document against the code.
