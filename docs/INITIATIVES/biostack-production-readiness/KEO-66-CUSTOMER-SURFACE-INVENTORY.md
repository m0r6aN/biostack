# KEO-66 Customer Surface Inventory

Date: 2026-07-13

Scope: public entry points, pricing claims, authenticated paid surfaces, and customer-visible no-op or placeholder behavior.

Release posture: **HOLD** until the external gates at the end of this document are closed.

## Disposition summary

| Surface / claim | Enforced capability evidence | KEO-66 disposition |
| --- | --- | --- |
| Protocol Analyzer | `ProtocolAnalyzerService` enforces `paid_intelligence` / Operator; `AnalyzerGateIntegrationTests` cover anonymous `401` and Observer `402` | Public copy now says Operator/Commander. The client resolves the same subscription feature before accepting input. Entitled results no longer show an unlock CTA. |
| Observer plan | Active-compound limit and basic portal sections are server enforced | Analyzer-score, finding-preview, and overlap-teaser claims removed. Highlights are limited to shipped Observer surfaces. |
| Operator plan | Analyzer, current-stack intelligence, weekly calendar, diet/lifestyle, and progress/milestones are server gated | Highlights narrowed to those shipped consumers. Unconsumed feature-code claims removed. |
| Commander plan | Protocol review, pattern, drift, sequence, monitoring, and mission-control services are server gated | Highlights narrowed to those shipped consumers. Priority-support and offline-only/runtime-forbidden claims removed. |
| Provider entry | Provider access request is durably queued; general multi-client access is not available | Homepage now advertises a provider pilot request, consistent with `/providers`. |
| Care-team action | Backend writes a trimmed `NoteAdded` timeline event; no recipient, channel, notification, or delivery record exists | All shipped entry/success copy now says save/note. The compatibility route remains, but the contract and client method explicitly describe protocol-record storage only. |
| Decision Theater | `/api/v1/stack-review/envelope` governs an explicit caller-supplied payload; it does not resolve a persisted protocol | Unsupported `ProtocolId` contract removed. The protocol page is labeled a client-supplied preview, canonical completion is unavailable, and exported context is marked `protocolBound: false`. |
| Shared application header | No runtime source justified the hard-coded `LOCAL` status | The status chip was removed. |
| Static protocol HTML template | Reference-only HTML with no server persistence | Its care-team interaction is labeled a preview and no longer claims message delivery or response timing. |
| Public calculators | Anonymous endpoints and frontend routes are implemented | Retained. |

## Regression evidence required for closeout

- Care-team note component copy and trimmed `NoteAdded` persistence.
- StackReview payload-only request contract and absence of protocol-completion controls.
- Anonymous/Observer analyzer pre-input gating and Operator/Commander result behavior.
- Homepage analyzer/provider copy consistency.
- Pricing-highlight allow-list limited to shipped server-enforced consumers.
- Shared header does not claim server-backed pages are local-only.
- Production frontend build and focused backend integration tests.

## External release gates

These are not safely resolvable by code inference and remain release blockers:

1. **Legal approval:** `/terms` and `/privacy` identify themselves as non-final placeholders. A human-approved effective version is required before checkout is enabled.
2. **Hosted billing proof:** verify configured Operator and Commander Stripe price IDs, checkout return, webhook fulfillment, subscription state, and customer portal against the deployed revision.
3. **Hosted OCR proof:** scan a real supported image through the deployed analyzer. If OCR is not configured, remove the scan claim before release.
4. **Purchase-intent continuity:** signed-out plan CTAs currently land at authentication without a selected-plan checkout contract. Preserve the chosen plan only after billing defines and consumes that contract; do not imply automatic checkout meanwhile.
5. **Security release gates:** KEO-65 remains authoritative for historical callback-secret rotation/invalidation, deployed proxy/rate-limit identity, and remaining go-live controls.

## Closeout rule

KEO-66 may close when the code regressions are green and this inventory is attached to the issue. Production readiness remains **HOLD** until every external release gate above has dated, revision-bound evidence.
