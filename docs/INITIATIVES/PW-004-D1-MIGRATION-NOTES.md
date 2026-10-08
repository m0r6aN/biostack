# BIO-PAIRWISE-004 Migration Rollback Documentation (PW-004-D1)

**Charter Compliance:** Migration `charter` class controls per PW-004-A1  
**Reviewer Requirement:** `bio_pairwise_004_retro_reviewer_2` Conditional PASS  
**PR:** #506 (merged at 14f0b59)  
**Migration ID:** `20260830000000_AddCompoundInteractionHintProvenance`

---

## Forward Migration

**Command:** `dotnet ef database update 20260830000000`

**Changes:**
- Adds `IsSourced` (bool, NOT NULL, default false) to `CompoundInteractionHints`
- Adds `SourceReference` (TEXT, maxLength 2048, nullable) to `CompoundInteractionHints`

**Behavior:**
- Existing 14 catalog rows inherit defaults: `IsSourced = false`, `SourceReference = NULL`
- Non-destructive. All existing rows remain valid without backfill.
- Matches 2026-09-17 owner ratification: unsourced hints carry no citation and must be quarantined from public rendering and evidence labeling while continuing to serve the internal score.

**Data Impact:** NONE. No existing data modified.

---

## Backward Migration (Down)

**Command:** `dotnet ef database update <prior-migration>` (e.g., `20260828090000`)

**Changes:**
- Drops `SourceReference` column (destructive if sourced hints exist)
- Drops `IsSourced` column

**Behavior:**
- Reverses the forward migration schema changes.
- If any `CompoundInteractionHint` rows contain `SourceReference` text, that data is **permanently lost** on downgrade.

**Data Loss Analysis:**
- **Current Risk (as of Oct 8, 2026):** NONE. All 14 catalog hints are unsourced with NULL `SourceReference`.
- **Future Risk:** If sourced hints with citation text are added, downgrade will permanently lose metadata.
- **Mitigation:** Before downgrading in production, verify no sourced hints exist or export SourceReference data if preservation is required.

---

## Rollback via Git Revert

**Procedure:**
```bash
git revert 14f0b59  # Reverts code to pre-provenance state
```

**What Happens:**
1. **Code:** Reverts to state before hint provenance was added
   - `CompoundInteractionHint.cs`: Removes `IsSourced` and `SourceReference` properties
   - `CompoundInteractionHintCatalog.cs`: Reverts catalog entries
   - `BioStackDbContext.cs`: Removes provenance configuration
   - Migration file: Removed from project

2. **Database Schema:** Remains at migration `20260830000000` (migration already applied)
   - **Code/Schema Mismatch:** Code no longer knows about the columns, but columns remain in database
   - **Application Behavior:** Application will not read/write provenance fields, but columns persist

3. **Operator Action Required:** To complete rollback and remove schema, manually run backward migration:
   ```bash
   dotnet ef database update 20260828090000
   ```

**Recommended Rollback Sequence:**
1. Run backward migration FIRST: `dotnet ef database update 20260828090000`
2. Then revert code: `git revert 14f0b59`
3. Redeploy application

**Reason:** Running code revert first leaves orphaned columns in schema; running migration first ensures clean removal.

---

## Migration Pattern Compliance

This migration follows the established BioStack pattern:

✅ Hand-written (not scaffolded)  
✅ Custom timestamp (20260830000000)  
✅ `type: "TEXT"` for cross-provider string parity (SQLite/PostgreSQL)  
✅ Central snapshot (`ProductionMigrationBaselineConfiguration`) intentionally untouched  
✅ No `.Designer.cs` scaffolding included  

Precedents:
- `IsSourced`/`IsBackupEligible` columns: `20260828090000_AddPasskeyAuthentication`
- `SourceReference`/`type: "TEXT"`: `20260626000000_AddReceiptClassToSpine.ReceiptClass`

---

## Integration Verification

Tested in `InteractionHintQuarantineIntegrationTests`:
- Migration applies cleanly on fresh SQLite DB
- Application boots without error
- 6 test scenarios pass (provenance-present + provenance-absent quarantine fixtures)

---

## Summary

| Aspect | Status | Notes |
|--------|--------|-------|
| **Forward Behavior** | Non-destructive | Adds columns with safe defaults; 14 existing rows unaffected |
| **Backward Behavior** | Destructive (future risk only) | Drops columns; current NULL SourceReference = no data loss now |
| **Rollback Procedure** | git revert + manual migration | Code revert alone leaves schema orphaned; migration required |
| **Data Loss** | None currently | Future sourced hints with citations would be lost on downgrade |
| **Charter Compliance** | PW-004-A1 satisfied | Forward/backward/rollback documented; revert behavior verified |

---

**Document Metadata:**  
- Fix Parcel: PW-004-D1
- Reviewer: `bio_pairwise_004_retro_reviewer_2`
- Coordinator Oversight: Yes
- Date: 2026-10-08
