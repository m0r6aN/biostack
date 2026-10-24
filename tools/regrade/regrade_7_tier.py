#!/usr/bin/env python3
"""Regrade substance records to the 7-tier methodology scale.

Source of the rules: docs/biostack/evidence-grading-methodology-v1.md, "Evidence Tier
System" table and "Contradiction Handling" decision guide. No rule beyond those
criteria is added; this script only applies them to what each record already holds
(legacy tier, confidence, claim text, record source types).

Per claim:
  0. Established         - a high-confidence regulatory/label/agency fact graded Strong and
                           backed by an authoritative source (it is verified as stated).
  1. Anecdotal Signal    - claim rests on user, community or case reports and legacy grade
                           is not Strong/Moderate.
  2. Hype Exceeds Evidence - claim text describes promotional / online certainty and the
                           legacy grade is not Strong/Moderate ("promotional certainty
                           materially exceeds evidence").
  3. Contradicted        - claim text reports a negative or conflicting human finding
                           (decision guide: "stronger or better-matched evidence directly
                           conflicts with the claim").
  4. Established         - legacy Strong with direct human or authoritative (label,
                           regulator, guideline) evidence for the exact claim; or a
                           high-confidence regulatory/label fact backed by such a source.
  5. Early Evidence      - human evidence that is not yet settled (legacy Moderate, Strong
                           without human/authoritative cue, or Limited with human cue).
  6. Plausible Theory    - mechanistic, preclinical or indirect rationale without direct
                           human outcome evidence.
  7. Unsupported         - legacy Insufficient/Unknown with nothing above.

Overall tier = best supportive claim tier (Established > Early Evidence > Plausible Theory >
Anecdotal Signal > Unsupported), ignoring identity and regulatory-fact claims, which say
what a compound is, not whether it works. If every claim is Contradicted or Hype Exceeds
Evidence, that tier is the overall tier.

Each regraded claim keeps its previous grade in `legacyTier` and a one-line
`gradeRationale`. The AI council (PR D) reviews the result; disagreements escalate to
the owner.

Usage: regrade_7_tier.py SEED.json [--report OUT.json]
Line endings and key order are preserved.
"""
import json
import re
import sys
from collections import Counter

TIERS = ["Established", "Plausible Theory", "Early Evidence", "Anecdotal Signal",
         "Contradicted", "Unsupported", "Hype Exceeds Evidence"]
LEGACY = {"Strong", "Moderate", "Limited", "Insufficient", "Unknown"}

HUMAN = re.compile(r"\b(trial|trials|rct|randomi[sz]ed|placebo|meta-analys|systematic review|human|humans|"
                   r"adults?|patients?|participants|volunteers|clinical|cohort|phase [1-4]|phase i|"
                   r"double-blind|crossover|men|women|subjects|observational)\b", re.I)
AUTHORITY = re.compile(r"\b(fda|label|labeled|labelled|approved|approval|prescription|prescribing|regulat\w*|"
                       r"dea|schedule [ivx]+|controlled substance|wada|prohibited|banned|ema|monograph|"
                       r"guideline|dietary reference|rda|upper limit|compounding|503a|503b)\b", re.I)
MECH = re.compile(r"\b(preclinical|animal|animals|in vitro|rodent|rodents|mice|mouse|rats?|cell|cells|"
                  r"cellular|mechanis\w*|theoretical|hypothes\w*|proposed|pathway|receptor|"
                  r"biomarker|ex vivo|porcine|canine|primate)\b", re.I)
NEGATIVE = re.compile(r"\b(did not|does not|no significant|not significant|failed to|no benefit|"
                      r"no meaningful|no effect|no clear (benefit|effect)|not superior|null|"
                      r"contradict\w*|conflicting|inconsistent|not supported by|no improvement|"
                      r"no difference)\b", re.I)
HYPE = re.compile(r"\b(marketed|marketing|promoted|promotion(al)?|hype|influencer\w*|touted|"
                  r"online claims?|sellers?|vendors?|gray-market|grey-market|popular claims?|"
                  r"social media|affiliate|overstated|exaggerat\w*|claimed online)\b", re.I)
ANECDOTE = re.compile(r"\b(anecdot\w*|user reports?|users report|self-report\w*|community|forum\w*|"
                      r"reddit|testimonial\w*|case reports?|case series|n-of-1|word of mouth)\b", re.I)
IDENTITY = re.compile(r"^\s*\S.{0,80}?\b(is|are)( described as| classified as)? (a|an)\b", re.I)


def is_regulatory_fact(text):
    return bool(AUTHORITY.search(text)) and not HUMAN.search(text)


