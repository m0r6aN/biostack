#!/usr/bin/env python3
"""Behavioural tests for build-source-inventory.py.

Run:  python -m pytest tools/research/test_build_source_inventory.py -q

These build synthetic packets in a temp directory and assert on the emitted
inventory. Nothing here reads or writes the real corpus, registry or decision
batch, and no test asserts anything about authorization: a count of declarations
is not a count of permissions, and no assertion below should ever be read as
evidence that a source may be used.
"""
import argparse
import importlib.util
import json
import os
import pathlib
import sys

import pytest

_SPEC = importlib.util.spec_from_file_location(
    "build_source_inventory",
    pathlib.Path(__file__).with_name("build-source-inventory.py"),
)
inv = importlib.util.module_from_spec(_SPEC)
_SPEC.loader.exec_module(inv)


# --------------------------------------------------------------------------
# fixtures
# --------------------------------------------------------------------------

def _source(sid, url, **over):
    rec = {
        "sourceId": sid, "sourceType": "structured-database", "authorityTier": "A2",
        "title": "T", "publisher": "P", "url": url,
        "doi": None, "pmid": None, "publishedAt": None,
        "accessedAt": "2026-01-01T00:00:00Z",
    }
    rec.update(over)
    return rec


def _packet(compound, sources, claims):
    return {"schemaVersion": "1.0.0", "recordType": "evidence-packet",
            "compound": {"name": compound}, "sources": sources, "claims": claims}


def _claim(cid, refs, evidence=()):
    return {"claimId": cid, "claimType": "identity", "sourceRefs": list(refs),
            "extractedEvidence": [dict(e) for e in evidence]}


def _write(tmp_path, packets):
    ev = tmp_path / "evidence"
    ev.mkdir(exist_ok=True)
    for name, packet in packets.items():
        (ev / ("%s.evidence.json" % name)).write_text(
            json.dumps(packet, indent=1), encoding="utf-8")

    registry = {"schemaVersion": "2.0.0", "sources": [{
        "identity": {"sourceId": "cls", "aliases": sorted(
            {s["sourceId"] for p in packets.values() for s in p["sources"]}),
            "primaryUrl": "https://example.org/"},
        "rights": {"reviewStatus": "approved"},
        "acquisition": {"enabled": False},
        "evidencePolicy": {"authorityTier": "A2"},
    }]}
    reg = tmp_path / "registry.json"
    reg.write_text(json.dumps(registry, indent=1), encoding="utf-8")

    return argparse.Namespace(
        registry=str(reg), evidence=str(ev / "*.evidence.json"),
        decisions=str(tmp_path / "missing-decisions.json"),
        out=str(tmp_path / "out"), only_class=None)


def _item(result, sid):
    return next(i for i in result["items"] if i["sourceId"] == sid)


# --------------------------------------------------------------------------
# repeated id with differing metadata -- the first-occurrence-wins regression
# --------------------------------------------------------------------------

def test_repeated_id_with_different_date_keeps_both_occurrences(tmp_path):
    """A second packet's retrieval date must not be discarded."""
    url = "https://example.org/a"
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", url, accessedAt="2026-01-01T00:00:00Z")],
                         [_claim("c1", ["s1"])]),
        "beta": _packet("beta", [_source("s1", url, accessedAt="2026-06-30T00:00:00Z")],
                        [_claim("c2", ["s1"])]),
    })
    item = _item(inv.build(args), "s1")

    assert item["occurrenceCount"] == 2
    dates = sorted(o["accessedAt"] for o in item["occurrences"])
    assert dates == ["2026-01-01T00:00:00Z", "2026-06-30T00:00:00Z"]
    assert "metadata-varies-across-packets" in item["defects"]

    variant = item["variantFields"]["accessedAt"]
    assert {v["value"] for v in variant} == set(dates)
    assert {p for v in variant for p in v["packets"]} == {
        "alpha.evidence.json", "beta.evidence.json"}


def test_repeated_id_with_different_url_groups_under_both(tmp_path):
    """pubchem-cid-44200882's real shape: one id, two recorded URLs."""
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"])]),
        "beta": _packet("beta", [_source("s1", "https://example.org/b")],
                        [_claim("c2", ["s1"])]),
    })
    result = inv.build(args)
    item = _item(result, "s1")

    assert "url" in item["variantFields"]
    assert item["normalizedUrlGroupKeys"] == [
        "example.org/a", "example.org/b"]
    # One id declaring two URLs contributes two groups; it must not be counted
    # as one group, which would hide the second URL from a rights reviewer.
    assert result["counts"]["normalizedUrlGroups"] == 2
    assert result["counts"]["distinctSourceIds"] == 1
    assert result["counts"]["sourceOccurrences"] == 2


def test_agreeing_occurrences_are_not_flagged_as_variant(tmp_path):
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"])]),
        "beta": _packet("beta", [_source("s1", "https://example.org/a")],
                        [_claim("c2", ["s1"])]),
    })
    item = _item(inv.build(args), "s1")
    assert item["occurrenceCount"] == 2
    assert item["variantFields"] == {}
    assert "metadata-varies-across-packets" not in item["defects"]


