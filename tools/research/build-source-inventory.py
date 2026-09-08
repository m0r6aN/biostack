#!/usr/bin/env python3
"""Build a reproducible per-item source inventory for rights review.

Read-only. Changes no packet, registry, decision, or seed. Writes a JSON
inventory artifact plus a human-readable summary to stdout.

Why this exists
---------------
Three separate attempts to count DrugBank exposure produced three different
answers (30 claims, 24 claims, 29 quotes) because each counted a different
thing. A claim is not a document, an alias is not a document, and a quote is
not a claim. Rights review needs the count of *works*, remediation needs the
count of *stored excerpts*, and neither is the number of claims. This script
emits all of them from one pass so the numbers stop disagreeing.

What this script does NOT do
----------------------------
It records rights metadata; it does not evaluate it. `rightsBasis` is reported
verbatim from the registry and is never inferred from a host, a URL prefix, an
authority tier, or a publisher string. A lane whose registry entry says
"approved" is reported as approved *and* flagged `approval-unbacked` when no
legalRights decision batch covers it -- that is a mechanical cross-check
against the decision artifact, not a legal conclusion.

Usage:
  python tools/research/build-source-inventory.py
  python tools/research/build-source-inventory.py --out <dir> --format md
"""
import argparse
import collections
import datetime
import glob
import hashlib
import json
import os
import sys
from urllib.parse import urlparse

DEFAULT_REGISTRY = "research/input/sources/pilot-source-registry.json"
DEFAULT_EVIDENCE_GLOB = "research/input/evidence/*.evidence.json"
DEFAULT_DECISIONS = "research/source-authorization/recommended-seven-source-decisions.v1.json"
DEFAULT_OUT = "research/output/source-inventory"

# Fields a rights review needs per admitted item. Anything absent from the
# registry is reported as a gap rather than silently defaulted, because a
# missing attribution requirement reads identically to "no attribution needed"
# once it is omitted.
REQUIRED_RIGHTS_FIELDS = [
    ("licenseVersion", "licence identifier and version"),
    ("attributionRequirements", "attribution requirements"),
    ("thirdPartyExclusions", "third-party content exclusions"),
]

# Hosts that republish or index works authored by someone else. A URL here
# records the acquisition route and nothing about ownership: go.drugbank.com/
# articles/A31488 is DrugBank's index page for a Journals of Gerontology paper
# (DOI 10.1093/gerona/gls078), whose own notice names the 2012 author as
# copyright holder with OUP publishing on behalf of the Gerontological Society
# of America -- three different parties, none of them the lane. A host check
# cannot catch this, because the host really is the lane's host. Flagging a
# record here means "read this work's own notice", not "the publisher is X".
AGGREGATOR_HOSTS = {
    "go.drugbank.com",
    "drugbank.com",
    "api.semanticscholar.org",
    "semanticscholar.org",
    "ebi.ac.uk",
    "europepmc.org",
}


def split_url(url):
    """Return (host, path+query) with only cosmetic differences collapsed.

    Only the host is case-folded. Everything right of it is left byte-exact,
    because the path and query are where record identity lives and case is
    frequently part of it: UNII codes (MU72812GK0), Bookshelf ids (NBK573221),
    PMCIDs, and DOIs embedded in paths (DOI:10.1056/NEJMoa1411481). Two earlier
    versions of this function were wrong in this area -- the first dropped the
    query entirely and collapsed 26 DailyMed labels (?setid=) and 4 Drugs@FDA
    applications (?ApplNo=) into one work each; the second still lowercased the
    path. Merging two works is invisible in the output, whereas splitting one is
    visible and fixable, so this errs toward splitting and reports case-only
    variants as a defect instead of silently folding them together.
    """
    if not url:
        return None, None
    p = urlparse(url.strip())
    host = (p.netloc or "").lower()
    if host.startswith("www."):
        host = host[4:]
    path = (p.path or "").rstrip("/")
    if p.query:
        path += "?" + p.query
    return (host or None), path


def normalize_url(url):
    """Collapse cosmetic URL differences so one work groups as one work."""
    host, path = split_url(url)
    return host + path if host else None


def load_registry(path):
    with open(path, "r", encoding="utf-8") as fh:
        reg = json.load(fh)
    alias_to_class, classes = {}, {}
    for entry in reg.get("sources", []):
        identity = entry.get("identity", {})
        sid = identity.get("sourceId")
        if not sid:
            continue
        classes[sid] = entry
        alias_to_class[sid] = sid
        for alias in identity.get("aliases", []):
            alias_to_class[alias] = sid
    return reg, alias_to_class, classes