def grade_claim(claim, record_has_authority):
    text = claim.get("claim", "")
    legacy = claim.get("legacyTier", claim.get("tier", "Unknown"))
    conf = (claim.get("confidence") or "").lower()
    strongish = legacy in ("Strong", "Moderate")

    if legacy == "Strong" and conf == "high" and AUTHORITY.search(text) and record_has_authority:
        return "Established", "High-confidence regulatory, label or agency fact backed by an authoritative source."
    if ANECDOTE.search(text) and not strongish:
        return "Anecdotal Signal", "Support is user, community or case reports, not generalizable evidence."
    if HYPE.search(text) and not strongish:
        return "Hype Exceeds Evidence", "Claim describes promotional or online certainty beyond the cited evidence."
    if NEGATIVE.search(text) and HUMAN.search(text):
        return "Contradicted", "Claim reports a negative or conflicting human finding."
    if is_regulatory_fact(text) and conf == "high" and record_has_authority:
        return "Established", "High-confidence regulatory or label fact backed by an authoritative source."
    if legacy == "Strong":
        if HUMAN.search(text) or record_has_authority:
            return "Established", "Consistent human or authoritative evidence for the stated claim (was Strong)."
        if MECH.search(text):
            return "Plausible Theory", "Mechanistic or preclinical support without direct human outcome evidence (was Strong)."
        return "Early Evidence", "Human relevance not shown for the exact claim; downgraded from Strong."
    if legacy == "Moderate":
        if MECH.search(text) and not HUMAN.search(text):
            return "Plausible Theory", "Mechanistic or preclinical support without direct human outcome evidence (was Moderate)."
        return "Early Evidence", "Human evidence exists but is not settled (was Moderate)."
    if legacy == "Limited":
        if HUMAN.search(text):
            return "Early Evidence", "Preliminary human evidence with documented limits (was Limited)."
        if is_regulatory_fact(text) and record_has_authority:
            return "Established", "Regulatory or label fact backed by an authoritative source (was Limited)."
        return "Plausible Theory", "Rationale is mechanistic or indirect; direct human outcome evidence is weak (was Limited)."
    # Insufficient / Unknown
    if MECH.search(text):
        return "Plausible Theory", "Only mechanistic or preclinical rationale found (was %s)." % legacy
    return "Unsupported", "No adequate source-verified support found for the specific claim (was %s)." % legacy


RANK = {"Established": 5, "Early Evidence": 4, "Plausible Theory": 3, "Anecdotal Signal": 2, "Unsupported": 1}


def overall(claims):
    support = [c for c in claims if not IDENTITY.match(c["claim"]) and not is_regulatory_fact(c["claim"])]
    pool = support or claims
    best = [c["tier"] for c in pool if c["tier"] in RANK]
    if best:
        return max(best, key=lambda t: RANK[t])
    neg = [c["tier"] for c in pool if c["tier"] in ("Contradicted", "Hype Exceeds Evidence")]
    if neg:
        return Counter(neg).most_common(1)[0][0]
    return "Unsupported"


def grade_indication(ind, claims_overall, record_has_authority):
    fake = {"claim": ind.get("benefitSummary", "") + " " + ind.get("useCase", ""),
            "tier": ind.get("evidenceTier", "Unknown"), "confidence": "moderate"}
    tier, _ = grade_claim(fake, record_has_authority)
    return tier


def regrade(records):
    report = []
    for r in records:
        srcs = {s.get("sourceType") for s in r["provenance"]["sourceRecords"]}
        has_auth = bool(srcs & {"label", "regulator", "guideline", "manufacturer"})
        ev = r["evidence"]
        old_overall = ev["overallTier"]
        rows = []
        for c in ev["claimSpecificEvidence"]:
            if c["tier"] in TIERS and "legacyTier" in c:
                rows.append({"claim": c["claim"], "from": c["legacyTier"], "to": c["tier"], "why": c.get("gradeRationale", "")})
                continue
            legacy = c["tier"]
            new, why = grade_claim(c, has_auth)
            c["legacyTier"] = legacy
            c["tier"] = new
            c["gradeRationale"] = why
            rows.append({"claim": c["claim"], "from": legacy, "to": new, "why": why})
        for ind in r["indications"]:
            if ind.get("evidenceTier") in LEGACY:
                ind["evidenceTier"] = grade_indication(ind, None, has_auth)
        ev["overallTier"] = overall(ev["claimSpecificEvidence"]) if ev["claimSpecificEvidence"] else (
            "Unsupported" if old_overall in ("Insufficient", "Unknown") else
            {"Strong": "Early Evidence", "Moderate": "Early Evidence", "Limited": "Plausible Theory"}.get(old_overall, old_overall))
        report.append({"compound": r["identity"]["canonicalName"], "overallFrom": old_overall,
                       "overallTo": ev["overallTier"], "claims": rows})
    return report


def main():
    path = sys.argv[1]
    out = sys.argv[sys.argv.index("--report") + 1] if "--report" in sys.argv else None
    raw = open(path, "rb").read()
    crlf = b"\r\n" in raw
    records = json.loads(raw.decode("utf-8"))
    report = regrade(records)
    text = json.dumps(records, indent=2, ensure_ascii=False) + "\n"
    if crlf:
        text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode("utf-8"))
    if out:
        summary = {
            "method": "docs/biostack/evidence-grading-methodology-v1.md (7 tiers); tools/regrade/regrade_7_tier.py",
            "records": len(report),
            "overallCounts": Counter(x["overallTo"] for x in report),
            "claimCounts": Counter(c["to"] for x in report for c in x["claims"]),
            "transitions": Counter(f'{c["from"]} -> {c["to"]}' for x in report for c in x["claims"]),
            "compounds": report,
        }
        open(out, "w", encoding="utf-8").write(json.dumps(summary, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps({"records": len(report),
                      "overall": Counter(x["overallTo"] for x in report),
                      "claims": Counter(c["to"] for x in report for c in x["claims"])}, indent=1))


if __name__ == "__main__":
    main()
