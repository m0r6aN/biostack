“The system generates evidence-bound, clinician-reviewable protocol hypotheses with transparent reasoning, uncertainty, contraindication checks, and required monitoring gates.”
---
1. Integration with Keon Collective and Keon Cortex:
---

## Architecture

### BioStack owns the biological operating picture

BioStack remains the product-specific domain layer:

* Compounds
* Biomarkers
* Protocols
* Phases
* Check-ins
* Evidence tiers
* Timeline observability
* Pathway overlaps
* User-facing dashboards

BioStack already claims protocol observability, compound management, evidence-tier metadata, and timeline correlation. 

### Cortex owns verifiable memory

Cortex should provide:

* Deterministic shard identity
* Append-only memory
* Causal lineage
* Tenant isolation
* Idempotent ingestion
* Authoritative document store
* Derivative vector index
* Decay and reinforcement
* Retrieval influence receipts
* Proof bundles

That is memory, not cognition. Cortex is canonically verifier-first deterministic AI memory with proof artifacts, not a vector DB or chat-history cache. 

So in the BioStack integration, Cortex stores and proves things like:

```text
User logged fatigue after Phase 2.
ALT increased between baseline and week 6.
This protocol thought used memory shards A, B, and C.
This retrieval included these memories and excluded those.
This remembered pattern decayed or was reinforced.
This memory belongs to this tenant only.
```

Cortex tells us **what was remembered, why it was retrievable, whether it is authoritative, and how it changed over time**.

It does not dream.

---

## Collective owns the cognition

The features I described map almost directly onto Collective canon:

| Desired BioStack behavior                                    | Correct Keon owner                        |
| ------------------------------------------------------------ | ----------------------------------------- |
| Generate protocol hypotheses                                 | Collective                                |
| Explore possible future protocol states                      | Collective Temporal Echo Planning         |
| Create non-effecting ideas during idle periods               | Collective Dream Offerings                |
| Challenge a protocol thought                                 | Collective Adversarial Challenge          |
| Produce Skeptic / Regulator / Historian / Optimizer findings | Collective Role-Based Perspective Review  |
| Generate counter-arguments                                   | Collective Active Contradiction Injection |
| Score confidence and uncertainty                             | Collective Confidence Profile             |
| Build reasoning graph                                        | Collective Reasoning Graph Artifact       |
| Produce human-readable rationale                             | Collective Witness Narratives             |
| Keep all cognition non-effecting                             | Collective Cognition-Execution Separation |

Dream Offerings are explicitly Collective: non-effecting proposals generated during quiescence, refined during idle time, and offered to operators for accept/reject/decay.  That is exactly the “invent thoughts that may help optimize a user’s stack protocol and results” idea.

So the more precise framing is:

> **BioStack supplies the biological context. Cortex supplies verifiable memory. Collective dreams, challenges, and collapses protocol hypotheses into non-effecting offerings.**

That’s the clean doctrine line.

---

## Revised integration model

```text
BioStack
  observes biology and protocol activity

Cortex
  remembers user history, evidence lineage, protocol events, labs, outcomes, and retrieval influence

Collective
  dreams new protocol thoughts, simulates branches, challenges assumptions, classifies uncertainty, and emits governed offerings

Runtime / BioStack UI
  decides what the user sees and what requires review, confirmation, or clinician involvement
```

Even sharper:

```text
BioStack asks.
Cortex recalls.
Collective reasons.
BioStack displays.
Runtime governs effects.
```

That might be the spine.

---

## What “Dream Offerings for BioStack” could look like

A **BioStack Dream Offering** is not a recommendation. It is a non-effecting, evidence-bound cognitive artifact.

Example:

```text
Dream Offering:
Possible metabolic readiness phase before initiating the highest-burden weight-loss component.

Why it appeared:
- Current stack includes compounds associated with metabolic, mitochondrial, and recovery pathways.
- User goal implies fat-loss acceleration.
- Relevant liver and lipid markers are missing or stale.
- Prior user logs show fatigue during aggressive phase changes.

Challenge result:
- Mechanism plausibility: moderate
- Evidence support: mixed
- User-specific confidence: low until labs are present
- Safety posture: clinician-review recommended

Available actions:
- Save as hypothesis
- Dismiss
- Request evidence view
- Add missing labs to tracking plan
- Send to clinician review packet
```

That is pure Collective. Cortex only supplies remembered facts and retrieval receipts.

