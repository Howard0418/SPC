"""只讀解析藥液日報 Excel 的欄位契約。

本模組只產生可供 dry-run/preview 使用的資料，不會呼叫 API 或寫入資料庫。
"""
from __future__ import annotations

from datetime import date, datetime
from decimal import Decimal, InvalidOperation
from pathlib import Path
import re
from typing import Any

import openpyxl
from openpyxl.utils import get_column_letter


CELL_REFERENCE = re.compile(r"(?<![A-Z0-9_])(?:(?:'?(?P<sheet>[^'!]+)'?)!)?\$?(?P<column>[A-Z]{1,3})\$?(?P<row>\d+)", re.IGNORECASE)


ALIASES = {
    "measurement_datetime": {"量測日期時間", "量測日期", "日期時間", "日期", "測定日期"},
    "line": {"線別", "機台", "製程", "線別/機台"},
    "slot": {"槽位", "槽別", "位置"},
    "analysis_item": {"分析項目", "分析項目名稱", "項目"},
    "value": {"量測值", "測定值", "數值", "濃度"},
    "spec": {"規格", "規格值"},
    "lower": {"下限", "規格下限", "LSL"},
    "upper": {"上限", "規格上限", "USL"},
    "formula": {"公式", "計算公式", "管制公式"},
    "f_value": {"F值", "F 值", "F值（係數）", "F係數", "F factor"},
}


def _text(value: Any) -> str:
    return "" if value is None else str(value).strip()


def _normalise(value: Any) -> str:
    return _text(value).replace(" ", "").replace("　", "").lower()


def _parse_datetime(value: Any) -> str | None:
    if isinstance(value, datetime):
        return value.isoformat(timespec="seconds")
    if isinstance(value, date):
        return datetime.combine(value, datetime.min.time()).isoformat(timespec="seconds")
    text = _text(value)
    if not text:
        return None
    for fmt in ("%Y/%m/%d %H:%M:%S", "%Y/%m/%d %H:%M", "%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%Y/%m/%d", "%Y-%m-%d"):
        try:
            return datetime.strptime(text, fmt).isoformat(timespec="seconds")
        except ValueError:
            continue
    return None


def _date_from_filename(path: Path) -> str | None:
    """Use only an explicit ISO date in the source file name; never infer from mtime."""
    match = re.search(r"(?<!\d)(20\d{2})-(\d{1,2})-(\d{1,2})(?!\d)", path.stem)
    if not match:
        return None
    try:
        return datetime(int(match.group(1)), int(match.group(2)), int(match.group(3))).isoformat(timespec="seconds")
    except ValueError:
        return None


def _header_map(values: list[Any]) -> dict[str, int]:
    result: dict[str, int] = {}
    normalised = {_normalise(alias): key for key, aliases in ALIASES.items() for alias in aliases}
    for index, value in enumerate(values):
        key = normalised.get(_normalise(value))
        if key and key not in result:
            result[key] = index
    return result


def _matrix_sheet(formula_ws: Any) -> bool:
    """辨識既有藥液日報的矩陣格式（第 6 列欄位、第 4 列日期）。"""
    values = next(formula_ws.iter_rows(min_row=6, max_row=6, max_col=14, values_only=True), ())
    headers = [_normalise(value) for value in values]
    return "分析項目" in headers and any("滴定" in header for header in headers)


def _f_value_from_formula(formula: Any, value_wb: Any, cached_values: dict[str, Any] | None = None) -> Any:
    """從 Excel 公式明確引用的 F 表儲存格取回快取值，不猜測 F 值。"""
    match = re.search(r"(?:'F'|F)!\$?([A-Z]{1,3})\$?(\d+)", _text(formula), re.IGNORECASE)
    if not match or "F" not in value_wb.sheetnames:
        return None
    coordinate = f"{match.group(1).upper()}{match.group(2)}"
    return cached_values.get(coordinate) if cached_values is not None else value_wb["F"][coordinate].value


