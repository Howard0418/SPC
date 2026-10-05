"""Read-only inventory: compare Sept 2026 Etch reports between test and production.

Does not write to any database. Uses release appsettings connection strings.
"""
from __future__ import annotations

import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path

import pyodbc

ROOT = Path(r"D:\SPC\specs\20260922-etch-production-inventory")
STAGING = Path(r"D:\SPC\release-staging\etch-prod-inventory-20260922")
MANIFEST = Path(r"D:\SPC\release-staging\etch-month-20260918\input.json")

CODES = ("ETCH_A_AVG", "ETCH_B_AVG", "ETCH_RATE", "ETCH_LINE_SPEED")
LINES = ("PT1", "PT2", "QE1", "QE2")


def catalog(cs: str) -> tuple[str, str]:
    server = re.search(r"Server=([^;]+)", cs, re.I)
    db = re.search(r"(?:Initial Catalog|Database)=([^;]+)", cs, re.I)
    return (server.group(1) if server else "?", db.group(1) if db else "?")


def load_cs(path: Path, key: str) -> str:
    j = json.loads(path.read_text(encoding="utf-8"))
    return j["ConnectionStrings"][key]


def to_odbc(cs: str) -> str:
    # Prefer ODBC Driver 18/17 if present; TrustServerCertificate already in many strings.
    if "Driver=" in cs:
        return cs
    for driver in (
        "ODBC Driver 18 for SQL Server",
        "ODBC Driver 17 for SQL Server",
        "SQL Server",
    ):
        return f"Driver={{{driver}}};{cs}"
    return cs


def query(cs: str, sql: str, params: tuple = ()) -> list[dict]:
    conn = pyodbc.connect(to_odbc(cs), timeout=30)
    try:
        cur = conn.cursor()
        cur.execute(sql, params)
        cols = [c[0] for c in cur.description]
        rows = []
        for row in cur.fetchall():
            item = {}
            for i, col in enumerate(cols):
                v = row[i]
                if hasattr(v, "isoformat"):
                    v = v.isoformat()
                elif isinstance(v, bytes):
                    v = v.hex()
                item[col] = v
            rows.append(item)
        return rows
    finally:
        conn.close()


def report_key(date: str, line: str) -> str:
    return f"{str(date)[:10]}:{line}"


