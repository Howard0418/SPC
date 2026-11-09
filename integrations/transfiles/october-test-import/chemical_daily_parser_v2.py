"""依表頭唯讀解析藥液日報；不呼叫API、不寫資料庫。"""
from __future__ import annotations

import re
from datetime import date, datetime
from pathlib import Path
from typing import Any

import openpyxl
from openpyxl.utils import get_column_letter


PLACEHOLDERS = {"", "-", "－", "—", ".", "。"}
F_REFERENCE = re.compile(r"(?:'?(?:F值|F)'?)!\$?([A-Z]{1,3})\$?(\d+)", re.IGNORECASE)


def _text(value: Any) -> str:
    return "" if value is None else str(value).strip()


def _normal(value: Any) -> str:
    return re.sub(r"[\s　]", "", _text(value)).upper()


def _number(value: Any) -> int | float | None:
    if isinstance(value, bool):
        return None
    return value if isinstance(value, (int, float)) else None


def _is_placeholder(value: Any) -> bool:
    return _text(value) in PLACEHOLDERS


def _line_from_sheet(title: str) -> str:
    cleaned = re.sub(r"[（(](?:開|收|OPEN|CLOSE)[）)]", "", title, flags=re.IGNORECASE)
    return _normal(cleaned)


def _stage(title: str, line: str) -> str | None:
    if line not in {"N1", "N2"}:
        return "GENERAL"
    normalized = _normal(title)
    if "開" in title or "OPEN" in normalized:
        return "OPEN"
    if "收" in title or "CLOSE" in normalized:
        return "CLOSE"
    return None


def _section_line(value: Any, sheet_line: str) -> str:
    """舊線／新線工作表內含 PT、DV、ST、QE 四條實際線別，依 A 欄區段標題解析。"""
    if sheet_line not in {"舊線", "新線"}:
        return sheet_line
    match = re.search(r"[（(]\s*(PT|DV|ST|QE)\s*(\d?)\s*[）)]", _text(value), re.IGNORECASE)
    if not match:
        return ""
    suffix = match.group(2) or ("1" if sheet_line == "舊線" else "2")
    return match.group(1).upper() + suffix


