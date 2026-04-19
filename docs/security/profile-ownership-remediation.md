# Profile Ownership Remediation

Profile ownership is now enforced from the application service layer. Normal authenticated profile-scoped queries only return `PersonProfile` rows whose `OwnerId` matches the current authenticated `AppUser`.

Existing profiles with `OwnerId = null` cannot be assigned safely because the historical data does not contain deterministic ownership evidence. The remediation approach is therefore isolation: orphaned profiles remain in the database for manual review, but they are excluded from all normal user reads and cannot be used as parents for new child records.

Manual remediation can assign `OwnerId` only when ownership is proven out of band. Until then, orphaned profiles are treated as quarantined legacy data rather than user-owned data.
