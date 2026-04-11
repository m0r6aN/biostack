
> **PHASE 2 DIRECTIVE APPLIED - STRATEGIC OVERLAY:** 
> BioStack is the definitive **Protocol Intelligence Platform** for human biology. It is NOT a tracker, peptide app, or supplement log. It is an observability engine for compounds and outcomes. 
> *Core Moat:* Synergy/interaction mapping, pathway overlap detection, timeline correlation engine, evidence-tier knowledge base.
> *Aha Moment:* Within 60 seconds of onboarding, users input 1-3 compounds and see pathway overlap, synergies, conflicts, and timeline initialization.
> *Monetization:* Users pay for reduced mistakes, increased clarity, confidence, and better outcomes through understanding.
> *Growth Engine:* Calculators are the primary acquisition surface and lead generation hook.

---
# BioStack Mission Control — Implementation Handoff & Product Ops

**Document:** 08 — Implementation Handoff  
**Team:** Engineering, Design, Content  
**Version:** 1.0  
**Date:** April 2026  
**Status:** Execution-Ready

---

## Overview

This document is the single authoritative engineering spec for launching BioStack Mission Control as a commercial product. It converts the strategy documents (01–07) into executable tickets, acceptance criteria, file paths, API routes, and go-live gates.

The product is built. The infrastructure for revenue is not. This document closes that gap.

**Fastest path to revenue:** Auth → Landing + Pricing → Stripe → Onboarding → SEO → Everything else.

---

## 1. Priority Matrix

| Item | Epic | Priority | Rationale |
|------|------|----------|-----------|
| User registration + login | Auth | P0 | Blocks all user-specific features, data isolation, and billing |
| JWT auth middleware | Auth | P0 | All existing API routes must be user-scoped |
| Data isolation (user_id on all records) | Auth | P0 | Without this, users share data — a hard blocker |
| Landing page (`/`) | Marketing | P0 | No entry point for unauthenticated visitors currently |
| Pricing page (`/pricing`) | Marketing | P0 | Required for any paid conversion path |
| Stripe Checkout integration | Stripe | P0 | No revenue is possible without payment processing |
| Stripe webhook handler | Stripe | P0 | Subscription status must be written server-side |
| Subscription status on user record | Stripe | P0 | Feature gating cannot function without this |
| Feature gating middleware | Stripe | P0 | Free tier cap enforcement and Pro/Elite gates |
| Onboarding wizard (6 steps) | Onboarding | P0 | New users hit a raw dashboard — this is a UX/trust blocker |
| Disclaimer acceptance (server-side) | Onboarding | P0 | Legal requirement per compliance spec |
| `robots.txt` | SEO | P0 | Must exist before indexing begins |
| `sitemap.xml` | SEO | P1 | Important for discoverability but not day-1 blocking |
| FAQ page (`/faq`) | Marketing | P1 | Reduces support load and improves SEO |
| Terms of Service (`/terms`) | Legal | P1 | Needed before paid users exist; link in Stripe checkout |
| Privacy Policy (`/privacy`) | Legal | P1 | Needed before paid users exist; required by Stripe |
| Public Reconstitution Calculator (`/tools/reconstitution-calculator`) | Tools | P1 | Primary top-of-funnel wedge per strategy doc 07 |
| Public Volume Calculator (`/tools/volume-calculator`) | Tools | P1 | Secondary free tool for SEO and lead capture |
| Public Unit Converter (`/tools/unit-converter`) | Tools | P2 | Always-free utility; lower funnel value than recon calc |
| Email capture on public tools | Tools | P1 | Lead gen per strategy doc 07 — post-calc prompt |
| Glossary index + term pages (`/glossary`, `/glossary/[term]`) | Content | P2 | SEO value but not blocking revenue |
| Blog / educational content (MDX) | Content | P2 | Organic traffic driver; not needed day 1 |
| JSON-LD structured data (FAQPage, HowTo, etc.) | SEO | P1 | Enhances rich snippets for high-value public pages |
| Open Graph + Twitter card metadata | SEO | P1 | Required for social sharing on landing + pricing |
| Stripe Customer Portal link | Stripe | P1 | Needed for users to manage/cancel subscriptions |
| Password reset flow | Auth | P1 | Not blocking day 1 but needed before public launch |
| Email verification | Auth | P2 | Recommended but not strictly blocking |
| First-run detection (empty state) | Onboarding | P0 | New users must be routed to onboarding, not raw dashboard |

---

## 2. Build Order & Dependency Chain

The sequence below is optimized for fastest path to a paid transaction. Nothing is built speculatively — each step unblocks the next.

```
PHASE 1 — FOUNDATION (unblocks everything)
├── [1.1] Database schema migrations: add users table, add user_id FK to all tables
├── [1.2] Auth endpoints: POST /api/auth/register, POST /api/auth/login, POST /api/auth/logout
├── [1.3] JWT middleware: validate token, attach user context to all protected routes
├── [1.4] Scope all existing API endpoints to authenticated user (user_id filtering)
└── [1.5] Frontend auth state: AuthContext, login/register pages, route guards

PHASE 2 — PUBLIC MARKETING LAYER (unblocks conversion)
├── [2.1] Next.js route group: (marketing) with its own layout
├── [2.2] Landing page: app/(marketing)/page.tsx
├── [2.3] Pricing page: app/(marketing)/pricing/page.tsx
├── [2.4] Terms of Service: app/(marketing)/terms/page.tsx
└── [2.5] Privacy Policy: app/(marketing)/privacy/page.tsx

PHASE 3 — STRIPE INTEGRATION (unblocks revenue)
├── [3.1] Stripe products + prices created in Stripe Dashboard
├── [3.2] POST /api/billing/checkout-session endpoint
├── [3.3] POST /api/billing/webhook endpoint (subscription lifecycle events)
├── [3.4] Add subscription_tier + stripe_customer_id to users table
├── [3.5] Feature gating hook/middleware (useSubscription, FeatureGate component)
└── [3.6] GET /api/billing/portal-session endpoint

PHASE 4 — ONBOARDING (unblocks trust + retention)
├── [4.1] First-run detection: GET /api/users/me returns onboarding_complete bool
├── [4.2] Onboarding wizard component (6 screens, modal overlay)
├── [4.3] POST /api/users/onboarding endpoint (accepts wizard payload, stores disclaimer)
└── [4.4] POST-onboarding redirect to dashboard

PHASE 5 — PUBLIC TOOLS (unblocks top-of-funnel)
├── [5.1] Route group: (tools) with public layout
├── [5.2] /tools/reconstitution-calculator page (no auth, email capture)
├── [5.3] /tools/volume-calculator page (no auth, email capture)
└── [5.4] POST /api/leads/capture endpoint (email + source tag)

PHASE 6 — SEO INFRASTRUCTURE (unblocks indexing)
├── [6.1] robots.txt (static file or Next.js route handler)
├── [6.2] sitemap.xml (dynamic Next.js route handler)
├── [6.3] Base metadata layout (OG tags, Twitter cards, canonical URLs)
├── [6.4] JSON-LD components (FAQPage, HowTo, DefinedTerm, Product)
└── [6.5] FAQ page: app/(marketing)/faq/page.tsx

PHASE 7 — CONTENT LAYER (unblocks organic growth)
├── [7.1] Glossary index: app/(marketing)/glossary/page.tsx
├── [7.2] Glossary term pages: app/(marketing)/glossary/[term]/page.tsx
└── [7.3] MDX pipeline for blog/educational content
```

