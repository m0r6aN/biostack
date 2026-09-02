# Security Gates

- Gate S0: no secrets or protected data in fixtures or logs.
- Gate S1: no external connection is permitted during tests.
- Gate S2: outbound tests use local fakes/interceptors and prove attempted payload shape without transmission.
- Gate S3: this phase cannot approve production routing, provider enablement, or release.

Status: S0-S3 are required for every parcel; independent production security review remains pending and outside this authorization.
