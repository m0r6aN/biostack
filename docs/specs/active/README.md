# Active Parcel Specs

Beginning with P2, this directory contains only approved active parcel specs recorded in the [authoritative registry](../INDEX.md) and governed by the [spec lifecycle](../README.md).

## Entry

A spec enters `active/` only after its Goal Charter and plan review are closed, required independent spec reviews pass, coordinator triage closes every finding, and Gate 2 names the exact spec hash, owner, branch, worktree, permissions, verification, evidence, and reviewers. The coordinator owns entry and registry updates.

## While active

The named builder works only on allowed surfaces in the coordinator-named worktree. An active spec contains no unresolved placeholders, ambient branch or worktree, or unresolved decision. Scope or contract drift, missing decisions, and failed or incomplete verification stop implementation and return control to the coordinator.

## Transition

An active spec does not move because implementation is merely complete. It moves to `done/` only after merge and complete closure evidence. The coordinator records the movement and updates the registry in the same governed change. P1 follows the one-time bootstrap exception documented in the lifecycle and is not stored here.
