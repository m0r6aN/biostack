# BioStack passkey authentication

Passkeys are the preferred returning-user sign-in method when the deployment gate is enabled. Email magic links remain the only registration and recovery bootstrap. The existing 15-minute, one-time email challenge, 30-day server-validated session, consent gate, role claims, and redirect allowlist remain authoritative.

## Security design

- BioStack uses `Fido2` / `Fido2.AspNet` 4.0.1 for WebAuthn option generation and attestation/assertion verification. Application code does not parse authenticator data or implement signature verification.
- Enrollment requires an active server-validated session backed by a verified `email` `AuthIdentity`.
- Enrollment requests resident/discoverable credentials and requires user verification. Attestation conveyance is `none` to avoid collecting unnecessary authenticator-identifying data.
- Returning sign-in is discoverable (no credential allow-list) and requires user verification. The credential ID resolves the existing first-party `AppUser` through a verified `passkey` `AuthIdentity`.
- The server validates the configured exact origin set and RP ID through the WebAuthn library. Production configuration validation also requires HTTPS and requires `FrontendUrl` to be an exact configured origin.
- Registration and authentication ceremonies expire after five minutes. The opaque request ID is stored only as SHA-256, is claimed atomically before verification, and cannot be reused. Failed verification burns the ceremony.
- Stored credential state includes credential ID, COSE public key, opaque user handle, credential type, signature counter, transports, AAGUID, backup eligibility/state, and created/last-used timestamps. Private keys never reach BioStack.
- Successful assertions update counter/backup metadata and issue the same 30-day server-held `Session` and cookie claims as email verification. Consent redirects and redirect normalization use the same code path.
- Credential deletion is owner-scoped, requires an authenticated session, and fails closed unless a verified email recovery identity remains.

## Production gate

Passkeys are disabled in base configuration. Set all of the following only after the public hostname and HTTPS origin are final:

```text
Auth__Passkeys__Enabled=true
Auth__Passkeys__RpId=biostack.cc
Auth__Passkeys__ServerName=BioStack
Auth__Passkeys__Origins__0=https://biostack.cc
```

`RpId` is a hostname without scheme, port, path, or wildcard. Every origin must be an exact origin whose host is the RP ID or a subdomain. In production every origin must use HTTPS and `FrontendUrl` must exactly match an entry. Changing an RP ID after enrollment makes existing credentials unusable; plan that value as durable account infrastructure.

Development enables passkeys for `http://localhost:3043` with RP ID `localhost`. Browser WebAuthn still requires a secure context; browsers conventionally treat localhost as secure.

## Schema and integration boundary

Migration `20260828090000_AddPasskeyAuthentication` creates only `PasskeyCredentials` and `PasskeyOperationChallenges` plus their indexes and foreign keys. It does not alter, cast, or repair any existing PostgreSQL column, including the known TEXT drift.

Likely merge conflicts with separate session/schema work are limited to:

- `backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs`: session issuance was extracted to `AuthSessionIssuer` so email and passkey flows share claims, duration, consent handling, and server session persistence.
- `backend/src/BioStack.Api/Program.cs`: WebAuthn configuration/service registration and endpoint mapping.
- `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`, `AppUser`, and `AuthIdentity`: two new entity sets and navigations.
- migration ordering/history: retain the passkey migration as an isolated create-only migration and rebase its timestamp if another schema migration takes the same slot. Do not fold unrelated type repairs into it.
