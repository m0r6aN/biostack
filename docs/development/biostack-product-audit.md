# BioStack Product Behavior Audit

Audit branch: `codex/biostack-product-audit`  
Base: local `origin/main` at `312257f00c5c15331112971db410c02f370d1c8c`

Runtime validation was blocked. `node`, `npm`, and `docker` are not available on PATH. Backend restore/build also fails before project compilation inside NuGet configuration default resolution. No screenshots were captured. This report is grounded in repository inspection.

## Executive Summary

Overall verdict: **strong foundation, fragmented toolset risk**.

BioStack is materially closer to a knowledge-driven protocol intelligence system than a calculator site. The repo already has a strategic compound intelligence substrate: rich `KnowledgeEntry` data, persistence, canonical lookup, admin ingest/upsert tooling, pathway overlap analysis, check-ins, phases, timelines, and calculators. The main gap is not missing data. The main gap is that high-value data is mostly displayed as reference content instead of driving workflows.

Top strengths already present:

1. Rich compound schema in `backend/src/BioStack.Domain/Entities/KnowledgeEntry.cs`, including aliases, evidence tier, pathways, synergies, cautions, blend fields, schedule/escalation/tiered dosing, interactions, and optimization fields.
2. Database-backed knowledge source and upsert path in `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs`, exposed through admin bulk ingest in `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`.
3. Real stack-intelligence seed: `OverlapService` compares pathways and creates `InteractionFlag` records.
4. Observability primitives exist: `CheckIn`, `TimelineEvent`, `ProtocolPhase`, trend charts, check-in history, and dashboard widgets.
5. Natural product loop entry points already exist: Mission Control, Knowledge Base, Compounds, Check-ins, Timeline, app calculators, public calculators, and onboarding preview.

Top gaps blocking the target product:

1. No protocol object or saved stack abstraction. A stack is inferred from active `CompoundRecord` rows.
2. No calculator-to-protocol conversion. Calculator results can be shown or lead-captured, but not persisted into protocol/compound state.
3. No schedule generation. Weekly schedule, escalation, and tiered dosing fields exist, but no service turns them into editable protocol schedules or reminders.
4. Stack intelligence is shallow. Only pathways are machine-used. `pairsWellWith`, `avoidWith`, `drugInteractions`, `vialCompatibility`, `compatibleBlends`, evidence tiers, and schedule fields are mostly display-only or unused.
5. Personalization is partly cosmetic and partly mocked. Profile fields exist, goal-filtered check-ins exist, but backend goal persistence is absent and dosage personalization copy is overly simplistic.

Final answer: BioStack has roughly **50-60% of the substrate** for a knowledge-driven protocol intelligence system, but only about **20-30% of the intended behavior** is wired into a coherent loop. The highest-leverage move is to connect existing assets: canonical knowledge lookup, active compounds, check-ins, phases, calculator outputs, and knowledge schedule fields into an enriched current-stack/protocol draft flow.

## Current State Overview

The repo is a .NET 10 backend plus Next.js 16 frontend:

- Backend solution: `backend/BioStack.sln`
- API endpoints: `backend/src/BioStack.Api/Endpoints`
- Application services: `backend/src/BioStack.Application/Services`
- Domain models: `backend/src/BioStack.Domain/Entities`
- Persistence: `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`
- Knowledge source/upsert: `backend/src/BioStack.Infrastructure/Knowledge`
- Frontend app routes: `frontend/src/app`
- Frontend product components: `frontend/src/components`
- Structured compound data: `data/compounds.json`

The current product is a personal tracking dashboard, compound reference, pathway overlap tool, calculator set, check-in logger, timeline logger, and marketing/onboarding preview. It is not yet a protocol simulator, schedule generator, saved protocol graph, predicted-vs-actual observability system, or calculator-to-protocol workflow.

## What Exists Now

### Knowledge Engine Foundation

The structured compound intelligence exists and is meaningful.

Evidence:

