"""Offline characterization of actual TransFiles source; no GUI import, DB, or business workbook."""
import argparse
import ast
import datetime
from decimal import Decimal, InvalidOperation, ROUND_HALF_UP
from pathlib import Path
import re
import socket
import unittest
from unittest.mock import patch


class TransFilesSourceBaseline(unittest.TestCase):
    root: Path

    @classmethod
    def setUpClass(cls):
        cls.text = (cls.root / "batch_convert_gui.py").read_text(encoding="utf-8-sig")
        cls.tree = ast.parse(cls.text)
        names = {"parse_filename_datetime", "round_measurement", "is_valid_value"}
        functions = [node for node in cls.tree.body if isinstance(node, ast.FunctionDef) and node.name in names]
        if len(functions) != len(names):
            raise AssertionError("Actual source functions missing; update the baseline explicitly.")
        cls.namespace = {"Path": Path, "re": re, "Decimal": Decimal, "InvalidOperation": InvalidOperation,
                         "ROUND_HALF_UP": ROUND_HALF_UP, "Any": object}
        exec(compile(ast.Module(body=functions, type_ignores=[]), str(cls.root / "batch_convert_gui.py"), "exec"), cls.namespace)

    def setUp(self):
        self.network_guard = patch.object(socket, "socket", side_effect=AssertionError("Network forbidden"))
        self.network_guard.start()
        self.addCleanup(self.network_guard.stop)

    def test_two_sides_of_midnight_use_filename_without_timezone(self):
        parse = self.namespace["parse_filename_datetime"]
        self.assertEqual(parse("2026-10-31-2359.xlsx"), (20261031, "23:59:00"))
        self.assertEqual(parse("2026-11-1-0000.xlsx"), (20261101, "00:00:00"))

    def test_date_only_filename_defaults_to_0910(self):
        self.assertEqual(self.namespace["parse_filename_datetime"]("2026-11-01.xlsx"), (20261101, "09:10:00"))

    def test_bad_filename_legacy_fallback_is_evidence_not_approved_new_behavior(self):
        self.assertEqual(self.namespace["parse_filename_datetime"]("invalid.xlsx"), (20260424, "09:10:00"))

    def test_calendar_and_clock_validation_are_missing_in_current_parser(self):
        date, clock = self.namespace["parse_filename_datetime"]("2026-2-30-2460.xlsx")
        self.assertEqual((date, clock), (20260230, "24:60:00"))
        with self.assertRaises(ValueError):
            datetime.datetime.strptime(f"{date} {clock}", "%Y%m%d %H:%M:%S")

    def test_positive_and_negative_midpoints_round_away_from_zero(self):
        round_value = self.namespace["round_measurement"]
        self.assertEqual(round_value("1.235"), 1.24)
        self.assertEqual(round_value("-1.235"), -1.24)

    def test_original_titration_precision_would_be_lost_if_only_rounded_export_is_kept(self):
        self.assertEqual(self.namespace["round_measurement"]("1.23456789"), 1.23)
        self.assertNotEqual(Decimal(str(self.namespace["round_measurement"]("1.23456789"))), Decimal("1.23456789"))

    def test_empty_markers_remain_empty_not_numeric_zero(self):
        for value in (None, "", "未取樣", "N/A"):
            with self.subTest(value=value):
                self.assertIsNone(self.namespace["round_measurement"](value))
        self.assertEqual(self.namespace["round_measurement"]("0"), 0)

    def test_current_chemical_import_uses_http_preview_then_confirm_not_sql(self):
        gui = next(n for n in self.tree.body if isinstance(n, ast.ClassDef) and n.name == "ConversionGUI")
        methods = {node.name: node for node in gui.body if isinstance(node, ast.FunctionDef)}
        preview = ast.get_source_segment(self.text, methods["upload_existing_excel"])
        confirm = ast.get_source_segment(self.text, methods["confirm_spc_batches"])
        self.assertIn("/v1/uploads/variable/excel?portalDaily=true", preview)
        self.assertIn("/v1/uploads/{batch_id}/confirm?mode=upsert", confirm)
        self.assertIn('method="POST"', confirm)
        self.assertNotIn("pyodbc", self.text)

    def test_cached_excel_and_titration_exports_are_current_behavior(self):
        gui = next(n for n in self.tree.body if isinstance(n, ast.ClassDef) and n.name == "ConversionGUI")
        method = next(n for n in gui.body if isinstance(n, ast.FunctionDef) and n.name == "parse_single_file")
        source = ast.get_source_segment(self.text, method)
        self.assertIn("data_only=True", source)
        self.assertIn("round_measurement(prim_titration)", source)
        self.assertIn("round_measurement(retest_titration)", source)

    def test_direct_import_module_explicitly_documents_no_database_connection(self):
        tree = ast.parse((self.root / "direct_import.py").read_text(encoding="utf-8-sig"))
        self.assertIn("does not connect", ast.get_docstring(tree))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--transfiles-root", type=Path, required=True)
    args = parser.parse_args()
    TransFilesSourceBaseline.root = args.transfiles_root.resolve(strict=True)
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(TransFilesSourceBaseline))
    raise SystemExit(0 if result.wasSuccessful() else 1)
