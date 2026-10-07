# Offline .NET Restore-Metadata Bootstrap Receipt

Date: 2026-09-02

Purpose: make fresh P01/P02 isolated worktrees dependency-ready without restore, registry, provider, or other network access.

## Source custody

- Source worktree: `D:\Repos\BioStack`
- Source branch: `main`
- Source commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Source tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`
- Tracked and staged diff: clean before copy
- All 15 backend `.csproj` files matched the destination copies by SHA-256.

## Permitted seed files

For each backend project, the coordinator copied only ignored files directly under its `obj/` directory whose names matched:

- `project.assets.json`
- `project.nuget.cache`
- `*.nuget.g.props`
- `*.nuget.g.targets`

No source, test, package, lock, binary, diagnostic, configuration, or generated compilation-output file was copied. No restore command or network operation was run.

## Destinations

| Parcel | Destination | Files | Sorted path/hash manifest SHA-256 | Git-visible status after copy |
|---|---|---:|---|---|
| P01 | `C:\Users\clint\.codex\worktrees\biostack-remediation-p01\BioStack` | 62 | `F9A7D134EFAE679313827E93240F34734AB24E56B07CE0C7213A386653A7DED8` | clean |
| P02 | `C:\Users\clint\.codex\worktrees\biostack-remediation-p02\BioStack` | 62 | `F9A7D134EFAE679313827E93240F34734AB24E56B07CE0C7213A386653A7DED8` | clean |

Every copied destination file was re-hashed against its source before this receipt was accepted. A baseline `--no-restore` test failure still stops the parcel; this receipt is environment provenance, not verification evidence.
