# PR7A Video/Channel Staged Transcript Candidate Persistence Gate

## Purpose

This document defines the PR7A design gate for durable staged transcript candidate review in the BioStack video/channel knowledge-source ingestion lane.

PR7A is **contract-only**. It defines Application-layer persistence contract shape and invariants for staged non-canonical transcript candidate review records.

## Scope of PR7A

- Application contract record for staged transcript candidate review
- Application store interface contract for staged review records
- Application contract tests and invariant coverage
- Architecture boundary documentation

## Explicitly Out of Scope for PR7A

- Infrastructure persistence implementation
- DbContext / DbSet changes
- migrations
- API/endpoints
- DI wiring
- DB writes
- canonical KnowledgeEntries writes
- promotion workflow execution
- extraction
- summarization
- safety classification
- medical interpretation
- network calls, transcript fetching, YouTube API

## Mandatory Boundary: Non-Canonical Staged Candidates

Staged transcript candidates are **separate from canonical KnowledgeEntries**.

`review_approved_for_promotion` is a **state/eligibility signal only** and is **not promotion execution**.

Until a future, explicit promotion workflow is introduced and approved, staged transcript candidate review records remain non-canonical and must not be written as canonical knowledge entries.

## PR7A Contract Intent

Define the minimum safe Application contract surface for durable staged review:

- Non-canonical-only staged candidate review record shape
- Constrained lifecycle-compatible review state transitions
- Deterministic artifact identity and deterministic metadata shape
- No canonical write or promotion execution methods exposed in contract surface

## PR7B Eligibility

PR7B is the **first eligible lane** for:

- Infrastructure persistence implementation
- migration introduction
- DbContext/DbSet mapping decisions
- persistence round-trip behavior

Only after PR7A + PR7B validation should API lane work begin.

## API Sequencing Rule

API must wait until:

1. PR7A contract and invariants are validated
2. PR7B persistence implementation and migrations are validated

No API promotion, canonicalization, or persistence endpoint behavior is authorized in PR7A.