def _formula_number(value: Any) -> str:
    if isinstance(value, bool) or value is None:
        raise ValueError("參照格不是數值")
    try:
        number = Decimal(str(value))
    except InvalidOperation:
        raise ValueError("參照格不是數值") from None
    if not number.is_finite():
        raise ValueError("參照格不是有限數值")
    return format(number, "f").rstrip("0").rstrip(".") if "." in format(number, "f") else format(number, "f")


def _normalise_adjustment_formula(formula: Any, row_number: int, concentration_cell: str, context: dict[str, Any]) -> str | None:
    if not isinstance(formula, str) or not formula.startswith("="):
        return None
    upper_context = {key.upper(): value for key, value in context.items()}
    na_cells = {key for key, value in upper_context.items() if _text(value).upper() in {"NA", "N/A"}}

    def split_args(body: str) -> list[str]:
        result: list[str] = []
        start = depth = 0
        quoted = False
        index = 0
        while index < len(body):
            char = body[index]
            if char == '"':
                if quoted and index + 1 < len(body) and body[index + 1] == '"':
                    index += 2
                    continue
                quoted = not quoted
            elif not quoted:
                if char == "(": depth += 1
                elif char == ")": depth -= 1
                elif char == "," and depth == 0:
                    result.append(body[start:index]); start = index + 1
            index += 1
        result.append(body[start:])
        return result

    def simplify(expression: str) -> str:
        stripped = expression.strip()
        upper = stripped.upper()
        for name, count in (("IF", 3), ("IFERROR", 2)):
            prefix = name + "("
            if upper.startswith(prefix) and stripped.endswith(")"):
                args = split_args(stripped[len(prefix):-1])
                if len(args) != count:
                    raise ValueError(f"{name}參數數量不正確")
                if name == "IF" and any(re.search(rf"(?<![A-Z0-9_])\$?{re.escape(cell[:-len(str(row_number))])}\$?{row_number}(?!\d)", args[0], re.I) for cell in na_cells):
                    return simplify(args[2])
                simplified = [simplify(arg) for arg in args]
                if name == "IF" and simplified[1] == simplified[2]:
                    return simplified[1]
                return name + "(" + ",".join(simplified) + ")"
        return stripped

    simplified = simplify(formula[1:])
    if simplified in {'""', ""}:
        return None
    pieces = re.split(r'("(?:""|[^"])*")', simplified)
    for index in range(0, len(pieces), 2):
        def replace(match: re.Match[str]) -> str:
            sheet = (match.group("sheet") or "").upper()
            address = f'{match.group("column").upper()}{match.group("row")}'
            if sheet in {"F", "F值"}:
                return f"F!{address}"
            if sheet:
                raise ValueError(f"不允許跨表參照：{match.group(0)}")
            if address == concentration_cell.upper():
                return "Concentration"
            if address in upper_context:
                if address in na_cells:
                    raise ValueError(f"NA格位未從條件移除：{address}")
                return _formula_number(upper_context[address])
            raise ValueError(f"無法證明的格位參照：{address}")
        pieces[index] = CELL_REFERENCE.sub(replace, pieces[index])
    return "".join(pieces)


