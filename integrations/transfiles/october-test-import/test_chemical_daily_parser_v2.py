import tempfile
import unittest
from pathlib import Path

import openpyxl

from chemical_daily_parser_v2 import parse_report


class ChemicalDailyParserV2Tests(unittest.TestCase):
    def fixture(self, sheet="N1（開）", line="N1", concentration_col=9):
        temp = tempfile.TemporaryDirectory()
        path = Path(temp.name) / "2026-10-28.xlsx"
        wb = openpyxl.Workbook(); ws = wb.active; ws.title = sheet
        ws["A3"] = "線別"; ws["B3"] = line; ws["A4"] = "日期"; ws["B4"] = "2026/10/28"
        headers = {1:"線別",2:"槽別",3:"分析項目",4:"規格",5:"下限",7:"上限",8:"滴定數(ml)",concentration_col:"濃度",13:"滴定數(ml)",14:"濃度"}
        for col,value in headers.items(): ws.cell(6,col).value=value
        wb.create_sheet("F")["B3"] = 0.99
        wb.save(path)
        return temp, path

    def metadata(self, shift="EARLY"):
        return {"reportDate":"2026-10-28","shift":shift,"reportDateTime":"2026-10-28T08:00:00+08:00","sha256":"a"*64,"relativePath":"2026-10-28.xlsx"}

    def test_parses_n1_open_formula_and_f_reference_by_headers(self):
        temp,path=self.fixture(concentration_col=10)
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N1（開）"];ws["A7"]="化鎳線 (N1)";ws["B7"]="表處";ws["C7"]="氫氧化鉀";ws["H7"]=8.2;ws["J7"]="=H7*8.7*F!$B$3";wb.save(path)
            result=parse_report(path,self.metadata())
            row=result["rows"][0]
            self.assertEqual((row["line"],row["samplingStage"]),("N1","OPEN"))
            self.assertEqual(row["initial"]["mode"],"FORMULA")
            self.assertEqual(row["initial"]["titration"],8.2)
            self.assertEqual(row["initial"]["formula"],"=H7*8.7*F!$B$3")
            self.assertEqual(row["initial"]["fReferences"],["F!B3"])
            self.assertEqual(row["initial"]["sourceCells"],{"titration":"H7","secondary":None,"concentration":"J7"})
        finally: temp.cleanup()

    def test_parses_recheck_independently_and_close_stage(self):
        temp,path=self.fixture(sheet="N2（收）",line="N2")
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N2（收）"];ws["B7"]="微蝕";ws["C7"]="硫酸";ws["M7"]=2.5;ws["N7"]="=M7*F!B3";wb.save(path)
            row=parse_report(path,self.metadata())["rows"][0]
            self.assertEqual(row["samplingStage"],"CLOSE")
            self.assertIsNone(row["initial"])
            self.assertEqual(row["recheck"]["mode"],"FORMULA")
            self.assertEqual(row["recheck"]["fReferences"],["F!B3"])
        finally: temp.cleanup()

    def test_non_n1_n2_is_general(self):
        temp,path=self.fixture(sheet="C5",line="C5")
        try:
            wb=openpyxl.load_workbook(path);ws=wb["C5"];ws["B7"]="清潔";ws["C7"]="硫酸";ws["H7"]=1;ws["I7"]="=H7*2";wb.save(path)
            self.assertEqual(parse_report(path,self.metadata())["rows"][0]["samplingStage"],"GENERAL")
        finally: temp.cleanup()

    def test_dash_titration_with_numeric_concentration_is_direct_input(self):
        temp,path=self.fixture()
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N1（開）"];ws["B7"]="調節";ws["C7"]="PH";ws["H7"]="-";ws["I7"]=9.78;wb.save(path)
            phase=parse_report(path,self.metadata())["rows"][0]["initial"]
            self.assertEqual(phase["mode"],"DIRECT")
            self.assertEqual(phase["concentration"],9.78)
            self.assertIsNone(phase["formula"])
            self.assertEqual(phase["fReferences"],[])
        finally: temp.cleanup()

    def test_both_empty_or_placeholder_are_skipped(self):
        temp,path=self.fixture()
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N1（開）"];ws["B7"]="調節";ws["C7"]="PH";ws["H7"]="-";ws["M7"]="-";wb.save(path)
            self.assertEqual(parse_report(path,self.metadata())["rows"],[])
        finally: temp.cleanup()

    def test_any_titration_value_without_formula_becomes_explicit_error(self):
        temp,path=self.fixture()
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N1（開）"];ws["B7"]="表處";ws["C7"]="P200";ws["H7"]=12.6;wb.save(path)
            row=parse_report(path,self.metadata())["rows"][0]
            self.assertEqual(row["status"],"ERROR")
            self.assertEqual(row["errors"][0]["code"],"FORMULA_MISSING")
        finally: temp.cleanup()

    def test_sheet_and_internal_line_mismatch_is_error(self):
        temp,path=self.fixture(sheet="C5",line="C3")
        try:
            result=parse_report(path,self.metadata())
            self.assertEqual(result["sheetErrors"][0]["code"],"LINE_MISMATCH")
            self.assertEqual(result["rows"],[])
        finally: temp.cleanup()

    def test_legacy_f_value_sheet_is_not_parsed_as_line_and_is_resolved(self):
        temp,path=self.fixture()
        try:
            wb=openpyxl.load_workbook(path);wb["F"].title="F值";ws=wb["N1（開）"];ws["B7"]="表處";ws["C7"]="硫酸";ws["H7"]=1;ws["I7"]="=H7*F值!$B$3";wb.save(path)
            result=parse_report(path,self.metadata())
            self.assertEqual(result["sheetErrors"],[])
            self.assertEqual(result["rows"][0]["initial"]["fReferences"],["F!B3"])
            self.assertEqual(result["fValues"]["F!B3"],0.99)
        finally: temp.cleanup()

    def test_formula_with_middle_secondary_input_is_captured(self):
        temp,path=self.fixture(concentration_col=10)
        try:
            wb=openpyxl.load_workbook(path);ws=wb["N1（開）"];ws["B7"]="化鎳";ws["C7"]="次磷酸鈉";ws["H7"]=19.9;ws["I7"]=12.9;ws["J7"]='=IF(I7="","",(H7-I7)*2.65)';wb.save(path)
            phase=parse_report(path,self.metadata())["rows"][0]["initial"]
            self.assertEqual(phase["secondary"],12.9)
            self.assertEqual(phase["sourceCells"]["secondary"],"I7")
        finally: temp.cleanup()

    def test_legacy_sheet_carries_actual_section_line_from_column_a(self):
        temp,path=self.fixture(sheet="舊線",line="舊線")
        try:
            wb=openpyxl.load_workbook(path);ws=wb["舊線"]
            ws["A7"]="活化線\n(PT)";ws["B7"]="抗氧化";ws["C7"]="PH";ws["H7"]="-";ws["I7"]=5.5
            ws["B8"]="酸洗";ws["C8"]="硫酸";ws["H8"]="-";ws["I8"]=4.8;wb.save(path)
            rows=parse_report(path,self.metadata())["rows"]
            self.assertEqual([row["line"] for row in rows],["PT1","PT1"])
        finally: temp.cleanup()

    def test_new_sheet_reads_explicit_qe2_section_line(self):
        temp,path=self.fixture(sheet="新線",line="新線")
        try:
            wb=openpyxl.load_workbook(path);ws=wb["新線"]
            ws["A7"]="蝕刻線\n(QE2)";ws["B7"]="蝕刻(銅)";ws["C7"]="銅離子";ws["H7"]="-";ws["I7"]=2.1;wb.save(path)
            self.assertEqual(parse_report(path,self.metadata())["rows"][0]["line"],"QE2")
        finally: temp.cleanup()


if __name__ == "__main__":
    unittest.main()
