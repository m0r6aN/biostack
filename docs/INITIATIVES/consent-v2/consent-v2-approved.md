# Consent v2 draft: `bio-observational-v2`

Status: DRAFT for Clint's sign-off. Nothing here is implemented. No legal review was done or is implied (Clint's rule); legal-type concerns are listed as known risks only.
Base: main @ 0fc597b2. Written 2026-10-28.

---

## 1. How consent works today (what I found)

| Question | Answer |
|---|---|
| Version string | `ConsentGate.CurrentConsentVersion = "bio-observational-v1"` in `backend/src/BioStack.Application/Services/ConsentGate.cs`. The server pins it; the client never supplies it. |
| Where the text lives | Inline JSX, three sentences, in `frontend/src/app/onboarding/consent/page.tsx` ("Before you save data" / "Choose whether to continue"). The `/privacy` and `/terms` pages are placeholders. |
| How it is stored | One latest-value record on `AppUser`: `ConsentVersion`, `ConsentAcceptedAtUtc`, `ConsentDeclinedAtUtc` (+ declined version). There is no history table. Accepting v2 overwrites the v1 acceptance. |
| How it is checked | `ConsentGate.GetStatusAsync` derives "accepted" as `ConsentVersion == CurrentConsentVersion`. Staleness is computed, so a bump needs NO data migration. |
| What blocks | `RequireConsentFilter` returns 403 `consent_required` on 18 write endpoints (profiles, compounds, goals, check-ins, protocols, phases, portal). Reads stay open. The frontend `api.ts` turns that 403 into a redirect to `/onboarding/consent?returnTo=...`. |
| Sign-in | `AuthSessionIssuer` sends users without current consent to `/onboarding/consent?returnTo=...` right after magic-link verify. |
| Decline | Records the decline and signs the user out. Their saved data stays on the server. |
| Existing users on a bump | Everyone becomes "stale" instantly. Existing 30-day sessions keep working for reads, and hit the consent redirect on their first write. |

## 2. Proposed v2 text (plain language)

Shown as a short screen plus an expandable "Details" for each section. Reading level: roughly grade 8. No affiliate wording, no legal terms of art, no promises about future features beyond "we ask again".

### Screen title
**Before you save data**  (returning users see: **We updated how BioStack works with your data**)

### Short version (always visible)
> BioStack keeps the profile, items, amounts, and logs you choose to save, so you can track what you take. That is health-related information, and you decide what goes in.
> BioStack never tells you what to take or how much. Amounts come only from you. It is not medical advice.
> We don't sell your data and we don't use it for ads. You can export everything or delete your account any time from the Account page.
> Some things below are not switched on yet. Before any of them start for you, we ask again with an on/off switch.

Buttons: **I agree and continue** / **Not now**

### Details

**1. What you save**
- Your profile details, goals, and check-ins.
- Everything I'm Taking: the supplements, vitamins, research compounds, or anything else you add. This includes custom items you type yourself that BioStack can't verify. We store them as you wrote them.
- Amounts, units, schedules, and the dose logs you enter ("I took this, at this time"). These are your own numbers. BioStack doesn't prescribe, suggest, or work out a dose for you, and nothing it shows should be read as one.
- Notes you write and the protocols you build.

**2. Who can see it**
- You. Nobody else can see your saved items or logs unless you choose to share them.
- Provider summary: when you copy or print a summary for your clinician, that happens on your device, when you ask. It only leaves BioStack when you send it.
- Services that run BioStack for you (hosting, email delivery, payments) handle only what they need to do their job. Payments see your email and billing details, not your items or logs.
- We don't sell your data. We don't share it for advertising.

**3. Coming later (not on yet; we ask again first)**
- *Your own catalog and notes.* Looking up the items you add against BioStack's library, and keeping personal notes on them.
- *Public-source lookups.* When you turn this on, the name you typed for an item may be sent to public databases (NIH/NLM DSLD, PubMed, ClinicalTrials.gov, openFDA, PubChem, RxNorm, and optionally Open Food Facts) to find information about it. Your name, email, and amounts are not sent. You can mark any item "keep private" and its name is never sent.
- *Anonymous research requests.* If an item has no information yet, you can ask us to look into it. The request is just the item name, with nothing linking it to you. We can't tell who asked.
- *Reminders.* Optional reminders by web push and by email. By default the text on your lock screen and in the email subject is generic ("BioStack reminder") with no item names. You can change this.

**4. Your controls**
- **Export:** download everything you saved (JSON or CSV) from the Account page.
- **Delete:** delete your account and your saved data from the Account page. Deleted data is removed from live systems right away. Backups age out within 35 days. We keep a small record that a deletion happened that contains nothing that identifies you. Payment records may be kept by our payment provider where tax rules require it.
- **Say no:** choose "Not now". You can still read the public library. Your saved data is not deleted unless you delete it.
- **Change your mind:** you can turn off any later feature, and delete your account, any time.