def _parse_matrix_sheet(formula_ws: Any, value_ws: Any, value_wb: Any, f_values: dict[str, Any] | None = None, filename_date: str | None = None) -> list[dict[str, Any]]:
    measurement_datetime = _parse_datetime(value_ws["B4"].value) or filename_date
    sheet_line = _text(value_ws["B3"].value) or formula_ws.title
    line = sheet_line
    line_source_cell = "B3"
    rows: list[dict[str, Any]] = []
    slot = ""
    headers = {_normalise(formula_ws.cell(6, column).value): column for column in range(1, formula_ws.max_column + 1)}
    adjustment_col = headers.get("調整")
    adjustment_amount_col = headers.get("調整量") or headers.get("添加量")
    formula_rows = formula_ws.iter_rows(min_row=7, max_col=formula_ws.max_column, values_only=True)
    value_rows = value_ws.iter_rows(min_row=7, max_col=14, values_only=True)
    for row_number, (formulas, values) in enumerate(zip(formula_rows, value_rows), 7):
        # Merged group headers can be on a row without measurements. Read their
        # context before skipping that row, and never carry a tank across lines.
        group_label = _text(values[0])
        group = re.search(r"[（(]\s*([A-Za-z][A-Za-z0-9-]*)\s*[）)]", group_label)
        group_code = group.group(1).upper() if group else (
            group_label.upper() if re.fullmatch(r"[A-Za-z][A-Za-z0-9-]*", group_label) else None)
        if group_code:
            if group_code != line:
                slot = ""
            line = group_code
            line_source_cell = f"A{row_number}"
        current_slot = _text(values[1])
        if current_slot:
            slot = current_slot
        analysis_item = _text(values[2])
        raw_value = values[7]
        recheck_value = values[12]
        # A dash is the established daily-report placeholder for "not
        # measured". It is not a zero and must not become a repair item or an
        # import candidate.
        if _text(raw_value) in {"-", "－", "—", ".", "。"} and not isinstance(recheck_value, (int, float)):
            continue
        # Daily-report tabs place the calculated analysis result in different
        # columns. Select only a formula that consumes this row's titration
        # value, never a later status/advice formula.
        raw_reference = re.compile(rf"(?<![A-Z0-9_])H\$?{row_number}(?!\d)", re.IGNORECASE)
        calculated = [
            (index, candidate, values[index])
            for index, candidate in enumerate(formulas)
            if isinstance(candidate, str) and candidate.startswith("=") and raw_reference.search(candidate)
        ]
        primary = next((item for item in calculated if item[0] < 12), (None, None, None))
        recheck = next((item for item in calculated if item[0] >= 12), (None, None, None))
        _, formula, formula_result = primary
        _, recheck_formula, recheck_formula_result = recheck
        has_measurement_definition = any((formula, recheck_formula, values[3], values[4], values[6]))
        if not analysis_item or (raw_value is None and recheck_value is None) or not has_measurement_definition:
            continue
        formula_context = {
            f"{get_column_letter(column)}{row_number}": value
            for column, value in enumerate(formulas, 1) if value is not None
        }
        concentration_cell = f"{get_column_letter(primary[0] + 1)}{row_number}" if primary[0] is not None else f"I{row_number}"
        adjustment_error = adjustment_amount_error = None
        try:
            adjustment_formula = _normalise_adjustment_formula(
                formulas[adjustment_col - 1] if adjustment_col and adjustment_col <= len(formulas) else None,
                row_number, concentration_cell, formula_context)
        except ValueError as exc:
            adjustment_formula = None; adjustment_error = str(exc)
        try:
            adjustment_amount_formula = _normalise_adjustment_formula(
                formulas[adjustment_amount_col - 1] if adjustment_amount_col and adjustment_amount_col <= len(formulas) else None,
                row_number, concentration_cell, formula_context)
        except ValueError as exc:
            adjustment_amount_formula = None; adjustment_amount_error = str(exc)
        item = {
            "sheet": formula_ws.title,
            "sourceRow": row_number,
            "measurementDatetime": measurement_datetime,
            "line": line,
            "sheetLine": sheet_line,
            "lineSourceCell": line_source_cell,
            "slot": slot,
            "analysisItem": analysis_item,
            "value": raw_value,
            "formula": formula,
            "formulaResult": formula_result,
            "fValue": _f_value_from_formula(formula, value_wb, f_values),
            "spec": values[3],
            "lower": values[4],
            "upper": values[6],
            "recheckValue": recheck_value,
            "recheckFormula": recheck_formula,
            "recheckFormulaResult": recheck_formula_result,
            "adjustmentFormula": adjustment_formula,
            "adjustmentAmountFormula": adjustment_amount_formula,
            "sourceFormat": "matrix-daily-report-v1",
        }
        missing = []
        if not measurement_datetime: missing.append("量測日期時間")
        if not slot: missing.append("槽位")
        if raw_value is None: missing.append("量測值")
        if not item["formula"]: missing.append("公式")
        if item["formulaResult"] is None: missing.append("公式計算結果")
        if formula and re.search(r"(?:'F'|F)!\$?[A-Z]{1,3}\$?\d+", formula, re.IGNORECASE) and item["fValue"] is None:
            missing.append("F值")
        if adjustment_error: missing.append("調整公式：" + adjustment_error)
        if adjustment_amount_error: missing.append("調整量公式：" + adjustment_amount_error)
        item["status"] = "manual_review" if missing else "ready_for_preview"
        item["reason"] = "缺少：" + "、".join(missing) if missing else "矩陣式藥液日報欄位契約檢核通過"
        rows.append(item)
    return rows


