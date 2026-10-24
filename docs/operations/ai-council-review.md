# AI council review

Owner ruling 5 (2026-10-24). Clinton Morgan is the sole owner, operator and decision maker. The council replaces every reviewer-role approval step. Its rules:

- Two independent AI reviewers check each record.
- Consensus passes.
- Disagreement escalates to Clint with distinct options.
- Legal items are logged as known risks only. They never block a record or change an outcome.

## How a record is decided

| Reviewers say | Council writes | Refresh shows |
|---|---|---|
| Both **approve** at the record's current overall tier | `approve-for-promotion` (blockers cleared) | `ai-council-reviewed` |
| Both **approve** or **request-changes** at the same tier, but not the current one, or both request changes | `request-changes` (with the agreed regrade or concerns) | `changes-requested` |
| They differ on decision or tier | `escalate-to-owner` plus an escalation file with options A, B and C | `escalated to owner` |
| Both **reject** | `escalate-to-owner`. Rejecting withholds a record, and only Clint can withhold (ruling 9: show everything) | `escalated to owner` |
| A reviewer errors, times out or returns invalid JSON | nothing. The record stays `pending-review` and the run exits non-zero | `pending-review` |

Options for Clint, de-duplicated:

- **Option A:** reviewer 0's position.
- **Option B:** reviewer 1's position.
- **Option C:** keep the record published as pending review and send it back to research.

Each option carries a ready-to-use `decisionTemplate`. To decide, copy the chosen template into a `review-decision-batch-<date>-owner.json` file in the `review-decisions` folder. Set `reviewedAt` to the current time. The next Refresh applies it, because the latest decision wins.

**Which records are due.** A record is due when no decision on file is at least as new as its `provenance.lastReviewedAt`. Escalated records wait for Clint and are not re-sent. Each run reviews up to `Council:MaxItemsPerRun` records (default 20).

**Outputs.** All are written to the share in production.

| Folder | Contents |
|---|---|
| `review-decisions/review-decision-batch-<ts>-ai-council.json` | Schema-valid decision batch. Every decision has a `council` block with both verdicts, the outcome, the owner options and the logged legal risks. |
| `owner-escalations/council-escalation-<ts>.json` and `.md` | Decisions waiting for Clint, with options. |
| `known-risks/council-known-risks-<ts>.json` | Legal items, logged only. |

## Configuration (provider-agnostic, two models)

| Key (env var form) | Value |
|---|---|
| `Council__Reviewers__0__Provider` / `__1__Provider` | `openai-compatible` or `anthropic` |
| `Council__Reviewers__N__Model` | Model name, for example the two models Clint picks |
| `Council__Reviewers__N__Endpoint` | Optional base URL. Default `https://api.openai.com/v1` or `https://api.anthropic.com`. Any OpenAI-compatible endpoint works (Azure OpenAI, xAI, OpenRouter, self-hosted). |
| `Council__Reviewers__N__AuthHeader` | `bearer` (default) or `api-key` for Azure OpenAI |
| `Council__Reviewers__N__ApiKey` | **Secret.** Comes from the environment or Key Vault only. |
| `Council__MaxItemsPerRun` | Default 20 |

API keys are never in source, appsettings or Bicep parameters. In Azure, `knowledge-schedules.bicep` takes only the Key Vault secret **URIs**: `councilReviewer0ApiKeySecretUri` and `councilReviewer1ApiKeySecretUri`. Locally, export the env vars in your own shell. A test fails the build if a key-shaped value appears in appsettings or infra. A missing setting fails the run before any work and names the env var. It never shows the value.

## Run

- **Locally:** run with `Worker__RunMode=CouncilReview` and the env vars above. Add `Worker__DryRun=true` to review without writing.
- **In production:** the `<base>-kw-council` job runs daily at 08:00 UTC (04:00 EDT / 03:00 EST), before the Refresh dry-run and apply.