---

## The product concept gets stronger with this correction

Instead of calling it “BioStack Cortex,” I’d avoid that because it mispositions Cortex as the intelligence product.

Better names:

* **BioStack Dream Offerings**
* **Protocol Dreams**
* **Stack Hypotheses**
* **Cognitive Stack Review**
* **Protocol Thought Feed**
* **Collective Review for BioStack**
* **BioStack Collective Mode**

My favorite pairing:

```text
Protocol Thought Feed
Powered by Keon Collective
Remembered by Keon Cortex
Grounded by BioStack
```

That is clean, brandable, and doctrinally accurate.

---

## Updated division of responsibility

### Cortex memory artifacts

Cortex stores:

* Lab result shards
* Protocol phase shards
* Compound usage shards
* Check-in shards
* User goal shards
* Prior adverse-event shards
* Evidence claim shards
* Dream offering lineage shards
* Accepted / rejected / decayed dream states
* Retrieval influence receipts

### Collective cognition artifacts

Collective generates:

* Dream offerings
* Reasoning graphs
* Challenge findings
* Contradiction findings
* Confidence profiles
* Witness narratives
* Branch simulations
* Collapse records
* Governed intent candidates

### BioStack domain artifacts

BioStack owns:

* Stack definitions
* Compound records
* Biomarker panels
* Protocol timelines
* Phase plans
* Observability views
* User-visible reports
* Education and safety boundaries

---

## One important refinement

The **Historian role** inside Collective can use Cortex, but does not become Cortex.

That matters.

```text
Historian role:
“Find prior similar patterns.”

Cortex:
“Here are the authorized memory shards, influence receipt, decay/reinforcement state, and lineage.”

Collective:
“Given that memory, here is the finding.”
```

So Cortex is not the historian. Cortex is the archive and proof system the Historian queries.

A clean metaphor:

```text
Cortex is the library.
The Historian is the librarian-scholar.
Collective is the council.
BioStack is the lab notebook and mission control.
```

---

## Stronger final architecture

```text
BioStack Protocol Event
        ↓
Cortex stores append-only memory shard with lineage
        ↓
Collective retrieves scoped memory through Cortex
        ↓
Collective Dream Offering is generated during quiescence
        ↓
Collective runs Temporal Echo + Challenge + Contradiction
        ↓
Collective emits non-effecting Protocol Thought
        ↓
BioStack displays it with evidence, uncertainty, and safety state
        ↓
User saves, dismisses, tracks, or exports for clinician review
        ↓
Cortex records the disposition and outcome
```

That is the loop.

And it compounds:

```text
More logs → better memory
Better memory → better dreams
Better dreams → better questions
Better questions → better tracking
Better tracking → better future dreams
```

That’s the flywheel.

---

## Final corrected thesis

The right integration is:

> **BioStack becomes the biological observability product. Keon Cortex becomes the verifiable memory substrate. Keon Collective becomes the cognitive engine that dreams, challenges, and offers protocol hypotheses without crossing into medical advice or execution.**

You nailed the distinction. Cortex should stay pure: memory, lineage, proof, replay, tenant safety.

The weird, dangerous, valuable thinking belongs to Collective.

That’s where the dream machine lives.

2. KEON SYSTEMS RUNTIME AND MCP GATEWAY
---

The way BioStack supports Keon Runtime is not by slapping a “governed by Keon” badge on a dashboard. It is by giving Runtime **real effect-bound decisions** in a messy, high-trust domain where receipts actually matter.

BioStack has exactly the right surface area for this because it touches protocols, labs, user observations, review states, evidence, exports, and potentially practitioner workflows. Keon Runtime canon is built for this: no effect-bound action should occur unless a valid Decision Receipt exists and is verified before execution.  MCP Gateway canon also gives us the integration rule: all MCP tool execution passes through the governed gateway, with identity, policy, receipts, and causal lineage injected into every interaction. 

So the product thesis becomes:

> **BioStack is the proving ground for receipt-bound biological protocol governance.**

Not fake governance. Real decisions. Real receipts. Real “this happened only after policy allowed it.”

---

## The core rule

Every Keon integration into BioStack should follow this path:

```text
BioStack UI / API
  -> MCP Gateway
  -> Keon Runtime policy decision
  -> Decision Receipt
  -> BioStack action executes only if receipt is valid
  -> Outcome Receipt / audit spine record
```

