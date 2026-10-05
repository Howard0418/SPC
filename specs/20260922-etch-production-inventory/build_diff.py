# -*- coding: utf-8 -*-
from __future__ import annotations
import json
from collections import Counter
from pathlib import Path

ROOT = Path(r"D:\SPC\specs\20260922-etch-production-inventory")
STAGING = Path(r"D:\SPC\release-staging\etch-prod-inventory-20260922")
MANIFEST = Path(r"D:\SPC\release-staging\etch-month-20260918\input.json")
RAW = STAGING / "raw-query.json"
CODES = ("ETCH_A_AVG", "ETCH_B_AVG", "ETCH_RATE", "ETCH_LINE_SPEED")
LINES = ("PT1", "PT2", "QE1", "QE2")


def key(date, line):
    return f"{str(date)[:10]}:{line}"


def as_list(x):
    if x is None:
        return []
    if isinstance(x, list):
        return x
    if isinstance(x, dict):
        if not x:
            return []
        # PowerShell ConvertTo-Json collapses single-element arrays to one object.
        if any(
            k in x
            for k in (
                "ReportDate",
                "LineCode",
                "PpcId",
                "ChartCode",
                "CharCode",
                "PointCnt",
                "RowCnt",
            )
        ):
            return [x]
        return []
    return [x]


def classify(expected, env_keys):
    e, k = set(expected), set(env_keys)
    return {
        "inExpectedAndEnv": sorted(e & k),
        "missingInEnv": sorted(e - k),
        "extraInEnv": sorted(k - e),
        "counts": {
            "expected": len(e),
            "env": len(k),
            "match": len(e & k),
            "missing": len(e - k),
            "extra": len(k - e),
        },
    }


def master(raw, env):
    maps = as_list(raw["spc"][env]["mappings"])
    machines = as_list(raw["spc"][env]["machines"])
    chars = as_list(raw["spc"][env]["characteristics"])
    xbar = as_list(raw["spc"][env]["xbarS"])
    needed = {(ln, cd) for ln in LINES for cd in CODES}
    enabled = {
        (m["LineCode"], m["CharCode"])
        for m in maps
        if m.get("IsEnabled") and m.get("ControlScope") == "PROCESS"
    }
    present = {(m["LineCode"], m["CharCode"]) for m in maps}
    return {
        "mappingCount": len(maps),
        "enabledProcessCount": len(enabled),
        "neededCount": len(needed),
        "missingEnabled": sorted(f"{a}:{b}" for a, b in sorted(needed - enabled)),
        "presentAny": sorted(f"{a}:{b}" for a, b in sorted(present)),
        "machines": machines,
        "characteristics": chars,
        "xbarS": xbar,
        "machinesMissing": [
            ln
            for ln in LINES
            if not any(m.get("LineCode") == ln and int(m.get("Cnt") or 0) > 0 for m in machines)
        ],
        "charsMissing": [
            cd
            for cd in CODES
            if not any(c.get("CharCode") == cd and int(c.get("Cnt") or 0) > 0 for c in chars)
        ],
        "xbarSPresent": len(xbar) > 0,
        "mappings": maps,
    }