def load_authorized_lanes(path):
    """Lanes covered by an issued decision batch. Absent file -> nothing covered."""
    if not os.path.exists(path):
        return set(), None
    with open(path, "r", encoding="utf-8") as fh:
        batch = json.load(fh)
    lanes = {s.get("sourceId") for s in batch.get("sources", []) if s.get("sourceId")}
    return lanes, batch.get("registryBinding", {}).get("sha256")


def collect_corpus(evidence_glob):
    """One pass over the packets. Returns per-sourceId records and usage."""
    records, usage = {}, collections.defaultdict(
        lambda: {"claims": set(), "quotes": 0, "quoteChars": 0, "compounds": set()}
    )
    packet_count = 0

    for path in sorted(glob.glob(evidence_glob)):
        with open(path, "r", encoding="utf-8") as fh:
            packet = json.load(fh)
        packet_count += 1
        compound = packet.get("compound", {})
        name = compound.get("name") if isinstance(compound, dict) else None
        name = name or os.path.basename(path).replace(".evidence.json", "")

        for src in packet.get("sources", []) or []:
            sid = src.get("sourceId")
            if sid and sid not in records:
                records[sid] = {
                    "sourceId": sid,
                    "url": src.get("url"),
                    "doi": src.get("doi"),
                    "pmid": src.get("pmid"),
                    "title": src.get("title"),
                    "publisher": src.get("publisher"),
                    "declaredAuthorityTier": src.get("authorityTier"),
                    "sourceType": src.get("sourceType"),
                    "publishedAt": src.get("publishedAt"),
                    "accessedAt": src.get("accessedAt"),
                    "firstSeenIn": os.path.basename(path),
                }

        for claim in packet.get("claims", []) or []:
            cid = claim.get("claimId")
            cited = set(claim.get("sourceRefs") or [])
            for ev in claim.get("extractedEvidence") or []:
                ref = ev.get("sourceRef")
                if not ref:
                    continue
                cited.add(ref)
                quote = (ev.get("quote") or "").strip()
                if quote:
                    usage[ref]["quotes"] += 1
                    usage[ref]["quoteChars"] += len(quote)
            for ref in cited:
                usage[ref]["claims"].add(cid)
                usage[ref]["compounds"].add(name)

    return records, usage, packet_count


def rights_view(entry, lane_authorized):
    """Report recorded rights metadata and the gaps, without judging it."""
    rights = (entry or {}).get("rights", {}) or {}
    acquisition = (entry or {}).get("acquisition", {}) or {}
    policy = (entry or {}).get("evidencePolicy", {}) or {}

    gaps = [label for field, label in REQUIRED_RIGHTS_FIELDS if not rights.get(field)]
    status = rights.get("reviewStatus")

    view = {
        "reviewStatus": status,
        "legalBasisOrLicense": rights.get("legalBasisOrLicense"),
        "termsUrl": rights.get("termsUrl"),
        "verifiedAtUtc": rights.get("verifiedAtUtc"),
        "allowedUses": rights.get("allowedUses") or [],
        "prohibitedUses": rights.get("prohibitedUses") or [],
        "acquisitionEnabled": acquisition.get("enabled"),
        "acquisitionMethod": acquisition.get("method"),
        "authorizedFieldUse": policy.get("authorizedFieldUse") or [],
        "registryAuthorityTier": policy.get("authorityTier"),
        "missingReviewFields": gaps,
        "coveredByDecisionBatch": lane_authorized,
    }
    return view, status, gaps


