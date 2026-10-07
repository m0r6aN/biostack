# Ops: Spine migrations, checkpoints, and research sidecar

## Spine hash-chain migration (F3)

Hand-written migrations (do **not** run `dotnet ef migrations add`):

| Migration | Purpose |
|---|---|
| `20260803000000_AddSpineHashChain` | SequenceNumber, PreviousEntryHash, EntryHash + indexes |
| `20260803120000_AddSpineChainCheckpoints` | Signed chain-head checkpoint table (F3+) |

### Production / Postgres

Migrations apply on API startup when `Environment=Production` (`Program.cs` → `db.Database.Migrate()`).

Deploy the new binaries, then restart the API. Confirm:

```bash
# after deploy — admin only
curl -H "Authorization: Bearer $TOKEN" \
  https://<host>/api/v1/receipts/chain/verify
```

### Development / SQLite

Dev uses `EnsureCreated()` (model → schema), not the migration history. Unit tests do the same. That means:

- Green unit tests **do not** prove a migration file was applied to a real DB.
- For a local Postgres that already has data, run the app once in Production mode against that database, or apply pending migrations with your usual deploy path.

### Legacy rows

Pre-chain Spine rows get deterministic placeholder hashes in the migration. `VerifyChainAsync` will report the first such row as a hash mismatch — **intended**. The migration point is the chain’s effective genesis.

---

## Spine checkpoints (F3+)

| Setting | Env | Default | Notes |
|---|---|---|---|
| `SpineCheckpoint:SigningKey` | `SpineCheckpoint__SigningKey` | empty | HMAC secret; keep **out of** the DB file |
| `SpineCheckpoint:SigningKeyIsServerHeld` | `SpineCheckpoint__SigningKeyIsServerHeld` | `false` | `true` → source=`server-hmac` |
| `SpineCheckpoint:AutoCheckpointEveryNEntries` | | `25` | `0` disables auto-on-append |
| `SpineCheckpoint:CadenceMinutes` | | `60` | `0` disables background cadence |

Admin endpoints (role admin):

| Method | Path |
|---|---|
| GET | `/api/v1/receipts/chain/verify` |
| POST | `/api/v1/receipts/chain/checkpoints?note=optional` |
| GET | `/api/v1/receipts/chain/checkpoints` |
| GET | `/api/v1/receipts/chain/checkpoints/verify` |
| GET | `/api/v1/receipts/chain/checkpoints/latest/export` |

Store exported JSON off-box for dispute readiness.

---

## Research sidecar path (post-rename)

Location: **`backend/research-sidecar`** (was `backend/src/BioStack.Research`).

After pull on any machine:

```bash
# Windows PowerShell
Remove-Item -Recurse -Force backend/research-sidecar/.venv -ErrorAction SilentlyContinue
cd backend/research-sidecar
uv sync --all-extras
uv run pytest
uv run python -m biostack_research_sidecar
```

```bash
# Unix
rm -rf backend/research-sidecar/.venv
cd backend/research-sidecar
uv sync --all-extras
uv run pytest
```

Docker:

```bash
cd backend/research-sidecar
docker build -t biostack-research-sidecar .
docker run --rm -p 8080:8080 biostack-research-sidecar
```

Provider identity strings (`biostack-research-sidecar`) in .NET are **not** filesystem paths — leave them alone.

---

## Governance wording (F5)

Class D primary control = reviewed templates + human review.  
Copy-guard regex tests = automated **backstop** only. See `RATIFICATION.md`.
