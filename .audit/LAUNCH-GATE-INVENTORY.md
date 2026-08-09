# LAUNCH-GATE INVENTORY — public routes reachable from the landing page

**Run:** 2026-08-08, against `D:\Repos\BioStack\frontend/src/app`, repo HEAD `9cecaf7`.
**Method:** line-count every public `page.tsx`; grep all pages for `index: false` and for
unfinished markers (`not final`, `legal review`, `coming soon`, `under construction`,
`launch plumbing`, `TODO`, `FIXME`, `WIP`, `stub`, `lorem`); read every thin file to
distinguish a stub from a thin wrapper delegating to a real component.

## Result: 2 stubs, both legal, both self-declared launch gates

| Route | Lines | Indexed | Status |
|---|---|---|---|
| **`/privacy`** | 31 | **noindex** | **STUB — launch gate** |
| **`/terms`** | 31 | **noindex** | **STUB — launch gate** |

Both files are byte-for-byte the same pattern: an amber `Legal review required` eyebrow, a real
`<h1>`, and this body:

> "This route now exists for launch plumbing, but the copy is intentionally not final. The
> commercialization brief marks an approved Privacy Policy as mandatory before payments and launch."

Both are linked from `MarketingFooter.tsx`, which renders on **every public page** — so every route
in this inventory is one click from an unfinished legal page. `robots: { index: false, follow: false }`
keeps them out of search results but does nothing for a visitor who clicks.

**These are not council findings.** The product's own commercialization brief already designates an
approved Privacy Policy as mandatory before payments and launch. This inventory only establishes
that the gate is still open and that Terms sits in the same state.

## Everything else is real

| Route | Lines | Verdict |
|---|---|---|
| `/` | 50 | Real. Under Round 1 remediation. |
| `/start` | 27 | **Thin wrapper, not a stub** — delegates to `OnboardingExperience`, handles `?mode=existing`. Wave 2's primary CTA destination is sound. |
| `/tools/analyzer` | 20 | **Thin wrapper, not a stub** — delegates to `AnalyzerExperience`. Real metadata. |
| `/tools` | 20 | **Thin wrapper, not a stub** — delegates to `ToolsDecisionSurface`. |
| `/pricing` | 92 | Real. Renders live prices from `pricingTiers` → `formatMonthlyPrice`. |
| `/providers` | 172 | Real. |
| `/knowledge` | 260 | Real. |
| `/how-it-works` | 128 | Real. |
| `/safety` | 55 | Real, and notably strong — see below. |
| `/faq` | 62 | Real. |
| `/billing` | 252 | Real. |
| `/onboarding` | 6 | Intentional redirect to `canonicalRoutes.onboarding`. |

Every other `placeholder` grep hit was a legitimate `<input placeholder=…>` attribute in admin,
auth, knowledge, or protocols. No false positives remain.

## Two incidental findings worth carrying forward

**1. `/safety` is the tonal reference the rest of the site should be measured against.** Its H1 is
`BioStack is not a doctor.` — the identical line as the landing disclaimer strip. That echo is
deliberate and it works: the doctrine language is consistent across surfaces, which is exactly what
Round 1 found missing from the *category* language. Its closing line — "No prescriptions. No
guesswork. Just structure." — is the clearest, most confident sentence on the entire public site.

**2. The self-description count is higher than Round 1 reported.** F2 counted four. `/safety` adds a
fifth on-page variant: "It is infrastructure for tracking, math, and clarity." Full set now:

| Surface | Self-description |
|---|---|
| `<title>` | Protocol Operations |
| meta description | Your protocol operations system |
| JSON-LD | Tracking, calculator, and stack mapping infrastructure for compound protocols |
| landing footer | Tracking, math, and clarity for complex stacks |
| `/safety` body | Infrastructure for tracking, math, and clarity |
| FAQ (`marketing.ts`) | A personal bio-protocol operating system |

Six variants. F2's severity is unchanged; its evidence base is now broader.

## Correction to the Round 1 directive

Finding **F3** justified its BLOCKER partly on the claim that *local-first* is BioStack's unspent
differentiator. **That claim was false and is retracted.** The string `local-first` appears **zero
times** across `.md`, `.ts`, `.tsx`, `.cs` and `.json` in this repo (node_modules excluded).
Persistence is server-side, provider-selected by `Database:Provider` (Npgsql or SQLite); the Docker
compose is local *orchestration*, a dev-environment fact, not a user-data-custody claim. The only
shipped user-facing storage language is the `marketing.ts` FAQ answer — "stored securely and is
private to your account" — which describes hosted storage.

The error originated in a stale project-memory note and was repeated without verification against
code. Memory has been corrected with the grep result and a standing warning.

**F3's severity does not change — its justification does, and the replacement is stronger.** The
page's only trust link lands on a legal stub that announces it is unfinished and cites the
commercialization brief blocking launch on it. That is a better reason to block than the one
originally given, and it is verifiable rather than inherited.

## What this changes downstream

- **Data-custody copy is not a council task.** No panel can draft trustworthy custody language while
  the policy is unapproved and the destination is a stub. Route to legal.
- **The other two positioning artifacts are unblocked** — category sentence and tier ladder — and
  the tier ladder is now trivially sourced: Observer **$0** → `/start`, Operator **$12/mo** →
  `/billing?plan=operator`, Commander **$29/mo** → `/billing?plan=commander`
  (`contracts/product-contract.v1.json`, contractVersion 1.0.0).
- **Round 1's scope fence had a cost.** Every seat was told "landing page only," so nobody followed
  the footer link. The fence was correct for the round's purpose and it hid a launch gate. Future
  rounds should permit one-hop destination checks on any link the page under review depends on for
  trust or conversion.
