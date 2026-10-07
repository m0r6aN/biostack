# C1 containment review

Scope: local implementation of the owner's DrugBank containment decision. Reviewed by Codex using
the security-review workflow. No production probing, provider call, credential use or deployment was
performed. The table preserves findings from the initial worker-only patch. The current correction
and verification status is recorded below; it supersedes those initial implementation descriptions.

| ID | Severity / confidence | Locator | Finding and required correction |
|---|---|---|---|
| C1-01 | Medium / high | `RestrictedExcerptPolicy.WithholdRestrictedExcerpts`; `ResearchJob.RunAsync` | Returning an unchanged packet when the registry is absent lets an optional-registry research run write restricted excerpts. The acquisition gate is a separate path. Containment must survive a missing registry for the approved DrugBank scope, with an actual emitted-artifact test. |
| C1-02 | Medium / high | `frontend/src/app/api/research/suggest/route.ts:307` and `:413` | The route sends up to three extracted-evidence entries from a client-supplied packet. A previously generated or cached packet can bypass a write-time-only filter. Apply containment before the provider request and test the actual request body with a synthetic restricted excerpt. |
| C1-03 | Medium / high | `frontend/src/app/api/research/artifacts/route.ts` GET | The reader returns older JSON files unchanged. New worker output does not protect existing artifacts. Filter the approved restricted scope at read time while preserving files and provenance. This endpoint is dev-only; do not describe it as proven production customer exposure. |
| C1-04 | Low / high | `RestrictedExcerptPolicy.ReadRestrictedSourceIds` | Any nonempty permitted-content list currently releases every excerpt even if the total-restriction marker remains. Partial identity permission must not release unrelated narrative text. Keep the contradictory total restriction effective or require an explicit reviewed per-item release. |

The marker-only patch also affects six classes, including the two retired placeholders and the ISSN
paper. C1 authorizes DrugBank containment; C3 retires authorization classes and is not deletion of
their literature. Prefer an explicitly bounded C1 rule or clearly justify and verify any additional
behavior from an existing restriction rather than quietly expanding the change.

Coverage: the worker evidence-artifact writer, optional registry loading, draft compiler (which does
not copy quote fields), frontend artifact reader and AI request construction were inspected. The
backend consent gate remains a separate requirement and should be preserved. Unknowns include any
already-deployed artifact copies, caches, alternate consumers and production configuration. A passing
local test or regenerated artifact cannot establish that those copies are contained.

## Combined correction and verification

C1-01 and C1-04 are corrected by a manifest-based worker policy that remains effective without a
registry and cannot be released by a partial permitted-content list. The optional registry can only
add DrugBank aliases to containment. `ResearchJobContainmentTests` executes the real job and reads
the emitted file, including a run with no registry. Original inputs and existing review flags are
preserved. The writer comment now explicitly acknowledges the separate read and provider boundaries.

C1-02 is corrected before the AI context drops source URLs. A route test captures the actual mocked
provider request and verifies that a known restricted ID and an unfamiliar ID on a real DrugBank host
both have null quotes, with citations and permitted synthetic text preserved. The backend consent
check and its outbound-boundary regression cases remain intact. No real provider request was made.

C1-03 is corrected when the artifact route returns an evidence packet. A historical-packet route
test verifies containment without writing to disk; its production denial still executes before any
read. This protects the tested read path without purging preserved historical records.

The authoritative rule is `shared/source-rights/drugbank-containment.v1.json`. Generated copies live
inside each application's Docker build context; `scripts/sync-source-rights.mjs --check` rejects drift
in both relevant CI workflows. This fixes the initial external-file packaging error and avoids
expanding the Next.js module root. The policy is deliberately limited to C1 and grants no rights to
other sources.

Independent final frontend verification: **22 tests passed** across the helper, historical artifact route,
AI route and existing consent boundary regression suite. `tsc --project tsconfig.build.json --noEmit
--incremental false` passed using the production configuration. A separate comparison executed the
actual helper over all 78 input packets: **29 quote fields / 3,501 Unicode characters withheld**, with
source references, locators, review flags and original file bytes preserved. Reports are under
`research/output/owner-decisions-20260908/` (ignored local verification output).

Independent final worker suite: **902 passed, 8 failed, 0 skipped, 910 total; 2m 34s**. All 21 new
containment cases passed, including the three emitted-file integration tests. The eight failure names
match the previous 881/8 baseline exactly: four historical declaration expectations and four
authorization-binding/denial consequences. No existing test was changed or suppressed. TRX evidence:
`research/output/owner-decisions-20260908/worker-tests-owner-decisions-final.trx`.

The packaging check also accepted matching copies and rejected a deliberately altered frontend copy
in an isolated fixture. Original files were unchanged. The dedicated worker workflow now includes
the containment tests alongside its existing runtime/storage test filters.

Final read-only review by Claude accepted the three-boundary design and historical-file correction.
Codex added a shared 16-entry synthetic conformance fixture consumed by both suites. This exposed
semantic differences outside the present corpus: the worker did not trim source IDs or null empty/
non-string quotes like the frontend did. Those behaviors are now aligned. The reader applies the
policy to every returned artifact, with a no-op for artifacts lacking the relevant claim structure.
Both generated copies are included in the publication; a missing copy remains a failing CI check.

An initial unqualified `tsc` invocation also included a local `node_modules.stalled-20260711` tree and
unrelated test sources and failed. It is not a passing check and is not the project's production type
configuration. The successful production-config check above is the relevant bounded result.

Targeted ESLint could not start because the local install lacks `espree/dist/espree.cjs`. No dependency
tree was rewritten to conceal that environment issue. It is recorded as **not executed**, not passed.
Docker images and deployed journeys were not built or exercised in this local verification.

Remaining limits: source provenance in arbitrary client text is not authenticated by this filter.
Renaming/moving copied prose into unrelated fields is outside its retained-corpus scope. The current
registry-binding failure remains an independent acquisition/release blocker. No deployed copies,
alternate consumers or caches were verified, and **zero of the 30 queued claims is yet re-sourced**.