- `KnowledgeEntry` has `CanonicalName` and `Aliases` at `backend/src/BioStack.Domain/Entities/KnowledgeEntry.cs:8-9`.
- `EvidenceTier` exists at `KnowledgeEntry.cs:13`.
- `Pathways` and `Benefits` exist at `KnowledgeEntry.cs:16-17`.
- `PairsWellWith`, `AvoidWith`, and `CompatibleBlends` exist at `KnowledgeEntry.cs:20-22`.
- `VialCompatibility`, dosage, frequency, and preferred time exist at `KnowledgeEntry.cs:25-30`.
- `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, and `DrugInteractions` exist at `KnowledgeEntry.cs:31-34`.
- Optimization fields exist at `KnowledgeEntry.cs:37-41`.
- Local inspection of `data/compounds.json` found 10 entries; all 10 have tiered dosing, drug interactions, weekly schedules, avoid-with fields, and pairs-well-with fields.

The database path is real:

- `BioStackDbContext` persists `KnowledgeEntries` in `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`.
- List fields are persisted through value converters in `BioStackDbContext`, including aliases, pathways, benefits, synergies, cautions, blends, schedules, escalation steps, drug interactions, optimization supplements, source references, and tiered dosing JSON.
- `DatabaseKnowledgeSource.GetCompoundAsync` looks up by canonical name and aliases in `backend/src/BioStack.Infrastructure/Knowledge/DatabaseKnowledgeSource.cs:16-23`.
- `DatabaseKnowledgeSource.UpsertCompoundAsync` updates by canonical name and reassigns list fields in `DatabaseKnowledgeSource.cs:42-72`.
- `DatabaseKnowledgeSource.IngestBulkAsync` loops through upserts in `DatabaseKnowledgeSource.cs:74-82`.
- Admin bulk ingest exists at `/api/v1/admin/knowledge/ingest` in `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`.

Conclusion: the compound schema and ingest path are strategic assets. Preserve and wire them deeper.

### Knowledge UI

The knowledge UI displays much of the structured data:

- `frontend/src/app/knowledge/page.tsx` searches knowledge entries client-side after calling `apiClient.getAllKnowledgeCompounds()`.
- `frontend/src/components/knowledge/CompoundIntelligenceCard.tsx:40` displays evidence tier via `EvidenceTierBadge`.
- `CompoundIntelligenceCard.tsx:63-90` displays profile-aware guidance.
- `CompoundIntelligenceCard.tsx:99-133` displays `pairsWellWith`, `avoidWith`, and `compatibleBlends`.
- `CompoundIntelligenceCard.tsx:141-176` displays dosage/frequency/preferred time/weekly schedule.
- `CompoundIntelligenceCard.tsx:180-214` displays optimization fields.

Conclusion: compound intelligence is visible, but mostly as reference content.

### Pathway Overlap Behavior

There is one real machine-usable intelligence behavior today: pathway overlap.

Evidence:

- `OverlapService.CheckOverlapAsync` loads knowledge entries by submitted compound name in `backend/src/BioStack.Application/Services/OverlapService.cs:21-39`.
- It computes `compound1.Pathways.Intersect(compound2.Pathways)` at `OverlapService.cs:43-44`.
- It creates `InteractionFlag` rows for shared pathways at `OverlapService.cs:51-62`.
- It labels confidence as limited/educational at `OverlapService.cs:58`.
- API route `/api/v1/knowledge/overlap-check` exists in `backend/src/BioStack.Api/Endpoints/KnowledgeEndpoints.cs`.
- Mission Control calls `apiClient.checkOverlap(activeCompoundNames)` for active compounds in `frontend/src/components/dashboard/MissionControlDashboard.tsx:84-89`.
- Mission Control renders `OverlapFlagsBanner` at `MissionControlDashboard.tsx:157`.
- `frontend/src/app/knowledge/page.tsx` includes a manual pathway overlap checker.

Conclusion: stack intelligence exists, but it is narrow.

### Compound Tracking

Compound records exist as profile-owned rows:

- Domain model: `backend/src/BioStack.Domain/Entities/CompoundRecord.cs`
- Fields: name, category, start/end dates, status, notes, source type, goal, source, price.
- API group: `/api/v1/profiles/{profileId}/compounds` in `backend/src/BioStack.Api/Endpoints/CompoundEndpoints.cs:10`.
- Creation creates a timeline event when `StartDate` exists in `backend/src/BioStack.Application/Services/CompoundService.cs:51-64`.
- UI form/list: `frontend/src/components/compounds/CompoundForm.tsx` and `frontend/src/components/compounds/CompoundList.tsx`.
- Compound page fetches knowledge detail for selected compounds in `frontend/src/app/compounds/page.tsx`.

Conclusion: compound tracking exists, but it lacks dose, frequency, route, selected tier, generated schedule, and canonical knowledge reference.

### Observability

Check-ins and timeline primitives are real:

- `backend/src/BioStack.Domain/Entities/CheckIn.cs` captures weight, sleep, energy, appetite, recovery, focus, thought clarity, skin, digestion, strength, endurance, joint pain, eyesight, side effects, photos, GI symptoms, mood, and notes.
- `CheckInService.CreateCheckInAsync` persists check-ins and creates timeline events at `backend/src/BioStack.Application/Services/CheckInService.cs:25-72`.
- `CheckInForm` changes submitted metrics based on profile goal categories at `frontend/src/components/checkins/CheckInForm.tsx:75-98`.
- The "Goal-Specific Metrics" section renders at `CheckInForm.tsx:183-211`.
- Trend charts cover weight, energy, sleep quality, and recovery in `frontend/src/components/checkins/TrendChart.tsx:8-33`.
- `ProtocolPhaseService` creates phase timeline events at `backend/src/BioStack.Application/Services/ProtocolPhaseService.cs:45-57`.

Conclusion: observability has strong primitives, but no correlation engine, overlays, schedule-state awareness, or predicted-vs-actual view.

### Calculators

Calculator functionality exists:

- Backend: `backend/src/BioStack.Application/Services/CalculatorService.cs`
- API: `backend/src/BioStack.Api/Endpoints/CalculatorEndpoints.cs`
- App calculators: `frontend/src/app/calculators/page.tsx`
- Public calculators: `frontend/src/app/tools/reconstitution-calculator/page.tsx`, `frontend/src/app/tools/volume-calculator/page.tsx`, `frontend/src/app/tools/unit-converter/page.tsx`
- Shared public UI: `frontend/src/components/marketing/PublicCalculatorExperience.tsx`

Calculator safety framing is clean:

- Backend disclaimer says mathematical calculation only.
- `frontend/src/components/SafetyDisclaimer.tsx` has calculation, educational, observation, and general variants.

Conclusion: calculators are useful but disconnected from protocol state.

## What Partially Exists

### Simulate -> Save -> Track -> Compare -> Evolve

- Simulate: weak. `StackIntelligencePanel` and onboarding preview simulate product feel, not real product behavior.
- Save: partial. Profiles, compounds, check-ins, and phases save. No saved protocol or saved calculation.
- Track: real. Compounds, check-ins, phases, and timeline exist.
- Compare: weak. Trend charts exist, but no stack/protocol comparison.
- Evolve: missing. No protocol versioning, fork, revision, or recommendation loop.

Evidence:

- Onboarding preview stores compounds/goals in localStorage at `frontend/src/components/marketing/OnboardingExperience.tsx:39-49` and `:77-81`.
- It fetches knowledge for suggestions at `OnboardingExperience.tsx:69`.
- It renders `StackIntelligencePanel` at `OnboardingExperience.tsx:416-418`.
- It routes to `/profiles` to finish setup at `OnboardingExperience.tsx:516-519`.
- It does not call `checkOverlap` or persist a stack/protocol.

### Calculator -> Add to Protocol

Present pieces:

- Calculators return structured result values.
- Public calculator has "Save This Result" lead capture at `frontend/src/components/marketing/PublicCalculatorExperience.tsx:243`.
- It calls `apiClient.captureLead` at `PublicCalculatorExperience.tsx:96`.
- It links to account creation at `PublicCalculatorExperience.tsx:267-268`.

Missing glue:

- No `CalculatorResult` persistence entity.
- No protocol entity to receive calculator output.
- No dose/frequency fields on `CompoundRecord`.
- No "add to protocol" action.

Conclusion: calculators are currently acquisition/tool endpoints, not protocol entry points.

### Knowledge -> Stack Intelligence -> Scheduling -> Observability

Present:

- Knowledge: strong.
- Stack intelligence: pathway overlap only.
- Scheduling: display-only weekly schedule.
- Observability: check-ins, timeline, trends.

Missing:

- Schedule generator service.
- Schedule entity.
- Dose events.
- Reminders.
- Schedule-aware conflict logic.
- Predicted-vs-actual overlay.

Conclusion: the sequence is supported by substrate, but not wired into one product loop.

### Personalization

Present:

- `PersonProfile` has sex, age, weight, goal summary, and notes.
- `ProfileForm` captures sex, age, weight, selected goals, and custom goal summary.
- `CheckInForm` filters metrics by goal categories.
- `CompoundIntelligenceCard` displays profile-aware guidance.

Weaknesses:

- Frontend calls `/api/v1/goals` and `/api/v1/profiles/{profileId}/goals`, then falls back to local definitions/localStorage in `frontend/src/lib/api.ts:240-270`; backend goal endpoints are absent.
- `ProfileForm` passes `selectedGoalIds`, but backend profile requests do not persist selected goals.
- `CompoundIntelligenceCard` says higher end of dosage range is recommended if weight is over 90 kg. That is too prescriptive and too simplistic.

Conclusion: personalization is partly real, partly mocked, and dosage personalization should be softened.

## What Is Missing Entirely

1. Protocol aggregate or saved stack model.
2. Protocol item/dose/schedule model.
3. Calculator result persistence.
4. One-click calculator-to-protocol action.
5. Schedule generation service.
6. Editable generated schedules.
7. Reminder or alert hooks.
8. Schedule-aware interaction logic.
9. Multi-compound synergy/conflict engine beyond pathway overlap.
10. Use of `avoidWith`, `drugInteractions`, `vialCompatibility`, and `compatibleBlends` in stack warnings.
11. Evidence-weighted scoring or confidence logic.
12. Predicted-vs-actual observability.
13. Check-in correlation with active compounds, dose changes, phases, or schedule adherence.
14. Protocol compare, clone, fork, versioning, or templates.
15. Backend goal definitions/profile-goal persistence.
16. Runtime path to convert onboarding preview compounds into actual profile compounds.

## High-Leverage Opportunities Using Existing Assets

1. **Create an enriched current-stack endpoint.** Return active compounds enriched with knowledge entries, overlap flags, avoid-with hits, drug-interaction notices, blend notes, evidence tier summary, and schedule snippets. This uses `CompoundRecord`, `KnowledgeEntry`, `OverlapService`, and profile context.
2. **Canonicalize `CompoundRecord` against `KnowledgeEntry`.** Add `KnowledgeEntryId` or canonical name when selected from knowledge. Keep manual entries string-only. This makes aliases and future intelligence reliable.
3. **Add calculator result attach flow before a full protocol rebuild.** Store calculator kind, inputs, output, unit, formula, and created time, then let the user attach it to an existing or new compound.
4. **Generate a schedule preview from existing knowledge fields.** Convert `WeeklyDosageSchedule`, `IncrementalEscalationSteps`, `TieredDosing`, `Frequency`, and `PreferredTimeOfDay` into a non-prescriptive "reference schedule preview."
5. **Add predicted-vs-actual lite.** Overlay compound starts/stops and protocol phases on check-in charts. Compare before/after windows. This uses existing timeline/check-in data without medical claims.

## UX and Workflow Friction Points

1. Public calculators and in-app calculators are fragmented: `/calculators` inside the app plus `/tools/*` public calculator pages.
2. Public calculator "Save This Result" means lead capture, not saving to protocol.
3. Onboarding says "Add to My Stack" but stores a local preview and routes to `/profiles`; it does not persist stack/protocol state.
4. Knowledge search fetches all compounds and filters client-side. This is acceptable for 10 compounds but not a real knowledge search strategy.
5. Compound add flow is guided by category and benefits, but ends at a manual `CompoundRecord` without dose/schedule/tier.
6. Mission Control surfaces overlap flags but does not provide a strong next action.
7. Trend charts are isolated from compound and phase events.
8. Affiliate-style contextual recommendations in `frontend/src/lib/recommendations.ts` may dilute intelligence surfaces unless kept secondary.
9. Frontend update/delete compound API paths do not match backend routes: frontend calls `/api/v1/compounds/{id}` at `frontend/src/lib/api.ts:120-132`, while backend exposes update/delete under `/api/v1/profiles/{profileId}/compounds/{id}` at `backend/src/BioStack.Api/Endpoints/CompoundEndpoints.cs:10-22`.
10. Goal UI is ahead of backend and relies on fallback/mock behavior.

## Data and Schema Leverage Assessment

The schema is strong, but underused:

- Canonical lookup: present, but `CompoundRecord` should store a stable canonical reference.
- Evidence tier: present and displayed, not used to weight warnings/confidence.
- Pathways: present and used in overlap detection.
- Pairs well with: present and displayed, not evaluated across stack.
- Avoid with: present and displayed, not evaluated across stack.
- Drug interactions: present and returned by backend, not used in warnings.
- Vial compatibility and compatible blends: present, but vial compatibility is not in the frontend `KnowledgeEntry` interface and not used in blend logic.
- Weekly schedule, escalation, tiered dosing: present, but only weekly schedule is displayed; escalation and tiered dosing are missing from frontend type/use.
- Optimization fields: present and displayed, not adapted to goals or check-in signals.

Important type mismatch:

- Backend response includes `VialCompatibility`, `StandardDosageRange`, `MaxReportedDose`, `IncrementalEscalationSteps`, and `TieredDosing`.
- Frontend `KnowledgeEntry` in `frontend/src/lib/types.ts` omits several high-value fields and types `optimizationSupplements` as `string` even though backend/domain use list values.

## Feature-Behavior Matrix

| Target behavior | Exists now | Partial | Missing | Repo evidence | Notes |
|---|---:|---:|---:|---|---|
| Canonical compound lookup | Yes | Yes | No | `DatabaseKnowledgeSource.cs:16-23` | Lookup exists, but compound records are not canonicalized |
| Evidence tier surfaced | Yes | No | No | `KnowledgeEntry.cs:13`, `CompoundIntelligenceCard.tsx:40` | Display only |
| Pathways used for insight | Yes | Yes | No | `OverlapService.cs:43-58` | Only shared pathway overlap |
| Synergy detection | No | Yes | Yes | `KnowledgeEntry.cs:20`, `CompoundIntelligenceCard.tsx:99-111` | Displayed, not reasoned over |
| Conflict detection | No | Yes | Yes | `KnowledgeEntry.cs:21,34` | Avoid/interactions not stack-evaluated |
| Blend compatibility | No | Yes | Yes | `KnowledgeEntry.cs:22,25` | Compatible blends shown, vial logic absent |
| Schedule generation | No | Yes | Yes | `KnowledgeEntry.cs:31-33`, `CompoundIntelligenceCard.tsx:141-176` | Schedule displayed as text only |
| Live stack projections | No | Yes | Yes | `MissionControlDashboard.tsx:84-89`, `OnboardingExperience.tsx:416-418` | Overlap count and animation, not simulation |
| Timeline generation | Yes | Yes | No | `CompoundService.cs:51-64`, `CheckInService.cs:59-72` | Event stream exists |
| Effect domains | No | Yes | Yes | `CheckInForm.tsx:75-98`, `goals.ts` | Goal categories affect metrics, not projections |
| Confidence/evidence weighting | No | Yes | Yes | `OverlapService.cs:58`, `KnowledgeEntry.cs:13` | Fixed confidence string |
| Stack scoring | No | No | Yes | No stack scoring service found | Defer |
| Goal-aware composition | No | Yes | Yes | `CompoundForm.tsx`, `GoalPicker.tsx` | Goal system is frontend-heavy |
| Protocol phases | Yes | No | No | `ProtocolPhase.cs`, `ProtocolPhaseService.cs` | Real phase tracking |
| Reusable protocols/templates | No | No | Yes | No protocol/template entity found | Absent |
| Check-in creation | Yes | No | No | `CheckInService.cs`, `CheckInForm.tsx` | Solid primitive |
| Trend analysis | Yes | Yes | No | `TrendChart.tsx` | Simple charts only |
| Compound/check-in correlation | No | Yes | Yes | Timeline exists; charts do not overlay events | High leverage |
| Calculator APIs | Yes | No | No | `CalculatorEndpoints.cs`, `CalculatorService.cs` | Solid math service |
| Calculator result persistence | No | No | Yes | No entity/repository found | Required |
| Calculator -> protocol | No | No | Yes | `PublicCalculatorExperience.tsx:243` | Current save is lead capture |
| Profile personalization | Yes | Yes | No | `PersonProfile.cs`, `ProfileForm.tsx`, `CheckInForm.tsx` | Mixed real/mock |
| Goal persistence | No | Yes | Yes | `api.ts:240-270`, no backend goal endpoints | Mock fallback hides gap |
| Safety framing | Yes | Yes | No | `SafetyDisclaimer.tsx`, `CalculatorService.cs`, `OverlapService.cs` | Mostly clean |
| UX coherence | No | Yes | Yes | `/calculators`, `/tools/*`, `/knowledge`, `/compounds` | Feels like connected tools, not one loop |

## Schema Leverage Appendix

### Stack Intelligence

Already enabled by schema:

- `CanonicalName` and `Aliases` for matching.
- `Pathways` for overlap/redundancy.
- `PairsWellWith` for synergy.
- `AvoidWith` and `DrugInteractions` for cautions.
- `CompatibleBlends` and `VialCompatibility` for blend warnings.
- `EvidenceTier` for confidence weighting.

Current use: pathways are used by `OverlapService`; pair/avoid/blend fields are displayed.  
Next use: stack analysis service returning shared pathways, positive pairings, avoid-with hits, drug interaction notices, blend notes, and evidence summary.

### Scheduling

Already enabled by schema:

- `Frequency`
- `PreferredTimeOfDay`
- `WeeklyDosageSchedule`
- `IncrementalEscalationSteps`
- `TieredDosing`
- `StandardDosageRange`
- `MaxReportedDose`

Current use: weekly schedule is displayed as text.  
Next use: generate an editable "knowledge-base reference schedule" from these fields and a user-selected start date.

### Simulation

Already enabled by schema:

- Pathways, benefits, evidence tier, schedules, optimization fields, and interactions are enough for a deterministic first simulator.

Current use: onboarding animation and dashboard overlap count.  
Next use: "If you add X, your stack gains these pathways, overlaps these pathways, introduces these cautions, and creates this reference schedule."

### Personalization

Already enabled:

- Profile age, sex, weight, goal summary.
- Frontend goal categories.
- Knowledge tiered dosing and optimization fields.

Current use: profile display, goal-filtered check-in metrics, weak dosage copy.  
Next use: persist goals backend-side, use goals to prioritize educational context and observability metrics, and avoid prescriptive dose personalization.

### Observability

Already enabled:

- Multi-dimensional `CheckIn`.
- `TimelineEvent` linked to related entities.
- `ProtocolPhase` segmentation.

Current use: trends and timeline.  
Next use: overlay active compounds and phases on trends, compare before/after compound starts, then later add predicted-vs-actual once schedule events exist.

### Future Protocol Graph

Already enabled:

- Profiles own compounds, check-ins, phases, and timeline events.
- Compound records have start/end/status.
- Knowledge entries can enrich compound nodes.

Missing:

- Protocol entity.
- Stack entity.
- Protocol item entity.
- Template entity.
- Version/fork lineage.
- Aggregation model.

Recommendation: do not start with social/community graph mechanics. First create a protocol aggregate that wraps current profile compounds and phases. Then add clone/version once individual protocols are useful.

## Screens, Routes, Components, Services Inventory

Frontend routes:

- `frontend/src/app/page.tsx`
- `frontend/src/app/admin/page.tsx`
- `frontend/src/app/auth/signin/page.tsx`
- `frontend/src/app/calculators/page.tsx`
- `frontend/src/app/checkins/page.tsx`
- `frontend/src/app/compounds/page.tsx`
- `frontend/src/app/faq/page.tsx`
- `frontend/src/app/knowledge/page.tsx`
- `frontend/src/app/mission-control/page.tsx`
- `frontend/src/app/onboarding/page.tsx`
- `frontend/src/app/pricing/page.tsx`
- `frontend/src/app/profiles/page.tsx`
- `frontend/src/app/profiles/[id]/page.tsx`
- `frontend/src/app/timeline/page.tsx`
- `frontend/src/app/tools/page.tsx`
- `frontend/src/app/tools/reconstitution-calculator/page.tsx`
- `frontend/src/app/tools/unit-converter/page.tsx`
- `frontend/src/app/tools/volume-calculator/page.tsx`

Major components:

- Dashboard: `MissionControlDashboard`, `ActiveCompoundsCard`, `ActiveGoalsCard`, `LatestCheckInCard`, `OverlapFlagsBanner`, `TimelineSnapshot`, `StatCard`
- Compounds: `CompoundForm`, `CompoundList`, `CompoundStatusBadge`
- Knowledge: `CompoundIntelligenceCard`, `EvidenceTierBadge`, `OverlapResults`
- Check-ins: `CheckInForm`, `CheckInHistory`, `TrendChart`
- Calculators: `ReconstitutionCalc`, `VolumeCalc`, `ConversionCalc`
- Marketing/onboarding: `OnboardingExperience`, `PublicCalculatorExperience`, `StackIntelligencePanel`, `LandingHero`
- Profiles/goals: `ProfileForm`, `GoalPicker`, `GoalBadge`, `GoalDisplay`
- Shell: `AppShell`, `Header`, `Sidebar`, `ProfileSwitcher`, `SafetyDisclaimer`

Backend API endpoint files:

- `AdminEndpoints.cs`
- `AuthEndpoints.cs`
- `CalculatorEndpoints.cs`
- `CheckInEndpoints.cs`
- `CompoundEndpoints.cs`
- `DevAuthEndpoints.cs`
- `KnowledgeEndpoints.cs`
- `LeadEndpoints.cs`
- `ProfileEndpoints.cs`
- `ProtocolPhaseEndpoints.cs`
- `TimelineEndpoints.cs`

Domain models:

- `AppUser`
- `PersonProfile`
- `CompoundRecord`
- `KnowledgeEntry`
- `TieredDosingData`
- `CheckIn`
- `ProtocolPhase`
- `TimelineEvent`
- `InteractionFlag`
- `LeadCapture`

Services:

- `KnowledgeService`
- `OverlapService`
- `CompoundService`
- `CalculatorService`
- `CheckInService`
- `TimelineService`
- `ProtocolPhaseService`
- `ProfileService`
- `JwtTokenService`

Ingestion/upsert tooling:

- `DatabaseKnowledgeSource.UpsertCompoundAsync`
- `DatabaseKnowledgeSource.IngestBulkAsync`
- Admin route `/api/v1/admin/knowledge/ingest`
- Seed fallback from `LocalKnowledgeSource` in `Program.cs`
- Structured source file `data/compounds.json`

## Safety and Compliance Assessment

Clean:

- Calculator backend disclaimer is math-only.
- `SafetyDisclaimer` distinguishes educational, calculation, observation, and general usage.
- `OverlapService` labels overlap output educational.
- Public calculators say calculated result only and not clinical guidance.

Risky:

- `CompoundIntelligenceCard` says "Higher end of dosage range recommended" based only on weight. Change to observational language.
- "Protocol Guidance" label around dosage/schedule should become "Knowledge-base reference" or "Reference schedule from source data."
- `avoidWith` mixes contraindications, duplicate-use limitations, lifestyle notes, and general cautions. The data model should eventually distinguish those categories if the product will act on them.

Powerful but safe behaviors:

- Pathway overlap.
- Redundancy detection.
- Source-listed caution surfacing.
- Reference schedule preview.
- Planned-vs-logged observability.
- Trend overlays around protocol events.

## Prioritized Recommendations

| Priority | Recommendation | Impact | Difficulty |
|---|---|---:|---:|
| 1 | Create enriched current-stack endpoint using active compounds plus knowledge entries | Very high | Medium |
| 2 | Canonicalize compound records to knowledge entries | Very high | Medium |
| 3 | Add stack-level warnings for `avoidWith`, `drugInteractions`, `compatibleBlends`, and vial compatibility | High | Medium |
| 4 | Add calculator result attach flow to compound record | High | Medium |
| 5 | Generate schedule preview from knowledge schedule fields | High | Medium |
| 6 | Overlay compound/phase events on check-in charts | High | Low-Medium |
| 7 | Persist profile goals in backend | Medium | Medium |
| 8 | Add protocol phase comparison views | Medium | Medium |
| 9 | Add protocol templates/cloning/versioning | Medium | High |
| 10 | Defer social/people-like-you protocol graph | Low now | High |

## Runtime and Git Receipts

Commands/results:

- `git fetch origin`: failed with `fatal: unable to access ... getaddrinfo() thread failed to start`.
- Worktree created from local `origin/main`.
- `git checkout -b codex/biostack-product-audit`: succeeded.
- `node --version`: command not found.
- `npm --version`: command not found.
- `docker --version`: command not found.
- `dotnet test BioStack.sln`: failed because MSBuild tried to create a temp directory under `C:\WINDOWS`.
- Retried with `TEMP`/`TMP` in repo `.tmp`: failed in NuGet target evaluation with `Value cannot be null. (Parameter 'path1')`.
- Retried restore with local `NUGET_PACKAGES`: same NuGet configuration default exception.
- `dotnet run --no-restore`: failed because `project.assets.json` does not exist.

Files created for the audit:

- `docs/development/biostack-product-audit.md`

Code changes made:

- None. Documentation-only audit.

Final git receipts:

- `git branch --show-current`: `codex/biostack-product-audit`
- `git status --short`: `?? docs/development/biostack-product-audit.md`
- `git diff --stat`: no output because the audit report is untracked and not staged