def parse_chemical_daily_report(path: Path) -> list[dict[str, Any]]:
    """解析所有工作表；格式問題以列級錯誤回傳，不抛出部分資料。"""
    path = Path(path)
    formula_wb = openpyxl.load_workbook(path, data_only=False, read_only=True)
    value_wb = openpyxl.load_workbook(path, data_only=True, read_only=True)
    rows: list[dict[str, Any]] = []
    try:
        f_values = {cell.coordinate: cell.value for row in value_wb["F"].iter_rows() for cell in row if cell.value is not None} if "F" in value_wb.sheetnames else None
        filename_date = _date_from_filename(path)
        for sheet_name in formula_wb.sheetnames:
            formula_ws = formula_wb[sheet_name]
            value_ws = value_wb[sheet_name]
            if sheet_name.upper() == "F":
                continue
            if _matrix_sheet(formula_ws):
                rows.extend(_parse_matrix_sheet(formula_ws, value_ws, value_wb, f_values, filename_date))
                continue
            header_row = None
            headers: dict[str, int] = {}
            for row_number, values in enumerate(formula_ws.iter_rows(min_row=1, max_row=30, values_only=True), 1):
                candidate = _header_map(list(values))
                if "measurement_datetime" in candidate and ("value" in candidate or "analysis_item" in candidate):
                    header_row, headers = row_number, candidate
                    break
            if header_row is None:
                rows.append({"sheet": sheet_name, "status": "invalid_format", "reason": "找不到日期與量測欄位標題"})
                continue
            for row_number, formula_values in enumerate(formula_ws.iter_rows(min_row=header_row + 1, values_only=True), header_row + 1):
                value_values = next(value_ws.iter_rows(min_row=row_number, max_row=row_number, values_only=True))
                if not any(_text(v) for v in formula_values):
                    continue
                def get(key: str, values: tuple[Any, ...] | list[Any] = formula_values) -> Any:
                    index = headers.get(key)
                    return values[index] if index is not None and index < len(values) else None
                measurement_datetime = _parse_datetime(get("measurement_datetime"))
                item = {
                    "sheet": sheet_name,
                    "sourceRow": row_number,
                    "measurementDatetime": measurement_datetime,
                    "line": _text(get("line")),
                    "slot": _text(get("slot")),
                    "analysisItem": _text(get("analysis_item")),
                    "value": get("value", value_values),
                    "formula": get("formula"),
                    "formulaResult": get("formula", value_values),
                    "fValue": get("f_value", value_values),
                    "spec": get("spec", value_values),
                    "lower": get("lower", value_values),
                    "upper": get("upper", value_values),
                }
                missing = []
                if not measurement_datetime: missing.append("量測日期時間")
                if not item["analysisItem"]: missing.append("分析項目")
                if item["value"] is None: missing.append("量測值")
                if not item["formula"]: missing.append("公式")
                if item["fValue"] is None: missing.append("F值")
                item["status"] = "manual_review" if missing else "ready_for_preview"
                item["reason"] = "缺少：" + "、".join(missing) if missing else "藥液欄位契約檢核通過"
                rows.append(item)
    finally:
        formula_wb.close()
        value_wb.close()
    return rows
