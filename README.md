# BioStack Mission Control

BioStack is an observational protocol-operations product for people who want a structured alternative to notes and spreadsheets when tracking compounds, check-ins, sources, and changing protocol context. It combines compound records, calculation tools, evidence-aware knowledge, timelines, and reviewed relationship signals without presenting itself as a prescriber, diagnostic system, or medical authority.

## Why it exists

Self-directed protocol tracking is usually fragmented across notes, spreadsheets, calculators, product pages, and research tabs. BioStack's product thesis is that users need a single, evidence-bounded operating surface that can preserve context over time:

- What was used, when, in what phase, and from what source?
- What changed before a user-observed outcome?
- What evidence, uncertainty, source-quality context, and high-risk warnings apply?
- Which relationships are reviewed, unsupported, or still unknown?

The product favors traceability and `Unknown` over unsupported inference. It is not designed to tell a user what to take, how to dose, how to inject, or what treatment decision to make.

## Current product surfaces

### Protocol operations

- Compound and protocol tracking, including manual entry and knowledge-base lookup.
- Timeline and phase context for starts, stops, changes, and user-recorded observations.
- Check-ins across subjective and operational measures, with longitudinal views intended to support correlation—not causation claims.
- Profile-backed workspaces for compounds, protocols, schedules, and billing.

### Research and decision support

- Public compound and evidence-library routes, plus public reconstitution, volume, unit-conversion, and protocol-analysis tools.
- Evidence tiers, citations, mechanism context, source-quality signals, and uncertainty states.
- Pathway-overlap and relationship analysis that surfaces reviewed context, conflicts, redundancy, and gaps for investigation.
- Warning-first handling for high-risk categories such as investigational peptides, compounded GLP-1s, prescription-only substances, SARMs/SERMs, gray-market products, and banned-in-sport substances.

### Calculation and visualization tools

- Reconstitution, volume, and unit-conversion math.
- Syringe-oriented visual guidance to make the calculation output easier to read.
- Protocol analyzer workflows that can be used before creating an account; saving, personal tracking, and private workspaces require authentication.

Calculators provide transparent math, not clinical direction. Users remain responsible for checking inputs and consulting an appropriate qualified professional.

## Revenue model and commercial position

BioStack is structured as a direct-to-consumer subscription SaaS product, with a provider-interest path rather than a deployed clinic offering.

| Tier | Price shown in product | Positioning | Current entitlement direction |
| --- | --- | --- | --- |
| Observer | Free | Entry point for exploration and basic tracking | Public tools, public evidence access, basic tracking, safety warnings, and limited free workflow access |
| Operator | $12/month | Primary revenue tier | Full protocol analysis, reviewed relationship and source-quality context, saved protocol workflow, and expanded tracking |
| Commander | $29/month | Intelligence upsell | Longitudinal observational patterns, cross-run comparison, ambiguity-analysis surfaces, and priority support |

The repository contains Stripe checkout, webhook, subscription-state, and customer-portal integrations for Operator and Commander. Stripe credentials and price identifiers are configuration-dependent and are blank in the checked-in application settings. This repository therefore demonstrates payment capability, **not evidence that production billing is configured, live, or revenue-generating**.

The near-term commercial wedge is an individual researcher/self-experimenter who outgrows a spreadsheet. The intended expansion path is provider workflows, but a multi-tenant clinic product is not represented here as a launched offering. Investors should treat provider access as discovery/lead generation, not booked B2B revenue.

## Product maturity: what is implemented vs. governed next steps

The application includes working product surfaces for tracking, public tools, authentication, subscription plumbing, knowledge ingestion, and safety-oriented gates. Its Protocol Intelligence governance foundation is more advanced than its runtime exposure:

- The source-first evidence pipeline, artifact evaluation, promotion reporting, and safety controls are present as offline/build-time workflows.
- Human review, cited provenance, evidence tiering, source quality, and forbidden-output checks are required before sensitive knowledge is promoted.
- The offline Protocol Intelligence evaluator is intentionally **not** a live, user-facing AI narrative or per-protocol recommendation service.

