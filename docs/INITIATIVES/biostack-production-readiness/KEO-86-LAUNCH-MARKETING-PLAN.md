# KEO-86 BioStack Launch and Marketing Plan

Status: planning complete; execution is **NO-GO / HOLD**.

Date: 2026-07-26

Evidence baseline: `origin/main@53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`

## 1. Decision boundary

This is a low-cost, evidence-led plan for work that may begin only when its
specific gate is open. It is not a release decision, campaign authorization,
claims approval, privacy approval, purchasing authorization, or permission to
contact prospective users.

The repository's current launch evidence remains `NO-GO / HOLD`. In
particular:

- legal terms and the privacy policy are unapproved placeholders;
- production billing, authentication, consent, database, monitoring, backup,
  rollback, accessibility, analytics, support, and live smoke evidence remain
  incomplete;
- provider requests have a durable intake seam, but no staffed owner,
  notification destination, response SLA, or generally available multi-client
  product;
- the evidence baseline at this commit does not establish a successful hosted
  release-SHA deployment or a GO-qualified release across all live gates;
- only the release owner may change the release posture after all required
  evidence and human approvals are recorded.

Therefore the immediate authorized marketing state is preparation only:
research the message, prepare owned content, define privacy-safe measurement,
and stage experiments in documents. Do not publish a launch announcement,
activate checkout promotion, enable analytics, send email, perform outreach,
buy media, or describe BioStack as production-ready.

## 2. Evidence hierarchy

Marketing claims must be supported in this order:

1. Current server-enforced product contract and focused tests.
2. Current customer-surface inventory and launch-readiness ledger.
3. Current public UI behavior at the exact release SHA.
4. Dated live evidence from the deployed release SHA.
5. Recorded human approvals for legal, privacy, claims, security, billing, and
   release.

Historical strategy documents, mockups, TODOs, future roadmaps, feature flags,
unmerged pull requests, and test fixtures are not launch claims.

## 3. Currently supportable capability story

The plan may prepare messages about these repository-backed capabilities. A
message may go live only after the matching release and claims gates pass.

| Capability | Supportable description | Boundary |
| --- | --- | --- |
| Public calculators | Transparent unit and concentration arithmetic from values the visitor supplies | No recommended amount, schedule, administration method, or statement that an output is safe |
| Compound records | Organize compound names, status, dates, and user-entered notes | No endorsement, sourcing, treatment plan, or claim that a compound is appropriate |
| Check-ins and timeline | Record observations and view them alongside protocol events over time | Correlation is not causation; no diagnosis or treatment inference |
| Evidence library | Browse compound summaries, evidence tiers, references, uncertainty, and caution context | No claim of complete coverage, clinical validation, compatibility, or efficacy |
| Protocol analyzer | Operator/Commander-gated analysis of user-supplied protocol records | Observational signals only; no instructions to add, remove, substitute, dose, or optimize |
| Reviewed relationship context | Surface reviewed or qualified relationship evidence when it exists | Zero findings and missing reviewed evidence remain Unknown |
| Observer | Free public and basic tracking surfaces, including the current eight-active-compound limit | Do not promise unavailable paid or future features |
| Operator | Current-stack analysis, weekly calendar, lifestyle framework, progress and milestone surfaces | $12 monthly is a product contract, not a sellable offer until billing and release gates pass |
| Commander | Protocol review, pattern, drift, sequence, monitoring, and mission-control surfaces | $29 monthly is gated; no clinical, causal, or outcome claims |
| Provider pilot intake | Collect privacy-minimal contact, organization, role, and consent fields for a review queue | No promise of access, follow-up time, multi-client capability, sharing, revocation, or clinical workflow |

## 4. Ideal customer profiles

### ICP 1: The evidence-conscious protocol organizer

- Already maintains their own records and has independently chosen what to
  track.
- Uses notes or spreadsheets and wants clearer organization, arithmetic,
  evidence references, and longitudinal observations.
- Values explicit uncertainty and wants to distinguish a recorded observation
  from a recommendation.
- Best initial surfaces: public tools, evidence library, Observer onboarding,
  check-ins, and timeline.

Do not target people asking BioStack to select compounds, determine doses,
interpret symptoms, diagnose conditions, or replace professional care.

### ICP 2: The spreadsheet-constrained longitudinal tracker

- Has repeated check-ins or protocol phases and struggles to reconstruct what
  changed and when.
- Wants consistent records and reviewable history rather than an optimization
  engine.
