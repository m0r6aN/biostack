# Plan-level adversarial review — wave3-positioning-copy

**Date:** 2026-09-24 · **Target:** ratified charter (Gate 1 D1–D5) · **Base:** `origin/main` `e5b75e0`
**Reviewers (fresh, read-only, no coordinator context):** Codex CLI (`codex exec --sandbox read-only`) → R1–R7; Claude CLI (`claude -p`, plan mode, Read/Grep/Glob only) → F1–F18. Both ran in the detached worktree `.worktrees/w3-plan-review-20260924`. Afterward the worktree was clean except for the two copied goal files. Raw outputs were captured at review time. This table is the durable record.

**Coordinator reproduction (on disk, before ruling):** I confirmed F1 (no `pricingTiers`/price consumer in `app/page.tsx` or `components/marketing`), F2 (`lib/productContract.ts:1` imports `@/contracts/...` mirror, and `sync-product-contract.mjs --check` runs only in `deploy.yml:29`), and F8 (`HomePageHero.test.tsx:24,27` already assert the new H1 and the comma-variant subhead). I also confirmed the F10–F12 source strings (packet `:113-114`), the F11 check-ins group being only `.RequireAuthorization()` (`CheckInEndpoints.cs:13`), the F6 variant in `marketing.ts:29`, and that packet AC2/AC3 are the origin of charter EC2/EC3 (packet `:171-181`).

## Triage

| ID | Sev | Finding (short) | Ruling | Action |
|---|---|---|---|---|
| R1 | blocker | D2 defers original 3.3/3.4 (trust block, FAQ/price strip, disclaimer move, closing CTA) | **accept-as-documented** | This is exactly ratified D2. No change. |
| F1 | blocker | EC3 / packet AC3 requires prices on `/`, but nothing on `/` renders prices and D2 defers the price strip | **fix: human** | Contradiction inside the signed/ratified set → **re-open Gate 1** for D2 × EC3 only (H1). |
| R2/F2 | blocker | P1 names only the canonical contract. The frontend reads `frontend/src/contracts/` and the backend embeds a third copy. The mirror check runs only at deploy. | **fix: applied** | P1 must run `node scripts/sync-product-contract.mjs` and commit all three copies. P3 runs `--check`. |
| R3/F3/F5 | blocker | EC2 / packet AC2 "zero retired strings outside `.audit/`" is unreachable. "Protocol Operations" is a live backend/offline-kit/in-app term, and "Longitudinal Intelligence" has 7 unowned consumers plus a pinning test. | **fix: human** | EC2 is signed acceptance text → **re-open Gate 1** (H2). |
| F4/R5 | blocker | No signed replacement copy exists for `pricingContent[].description` or the `pricing/page.tsx:27-28` intro, or for the billing/TierGate/ProtocolConsole strings. Under D1's verbatim rule, the builder cannot author them. | **fix: human** | H3. |
| R4/F6 | major | #253 landed off-packet variants: eyebrow, subhead (+ "Tracking and analysis come after."), `SITE_TITLE`, `SITE_DESCRIPTION`, JSON-LD, footer | **resolved by D1** | D1 makes packet text authoritative, so P1/P2 overwrite P1–P5 with the verbatim text. Recorded as amendment A3. |
| F7 | major | `marketing.ts:29` FAQ is a sixth, unowned variant of the category sentence. Packet AC1 forbids any variant. It also contains tier-feature claims. | **fix: human** | H4. |
| F8 | major | P2 premise stale: #253 already moved both lockstep assertions. EC4 has no diff base. The subhead regex will need a further lockstep edit. | **fix: applied** | P2/EC4 re-baselined to the P2 dispatch base (A4). The subhead-regex lockstep edit is permitted, since it is the same assertion the packet already designates. |
| F9 | major | P2 collides with Wave 2 in `HomePageHero.test.tsx` card assertions | **fix: applied** | P2 base = the actual Wave 2 merge SHA. Card assertions are verified, never re-edited (A4). |
| F10 | major | Observer detail "No account. Nothing held back." vs 8-compound cap / Operator-gated context | **fix: human (D5)** | Partly disputed: the sentence scopes to library + calculators, which are public. The "Nothing held back" absolute is still an entitlement risk → H5. |
| F11 | major | Operator detail sells check-ins as a paid differentiator, but check-ins are auth-only and not tier-gated | **fix: human (D5)** | Reproduced. Unenforced-entitlement claim → H5. The charter already forbids cap-lift claims without enforcement evidence. |
| F12 | major | "Upgrade when you stop reading about compounds and start running specific ones" can read as a nudge to start using compounds | **fix: human (D5)** | Highest-risk clause → H5. |
| R6/F13 | major/minor | "Every run, side by side." — no side-by-side cross-run surface found | **fix: human (D5)** | Needs evidence of the surface or the signer's acceptance → H5. |
| F14 | minor | Packet premise "contract declares no entitlements" is stale (Observer cap is enforced) | **accept-as-documented** | The charter constraint still holds. Rationale noted here. |
| F15 | info | D4 is forced: the sync script and FE/BE tests pin `1.0.0` | **informational** | Strengthens D4. |
| F16 | minor | OG alt text `'BioStack protocol operations'` (`site.ts:14`) is unowned | **fix: applied** | Added to P1 (A2). |
| F17 | minor | Governance state contradictions (W3-P0 said Gate 1 not ratified; findings file missing) | **fix: applied** | W3-P0 governance section superseded by the charter state log. This file now exists. |
| F18 | minor | Footer-crowding fallback and `SITE_TITLE` ≤~60-char check unowned. The current title is 70 chars. | **fix: applied** | Added to P3 checklist (A5). |
| R7 | info | `LandingHero.tsx` Wave 2 collision correctly serialized | **informational** | — |

## Human decisions required (Gate 1 re-opened for these items only)

- **H1 (D2 × EC3):** Recommend narrowing EC3 to "`/pricing` renders $0/$12/$29 with the new taglines". Prices on `/` return with the deferred 3.3 work.
- **H2 (EC2 scope):** Recommend making the retired-string sweep cover the public marketing/tier surfaces plus all three contract copies, including the in-app tier labels `billing/page.tsx:42`, `TierGate.tsx:19` and `ProtocolConsole.tsx:415,484`. Exclude the backend/offline-kit "Protocol Operations" product name and the `ProtocolConsole` "Protocol Operations" subtitle.
- **H3 (unsigned copy):** Recommend one rule: every tier `description`/eyebrow mirrors its signed tagline verbatim (e.g. `Commander — Every run, side by side.`). Also supply one sentence for the `pricing/page.tsx:27-28` intro, or approve deleting that sentence. Permit the lockstep edit to `launchSafetyCopy.test.ts:33,102`, since both assertions pin copy rather than doctrine.
- **H4 (FAQ variant `marketing.ts:29`):** Recommend replacing only its first sentence with the verbatim category sentence and leaving the remainder for the D5 review.
- **H5 (safety/entitlement):** Recommend running the D5 focused clinical-safety copy review on the packet text now, before build, and having the signer rule on: "Nothing held back"; check-ins as an Operator differentiator; "start running specific ones"; "Every run, side by side".
