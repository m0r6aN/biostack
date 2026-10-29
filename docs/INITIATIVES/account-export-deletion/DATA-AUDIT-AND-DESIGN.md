# Account export and deletion: data audit and design

Status: implemented on `feat/account-export-deletion`; **not merged** (needs Clint's OK). No legal review was done or is implied (Clint's rule); legal-type items are known risks only.
Base: main @ 0fc597b2. Procedure version: 1 (`AccountDataCatalog.ProcedureVersion`).

## 1. Single source of truth

`backend/src/BioStack.Api/AccountData/AccountDataCatalog.cs` lists **every** table in `BioStackDbContext`, in deletion order (children first). Each entry is one of:

| Disposition | Meaning |
|---|---|
| `UserScoped` | Rows belong to one person. Exported. Removed (or irreversibly anonymized) on deletion. |
| `RetainedPseudonymous` | Rows carry only a pseudonymous actor id. Exported. Cannot be edited. |
| `AnonymousByDesign` | Stored with no link to a person. |
| `GlobalReference` | Product or operator content. Not about a person. |

`AccountDataCatalogCompletenessTests` fails the build when:
- an entity type exists in the model but not in the catalog (**a new per-user table cannot ship without an export mapping and a deletion rule**);
- a reference table has a person-key column (`UserId`, `OwnerId`, `PersonId`, `ProfileId`, `IdentityId`, `ActorId`, `SubmitterKey`, `Email`, `StripeCustomerId`);
- a table with a foreign key to `AppUser`, `PersonProfile`, `Protocol` or `AuthIdentity` is not removed on deletion;
- the deletion order puts a principal before a dependent across a Restrict/NoAction foreign key.

The integration test also seeds a row in **every** exported table for two users and fails if any is empty, so a new table must be seeded too.

## 2. The audit

| Table | Holds | Linked by | Export | On deletion |
|---|---|---|---|---|
| AppUsers | Email, display name, role, consent record, Stripe customer id | itself | yes | deleted (last) |
| AuthIdentities | Sign-in email | `UserId` (cascade) | yes | deleted |
| AuthChallenges | Magic-link and deletion-link hashes, IP | identity (cascade) | yes, **without** the token hash | deleted |
| PasskeyCredentials | Passkey name and dates; key material | identity (cascade) | yes, **without** credential id, public key, user handle | deleted |
| PasskeyOperationChallenges | Short-lived passkey ceremonies, IP | `UserId` (cascade) | yes, **without** options and request hash | deleted |
| Sessions | Token hash, IP, user agent | `UserId` (cascade) | yes, **without** the token hash | revoked, then deleted |
| PersonProfiles | Name, sex, age or birth date, weight, goals, notes | `OwnerId` (**SetNull**, so deleted explicitly) | yes | deleted |
| ProfileGoals | Chosen goals | profile (cascade) | yes | deleted |
| CompoundRecords | Everything I'm Taking items, custom and unverified items as typed, user-entered dose fields | profile (cascade) | yes | deleted |
| DoseLogEntries | Dose logs the user entered | profile + item (cascade) | yes | deleted |
| CheckIns | Check-ins, side-effect, GI, mood text | profile (cascade) | yes | deleted |
| Protocols | Saved protocols, version chain | profile (cascade); parent/origin **Restrict** | yes | links cleared, then deleted |
| ProtocolItems | Snapshots of items in a protocol | protocol (cascade); item **Restrict** | yes | deleted before items |
| ProtocolRuns, ProtocolComputationRecords, ProtocolReviewCompletedEvents | Run history and saved computations | protocol/profile (cascade) | yes | deleted |
| ProtocolPhases, TimelineEvents | Phases and timeline | profile (cascade) | yes | deleted |
| EvidenceWatches, EvidenceAlertPreferences, EvidenceAlertLog | Watchlist, alert settings, alerts sent | `UserId`, **no FK** | yes (unsubscribe token omitted) | deleted explicitly |
| Subscriptions | Stripe-mirrored plan state | `AppUserId` (cascade) | yes | deleted; Stripe customer deleted first |
| ClaimSubmissions | Claims submitted; free text | **salted hash of the user id** (`SubmitterKey`) | yes (key omitted) | signed-in submissions deleted (key recomputed server-side). Signed-out submissions can't be linked and stay anonymous. |
| LeadCaptures | Email sign-ups | email only | yes | deleted by the account's address(es) |
| ProviderAccessRequests | Provider requests | email only | yes | **anonymized in place** with the existing `AnonymizeForRetention()` (redacted email, name, organization, role) |
| ProviderAccessAuditEntries | Operator status changes | `ActorId` = admin user id | yes | actor id overwritten with the empty id |
| SpineEntries | Governance receipts, append-only SHA-256 hash chain | `ActorId` = `user:<id>` | yes | **kept** (see risks) |
| ClaimCheckQueries | Hashed claim-checker queries | none | no | anonymous by design |
| AccountDeletionRecords | The minimal deletion record | none | no | anonymous by design |
| StripeWebhookEvents, SpineChainCheckpoints | Event ids, checkpoints | none | no | global |
| InteractionFlags, CompoundInteractionHints, KnowledgeEntries, KnowledgeSourceIntakeRequests, StagedTranscriptCandidateReviews, CompoundGraph* | Product and operator content | none | no | global |