def main():
    raw = json.loads(RAW.read_text(encoding="utf-8-sig"))
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    expected = sorted(key(r["reportDate"], r["lineCode"]) for r in manifest["reports"])
    expected_set = set(expected)
    expected_points = sum(len(r["points"]) for r in manifest["reports"])
    portal_test = as_list(raw["portal"]["test"])
    portal_prod = as_list(raw["portal"]["prod"])
    pt_keys = [key(r["ReportDate"], r["LineCode"]) for r in portal_test]
    pp_keys = [key(r["ReportDate"], r["LineCode"]) for r in portal_prod]

    def spc_keys(groups):
        return sorted({key(r["ReportDate"], r["LineCode"]) for r in as_list(groups)})

    st_keys = spc_keys(raw["spc"]["test"]["etchSourceGroups"])
    sp_keys = spc_keys(raw["spc"]["prod"]["etchSourceGroups"])

    def row_total(groups, field="RowCnt"):
        return sum(int(r.get(field) or 0) for r in as_list(groups))

    def point_total(reports):
        return sum(int(r.get("PointCnt") or 0) for r in reports)

    master_test = master(raw, "test")
    master_prod = master(raw, "prod")
    recommendations = []
    if len(pp_keys) == 0 and len(sp_keys) == 0:
        recommendations.append(
            "PROD Portal+SPC Sept etch empty; can plan import of same 48-report manifest."
        )
    prod_db = raw["environment"]["portal_prod"]["Database"]
    if prod_db and "UAT" in str(prod_db).upper():
        recommendations.append(
            f"Portal production connection points to {prod_db}; confirm this is the real production Portal DB before any write."
        )
    if master_prod["missingEnabled"]:
        recommendations.append(
            f"PROD SPC missing {len(master_prod['missingEnabled'])} enabled PROCESS PPC (need 16); import must create masters like test tool."
        )
    if not master_prod["xbarSPresent"]:
        recommendations.append("PROD SPC has no XBAR_S chart type; must create before import.")
    if set(pt_keys) != expected_set:
        recommendations.append("TEST Portal keys do not fully match 48-report manifesto.")
    if set(st_keys) != expected_set:
        recommendations.append("TEST SPC ETCH source keys do not fully match 48-report manifesto.")
    if master_test["enabledProcessCount"] < 16:
        recommendations.append(
            f"TEST SPC enabled PPC count is {master_test['enabledProcessCount']} (expected 16); inspect master query filters."
        )

    quarantined = [
        "2026-09-02:PT2",
        "2026-09-08:PT1",
        "2026-09-11:PT1",
        "2026-09-16:PT1",
        "2026-09-18:PT1",
        "2026-09-18:PT2",
        "2026-09-18:QE1",
        "2026-09-18:QE2",
    ]

    diff = {
        "asOfLocal": raw.get("asOfLocal"),
        "mode": "read-only",
        "environment": raw["environment"],
        "manifest": {
            "path": str(MANIFEST),
            "sha256": manifest.get("sha256"),
            "expectedReports": len(expected),
            "expectedPoints": expected_points,
            "byLine": dict(Counter(k.split(":")[1] for k in expected)),
        },
        "summary": {
            "portalTestReports": len(pt_keys),
            "portalTestPoints": point_total(portal_test),
            "portalProdReports": len(pp_keys),
            "portalProdPoints": point_total(portal_prod),
            "portalProdDatabase": prod_db,
            "spcTestEtchRows": row_total(raw["spc"]["test"]["etchSourceGroups"]),
            "spcProdEtchRows": row_total(raw["spc"]["prod"]["etchSourceGroups"]),
            "spcTestSeptProcessRows": row_total(raw["spc"]["test"]["septProcessGroups"]),
            "spcProdSeptProcessRows": row_total(raw["spc"]["prod"]["septProcessGroups"]),
            "spcTestEnabledPpc": master_test["enabledProcessCount"],
            "spcProdEnabledPpc": master_prod["enabledProcessCount"],
        },
        "portal_test_vs_expected": classify(expected, pt_keys),
        "portal_prod_vs_expected": classify(expected, pp_keys),
        "spc_test_vs_expected": classify(expected, st_keys),
        "spc_prod_vs_expected": classify(expected, sp_keys),
        "portal_prod_vs_test": {
            "onlyInTest": sorted(set(pt_keys) - set(pp_keys)),
            "onlyInProd": sorted(set(pp_keys) - set(pt_keys)),
            "inBoth": sorted(set(pt_keys) & set(pp_keys)),
        },
        "quarantinedStillExcluded": quarantined,
        "masterTest": master_test,
        "masterProd": master_prod,
        "recommendations": recommendations,
    }

    csv_lines = [
        "date,line,key,inManifest,portalTest,portalProd,spcTest,spcProd,suggestedAction"
    ]
    all_keys = sorted(expected_set | set(pt_keys) | set(pp_keys) | set(st_keys) | set(sp_keys))
    for k in all_keys:
        date, line = k.split(":")
        in_m = k in expected_set
        pt = k in pt_keys
        pp = k in pp_keys
        st = k in st_keys
        sp = k in sp_keys
        if k in quarantined:
            action = "quarantined-skip"
        elif in_m and not pp and not sp:
            action = "prod-can-add"
        elif in_m and pp and sp:
            action = "prod-exists-compare-content"
        elif in_m and (pp ^ sp):
            action = "prod-portal-spc-mismatch-stop"
        elif not in_m and (pp or sp):
            action = "prod-extra-review"
        elif in_m and not pt:
            action = "test-missing-baseline-issue"
        else:
            action = "review"
        csv_lines.append(
            f"{date},{line},{k},{int(in_m)},{int(pt)},{int(pp)},{int(st)},{int(sp)},{action}"
        )

    for path in (STAGING, ROOT):
        (path / "diff.json").write_text(
            json.dumps(diff, ensure_ascii=False, indent=2), encoding="utf-8"
        )
        (path / "diff.csv").write_text("\n".join(csv_lines) + "\n", encoding="utf-8")
        (path / "summary.json").write_text(
            json.dumps(
                {
                    "summary": diff["summary"],
                    "recommendations": recommendations,
                    "portal_prod_missing": diff["portal_prod_vs_expected"]["counts"],
                    "spc_prod_missing": diff["spc_prod_vs_expected"]["counts"],
                    "masterProdMissingEnabled": master_prod["missingEnabled"],
                    "masterProdXbarS": master_prod["xbarSPresent"],
                    "masterTestEnabled": master_test["enabledProcessCount"],
                    "masterProdPresentAny": master_prod["presentAny"],
                    "masterTestPresentAny": master_test["presentAny"],
                },
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )

    print(json.dumps(diff["summary"], ensure_ascii=False, indent=2))
    print("RECOMMENDATIONS:")
    for r in recommendations:
        print("-", r)
    print("PROD_MISSING_PPC", len(master_prod["missingEnabled"]))
    print(
        "TEST_ENABLED_PPC",
        master_test["enabledProcessCount"],
        "PROD_ENABLED_PPC",
        master_prod["enabledProcessCount"],
    )
    print("TEST_PRESENT", master_test["presentAny"])
    print("PROD_PRESENT", master_prod["presentAny"])


if __name__ == "__main__":
    main()