This distinction is material: BioStack should not be valued or marketed as an autonomous medical-AI product. Its investable thesis is governed protocol operations and evidence-aware observational intelligence, with runtime intelligence expansion subject to separate design, review, provenance, and safety gates.

## Architecture

BioStack uses a layered .NET backend and a Next.js frontend:

```text
Next.js 16 / React 19 frontend
        |
        v
.NET 10 Minimal API
  ├─ Application and domain services
  ├─ Infrastructure, authentication, billing, and persistence
  ├─ Stripe checkout, webhook, and customer-portal integration
  └─ Knowledge and safety gates
        |
        +-- PostgreSQL in production Docker Compose
        +-- SQLite for local development
        +-- One-shot Knowledge Worker for seed, refresh, and offline evaluation jobs
```

Key technology includes .NET 10, Entity Framework Core, PostgreSQL, SQLite, Next.js 16, React 19, TypeScript, Tailwind CSS, Recharts, Docker Compose, Stripe, magic-link email authentication, and optional OAuth providers. Redis is optional; the application falls back to in-memory caching when it is absent.

The knowledge architecture is source-first: approved sources are ingested and normalized deterministically, classified for evidence and risk, reviewed where required, and promoted into canonical product knowledge. A general-purpose model may assist only within a constrained, cited, guardrailed workflow; it is not an autonomous authority.

## Safety and compliance boundary

BioStack is for educational and observational use only. It may organize user-recorded data, show evidence context and uncertainty, surface source-quality or regulatory warnings, and suggest that a user discuss relevant observations with a qualified clinician.

**Not Medical Advice.** BioStack does not provide medical dosing recommendations or clinical diagnosis.

**Mathematical Logic Only.** Calculator outputs use pure mathematical formulas to make their calculations transparent; they are not clinical direction.

It does not provide diagnosis, prescribing, individualized dosing, injection instructions, treatment plans, substance sourcing, cycle design, start/stop/taper/escalation advice, or claims that investigational substances are safe or effective for human use. Safety warnings and high-risk guardrails are not paid features.

## Local development

### Prerequisites

- Docker Desktop with Docker Compose
- A copy of `.env.example` as `.env` for local environment values

### Run the development stack

```bash
docker compose -f docker-compose.dev.yml up --build
```

- API health endpoint: `http://localhost:5000/health`
- Frontend: `http://localhost:3043`
- Development persistence: SQLite mounted at `backend/data/`

Development can use an in-memory magic-link inbox when SMTP is not configured. Never use development defaults or placeholder secrets in production.

### Run the production-shaped local stack

```bash
docker compose up -d --build
```

This composition runs PostgreSQL, the API, the frontend, and a one-shot knowledge-worker service. Set the required secrets in `.env` first. To stop it and remove persisted Docker volumes:

```bash
docker compose down -v
```

## Repository guide

- `frontend/` — Next.js product and marketing surfaces.
- `backend/` — .NET API, application/domain/infrastructure layers, knowledge worker, and tests.
- `docs/architecture/` — source-first knowledge-engine and integration decisions.
- `docs/canon/` — product and safety canon for protocol intelligence.
- `docs/billing/` and `docs/commercialization/` — tier enforcement and commercialization planning.
- `research/` — source artifacts and offline protocol-intelligence inputs.
- `infra/` — deployment infrastructure.

## Investor and partner diligence

The repository can demonstrate a coherent product thesis, subscription model, governed knowledge architecture, and a safety-conscious technical foundation. It does not, by itself, establish live revenue, active subscribers, clinical validation, regulatory clearance, production Stripe configuration, or a launched B2B provider offering.

Before a funding, partnership, or go-live decision, validate the deployment environment, payment configuration and lifecycle, legal/privacy posture, content-review operations, evidence-source licensing, customer demand, retention, and the boundary between offline intelligence artifacts and user-facing functionality.
