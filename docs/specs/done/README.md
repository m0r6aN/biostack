# Completed Parcel Specs

Beginning with P2, this directory contains parcel specs whose implementations are merged and whose closure evidence is complete under the [spec lifecycle](../README.md). The [authoritative registry](../INDEX.md) links each completed spec and closure record.

## Entry and closure

Only the coordinator moves a spec from `active/` to `done/`. Entry requires a merged implementation, green deterministic verification, all required independent reviews, resolved rework, an acceptance-to-evidence map, and a complete closure record naming the merge commit and durable evidence.

The move, closure link, and registry status change occur in the same governed change. Completed specs remain immutable historical contracts; later changes require a new parcel or an approved contract amendment rather than rewriting closed evidence. P1 uses its documented one-time closure-only registry update and is not moved into this directory.
