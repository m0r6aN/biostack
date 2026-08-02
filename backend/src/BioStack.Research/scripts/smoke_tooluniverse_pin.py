#!/usr/bin/env python3
"""Smoke-check the ToolUniverse pin and allowlist (optional network)."""

from __future__ import annotations

import argparse
import sys

from biostack_research_sidecar.tooluniverse_integration.adapter import (
    PINNED_PACKAGE_VERSION,
    ToolUniverseAdapter,
    create_adapter,
)
from biostack_research_sidecar.tooluniverse_integration.allowlist import load_allowlist


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--live",
        action="store_true",
        help="Invoke allowlisted PubChem name lookup (network).",
    )
    parser.add_argument("--subject", default="aspirin")
    args = parser.parse_args()

    allowlist = load_allowlist()
    print(f"allowlist_version={allowlist.allowlist_version}")
    print(f"allowlist_package={allowlist.package_version}")
    print(f"approved_tools={len(allowlist.approved_tools)}")
    print(f"approved_skills={len(allowlist.approved_skills)}")

    adapter = create_adapter()
    installed = adapter.package_version()
    print(f"installed={installed}")
    if installed != PINNED_PACKAGE_VERSION:
        print(f"FAIL version pin mismatch expected={PINNED_PACKAGE_VERSION}")
        return 1
    adapter.assert_version_pin()
    print("PASS version pin")

    denied = adapter.run_tool("ExecuteAnyTool", {"x": 1})
    if denied.success or denied.error_code != "tool_not_allowlisted":
        print(f"FAIL allowlist gate: {denied}")
        return 1
    print("PASS deny ExecuteAnyTool")

    if args.live:
        result = adapter.run_tool(
            "PubChem_get_CID_by_compound_name",
            {"name": args.subject},
        )
        print(
            f"live tool={result.tool_name} success={result.success} "
            f"error={result.error_code} msg={result.error_message}"
        )
        if not result.success:
            print("WARN live call failed (keys/rate limits may apply); pin still valid")
    else:
        print("skip live network (pass --live to exercise PubChem)")

    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
