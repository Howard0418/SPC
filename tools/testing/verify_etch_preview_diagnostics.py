"""Offline acceptance test against a TransFiles source checkout; no GUI or network.

Usage: python tools/testing/verify_etch_preview_diagnostics.py --transfiles-root PATH
"""
import argparse
import io
import json
from pathlib import Path
import sys
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch
from urllib.error import HTTPError


class PreviewDiagnosticsTests(unittest.TestCase):
    def run_failure(self, status, body):
        client = upload.EtchImportClient()
        error = HTTPError(
            "https://portaltest.pmr.com.tw/api/etch-import-test/preview",
            status, "fixture", {}, io.BytesIO(body),
        )
        reports = [
            {"reportDate": "2026-10-16", "lineCode": "PT1"},
            {"reportDate": "2026-10-16", "lineCode": "PT2"},
        ]
        messages = Mock()
        with TemporaryDirectory() as directory:
            view = SimpleNamespace(
                etch_import_client=client,
                selected_output_dir=Path(directory),
                spc_environment_var=SimpleNamespace(get=lambda: "test"),
                output_file_paths=[],
                update_status=Mock(), log=Mock(),
                root=SimpleNamespace(after=lambda delay, callback: callback()),
                ask_on_ui_thread=Mock(),
            )
            with patch.object(client.opener, "open", side_effect=error) as opened, \
                    patch.object(client, "confirm") as confirm, \
                    patch.object(gui, "messagebox", messages):
                with self.assertRaisesRegex(ValueError, "LOG"):
                    gui.ConversionGUI.import_complete_reports(
                        view, [{"reports": reports}], 0,
                    )
                opened.assert_called_once()
                request = opened.call_args.args[0]
                self.assertTrue(request.full_url.endswith("/preview"))
                self.assertEqual("PT1", json.loads(request.data)["lineCode"])
                confirm.assert_not_called()
                view.ask_on_ui_thread.assert_not_called()
                with self.assertRaises(ValueError):
                    upload.EtchImportClient.confirm(client, client._preview)
            self.assertEqual(1, len(view.output_file_paths))
            record = json.loads(view.output_file_paths[0].read_text(encoding="utf-8"))
            self.assertFalse(record["writtenToDatabase"])
            self.assertEqual("PT1", record["lineCode"])
            self.assertEqual("test", record["environment"])
            messages.showerror.assert_called_once()
            popup = messages.showerror.call_args.args[1]
            logs = "\n".join(call.args[0] for call in view.log.call_args_list)
            self.assertIn(record["error"], popup)
            self.assertIn(record["error"], logs)
            return record["error"], popup, logs

    def test_500_and_503_diagnostics_reach_popup_and_log(self):
        for status, code in [(500, "ETCH_PREVIEW_SERVER_FAILURE"),
                             (503, "ETCH_PREVIEW_DATABASE_FAILURE")]:
            with self.subTest(status=status):
                trace = "offline-trace-123"
                message = f"SPC preview [{code}], trace {trace}"
                body = json.dumps({"message": message, "errorCode": code,
                                   "traceId": trace, "detail": json.dumps({
                                       "message": message, "errorCode": code,
                                       "traceId": trace})}).encode()
                for text in self.run_failure(status, body):
                    self.assertIn(code, text)
                    self.assertIn(trace, text)
                    self.assertIn(f"HTTP {status}", text)

    def test_400_and_409_keep_readable_rejection(self):
        for status in [400, 409]:
            with self.subTest(status=status):
                for text in self.run_failure(status, b'{"message":"Invalid report"}'):
                    self.assertIn("Invalid report", text)
                    self.assertIn(f"HTTP {status}", text)

    def test_invalid_json_uses_safe_fallback(self):
        for text in self.run_failure(503, b"not-json internal-secret"):
            self.assertIn("HTTP 503", text)
            self.assertNotIn("internal-secret", text)

    def test_log_and_popup_redact_credential_fragments(self):
        for text in self.run_failure(500, b'{"message":"token=secret-value"}'):
            self.assertNotIn("secret-value", text)
            self.assertIn("[REDACTED]", text)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--transfiles-root", type=Path, required=True)
    args = parser.parse_args()
    sys.path.insert(0, str(args.transfiles_root.resolve()))
    sys.dont_write_bytecode = True
    import etch_upload as upload
    import batch_convert_gui as gui
    unittest.main(argv=[sys.argv[0]], verbosity=2)