- Best eventual offer: Operator after billing, consent, and release gates pass.

The message is “make the record easier to inspect,” not “get better outcomes.”

### ICP 3: The qualified provider-pilot evaluator

- A provider, researcher, or operational lead willing to evaluate a strictly
  observational pilot boundary.
- Understands that BioStack is not an EHR, medical device, prescribing tool, or
  clinical decision-support system.
- Best surface: the existing provider-pilot request page after provider
  operations, privacy terms, response ownership, and pilot controls are
  approved.

This is not a generally available provider product. No outbound provider
recruitment begins while the release remains on HOLD.

### Excluded audiences

- Minors.
- People seeking suppliers, purchasing guidance, injection instructions,
  cycles, post-cycle therapy, diagnosis, or individualized dosing.
- Clinics expecting an EHR, HIPAA BAA, patient-management system, clinical
  decision support, or prescribing workflow.
- Buyers whose core requirement is guaranteed safety, efficacy, outcomes, or
  complete evidence coverage.

## 5. Positioning and message hierarchy

### Category

BioStack is observational protocol organization and evidence context software.

### Primary promise

Keep protocol records, arithmetic, evidence references, and observations in
one inspectable place—with uncertainty visible.

### Supporting pillars

1. **Organize:** replace scattered notes with structured protocol records,
   phases, and timelines.
2. **Observe:** align user-entered check-ins with recorded events without
   claiming causation.
3. **Inspect:** view evidence tiers, references, cautions, and qualified
   relationship signals.
4. **Calculate:** perform transparent arithmetic from user-supplied values
   without recommending inputs or actions.
5. **Preserve uncertainty:** treat missing evidence and zero findings as
   Unknown, not safe, compatible, or conflict-free.

### Trust statement

BioStack does not prescribe, diagnose, recommend compounds or dosing, provide
administration or sourcing instructions, or replace qualified professional
care.

### Message order by surface

| Surface | First message | Proof | Boundary |
| --- | --- | --- | --- |
| Homepage | Organize and inspect an observational protocol record | Timeline, calculators, evidence references | Not medical advice or an outcome engine |
| Public tool | Transparent arithmetic from your inputs | Formula, units, validation, accessible visual | Inputs are not recommendations |
| Evidence page | See the evidence tier, source, and uncertainty | References and caution context | No complete-coverage or compatibility claim |
| Start/Observer | Begin with a useful free organizational record | Current Observer capabilities | No trial, checkout, or paid promise while HOLD |
| Pricing | Compare only server-enforced capabilities | Versioned product contract | Paid CTAs activate only after billing/legal/release proof |
| Provider | Request consideration for an observational pilot | Privacy-minimal durable request | No access or multi-client promise |

## 6. Explicit claims exclusions

Do not publish, test, paraphrase, or imply:

- “safe,” “proven safe,” “effective,” “clinically proven,” “medical-grade,” or
  “doctor approved” unless an approved claim record provides the exact scope;
- that BioStack improves health outcomes, prevents mistakes, reduces adverse
  events, or tells a user what to do;
- recommended doses, schedules, timing, cycles, escalation, tapering,
  substitutions, combinations, “pairs well with,” or “compatible blends”;
- that zero overlap findings mean no conflicts or compatibility;
- diagnostic, treatment, prescribing, clinical decision-support, or
  individualized optimization claims;
- causal claims from check-ins, timelines, patterns, drift, or correlations;
- supplier, purchasing, gray-market, reconstitution, injection, or
  administration instructions;
- that the evidence library is exhaustive, current for every jurisdiction, or
  a substitute for primary-source or professional review;
- that provider multi-client access, sharing, revocation, exports, EHR
  integration, HIPAA compliance, a BAA, or clinical workflows are generally
  available;
- that legal terms, privacy terms, analytics consent, security review,
  production billing, backups, monitoring, support, accessibility, or the
  release itself are approved before dated evidence exists;
- annual billing, a free paid-tier trial, discounts, priority support, PDF
  reports, future AI features, or other roadmap items not in the enforced
  release contract;
- “private,” “secure,” “anonymous,” or “we never share” beyond the exact
  approved privacy and security language.

## 7. Offer and funnel

### Offer architecture

