# BIO-FE-002 Gate 2 Dispatch Record (onboarding route consolidation)

Coordinator-owned record, frozen at dispatch (2026-10-08). Spec review: APPROVE-WITH-FIXES, all
six checklist items pass, no blockers.

**Ruling F-308 (coordinator, 2026-10-08):** D-F(1)'s "301 redirects" is satisfied by HTTP **308
Permanent Redirect** (Next.js `permanent: true` semantics). Both are permanent and cache-
equivalent; 308 is method-preserving and the framework's canonical permanent redirect. The spec's
interpretation is CONFIRMED. (Owner may override; override implies a manual status-code
configuration in the build.)

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-FE-002",
  "specPath": "docs/specs/active/BIO-FE-002-onboarding-route-consolidation.md",
  "builderId": "bio_fe_002_builder",
  "branch": "feat/bio-fe-002-route-consolidation",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/bio-fe-002",
  "permissionEnvelope": "frontend-routing-per-spec",
  "builderSurfaces": "exactly the spec's Allowed Files section",
  "reviewerIds": ["bio_fe_002_review_1"],
  "risk": "standard"
}
```

Dispatch notes: `/start` canonical; `/map` + `/onboarding` permanent redirects (308 per F-308);
every legacy entry state reproducible via the mode toggle (the spec's preservation mapping);
SEO canonical tags + sitemap/metadata handled; Required Tests (redirect codes, mode preservation
per legacy entry, zero-404) run and recorded. Delivery: commit → push → PR vs `main` (Gate 3
owner's) → worktree removed.
