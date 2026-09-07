# Goal Charter: Wave 3 positioning copy

**Status:** Draft — Gate 1 ratification required  
**Coordinator:** Codex (`/goal`)  
**Source packet:** `.audit/WAVE3-EXECUTION-PACKET.md` (signed copy, 2026-08-08)  
**Canonical prior directive:** `.audit/COORDINATOR-HANDOFF-landing-r1.md`

## Objective

After Waves 1 and 2 are actually merged, publish the signed Wave 3 positioning copy without weakening BioStack's doctrine regression net, changing plan entitlements, or implying prescriptions or personalized guidance.

## Verified starting state

- `main` is clean at `e83bb09`.
- The prerequisite is not yet true on this checkout: `LandingHero.tsx` still contains the Wave 2 gate strings `Operator required` and `Operator access`, and the old headline/subhead remain. The execution packet itself requires Waves 1 and 2 to merge first.
- The old strings named for retirement remain in current source: `SITE_TITLE` contains `Protocol Operations`; the footer contains `Tracking, math, and clarity`; the Commander contract tagline is `Longitudinal Intelligence`; and `/safety` ends in `Just structure.`
- `contracts/product-contract.v1.json` is version `1.0.0`; its version test pins that value. No version-bump convention for display-only taglines was found in the packet or contract.

## Governing constraints

- The signed packet's category sentence, hero H1, tier prices/taglines, approved details, and `/safety` echo are verbatim-only decisions.
- Preserve `monthlyPriceCents`, `marketingCtaPath`, and the Observer highlight `Up to 8 active compounds`. Do not claim a paid plan lifts a cap without enforcement evidence.
- Preserve the doctrine assertions in `HomePageHero.test.tsx`; only its two old-copy assertions may move in lockstep with the signed copy.
- `docs/guidance/biostack-guidance-content-contract.v1.md` outranks all marketing work. No dosing, switching, tapering, sourcing, or personalized medical language may enter scope.
- One file, one owner per wave; `LandingHero.tsx` remains serialized behind Waves 1 and 2.

## Decisions for Gate 1

| ID | Proposed decision | Rationale |
|---|---|---|
| D1 | Treat the signed packet as authoritative for its listed changes: five category placements, hero copy, pricing-page/contract copy, and `/safety` echo. | The packet says these words and placements are final. |
| D2 | Interpret the packet as a **partial replacement** for original tasks 3.3 and 3.4: defer the original landing trust block, FAQ/price strip, moved disclaimer, and closing CTA until separately specified. | The packet says it unblocks 3.1–3.4, but its explicit files and acceptance criteria do not implement those original 3.3/3.4 deliverables and it marks F3/F6 still open/partial. This prevents a silent scope change. |
| D3 | Do not dispatch code until the Wave 1/2 prerequisite is evidenced as merged on the base branch. | The packet expressly requires this, and current `main` still shows Wave 2's pre-merge state. |
| D4 | Do **not** bump `contractVersion` for the tagline-only change; retain `1.0.0`. | The change alters no entitlement, price, route, or public guidance authority. A bump would also require a deliberate contract-test update. |
| D5 | Require a focused clinical-safety copy review of the final exact diff before merge, in addition to the normal doctrine tests. | The prior coordinator handoff assigns Wave 3 copy to that gate; the signed copy does not remove the gate. |

## Parcel queue (blocked until Gate 1 and D3)

1. **W3-P0 — prerequisite reconciliation** (standard risk): verify exact Wave 1/2 merge/PR/base-branch state; produce evidence only. No product files.
2. **W3-P1 — product and metadata copy** (contract-sensitive): update the signed category sentence in `frontend/src/lib/site.ts`, `frontend/src/app/page.tsx`, and `frontend/src/components/marketing/MarketingFooter.tsx`; update taglines in `contracts/product-contract.v1.json`, tier details in `frontend/src/lib/marketing.ts`, and pricing metadata in `frontend/src/app/pricing/page.tsx`. Depends on D4.
3. **W3-P2 — hero, safety, and lockstep regression assertions** (serialized, doctrine-sensitive): update `LandingHero.tsx`, `frontend/src/app/safety/page.tsx`, and only the two stale-copy assertions in `HomePageHero.test.tsx`. Depends on W3-P1 and the confirmed Wave 2 base.
4. **W3-P3 — evidence and safety closure** (review/verification only): run the exact regression, lint, build, rendered-route, source-sweep, and clinical-safety-copy review checks. Depends on W3-P1 and W3-P2.
5. **W3-P4 — human first-viewport check** (human gate): record feedback from one person unfamiliar with the site. This is an exit-condition gate, not an agent-completable task.

## Exit criterion

The signed copy is merged only after:

1. All five category placements match exactly (P2 may use only the approved trim).
2. The retired strings have zero non-`.audit/` occurrences.
3. `/` and `/pricing` render $0/$12/$29 with the new taglines, and the hero and `/safety` share `Just what's known.`
4. `HomePageHero.test.tsx` changes only the two permitted stale-copy assertions; sacred doctrine assertions remain unchanged.
5. `npm test`, `npm run lint`, and `npm run build` pass in `frontend/`.
6. The focused clinical-safety copy review is green, and an unfamiliar-person first-viewport comprehension check is recorded.

## Requested standing authorizations

- **Gate 2 (dispatch):** not granted. Requested only after this charter is ratified and W3-P0 proves the prerequisite.
- **Gate 3 (merge):** not granted. Requested only for a green chain meeting every exit condition; human comprehension feedback and any review finding remain explicit stops.

## Stop conditions

- Wave 1/2 are not merged on the selected base branch.
- A required copy, file, contract version, or scope decision differs from D1–D5.
- Any sacred doctrine assertion would need to change, or the safety review identifies prohibited/Class D language.
- The human comprehension check has not been completed.

