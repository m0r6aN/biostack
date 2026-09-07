#!/usr/bin/env python3
"""Audit evidence-packet source authorization against the source registry.

Read-only. Changes no packet, registry, decision, or seed. Writes a JSON audit
artifact plus a human-readable summary to stdout.

This reproduces the pipeline's own field-authority semantics
(FieldAuthorityPolicy + EvidencePacketPreprocessor) and contrasts them with what
a registry-backed gate would decide, so the difference is measurable rather than
asserted.

Usage:
  python tools/research/audit-source-authorization.py
  python tools/research/audit-source-authorization.py --registry <path> --out <dir>
"""
import argparse
import collections
import glob
import io
import json
import os
import sys

# Mirrors backend/src/BioStack.KnowledgeWorker/Pipeline/FieldAuthorityPolicy.cs
SAFETY_CRITICAL_CLAIM_TYPES = {
    "regulatory", "approved-indication", "dose-context", "formulation",
    "storage-reconstitution", "contraindication", "warning", "monitoring", "interaction",
}
AUTHORITATIVE_TIERS = {"A1", "A2"}
TIER_ORDER = {"A1": 1, "A2": 2, "B1": 3, "B2": 4, "C1": 5, "C2": 6, "D": 7}

# Packet sourceId prefix -> registry class sourceId. Used ONLY to diagnose which
# class an unregistered source would plausibly belong to. It is deliberately not
# a registration mechanism: see the note on bucket B in the audit output.
CLASS_PREFIXES = {
    "dailymed": "dailymed", "fda": "fda", "nih-ods": "nih-ods", "nih-nccih": "nih-nccih",
    "issn": "issn-position-stands", "drugbank": "drugbank", "pubchem": "pubchem",
    "pubmed": "pubmed", "clinicaltrials": "clinicaltrials", "wada": "wada",
}


def requires_authoritative_support(claim_type, field_authority_required):
    return bool(field_authority_required) or (claim_type or "") in SAFETY_CRITICAL_CLAIM_TYPES


def load_registry(path):
    reg = json.load(io.open(path, encoding="utf-8"))
    classes, by_key = {}, {}
    for s in reg["sources"]:
        ident = s["identity"]
        classes[ident["sourceId"]] = s
        for key in [ident["sourceId"]] + list(ident.get("aliases") or []):
            by_key[key.lower()] = s
    return reg, classes, by_key


def infer_class(source_id):
    s = source_id.lower()
    best = None
    for prefix, cls in CLASS_PREFIXES.items():
        if s == prefix or s.startswith(prefix + "-"):
            if best is None or len(prefix) > len(best[0]):
                best = (prefix, cls)
    return best[1] if best else None


