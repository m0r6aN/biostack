# KEO-69 Auth, Onboarding, Consent, and Return-Path Runbook

## Deterministic boundary

This lane hardens repository-verifiable behavior. It does not prove production email delivery, deployed callback configuration, approved consent wording, or a live browser session.

Implemented behavior:

- production startup rejects missing or unsafe frontend, CORS, and email-provider configuration;
- each resend invalidates older unused magic links for the same identity;
- production token exchange is `POST /api/v1/auth/verify`; the development-only GET redirect remains available for local integration helpers;
- outside development, one-time tokens use a URL fragment so they are absent from the initial HTTP request and referrer; they are removed from browser history before exchange and never returned in the redirect payload;
- expired, consumed, or concurrently replayed tokens do not create another session;
- new or stale-consent sessions route through `/onboarding/consent` while preserving an approved relative return path;
- current-version acceptance and refusal are persisted, and refusal signs the browser out without clearing the anonymous device preview;
- returning users with current consent continue to the approved requested route;
- an expired cookie on a protected route returns through sign-in with the relative path preserved;
- rejected return paths emit the `AuthReturnPathRejected` event without logging the rejected value or token.

## Production configuration gate

Before deployment, verify all of the following without copying secret values into evidence:

- `FrontendUrl` is the exact public HTTPS origin with no path, query, fragment, or credentials.
- `Cors__AllowedOrigins__0` includes that exact frontend origin. Add other approved HTTPS origins as additional indexed entries only when required.
- Exactly one email provider is configured:
  - Azure Communication Email: connection string and verified sender address; or
  - SMTP: host, valid from address, TLS enabled, valid port, and password when a username is configured.
- The public frontend build proxies `/api/v1/*` to the intended API deployment.
- The session cookie is `HttpOnly`, `Secure` in production, `SameSite=Lax`, persistent for 30 days, and server-validated against the session record on every authenticated request.
- Migration `20260713230000_AddVersionedConsentDecline` is present. Its reviewed SQL adds only `ConsentDeclinedAtUtc` and `ConsentDeclinedVersion` to `AppUsers`.

## Human approval gate

The consent page copy is implementation-ready but not legally approved by this lane. A human owner must approve the exact text and the `bio-observational-v1` disclosure version before live acceptance. Any material text change must either remain compatible with that version or deliberately bump the server version and require re-acceptance.

## Authorized live evidence scenarios

Run these against the deployed production URLs using dedicated test identities. Record timestamps, redacted screenshots, response status, final route, and relevant non-secret log event IDs.

1. New user, accepted consent
   - Start with an anonymous analyzer or onboarding preview.
   - Request a link with a protected relative return path.
   - Confirm email receipt and sender identity.
   - Open the link once, approve consent, and confirm the preview and return path survive.
   - Create the first profile and confirm only the intended user can read it.
2. Refusal
   - Request a new link with a fresh identity.
   - Choose `Not now`.
   - Confirm a current-version refusal record, signed-out browser state, no server-side profile creation, and device-local preview retention.
3. Returning user
   - Sign in with an identity that has current consent and an existing profile.
   - Confirm direct return to the approved relative route without a consent loop.
4. Expiry and replay
   - Confirm an expired link fails neutrally.
   - Confirm the same consumed link fails on replay.
   - Request two links and confirm only the newest unused link succeeds.
   - Exercise concurrent exchange and confirm exactly one active session is created.
5. Session expiry and logout
   - Expire or revoke the server session while retaining the browser cookie.
   - Open a protected route and confirm sign-in receives the full relative path once, without a loop.
   - Confirm logout revokes the session and clears the cookie.
6. Negative routing and isolation
   - Submit absolute, scheme-relative, backslash, and unapproved return paths; confirm the canonical fallback and `AuthReturnPathRejected` telemetry.
   - Attempt cross-user resource access after sign-in and confirm denial.

## Evidence closeout

KEO-69 can move to Done only when the deterministic pull request is merged or otherwise accepted, the consent text and production configuration are approved, and the deployed positive and negative browser scenarios pass. A pull-request build is not deployment evidence.