def build(args):
    reg, alias_to_class, classes = load_registry(args.registry)
    authorized_lanes, pinned_hash = load_authorized_lanes(args.decisions)
    records, usage, packet_count = collect_corpus(args.evidence)

    with open(args.registry, "rb") as fh:
        registry_sha = hashlib.sha256(fh.read()).hexdigest()

    # Group by work so duplicate identifiers for one document collapse.
    work_members = collections.defaultdict(list)
    for sid, rec in records.items():
        work_members[normalize_url(rec.get("url")) or ("__nourl__:" + sid)].append(sid)

    # split_url is deliberately case-sensitive right of the host, so two records
    # differing only by case stay separate works. That is the safe direction, but
    # it must not be silent -- surface them so a real duplicate is not read as two.
    case_variants = collections.defaultdict(set)
    for key in work_members:
        case_variants[key.lower()].add(key)
    case_variant_keys = {k for group in case_variants.values() if len(group) > 1 for k in group}

    items, defect_index = [], collections.Counter()
    for sid in sorted(records):
        rec = dict(records[sid])
        cls = alias_to_class.get(sid)
        entry = classes.get(cls)
        lane_authorized = cls in authorized_lanes if cls else False
        rights, status, gaps = rights_view(entry, lane_authorized)

        work = normalize_url(rec.get("url")) or ("__nourl__:" + sid)
        siblings = sorted(s for s in work_members[work] if s != sid)

        defects = []
        if not cls:
            defects.append("unregistered-sourceid")
        if siblings:
            defects.append("duplicate-identifiers-for-one-work")
        if work in case_variant_keys:
            defects.append("case-variant-identifiers")
        if not rec.get("url"):
            defects.append("no-url-recorded")
        if cls and entry:
            # Host and path are separate signals. A different host means the item
            # is served by a different platform under different terms -- that is
            # the precisionFDA/api.semanticscholar case and it matters. A different
            # path under the same host is usually a class primaryUrl pointing at a
            # listing page (nih-ods points at factsheets/list-all/), so it is
            # reported but is not by itself evidence of a misassignment.
            p_host, p_path = split_url(entry.get("identity", {}).get("primaryUrl"))
            i_host, i_path = split_url(rec.get("url"))
            if p_host and i_host and i_host != p_host:
                defects.append("different-host-from-class")
            elif p_path and i_path and not i_path.startswith(p_path):
                defects.append("different-path-from-class")
        if status == "approved" and not lane_authorized:
            defects.append("approval-unbacked")
        if gaps:
            defects.append("incomplete-rights-record")
        for d in defects:
            defect_index[d] += 1

        route_host, _ = split_url(rec.get("url"))
        if route_host in AGGREGATOR_HOSTS:
            defects.append("aggregator-routed-record")
            defect_index["aggregator-routed-record"] += 1

        u = usage.get(sid, {"claims": set(), "quotes": 0, "quoteChars": 0, "compounds": set()})
        rec.update({
            "registryClass": cls,
            "workKey": work,
            "acquisitionRouteHost": route_host,
            # The corpus's own publisher string, carried verbatim. It is an
            # UNVERIFIED declaration by whoever wrote the packet, not a checked
            # ownership finding: basaria-2013-jgerontol-rct declares "Journals of
            # Gerontology ... accessed via DrugBank" while the article's actual
            # notice names the 2012 author as copyright holder, with OUP publishing
            # on behalf of the Gerontological Society of America. Verifying an owner
            # means reading the work's own notice; this field cannot substitute.
            "declaredPublisherUnverified": rec.get("publisher"),
            "duplicateIdentifiers": siblings,
            "usage": {
                "claimCount": len(u["claims"]),
                "quoteCount": u["quotes"],
                "quoteChars": u["quoteChars"],
                "compounds": sorted(u["compounds"]),
            },
            "rights": rights,
            "defects": defects,
            "rightsBasis": "pending" if status != "approved" or not lane_authorized else "recorded",
        })
        items.append(rec)

    cited = [i for i in items if i["usage"]["claimCount"] > 0]
    inventory = {
        "schemaVersion": "1.0.0",
        "recordType": "source-inventory",
        "generatedAtUtc": datetime.datetime.now(datetime.timezone.utc)
        .replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "generatedBy": "tools/research/build-source-inventory.py",
        "disclaimer": (
            "Inventory of recorded metadata. Not a rights determination, not legal "
            "advice. rightsBasis 'recorded' means a decision batch covers the lane "
            "and the registry marks it approved -- not that the use is permitted."
        ),
        "inputs": {
            "registry": args.registry,
            "registrySha256": registry_sha,
            "decisionBatch": args.decisions,
            "decisionBatchPinnedSha256": pinned_hash,
            "registryMatchesDecisionBinding": pinned_hash == registry_sha,
            "evidenceGlob": args.evidence,
            "packetsRead": packet_count,
        },
        "counts": {
            "distinctSourceIds": len(items),
            # Records sharing a normalized URL. Kept under this key for stability,
            # but it is an acquisition-identity grouping, NOT a count of legally
            # distinct works: one URL can serve different versions or formats of a
            # work over time. Establishing a distinct work is a human judgement.
            "distinctWorks": len({i["workKey"] for i in items}),
            "distinctSourceIdsCited": len(cited),
            "distinctWorksCited": len({i["workKey"] for i in cited}),
            "totalClaimCitations": sum(i["usage"]["claimCount"] for i in items),
            "totalStoredQuotes": sum(i["usage"]["quoteCount"] for i in items),
            "totalStoredQuoteChars": sum(i["usage"]["quoteChars"] for i in items),
        },
        "defectCounts": dict(sorted(defect_index.items())),
        "byClass": {},
        "items": items,
    }

    for cls in sorted({i["registryClass"] for i in items if i["registryClass"]}):
        members = [i for i in items if i["registryClass"] == cls]
        entry = classes.get(cls, {})
        inventory["byClass"][cls] = {
            "sourceIds": len(members),
            "works": len({i["workKey"] for i in members}),
            "claimCitations": sum(i["usage"]["claimCount"] for i in members),
            "storedQuotes": sum(i["usage"]["quoteCount"] for i in members),
            "reviewStatus": (entry.get("rights") or {}).get("reviewStatus"),
            "acquisitionEnabled": (entry.get("acquisition") or {}).get("enabled"),
            "coveredByDecisionBatch": cls in authorized_lanes,
            "hosts": sorted({urlparse(i["url"]).netloc.lower().replace("www.", "")
                             for i in members if i.get("url")}),
        }

    unregistered = [i["sourceId"] for i in items if not i["registryClass"]]
    if unregistered:
        inventory["unregisteredSourceIds"] = sorted(unregistered)

    return inventory


