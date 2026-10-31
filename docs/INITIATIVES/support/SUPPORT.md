# Support route: what exists, what was built, what Clint decides

Status: implemented on `feat/support-page`; **not merged** (needs Clint's OK). Written 2026-10-31 (America/New_York). No legal review was done or is implied (Clint's rule).

## 1. What existed

| Piece | State |
|---|---|
| `/contact` form (name, email, category, message up to 4000 chars) | Works. Rate-limited (`contact` policy). Posts to `POST /api/v1/contact`. |
| Delivery | `AzureCommunicationEmailContactMessageSender` sends through ACS (`AzureCommunicationEmail:*`) **to the hard-coded `support@biostack.cc`**, with the visitor as ReplyTo. |
| Footer "BioStack Support" | `mailto:support@biostack.cc`. |
| Passkey error text | Told people to "contact support@biostack.cc". |
| `/support` | Did not exist (404 on prod). |

**Finding:** `biostack.cc` has **no MX record** (checked 2026-10-31 with DNS lookups). Mail to `support@biostack.cc` cannot be delivered, so both the footer `mailto:` and the contact form's destination were dead ends. ACS reports "started", not "delivered", so the form tells the visitor "Message sent" while the mail bounces.

## 2. What this PR does

- `/support`: public help page. Seven groups of answers drawn from current product behavior (sources are named in `frontend/src/lib/supportContent.ts`): magic-link sign-in (one-time, 15 minutes), consent (the approved v2 text), data export (JSON or CSV zip), account deletion (typed phrase, emailed 15-minute link, 35-day backup window), what BioStack does and does not do (never tells you what to take or how much; amounts only from you; not medical advice), custom items and the Unverified state, the seven evidence tiers (rendered from `evidenceTiers.ts`), reporting a data error, billing, and a contact section. FAQPage structured data, in-page contents, sitemap and robots entries, and the public-route contract updated (`/support`, `/contact`).
- Links to it from the marketing footer (replacing the dead `mailto:`), the sidebar, `/contact`, the 404 page, and a new route error boundary (`app/error.tsx`), which also keeps a "Try again" and home path.
- The page does **not** print an email address. The contact form is the channel.
- Backend: the contact recipient is now an app setting, `Contact:SupportInboxAddress` (env `Contact__SupportInboxAddress`). Unset or malformed falls back to `support@biostack.cc`, so behavior does not change until Clint sets it. Added to `docker-compose.yml` and `.env.example`.
- Tests: page content invariants (topics present; no email address; no dose numbers or "we recommend"; export/deletion facts; seven tiers; FAQ schema; anchors), metadata and sitemap, skip-link target, and the recipient resolver.

## 3. Not built, on purpose: ticket storage

The brief said to build ticket storage only if no contact mechanism existed. One exists (the form), so none was added. Storing messages would also:
- add a per-person table, which must be exported and deleted with the account (`AccountDataCatalog` fails the build otherwise) while visitors who write in are not always account holders;
- store free text that may contain health information, which needs a retention rule.

It is the right fix for the silent-loss problem (a stored ticket survives a bounce). It needs Clint's decision on retention first.

## 4. Decisions needed from Clint

1. **A real inbox.** Create a mailbox that receives mail (and add MX for `biostack.cc`, or pick an address on a domain that has mail), or name another recipient. Then set `Contact__SupportInboxAddress` on the API.
2. **Ticket storage:** want it? If yes, how long to keep messages.
3. The contact success text says "reply to any BioStack email and it will reach the same inbox". Sign-in mail is sent from `noreply@`, so that line is not true today. Reword or make it true.
4. Done in this PR after #613 merged: the sidebar (expanded and collapsed) and `/contact` link to `/support`, and `/support`, the 404 page and the route error boundary are in the axe CI gate.

## 5. Production config (for Codex, after Clint names the inbox)

Dry run first. Stop before applying. Set one app setting, `Contact__SupportInboxAddress=<address Clint chose>`, on the API container app. Nothing else changes. Roll back by clearing the value (the code falls back to the old default). Do not touch `AzureCommunicationEmail__*`.
