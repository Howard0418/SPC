import tempfile
import unittest
from pathlib import Path

import openpyxl

from chemical_preview_builder import build_preview
from chemical_source_scanner import scan_source


class ChemicalPreviewBuilderTests(unittest.TestCase):
    def workbook(self, root: Path, name: str, titration=2.0, formula="=IF(H7=\"\",\"\",H7*F!$B$3)", direct=None):
        path=root/name; wb=openpyxl.Workbook(); ws=wb.active; ws.title="C5"
        ws["A3"]="線別";ws["B3"]="C5";ws["A4"]="日期";ws["B4"]="2026/10/28"
        for col,value in {1:"線別",2:"槽別",3:"分析項目",4:"規格",5:"下限",7:"上限",8:"滴定數",9:"濃度",13:"滴定數",14:"濃度"}.items():ws.cell(6,col).value=value
        ws["B7"]="清潔";ws["C7"]="硫酸"
        if direct is None:ws["H7"]=titration;ws["I7"]=formula
        else:ws["H7"]="-";ws["I7"]=direct
        wb.create_sheet("F")["B3"]=0.99;wb.save(path);return path

    def test_direct_concentration_is_ready_without_formula_or_f(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);self.workbook(root,"2026-10-28.xlsx",direct=9.78)
            preview=build_preview(root,scan_source(root))
            self.assertEqual(preview["summary"]["readyRows"],1)
            phase=preview["rows"][0]["initial"]
            self.assertEqual((phase["mode"],phase["concentration"]),("DIRECT",9.78))

    def test_missing_cached_formula_result_is_actionable_error(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);self.workbook(root,"2026-10-28.xlsx")
            preview=build_preview(root,scan_source(root))
            self.assertEqual(preview["summary"]["errorRows"],1)
            self.assertIn("FORMULA_RESULT_MISSING",[e["code"] for e in preview["errors"]])

    def test_same_date_shift_files_are_conflicts_not_ready(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);(root/"a").mkdir();(root/"b").mkdir()
            self.workbook(root/"a","2026-10-28.xlsx",direct=1)
            self.workbook(root/"b","2026-10-28.xlsx",direct=2)
            preview=build_preview(root,scan_source(root))
            self.assertEqual(preview["summary"]["conflictRows"],2)
            self.assertEqual(preview["summary"]["readyRows"],0)
            self.assertTrue(all(row["status"]=="SOURCE_CONFLICT" for row in preview["rows"]))

    def test_every_included_file_has_reconciliation_row(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);self.workbook(root,"2026-10-28.xlsx",direct=1);(root/"2026-10-29.xlsx").write_bytes(b"bad-xlsx")
            preview=build_preview(root,scan_source(root))
            self.assertEqual(len(preview["files"]),2)
            failed=next(row for row in preview["files"] if row["fileName"]=="2026-10-29.xlsx")
            self.assertEqual(failed["status"],"FILE_ERROR")

    def test_source_hash_change_is_rejected_before_parse(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);path=self.workbook(root,"2026-10-28.xlsx",direct=1);manifest=scan_source(root)
            path.write_bytes(path.read_bytes()+b"changed")
            preview=build_preview(root,manifest)
            self.assertEqual(preview["files"][0]["status"],"FILE_ERROR")
            self.assertEqual(preview["errors"][0]["code"],"SOURCE_HASH_MISMATCH")


if __name__=="__main__":unittest.main()