No side doors.

MCP Gateway explicitly supports no-bypass governance enforcement and wraps every tool result in a canonical governance envelope containing decision, policy state, receipts, correlation, and proof artifacts.  Runtime then owns the pre-execution proof and fail-closed execution model. 

That means BioStack does not call “dangerous” or consequential internal actions directly. It calls governed tools exposed through MCP Gateway.

---

# Best real BioStack actions for Keon Runtime governance

## 1. Promote staged knowledge into canonical BioStack knowledge

This is the cleanest, strongest first use case.

You already have staged transcript candidate reviews and promotion concepts in BioStack. That is perfect for Runtime.

### Action

Promote a staged candidate into canonical knowledge.

### Why it matters

This changes what future users may see as trusted BioStack intelligence. That is a real-world effect inside the product.

### Governed decision

Runtime checks:

* Artifact exists
* Review state is approved
* Target canonical entity exists
* Evidence tier is assigned
* Required citations are present
* No fixture/test artifact
* No unresolved safety flags
* Actor has admin or reviewer authority
* Policy hash matches current promotion policy

### Receipt

```json
{
  "action": "promote_staged_knowledge_candidate",
  "actor_id": "admin:user-123",
  "tenant_id": "biostack",
  "artifact_id": "staged-review-789",
  "target": "MOTS-C",
  "policy_hash": "sha256:...",
  "decision": "allow",
  "receipt_type": "DecisionReceipt",
  "reason": "approved review state, citations present, target assigned, no safety blocks"
}
```

### Why this is elite

It directly proves that BioStack canonical knowledge cannot be silently mutated by an AI process, admin mistake, or rogue integration. Knowledge promotion becomes receipt-bound.

This is the best PR-grade first integration.

---

## 2. Publish or update evidence-tier claims

BioStack should govern any action that changes claim authority.

### Action

Change a claim from:

* Anecdotal to Limited
* Limited to Moderate
* Moderate to Strong
* Strong to downgraded
* Bro-science to rejected
* Hypothesis to supported mechanism

### Why it matters

Evidence tiers influence user trust and downstream cognition. If a claim becomes “moderate evidence,” Collective may reason differently and users may perceive it differently.

### Governed decision

Runtime checks:

* Claim has source references
* Source count meets policy threshold
* Reviewer identity is authorized
* Claim category allows upgrade
* Contradictory evidence review completed
* Change includes rationale
* No source provenance gaps
* No unsupported “safe dosage” claim sneaks in

### Receipt

```text
Decision Receipt:
Evidence claim tier update allowed.

Before:
Hypothesis

After:
Supported Mechanism

Policy:
biostack-evidence-tier-policy-v1

Reason:
Minimum citation requirements met, contradiction review attached, safety classifier passed.
```

### Why this is not bullshit

This is governance over epistemic authority. In BioStack, claim authority is product power. Runtime should guard it.

---

## 3. Generate a clinician review packet

This is a great external-effect boundary.

### Action

Export a protocol report, lab trend packet, or clinician review PDF.

### Why it matters

Once data leaves BioStack, it can influence medical conversations, decisions, and records. That deserves a receipt.

### Governed decision

Runtime checks:

* User consent present
* PHI export scope confirmed
* Recipient type selected
* Report includes disclaimers
* Report does not include unapproved dosing directives
* Evidence claims include tiers
* Generated hypotheses are marked non-prescriptive
* Export destination is allowed
* Tenant and actor identity are bound

### Receipt

```json
{
  "action": "export_clinician_review_packet",
  "decision": "allow",
  "actor": "user",
  "scope": [
    "protocol_timeline",
    "selected_labs",
    "checkins",
    "evidence_receipts",
    "dream_offerings_marked_non_effecting"
  ],
  "policy_hash": "sha256:...",
  "recipient_class": "clinician",
  "expires_at": "..."
}
```

### Killer product angle

BioStack can show:

> “This packet was generated under receipt-bound export governance.”

That is a real trust feature.

---

## 4. Accept a Collective Dream Offering into a BioStack protocol plan

This is the big one.

### Action

User or practitioner accepts a Dream Offering and turns it into a saved protocol hypothesis, phase plan, or monitoring plan.

### Why it matters

A dream is non-effecting while it is just a proposal. But accepting it into a user’s active plan changes the system of record.

