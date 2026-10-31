# Accessibility audit: WCAG 2.2 AA

Status: implemented on `feat/a11y-audit`; **not merged** (needs Clint's OK). Audit date: 2026-10-31 (America/New_York).
Scope: the Next.js 16 frontend. No legal review was done or is implied (Clint's rule). This is an engineering audit, not a conformance certificate or a VPAT.

## 1. Method

| Step | Tool | Target |
|---|---|---|
| Baseline | axe-core 4.14 via Playwright (A, AA, 2.1, 2.2 AA, best-practice rule sets), desktop 1280px and mobile 390px | read-only against https://biostack.cc, 19 public routes |
| Baseline | Lighthouse 13.6 accessibility category | prod: `/`, `/auth/signin`, `/contact`, `/knowledge/bpc-157`, `/pricing`, `/tools` |
| Manual code review | keyboard, focus, skip link, landmarks, headings, labels and errors, custom widgets, contrast (token review), reduced motion, target size, alt text, live regions | all of `frontend/src` |
| Keyboard and reflow sweeps | Playwright: Tab order, Escape, focus return, 320px reflow, `prefers-reduced-motion` | local production build |
| Authenticated routes | axe via Playwright with a real dev sign-in (magic link) against a local API and production build | 17 routes (15 reached, see R5): console, my protocol (taking tab), compounds, protocols, check-ins, timeline, account data, deletion confirm, security, alerts, billing, consent, knowledge, tools |
| Verification | same scans and Lighthouse on the final local production build | see section 4 |

Local runs used the real API with an empty dev database, with knowledge reads proxied from production, so compound pages had real content.
The scan script is `frontend/scripts/a11y/scan.mjs` (not part of CI).

## 2. Baseline findings on production (before)

Public routes, desktop and mobile, 129 failing nodes in total:

| Rule | WCAG | Impact | Nodes | Cause |
|---|---|---|---|---|
| `color-contrast` | 1.4.3 | serious | 91 | `text-white/30` to `/46` and the dim emerald, amber and red tints on the dark surface, 3.0:1 to 4.46:1 |
| `label-content-name-mismatch` | 2.5.3 | serious | 12 | `aria-label` on links and buttons that did not contain the visible text |
| `target-size` | 2.5.8 | serious | 7 | inline icon buttons and small links under 24x24 CSS px |
| `region`, `skip-link` | 1.3.1, 2.4.1 | moderate | 16 | no `<main>` or no usable skip target on several app pages |
| `heading-order` | 1.3.1 | moderate | 2 | jumps from h1 to h3 |
| `landmark-unique` | 1.3.1 | moderate | 1 | two unnamed `nav` landmarks |

Lighthouse accessibility on prod: 100, 96, 100, 95, 96, 95.

Manual review found, in addition (axe cannot see these):

| # | Finding | WCAG | Severity |
|---|---|---|---|
| M1 | Mobile sidebar drawer stayed reachable by Tab and screen readers while closed, had no Escape handling, and its toggle had no state | 2.1.2, 4.1.2 | High |
| M2 | Catalog lookup status ("Checking...", results, "Pending review") changed silently | 4.1.3 | High |
| M3 | Notice bell: no focus return on close, unnamed count | 2.4.3, 4.1.2 | Medium |
| M4 | Dose log: after save, focus was lost and the result was not announced | 2.4.3, 4.1.3 | Medium |
| M5 | Many routes shared one document title | 2.4.2 | Medium |
| M6 | Horizontal scroll at 320px on `/pricing`, `/tools`, calculators, protocol cards (`grid-cols-N` without a mobile base) | 1.4.10 | Medium |
| M7 | No global focus indicator; some controls relied on `outline-none` with a faint border change | 2.4.7, 2.4.13 | High |
| M8 | Animations ran regardless of `prefers-reduced-motion` | 2.3.3 | Low |
| M9 | Inline error text used no `role="alert"`, so failures were silent | 4.1.3 | Medium |
| M10 | Pages had no `<main>` (`div`), so the skip link had no target | 1.3.1, 2.4.1 | Medium |

## 3. What was fixed (this PR)

- **Contrast.** A codemod raised the text opacity and tint tokens that fell below the AA floor, with no layout or colour redesign. `contrastFloor.test.ts` documents the floor (white at 50% or more; tinted text by shade). `contrastFloor.test.ts` fails if a new `text-white/NN` below the floor is added.
- **Focus.** One global `:focus-visible` ring in `globals.css` (2px emerald, offset, 3:1 against both surfaces), so every control has a visible indicator even where a component sets `outline-none`.
- **Landmarks and skip link.** A shared `PageMain` (`<main id="main" tabIndex={-1}>`) replaced `div` roots on app pages. Every page has exactly one `main`; the skip link lands on it. Unique `aria-label`s on the nav landmarks.
- **Headings.** Heading levels no longer skip.
- **Names.** Accessible names now contain the visible text (2.5.3), with the extra context in an `sr-only` suffix. Sidebar, link-buttons, dose log and onboarding tests were updated for the new names.
- **Target size.** Small controls reach at least 24x24 CSS px without changing the layout.
- **Sidebar drawer.** `inert` while closed, Escape closes, focus returns to the toggle, `aria-expanded` and `aria-controls` on the toggle.
- **Live regions.** Catalog lookup, dose log result and notice bell updates are announced through `role="status"` or `role="alert"`. Dose log moves focus to the result after saving.
- **Titles.** 16 route `layout.tsx` files give each client route its own `<title>`.
- **Reflow.** `grid-cols-1` base and `flex-wrap` where content overflowed. Verified at 320px on every scanned route.
- **Reduced motion.** `@media (prefers-reduced-motion: reduce)` disables animation and smooth scrolling globally.
- **Errors.** Inline error messages use `role="alert"`.

### CI gate

`frontend/src/__tests__/a11y/axe.routes.test.tsx` renders the key routes and widgets (home-adjacent public pages, sign-in, consent, contact, account data, pricing, privacy, terms, safety, FAQ, how-it-works, the sidebar drawer, notice bell, dose log, taking tab) and runs axe-core under jsdom. It **fails on any serious or critical violation**. It runs in the existing vitest job (`npm test`), so no workflow change was needed. It was verified to fail against the unfixed `main`.
jsdom has no layout, so contrast, target size and reflow are not covered by it. Contrast is covered by `contrastFloor.test.ts` and by the Playwright scan.

## 4. After (final local production build)

| Check | Result |
|---|---|
| axe, 19 public routes, desktop and mobile | 0 violations |
| axe, 15 authenticated routes reached (R5 covers the other two), desktop and mobile | 0 violations |
| Lighthouse accessibility, same six pages as the baseline | 100 on all six (baseline 95 to 100) |
| Keyboard sweep (Tab order, Escape, focus return) | passes on sidebar drawer, notice bell, dose log |
| 320px reflow | no horizontal scroll on scanned routes |
| vitest, `next build` | pass (193 files, 1771 tests). `tsc --noEmit` and `eslint` report errors that exist on `main` in files this PR does not touch (neither runs in CI); no new ones in changed files |

## 5. Remaining (not fixed)

| # | Item | Why not fixed | Severity |
|---|---|---|---|
| R1 | ~~Hydration mismatch on `/auth/signin`~~ **Fixed (follow-up PR).** The passkey option is now read with `useSyncExternalStore` (server snapshot: unsupported), so server and first client render match and the option appears right after hydration. Covered by `SignInPage.hydration.test.tsx`. | Done | n/a |
| R2 | ~~Drawer focus on open~~ **Fixed (follow-up PR).** Opening the drawer moves focus to its Close button; closing returns focus to the toggle. Verified in Chromium at 390px and covered by `Sidebar.drawerFocus.test.tsx`. | Done | n/a |
| R3 | Dark theme only. There is no light theme, so there is no theme-level choice for people who need light mode (not a WCAG 2.2 AA requirement). | Product decision. | Low |
| R4 | Manual screen-reader testing (NVDA, VoiceOver, TalkBack) was **not** performed. Section 7 is an automated stand-in only. | Needs people and devices. | Medium. Do this before claiming conformance. |
| R5 | `/profiles` and `/map-my-stack` redirected to sign-in in the authed scan (`session-unavailable`, a session blip). They were covered by code review and by the axe test, not by the live scan. | Not rerun. | Low |
| R6 | Third-party Stripe Checkout and Customer Portal pages are outside this repo and were not audited. | n/a | n/a |

## 6. How to re-run

```
cd frontend
npm test -- src/__tests__/a11y           # the CI gate
npm i --no-save playwright && npx playwright install chromium
node scripts/a11y/scan.mjs https://biostack.cc /,/pricing,/tools /tmp/out.json
```

## 7. Automated screen-reader stand-in (2026-10-31, after the follow-up PR)

`frontend/scripts/a11y/structure.mjs` walks a route in Chromium and reports what a screen reader would be given: landmarks, heading outline, accessible names, live regions, skip link, title and language, and the Tab order.
Routes: `/`, `/knowledge` (compound list and search), `/knowledge/bpc-157` (a compound page), `/support`, `/auth/signin`, `/contact`, `/pricing`, desktop and 390px.

| Check | Result |
|---|---|
| Exactly one `h1` and one `main`, skip link lands on `#main`, unique titles, `lang="en"` | pass on all routes |
| Heading levels never skip | pass |
| Controls with no accessible name, images without `alt`, unlabeled fields | none |
| Landmarks | named where there is more than one of a kind (`Primary`, `Footer`, `Head to head`, `Compound pages`, `Support topics`); one section landmark per compound-page section |
| Tab order | follows reading order; every stop shows the focus ring; Tab wraps through the browser chrome |
| Home tabs (`List` / `Evidence`) | roving tabindex, ArrowRight moves selection and focus, panel is labelled by its tab |
| Drawer (390px, signed-in shell) | opens with focus on Close; Escape and Close return focus to the toggle; closed drawer is `inert` |

Gaps the pass found, fixed in the follow-up PR:

- `/knowledge`: search results, "no results", and the full compound list appeared silently, and "Show all compounds" unmounted while focused so focus fell to `<body>`. Both now have an always-mounted `aria-live` region, and focus moves to the section heading.
- `/auth/signin`: no way out (no home link, no help link). The logo now links home and a "Help and support" link was added.
- Anonymous visitors triggered `401` console errors on every public page (entitlements) and on `/pricing` (subscription). Both calls now wait for a signed-in session.
- `/knowledge/<unknown>` returned HTTP 200 with a "not found" body and an `index, follow` robots tag. It is now a real 404; a timeout or API error still renders the retrying client fallback.

Known and not changed:

- `/contact` has the in-app header (title and library search) but no site navigation or footer. It is reachable from `/support` and the footer; adding the marketing chrome is a layout decision.
- The compound page shows an `h2` with the compound name directly under the `h1` with the same name.

Still needs a human with NVDA (Windows) and VoiceOver (macOS and iOS), plus TalkBack if Android matters:

- Reading order and verbosity of the verdict block, evidence-tier badges and the "doses found" tables.
- Whether the new live regions are announced once, at the right time, without interrupting typing.
- The home-page tablist, the notice bell, the dose log result and the sidebar drawer with real speech.
- Charts, tooltips (`HelpTip`) and any content that depends on hover.
- Zoom to 200% and 400%, Windows High Contrast, and voice control (Dragon, Voice Control) name matching.