| Offer | Contract | Activation rule |
| --- | --- | --- |
| Observer | $0; public/basic organizational surfaces; up to eight active compounds | May be promoted only after auth, consent, legal/privacy, support, accessibility, and deployed smoke gates pass |
| Operator | $12 per month; enforced Operator capabilities | Do not promote checkout until Stripe products/prices, webhook lifecycle, portal, entitlement, refund/cancellation, and release evidence pass |
| Commander | $29 per month; enforced Commander capabilities | Do not promote until every Commander claim is verified on the release SHA and support expectations are approved |
| Provider pilot request | No public price or access promise | Do not recruit until a staffed owner, SLA, privacy terms, notification, qualification, access, and revocation boundaries exist |

No launch discount, annual plan, credit-card trial, or paid onboarding offer is
authorized by this plan.

### Conditional funnel

The funnel is staged, not active:

1. High-intent owned discovery: public calculator, evidence, safety, or
   protocol-organization query.
2. Useful anonymous experience: complete arithmetic or inspect evidence without
   surrendering health details.
3. Boundary-aware next step: view how BioStack organizes records and preserves
   uncertainty.
4. Observer activation: create an account and complete one non-sensitive
   organizational milestone.
5. Return value: revisit a timeline, record a check-in, or inspect an evidence
   source.
6. Operator consideration: encounter a genuinely enforced paid capability and
   review monthly pricing.
7. Checkout: only after billing and release gates pass.
8. Retention: product value, not health-result promises, drives renewal.

Provider pilot requests remain a separate funnel and never enter consumer
checkout automatically.

## 8. Low-cost channel mix

The percentages are planning effort, not authorized spend.

| Channel | Effort | Role | Gate |
| --- | ---: | --- | --- |
| Owned high-intent tools and evidence pages | 40% | Capture specific arithmetic, evidence-tier, and protocol-organization intent | Claims, SEO, accessibility, public-content boundary, and deployed crawl |
| Product-led onboarding and in-product education | 25% | Convert anonymous value into an Observer organizational milestone | Auth, consent, privacy, product QA, and analytics approval |
| Founder/editorial education | 20% | Explain uncertainty, record quality, source inspection, and observational methods | Claims review and release-owner approval before publication |
| Consented lifecycle messaging | 10% | Help activated users return to unfinished organizational work | Approved consent basis, email provider, preferences, deletion, retention, and unsubscribe |
| Provider-pilot owned content | 5% | Describe the qualification boundary and collect eligible requests | Provider operations and privacy gates |
| Paid acquisition | 0% initially | Reserved for later validation | Requires proven activation, paid conversion, retention, privacy-safe attribution, and a written spend cap |

Organic participation must be educational and transparent. No unsolicited
direct messages, scraped lists, purchased lists, affiliate claims, covert
promotion, or community spam.

## 9. Ninety-day conditional calendar

The 90-day clock starts only after the release owner records GO for the exact
release SHA. Until then, every item below is a draft or internal preparation
task.

| Period | Objective | Planned work | Exit evidence |
| --- | --- | --- | --- |
| Pre-clock HOLD | Prepare without launching | Claims inventory, content drafts, analytics specification, owner assignments, gate evidence | Release owner records GO; no campaign activity before then |
| Days 1–7 | Establish trustworthy owned entry points | Verify production metadata, crawl, public tools, evidence pages, safety copy, terms/privacy versions, and support route | Release-SHA crawl and claims approval |
| Days 8–14 | Observe anonymous utility | Publish only approved tool/evidence content; review page-to-start behavior | Privacy-approved aggregate measurement |
| Days 15–21 | Improve boundary-aware onboarding | Test organization-first versus evidence-first start copy | Account-start and activation deltas without health payloads |
| Days 22–30 | Validate first retained value | Improve the first saved record, first timeline view, and first evidence-source view | Activation and seven-day return cohort |
| Days 31–45 | Build one narrow organic cluster | Create source-cited content around record organization, evidence tiers, uncertainty, and calculator transparency | Qualified visits and activation, not impressions alone |
| Days 46–60 | Validate paid consideration | Clarify enforced Operator/Commander differences; inspect paywall-to-pricing continuity | Pricing comprehension and checkout-start evidence; billing gates remain mandatory |
| Days 61–75 | Improve consented return paths | Add only approved lifecycle reminders for unfinished setup or requested summaries | Unsubscribe, complaint, return, and deletion metrics |
| Days 76–90 | Consolidate evidence | Stop weak experiments, document winners, assess retention and support load, decide whether any capped paid test is justified | Day-30 retention, paid conversion, refund/support burden, privacy review |

At day 90, do not scale merely because acquisition is cheap. Continue only if
activation, retention, support capacity, privacy compliance, and claim quality
remain acceptable.