Collective canon is clear that Dream Offerings are non-effecting proposals generated during quiescence, and nothing they touch can reach reality without passing through the governed execution boundary.  This is exactly where Runtime steps in.

### Governed decision

Runtime checks:

* Dream offering is still valid, not decayed
* Reasoning graph lineage exists
* Safety classification allows saving
* Medical-action language is blocked or downgraded
* User accepted explicitly
* Missing-data warnings are preserved
* Clinician-review flag is preserved if required
* BioStack protocol state mutation is allowed

### Receipt

```text
Decision Receipt:
Dream Offering accepted into protocol plan.

Important:
The receipt authorizes saving the hypothesis.
It does not authorize medical action.
```

That distinction is gold.

### Why this matters strategically

This demonstrates the full Keon stack:

* Collective dreams
* Cortex remembers
* MCP Gateway governs integration
* Runtime decides before effect
* BioStack records the outcome

That is not a demo. That is the architecture singing.

---

## 5. Add or change protocol phase state

### Action

Move a protocol from:

* Draft to Active
* Baseline to Preparation
* Preparation to Escalation
* Active to Paused
* Active to Archived

### Why it matters

Changing phase affects dashboards, reminders, monitoring, and user interpretation.

### Governed decision

Runtime checks:

* User confirms transition
* Required baseline data acknowledged
* Risk flags acknowledged
* Open contraindication warnings reviewed
* Required monitoring cadence exists
* For practitioner mode, practitioner authority exists
* For self-user mode, output stays educational

### Receipt

```json
{
  "action": "transition_protocol_phase",
  "from": "preparation",
  "to": "active",
  "decision": "allow_with_warnings",
  "warnings": [
    "missing_recent_gGT",
    "missing_fasting_insulin"
  ],
  "actor_confirmation": true
}
```

### Product value

BioStack can show a phase history with receipts:

> “Phase changed to Active on June 7, 2026, under policy v1.3, with two acknowledged missing-data warnings.”

That is a real governance artifact.

---

## 6. Create or modify lab monitoring plan

### Action

Add lab reminders, monitoring panels, or required follow-up check windows.

### Why it matters

This is not prescribing treatment, but it changes the observability obligations of the protocol.

### Governed decision

Runtime checks:

* Monitoring plan is framed as observability, not diagnosis
* Required labs are relevant to selected pathways
* No prohibited clinical claim
* User consent for reminders
* Export or sharing scope, if any
* Actor authority

### Receipt

```text
Decision Receipt:
Monitoring plan created.

Policy:
BioStack Observability Plan Policy

Authorized effect:
Create reminders and tracking tasks only.

Not authorized:
Treatment advice, diagnosis, medication adjustment.
```

### This one is practical

It gives BioStack a safe but meaningful governed action that users will actually touch.

---

## 7. Practitioner approval workflow

This could become a paid tier.

### Action

Practitioner signs off on a review note, protocol observation, or education packet.

### Why it matters

This is where clinics, longevity practices, and med spas would care.

### Governed decision

Runtime checks:

* Practitioner identity verified
* Practitioner belongs to tenant or client relationship
* Review scope is explicit
* Approval does not exceed allowed scope
* Evidence packet attached
* Patient/user consent exists
* Receipt minted

### Receipt

```json
{
  "action": "practitioner_review_attestation",
  "decision": "allow",
  "practitioner_id": "clinician-456",
  "user_id": "user-123",
  "scope": "education_packet_review",
  "not_scope": "diagnosis_or_prescription",
  "evidence_pack_id": "pack-abc"
}
```

### Money angle

This lets BioStack become a lightweight governed workflow layer for clinics without pretending to be an EHR.

---

## 8. Source ingestion from transcript or external content

### Action

Import YouTube transcript, paper, article, podcast, clinician note, or vendor claim into the staged knowledge pipeline.

### Why it matters

Bad source ingestion poisons the knowledge system.

### Governed decision

Runtime checks:

* Source URL allowed
* Source type classified
* Copyright/usage policy satisfied
* Transcript provider allowed
* Ingestion mode is staged only
* No canonical writes
* No medical claim promoted automatically
* Actor allowed to ingest

### Receipt

```text
Decision Receipt:
Transcript ingestion allowed as staged candidate only.

Denied effects:
Canonical knowledge write
Evidence tier upgrade
User protocol mutation
```

This is especially strong because it proves BioStack’s intake system is governed before knowledge becomes authoritative.

