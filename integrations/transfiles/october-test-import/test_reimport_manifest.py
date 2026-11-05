import copy
import unittest

from reimport_manifest import apply_approved_precedence


class ReimportManifestTests(unittest.TestCase):
    def test_line_override_and_winner_are_applied_without_accepting_loser(self):
        manifest = {"summary": {"month": "2026-09"}, "rows": [
            {"fileDay": "2026-09-02", "sheet": "N1 (開)", "sourceRow": 7, "line": "N2", "fileHash": "A", "status": "validated"},
            {"fileDay": "2026-09-30", "sheet": "新線", "sourceRow": 30, "line": "QE2", "fileHash": "OLD", "status": "blocked"},
            {"fileDay": "2026-09-30", "sheet": "新線", "sourceRow": 30, "line": "QE2", "fileHash": "WIN", "status": "blocked"},
        ]}
        original = copy.deepcopy(manifest)
        result = apply_approved_precedence(manifest, [{"fileHash": "WIN", "row": 30}], "approved-sha")
        self.assertEqual("N1", result["rows"][0]["line"])
        self.assertEqual("blocked", result["rows"][1]["status"])
        self.assertEqual("validated", result["rows"][2]["status"])
        self.assertEqual(2, result["summary"]["reimportVersion"])
        self.assertEqual("approved-sha", result["summary"]["approvedWorkbookSha256"])
        self.assertEqual("N2", original["rows"][0]["line"])

    def test_unknown_winner_is_rejected(self):
        with self.assertRaises(ValueError):
            apply_approved_precedence({"summary": {"month": "2026-10"}, "rows": []}, [{"fileHash": "MISSING", "row": 9}], "sha")

    def test_only_sep_oct_are_allowed(self):
        with self.assertRaises(ValueError):
            apply_approved_precedence({"summary": {"month": "2026-11"}, "rows": []}, [], "sha")


if __name__ == "__main__":
    unittest.main()
