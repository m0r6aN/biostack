# Session handoff — resume 4

Initiative: `biostack-go-live`
Date: 2026-10-14
Agent: coordinator resume session
Decision: **NO-GO / HOLD**
This file is the current operational record. It does not authorize production release and does not change `docs/INITIATIVES/biostack-production-readiness/`.

The production probe in `SESSION-2026-10-14-resume-3.md` still stands. This session did not deploy, Refresh, spend, or call production again.

## Starting state

- Coordinator branch: `docs/go-live-library-record` at `5b686e69536f2eedf0195ee77466f20b08a490d8`, plus uncommitted coordinator records on that worktree.
- `origin/main`: `9550964256d507d03b789df8f0eb4c448a6f7ac8` (merge of PR #545).
- `origin/dev`: `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee`. It does not contain PR #544 or PR #545. Do not sync it.
- Production: last successful Azure deploy is run [38136962345](https://github.com/m0r6aN/biostack/actions/runs/38136962345) at `6c9d8ecc` on 2026-10-11.
- `rtk` is not on PATH. Git, `dotnet`, and `npm` ran without it.

## Local commits

Each commit is based on `5b686e69`. None is pushed. None is merged. Merging any of them to `main` can start `.github/workflows/deploy.yml`. BIO-GOLIVE-005 is the change that lets that workflow pass the high-advisory restore. A push of `main` after that pin would attempt Azure. That is a production change, and this session did not do it.

| Parcel | Commit | Branch | Worktree | Impl review |
|---|---|---|---|---|
| BIO-GOLIVE-001 | `6eb3710148d94e90f68cc2bbc5ddd433204dd746` | `feat/bio-golive-001-classification-list` | `.worktrees/bio-golive-001-classification-list` | [PASS](reviews/BIO-GOLIVE-001-impl-review-1.md). 16 knowledge tests, 18 application tests. |
| BIO-GOLIVE-004 | `bd4a0ac960a820a7c4970e636b9b8dc05f9491b0` | `fix/bio-golive-004-identity-and-receipts` | `.worktrees/bio-golive-004-identity-and-receipts` | [PASS](reviews/BIO-GOLIVE-004-impl-review-1.md). 14 API tests, 2 receipt tests. |
| BIO-GOLIVE-005 | `d285e4fac26cccad8f1ff74e9b954ecf337d1869` | `fix/bio-golive-005-nuget-advisory-pins` | `.worktrees/bio-golive-005-nuget-advisory-pins` | [PASS](reviews/BIO-GOLIVE-005-impl-review-1.md). Audited restore exited 0. Pkcs and Xml resolve only at 10.0.13. |
| BIO-GOLIVE-006 | `012c33c161c526d5bb2ce2fe6a89040cab45e424` | `test/bio-golive-006-commander-checkout` | `.worktrees/bio-golive-006-commander-checkout` | [PASS](reviews/BIO-GOLIVE-006-impl-review-1.md). 8 billing-intent tests. Node on the reviewer host was v24.7.0; `engines` asks for >=26.8.2. |

Reviewed spec hashes, before the status flip to `active`:

- 001: `EEBB2145824FFE07B1FD3E7E79FBD8104AA0A61A41ED09E81B1F0F25C89C3666` (reviews 5 and 6). Reviews 1–4 failed and do not count.
- 004: `6CE25898ADEDF27B05AE7F7288303EA417CE3AA2E6F10C43136F0E77FA2E4CE8`
- 005: `354DF73DAA171672E37B69D6D0605A3611A60466DDCD38DB115FAA6078D9F7D7`
- 006: `FEEDC2FEF6BB2E6813696E98A8AD44C68780D3BB1DA0DD30964CF6A373D0F67A`

The specs stay `active`. Governed closure requires a merge plus a closure record. BIO-GOLIVE-001 also stays open until a coordinator HTTP transcript names the deployed revision and shows Glycine and Retatrutide still `Unknown`. That transcript cannot be written until a human deploy.

## What those commits change

- 001 appends `AminoAcid = 11` and `ResearchCompound = 12` and maps the schema strings `Amino Acid` and `Research Compound`. `Small Molecule` stays `Unknown`. The public list uses `ListPublicCompoundsAsync`. `GetAllCompoundsAsync` stays a full-entity read. The committed Retatrutide seed class remains Peptide and is still unreviewed. This commit does not edit the seed and does not rewrite stored rows.
- 004 trusts a peer only when it is loopback or listed in `RateLimit:TrustedProxies`. A trusted peer uses the rightmost forwarded hop that is not itself a trusted proxy. Named contact and magic-link logs no longer receive an exception, email, domain, or link. An unexpected webhook exception keeps the receipt as `failed` with `FailureCode` `unexpected_processing_error` and rethrows. Quarantine reclaim remains. The in-process per-email magic-link budget is still a residual, not a waiver. Deploying this before `RateLimit:TrustedProxies` is set would put Azure private-ingress peers in one socket bucket.
- 005 adds `backend/Directory.Build.props` pinning Pkcs and Xml at 10.0.13. The two direct Xml 10.0.10 attributes were left in place because restore selected 10.0.13. NU1504 and NU1510 are warnings, not the deploy gate.
- 006 adds one signed-in test that a Commander click calls `createCheckoutSession('commander')`. The Operator assertion remains.

## Still open

- F-001 production OpenAPI and Scalar returned HTTP 200 on 2026-10-14. Source on `main` guards them when `ASPNETCORE_ENVIRONMENT=Production`. That source is not the live revision.
- F-005 terms and privacy still say legal review is required. Counsel has not approved them.
- F-007 ACR admin stays enabled for the source-acquisition transition. Do not edit `infra/azure` and do not change the live registry.
- Premium journeys, paid lifecycle (refund, cancel, renewal, payment-failure downgrade), monitoring, support, restore drill, alert delivery, rollback rehearsal, and any spend stay open.
- BIO-GOLIVE-002 and BIO-GOLIVE-003 remain `review-candidate`. Their comparison base is stale: PR #544 already landed overlapping source on `main` without Gate 2, and 004 now owns `ClientIpResolver`. Do not Gate 2 them until a new spec is reviewed against current `main`.

## Next safe action

Do not merge these branches. A later human deploy decision has to set `RateLimit:TrustedProxies` before BIO-GOLIVE-004 is deployed, and has to accept that BIO-GOLIVE-005 removes the advisory block that currently stops `main` from reaching Azure. After that deploy, the coordinator writes the Glycine and Retatrutide transcript. Live class repair is a separate parcel and still must name Retatrutide's class before any repair writes one.

## Do not touch

- Production Refresh, unless the seed changes under an approved parcel and a new dry run is read first.
- `ConnectionStrings__DefaultConnection` and Container App secrets.
- Live Stripe charges, refunds, and cancellations.
- Terms, privacy, disclaimer, and subscription-policy approval.
- The production-readiness initiative's NO-GO.
- Dose, schedule, and pairing `JsonIgnore` attributes.
- The Retatrutide seed class.
- Live ACR admin.
- `origin/dev`.
