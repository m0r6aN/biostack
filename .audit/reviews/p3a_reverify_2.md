# p3a_reverify_2 — P3-A remediation re-verification

**Verdict:** PASS-WITH-FIXES
**Subject:** fix/p3a-remediation-1, PR #527, tested hash `69acff8e4051deab1b4dbc598e1c24a0ba552083`
**Dispatch:** `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P3A-REMEDIATION-1.md`
**Date:** 2026-10-09 (session clock)

## Method

Read-only. Worktree `/home/cmorgan76/Repos/biostack-wt/p3a-remediation-1` was never modified
(confirmed clean `git status` before and after). All adversarial testing was done in a disposable
clone at `/tmp/p3a_reverify2_clone` (`git clone` of `/home/cmorgan76/Repos/biostack`, fetched the
worktree's branch from the worktree path as a remote, checked out `69acff8` on a scratch local
branch). No other review file in `.audit/reviews/` was read.

Because `verify-p3a.ps1` check 3 hardcodes a hand-authored `AllowedSurfaces` list of exactly 28
paths and diffs `BaseCommit...HEAD` with **no path scoping**, and because this repository's
`main` genuinely carries interleaved coordinator-process commits (`TRIAGE-2026-10-08.md`,
`GATE2-P3A-REMEDIATION-1.md`, `parcels/P0-A.md`, `parcels/P3-A.md`,
`COORDINATOR-DISPATCH-QUEUE.md`) between the original P3-A build anchor (`756c9d1`) and this
remediation, the committed script **cannot** produce a literal clean run at any real BaseCommit
(reproduced below). The PR body (#527) fully and accurately discloses this, reproduces the exact
failure, and uses an admitted, explicitly-labeled, uncommitted `/tmp` diagnostic copy (extending
only the check-3 expected-set and nothing else) to demonstrate the remaining checks. I independently
reproduced that same diagnostic methodology (my own `/tmp/verify-p3a-diagnostic.ps1`, which only
widens check 3's `$ExpectedChanges`, not `$AllowedSurfaces` used by check 12's scan loop — the PR's
own description of its diagnostic edit is slightly imprecise on this point, see finding F3) to
independently confirm every other check, and additionally isolated check 7's and check 4's logic
by extracting the **unmodified, committed** functions verbatim and running them standalone, so my
blocker finding below does not depend on my diagnostic harness at all.

## Diff scope confirmation

```
$ git diff --name-only 13c5c3f...HEAD     # 13c5c3f = true parent of this remediation's one commit, already on main
docs/specs/README.md
docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md
docs/specs/scripts/verify-p3a.ps1
```
Matches the dispatch's `builderSurfaces` envelope exactly (three files, one of the two permitted
fixture-paragraph locations chosen, README restricted to the four link strings). Confirmed clean.

## Clean-run confirmation

Using the same disclosed diagnostic methodology as the PR body (BaseCommit `756c9d1...`,
BuilderId `p3a_builder`, ReviewerIds `p3a_impl_review_1,p3a_impl_review_2`):
```
P3-A verification PASS
```
And confirmed the **unmodified, committed** script genuinely fails check 3 at that same BaseCommit
with the exact five extraneous files the PR discloses (`COORDINATOR-DISPATCH-QUEUE.md`,
`TRIAGE-2026-10-08.md`, `GATE2-P3A-REMEDIATION-1.md`, `parcels/P0-A.md`, `parcels/P3-A.md`) —
the PR's "Expected 28, got 33" transcript reproduces byte-for-byte. This is a genuine, accurately
and fully disclosed pre-existing limitation, not a new defect introduced by this remediation
(`AllowedSurfaces` is untouched by `69acff8`), and not one of the dispatch's six required fixes.
I do **not** treat it as a BLOCKER against this PR, but see F3 below for a residual process risk.

## Adversarial findings (new, beyond the two previously-demonstrated exploits)

### F1 — BLOCKER (re-opens the spirit of R2-F2): decoy-heading substring match bypasses check 7's per-subsection scoping

`Get-ExtensionPointSection` (added by this remediation to fix R2-F2) resolves a named extension
point's subsection by:
```powershell
return ($ExtPointsSections | Where-Object { $_.Text.Contains($NameNeedle) } | Select-Object -First 1)
```
`$_.Text` is the **entire heading line's text** (any `##`/`###` heading in the file), and
`.Contains` is a **substring** test, not an exact-heading-identity test. `Select-Object -First 1`
takes the first matching heading **in document order**. This means any earlier `##`/`###` heading
whose prose merely *contains* the extension-point name as a substring — not the pinned, backticked
heading itself — silently becomes the section that check 7 inspects, and the real, pinned
`## \`delivery-class-extension\`` subsection is **never checked at all**.

