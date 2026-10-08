# BIO-PAIRWISE-001 — P0 Relationship Substrate Inventory

Tested SHA: `c6205fae91357e30a1936f9e2318a84dbf251774`
Census generated (UTC): `2026-10-07T23:58:17Z`

Census only. No code changed, no schema edited, no packet authored, no database touched. Every
line below is a copied token with a `file:line` citation. Zero sentences in this document assert
that any specific compound interaction exists.

## 1. Component inventory (`file:line`)

| component | file | line(s) |
|---|---|---|
| Relationship packet schema (root) | `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json` | 1 |
| `relationship.relationshipType` enum (14 values) | same | 76-94 |
| `relationship.directionality` enum | same | 95-98 |
| `relationship.evidenceTier` enum | same | 104-107 |
| `relationship.assertionClass` enum (7 values) | same | 164-175 |
| `relationship.sourceRefs` (`sourceRefArray`) | same | 103 |
| `sourceSnapshot` def (sourceId/sourceType/authorityTier/title/publisher/url/doi/pmid/publishedAt/accessedAt) | same | 234-253 (approx; `sourceSnapshot` opens at 234) |
| Protocol-intelligence relationship taxonomy (12 `relationshipTypes` ids, distinct vocabulary from the schema's `relationshipType` enum) | `research/protocol-intelligence/relationship-taxonomy.json` | 7-62 |
| `CompoundGraphArtifact` entity | `backend/src/BioStack.Domain/Entities/Graph/CompoundGraphArtifact.cs` | 16-47 |
| `CompoundGraphRelationship` entity | `backend/src/BioStack.Domain/Entities/Graph/CompoundGraphRelationship.cs` | 11-54 |
| `CompoundGraphFinding` entity | `backend/src/BioStack.Domain/Entities/Graph/CompoundGraphFinding.cs` | 8-35 |
| `GraphRelationshipType` canon vocabulary (10 constants, distinct from both the schema's 14-value enum and the taxonomy's 12 ids) | `backend/src/BioStack.Domain/Entities/Graph/GraphRelationshipType.cs` | 14-23 |
| `GraphIntelligenceService` (read surface) | `backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs` | 26-146 |
| `ICompoundGraphStore` interface | `backend/src/BioStack.Infrastructure/Knowledge/ICompoundGraphStore.cs` | 13-44 |
| `CompoundGraphStore.PublishAsync` (write path) | `backend/src/BioStack.Infrastructure/Knowledge/CompoundGraphStore.cs` | 16-50 |
| `CompoundGraphBuilder` (reads relationship packets) | `backend/src/BioStack.KnowledgeWorker/Pipeline/CompoundGraphBuilder.cs` | 7-30 (interface), 20-34 (ctor/Build signature) |
| `CompoundGraphBuilder.MapRelationshipType` (packet `relationshipType` → `CompoundGraphEdgeType`) | same | 595-613 |
| `CompoundGraphEdgeType` enum (17 values, built-in `CompoundGraphBuilder` edge-type vocabulary) | `backend/src/BioStack.KnowledgeWorker/Pipeline/Graph/CompoundGraphEdge.cs` | 23-41 |
| `CompoundGraphPersistenceMapper.Map` (edge → DB entity projection) | `backend/src/BioStack.KnowledgeWorker/Pipeline/Graph/CompoundGraphPersistenceMapper.cs` | 17-121 |
| `CompoundGraphPersistenceMapper.RelationshipTypeMap` (8-entry dictionary, `CompoundGraphEdgeType` → `GraphRelationshipType`) | same | 23-32 |
| `RelationshipPacketAuthorizer` (authority-tier policy for packet edges) | `backend/src/BioStack.KnowledgeWorker/Pipeline/Graph/RelationshipPacketAuthorizer.cs` | 1-174 |
| `RunMode.Research` enum member + doc comment | `backend/src/BioStack.KnowledgeWorker/Config/RunMode.cs` | 24-27 |
| `WorkerRunModePolicy.IsDatabaseFree` (includes `RunMode.Research`) | same | 59-65 |
| `WorkerOptions.ResearchRelationshipPacketPath` | `backend/src/BioStack.KnowledgeWorker/Config/WorkerOptions.cs` | 122 |
| `WorkerOptions.ResearchRelationshipPacketDirectory` | same | 127 |
| `ResearchJob.LoadRelationshipPackets` call site | `backend/src/BioStack.KnowledgeWorker/Jobs/ResearchJob.cs` | 94 |
| `ResearchJob` → `_compoundGraphBuilder.Build(...)` call site | same | 162 |
| `ResearchJob` writes `compound-graph.json` to disk | same | 163 |
| `IngestionWorker` dispatches `RunMode.Research` → `IResearchJob` | `backend/src/BioStack.KnowledgeWorker/Workers/IngestionWorker.cs` | 87 |
| Migration: `CompoundGraphArtifacts`/`CompoundGraphRelationships`/`CompoundGraphFindings` tables | `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260626100000_AddCompoundGraphArtifacts.cs` | 19, 40, 72 |
| Migration: Postgres type repair for graph tables | `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260727090000_RepairCompoundGraphPostgresTypes.cs` | 1 |
| Test fixture: `relationship-packet.sample.json` | `backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/relationship-packet.sample.json` | 1 |
| Test fixture: `relationship-packet.synergy-chain.sample.json` | `backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/relationship-packet.synergy-chain.sample.json` | 1 |
| Tests exercising the graph builder | `backend/tests/BioStack.KnowledgeWorker.Tests/CompoundGraphTests.cs` | 1 |
| Tests exercising the authorizer | `backend/tests/BioStack.KnowledgeWorker.Tests/RelationshipPacketAuthorizerTests.cs` | 1 |
| Tests exercising `CompoundGraphPersistenceMapper` + `PublishAsync` | `backend/tests/BioStack.KnowledgeWorker.Tests/CompoundGraphPersistenceMapperTests.cs` | 18-77 |
| Tests exercising `GraphIntelligenceService`/`CompoundGraphStore` via `PublishAsync` | `backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs` | 42, 84, 254 |
| Tests exercising the intelligence endpoints via `PublishAsync` | `backend/tests/BioStack.Api.Tests/Integration/IntelligenceEndpointsIntegrationTests.cs` | 82 |
| Tests exercising the safety-gate endpoint via `PublishAsync` | `backend/tests/BioStack.Api.Tests/Integration/IntelligenceSafetyGateIntegrationTests.cs` | 94 |
| Input directory named by the spec | `research/input/relationships/` | contains only `.gitkeep` (confirmed empty; matches spec's framing) |

## 2. Connectivity verdict — does a packet placed in `research/input/relationships/` reach...

### (a) the graph store (`CompoundGraphStore` / DB)? **not connected** (in production code paths)

- `ResearchJob.LoadRelationshipPackets` (`backend/src/BioStack.KnowledgeWorker/Jobs/ResearchJob.cs:94,419`)
  reads files from `WorkerOptions.ResearchRelationshipPacketPath`/`ResearchRelationshipPacketDirectory`
  (`backend/src/BioStack.KnowledgeWorker/Config/WorkerOptions.cs:122,127`) and passes them into
  `_compoundGraphBuilder.Build(...)` (`ResearchJob.cs:162`), which writes the result to
  `compound-graph.json` on disk (`ResearchJob.cs:163`).
- `RunMode.Research`'s own doc comment states it "emits review/report artifacts without connecting
  to or writing a database" (`backend/src/BioStack.KnowledgeWorker/Config/RunMode.cs:24-27`), and
  `WorkerRunModePolicy.IsDatabaseFree` lists `RunMode.Research` as database-free
  (`RunMode.cs:59-65`), consumed by `Program.cs:65,124,149` to skip the Postgres connectivity
  check entirely in Research mode.
- The persistence path that would carry `compound-graph.json` into `CompoundGraphStore` exists —
  `CompoundGraphPersistenceMapper.Map` + `ICompoundGraphStore.PublishAsync`
  (`backend/src/BioStack.KnowledgeWorker/Pipeline/Graph/CompoundGraphPersistenceMapper.cs:17-121`,
  `backend/src/BioStack.Infrastructure/Knowledge/CompoundGraphStore.cs:16-50`) — and the mapper's
  own doc comment names itself as "the persistence path a DB-connected worker run (or an importer)
  calls" (`CompoundGraphPersistenceMapper.cs:14-15`).
- **Break point**: `grep -rn "PublishAsync" --include=*.cs backend/src/ backend/tests/` finds
  `PublishAsync` called only from test files (`CompoundGraphIntelligenceTests.cs:254`,
  `IntelligenceEndpointsIntegrationTests.cs:82`, `IntelligenceSafetyGateIntegrationTests.cs:94`,
  `CompoundGraphPersistenceMapperTests.cs:77`). `grep -rn "CompoundGraphPersistenceMapper"
  --include=*.cs backend/src/ backend/tests/` finds it referenced only in its own definition file
  and in `CompoundGraphPersistenceMapperTests.cs`. No job, controller, or `Program.cs` entry point
  under `backend/src/` calls either. A relationship packet therefore reaches a disk file
  (`compound-graph.json`) but not the database-backed `CompoundGraphStore`.

### (b) any `KnowledgeEntry` field? **not connected**

- `CompoundGraphRelationship`/`CompoundGraphFinding` (the entities a relationship packet would
  populate, via path (a)) are their own tables (`CompoundGraphArtifacts`,
  `CompoundGraphRelationships`, `CompoundGraphFindings` —
  `.../Migrations/20260626100000_AddCompoundGraphArtifacts.cs:19,40,72`), not columns on
  `KnowledgeEntry`.
- The only code path that writes `KnowledgeEntry.DrugInteractions` is
  `SubstanceCanonicalizer.ToKnowledgeEntry`: `DrugInteractions = record.Interactions`
  (`backend/src/BioStack.KnowledgeWorker/Pipeline/SubstanceCanonicalizer.cs:73`). `record` here is
  a compiled `SubstanceRecord` produced from an **evidence** packet (not a relationship packet) by
  `EvidencePacketSubstanceRecordCompiler.CompileDraft`, which hardcodes
  `["interactions"] = new JsonArray()` unconditionally
  (`backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketSubstanceRecordCompiler.cs:44`).
- **Break point**: relationship packets never flow into `SubstanceCanonicalizer` or
  `EvidencePacketSubstanceRecordCompiler` at all (those consume evidence packets only), so there is
  no code path from `research/input/relationships/` to any `KnowledgeEntry` field. Separately, and
  independently, the one `KnowledgeEntry` field that does carry interaction-shaped data
  (`DrugInteractions`) is fed from a source that is hardcoded empty at
  `EvidencePacketSubstanceRecordCompiler.cs:44`, so it is empty regardless of what any packet
  (evidence or relationship) contains.

### (c) any API response? **connected, conditionally, pending (a)**

- `GraphIntelligenceService.GetRelationshipsForCompoundAsync` and `GetCompatibilityAsync`
  (`backend/src/BioStack.Application/Services/Intelligence/GraphIntelligenceService.cs:34-56,59-118`)
  read `ICompoundGraphStore.GetActiveArtifactAsync`/`GetRelationshipsForCompoundAsync`/
  `FindRelationshipAsync` and map results into `CompoundRelationshipsResponse`/
  `CompoundCompatibilityResponse`.
- `IntelligenceEndpoints.cs` wires `GET /compounds/{compound}/relationships` (line 33) and
  `GET /compatibility` (line 36) to those service methods.
- `InteractionIntelligenceService.TryEvaluateFromGraphAsync`
  (`backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs:277-299`) also reads
  `ICompoundGraphStore.FindRelationshipAsync`/`GetActiveArtifactAsync`, ahead of the
  `CompoundInteractionHint` repository lookup in call order (`InteractionIntelligenceService.cs:178`
  AvoidWith check, `278-299` graph check, `185` hint-repository check, `198` `DrugInteractions`
  field check — in that source-code order inside `EvaluatePairAsync`).
- This read path is code-complete and would serve a published graph artifact to these API
  responses. It is gated entirely on break point (a): `GetActiveArtifactAsync` returns `null` when
  no artifact has ever been published, and no production caller publishes one, so in the current
  repository state these endpoints return `Source: Fallback` / `RelationshipType:
  unknown_or_insufficient_evidence` (`GraphIntelligenceService.cs:95` /
  `GraphRelationshipType.UnknownOrInsufficientEvidence`, `GraphRelationshipType.cs:23`) for every
  compound pair, not because the read wiring is broken, but because nothing populates the store it
  reads from.

### (d) any rendered surface? **not connected in production** (connected only in a dev-only fixture path)

- The only UI component that renders compound-graph relationship data is
  `CompoundRelationshipsSection.tsx`, used by `CompoundDossierExperience.tsx`
  (`grep -rln "CompoundRelationshipsSection" frontend/src --include=*.tsx`, excluding tests, returns
  exactly these two files).
- It calls `fetchCompoundGraph('')` (`frontend/src/components/knowledge/CompoundRelationshipsSection.tsx:62`),
  imported from `frontend/src/lib/research/loader.ts:80`, which requests
  `` `/api/research/artifacts?artifact=${...}` `` (`frontend/src/lib/research/loader.ts:40`).
- The component's own inline comment states: "The artifact API is dev-only (`/api/research/artifacts`
  returns 404 in production)... In production, the 404 is caught below and the section renders null
  silently." (`CompoundRelationshipsSection.tsx:59-61`).
- **Break point**: the rendered surface does not call the production
  `/compounds/{compound}/relationships` or `/compatibility` endpoints described in (c) at all; it
  reads a separate dev-only artifact-file endpoint that 404s (and is documented as 404ing) in
  production. A relationship packet therefore has no path to any rendered surface in production
  under the current wiring, independent of break point (a).

## 3. Authoritative-mechanism determination (criterion 3)

Two mechanisms are named by the spec: `EvidencePacketSubstanceRecordCompiler`'s hardcoded empty
`interactions` array, and the relationship-packet path.

- `EvidencePacketSubstanceRecordCompiler.CompileDraft` writes `["interactions"] = new JsonArray()`
  unconditionally, on every call, regardless of packet content
  (`EvidencePacketSubstanceRecordCompiler.cs:44`). This value flows, via
  `SubstanceCanonicalizer.cs:73`, into `KnowledgeEntry.DrugInteractions`
  (`backend/src/BioStack.Domain/Entities/KnowledgeEntry.cs:34`), which is read by
  `InteractionIntelligenceService.cs:198` (`HasNamedInteraction(compoundA.DrugInteractions, ...)`).
  This field is therefore always empty at the point `HasNamedInteraction` is evaluated, for every
  compound compiled through this pipeline, by construction — not because of missing packet data.
- The relationship-packet path (`CompoundGraphBuilder` → `compound-graph.json` →
  `CompoundGraphPersistenceMapper` → `ICompoundGraphStore.PublishAsync` →
  `GraphIntelligenceService`/`InteractionIntelligenceService.TryEvaluateFromGraphAsync`) targets a
  different field entirely: `CompoundGraphRelationship` rows read via `ICompoundGraphStore`, not
  `KnowledgeEntry.DrugInteractions`. In `InteractionIntelligenceService.EvaluatePairAsync`,
  `TryEvaluateFromGraphAsync` (line ~178, calling into `277-299`) runs **before** the
  `CompoundInteractionHint` repository lookup (line 185) and before the `DrugInteractions` field
  check (line 198), in source order.
- These are **not competing mechanisms writing the same field** — they write two different
  entities (`KnowledgeEntry.DrugInteractions` vs. `CompoundGraphRelationship`), and
  `InteractionIntelligenceService`'s own call order already treats the graph path as
  higher-precedence than both the hint catalog and the `DrugInteractions` field, when the graph
  path has data. `grep -rn "AssertionClass" --include=*.cs backend/src/` returns zero matches: the
  schema's `assertionClass` field (`relationship-packet.schema.json:164-175`) has no persisted
  counterpart anywhere in `CompoundGraphRelationship` or the persistence mapper.
  `CompoundGraphPersistenceMapper.RelationshipTypeMap` (`CompoundGraphPersistenceMapper.cs:23-32`)
  maps only 8 of the 17 `CompoundGraphEdgeType` values to a `GraphRelationshipType` string;
  `CompoundGraphBuilder.MapRelationshipType` (`CompoundGraphBuilder.cs:595-613`) maps all 14 of the
  schema's `relationshipType` enum values into `CompoundGraphEdgeType`, but several many-to-one
  (e.g. `"contraindicated"` and `"caution"` both map to `CompoundGraphEdgeType.AvoidWith` at
  `CompoundGraphBuilder.cs:603-604`, which in turn maps to the single persisted string
  `GraphRelationshipType.AvoidWith` = `"avoid_with"` at `GraphRelationshipType.cs:17`); the
  original packet-level `relationshipType` token is not separately retained in
  `CompoundGraphRelationship` once collapsed through this map.
- Per source-code precedence order, the relationship-packet/graph path is the mechanism already
  coded as authoritative ahead of the hint catalog and the `DrugInteractions` field, for the subset
  of data it carries through to a published artifact. The `EvidencePacketSubstanceRecordCompiler`
  `interactions` field is, by construction (line 44), never populated regardless of lane
  investment, and is not in competition with the graph path because it feeds a different,
  always-empty field.

## 4. Schema-field mapping table (D-A gate basis)

Pairwise-contract field family → frozen `relationship-packet.schema.json` field → citation:

| pairwise-contract field family | schema field | enum / shape | citation |
|---|---|---|---|
| relationship types (incl. negative types) | `relationship.relationshipType` | `synergy, complementary, redundant, conflict, contraindicated, caution, timing-sensitive, dose-dependent, mechanism-overlap, opposing-effect, community-stack, popular-but-unsupported, vendor-claimed, misinformation-pattern` (14 values) | `relationship-packet.schema.json:76-94` |
| assertion classes | `relationship.assertionClass` | `direct-evidence, mechanistic-inference, category-inference, community-signal, vendor-claim, authoritative-caution, curator-hypothesis` (7 values) | `relationship-packet.schema.json:164-175` |
| source references | `relationship.sourceRefs` (→ `sourceRefArray`) + top-level `sources[]` (→ `sourceSnapshot`: `sourceId, sourceType, authorityTier, title, publisher, url, doi, pmid, publishedAt, accessedAt`) | array, min 1 item, unique | `relationship-packet.schema.json:103`, `234-253` |
| evidence tiers | `relationship.evidenceTier` | `Strong, Moderate, Limited, Anecdotal, Insufficient, Unknown` (6 values) | `relationship-packet.schema.json:104-107` |

All four field families named in the D-A decision (relationship types, assertion classes, source
references, evidence tiers) are present as fields on the frozen schema's `relationship` object, as
copyable token enums/arrays, at the citations above. This table answers the schema (input/authoring
contract) question only. Section 3 above separately records that two of these four field families
(the full 14-value `relationshipType` granularity, and `assertionClass` entirely) are not carried
through the existing downstream persistence mapping (`CompoundGraphPersistenceMapper` /
`CompoundGraphRelationship`) without loss or omission — a fact about the downstream persistence
path, not about the schema itself.

## 5. SUBSTRATE VERDICT

**SCHEMA-SUFFICIENT**

Basis: the frozen `relationship-packet.schema.json` already defines, as copyable token
enums/arrays, all four field families named in D-A — relationship types including negative types
(`contraindicated`, `caution`, `conflict`; `relationship-packet.schema.json:76-94`), assertion
classes (`relationship-packet.schema.json:164-175`), source references with structured source
snapshots (`relationship-packet.schema.json:103,234-253`), and evidence tiers
(`relationship-packet.schema.json:104-107`). No gap requires authoring a new schema field for a
packet to express a pairwise negative relationship, its assertion basis, its sources, or its
evidence tier.

Caveat recorded for downstream parcels (not part of this verdict, and not a schema gap): the
existing `CompoundGraphPersistenceMapper`/`CompoundGraphRelationship` persistence path does not
carry `assertionClass` at all (zero references anywhere in `backend/src/`,
`CompoundGraphPersistenceMapper.cs:1-178`) and collapses several of the 14 `relationshipType`
values into a smaller 10-value canon vocabulary on write (`CompoundGraphBuilder.cs:595-613`,
`CompoundGraphPersistenceMapper.cs:23-32`, `GraphRelationshipType.cs:14-23`). This is a mapper/
persistence-layer finding, not a schema-sufficiency finding, and is reported here only because D-A's
scoped gap list is a strict subset of what it could otherwise be read to cover.

## 6. Out-of-scope reminder

This document authors no packet, edits no schema, changes no code, touches no database, and makes
no promotion or Refresh call, consistent with the spec's Constraints section. All findings above
are reproducible via the `grep`/`find` commands cited inline.