## 10. Budget bands

| Band | Monthly external spend | Use | Activation rule |
| --- | ---: | --- | --- |
| HOLD | $0 | Repository research, internal drafts, gate closeout | Current state |
| Organic validation | $0–$250 | Approved domain/search tooling, lightweight creative, consented email infrastructure | GO plus privacy, analytics, support, and claims approval |
| Proof-led growth | $250–$750 | Freelance editing/design or a tightly bounded high-intent experiment | Four weeks of qualified activation evidence and an accountable owner |
| Capped acquisition test | $750–$1,500 | One narrow search-intent test; no broad social prospecting | Proven checkout, paid conversion, early retention, attribution, refund path, and written stop-loss |
| Scale | Not authorized | Requires a separate plan and budget decision | Cohort economics, support, privacy, and retention evidence |

No paid media is part of the initial mix. Software should remain free or
already-owned where practical. Do not prepay annual marketing tools while the
launch is on HOLD.

## 11. Ownership and dependencies

| Owner | Named accountable person | Accountable evidence | Dependency for marketing |
| --- | --- | --- | --- |
| Product owner | Clint Morgan | Product scope and release-candidate recommendation | Coordinates the product decision without replacing independent gate owners |
| Release owner | Unassigned | Exact-SHA GO decision | Starts the 90-day clock |
| Evidence reviewer | Clint Morgan | Scientific evidence review within the recorded source-governance scope | Does not independently approve product claims, legal copy, or release |
| Product/safety claims approver | Unassigned | Approved claims registry, exclusions, page-level copy | Every public message and experiment |
| Source legal/rights approver | Johnathan Harper | Source-use and rights decisions within the recorded seven-source scope | Does not constitute Terms, Privacy, or campaign-copy approval |
| Terms/privacy legal approver | Unassigned | Dated Terms and Privacy approval, lawful basis, retention/deletion, subprocessors | Account creation, paid offer, analytics, forms, email, provider pilot |
| Security/data owner | Pradic Patel | Security and data-handling review within the recorded source-governance scope | Does not replace legal privacy approval or independently approve analytics |
| Platform/DBA owner | Unassigned | Green deploy, Postgres migration, backups/restore, probes, monitoring, rollback | Public launch and paid traffic |
| Billing owner | Unassigned | Stripe product/price, lifecycle, webhook, portal, refund/cancellation evidence | Operator/Commander promotion |
| Product engineering owner | Unassigned | Enforced capability-to-copy inventory and public content boundary | Feature and pricing claims |
| QA/accessibility owner | Unassigned | Browser matrix, WCAG target, live user journeys, crawl | Public campaign start |
| Data/analytics owner | Unassigned | Approved event schema, redaction, retention, access, deletion | Any measurement beyond server totals |
| Support owner | Unassigned | Contact route, hours, response expectations, escalation, sensitive-data handling | Public and paid launch |
| Provider operations owner | Unassigned | Queue staffing, qualification, notification, SLA, pilot contract | Any provider recruitment |
| Marketing owner | Unassigned | Calendar, creative review, channel execution, experiment ledger | Executes only after upstream gates |

No role may self-certify another owner's approval.

## 12. Privacy-safe measurement

No analytics collector is currently launch-approved. The following is a
proposed minimum schema for privacy/legal review, not authorization to
instrument it.

### Allowed event dimensions

- stable event name and schema version;
- coarse route group, not the full URL or query string;
- UTC day/hour bucket;
- approved campaign code from a strict allow-list;
- plan code or capability code;
- coarse device category;
- success/failure reason from a fixed non-sensitive enum;
- pseudonymous account or session key only if approved, rotated, access-limited,
  and deleted on schedule.

### Prohibited analytics payloads

- compound names, protocol items, doses, units, schedules, notes, goals, side
  effects, biometrics, photos, uploads, extracted text, search terms, or chat
  content;
- email, name, IP address retained as an analytics identifier, exact location,
  raw user agent, authentication token, receipt payload, or provider/client
  details;
- arbitrary URL/query/referrer values, free text, DOM capture, session replay,
  keystrokes, or error payloads containing user data;
- audiences or campaigns inferred from health, substance, or protocol data.

### Decision metrics

