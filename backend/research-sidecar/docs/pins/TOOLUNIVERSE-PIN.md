# ToolUniverse Pin Receipt

| Field | Value |
|---|---|
| Status | **Pinned for foundation** |
| Date | 2026-08-02 |
| Package | `tooluniverse` |
| Exact version | **1.4.0** |
| Install style | **Base package only** — **not** `tooluniverse[all]` |
| Extra groups | None (no `ml`, `visualization`, `bioinformatics`, `embedding`, `graph`) |
| PyPI project | https://pypi.org/project/tooluniverse/1.4.0/ |
| Upstream repo | https://github.com/mims-harvard/ToolUniverse |
| License | See package `LICENSE` (inspect before redistribution) |

## Artifact digests (PyPI)

| Artifact | SHA256 | Size (bytes) |
|---|---|---|
| `tooluniverse-1.4.0-py3-none-any.whl` | `506c3b3112714df38fcfcb2a7abe29318f53a55749fde4f87a5f75b54ebe5148` | 7619940 |
| `tooluniverse-1.4.0.tar.gz` | `de4fb321d83912d6ba1f6916764445eb214e0128facab544e26db6b339c380b5` | 4992403 |

## Dependency declaration

```toml
[project.optional-dependencies]
tooluniverse = [
    "tooluniverse==1.4.0",
]
```

Install:

```bash
cd backend/research-sidecar
uv sync --extra tooluniverse --extra dev
```

Docker production image should use:

```dockerfile
uv sync --locked --no-dev --extra tooluniverse
```

Default `uv sync --no-dev` **does not** install ToolUniverse (keeps lean health-only images possible).

## Smoke verification (2026-08-02)

| Check | Result |
|---|---|
| `import tooluniverse` | Pass — `__version__ == 1.4.0` |
| `importlib.metadata.version("tooluniverse")` | `1.4.0` |
| `ToolUniverse().load_tools(include_tools=[...])` | Pass — loaded exactly 3 named tools |
| Named tools verified | `PubMed_search_articles`, `FAERS_count_reactions_by_drug_event`, `UniProt_get_function_by_accession` |
| Full registry browse | `list_built_in_tools` reports **2736** unique tools across many categories |
| `tooluniverse[all]` | **Not installed / not approved** |

## Explicit non-goals of this pin

- Does **not** approve unrestricted `tu.run` for arbitrary tool names
- Does **not** install GPU/ML extras (`[ml]`, `[embedding]`, etc.)
- Does **not** enable agent skills as autonomous authority
- Does **not** transmit BioStack user health data to upstream APIs
- Does **not** make ToolUniverse output canonical without BioStack review

## Allowlisted skills (kickoff Phase 6)

These are **workflow skill names** (agent skill docs), not free-form tool execution:

1. `tooluniverse-chemical-compound-retrieval`
2. `tooluniverse-literature-deep-research`
3. `tooluniverse-drug-research`
4. `tooluniverse-adverse-event-detection`
5. `tooluniverse-pharmacovigilance`
6. `tooluniverse-systems-biology`
7. `tooluniverse-target-research`

Executable tool names are constrained separately in:

```text
src/biostack_research_sidecar/data/tooluniverse_allowlist.v1.json
```

This is the single canonical allowlist: it is present in the editable layout and ships
in the wheel. Resolution never consults the current working directory. To point at a
different allowlist, set `BIOSTACK_RESEARCH_TOOLUNIVERSE_ALLOWLIST_PATH` explicitly —
there is no implicit discovery.

## Runtime enablement

| Env | Meaning |
|---|---|
| `BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=true` | Allow adapter use (still allowlist-bound) |
| `BIOSTACK_RESEARCH_TOOLUNIVERSE_VERSION=1.4.0` | Expected package version (mismatch → fail closed) |

Default remains **disabled** until the sidecar process is started with the extra installed **and** the env flag set.

## Upgrade policy

1. Choose a new exact version (never a floating range).
2. Record new wheel/sdist SHA256 digests.
3. Re-run allowlist smoke tests.
4. Re-validate each BioStack workflow mapping.
5. Bump allowlist file version if tool names change.
6. Update this pin document and ADR follow-up notes.

## Known operational notes

- Base install still pulls a large transitive set (http clients, PDF/markitdown stack, etc.). Acceptable for the research sidecar image; still far smaller than `[all]`.
- Some tools require API keys (`NCBI_API_KEY`, `FDA_API_KEY`, etc.) for rate limits; absence must degrade typed, not crash the sidecar.
- Category names are case-sensitive (`pubchem`, `ChEMBL`, `pubmed`, `opentarget`).