**5. What BioStack is not**
BioStack is an information tool. It isn't a doctor, pharmacy, or emergency service. If something feels wrong, contact a clinician or emergency services.

Footer: `Consent record: bio-observational-v2`

### "What changed" (returning users only)
> Since you last agreed, we added: logging doses you took, the Everything I'm Taking list (including custom items), export and delete tools, and a plain statement of what is coming later. Nothing new is switched on for you by agreeing.

## 3. Re-consent UX plan

1. **Bump:** change `CurrentConsentVersion` to `bio-observational-v2`. No schema or data migration. Update the consent page text, `ConsentPage.test.tsx`, `ConsentGateIntegrationTests.cs`, the launch-readiness ledger and the KEO-69 runbook.
2. **When existing users see it:** at their next sign-in (magic-link verify already redirects stale users) or, for a live session, at their first write (403 `consent_required` already redirects). Returning users see the "We updated..." title and the "What changed" box. The page knows they are returning when status shows an older accepted version.
3. **Blocked until they agree:** every write endpoint behind `RequireConsentFilter` (profiles, items, goals, check-ins, protocols, portal, dose logs). Also future write endpoints: new user-data endpoints MUST carry the filter.
4. **Not blocked:** reads of their own saved data, the public library, **export, and account deletion** (rights must never sit behind a consent wall; Part B builds them without the filter), sign-out, and billing/portal.
5. **Decline:** same as today. Data stays; they are signed out; they can return and agree later, or delete.
6. **Version history (small optional add, my suggestion):** an additive `ConsentEvents` table (user id cascade, version, accepted/declined, timestamp) so v2 does not erase proof of v1. Cascades on account deletion. Say the word and I'll include it; it is not required for the bump.
7. **Tests:** stale v1 user gets 403 on a write and 200 on a read; accept v2 unblocks; export and delete work while stale.

### Future-phase consent: now, or per-feature later?
**Recommendation: one v2 consent that DISCLOSES everything above, plus an explicit off-by-default opt-in at first use of each external-facing feature.**
- v2 text (above) explains the future items in advance, so there is no surprise.
- Phase 3 (per-user catalog and in-app notes) touches nothing external and ships under v2 alone.
- Public-source lookups (typed names leave BioStack) and reminders (push permission, email) each get their own on/off switch the first time the user meets the feature. The switch is the consent for that feature, stored as a per-user setting.
- No v3 bump is needed each time a phase ships, and nothing is "pre-consented" to something the user never saw.
- Consent now to everything in one click was rejected: it asks for agreement to things that don't exist yet, and external sharing is the most sensitive part.
- Per-entry "keep private" stays as designed in spec v5 and is independent of the account-level switch.

## 4. Known risks (listed only; no legal review, per Clint's rule)

1. `/privacy` and `/terms` are still placeholders. The consent screen is the only disclosure users see today.
2. Washington's My Health My Data Act (and similar state laws) is understood to expect a standalone consumer-health-data privacy policy, consent to collect, separate consent to share, and access/deletion rights. Supplement and dose logs are plausibly "consumer health data". This draft and Part B cover the product side only.
3. The v1 acceptance is overwritten on re-consent unless the optional history table is added.
4. Backups: Postgres PITR retains 35 days, so deleted data survives in backups up to 35 days. The deletion screen says so.
5. Typed item names sent to third-party public sources (phase 5/7): a user could type something personal as a name. The "keep private" switch and the opt-in are the mitigations.
6. Reminder content by push and email can be seen by others on a shared device or inbox. Generic text by default is the mitigation.
7. The governance Spine is an append-only hash chain that contains the pseudonymous actor id `user:<guid>`. It cannot be edited without breaking the chain. After deletion that id maps to nothing, but it is not removed.
8. No age gate exists. Minors' data is not addressed anywhere in the text.
9. Stripe keeps its own billing records after a customer is deleted; the text says so.
10. Application logs and cloud telemetry may hold IP addresses for a limited time. Retention settings have not been audited.

## 5. Questions for Clint (few)

1. **Future-phase consent:** approve the recommended model (one v2 disclosing everything, plus off-by-default per-feature opt-ins at first use)?
2. **Version history:** add the small `ConsentEvents` table in the bump PR so v1 acceptance is not erased?
3. **Wording:** any sentence in section 2 you want changed? Biggest judgment calls: "We don't sell your data" and "Not now still lets you read the public library".

I will not touch consent code until you sign off on the text.