# --------------------------------------------------------------------------
# identity: query values and path case must not collapse
# --------------------------------------------------------------------------

def test_distinct_query_values_stay_distinct(tmp_path):
    """DailyMed serves every label from one path, keyed only by ?setid=."""
    base = "https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=%s"
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", base % "AAA"),
                                   _source("s2", base % "BBB")],
                         [_claim("c1", ["s1", "s2"])]),
    })
    result = inv.build(args)
    assert result["counts"]["normalizedUrlGroups"] == 2
    assert _item(result, "s1")["sharedNormalizedUrlWith"] == []


def test_distinct_path_case_stays_distinct_and_is_reported(tmp_path):
    """UNII/NBK/PMCID case carries identity, so it must survive normalization."""
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/unii/MU72812GK0"),
                                   _source("s2", "https://example.org/unii/mu72812gk0")],
                         [_claim("c1", ["s1", "s2"])]),
    })
    result = inv.build(args)
    assert result["counts"]["normalizedUrlGroups"] == 2
    # Splitting is the safe direction, but it must be visible rather than silent.
    assert "case-variant-identifiers" in _item(result, "s1")["defects"]
    assert "case-variant-identifiers" in _item(result, "s2")["defects"]


def test_host_case_and_www_still_collapse(tmp_path):
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://WWW.Example.org/a"),
                                   _source("s2", "https://example.org/a")],
                         [_claim("c1", ["s1", "s2"])]),
    })
    result = inv.build(args)
    assert result["counts"]["normalizedUrlGroups"] == 1
    assert _item(result, "s1")["sharedNormalizedUrlWith"] == ["s2"]
    assert "shared-normalized-url-with-other-ids" in _item(result, "s1")["defects"]


# --------------------------------------------------------------------------
# aggregate usage must be unchanged by occurrence retention
# --------------------------------------------------------------------------

def test_usage_aggregates_count_claims_not_occurrences(tmp_path):
    """Retaining occurrences must not inflate claim, quote or char totals."""
    ev = [{"sourceRef": "s1", "quote": "abcde", "pageOrSection": "Sec 1"}]
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"], ev)]),
        "beta": _packet("beta", [_source("s1", "https://example.org/a")],
                        [_claim("c2", ["s1"], ev)]),
    })
    result = inv.build(args)
    item = _item(result, "s1")

    assert item["occurrenceCount"] == 2
    assert item["usage"]["claimCount"] == 2
    assert item["usage"]["quoteCount"] == 2
    assert item["usage"]["quoteChars"] == 10
    assert result["counts"]["totalClaimCitations"] == 2
    assert result["counts"]["totalStoredQuotes"] == 2


def test_claim_cited_via_sourcerefs_and_via_evidence_counts_once(tmp_path):
    """The bug that made an early hand count read 24 claims instead of 30."""
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"],
                                 [{"sourceRef": "s1", "quote": "q", "pageOrSection": None}])]),
    })
    assert _item(inv.build(args), "s1")["usage"]["claimCount"] == 1


def test_claim_referencing_without_quote_is_still_counted(tmp_path):
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"]),
                          _claim("c2", ["s1"],
                                 [{"sourceRef": "s1", "quote": "q", "pageOrSection": None}])]),
    })
    item = _item(inv.build(args), "s1")
    assert item["usage"]["claimCount"] == 2
    assert item["usage"]["quoteCount"] == 1


# --------------------------------------------------------------------------
# locators are retained; quote text is not
# --------------------------------------------------------------------------

def test_locators_retained_without_copying_quote_text(tmp_path):
    secret = "VERBATIM RESTRICTED TEXT"
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a")],
                         [_claim("c1", ["s1"],
                                 [{"sourceRef": "s1", "quote": secret,
                                   "pageOrSection": "Mechanism of Action"}])]),
    })
    result = inv.build(args)
    locators = _item(result, "s1")["usage"]["citationLocators"]

    assert locators == [{"packet": "alpha.evidence.json", "claimId": "c1",
                         "pageOrSection": "Mechanism of Action",
                         "quoteChars": len(secret)}]
    # The whole inventory must never carry the excerpt itself.
    assert secret not in json.dumps(result)


# --------------------------------------------------------------------------
# authorization must never be derived from grouping
# --------------------------------------------------------------------------

def test_grouping_never_confers_authorization(tmp_path):
    """Registry says approved; no decision batch exists. Must stay pending."""
    args = _write(tmp_path, {
        "alpha": _packet("alpha", [_source("s1", "https://example.org/a"),
                                   _source("s2", "https://example.org/a")],
                         [_claim("c1", ["s1", "s2"])]),
    })
    result = inv.build(args)
    for sid in ("s1", "s2"):
        item = _item(result, sid)
        assert item["rights"]["reviewStatus"] == "approved"
        assert item["rights"]["coveredByDecisionBatch"] is False
        assert item["rightsBasis"] == "pending"
        assert "approval-unbacked" in item["defects"]
    assert result["byClass"]["cls"]["coveredByDecisionBatch"] is False


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-q"]))