---

## 9. User data deletion, export, or memory promotion

### Action

Delete user protocol data, export user archive, or promote anonymized learning.

### Why it matters

Privacy, trust, and tenant boundaries.

### Governed decision

Runtime checks:

* Actor identity
* Tenant binding
* Consent
* Data scope
* Retention policy
* Whether deletion affects proof spine or only user-facing records
* Whether anonymized learning can be promoted

### Receipt

```text
Decision Receipt:
User archive export allowed.

Scope:
Protocol timeline, check-ins, lab metadata, compound list.

Excluded:
Other tenant data, internal proof spine secrets, unrelated system memory.
```

### Note

Cortex already has fail-closed tenant isolation and proof-backed memory behavior.  Runtime can govern the action, Cortex can prove the memory substrate behavior.

---

## 10. Supplier/source quality tagging

### Action

Mark a compound source/vendor as:

* Verified
* Unverified
* Flagged
* User-reported issue
* Lab-tested
* Do-not-recommend

### Why it matters

This could influence purchase behavior. That is sensitive and commercially meaningful.

### Governed decision

Runtime checks:

* Evidence required for negative or positive vendor claims
* Actor authority
* Defamation risk guard
* Tenant scope
* Claim visibility
* Whether tag is personal, tenant-level, or public

### Receipt

```text
Decision Receipt:
Vendor source flag added.

Visibility:
Private tenant only.

Evidence:
User report attached.

Policy:
No public vendor claim without admin review.
```

This could be very valuable if BioStack eventually includes source tracking and vendor intelligence.

---

# The strongest MVP: 3 governed BioStack actions

For first integration, I would not boil the ocean. I’d do three actions that prove the full Keon story.

## MVP Action 1: Promote staged candidate to canonical knowledge

Best engineering fit with current BioStack direction.

**Receipt proves:** canonical knowledge changed only after approval.

## MVP Action 2: Accept Dream Offering into protocol plan

Best Keon ecosystem showcase.

**Receipt proves:** Collective cognition remained non-effecting until Runtime authorized a BioStack state change.

## MVP Action 3: Export clinician review packet

Best user/business value.

**Receipt proves:** sensitive, potentially influential data left the system only under explicit scoped authorization.

That trio is beast mode.

It demonstrates:

```text
Knowledge governance
Cognition governance
Data/export governance
```

That is not fluff. That is real.

---

# How MCP Gateway should fit

The integration should not be:

```text
BioStack calls Runtime directly sometimes.
BioStack calls Collective directly sometimes.
BioStack calls Cortex directly sometimes.
```

That creates bypass risk.

It should be:

```text
BioStack -> MCP Gateway -> Keon service/tool -> Governed Envelope -> BioStack
```

MCP Gateway is canonically the universal enforcement layer that intercepts and governs MCP tool execution, injecting identity, policy, receipts, and causal lineage into every interaction. 

So BioStack exposes or consumes tools like:

```text
biostack.promoteStagedKnowledgeCandidate
biostack.acceptDreamOffering
biostack.exportClinicianReviewPacket
biostack.createMonitoringPlan
biostack.transitionProtocolPhase
biostack.ingestTranscriptCandidate
biostack.updateEvidenceTier
biostack.recordPractitionerAttestation
```

Each tool must be MCP Gateway-governed.

---

# Suggested policy classes

## Policy: Knowledge Promotion

Guards canonical knowledge mutations.

```text
Requires:
- approved review state
- target assigned
- evidence citations
- no unresolved safety flags
- authorized reviewer/admin
- fixture guard
- idempotency guard
```

## Policy: Dream Offering Acceptance

Guards Collective-to-BioStack state changes.

```text
Requires:
- dream offering lineage
- user confirmation
- non-prescriptive language
- safety classification preserved
- missing-data warnings preserved
- active tenant scope
```

## Policy: Export and Sharing

Guards external transmission.

```text
Requires:
- explicit consent
- scope declaration
- recipient class
- disclaimer inclusion
- PHI minimization
- evidence tier labels
```

## Policy: Protocol Phase Transition

Guards active protocol state changes.

```text
Requires:
- actor confirmation
- required warnings acknowledged
- monitoring plan present or waived
- user/practitioner authority
```

## Policy: Evidence Tier Mutation

Guards claim authority changes.