def main() -> None:
    STAGING.mkdir(parents=True, exist_ok=True)
    ROOT.mkdir(parents=True, exist_ok=True)

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    expected = sorted(
        report_key(r["reportDate"], r["lineCode"]) for r in manifest["reports"]
    )
    expected_set = set(expected)
    expected_points = sum(len(r["points"]) for r in manifest["reports"])

    targets = {
        "spc_test": (
            load_cs(Path(r"D:\SPC\release\test\backend\appsettings.json"), "SqlServer"),
            "PMR_SPC_TEST",
        ),
        "spc_prod": (
            load_cs(
                Path(r"D:\SPC\release\production\backend\appsettings.json"), "SqlServer"
            ),
            "PMR_SPC_2026",
        ),
        "portal_test": (
            load_cs(
                Path(r"D:\PmrPortal\release\test\portal-api\appsettings.json"), "Test"
            ),
            "PMR_PORTAL_TEST",
        ),
        "portal_prod": (
            load_cs(
                Path(r"D:\PmrPortal\release\production\portal-api\appsettings.json"),
                "Production",
            ),
            None,  # discover actual name
        ),
    }

    env_info = {}
    for name, (cs, expect_db) in targets.items():
        server, db = catalog(cs)
        if expect_db and db != expect_db:
            raise SystemExit(f"{name}: expected {expect_db}, got {db}")
        if name.startswith("spc") and server != "172.16.110.16":
            raise SystemExit(f"{name}: unexpected server {server}")
        env_info[name] = {"server": server, "database": db}

    # Portal September reports (headers only)
    portal_sql = """
SELECT CONVERT(varchar(10), r.ReportDate, 23) AS [date],
       r.LineCode AS line,
       r.LineSpeed AS lineSpeed,
       r.OperatorName AS operatorName,
       r.SpcSyncStatus AS spcSyncStatus,
       (SELECT COUNT(*) FROM etch_amount_points p WHERE p.ReportId = r.Id) AS pointCount
FROM etch_amount_reports r
WHERE r.ReportDate >= '20260901' AND r.ReportDate < '20261001'
ORDER BY r.ReportDate, r.LineCode
"""

    # SPC measurements for ETCH:2026-09-
    spc_meas_sql = """
SELECT CONVERT(varchar(10), v.MeasuredAt, 23) AS [date],
       m.MachineCode AS line,
       c.CharacteristicCode AS code,
       COUNT(*) AS rowCount
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
WHERE v.SourceReference LIKE 'ETCH:2026-09-%'
GROUP BY CONVERT(varchar(10), v.MeasuredAt, 23), m.MachineCode, c.CharacteristicCode
ORDER BY [date], line, code
"""

    # Also catch any Sept PROCESS etch without SourceReference pattern
    spc_any_etch_sql = """
SELECT CONVERT(varchar(10), v.MeasuredAt, 23) AS [date],
       m.MachineCode AS line,
       c.CharacteristicCode AS code,
       COUNT(*) AS rowCount,
       SUM(CASE WHEN v.SourceReference LIKE 'ETCH:%' THEN 1 ELSE 0 END) AS etchRefCount,
       SUM(CASE WHEN v.SourceReference IS NULL OR v.SourceReference NOT LIKE 'ETCH:%' THEN 1 ELSE 0 END) AS otherRefCount
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
  AND v.MeasuredAt >= '2026-09-01' AND v.MeasuredAt < '2026-10-01'
GROUP BY CONVERT(varchar(10), v.MeasuredAt, 23), m.MachineCode, c.CharacteristicCode
ORDER BY [date], line, code
"""

    maps_sql = """
SELECT p.Id AS ppcId,
       m.MachineCode AS line,
       c.CharacteristicCode AS code,
       p.IsEnabled AS isEnabled,
       p.ControlScope AS controlScope,
       ct.ChartTypeCode AS chartType
FROM PartProcessCharacteristics p
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
LEFT JOIN ControlChartTypes ct ON ct.Id = p.ChartTypeId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
ORDER BY line, code
"""

    machines_sql = """
SELECT MachineCode AS line, COUNT(*) AS cnt
FROM Machines
WHERE MachineCode IN ('PT1','PT2','QE1','QE2')
GROUP BY MachineCode
ORDER BY line
"""

    chars_sql = """
SELECT CharacteristicCode AS code, COUNT(*) AS cnt
FROM QualityCharacteristics
WHERE CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
GROUP BY CharacteristicCode
ORDER BY code
"""

    xbar_sql = """
SELECT ChartTypeCode AS code, IsEnabled AS isEnabled, COUNT(*) AS cnt
FROM ControlChartTypes
WHERE ChartTypeCode = 'XBAR_S'
GROUP BY ChartTypeCode, IsEnabled
"""

    snapshot = {
        "asOfUtc": datetime.now(timezone.utc).isoformat(),
        "mode": "read-only",
        "manifest": {
            "path": str(MANIFEST),
            "sha256": manifest.get("sha256"),
            "source": manifest.get("source"),
            "expectedReports": len(expected),
            "expectedPoints": expected_points,
            "byLine": dict(Counter(k.split(":")[1] for k in expected)),
            "keys": expected,
        },
        "environment": env_info,
        "portal": {},
        "spc": {},
    }

    for env_name, cs_key in (("test", "portal_test"), ("prod", "portal_prod")):
        cs = targets[cs_key][0]
        rows = query(cs, portal_sql)
        keys = [report_key(r["date"], r["line"]) for r in rows]
        snapshot["portal"][env_name] = {
            "database": env_info[cs_key]["database"],
            "reportCount": len(rows),
            "pointCount": sum(int(r["pointCount"] or 0) for r in rows),
            "byLine": dict(Counter(r["line"] for r in rows)),
            "byStatus": dict(Counter(str(r["spcSyncStatus"]) for r in rows)),
            "keys": keys,
            "reports": rows,
        }

    for env_name, cs_key in (("test", "spc_test"), ("prod", "spc_prod")):
        cs = targets[cs_key][0]
        meas = query(cs, spc_meas_sql)
        any_etch = query(cs, spc_any_etch_sql)
        maps = query(cs, maps_sql)
        machines = query(cs, machines_sql)
        chars = query(cs, chars_sql)
        xbar = query(cs, xbar_sql)
        # rebuild report keys from SourceReference ETCH:yyyy-mm-dd:LINE:...
        ref_keys = set()
        for r in meas:
            ref_keys.add(report_key(r["date"], r["line"]))
        snapshot["spc"][env_name] = {
            "database": env_info[cs_key]["database"],
            "etchSourceRowGroups": meas,
            "etchSourceRowTotal": sum(int(r["rowCount"]) for r in meas),
            "etchSourceReportKeys": sorted(ref_keys),
            "septProcessGroups": any_etch,
            "septProcessRowTotal": sum(int(r["rowCount"]) for r in any_etch),
            "machines": machines,
            "characteristics": chars,
            "xbarS": xbar,
            "mappings": maps,
            "mappingCount": len(maps),
            "enabledMappingCount": sum(1 for m in maps if m["isEnabled"]),
        }

    # Diff vs expected 48
    test_portal_keys = set(snapshot["portal"]["test"]["keys"])
    prod_portal_keys = set(snapshot["portal"]["prod"]["keys"])
    test_spc_keys = set(snapshot["spc"]["test"]["etchSourceReportKeys"])
    prod_spc_keys = set(snapshot["spc"]["prod"]["etchSourceReportKeys"])

    def classify(env_keys: set[str]) -> dict:
        return {
            "inExpectedAndEnv": sorted(expected_set & env_keys),
            "missingInEnv": sorted(expected_set - env_keys),
            "extraInEnv": sorted(env_keys - expected_set),
            "counts": {
                "expected": len(expected_set),
                "env": len(env_keys),
                "match": len(expected_set & env_keys),
                "missing": len(expected_set - env_keys),
                "extra": len(env_keys - expected_set),
            },
        }

    # Master gaps for production: need 4 lines x 4 codes = 16 enabled PROCESS mappings
    prod_maps = snapshot["spc"]["prod"]["mappings"]
    needed = {(line, code) for line in LINES for code in CODES}
    have = {(m["line"], m["code"]) for m in prod_maps if m["isEnabled"] and m["controlScope"] == "PROCESS"}
    have_any = {(m["line"], m["code"]) for m in prod_maps}
    master_gap = {
        "needed": sorted(f"{a}:{b}" for a, b in needed),
        "haveEnabled": sorted(f"{a}:{b}" for a, b in have),
        "missingEnabled": sorted(f"{a}:{b}" for a, b in (needed - have)),
        "presentButNotEnabledOrWrongScope": sorted(
            f"{a}:{b}" for a, b in (have_any - have) if (a, b) in needed
        ),
        "machinesMissing": [line for line in LINES if not any(m["line"] == line and m["cnt"] for m in snapshot["spc"]["prod"]["machines"])],
        "charsMissing": [code for code in CODES if not any(c["code"] == code and c["cnt"] for c in snapshot["spc"]["prod"]["characteristics"])],
        "xbarSPresent": bool(snapshot["spc"]["prod"]["xbarS"]),
    }

    recommendations = []
    if snapshot["portal"]["prod"]["reportCount"] == 0 and snapshot["spc"]["prod"]["etchSourceRowTotal"] == 0:
        recommendations.append("???? Portal/SPC 9??r?k???F?i?????48???P?@ manifest ?W????????J?C")
    if snapshot["portal"]["prod"]["reportCount"] > 0:
        recommendations.append("???? Portal ?w??9?????F???v?????e?A???i?Èa\?C")
    if snapshot["spc"]["prod"]["etchSourceRowTotal"] > 0:
        recommendations.append("???? SPC ?w?? ETCH:2026-09 ?q???F???T?{?P Portal ?O?_?@?P?C")
    if master_gap["missingEnabled"]:
        recommendations.append(
            f"?????? {len(master_gap['missingEnabled'])} ???? PPC?F??J?e???????D?ˆ¨]?P????u??^?C"
        )
    if not master_gap["xbarSPresent"]:
        recommendations.append("?????? XBAR_S ?????????F??J?e??ˆ¨C")
    if test_portal_keys != expected_set:
        recommendations.append("???? Portal ?P manifest 48 ???????@?P?F????????w?A?????C")
    if test_spc_keys != expected_set:
        recommendations.append("???? SPC ETCH ?????P manifest ???????@?P?F????????w?C")

    diff = {
        "portal_test_vs_expected": classify(test_portal_keys),
        "portal_prod_vs_expected": classify(prod_portal_keys),
        "spc_test_vs_expected": classify(test_spc_keys),
        "spc_prod_vs_expected": classify(prod_spc_keys),
        "portal_prod_vs_test": {
            "onlyInTest": sorted(test_portal_keys - prod_portal_keys),
            "onlyInProd": sorted(prod_portal_keys - test_portal_keys),
            "inBoth": sorted(test_portal_keys & prod_portal_keys),
        },
        "masterGapProduction": master_gap,
        "recommendations": recommendations,
    }

    # per-key CSV rows
    csv_lines = ["date,line,key,inManifest,portalTest,portalProd,spcTest,spcProd,suggestedAction"]
    all_keys = sorted(expected_set | test_portal_keys | prod_portal_keys | test_spc_keys | prod_spc_keys)
    for key in all_keys:
        date, line = key.split(":")
        in_m = key in expected_set
        pt = key in test_portal_keys
        pp = key in prod_portal_keys
        st = key in test_spc_keys
        sp = key in prod_spc_keys
        if in_m and not pp and not sp:
            action = "?????i?s?W"
        elif in_m and pp and sp:
            action = "?????w?s?b?]?????e???^"
        elif in_m and (pp ^ sp):
            action = "????Portal/SPC???@?P?]???^"
        elif not in_m and (pp or sp):
            action = "???????B?~??]?f?d?^"
        elif in_m and not pt:
            action = "????????]?????`?^"
        else:
            action = "?f?d"
        csv_lines.append(
            f"{date},{line},{key},{int(in_m)},{int(pt)},{int(pp)},{int(st)},{int(sp)},{action}"
        )

    out_snap = STAGING / "inventory.json"
    out_diff = STAGING / "diff.json"
    out_csv = STAGING / "diff.csv"
    out_snap.write_text(json.dumps(snapshot, ensure_ascii=False, indent=2), encoding="utf-8")
    out_diff.write_text(json.dumps(diff, ensure_ascii=False, indent=2), encoding="utf-8")
    out_csv.write_text("\n".join(csv_lines) + "\n", encoding="utf-8")

    # also copy under specs for SDD evidence
    (ROOT / "inventory.json").write_text(out_snap.read_text(encoding="utf-8"), encoding="utf-8")
    (ROOT / "diff.json").write_text(out_diff.read_text(encoding="utf-8"), encoding="utf-8")
    (ROOT / "diff.csv").write_text(out_csv.read_text(encoding="utf-8"), encoding="utf-8")

    summary = {
        "portal_test": snapshot["portal"]["test"]["reportCount"],
        "portal_prod": snapshot["portal"]["prod"]["reportCount"],
        "spc_test_rows": snapshot["spc"]["test"]["etchSourceRowTotal"],
        "spc_prod_rows": snapshot["spc"]["prod"]["etchSourceRowTotal"],
        "prod_missing_keys": diff["portal_prod_vs_expected"]["counts"]["missing"],
        "prod_master_missing": len(master_gap["missingEnabled"]),
        "recommendations": recommendations,
        "files": [str(out_snap), str(out_diff), str(out_csv)],
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