def summarize(inv, stream):
    w = stream.write
    c, inp = inv["counts"], inv["inputs"]
    w("Source inventory  (%s)\n" % inv["generatedAtUtc"])
    w("  packets read              %d\n" % inp["packetsRead"])
    w("  registry sha256           %s\n" % inp["registrySha256"][:16])
    w("  matches decision binding  %s\n" % inp["registryMatchesDecisionBinding"])
    w("\nCounts that are not interchangeable:\n")
    w("  normalized URL groups     %d   (a grouping, not a count of legally\n" % c["distinctWorks"])
    w("                                 distinct works -- one URL can serve\n")
    w("                                 several versions or formats)\n")
    w("  distinct sourceIds        %d   (identifiers, inflated by duplicates)\n" % c["distinctSourceIds"])
    w("  claim citations           %d   (usage, not works)\n" % c["totalClaimCitations"])
    w("  stored quotes             %d   (%s chars -- remediation operates on this)\n"
      % (c["totalStoredQuotes"], format(c["totalStoredQuoteChars"], ",")))
    if inv["defectCounts"]:
        w("\nDefects:\n")
        for k, v in inv["defectCounts"].items():
            w("  %-38s %d\n" % (k, v))
    w("\n%-24s %6s %6s %7s %7s  %-9s %s\n"
      % ("class", "works", "ids", "claims", "quotes", "acquire", "status"))
    for cls, s in sorted(inv["byClass"].items(), key=lambda kv: -kv[1]["storedQuotes"]):
        flag = "" if s["coveredByDecisionBatch"] else "  <- no decision batch"
        w("%-24s %6d %6d %7d %7d  %-9s %s%s\n"
          % (cls[:24], s["works"], s["sourceIds"], s["claimCitations"], s["storedQuotes"],
             str(s["acquisitionEnabled"]), s["reviewStatus"], flag))
    if inv.get("unregisteredSourceIds"):
        w("\nunregistered sourceIds: %d\n" % len(inv["unregisteredSourceIds"]))


def main():
    p = argparse.ArgumentParser(description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--registry", default=DEFAULT_REGISTRY)
    p.add_argument("--evidence", default=DEFAULT_EVIDENCE_GLOB)
    p.add_argument("--decisions", default=DEFAULT_DECISIONS)
    p.add_argument("--out", default=DEFAULT_OUT)
    p.add_argument("--class", dest="only_class", help="summarize a single registry class")
    args = p.parse_args()

    inv = build(args)

    if args.only_class:
        inv["items"] = [i for i in inv["items"] if i["registryClass"] == args.only_class]
        inv["byClass"] = {k: v for k, v in inv["byClass"].items() if k == args.only_class}

    os.makedirs(args.out, exist_ok=True)
    dest = os.path.join(args.out, "source-inventory.json")
    with open(dest, "w", encoding="utf-8") as fh:
        json.dump(inv, fh, indent=2, ensure_ascii=False)
        fh.write("\n")

    summarize(inv, sys.stdout)
    sys.stdout.write("\nwrote %s\n" % dest)
    return 0


if __name__ == "__main__":
    sys.exit(main())