```text
Requires:
- citation delta
- reviewer rationale
- contradiction check
- safety classifier pass
- downgrade path always allowed with lower friction
```

---

# Receipt types BioStack should display

BioStack should expose a “Governance” or “Proof” tab per protocol/report/knowledge claim.

Receipt types:

```text
Decision Receipt
Evidence Pack
Execution Receipt
Outcome Receipt
PolicyHash
Causal Spine Record
MCP Gateway Envelope
Collective Reasoning Graph Reference
Cortex Memory Influence Receipt
```

Runtime canon includes Decision Receipts, PolicyHash Verification, Governed Spine, Receipt Supremacy, and the artifact hierarchy where receipts outrank derived narratives. 

That means BioStack should not just show pretty summaries. It should show:

> “This claim/report/action is backed by receipt X under policy hash Y.”

That is how BioStack becomes a real Keon proof demo.

---

# BioStack as Runtime’s vertical proof case

This is the big strategic win.

Keon Runtime by itself can sound abstract:

> “We govern AI execution with receipts.”

BioStack makes it concrete:

> “An AI-generated protocol hypothesis could not be saved, exported, promoted, or shown as authoritative unless Runtime authorized the action and minted a receipt.”

That is a buyer-understandable story.

Especially in healthcare-adjacent, wellness, longevity, med spa, research, and regulated workflow contexts.

---

# The real demo flow

Here is the demo I would want on stage:

1. User selects BPC-157, TB-500, NAD+, MOTS-C, Retatrutide.
2. BioStack builds the stack context.
3. Collective generates a Dream Offering:

   * “Potential metabolic readiness phase before escalation.”
4. Cortex provides memory:

   * Prior fatigue logs
   * Missing labs
   * Similar past protocol response
   * Retrieval influence receipt
5. User clicks **Save to Protocol Plan**.
6. BioStack attempts state change through MCP Gateway.
7. Runtime evaluates policy.
8. Runtime allows save but requires warnings preserved.
9. Decision Receipt minted.
10. BioStack shows:

* Protocol Thought saved
* Receipt ID
* PolicyHash
* Warnings preserved
* Not medical advice
* Exportable evidence packet

That demo tells the whole story:

```text
Collective thinks.
Cortex remembers.
Runtime governs.
MCP Gateway enforces.
BioStack proves it in a domain people care about.
```

That is the integrated Keon Systems story.

---

# What not to govern

To avoid governance theater, don’t govern tiny actions like:

* Opening a compound card
* Viewing a chart
* Sorting a table
* Editing a harmless note draft locally
* Expanding a tooltip
* Running pure math calculators, unless saving/exporting the result as part of a protocol artifact

Governance should activate when there is an effect:

```text
Canonical knowledge changes
User protocol state changes
External export occurs
Dream becomes saved plan
Claim authority changes
Practitioner attestation occurs
Memory is written/promoted/deleted
Source/vendor quality tag changes
```

That gives Keon weight instead of noise.

---

# My recommended product framing

## BioStack governed by Keon Runtime

> BioStack uses Keon Runtime to govern consequential protocol, knowledge, and export actions with pre-execution receipts. Cognitive suggestions may be generated freely, but no meaningful state change occurs without a verified decision.

## Slightly sharper

> In BioStack, AI can think without permission. It cannot change trusted knowledge, save protocol hypotheses, export sensitive reports, or mutate user protocol state without Keon Runtime authorization.

That line is money.

## Even sharper

> Thoughts are cheap. Effects require receipts.

That’s Keon’s doctrine in BioStack clothing.

---

# Final answer

Yes, BioStack can absolutely support Keon Runtime, and it might be one of the best vertical proving grounds for it.

The right governed actions are:

1. **Promote staged candidate to canonical knowledge**
2. **Accept Collective Dream Offering into protocol plan**
3. **Export clinician review packet**
4. **Change evidence-tier authority**
5. **Transition protocol phase**
6. **Create monitoring plan**
7. **Record practitioner attestation**
8. **Ingest external source into staged review**
9. **Export/delete/promote memory-backed user data**
10. **Tag supplier/source quality**

And the non-negotiable architecture:

```text
All Keon integrations go through MCP Gateway.
All consequential BioStack actions require Runtime Decision Receipts.
All receipts are visible, verifiable, and tied to policy hashes.
All cognition remains non-effecting until governed.
```

That is not bullshit governance.

That is BioStack becoming a real-world receipt machine.
