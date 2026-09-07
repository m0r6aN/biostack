# GTM Handoff: September 7, 2026

Base: `aeda45f` (origin/main after PR #282). PR #283 open at time of writing.
Scope: completion of wave006 remediation directive items 1-3, plus the source-authorization work those
items exposed. **No compound was promoted. No production action was taken.**

Read this before the receipts. It exists so the next session starts grounded in the repository rather than in
a previous session's summary.

## The Standing Rule

**The worker is the source of truth for what is authorized. An audit script is not.**

This cost a wrong report on 2026-09-07. `tools/research/audit-source-authorization.py` checks authority tier
and rights review status, and on that basis reported registry-backed coverage rising from 13 to 166 claims.
The worker additionally requires `acquisition.enabled` and roughly twenty other conditions
(`SourceRegistryActivationPolicy.AddUnless`), and by its reckoning the same change authorized almost nothing
until PR #281. Measure by re-running the worker and counting `ops.qualityFlags` on the emitted packets under
`research/output/<tag>/evidence-packet/`.

There are **two** authority gates, and they are easy to confuse:

1. `EvidencePacketPreprocessor` emits `missing-authoritative-support` from the packet's **own self-declared
   tier**. Still self-asserted. This is the remaining work.
2. `SourceRegistryAuthorizer` runs immediately after and **does** consult the registry and **does** check
   `authorizedFieldUse`, emitting `source-registry-unmapped-source`, `source-registry-source-disabled`,
   `source-registry-field-mismatch`. This has existed all along.

## Current Measured Baseline

Counted off the emitted packets, not the audit script. Reproduce with:

```bash
pwsh -NoProfile -File tools/research/run-knowledge-research.ps1 -SourceRegistryFile research/input/sources/pilot-source-registry.json -OutputDirectory research/output/<tag>
```

| Gate flag | Count |
|---|---|
| `source-registry-unmapped-source` | 62 |
| `source-registry-field-mismatch` | 61 |
| `source-registry-source-disabled` | 29 |
| `missing-authoritative-support` | 19 |

Manifest: **71 blocked, 5 review-required, 1 research-requested, 2 candidates** (LL-37, Semaglutide).
Registry: 30 classes. Run health: `Scanned=78 Created=78 Failed=0`.

## Open Decisions — Clint Only

1. **LL-37 promotion eligibility.** It moved Blocked → CandidatesForPromotion in PR #281 as a side effect of
   enabling acquisition on the ratified classes — because its only blocker was an authorization issue, not
   because its evidence improved. It is eligible for promotion *review*, not promoted: `Completeness:
   partial`, 7 open review-queue items, flags `thin-human-evidence`, `single-human-rct-mixed-result`,
   `gray-market-marketing-gap`, `regulatory-status-volatile`. It is also one of the wave005 compounds that
   returns 404 in production. **If it should stay blocked pending its own review, that needs an explicit
   gate.**
2. **WADA redistribution rights.** Still `pending-human-legal`; the class is deliberately disabled and six
   claims rest on it. The 2026-09-07 ruling addressed source *dates*, not rights. Unresolved.
3. **Unexpected-field CI check.** `sourceSnapshot.additionalProperties` is now `true` by ruling, so a typo
   such as `authorityTeir: "A1"` will validate silently while the real field defaults. A CI check listing
   unexpected source fields would restore typo detection without re-tightening the schema. Not built.
4. **The one tier raise.** `tamoxifen-efficacy-atlas-extended-001` goes Moderate → Strong in PR #283, the
   only raise in the entire sequence. Justified by the anchoring precondition being met, with `confidence`
   deliberately left at moderate. Reversible.

## Next Work

**Make `EvidencePacketPreprocessor` registry-backed.** This is the last structural item and the only one that
still lets a packet authorize itself. Expect it to move compounds. Land it alone, measure with the worker,
and surface any readiness movement before merging — the LL-37 surprise came from not doing that.

Then: the 62 unmapped and 61 field-mismatch claims need per-item work, not another bulk pass. 277 source
entries across 152 hosts remain deliberately unregistered (Wikipedia, vendor catalogues, press releases,
investor relations, forums, law-firm blogs); that is the correct resting state, not a backlog.

## Still Open Per Packet

Recorded in `receipts/rereview-2026-09-07.md` and the per-packet receipts. Nothing below is cleared.

- **creatine** — the population-scope conflict for pre-existing renal impairment; two Discussion-attributed
  quotes unverified against the primary; both endurance dispositions remain request-changes until endurance
  evidence is actually retrieved and anchored; NIH ODS remains unread after three failed retrievals while
  sourcing six of eight claims.
- **vitamin-d3** — the 2018 falls-recommendation quote is **not anchored to any cited URL** and must not be
  published until an authorized falls source is registered; the VITAL claim was not confirmed on re-review
  (primary inaccessible, only two secondary summaries for a Strong/`fieldAuthorityRequired` claim); mechanism
  claim remains unresolved.
- **tamoxifen** — the endometrial safety assertion embedded in the ATLAS efficacy claim still has no eligible
  A1/A2 source; `tamoxifen-mechanism-serm-001` remains unresolved with no different-family corroboration;
  the coumarin/warfarin scope is unresolved.
- **corpus** — four claims carry no authority support at all after the 2026-09-07 dispositions, three of them
  safety-relevant. Listed in `receipts/source-authority-dispositions-2026-09-07.md`.

## Working Rules Learned The Hard Way

- **Never `json.load` → `json.dump` an evidence packet.** Per-file formatting differs (inline vs expanded
  arrays, trailing newline present or absent, literal vs escaped non-ASCII). Use claim-scoped raw-text
  substitution and verify trailing bytes against `HEAD`. Applies to the schema files too.
- **Never register a source by id prefix.** `fda-` is not evidence of FDA authorship: the corpus contained a
  Drugs.com page, a Philippine FDA advisory, and an industry GRAS notice all wearing that prefix. Verify by
  parsing the source's own URL host.
- **Review the whole claim, not the named error.** Three independent reviewers found the correction passes
  "correct but incomplete" because each fixed the named blocker and missed adjacent defects in the same
  claim. Briefs should say so explicitly.
- **A brief that freezes a field and also says "lowering is fine" is contradictory.** Two agents correctly
  flagged rather than guessed; a third correctly refused three of nine items because the premise was wrong.
  Treat agent pushback as signal and verify it against the packets.
- **Verify agent work independently.** Every agent result this session was re-derived before shipping.

## Boundaries

Unchanged. Branches and PRs only. Clint retains merges, deployment, production DB writes including Refresh
startup, secrets and infrastructure, Stripe production, legal, and money. None of those were touched. The
`Refresh` dry-run hazard recorded in the 2026-09-05 handoff still stands: `DryRun=true` is **not**
database-write-free.

## Merged This Sequence

| PR | Subject |
|---|---|
| #269 | Correct three lead-validated wave006 packet errors |
| #270 | Source authorization gap audit (directive item 2) |
| #271 | Fix the dead source-registry tier lookup (two sites) |
| #272 | Make runner missing-input failures loud, not silent |
| #273 | Null 67 fabricated placeholder publication dates |
| #274 | Apply ratified source-authority dispositions |
| #275 | Check `authorizedFieldUse`, not just authority tier |
| #276 | Withdraw ATLAS numeric bundle; lower two contradicted tiers |
| #277 | Expand source registry with ratified classes (13 → 30) |
| #278 | Correct creatine re-review findings |
| #279 | Correct vitamin D3 re-review findings |
| #280 | Honest evidence labels for thin efficacy claims |
| #281 | Activate the ratified registry classes so they authorize |
| #282 | Source `effectiveDate` and `dateNote` fields |
| #283 | Restore corrected ATLAS figures; close tamoxifen findings (**open**) |

Thoughts are free. Effects are governed.