**Critical path:** Phase 1 → Phase 2 → Phase 3 → Phase 4 → Go-live eligible.  
Phases 5–7 can run in parallel after Phase 1 is complete.

---

## 3. Engineering Epics & Tickets

---

### Epic 1: Authentication & Multi-Tenancy

**Goal:** Every user has an isolated account. All data is scoped to the authenticated user. No data leakage between users.

**Acceptance Criteria (Epic-level):**
- A user can register with email + password
- A user can log in and receive a session or JWT
- All existing API endpoints return only data belonging to the authenticated user
- An unauthenticated request to any protected endpoint returns HTTP 401
- A logged-out user is redirected to `/login`

---

#### Ticket 1.1 — Database: Users Table & FK Migrations

**File:** `backend/src/BioStack.Infrastructure/Migrations/`

**Tasks:**
- Create `Users` table: `Id` (Guid), `Email` (unique, not null), `PasswordHash` (not null), `CreatedAt`, `OnboardingComplete` (bool, default false), `DisclaimerAcceptedAt` (DateTime nullable), `SubscriptionTier` (enum: Observer/Operator/Commander, default Observer), `StripeCustomerId` (string nullable), `StripeSubscriptionId` (string nullable)
- Add `UserId` (Guid, FK → Users.Id) to: `Profiles`, `Compounds`, `CheckIns`, `ProtocolPhases`, `KnowledgeEntries`
- Write EF Core migration; ensure SQLite compatibility
- Seed a dev user (email: dev@biostack.local, password: devpass) for local development

**Acceptance Criteria:**
- `dotnet ef database update` runs without error
- All existing tables have a nullable `UserId` column (nullable to allow backfill before enforcement)
- Dev seed user exists after migration

---

#### Ticket 1.2 — Backend: Auth Endpoints

**File:** `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs`

**Routes to implement:**

| Method | Route | Auth Required | Description |
|--------|-------|---------------|-------------|
| POST | `/api/auth/register` | No | Create user account |
| POST | `/api/auth/login` | No | Return JWT + refresh token |
| POST | `/api/auth/logout` | Yes | Invalidate refresh token |
| POST | `/api/auth/refresh` | No | Exchange refresh token for new JWT |
| POST | `/api/auth/forgot-password` | No | Trigger password reset email |
| POST | `/api/auth/reset-password` | No | Accept reset token + new password |
| GET | `/api/users/me` | Yes | Return current user profile + subscription tier + onboarding_complete |
| PATCH | `/api/users/me` | Yes | Update display name, bio stats |

**Register request body:**
```json
{ "email": "string", "password": "string" }
```