def audit(evidence_glob, registry_path):
    reg, classes, by_key = load_registry(registry_path)
    per_packet, gap_claims, placeholder_dates = [], [], []
    source_records = {}

    for path in sorted(glob.glob(evidence_glob)):
        compound = os.path.basename(path).replace(".evidence.json", "")
        packet = json.load(io.open(path, encoding="utf-8"))
        sources = {s["sourceId"]: s for s in packet.get("sources", [])}

        for sid, s in sources.items():
            published = s.get("publishedAt")
            if published and published[4:10] == "-01-01":
                placeholder_dates.append(
                    {"compound": compound, "sourceId": sid, "publishedAt": published})

        n_required = n_pass_today = n_pass_registry = 0
        for claim in packet.get("claims", []):
            claim_type = claim.get("claimType")
            far = claim.get("fieldAuthorityRequired")
            if not requires_authoritative_support(claim_type, far):
                continue
            n_required += 1
            refs = claim.get("sourceRefs", [])

            # Today: the gate reads the packet's own self-declared tier.
            today = [r for r in refs
                     if sources.get(r, {}).get("authorityTier") in AUTHORITATIVE_TIERS]
            if today:
                n_pass_today += 1

            # Counterfactual: registry-backed, rights-approved authority only.
            backed = []
            for r in refs:
                entry = by_key.get(r.lower())
                if not entry:
                    continue
                if (entry["evidencePolicy"]["authorityTier"] in AUTHORITATIVE_TIERS
                        and entry["rights"]["reviewStatus"] == "approved"):
                    backed.append(r)
            if backed:
                n_pass_registry += 1

            if today and not backed:
                gap_claims.append({
                    "compound": compound, "claimId": claim["claimId"],
                    "claimType": claim_type, "fieldAuthorityRequired": bool(far),
                    "selfAssertedSources": today,
                })
                for r in today:
                    rec = source_records.setdefault(r, {
                        "sourceId": r,
                        "packetTier": sources[r].get("authorityTier"),
                        "publisher": sources[r].get("publisher"),
                        "url": sources[r].get("url"),
                        "claimCount": 0, "compounds": set(),
                    })
                    rec["claimCount"] += 1
                    rec["compounds"].add(compound)

        per_packet.append({
            "compound": compound, "claimsRequiringAuthority": n_required,
            "passTodaySelfAsserted": n_pass_today, "passUnderRegistryBacked": n_pass_registry,
        })

    # Bucket each self-asserted source by what action it needs.
    buckets = collections.defaultdict(list)
    for rec in source_records.values():
        cls = infer_class(rec["sourceId"])
        rec["inferredClass"] = cls
        rec["classTier"] = classes[cls]["evidencePolicy"]["authorityTier"] if cls else None
        rec["classRights"] = classes[cls]["rights"]["reviewStatus"] if cls else None
        rec["compounds"] = sorted(rec["compounds"])
        if cls is None:
            b = "D-no-registry-class"
        elif TIER_ORDER.get(rec["packetTier"], 9) < TIER_ORDER.get(rec["classTier"], 9):
            b = "A-over-asserted"
        elif rec["classRights"] != "approved":
            b = "C-class-rights-pending"
        else:
            b = "B-registration-missing"
        rec["bucket"] = b
        buckets[b].append(rec)

    total_required = sum(p["claimsRequiringAuthority"] for p in per_packet)
    return {
        "recordType": "source-authorization-audit",
        "registry": registry_path,
        "registrySourceCount": len(reg["sources"]),
        "packetCount": len(per_packet),
        "totals": {
            "claimsRequiringAuthority": total_required,
            "passTodaySelfAsserted": sum(p["passTodaySelfAsserted"] for p in per_packet),
            "passUnderRegistryBacked": sum(p["passUnderRegistryBacked"] for p in per_packet),
            "gapClaims": len(gap_claims),
            "distinctSelfAssertedSources": len(source_records),
            "placeholderJan1Dates": len(placeholder_dates),
        },
        "bucketCounts": {
            k: {"sources": len(v), "claimReferences": sum(r["claimCount"] for r in v)}
            for k, v in sorted(buckets.items())
        },
        "buckets": {k: sorted(v, key=lambda r: -r["claimCount"]) for k, v in sorted(buckets.items())},
        "placeholderDates": placeholder_dates,
        "gapClaims": gap_claims,
        "perPacket": per_packet,
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--evidence", default="research/input/evidence/*.evidence.json")
    ap.add_argument("--registry", default="research/input/sources/pilot-source-registry.json")
    ap.add_argument("--out", default=None, help="directory to write the JSON artifact into")
    args = ap.parse_args()

    if not os.path.exists(args.registry):
        print("registry not found: %s" % args.registry, file=sys.stderr)
        return 2

    result = audit(args.evidence, args.registry)
    t = result["totals"]
    print("packets: %d | registry sources: %d" % (result["packetCount"], result["registrySourceCount"]))
    print("claims requiring A1/A2 support: %d" % t["claimsRequiringAuthority"])
    print("  pass today (packet self-asserted tier): %d" % t["passTodaySelfAsserted"])
    print("  pass under a registry-backed gate:      %d" % t["passUnderRegistryBacked"])
    print("  gap (pass only by self-assertion):      %d" % t["gapClaims"])
    print("placeholder Jan-1 publication dates: %d" % t["placeholderJan1Dates"])
    print()
    labels = {
        "A-over-asserted": "A. OVER-ASSERTED (packet tier stronger than its own registry class)",
        "B-registration-missing": "B. REGISTRATION MISSING (class approved; needs per-item review)",
        "C-class-rights-pending": "C. CLASS RIGHTS PENDING (blocked on legal/rights review)",
        "D-no-registry-class": "D. NO REGISTRY CLASS AT ALL",
    }
    for key in ["A-over-asserted", "C-class-rights-pending", "D-no-registry-class",
                "B-registration-missing"]:
        c = result["bucketCounts"].get(key)
        if not c:
            continue
        print("%s\n   %d sources, %d claim references"
              % (labels[key], c["sources"], c["claimReferences"]))

    if args.out:
        os.makedirs(args.out, exist_ok=True)
        dest = os.path.join(args.out, "source-authorization-audit.json")
        with io.open(dest, "w", encoding="utf-8", newline="\n") as f:
            json.dump(result, f, indent=2, ensure_ascii=False)
            f.write("\n")
        print("\nwrote %s" % dest)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