**Reproduction** (isolated from my diagnostic harness, against the unmodified committed
`verify-p3a.ps1`'s own `Get-HeadingSections`/`Get-ExtensionPointSection` logic extracted verbatim):
I inserted a decoy heading `## Preface note on delivery-class-extension rollout readiness` before
the real `## \`delivery-class-extension\`` heading in `EXTENSION-POINTS.md`, moved both pinned
sentences (the non-binding sentence and the append-only invariant sentence) into the decoy's body,
and **removed them from the real subsection**. Isolated check:
```
Matched heading text: Preface note on delivery-class-extension rollout readiness
Contains NonBinding: True
Contains AppendOnly: True
```
Running this mutation through the full diagnostic pass produced `P3-A verification PASS` — check 7
is satisfied even though the actual `## \`delivery-class-extension\`` subsection now contains
neither pinned sentence. This is functionally identical in effect to the originally-demonstrated
R2-F2 exploit (pinned sentence present somewhere in the file but not in its own named subsection)
but defeats the specific remediation that was supposed to close it, via a different mechanism
(substring-matched decoy heading vs. the already-blocked "move sentence to a different *real*
named subsection").

**Smallest amendment:** `Get-ExtensionPointSection` must match heading **identity**, not
substring containment — e.g. require the heading text to equal (after trimming/backtick-stripping)
exactly `` `delivery-class-extension` `` / `` `domain-overlay-insertion` `` / `` `template-set-extension` ``,
or reuse the same literal `$RequiredExtHeadings` strings (minus the `## ` prefix) for exact
comparison instead of `.Contains`.

### F2 — MAJOR: alias-matching resolver has no quality/distinctiveness guard, enabling silent false-positive section satisfaction via a future additive alias

R2-F3 wired `SECTION-HEADING-MAP.md`'s "Canonical alias(es)" column into `Resolve-RequiredSections`
so a term can be satisfied by any heading matching the term **or any listed alias**. Check 6
validates only that the alias cell is non-empty and the Source cell is the pinned literal — it
performs **no check on alias content quality** (minimum token count, distinctiveness from common
words, etc.). `Resolve-RequiredSections`'s own heading-length bound (`hToks.Count -gt ($phraseLen + 4)`)
means a **one-token** alias can match any heading of up to 5 normalized tokens containing that
token anywhere in sequence — i.e. an almost-unconstrained match.

**Reproduction** (isolated extraction of the unmodified, committed `Resolve-RequiredSections`,
`ConvertTo-NormalizedTokens`, `Get-HeadingSections`, `Sort-Ordinal`, run standalone): simulating a
plausible, innocuous-looking future `delivery-class-extension` amendment that adds the generic
one-word alias `overview` to the existing term `objective`, and a document that has **no dedicated
"Objective" section** at all, only an unrelated generic `## Project Overview` heading used for
something else entirely plus the other five legitimately-named required sections:
```
Unsatisfied terms: 
EXPLOIT: 'objective' incorrectly marked SATISFIED by unrelated 'Project Overview' heading via generic 1-token alias.
```
The document contract's own prose (`SECTION-HEADING-MAP.md`, "Matching rule") does not forbid this
either — it only describes the matching mechanic, not alias quality. This is not exploitable by
P3-A's own current content (every alias today equals its own term, so nothing currently in this
PR's diff or the existing SECTION-HEADING-MAP.md rows can trigger it), but it is a real, latent
weakness in the exact mechanism R2-F3 introduced, reachable by any future, individually-reviewed
`delivery-class-extension` amendment with no additional verifier guard standing in its way.

**Smallest amendment:** add a check-6 (or new) assertion that every alias's normalized token count
is `>= 2` (or otherwise bars single common-word aliases), or require an alias to share at least one
token with its own term, before `Resolve-RequiredSections` is permitted to accept it as a candidate
phrase.

### F3 — MINOR: PR body's diagnostic-edit description is imprecise and could mislead a less-careful re-verifier

The PR body states the diagnostic copy differs from the committed file only by extending
"`AllowedSurfaces`/the check-12 placeholder-scan-exclusion list." In practice, correctly isolating
the five known pre-existing coordinator files requires extending **only** check 3's
`$ExpectedChanges` set, **not** `$AllowedSurfaces` itself — `$AllowedSurfaces` is iterated directly
by check 12's placeholder scan (`foreach ($surface in $AllowedSurfaces)`), so naively extending
`$AllowedSurfaces` (as the PR's wording suggests) causes check 12 to scan the five extra files too,
and I reproduced a check-12 false failure (`Unresolved placeholder found in
docs/INITIATIVES/COORDINATOR-DISPATCH-QUEUE.md`) when I first tried that literal approach, before
correcting my patch to touch only `$ExpectedChanges`. I cannot rule out that the PR author's actual
`/tmp/verify-p3a-diagnostic.ps1` was in fact correctly scoped (their transcript shows a clean PASS
including check 12, which is only consistent with a correctly-scoped edit) — this is a wording
imprecision in the PR's self-description, not a demonstrated defect in their actual diagnostic run
or in the delivered three files.

**Smallest amendment:** PR body wording — "extended `$ExpectedChanges` (check 3's expected-file-set
constant only, not `$AllowedSurfaces` itself, which check 12 also consumes)."

## Does either BLOCKER/MAJOR finding change a locked decision?

No. F1 and F2 are both pre-existing latent defects in `verify-p3a.ps1`'s own logic, reachable
independent of this remediation's three-file diff; neither is caused by, nor invalidates, any of
the six specific fixes (R2-F1, R2-F2-as-literally-scoped-against-the-two-previously-demonstrated-
patterns, R2-F3-as-literally-wired, R2-F4, R1-F1, R1-F2) this PR closes. F1 specifically means the
R2-F2 closure claim ("pinned-sentence assertions scoped per subsection... not whole-file
`.Contains`") is **narrower than its own commit message implies**: it closes the one demonstrated
pattern but not the class of per-subsection-scoping bypasses in general.

## Verification notes (checked and found clean)

- `git diff --stat 69acff8e4051deab1b4dbc598e1c24a0ba552083^..69acff8e4051deab1b4dbc598e1c24a0ba552083`
  touches exactly `docs/specs/README.md`, `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md`,
  `docs/specs/scripts/verify-p3a.ps1` — matches the dispatch's three permitted surfaces exactly.
- Check 4 (frozen-surface census computed from `git ls-tree` at `BaseCommit`) genuinely detects a
  deletion between `BaseCommit` and `HEAD` of a previously-frozen `docs/specs/done/*.md` file
  (isolated reproduction: `DETECTED CHANGE: docs/specs/done/BIO-ANALYZER-001-upload-table-label-leak.md`)
  — R2-F4 is genuinely closed.
- Check 11's href-exactness fix (R2-F1) correctly **rejects** a zero-width-space homoglyph inserted
  into an otherwise-correct href (`'...P3-A\u200B.md'` vs pinned `'...P3-A.md'`), confirming the
  ordinal-exact comparison fails safe and is not satisfiable by visually-identical-but-byte-different
  hrefs.
- Both of the two previously-demonstrated exploits (bogus INDEX.md href via shape-only regex;
  pinned sentence relocated to a different, real, already-named subsection) correctly **fail**
  against the committed, remediated script, as the PR claims.
- The pre-existing check-3/coordinator-interleaving limitation (see "Clean-run confirmation" above)
  is accurately and fully disclosed in the PR body with a byte-for-byte matching reproduction; I
  treat it as a known, honestly-reported limitation, not a new finding against this remediation.
- Placeholder-normalization pipeline (9-step order: comments → tags → code-delims → emphasis →
  escape-backslashes → entity-decode → Cf/Cc-strip → NFKC → confusables-skeleton): attempted several
  order-dependent evasions (entity-encoded comment/tag syntax revealed only after decode, mid-word
  zero-width/soft-hyphen disguise, split-by-real-HTML-tag disguise, escaped-asterisk literal-text
  disguise). All either still resolve to a literal, contiguous, word-bounded `TBD`/`TODO`/`FIXME`
  substring after normalization (correctly caught) or render invisibly in both the raw markdown and
  the normalized text alike (not a meaningful evasion, since nothing visible is being hidden from a
  reader either). No working evasion found in the time available; this is not an exhaustive proof
  of soundness, only a negative result against the patterns attempted.
