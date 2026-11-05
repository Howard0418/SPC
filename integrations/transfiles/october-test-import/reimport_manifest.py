"""Build a second-generation TEST-only chemical manifest from reviewed precedence decisions."""

import copy


def _key(row):
    return str(row.get("fileHash", "")).lower(), int(row.get("sourceRow", row.get("row", 0)))


def apply_approved_precedence(manifest, approved_winners, approved_workbook_sha256):
    result = copy.deepcopy(manifest)
    month = result.get("summary", {}).get("month")
    if month not in {"2026-09", "2026-10"}:
        raise ValueError("Only 2026-09 and 2026-10 are allowed")
    rows = result.get("rows", [])

    # Exact user-approved 9/2 N1(open) identity. N1(close) and other dates are not broadened.
    if month == "2026-09":
        for row in rows:
            if row.get("fileDay") == "2026-09-02" and str(row.get("sheet", "")).replace("（", "(").startswith("N1 (") \
                    and 7 <= int(row.get("sourceRow", 0)) <= 20 and row.get("line") == "N2":
                row["line"] = "N1"
                row["approvedLineOverride"] = True

    winners = {_key(x) for x in approved_winners if str(x.get("day", month)).startswith(month)}
    found = set()
    winner_business = set()
    for row in rows:
        if _key(row) in winners:
            row["status"] = "validated"
            row["issue"] = None
            row["approvedLaterSource"] = True
            found.add(_key(row))
            winner_business.add((row.get("fileDay"), row.get("line"), row.get("slot"), row.get("analysisItem"), row.get("samplingPhase"), row.get("samplingStage")))
    missing = winners - found
    if missing:
        raise ValueError(f"Approved winner missing from source manifest: {sorted(missing)}")
    for row in rows:
        business = (row.get("fileDay"), row.get("line"), row.get("slot"), row.get("analysisItem"), row.get("samplingPhase"), row.get("samplingStage"))
        if business in winner_business and _key(row) not in winners:
            row["status"] = "blocked"
            row["issue"] = "已由使用者核准的後檔取代。"

    summary = result.setdefault("summary", {})
    summary["reimportVersion"] = 2
    summary["approvedWorkbookSha256"] = approved_workbook_sha256
    summary["databaseWritten"] = False
    summary["validatedRows"] = sum(row.get("status") == "validated" for row in rows)
    return result