Not in the database and not handled by this feature: browser storage on the user's own device (unsaved previews), application logs and telemetry (IP addresses; retention not audited), email provider delivery logs, Stripe's own records.

## 3. Export

- `GET /api/v1/account/export?format=json|csv`, signed-in only, **not** behind the consent gate (a person can always take their data out).
- Rate limit: 3 per 10 minutes per session (the limiter runs before authentication, so the session cookie is hashed as the partition key; signed-out callers fall back to the address). `Cache-Control: no-store`.
- JSON: `{ exportFormat, exportVersion, userId, counts, tables{...}, retainedButNotDeleted[], notIncluded[] }`. Every exported table is present, even when empty.
- CSV: a zip with one CSV per table plus a README. Text cells that start with `= + - @` get an apostrophe prefix so spreadsheets don't run them.
- Secrets are never exported: token hashes, passkey key material, unsubscribe tokens, the claim submitter key.
- `GET /api/v1/account/data-summary` feeds the UI with per-table counts and what survives deletion.

## 4. Deletion: how a person is re-authenticated, and why

**Choice: typed phrase + a fresh single-use emailed link that only works from an already signed-in browser.**
1. In the UI the person types `DELETE MY ACCOUNT`. The server checks the phrase.
2. The server emails a single-use link (15 minutes, SHA-256 hashed at rest, stored as an `AuthChallenge` of type `account_deletion`) to the account address from the existing ACS sender (`noreply@biostack.cc` via `AzureCommunicationEmail:SenderAddress`).
3. The person opens the link, which lands on `/account/data/delete/confirm`. The token is read from the address bar, removed from it, and sent only when the person presses the button (so a link scanner that opens the page can't delete anything). The API requires the session cookie **and** a token that belongs to that same user.

Why: a stolen session cookie or an unattended browser alone can't erase the account (no mailbox access), and a stolen mailbox alone can't either (no session). It reuses the proven magic-link pattern, adds no new secret store, and the link type can't be used to sign in (`/auth/verify` only accepts `magic_link`). A typed phrase alone would not prove the person at the keyboard is the owner. A fresh magic link alone would not stop an accidental click. Passkey-only users are covered too: every account has an email identity, and the link goes to it.

Limits: 3 link requests per hour per session (limiter) and 3 per hour per **account** (service, independent of session count); 5 confirms per 10 minutes. Operator (admin) accounts are blocked from self-serve deletion (`admin_account_protected`), since the sole owner account must not be erasable by a stray click.

## 5. Deletion: what runs

1. Atomically consume the link (must be unconsumed, unexpired, type `account_deletion`, and belong to this user).
2. **Stripe first**: if the user has a Stripe customer, delete it (this cancels subscriptions immediately). If Stripe is unreachable, **nothing is deleted**, the link is released for reuse and the person sees a retry message (`503 billing_unavailable`). Billing can never keep running for an erased account.
3. One database transaction: every `UserScoped` table in catalog order (sessions are revoked, then removed), then one `AccountDeletionRecords` row. Commit.
4. Clear the session cookie. Send the receipt email (best effort: a mail failure can't undo the deletion and is reported as `receiptEmailQueued: false`; the on-screen receipt always shows).

Hard delete everywhere except: provider requests (anonymized in place with the existing redaction), operator audit actor ids (overwritten), the append-only spine (kept), and anonymous rows (no link to remove).

**Minimal non-identifying record** (`AccountDeletionRecords`): random id, UTC day (no time of day), total rows affected, the day backups clear, procedure version. No user id, email, profile, session or IP. Structurally it has no column that could hold one (asserted in a test).

## 6. Backups, honestly

Postgres point-in-time-restore retention is 35 days. Deleted data can remain in a backup for up to 35 days and is not touched by the deletion job. The UI, the confirmation screen and the receipt email all say "backups age out within 35 days" and give the date (`day of deletion + 35`). Backups are used only to recover from a failure.

## 7. Known risks (listed only; no legal review)

1. **Spine entries are kept.** They are an append-only hash chain: editing or removing one breaks every later entry. They hold the pseudonymous actor id `user:<guid>`, receipt hashes and subject URIs, no name, email or health values. After deletion that id points to nothing, but it is not removed.
2. **Backups** hold deleted data for up to 35 days.
3. **Stripe** keeps its own invoice and tax records after the customer is deleted.
4. **Logs and telemetry** (IP addresses) and the email provider's delivery logs are outside this feature; retention is not audited.
5. **Signed-out claim submissions** carry an address-derived salted hash and can't be linked to a person, so they remain.
6. `/privacy` and `/terms` are still placeholders. Washington's My Health My Data Act is understood to expect a standalone consumer-health-data policy; this feature supplies the access/deletion mechanics only.
7. The submitter key of signed-in claim submissions is recomputed from the server's salt. If the salt were ever rotated, older submissions could no longer be matched to their author at deletion time (the salt is a Key Vault secret; do not rotate it without re-keying).
8. Admin accounts can't self-delete (by design).
9. Rate limits for export are per session, not per account (a person with several live sessions has several budgets). The global per-address limit still applies.

## 8. Maintenance rule for engineers

New table with per-user data: add it to `AccountDataCatalog` (with a query, any secret columns to omit, and a delete rule), give it a cascading foreign key to the account or profile where possible, and seed it in `AccountExportDeletionIntegrationTests.SeedAsync`. The completeness tests tell you which of these you missed. Bump `ProcedureVersion` when the list changes.