**Login response body:**
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 3600,
  "user": {
    "id": "guid",
    "email": "string",
    "subscriptionTier": "Observer|Operator|Commander",
    "onboardingComplete": false
  }
}
```

**Acceptance Criteria:**
- Passwords hashed with BCrypt (min cost factor 12)
- JWT signed with HS256, 1-hour expiry
- Refresh tokens stored in database, 30-day expiry, single-use (rotate on use)
- Register returns 409 if email already exists
- Login returns 401 on bad credentials (do not leak whether email exists)
- All endpoints return `application/json`

---

#### Ticket 1.3 — Backend: JWT Middleware & User Scoping

**File:** `backend/src/BioStack.Api/Program.cs`, `backend/src/BioStack.Infrastructure/`

**Tasks:**
- Register `AddAuthentication().AddJwtBearer()` in `Program.cs`
- Create `ICurrentUserService` interface + `HttpContextCurrentUserService` implementation
- Inject `ICurrentUserService` into all endpoint handlers
- Update all query methods in repositories/handlers to filter by `UserId`
- Mark all existing endpoints with `[RequireAuthorization]` (or `.RequireAuthorization()` in minimal API style)

**Acceptance Criteria:**
- `GET /api/compounds` with a valid JWT returns only the authenticated user's compounds
- `GET /api/compounds` without a JWT returns 401
- `GET /api/compounds` with another user's JWT returns their compounds only (no cross-contamination)
- Unit tests cover user scoping on at least: CompoundEndpoints, ProfileEndpoints, CheckInEndpoints

---

#### Ticket 1.4 — Frontend: Auth Context, Login & Register Pages

**Files:**
- `frontend/src/contexts/AuthContext.tsx`
- `frontend/src/app/(auth)/login/page.tsx`
- `frontend/src/app/(auth)/register/page.tsx`
- `frontend/src/app/(auth)/forgot-password/page.tsx`
- `frontend/src/middleware.ts`

**Tasks:**
- `AuthContext`: stores `user`, `accessToken`, exposes `login()`, `logout()`, `register()` methods; persists refresh token in `httpOnly` cookie via a Next.js API route proxy
- Login page: email + password form, Zod validation, error display, link to register
- Register page: email + password + confirm password form, Zod validation
- `middleware.ts`: redirect unauthenticated users hitting `/dashboard/*`, `/profiles/*`, `/compounds/*`, `/checkins/*`, `/timeline/*`, `/calculators/*`, `/knowledge/*`, `/admin/*` to `/login`
- Redirect authenticated users hitting `/login` or `/register` to `/`

**Route group:** `app/(auth)/` with a minimal centered layout (logo + card)

**Acceptance Criteria:**
- Successful login stores token and redirects to `/` (dashboard or onboarding based on `onboardingComplete`)
- Failed login shows inline error "Invalid email or password"
- Register creates account and auto-logs in (no separate confirm step for MVP)
- Unauthenticated navigation to `/compounds` results in redirect to `/login?next=/compounds`
- After login with `?next=` param, user is redirected to the originally intended route

---

### Epic 2: Public Marketing Layer

**Goal:** Unauthenticated visitors land on a product page, understand the value proposition, see pricing, and can sign up or log in.

**Route Group:** `frontend/src/app/(marketing)/`  
**Layout:** `frontend/src/app/(marketing)/layout.tsx` — public nav (logo, Pricing, FAQ, Glossary, Login CTA, Sign Up CTA), footer (legal links, copyright)

---

#### Ticket 2.1 — Landing Page

**File:** `frontend/src/app/(marketing)/page.tsx`

**Sections (in order):**
1. Hero: headline, subheadline, primary CTA ("Start free"), secondary CTA ("See pricing"), hero image/animation
2. Social proof bar: "Trusted by X researchers" or placeholder metric strip
3. Feature highlights: 3-column grid (Protocol Tracking / Bio-Intelligence / Precision Calculators)
4. Calculator preview: embedded live reconstitution calculator widget (functional, no auth)
5. Pricing teaser: 3-tier card summary with CTA to `/pricing`
6. FAQ teaser: 3 top questions with link to `/faq`
7. CTA banner: "Start free. Upgrade when you're ready."
8. Footer

**Copy source:** `docs/commercialization/03-landing-page-copy.md`

**Acceptance Criteria:**
- Page renders with no auth required
- All CTAs link to correct destinations
- Page passes Lighthouse performance score ≥ 85 on mobile
- OG metadata present (title, description, image)

---

#### Ticket 2.2 — Pricing Page

**File:** `frontend/src/app/(marketing)/pricing/page.tsx`

**Requirements:**
- 3-column pricing card layout: Observer / Operator / Commander
- Monthly/Annual toggle; Annual selected by default; show savings badge ("Save 33%")
- Feature comparison table below cards (full matrix from doc 01)
- FAQ accordion below comparison table (5–7 billing-related questions)
- CTA for each paid tier triggers Stripe Checkout (`POST /api/billing/checkout-session`)
- Observer CTA goes to `/register`
- "Currently on [tier]" state for authenticated users

**Acceptance Criteria:**
- Prices match doc 01: Observer $0 / Operator $12mo/$96yr / Commander $29mo/$228yr
- Annual toggle correctly updates displayed prices and checkout session price IDs
- Stripe Checkout opens in same tab (not popup) on paid tier CTA click
- Authenticated users see their current tier highlighted
- Unauthenticated users hitting a paid tier CTA are prompted to register first, then continue to checkout

---

#### Ticket 2.3 — Legal Pages

**Files:**
- `frontend/src/app/(marketing)/terms/page.tsx`
- `frontend/src/app/(marketing)/privacy/page.tsx`

**Requirements:**
- Static MDX or inline content pages
- Last Updated date visible
- Linked from footer, Stripe checkout, and onboarding disclaimer screen
- Both pages must exist before Stripe goes live (Stripe requires ToS + Privacy Policy URLs in account settings)

**Acceptance Criteria:**
- Pages render with correct legal copy (copy provided separately — see Copy Handoff Checklist)
- Links in footer resolve correctly
- Pages are excluded from feature gating (always public)

---

#### Ticket 2.4 — FAQ Page

**File:** `frontend/src/app/(marketing)/faq/page.tsx`

**Requirements:**
- Accordion component (one open at a time)
- Minimum 15 questions grouped by category: General / Billing / Privacy & Safety / Technical
- `FAQPage` JSON-LD schema injected via `<script type="application/ld+json">`
- Link to `/faq` in marketing nav and footer

**Acceptance Criteria:**
- All questions expand/collapse correctly
- JSON-LD validates in Google's Rich Results Test
- Page is server-rendered (not client-only) for SEO

---

### Epic 3: Onboarding Flow

**Goal:** Every new user completes a guided 6-step wizard before accessing the dashboard. Disclaimer acceptance is recorded server-side. Users who have completed onboarding are never shown it again.

**File:** `frontend/src/app/(app)/onboarding/page.tsx`  
(Route group `(app)` wraps all authenticated app pages)

---

#### Ticket 3.1 — First-Run Detection & Routing

**Tasks:**
- `GET /api/users/me` returns `onboardingComplete: boolean`
- After login, if `onboardingComplete === false`, redirect to `/onboarding`
- If `onboardingComplete === true`, redirect to `/` (dashboard)
- Accessing `/onboarding` while `onboardingComplete === true` redirects to `/`

**Acceptance Criteria:**
- New user after register → `/onboarding`
- Returning user after login → `/` (dashboard)
- Refresh on `/onboarding` with complete user → redirect to `/`

---

#### Ticket 3.2 — Onboarding Wizard Component

**File:** `frontend/src/components/onboarding/OnboardingWizard.tsx`

**Steps (per spec in doc 02):**

| Step | Screen | Key Fields | Required |
|------|--------|------------|----------|
| 1 | Welcome | — (animation + headline) | — |
| 2 | Disclaimer + Consent | Two checkboxes (not medical advice, not prescribing) | Both checked to advance |
| 3 | Profile Creation | Name, age, biological sex, height, weight | Name required; rest optional |
| 4 | Goal Selection | Multi-select: performance / recovery / longevity / body comp / cognitive | At least 1 required |
| 5 | First Compound (optional) | Compound name, category, status | Skip button visible |
| 6 | Dashboard Reveal | Animated "You're in" confirmation | — |

**UI requirements:**
- Full-screen modal overlay on blurred dashboard background
- Progress indicator (step X of 6)
- Back button on steps 3–5
- Skip button on step 5 only
- Wizard state managed in local React state; submitted as a single payload on step 6 confirmation

**Acceptance Criteria:**
- Step 2 CTA is disabled until both checkboxes are checked
- Submitting the wizard fires `POST /api/users/onboarding`
- On success, `onboardingComplete` is set to `true` in AuthContext
- If the API call fails, show an error toast and allow retry
- The wizard is not re-shown after completion

---

#### Ticket 3.3 — Onboarding API Endpoint

**File:** `backend/src/BioStack.Api/Endpoints/OnboardingEndpoints.cs`

**Route:** `POST /api/users/onboarding`

**Request body:**
```json
{
  "disclaimerAccepted": true,
  "profile": {
    "name": "string",
    "age": 30,
    "biologicalSex": "male|female|other|prefer_not_to_say",
    "heightCm": 180,
    "weightKg": 80
  },
  "goals": ["performance", "recovery"],
  "firstCompound": {
    "name": "BPC-157",
    "category": "peptide",
    "status": "active"
  } // nullable
}
```

**Server actions:**
1. Set `Users.OnboardingComplete = true`
2. Set `Users.DisclaimerAcceptedAt = DateTime.UtcNow`
3. Create `Profile` record linked to user
4. Create `Compound` record if `firstCompound` is not null
5. Return `{ success: true }`

**Acceptance Criteria:**
- Returns 400 if `disclaimerAccepted !== true`
- Returns 401 if unauthenticated
- Returns 409 if onboarding already complete (idempotency guard)
- All records created are associated with the authenticated user

---

### Epic 4: Stripe Integration

**Goal:** Users can subscribe to Operator or Commander tier via Stripe Checkout. Subscription status is stored on the user record and used to enforce feature gates.

---

#### Ticket 4.1 — Stripe Configuration & Products

**Tasks:**
- Create Stripe products in dashboard: "BioStack Operator" and "BioStack Commander"
- Create prices: monthly + annual for each product
- Store price IDs in environment variables:
  - `STRIPE_OPERATOR_MONTHLY_PRICE_ID`
  - `STRIPE_OPERATOR_ANNUAL_PRICE_ID`
  - `STRIPE_COMMANDER_MONTHLY_PRICE_ID`
  - `STRIPE_COMMANDER_ANNUAL_PRICE_ID`
  - `STRIPE_WEBHOOK_SECRET`
  - `STRIPE_SECRET_KEY`
- Update `.env.example` with all new Stripe env vars (values blank)

---

#### Ticket 4.2 — Checkout Session Endpoint

**File:** `backend/src/BioStack.Api/Endpoints/BillingEndpoints.cs`

**Route:** `POST /api/billing/checkout-session`

**Request:**
```json
{ "priceId": "price_xxx", "billingInterval": "monthly|annual" }
```

**Server actions:**
1. Look up or create Stripe Customer for authenticated user (store `StripeCustomerId` on first creation)
2. Create Stripe Checkout Session with:
   - `mode: subscription`
   - `success_url: {APP_URL}/billing/success?session_id={CHECKOUT_SESSION_ID}`
   - `cancel_url: {APP_URL}/pricing`
   - `customer: stripeCustomerId`
   - `metadata: { userId: user.Id }`
3. Return `{ url: session.Url }`

**Acceptance Criteria:**
- Returns 401 if unauthenticated
- Returns 400 if `priceId` is not one of the 4 valid price IDs
- Checkout session URL is returned and frontend redirects to it
- Existing Stripe Customer is reused (not duplicated on repeat checkout)

---

#### Ticket 4.3 — Stripe Webhook Handler

**File:** `backend/src/BioStack.Api/Endpoints/BillingEndpoints.cs`

**Route:** `POST /api/billing/webhook`

**Events to handle:**

| Event | Action |
|-------|--------|
| `checkout.session.completed` | Set `SubscriptionTier`, set `StripeSubscriptionId`, set `StripeCustomerId` |
| `customer.subscription.updated` | Update `SubscriptionTier` based on price ID |
| `customer.subscription.deleted` | Set `SubscriptionTier = Observer`, clear `StripeSubscriptionId` |
| `invoice.payment_failed` | Log failure; optionally send notification email |

**Acceptance Criteria:**
- Webhook signature is verified using `STRIPE_WEBHOOK_SECRET` before any processing
- Returns 200 immediately for unhandled event types (do not return 4xx for unknown events)
- `SubscriptionTier` on the user record reflects the active subscription within 5 seconds of event receipt
- Integration tested with Stripe CLI `stripe listen --forward-to localhost:5000/api/billing/webhook`

---

#### Ticket 4.4 — Feature Gating

**Files:**
- `frontend/src/hooks/useSubscription.ts`
- `frontend/src/components/FeatureGate.tsx`
- `frontend/src/lib/featureFlags.ts`

**`useSubscription` hook:** returns `{ tier, isOperator, isCommander, isObserver }` from AuthContext

**`FeatureGate` component:**
```tsx
<FeatureGate requiredTier="Operator" fallback={<UpgradePrompt />}>
  <ReconstitutionCalculator />
</FeatureGate>
```

**`featureFlags.ts`:** maps feature names to minimum required tiers

**Acceptance Criteria:**
- Observer user sees `UpgradePrompt` in place of gated features
- Operator user can access all Operator-tier features
- Gating is enforced client-side for UX and server-side for data security
- Server-side enforcement: all gated API endpoints check `user.SubscriptionTier` before executing

---

#### Ticket 4.5 — Customer Portal

**Route:** `GET /api/billing/portal-session`

**Server actions:**
1. Retrieve `StripeCustomerId` for authenticated user
2. Create Stripe Billing Portal session
3. Return `{ url: portalSession.Url }`

**Frontend:** Settings page includes "Manage Subscription" button that calls this endpoint and redirects to the portal URL.

**Acceptance Criteria:**
- Returns 404 if user has no `StripeCustomerId` (i.e., never subscribed)
- Portal URL is valid and redirects to Stripe-hosted portal
- After portal actions, user is returned to `{APP_URL}/settings`

---

### Epic 5: SEO Infrastructure

**Goal:** All public pages are indexable, have correct metadata, and emit structured data where applicable.

---

#### Ticket 5.1 — robots.txt

**File:** `frontend/src/app/robots.ts` (Next.js 13+ native)

```ts
export default function robots() {
  return {
    rules: [
      { userAgent: '*', allow: '/', disallow: ['/api/', '/admin/', '/onboarding'] },
    ],
    sitemap: `${process.env.NEXT_PUBLIC_APP_URL}/sitemap.xml`,
  };
}
```

**Acceptance Criteria:**
- `GET /robots.txt` returns correct directives
- `/api/`, `/admin/`, `/onboarding` are disallowed
- Sitemap URL is present

---

#### Ticket 5.2 — Dynamic sitemap.xml

**File:** `frontend/src/app/sitemap.ts`

**Includes:**
- All static marketing pages: `/`, `/pricing`, `/faq`, `/glossary`, `/terms`, `/privacy`
- All public tool pages: `/tools/reconstitution-calculator`, `/tools/volume-calculator`, `/tools/unit-converter`
- All glossary term pages (dynamic, fetched from database or MDX files)

**Acceptance Criteria:**
- `GET /sitemap.xml` returns valid XML
- All public pages are listed with `lastmod` and `priority`
- Authenticated app routes (`/compounds`, `/profiles`, etc.) are NOT included

---

#### Ticket 5.3 — Base Metadata Layout

**File:** `frontend/src/app/(marketing)/layout.tsx`

**Metadata to set:**
- `title` (with template: `%s | BioStack Mission Control`)
- `description`
- `openGraph`: title, description, image (`/og-image.png`), url, siteName, type
- `twitter`: card type `summary_large_image`, title, description, image
- `canonical` URL (derived from `NEXT_PUBLIC_APP_URL` + current path)
- `robots: index, follow` for marketing pages; `noindex` for auth + app pages

**Acceptance Criteria:**
- Each marketing page has unique title and description
- OG image resolves correctly when URL is pasted into Twitter/LinkedIn/Slack
- Canonical tag present on all marketing pages

---

#### Ticket 5.4 — JSON-LD Structured Data Components

**File:** `frontend/src/components/seo/`

**Components to build:**

| Component | Schema Type | Used On |
|-----------|-------------|---------|
| `FaqSchema` | FAQPage | `/faq` |
| `HowToSchema` | HowTo | `/tools/reconstitution-calculator` |
| `DefinedTermSchema` | DefinedTerm | `/glossary/[term]` |
| `ProductSchema` | Product | `/pricing` |
| `WebSiteSchema` | WebSite | `/` (landing) |

All components emit `<script type="application/ld+json">` within the page `<head>`.

**Acceptance Criteria:**
- All JSON-LD validates without errors in Google's Rich Results Test
- No schema is emitted on authenticated app pages

---

### Epic 6: Public Calculator Tools

**Goal:** The three calculators are available as standalone, unauthenticated public pages. They serve as the primary top-of-funnel lead magnet. After a calculation, users are prompted to capture their email or sign up.

**Route group:** `frontend/src/app/(tools)/`  
**Layout:** `frontend/src/app/(tools)/layout.tsx` — minimal public layout with nav (Logo + Sign Up CTA)

---

#### Ticket 6.1 — Public Reconstitution Calculator

**File:** `frontend/src/app/(tools)/tools/reconstitution-calculator/page.tsx`

**Requirements:**
- Port the existing calculator logic from `frontend/src/app/calculators/` into a standalone public page
- No auth required
- After first successful calculation, show email capture prompt:
  - "Save this calculation + get the free Reconstitution Reference Card"
  - Email input + "Send it to me" button
  - Fires `POST /api/leads/capture` with `{ email, source: "recon-calculator" }`
- Below calculator: "Want to save unlimited calculations? Create a free BioStack account."
- `HowTo` JSON-LD schema describing how to use the calculator
- Full SEO metadata: title "Peptide Reconstitution Calculator — Free | BioStack", description targeting search intent

**Acceptance Criteria:**
- Calculator produces correct results (parity with authenticated version)
- Email capture fires lead API and shows success confirmation
- Email capture is dismissible without blocking calculator use
- Page passes Lighthouse accessibility score ≥ 90
- HowTo JSON-LD validates in Rich Results Test

---

#### Ticket 6.2 — Public Volume Calculator

**File:** `frontend/src/app/(tools)/tools/volume-calculator/page.tsx`

**Requirements:**
- Port existing volume calculator logic
- Post-calculation email capture prompt (same pattern as ticket 6.1, `source: "volume-calculator"`)
- SEO metadata targeting "peptide volume calculator" and related queries

**Acceptance Criteria:**
- Parity with authenticated calculator
- Email capture present
- SEO metadata present

---

#### Ticket 6.3 — Public Unit Converter

**File:** `frontend/src/app/(tools)/tools/unit-converter/page.tsx`

**Requirements:**
- Port existing unit conversion calculator
- Unit converter is "always free" per pricing matrix — no email gate required
- Post-conversion upgrade prompt: "Working with peptides? The Reconstitution Calculator is free too →"
- SEO metadata targeting unit conversion queries

---

#### Ticket 6.4 — Lead Capture API Endpoint

**File:** `backend/src/BioStack.Api/Endpoints/LeadEndpoints.cs`

**Route:** `POST /api/leads/capture`

**Request:**
```json
{ "email": "string", "source": "recon-calculator|volume-calculator|lead-magnet" }
```

**Server actions:**
1. Validate email format
2. Insert into `Leads` table (Id, Email, Source, CapturedAt) — ignore duplicate emails per source
3. (Future) Trigger email automation via configured provider

**Acceptance Criteria:**
- Returns 200 on success (including duplicate — do not leak whether email already exists)
- Returns 400 on malformed email
- Lead is stored in database
- No auth required

---

### Epic 7: Content Infrastructure

**Goal:** Glossary and FAQ content is manageable, SEO-optimized, and expandable without engineering involvement.

---

#### Ticket 7.1 — Glossary Index Page

**File:** `frontend/src/app/(marketing)/glossary/page.tsx`

**Requirements:**
- Alphabetical index of all glossary terms with links to individual term pages
- A–Z quick-jump navigation
- Search/filter input (client-side filtering)
- Each term listed with a one-line definition excerpt

---

#### Ticket 7.2 — Glossary Term Pages

**File:** `frontend/src/app/(marketing)/glossary/[term]/page.tsx`

**Data source:** MDX files in `content/glossary/[term].mdx` OR database-driven via `GET /api/glossary/[slug]`  
**Recommendation:** MDX for MVP (no database dependency, content team can edit directly)

**MDX frontmatter schema:**
```yaml
---
term: "BAC Water"
slug: "bac-water"
shortDefinition: "Bacteriostatic water used as a reconstitution solvent."
category: "solvents"
relatedTerms: ["reconstitution", "peptide-storage"]
lastUpdated: "2026-04-01"
---
```

**Acceptance Criteria:**
- Term page renders MDX content with correct formatting
- `DefinedTerm` JSON-LD present on each page
- `relatedTerms` renders as linked chips at the bottom of the page
- Canonical URL matches `{APP_URL}/glossary/[slug]`
- 404 page served for unknown slugs

---

## 4. Copy Handoff Checklist

All copy must be approved before the associated page is deployed to production.

| Asset | Page/Feature | Status | Owner |
|-------|-------------|--------|-------|
| Hero headline + subheadline | Landing page | Draft exists (doc 03) | Content |
| Feature highlights (3 items) | Landing page | Draft exists (doc 03) | Content |
| Social proof bar copy | Landing page | Needed | Content |
| CTA button copy | Landing page | Draft exists (doc 03) | Content |
| Pricing tier names + descriptions | Pricing page | Draft exists (doc 01) | Content |
| Pricing feature comparison table | Pricing page | Draft exists (doc 01) | Content |
| Pricing page FAQ (5–7 Qs) | Pricing page | Needed | Content |
| FAQ page (minimum 15 Qs) | FAQ page | Needed | Content |
| Terms of Service | /terms | Needed | Legal |
| Privacy Policy | /privacy | Needed | Legal |
| Onboarding wizard — all 6 screens | Onboarding | Draft exists (doc 02) | Content |
| Disclaimer checkbox text (exact legal wording) | Onboarding step 2 | Draft exists (doc 02) | Legal review |
| Email capture prompt copy (post-calculation) | Public tools | Needed | Content |
| Upgrade prompt copy (gated features) | FeatureGate component | Needed | Content |
| Welcome email (post-register) | Email system | Needed | Content |
| Lead magnet delivery email | Lead capture | Needed (doc 07) | Content |
| Reconstitution Calculator — page headline + meta description | /tools/recon-calculator | Needed | SEO/Content |
| Volume Calculator — page headline + meta description | /tools/volume-calculator | Needed | SEO/Content |
| Unit Converter — page headline + meta description | /tools/unit-converter | Needed | SEO/Content |
| Glossary term content (minimum 20 terms at launch) | Glossary | Needed | Content |
| Admin → billing settings page copy | /settings/billing | Needed | Content |

---

## 5. Design Handoff Checklist

| Artifact | Description | Status | Owner |
|----------|-------------|--------|-------|
| Marketing layout (nav + footer) | Public site nav with logo, links, and auth CTAs | Needed | Design |
| Landing page — hero section | Headline, subheadline, CTA buttons, hero visual | Needed | Design |
| Landing page — feature grid | 3-column feature highlight layout | Needed | Design |
| Pricing cards | 3-tier card layout with toggle, feature list, CTA | Needed | Design |
| Pricing comparison table | Full feature matrix, mobile-responsive | Needed | Design |
| Auth pages (login + register) | Centered card layout, form states, error states | Needed | Design |
| Onboarding wizard — all 6 screens | Full-screen modal, blurred dashboard background, step indicator | Draft exists (doc 02) | Design |
| Onboarding — progress indicator | Step X of 6, visual style | Needed | Design |
| FeatureGate / upgrade prompt | Inline upgrade CTA replacing gated feature | Needed | Design |
| Public tools layout | Minimal nav, calculator card, post-calc email capture | Needed | Design |
| Email capture prompt | Post-calculation modal/inline prompt | Needed | Design |
| Glossary index page | Alphabetical layout, A–Z nav, search input | Needed | Design |
| Glossary term page | MDX prose layout, related terms chips, breadcrumb | Needed | Design |
| FAQ page | Accordion component, category headers | Needed | Design |
| OG image (1200×630) | Brand-correct Open Graph image for social sharing | Needed | Design |
| Favicon + app icon set | 16/32/180/192/512px variants | Exists (partial) | Design |
| Settings / Billing page | Subscription status, tier badge, "Manage Subscription" CTA | Needed | Design |

---

## 6. SEO Content Handoff Checklist

| Asset | Type | Target Keyword(s) | Status |
|-------|------|-------------------|--------|
| Landing page meta title | Title tag | "protocol tracking", "bio-observability" | Needed |
| Landing page meta description | Meta | product-level description, ≤160 chars | Needed |
| Pricing page meta title + description | Title + meta | "BioStack pricing", "protocol Protocol Intelligence Platform subscription" | Needed |
| FAQ page meta title + description | Title + meta | "BioStack FAQ", "peptide Protocol Intelligence Platform questions" | Needed |
| Reconstitution Calculator title + description | Title + meta | "peptide reconstitution calculator", "BAC water calculator" | Needed |
| Volume Calculator title + description | Title + meta | "peptide volume calculator", "injection volume calculator" | Needed |
| Unit Converter title + description | Title + meta | "mcg to mg converter", "peptide unit conversion" | Needed |
| Glossary index title + description | Title + meta | "peptide glossary", "bio-observability terms" | Needed |
| HowTo schema — Reconstitution Calculator | JSON-LD | Structured data for rich snippet | Needed |
| FAQPage schema — FAQ page | JSON-LD | FAQ rich result | Needed |
| DefinedTerm schema — each glossary term | JSON-LD | Term definition rich result | Needed |
| Product schema — Pricing page | JSON-LD | Product/SoftwareApplication rich result | Needed |
| WebSite schema — Landing page | JSON-LD | Sitelinks search box eligibility | Needed |
| OG image (1200×630) | Image | Social sharing across all marketing pages | Needed |
| 20 glossary terms (MDX content) | Content | Long-tail compound + protocol terms | Needed |
| Blog post 1: "How to calculate reconstitution" | MDX | "how to reconstitute peptides", "reconstitution formula" | Needed |
| Blog post 2: "Understanding BAC water" | MDX | "BAC water guide", "bacteriostatic water peptides" | Needed |
| Internal linking map | Document | Cross-links between glossary, tools, blog | Needed |

---

## 7. Launch Checklist (Go-Live Gates)

All items in this checklist must be checked off before the production environment is made publicly accessible.

### Authentication & Security
- [ ] User registration and login functional in production environment
- [ ] JWT tokens expire correctly; refresh token rotation working
- [ ] All API endpoints return 401 for unauthenticated requests
- [ ] All API endpoints return only the authenticated user's data
- [ ] Passwords are hashed (BCrypt, cost ≥ 12) — verified via database inspection
- [ ] HTTPS enforced (HTTP redirects to HTTPS)
- [ ] CORS configured to only allow the production frontend origin
- [ ] Rate limiting on auth endpoints (registration + login)
- [ ] SQL injection protection verified (EF Core parameterized queries — confirm no raw SQL)

### Legal & Compliance
- [ ] Terms of Service page live at `/terms` with approved legal copy
- [ ] Privacy Policy page live at `/privacy` with approved legal copy
- [ ] Disclaimer acceptance stored server-side for every onboarded user
- [ ] "Not medical advice" messaging present on dashboard, onboarding, and public tools
- [ ] Terms + Privacy Policy URLs configured in Stripe Dashboard account settings
- [ ] Cookie consent banner present (if applicable to target markets)

### Payments
- [ ] Stripe account in live mode (not test mode)
- [ ] All 4 price IDs configured in production environment variables
- [ ] Stripe Checkout opens correctly for both tiers and both billing intervals
- [ ] Webhook endpoint receiving events from Stripe (verified in Stripe Dashboard)
- [ ] Subscription lifecycle tested: subscribe → active, cancel → observer, payment fail → handled
- [ ] Customer Portal link functional
- [ ] `/billing/success` page shows confirmation after checkout
- [ ] Stripe dashboard shows test transactions before switching to live

### Onboarding
- [ ] New user after register is routed to onboarding wizard
- [ ] Onboarding wizard completes all 6 steps without errors
- [ ] Disclaimer checkboxes block progression if unchecked
- [ ] Completed user is not shown onboarding again
- [ ] Onboarding data (profile, first compound) saved correctly to database

### Marketing Pages
- [ ] Landing page live and renders correctly on mobile and desktop
- [ ] Pricing page live with correct pricing and working Stripe Checkout CTAs
- [ ] FAQ page live with minimum 15 questions
- [ ] Terms page live with approved copy
- [ ] Privacy page live with approved copy
- [ ] All nav links in marketing layout resolve correctly
- [ ] All footer links resolve correctly

### Public Tools
- [ ] `/tools/reconstitution-calculator` accessible without auth
- [ ] `/tools/volume-calculator` accessible without auth
- [ ] `/tools/unit-converter` accessible without auth
- [ ] Email capture on public tools functional (leads stored in database)
- [ ] Post-calculation upgrade prompt visible to unauthenticated users
- [ ] Tool pages are not blocked by auth middleware

### SEO
- [ ] `robots.txt` returns correct directives at `/robots.txt`
- [ ] `sitemap.xml` returns valid XML at `/sitemap.xml` with all public pages
- [ ] OG image resolves at `/og-image.png` (1200×630)
- [ ] Landing page OG metadata present and correct (verify with opengraph.xyz)
- [ ] Pricing page OG metadata present
- [ ] FAQ page JSON-LD validated in Google Rich Results Test
- [ ] Reconstitution Calculator HowTo JSON-LD validated
- [ ] No `noindex` tags present on any marketing or public tool pages
- [ ] Google Search Console property created and sitemap submitted

### Infrastructure
- [ ] Production environment variables set (all Stripe keys, JWT secret, app URL, DB connection)
- [ ] Database migrations applied to production database
- [ ] Health check endpoint returns 200 (`GET /api/health`)
- [ ] Error monitoring configured (Sentry or equivalent)
- [ ] Logging configured and shipping to persistent store
- [ ] Backup strategy in place for production database
- [ ] Docker Compose production config tested end-to-end
- [ ] Domain DNS configured and propagated
- [ ] SSL certificate valid and auto-renewing

### Quality Assurance
- [ ] End-to-end test: register → onboard → add compound → check in → upgrade → view gated feature
- [ ] End-to-end test: land on `/` → click pricing → Stripe Checkout → subscription active
- [ ] End-to-end test: public calculator → email capture → lead stored in DB
- [ ] Mobile (375px) layout verified on: landing, pricing, onboarding, dashboard
- [ ] No console errors on any production page load
- [ ] Lighthouse scores: Performance ≥ 85, Accessibility ≥ 90, SEO ≥ 95 on landing page

---

## 8. New API Routes Reference

Complete list of all new backend routes required for launch, beyond what already exists.

| Method | Route | Auth | Epic | Priority |
|--------|-------|------|------|----------|
| POST | `/api/auth/register` | No | Auth | P0 |
| POST | `/api/auth/login` | No | Auth | P0 |
| POST | `/api/auth/logout` | Yes | Auth | P0 |
| POST | `/api/auth/refresh` | No | Auth | P0 |
| POST | `/api/auth/forgot-password` | No | Auth | P1 |
| POST | `/api/auth/reset-password` | No | Auth | P1 |
| GET | `/api/users/me` | Yes | Auth | P0 |
| PATCH | `/api/users/me` | Yes | Auth | P1 |
| POST | `/api/users/onboarding` | Yes | Onboarding | P0 |
| POST | `/api/billing/checkout-session` | Yes | Stripe | P0 |
| POST | `/api/billing/webhook` | No (Stripe signature) | Stripe | P0 |
| GET | `/api/billing/portal-session` | Yes | Stripe | P1 |
| POST | `/api/leads/capture` | No | Tools | P1 |
| GET | `/api/health` | No | Infra | P0 |

---

## 9. New Frontend Routes Reference

Complete list of all new Next.js routes required for launch.

| Route | File Path | Auth | Group | Priority |
|-------|-----------|------|-------|----------|
| `/` | `app/(marketing)/page.tsx` | No | marketing | P0 |
| `/pricing` | `app/(marketing)/pricing/page.tsx` | No | marketing | P0 |
| `/faq` | `app/(marketing)/faq/page.tsx` | No | marketing | P1 |
| `/terms` | `app/(marketing)/terms/page.tsx` | No | marketing | P1 |
| `/privacy` | `app/(marketing)/privacy/page.tsx` | No | marketing | P1 |
| `/glossary` | `app/(marketing)/glossary/page.tsx` | No | marketing | P2 |
| `/glossary/[term]` | `app/(marketing)/glossary/[term]/page.tsx` | No | marketing | P2 |
| `/login` | `app/(auth)/login/page.tsx` | No | auth | P0 |
| `/register` | `app/(auth)/register/page.tsx` | No | auth | P0 |
| `/forgot-password` | `app/(auth)/forgot-password/page.tsx` | No | auth | P1 |
| `/onboarding` | `app/(app)/onboarding/page.tsx` | Yes | app | P0 |
| `/settings` | `app/(app)/settings/page.tsx` | Yes | app | P1 |
| `/settings/billing` | `app/(app)/settings/billing/page.tsx` | Yes | app | P1 |
| `/billing/success` | `app/(app)/billing/success/page.tsx` | Yes | app | P0 |
| `/tools/reconstitution-calculator` | `app/(tools)/tools/reconstitution-calculator/page.tsx` | No | tools | P1 |
| `/tools/volume-calculator` | `app/(tools)/tools/volume-calculator/page.tsx` | No | tools | P1 |
| `/tools/unit-converter` | `app/(tools)/tools/unit-converter/page.tsx` | No | tools | P2 |

**Note on existing routes:** The current `app/` routes (`/profiles`, `/compounds`, `/checkins`, etc.) must be moved into the `(app)` route group so they inherit the authenticated layout and middleware guards. This is a refactor ticket, not new work.

---

## 10. Risks & Blockers

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Auth implementation takes longer than estimated due to refresh token complexity | Medium | High — blocks all Phase 2+ work | Use `ASP.NET Core Identity` or a proven JWT library rather than rolling from scratch; use `next-auth` on the frontend |
| Stripe webhook handler misses subscription events in production | Medium | High — users billed but not upgraded | Test with Stripe CLI locally; implement idempotency keys; add monitoring alert for failed webhook deliveries |
| Legal copy (ToS + Privacy) not ready before Stripe goes live | High | High — Stripe requires these URLs in account settings | Assign legal copy as first-priority content task; use a lawyer-reviewed template for MVP |
| Existing app routes not properly guarded after auth is added | High | Critical — data leakage between users | Auth middleware must be tested with two separate test accounts before any real data exists |
| Data migration: existing SQLite rows have no `user_id` | Low (dev-only data) | Low — dev environment only | Document: wipe dev DB on first auth migration; do not attempt to migrate existing rows |
| SEO pages indexed before Terms/Privacy are live | Medium | Medium — Stripe + trust issues | `noindex` all marketing pages in staging; only remove `noindex` from production after legal pages are approved |
| Feature gating enforced only client-side | High | High — trivially bypassed | All gated features must validate `SubscriptionTier` on the server; client-side gating is UX-only |
| Onboarding wizard can be skipped by navigating directly to `/` | Medium | Medium — disclaimer not recorded | Server-side check on all app routes: if `onboardingComplete === false`, redirect to `/onboarding`; enforce in middleware.ts |
| Public tool pages are accidentally included in auth middleware | Medium | Medium — blocks top-of-funnel traffic | Explicitly list protected route prefixes in `middleware.ts`; public tools must be excluded |
| Stripe test mode keys accidentally used in production | Low | High — payments silently fail | Enforce `STRIPE_SECRET_KEY` format check at startup: `sk_live_` prefix required in `NODE_ENV=production` |

---

## Appendix A: Environment Variables Required

All new environment variables needed beyond what currently exists:

**Backend (`backend/src/BioStack.Api/appsettings.json` / secrets):**
```
JWT_SECRET                         # ≥32 random bytes, base64 encoded
JWT_ISSUER                         # e.g., "biostack-mission-control"
JWT_AUDIENCE                       # e.g., "biostack-users"
STRIPE_SECRET_KEY                  # sk_live_xxx (sk_test_xxx for dev)
STRIPE_WEBHOOK_SECRET              # whsec_xxx
STRIPE_OPERATOR_MONTHLY_PRICE_ID   # price_xxx
STRIPE_OPERATOR_ANNUAL_PRICE_ID    # price_xxx
STRIPE_COMMANDER_MONTHLY_PRICE_ID  # price_xxx
STRIPE_COMMANDER_ANNUAL_PRICE_ID   # price_xxx
```

**Frontend (`frontend/.env.local`):**
```
NEXT_PUBLIC_APP_URL                # https://app.biostack.io (or localhost:3000 for dev)
NEXT_PUBLIC_API_URL                # https://api.biostack.io/api (or localhost:5000/api)
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY # pk_live_xxx
```

---

## Appendix B: File Structure Summary (New Files Only)

```
frontend/src/
├── app/
│   ├── (auth)/
│   │   ├── layout.tsx
│   │   ├── login/page.tsx
│   │   ├── register/page.tsx
│   │   └── forgot-password/page.tsx
│   ├── (marketing)/
│   │   ├── layout.tsx
│   │   ├── page.tsx                         ← landing page
│   │   ├── pricing/page.tsx
│   │   ├── faq/page.tsx
│   │   ├── terms/page.tsx
│   │   ├── privacy/page.tsx
│   │   └── glossary/
│   │       ├── page.tsx
│   │       └── [term]/page.tsx
│   ├── (tools)/
│   │   ├── layout.tsx
│   │   └── tools/
│   │       ├── reconstitution-calculator/page.tsx
│   │       ├── volume-calculator/page.tsx
│   │       └── unit-converter/page.tsx
│   ├── (app)/                               ← move existing routes here
│   │   ├── layout.tsx
│   │   ├── onboarding/page.tsx
│   │   ├── settings/
│   │   │   ├── page.tsx
│   │   │   └── billing/page.tsx
│   │   └── billing/
│   │       └── success/page.tsx
│   ├── robots.ts
│   └── sitemap.ts
├── contexts/
│   └── AuthContext.tsx
├── hooks/
│   └── useSubscription.ts
├── components/
│   ├── onboarding/
│   │   └── OnboardingWizard.tsx
│   ├── seo/
│   │   ├── FaqSchema.tsx
│   │   ├── HowToSchema.tsx
│   │   ├── DefinedTermSchema.tsx
│   │   ├── ProductSchema.tsx
│   │   └── WebSiteSchema.tsx
│   └── FeatureGate.tsx
├── lib/
│   └── featureFlags.ts
└── middleware.ts

backend/src/BioStack.Api/Endpoints/
├── AuthEndpoints.cs
├── OnboardingEndpoints.cs
├── BillingEndpoints.cs
└── LeadEndpoints.cs

content/
└── glossary/
    └── *.mdx
```

---

*This document supersedes any conflicting implementation notes in documents 01–07. All tickets reference this document as the authoritative source of acceptance criteria.*