def _date_value(value: Any) -> str | None:
    if isinstance(value, datetime):
        return value.date().isoformat()
    if isinstance(value, date):
        return value.isoformat()
    text = _text(value)
    for fmt in ("%Y/%m/%d", "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt).date().isoformat()
        except ValueError:
            pass
    return None


def _header_contract(ws: Any) -> tuple[int, int, list[int], list[int], int | None, int | None] | None:
    for row_number in range(1, min(ws.max_row, 20) + 1):
        values = [_text(ws.cell(row_number, column).value) for column in range(1, min(ws.max_column, 40) + 1)]
        item_columns = [index + 1 for index, value in enumerate(values) if "分析項目" in value]
        titration_columns = [index + 1 for index, value in enumerate(values) if "滴定" in value]
        concentration_columns = [index + 1 for index, value in enumerate(values) if "濃度" in value]
        if item_columns and len(titration_columns) >= 2 and len(concentration_columns) >= 2:
            adjustment = next((index + 1 for index, value in enumerate(values) if _normal(value) == "調整"), None)
            amount = next((index + 1 for index, value in enumerate(values) if _normal(value) == "調整量"), None)
            return row_number, item_columns[0], titration_columns[:2], concentration_columns[:2], adjustment, amount
    return None


def _f_references(formula: Any) -> list[str]:
    return sorted({f"F!{match.group(1).upper()}{match.group(2)}" for match in F_REFERENCE.finditer(_text(formula))})


def _phase(formula_ws: Any, value_ws: Any, row: int, titration_col: int, concentration_col: int) -> tuple[dict[str, Any] | None, list[dict[str, str]]]:
    titration = formula_ws.cell(row, titration_col).value
    formula_or_value = formula_ws.cell(row, concentration_col).value
    cached_concentration = value_ws.cell(row, concentration_col).value
    concentration = _number(formula_or_value) if _number(formula_or_value) is not None else _number(cached_concentration)
    titration_number = _number(titration)
    has_titration = titration_number is not None
    has_concentration = concentration is not None
    formula = formula_or_value if isinstance(formula_or_value, str) and formula_or_value.startswith("=") else None
    secondary_col = titration_col + 1 if concentration_col == titration_col + 2 else None
    secondary_cell = f"{get_column_letter(secondary_col)}{row}" if secondary_col else None
    secondary = _number(formula_ws.cell(row, secondary_col).value) if secondary_col and formula and re.search(rf"(?<![A-Z0-9_])\$?{get_column_letter(secondary_col)}\$?{row}(?!\d)", formula, re.IGNORECASE) else None
    if not has_titration and not has_concentration:
        return None, []

    titration_cell = f"{get_column_letter(titration_col)}{row}"
    concentration_cell = f"{get_column_letter(concentration_col)}{row}"
    errors: list[dict[str, str]] = []
    if has_titration:
        mode = "FORMULA"
        if not formula:
            errors.append({"code": "FORMULA_MISSING", "cell": concentration_cell, "reason": "滴定數有值但濃度公式缺少"})
        elif not re.search(rf"(?<![A-Z0-9_])\$?{get_column_letter(titration_col)}\$?{row}(?!\d)", formula, re.IGNORECASE):
            errors.append({"code": "FORMULA_TITRATION_REFERENCE_MISMATCH", "cell": concentration_cell, "reason": "濃度公式未引用同列滴定數"})
    else:
        mode = "DIRECT"
        formula = None

    references = _f_references(formula)
    return {
        "mode": mode,
        "titration": titration_number,
        "secondary": secondary,
        "concentration": concentration,
        "formula": formula,
        "fReferences": references,
        "sourceCells": {"titration": titration_cell, "secondary": secondary_cell if secondary is not None else None, "concentration": concentration_cell},
    }, errors


def parse_report(path: Path, source: dict[str, Any]) -> dict[str, Any]:
    path = Path(path)
    formula_wb = openpyxl.load_workbook(path, read_only=True, data_only=False)
    value_wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    rows: list[dict[str, Any]] = []
    sheet_errors: list[dict[str, Any]] = []
    try:
        f_values = {}
        f_sheet = next((name for name in value_wb.sheetnames if _normal(name) in {"F","F值"}), None)
        if f_sheet:
            for row in value_wb[f_sheet].iter_rows():
                for cell in row:
                    if _number(cell.value) is not None:
                        f_values[f"F!{cell.coordinate}"] = cell.value
        for title in formula_wb.sheetnames:
            if _normal(title) in {"F", "F值"}:
                continue
            formula_ws, value_ws = formula_wb[title], value_wb[title]
            line = _line_from_sheet(title)
            internal_line = _normal(value_ws["B3"].value)
            if internal_line and internal_line != line:
                sheet_errors.append({"sheet": title, "code": "LINE_MISMATCH", "cell": "B3", "reason": f"Sheet線別{line}與表內線別{internal_line}不一致"})
                continue
            sampling_stage = _stage(title, line)
            if sampling_stage is None:
                sheet_errors.append({"sheet": title, "code": "SAMPLING_STAGE_MISSING", "reason": "N1／N2工作表名稱必須標示開或收"})
                continue
            internal_date = _date_value(value_ws["B4"].value)
            if internal_date and internal_date != source["reportDate"]:
                sheet_errors.append({"sheet": title, "code": "REPORT_DATE_MISMATCH", "cell": "B4", "reason": f"檔名日期{source['reportDate']}與表內日期{internal_date}不一致"})
                continue
            contract = _header_contract(formula_ws)
            if contract is None:
                sheet_errors.append({"sheet": title, "code": "HEADER_CONTRACT_MISSING", "reason": "找不到兩組滴定數與濃度表頭"})
                continue
            header_row, item_col, titration_cols, concentration_cols, adjustment_col, amount_col = contract
            slot = ""
            section_line = ""
            for row_number in range(header_row + 1, formula_ws.max_row + 1):
                current_section = _text(value_ws.cell(row_number, 1).value)
                if current_section:
                    parsed_section = _section_line(current_section, line)
                    if parsed_section:
                        section_line = parsed_section
                current_slot = _text(value_ws.cell(row_number, 2).value)
                if current_slot:
                    slot = current_slot
                analysis_item = _text(value_ws.cell(row_number, item_col).value)
                initial, initial_errors = _phase(formula_ws, value_ws, row_number, titration_cols[0], concentration_cols[0])
                recheck, recheck_errors = _phase(formula_ws, value_ws, row_number, titration_cols[1], concentration_cols[1])
                if initial is None and recheck is None:
                    continue
                errors = initial_errors + recheck_errors
                if not analysis_item:
                    errors.append({"code": "ANALYSIS_ITEM_MISSING", "cell": f"{get_column_letter(item_col)}{row_number}", "reason": "有量測值但分析項目空白"})
                if not slot:
                    errors.append({"code": "SLOT_MISSING", "cell": f"B{row_number}", "reason": "有量測值但槽位空白"})
                row_line = section_line if line in {"舊線", "新線"} else line
                if not row_line:
                    errors.append({"code": "SECTION_LINE_MISSING", "cell": f"A{row_number}", "reason": "舊線／新線資料缺少PT／DV／ST／QE區段線別"})
                def raw_formula(column: int | None) -> str | None:
                    value = formula_ws.cell(row_number, column).value if column else None
                    return value if isinstance(value, str) and value.startswith("=") else None
                context = {f"{get_column_letter(column)}{row_number}": formula_ws.cell(row_number, column).value
                           for column in range(1, formula_ws.max_column + 1)
                           if formula_ws.cell(row_number, column).value is not None}
                rows.append({
                    "sourceFile": source["relativePath"], "sourceHash": source["sha256"], "sheet": title, "sourceRow": row_number,
                    "reportDate": source["reportDate"], "shift": source["shift"], "reportDateTime": source["reportDateTime"],
                    "line": row_line, "samplingStage": sampling_stage, "slot": slot, "analysisItem": analysis_item,
                    "adjustmentFormulaRaw": raw_formula(adjustment_col), "adjustmentAmountFormulaRaw": raw_formula(amount_col),
                    "formulaContext": context,
                    "initial": initial, "recheck": recheck, "status": "ERROR" if errors else "CANDIDATE", "errors": errors,
                })
        return {"source": source, "fValues": f_values, "rows": rows, "sheetErrors": sheet_errors,
                "summary": {"candidates": sum(row["status"] == "CANDIDATE" for row in rows), "errors": sum(row["status"] == "ERROR" for row in rows) + len(sheet_errors), "skippedSheets": len(sheet_errors)}}
    finally:
        formula_wb.close(); value_wb.close()
