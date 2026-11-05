import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from chemical_source_scanner import scan_source


class ChemicalSourceScannerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def write(self, name: str, content: bytes = b"xlsx-fixture") -> Path:
        path = self.root / name
        path.write_bytes(content)
        return path

    def test_classifies_early_and_middle_shift_with_report_time(self):
        early = self.write("2026-10-28.xlsx", b"early")
        middle = self.write("2026-10-28中班.xlsx", b"middle")

        manifest = scan_source(self.root)
        included = {row["fileName"]: row for row in manifest["included"]}

        self.assertEqual(included[early.name]["reportDate"], "2026-10-28")
        self.assertEqual(included[early.name]["shift"], "EARLY")
        self.assertEqual(included[early.name]["reportDateTime"], "2026-10-28T08:00:00+08:00")
        self.assertEqual(included[middle.name]["shift"], "MIDDLE")
        self.assertEqual(included[middle.name]["reportDateTime"], "2026-10-28T16:00:00+08:00")

    def test_excludes_replenishment_tank_change_and_excel_temp_files(self):
        for name in ("2026-11-05補液位.xlsx", "2026-11-04更槽.xlsx", "~$2026-11-04.xlsx"):
            self.write(name)

        manifest = scan_source(self.root)

        self.assertEqual(manifest["summary"]["included"], 0)
        self.assertEqual(
            {row["reasonCode"] for row in manifest["excluded"]},
            {"REPLENISHMENT_FILE", "TANK_CHANGE_FILE", "EXCEL_TEMP_FILE"},
        )

    def test_accepts_non_padded_date_and_uses_content_sha256(self):
        source = self.write("2026-1-5.xlsx", b"same-content")

        row = scan_source(self.root)["included"][0]

        self.assertEqual(row["reportDate"], "2026-01-05")
        self.assertEqual(row["sha256"], hashlib.sha256(source.read_bytes()).hexdigest())
        self.assertEqual(row["relativePath"], source.name)

    def test_invalid_date_and_non_xlsx_are_explicitly_excluded(self):
        self.write("2026-02-30.xlsx")
        self.write("readme.txt")

        manifest = scan_source(self.root)

        self.assertEqual(manifest["summary"]["included"], 0)
        self.assertEqual(
            [row["reasonCode"] for row in manifest["excluded"]],
            ["INVALID_REPORT_DATE", "UNSUPPORTED_EXTENSION"],
        )

    def test_manifest_is_deterministic_and_json_serializable(self):
        self.write("2026-10-30中班.xlsx", b"b")
        self.write("2026-10-29.xlsx", b"a")

        first = scan_source(self.root)
        second = scan_source(self.root)

        self.assertEqual(first, second)
        self.assertEqual(json.loads(json.dumps(first, ensure_ascii=False)), first)

    def test_same_report_date_and_shift_are_reported_as_conflict_group(self):
        first = self.root / "a" / "2026-10-30.xlsx"
        second = self.root / "b" / "2026-10-30.xlsx"
        first.parent.mkdir(); second.parent.mkdir()
        first.write_bytes(b"first"); second.write_bytes(b"second")

        manifest = scan_source(self.root)

        self.assertEqual(manifest["summary"]["conflictGroups"], 1)
        self.assertEqual(manifest["conflicts"][0]["reportDate"], "2026-10-30")
        self.assertEqual(manifest["conflicts"][0]["shift"], "EARLY")
        self.assertEqual(manifest["conflicts"][0]["fileCount"], 2)
        self.assertEqual(manifest["conflicts"][0]["relativePaths"], ["a/2026-10-30.xlsx", "b/2026-10-30.xlsx"])

    def test_missing_source_fails_without_creating_output(self):
        missing = self.root / "missing"

        with self.assertRaisesRegex(FileNotFoundError, "來源資料夾不存在"):
            scan_source(missing)


if __name__ == "__main__":
    unittest.main()
