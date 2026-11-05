"""唯讀掃描藥液日報來源檔，建立可重現的來源 manifest。"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from datetime import date, datetime, time, timezone, timedelta
from pathlib import Path
from typing import Any


TAIPEI = timezone(timedelta(hours=8))
DATE_PATTERN = re.compile(r"(?<!\d)(20\d{2})-(\d{1,2})-(\d{1,2})(?!\d)")
SHIFT_TIME = {"EARLY": time(8, 0), "MIDDLE": time(16, 0)}


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _report_date(file_name: str) -> date | None:
    match = DATE_PATTERN.search(Path(file_name).stem)
    if not match:
        return None
    try:
        return date(*(int(value) for value in match.groups()))
    except ValueError:
        return None


def _excluded_reason(path: Path) -> tuple[str, str] | None:
    name = path.name
    if name.startswith("~$"):
        return "EXCEL_TEMP_FILE", "Excel暫存檔不匯入"
    if path.suffix.lower() != ".xlsx":
        return "UNSUPPORTED_EXTENSION", "只接受.xlsx藥液日報"
    if "補液位" in path.stem:
        return "REPLENISHMENT_FILE", "檔名含補液位，依規則排除"
    if "更槽" in path.stem:
        return "TANK_CHANGE_FILE", "檔名含更槽，依規則排除"
    if _report_date(name) is None:
        return "INVALID_REPORT_DATE", "檔名沒有有效西元日期yyyy-M-d或yyyy-MM-dd"
    return None


def scan_source(source_root: Path) -> dict[str, Any]:
    """掃描來源但不開啟Excel、不寫資料庫；回傳排序固定的manifest。"""
    root = Path(source_root)
    if not root.is_dir():
        raise FileNotFoundError(f"來源資料夾不存在：{root}")

    included: list[dict[str, Any]] = []
    excluded: list[dict[str, Any]] = []
    for path in sorted((entry for entry in root.rglob("*") if entry.is_file()), key=lambda item: item.relative_to(root).as_posix().casefold()):
        relative_path = path.relative_to(root).as_posix()
        reason = _excluded_reason(path)
        if reason:
            code, message = reason
            excluded.append({"fileName": path.name, "relativePath": relative_path, "reasonCode": code, "reason": message})
            continue

        report_date = _report_date(path.name)
        assert report_date is not None
        shift = "MIDDLE" if "中班" in path.stem else "EARLY"
        report_datetime = datetime.combine(report_date, SHIFT_TIME[shift], TAIPEI)
        included.append({
            "fileName": path.name,
            "relativePath": relative_path,
            "reportDate": report_date.isoformat(),
            "shift": shift,
            "reportDateTime": report_datetime.isoformat(timespec="seconds"),
            "timeSemantics": "REPORT_SHIFT_TIME_NOT_ACTUAL_SAMPLE_TIME",
            "sizeBytes": path.stat().st_size,
            "sha256": _sha256(path),
        })

    counts: dict[str, int] = {}
    for row in excluded:
        counts[row["reasonCode"]] = counts.get(row["reasonCode"], 0) + 1
    grouped: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for row in included:
        grouped.setdefault((row["reportDate"], row["shift"]), []).append(row)
    conflicts = [
        {
            "reportDate": report_date,
            "shift": shift,
            "fileCount": len(rows),
            "relativePaths": [row["relativePath"] for row in rows],
            "reasonCode": "MULTIPLE_FILES_FOR_REPORT_SHIFT",
            "reason": "同一報表日期與班別有多份合格檔，須由核准清單指定，不自動覆蓋",
        }
        for (report_date, shift), rows in sorted(grouped.items())
        if len(rows) > 1
    ]
    return {
        "schemaVersion": "chemical-source-manifest-v2",
        "sourceRoot": str(root),
        "rules": {
            "earlyTime": "08:00:00+08:00",
            "middleTime": "16:00:00+08:00",
            "timeSemantics": "報表班別時間，非實際取樣時間",
            "excludedNameTokens": ["補液位", "更槽"],
            "nightShiftCreated": False,
        },
        "summary": {"scanned": len(included) + len(excluded), "included": len(included), "excluded": len(excluded), "conflictGroups": len(conflicts), "excludedByReason": counts},
        "included": included,
        "excluded": excluded,
        "conflicts": conflicts,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="唯讀建立藥液日報來源manifest")
    parser.add_argument("source", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest = scan_source(args.source)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest["summary"], ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
