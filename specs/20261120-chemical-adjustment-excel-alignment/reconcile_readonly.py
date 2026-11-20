"""Read an Excel chemical daily report and compare it with active PPC rows from stdin JSON.

The caller is responsible for querying the guarded test database and piping an array with
PPC/machine/tank/characteristic/config fields. This script performs no database or workbook writes.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import unicodedata
from collections import Counter, defaultdict
from decimal import Decimal, InvalidOperation

from openpyxl import load_workbook
from openpyxl.utils import get_column_letter


def text(value: object) -> str:
    return "" if value is None else str(value).strip()


def normalized(value: object) -> str:
    return unicodedata.normalize("NFKC", text(value))


def item_key(value: object) -> str:
    first_line = normalized(value).split("\n")[0].strip()
    without_unit = re.sub(r"\s*\([^)]*\)\s*$", "", first_line)
    return re.sub(r"[\s　]+", "", without_unit).upper().replace("‧", ".").replace("．", ".")


def tank_key(value: object) -> str:
    return re.sub(r"[\s　]+", "", normalized(value)).upper()


def machine_code(line: str, sheet: str) -> str:
    if sheet.startswith("N1"):
        return "N1"
    if sheet.startswith("N2"):
        return "N2"
    if sheet in {"CN", "C1", "C3", "C5", "DP"}:
        return sheet
    match = re.search(r"\(([^()]+)\)", normalized(line))
    code = match.group(1).upper() if match else ""
    if sheet == "舊線":
        return {"PT": "PT1", "DV": "DV1", "ST": "ST1", "QE": "QE1"}.get(code, code)
    return code


def decimal_value(value: object) -> Decimal | None:
    if value is None or text(value) == "" or text(value).upper() in {"NA", "N/A", "-"}:
        return None
    try:
        return Decimal(text(value))
    except InvalidOperation:
        return None


def decimal_text(value: object) -> str | None:
    parsed = decimal_value(value)
    return None if parsed is None else format(parsed.normalize(), "f")


def value_kind(value: object) -> str:
    if value is None or text(value) == "":
        return "blank"
    if text(value).upper() in {"NA", "N/A", "-"}:
        return "NA"
    return "value" if decimal_value(value) is not None else "text"


def parse_config(row: dict) -> dict:
    try:
        return json.loads(row.get("ChemicalAnalysisConfigJson") or "{}")
    except (TypeError, json.JSONDecodeError):
        return {}


def replace_references(formula: object, row_number: int, references: dict[str, str]) -> str:
    if not formula:
        return ""
    result = normalized(formula).lstrip("=")
    for column, value in sorted(references.items(), key=lambda item: -len(item[0])):
        pattern = rf"\$?{re.escape(column)}\$?{row_number}(?!\d)"
        result = re.sub(pattern, value, result, flags=re.IGNORECASE)
    return result


def formula_normalized(formula: object) -> str:
    result = normalized(formula).lstrip("=")
    for old, new in {"：": ":", "，": ",", "；": ";", "‧": ".", "．": "."}.items():
        result = result.replace(old, new)
    return re.sub(r"\s+", "", result).upper()


def formula_structure(formula: object) -> str:
    return re.sub(r'"(?:""|[^"])*"', '""', formula_normalized(formula))


def formula_class(source: object, website: object) -> str:
    source_normalized = formula_normalized(source)
    website_normalized = formula_normalized(website)
    if not source_normalized and not website_normalized:
        return "both_blank"
    if source_normalized and not website_normalized:
        return "website_missing"
    if not source_normalized and website_normalized:
        return "source_missing"
    if source_normalized == website_normalized:
        return "exact"
    if formula_structure(source) == formula_structure(website):
        return "structure_same_text_diff"
    return "different"


def read_source(path: str) -> list[dict]:
    workbook = load_workbook(path, data_only=False, read_only=True)
    rows: list[dict] = []
    for worksheet in workbook.worksheets:
        if worksheet.title == "F":
            continue
        headers = {
            normalized(worksheet.cell(6, column).value): column
            for column in range(1, worksheet.max_column + 1)
            if worksheet.cell(6, column).value is not None
        }
        volume_column = headers.get("槽容(L)", headers.get("槽容"))
        if not volume_column:
            continue
        concentration_column = min(
            column
            for column in range(1, worksheet.max_column + 1)
            if normalized(worksheet.cell(6, column).value) == "濃度"
        )
        adjustment_column = headers["調整"]
        amount_column = headers["調整量"]
        addition_column = headers["添加管制值"]
        dilution_column = headers["稀釋管制值"]
        line = tank = ""
        for row_number in range(7, worksheet.max_row + 1):
            if worksheet.cell(row_number, 1).value is not None:
                line = normalized(worksheet.cell(row_number, 1).value)
            if worksheet.cell(row_number, 2).value is not None:
                tank = normalized(worksheet.cell(row_number, 2).value)
            item = worksheet.cell(row_number, 3).value
            if item is None:
                continue
            values = [
                worksheet.cell(row_number, column).value
                for column in (adjustment_column, amount_column, volume_column, addition_column, dilution_column)
            ]
            if not any(value is not None for value in values):
                continue
            volume, addition, dilution = values[2], values[3], values[4]
            reference_values = {}
            for column, value, token in (
                (concentration_column, None, "Concentration"),
                (volume_column, volume, "BLANK"),
                (addition_column, addition, "BLANK"),
                (dilution_column, dilution, "BLANK"),
            ):
                replacement = token if column == concentration_column else (
                    decimal_text(value) or ("NA" if value_kind(value) == "NA" else "BLANK")
                )
                reference_values[get_column_letter(column)] = replacement
            machine = machine_code(line, worksheet.title)
            rows.append(
                {
                    "sheet": worksheet.title,
                    "row": row_number,
                    "machine": machine,
                    "tank": tank,
                    "item": normalized(item),
                    "key": (machine, tank_key(tank), item_key(item)),
                    "volume": volume,
                    "add": addition,
                    "dilute": dilution,
                    "adjust": replace_references(values[0], row_number, reference_values),
                    "amount": replace_references(values[1], row_number, reference_values),
                }
            )
    return rows


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("workbook")
    parser.add_argument("--matrix-output", help="Write the human-readable per-identity matrix as Markdown.")
    args = parser.parse_args()
    database_rows = json.load(sys.stdin)
    if isinstance(database_rows, dict):
        database_rows = [database_rows]
    for database_row in database_rows:
        database_row["_config"] = parse_config(database_row)

    source_rows = read_source(args.workbook)
    source_by_key: defaultdict[tuple, list[dict]] = defaultdict(list)
    for row in source_rows:
        source_by_key[row["key"]].append(row)

    identities: list[dict] = []
    source_conflicts: list[dict] = []
    for key, rows in source_by_key.items():
        signatures = {
            (
                decimal_text(row["volume"]), value_kind(row["volume"]),
                decimal_text(row["add"]), value_kind(row["add"]),
                decimal_text(row["dilute"]), value_kind(row["dilute"]),
                formula_normalized(row["adjust"]), formula_normalized(row["amount"]),
            )
            for row in rows
        }
        if len(signatures) > 1:
            source_conflicts.append({"key": key, "references": [f'{row["sheet"]}!{row["row"]}' for row in rows]})
        identity = rows[0].copy()
        identity["references"] = [f'{row["sheet"]}!{row["row"]}' for row in rows]
        identity["variantCount"] = len(signatures)
        identities.append(identity)

    database_by_key: defaultdict[tuple, list[dict]] = defaultdict(list)
    for row in database_rows:
        key = (text(row["MachineCode"]).upper(), tank_key(row["TankName"]), item_key(row["CharacteristicName"]))
        database_by_key[key].append(row)

    field_counts = {field: Counter() for field in ("volume", "add", "dilute")}
    formula_counts = {field: Counter() for field in ("adjust", "amount")}
    matched: list[dict] = []
    unmatched: list[dict] = []
    ambiguous: list[dict] = []
    for source in identities:
        candidates = database_by_key.get(source["key"], [])
        if not candidates:
            unmatched.append(source)
            continue
        if len(candidates) > 1:
            ambiguous.append(source)
            continue
        database = candidates[0]
        config = database["_config"]
        comparison = {"source": source, "database": database, "fields": {}, "formulas": {}}
        for source_field, config_field in (
            ("volume", "tankCapacityLiters"),
            ("add", "additionControlValue"),
            ("dilute", "dilutionControlValue"),
        ):
            source_kind = value_kind(source[source_field])
            source_value = decimal_text(source[source_field])
            website_value = decimal_text(config.get(config_field))
            if source_kind == "NA":
                category = "source_NA"
            elif source_kind == "blank":
                category = "source_blank"
            elif website_value is None:
                category = "website_missing"
            elif source_value == website_value:
                category = "exact"
            else:
                category = "different"
            field_counts[source_field][category] += 1
            comparison["fields"][source_field] = (category, source_value, website_value)
        for source_field, config_field in (("adjust", "adjustmentFormula"), ("amount", "adjustmentAmountFormula")):
            source_formula = source[source_field]
            website_formula = config.get(config_field) or ""
            category = formula_class(source_formula, website_formula)
            formula_counts[source_field][category] += 1
            comparison["formulas"][source_field] = (category, source_formula, website_formula)
        matched.append(comparison)

    def source_label(row: dict) -> dict:
        return {
            "references": row["references"],
            "machine": row["machine"],
            "tank": row["tank"],
            "item": row["item"].split("\n")[0],
        }

    def field_differences(field: str) -> list[dict]:
        result = []
        for comparison in matched:
            category, source_value, website_value = comparison["fields"][field]
            if category == "exact":
                continue
            result.append(
                {
                    "ppcId": comparison["database"]["Id"],
                    **source_label(comparison["source"]),
                    "source": source_value or value_kind(comparison["source"][field]),
                    "website": website_value,
                    "class": category,
                }
            )
        return result

    def formula_differences(field: str) -> list[dict]:
        result = []
        for comparison in matched:
            category, source_formula, website_formula = comparison["formulas"][field]
            if category in {"exact", "both_blank", "structure_same_text_diff"}:
                continue
            result.append(
                {
                    "ppcId": comparison["database"]["Id"],
                    **source_label(comparison["source"]),
                    "class": category,
                    "sourceFormula": source_formula,
                    "websiteFormula": website_formula,
                }
            )
        return result

    matched_ids = {comparison["database"]["Id"] for comparison in matched}
    result = {
        "sourceRows": len(source_rows),
        "sourceIdentities": len(identities),
        "sourceDuplicateRows": len(source_rows) - len(identities),
        "sourceConflicts": source_conflicts,
        "activeWebsitePpcs": len(database_rows),
        "matchedIdentities": len(matched),
        "unmatchedSource": [source_label(row) for row in unmatched],
        "ambiguousSource": [source_label(row) for row in ambiguous],
        "websiteOnly": [
            {"ppcId": row["Id"], "machine": row["MachineCode"], "tank": row["TankName"], "item": row["CharacteristicName"]}
            for row in database_rows
            if row["Id"] not in matched_ids
        ],
        "fieldCounts": {field: dict(counts) for field, counts in field_counts.items()},
        "formulaCounts": {field: dict(counts) for field, counts in formula_counts.items()},
        "fieldDifferences": {field: field_differences(field) for field in field_counts},
        "formulaDifferences": {field: formula_differences(field) for field in formula_counts},
    }
    if args.matrix_output:
        labels = {
            "exact": "相符",
            "source_NA": "來源NA",
            "source_blank": "來源空白",
            "website_missing": "網站缺值",
            "source_missing": "來源缺值",
            "both_blank": "皆空白",
            "structure_same_text_diff": "結構相符／文字不同",
            "different": "不同",
        }

        def field_label(comparison: dict, field: str) -> str:
            category, source_value, website_value = comparison["fields"][field]
            if category == "different":
                return f"{source_value}≠{website_value}"
            if category in {"source_NA", "source_blank"} and website_value is not None:
                return f"{labels[category]}／網站{website_value}"
            return labels[category]

        lines = [
            "# Excel／測試網站逐列對帳矩陣",
            "",
            f"- 來源列：{len(source_rows)}；合併開／收線同值後：{len(identities)} 個來源身分。",
            f"- 唯一配對：{len(matched)}；來源未配對：{len(unmatched)}；網站額外啟用項目：{len(database_rows) - len(matched_ids)}。",
            "- 公式狀態是在把 Excel 本列濃度、槽容、添加及稀釋儲存格參照換成實值後比較；「不同」仍須人工判讀目標值、倍率、NA 規則與文字差異。",
            "",
            "| Excel 位置 | PPC | 線別 | 槽別 | 分析項目 | 槽容 | 添加 | 稀釋 | 調整公式 | 調整量公式 |",
            "|---|---:|---|---|---|---|---|---|---|---|",
        ]
        for comparison in sorted(matched, key=lambda item: (item["source"]["machine"], item["source"]["references"][0])):
            source = comparison["source"]
            database = comparison["database"]
            adjust_category = comparison["formulas"]["adjust"][0]
            amount_category = comparison["formulas"]["amount"][0]
            lines.append(
                "| {refs} | {ppc} | {machine} | {tank} | {item} | {volume} | {addition} | {dilution} | {adjust} | {amount} |".format(
                    refs="、".join(source["references"]),
                    ppc=database["Id"],
                    machine=source["machine"],
                    tank=source["tank"].replace("|", "\\|"),
                    item=source["item"].split("\n")[0].replace("|", "\\|"),
                    volume=field_label(comparison, "volume"),
                    addition=field_label(comparison, "add"),
                    dilution=field_label(comparison, "dilute"),
                    adjust=labels[adjust_category],
                    amount=labels[amount_category],
                )
            )
        for source in unmatched:
            lines.append(
                f'| {"、".join(source["references"])} | — | {source["machine"]} | {source["tank"]} | {source["item"].split(chr(10))[0]} | 未配對 | 未配對 | 未配對 | 未配對 | 未配對 |'
            )
        lines.extend(
            [
                "",
                "## 網站額外啟用項目",
                "",
                "| PPC | 線別 | 槽別 | 分析項目 |",
                "|---:|---|---|---|",
            ]
        )
        for database in database_rows:
            if database["Id"] not in matched_ids:
                lines.append(f'| {database["Id"]} | {database["MachineCode"]} | {database["TankName"]} | {database["CharacteristicName"]} |')
        with open(args.matrix_output, "w", encoding="utf-8", newline="\n") as output:
            output.write("\n".join(lines) + "\n")
    print(json.dumps(result, ensure_ascii=False, indent=2, default=str))


if __name__ == "__main__":
    main()