| Stage | Metric | Interpretation |
| --- | --- | --- |
| Discovery | Qualified owned visit to completed anonymous utility | Utility relevance, not health intent |
| Consideration | Utility/evidence visitor to start-page view | Message continuity |
| Activation | Account created to first approved organizational milestone | Product setup |
| Return | Day-7 and day-30 return cohorts | Whether records remain useful |
| Paid consideration | Enforced paid-capability view to pricing comprehension | Offer clarity |
| Revenue | Checkout start/completion, paid activation, cancellation, refund | Only after billing gates pass |
| Trust | Boundary-page views, help usage, claim complaints, unsafe-request refusals | Safety and clarity |
| Operations | Support volume, response time, unresolved privacy/deletion requests | Capacity and risk |
| Provider pilot | Qualified request count and owner response time | Only after provider operations approval |

Do not optimize for raw impressions, total traffic, email-list size, or generic
lead count without downstream qualified activation and retention.

## 13. Experiment backlog

Every experiment requires a named owner, approved copy, one primary metric, a
maximum exposure window, and a stop condition.

| Priority | Hypothesis | Surface | Primary metric | Stop condition | Gate |
| --- | --- | --- | --- | --- | --- |
| P0 | “Organize and inspect” is clearer than “optimize” language | Homepage hero | Start-page continuation | Boundary complaints or lower qualified activation | Claims + GO |
| P0 | Showing uncertainty before the CTA increases qualified trust | Evidence pages | Evidence-to-start continuation | Any implication of compatibility or safety | Public-content boundary |
| P0 | Formula transparency increases completed calculator use | Public calculator | Valid calculation completion | More error/help events or action-shaped interpretation | Safety + accessibility |
| P0 | A single Observer organizational milestone reduces onboarding abandonment | Start flow | First milestone completion | Increased consent refusal or support burden | Auth + consent + privacy |
| P1 | Evidence-source previews attract higher-quality organic visitors than compound promises | Search landing content | Qualified activation | Low source engagement or claim-review rejection | SEO + claims |
| P1 | Capability-based tier comparison improves pricing comprehension | Pricing | Correct tier-selection task | Checkout confusion or unsupported-feature clicks | Billing + product contract |
| P1 | “Unknown means not established” reduces compatibility misinterpretation | Relationship UI/help | Help comprehension | Users infer safety from missing findings | Boundary QA |
| P1 | Timeline examples framed as records improve return intent | Product education | Day-7 return | Causal/outcome interpretation | Claims |
| P2 | A short evidence-tier explainer improves source inspection | Evidence library | Source-reference open | Lower task completion or overconfidence | Claims |
| P2 | Consent-based setup reminders improve activation | Lifecycle email | Return to unfinished setup | Complaints, unsubscribes, privacy issues | Email + privacy |
| P2 | Provider qualification copy reduces unsuitable requests | Provider page | Qualified-request ratio | Fewer suitable requests or clinical expectations | Provider ops |
| P3 | A narrow high-intent search test can acquire retained users economically | Paid search | Day-30 activated cost | Written spend cap, claim issue, or weak retention | Separate paid-test approval |

Explicitly prohibited experiments include fear-based health copy, outcome
claims, medical authority cues, dose or stack recommendations, supplier content,
health-data targeting, hidden personalization, dark patterns, fake scarcity,
testimonial claims without substantiation, and any test that weakens a safety
or consent boundary.

## 14. Launch sequencing and stop rules

### Preparation may continue while HOLD

- maintain the claims/exclusions inventory;
- draft source-cited educational content;
- define a privacy-safe event dictionary;
- prepare QA checklists and experiment cards;
- assign accountable owners;
- reconcile all copy to the enforced product contract.

### Marketing execution remains blocked until

1. the exact release SHA passes required hosted build, test, security, deploy,
   smoke, accessibility, and crawl evidence;
2. legal, privacy, claims, security, platform, billing, support, data, and
   release owners record their approvals;
3. the public content boundary is proven Unknown-first and non-prescriptive;
4. live auth, consent, email, billing, database, backup, monitoring, rollback,
   support, and deletion paths are demonstrated;
5. analytics receives a separately approved minimal event contract.

Stop acquisition immediately for a claims breach, privacy incident, unsafe
interpretation, broken consent or deletion path, billing/entitlement failure,
unhealthy release, unstaffed support queue, or evidence that a public surface
is being used as medical guidance.

## 15. Success definition

KEO-86 documentation is complete when the plan is reviewable and evidence-led.
It does not complete launch readiness.

The eventual marketing program succeeds only if BioStack earns retained use
through inspectable organization and evidence context while preserving
uncertainty, privacy, and the non-prescriptive boundary.
