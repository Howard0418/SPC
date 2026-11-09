import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

import openpyxl

from chemical_daily_parser import parse_chemical_daily_report


class ChemicalDailyParserTests(unittest.TestCase):
    def test_extracts_formula_and_cached_value_contract(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "daily.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = "2026-10-30"
            ws.append(["量測日期時間", "分析項目", "量測值", "公式", "F值"])
            ws.append(["2026/10/30 08:30", "硫酸", 10.8, "=A2*2", 0.995])
            wb.save(path)
            rows = parse_chemical_daily_report(path)
            self.assertEqual(rows[0]["status"], "ready_for_preview")
            self.assertEqual(rows[0]["measurementDatetime"], "2026-10-30T08:30:00")
            self.assertEqual(rows[0]["formula"], "=A2*2")
            self.assertEqual(rows[0]["fValue"], 0.995)

    def test_missing_date_is_manual_review(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "bad.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.append(["量測日期時間", "分析項目", "量測值"])
            ws.append([None, "硫酸", 10.8])
            wb.save(path)
            rows = parse_chemical_daily_report(path)
            self.assertEqual(rows[0]["status"], "manual_review")
            self.assertIn("量測日期時間", rows[0]["reason"])

    def test_missing_formula_or_f_value_is_blocked(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "incomplete.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.append(["量測日期時間", "分析項目", "量測值", "公式", "F值"])
            ws.append(["2026/10/30 08:30", "硫酸", 10.8, None, None])
            wb.save(path)
            rows = parse_chemical_daily_report(path)
            self.assertEqual(rows[0]["status"], "manual_review")
            self.assertIn("公式", rows[0]["reason"])
            self.assertIn("F值", rows[0]["reason"])

    def test_extracts_matrix_daily_sheet_and_f_table_reference(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "matrix.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = "CN"
            ws["B3"] = "CN"
            ws["B4"] = "2026/10/27"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            for col, value in enumerate(["CN", "槽位1", "硫酸", "10±4", 6, "~", 14, 10.8, "=H7*F!$B$3"], 1):
                ws.cell(7, col).value = value
            f_sheet = wb.create_sheet("F")
            f_sheet["B3"] = 0.995
            wb.save(path)

            rows = parse_chemical_daily_report(path)

            self.assertEqual(len(rows), 1)
            row = rows[0]
            self.assertEqual(row["measurementDatetime"], "2026-10-27T00:00:00")
            self.assertEqual(row["line"], "CN")
            self.assertEqual(row["slot"], "槽位1")
            self.assertEqual(row["analysisItem"], "硫酸")
            self.assertEqual(row["value"], 10.8)
            self.assertEqual(row["formula"], "=H7*F!$B$3")
            self.assertEqual(row["fValue"], 0.995)
            self.assertEqual(row["status"], "manual_review")
            self.assertIn("公式計算結果", row["reason"])

    def test_matrix_sheet_without_internal_date_is_not_importable(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "matrix-no-date.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = "CN"
            ws["B3"] = "CN"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            for col, value in enumerate(["CN", "槽位1", "硫酸", "10±4", 6, "~", 14, 10.8, "=H7*F!$B$3"], 1):
                ws.cell(7, col).value = value
            f_sheet = wb.create_sheet("F")
            f_sheet["B3"] = 0.995
            wb.save(path)

            row = parse_chemical_daily_report(path)[0]

            self.assertEqual(row["status"], "manual_review")
            self.assertIn("量測日期時間", row["reason"])

    def test_matrix_uses_explicit_yyyy_mm_dd_filename_when_cell_date_is_missing(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "2026-10-30.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "CN"
            ws["B3"] = "CN"; ws["B7"] = "槽位1"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "硫酸"; ws["D7"] = "10±4"; ws["E7"] = 6; ws["G7"] = 14; ws["H7"] = 1; ws["I7"] = "=H7*2"
            wb.save(path)
            row = parse_chemical_daily_report(path)[0]
            self.assertEqual(row["measurementDatetime"], "2026-10-30T00:00:00")

    def test_matrix_footer_text_is_not_parsed_as_measurement(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "matrix-footer.xlsx"
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = "CN"
            ws["B3"] = "CN"
            ws["B4"] = "2026/10/27"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            for col, value in enumerate(["CN", "槽位1", "硫酸", "10±4", 6, "~", 14, 10.8, "=H7*F!$B$3"], 1):
                ws.cell(7, col).value = value
            ws["C8"] = "核准"
            ws["H8"] = "確認"
            f_sheet = wb.create_sheet("F")
            f_sheet["B3"] = 0.995
            wb.save(path)

            rows = parse_chemical_daily_report(path)

            self.assertEqual(len(rows), 1)
            self.assertEqual(rows[0]["analysisItem"], "硫酸")


    def test_matrix_group_line_uses_column_a_and_does_not_reuse_previous_line_tank(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "grouped.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "新線"
            ws["B3"] = "新線"; ws["B4"] = "2026/10/27"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            ws["A7"] = "粗化線\n(PT2)"; ws["B7"] = "脫脂"  # Group header has no measured value.
            for r in [8, 9, 10]:
                ws.cell(r, 3).value = "硫酸"; ws.cell(r, 4).value = "10±4"
                ws.cell(r, 8).value = 1; ws.cell(r, 9).value = f"=H{r}*F!B3"
            ws["A9"] = "顯影線\n(DV2)"; ws["B9"] = "顯影1"
            ws["A10"] = "粗化線\n(PT2)"  # No tank: never inherit DV2's tank.
            wb.create_sheet("F")["B3"] = 1; wb.save(path)
            rows = parse_chemical_daily_report(path)
            self.assertEqual([r["line"] for r in rows], ["PT2", "DV2", "PT2"])
            self.assertEqual([r["slot"] for r in rows], ["脫脂", "顯影1", ""])
            self.assertIn("槽位", rows[-1]["reason"])

    def test_matrix_single_line_keeps_tank_definition_from_unmeasured_row(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "single.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "CN"
            ws["B3"] = "CN"; ws["B4"] = "2026/10/27"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            ws["B7"] = "酸洗1"
            ws["C8"] = "硫酸"; ws["D8"] = "10±4"; ws["H8"] = 0; ws["I8"] = "=H8*F!B3"
            wb.create_sheet("F")["B3"] = 1; wb.save(path)
            row = parse_chemical_daily_report(path)[0]
            self.assertEqual(row["line"], "CN"); self.assertEqual(row["slot"], "酸洗1")
            self.assertEqual(row["value"], 0)

    def test_matrix_uses_numeric_result_formula_in_j_without_f_reference(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "matrix-j.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "N2"
            ws["B3"] = "N2"; ws["B4"] = "2026/10/30"; ws["B7"] = "槽位1"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "KOH"; ws["D7"] = "20±2"; ws["E7"] = 18; ws["G7"] = 22; ws["H7"] = 9.1
            ws["J7"] = "=H7*2"; ws["K7"] = "=IF(J7>22,\"NG\",\"OK\")"
            wb.save(path)
            rows = parse_chemical_daily_report(path)
            row = rows[0]
            self.assertEqual(row["formula"], "=H7*2")
            self.assertIsNone(row["fValue"])
            self.assertEqual(row["status"], "manual_review")  # no cached formula result in a new workbook

    def test_matrix_dash_placeholder_is_not_a_measurement(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "2026-10-30.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "N1"
            ws["B3"] = "N1"; ws["B4"] = "2026/10/30"; ws["B7"] = "槽位1"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "分析值"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "IPA"; ws["H7"] = "."; ws["M7"] = "-"
            wb.save(path)
            self.assertEqual(parse_chemical_daily_report(path), [])

    def test_matrix_exports_normalized_adjustment_formulas(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "2026-10-30.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "C5"
            ws["B3"] = "C5"; ws["B4"] = "2026/10/30"; ws["B7"] = "清潔"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "濃度", "判定", "調整", "調整量"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "硫酸"; ws["D7"] = "100±20"; ws["E7"] = 80; ws["G7"] = 120
            ws["H7"] = 10; ws["I7"] = "=H7*8.7*F!$B$3"
            ws["K7"] = '=IF(I7<E7,"添加",IF(I7>G7,"稀釋",""))'
            ws["L7"] = '=IF(I7<E7,"藥液："&ROUND((E7-I7)*100,1)&" L","")'
            wb.create_sheet("F")["B3"] = 0.99; wb.save(path)

            row = parse_chemical_daily_report(path)[0]

            self.assertEqual(row["adjustmentFormula"], 'IF(Concentration<80,"添加",IF(Concentration>120,"稀釋",""))')
            self.assertEqual(row["adjustmentAmountFormula"], 'IF(Concentration<80,"藥液："&ROUND((80-Concentration)*100,1)&" L","")')

    def test_matrix_rejects_unknown_adjustment_reference(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "2026-10-30.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "C5"
            ws["B3"] = "C5"; ws["B4"] = "2026/10/30"; ws["B7"] = "清潔"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "濃度", "判定", "調整", "調整量"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "硫酸"; ws["D7"] = "100±20"; ws["H7"] = 10; ws["I7"] = "=H7*8.7"
            ws["K7"] = '=IF(I7<T2,"添加","")'; wb.save(path)

            row = parse_chemical_daily_report(path)[0]

            self.assertEqual(row["status"], "manual_review")
            self.assertIn("調整公式", row["reason"])

    def test_matrix_removes_na_only_adjustment_branch(self):
        with TemporaryDirectory() as temp:
            path = Path(temp) / "2026-10-30.xlsx"
            wb = openpyxl.Workbook(); ws = wb.active; ws.title = "N2"
            ws["B3"] = "N2"; ws["B4"] = "2026/10/30"; ws["B7"] = "催化"
            for col, value in enumerate(["線別", "槽別", "分析項目", "規格", "下限", "", "上限", "滴定數", "濃度", "判定", "調整", "調整量"], 1):
                ws.cell(6, col).value = value
            ws["C7"] = "氯化亞錫"; ws["H7"] = 1; ws["I7"] = "=H7*2"; ws["T7"] = 10; ws["U7"] = "NA"
            ws["K7"] = '=IF(I7<T7,"添加",IF(I7>U7,"更槽",""))'; wb.save(path)

            row = parse_chemical_daily_report(path)[0]

            self.assertEqual(row["adjustmentFormula"], 'IF(Concentration<10,"添加","")')

if __name__ == "__main__":
    unittest.main()
