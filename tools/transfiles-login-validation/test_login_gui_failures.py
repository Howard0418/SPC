"""Run with TRANSFILES_SOURCE pointing at the existing TransFiles source directory."""
import os
import sys
import ssl
import socket
import unittest
from types import SimpleNamespace
from unittest.mock import Mock, patch

sys.path.insert(0, os.environ["TRANSFILES_SOURCE"])
import batch_convert_gui as gui
from connection_test import run_read_only_login_test
from connection_settings import default_connection_settings
from etch_upload import describe_network_failure


class DeferredLoginFailureTests(unittest.TestCase):
    def check_stage(self, stage):
        for error, expected in [(ssl.SSLCertVerificationError("synthetic-secret"), "憑證驗證失敗"),
                                (socket.timeout("synthetic-secret"), "連線逾時"),
                                (ConnectionResetError("synthetic-secret"), "伺服器或網路設備中斷")]:
            with self.subTest(stage=stage, category=expected):
                callbacks = []
                root = SimpleNamespace(after=lambda delay, callback: callbacks.append(callback))
                app = SimpleNamespace(root=root, spc_environment_var=SimpleNamespace(get=lambda: "test"),
                                      connection_settings=default_connection_settings(),
                                      test_login_btn=Mock(), update_status=Mock(), log=Mock())
                failure = RuntimeError(describe_network_failure(error))
                portal = Mock()
                portal.login.return_value = None
                portal.spc_token.return_value = "synthetic-token"
                get_json = Mock(return_value={"generatedAt": "synthetic"})
                if stage == "Portal 登入":
                    portal.login.side_effect = failure
                elif stage == "取得 SPC 權杖":
                    portal.spc_token.side_effect = failure
                else:
                    get_json.side_effect = failure

                def run(settings, environment, account, password, **ignored):
                    return run_read_only_login_test(settings, environment, account, password,
                                                   portal_client_factory=lambda url: portal, get_json=get_json)

                def thread(**kwargs):
                    return SimpleNamespace(start=kwargs["target"])

                with patch.object(gui.simpledialog, "askstring", side_effect=["synthetic-user", "synthetic-secret"]), \
                     patch.object(gui.threading, "Thread", side_effect=thread), \
                     patch.object(gui, "run_read_only_login_test", side_effect=run), \
                     patch.object(gui.messagebox, "showerror") as showerror, \
                     patch.object(gui.messagebox, "showinfo") as showinfo:
                    gui.ConversionGUI.start_read_only_login_test(app)
                    showerror.assert_not_called()
                    for callback in callbacks:
                        callback()
                    showerror.assert_called_once()
                    message = showerror.call_args.args[1]
                    self.assertIn(stage, message)
                    self.assertIn(expected, message)
                    self.assertNotIn("synthetic-secret", message)
                    self.assertNotIn("synthetic-token", message)
                    self.assertNotIn("synthetic-secret", str(app.log.call_args_list))
                    showinfo.assert_not_called()
                    app.test_login_btn.configure.assert_any_call(state=gui.tk.NORMAL)
                if stage == "Portal 登入":
                    portal.spc_token.assert_not_called()
                if stage != "SPC 唯讀驗證":
                    get_json.assert_not_called()
                portal.preview.assert_not_called()
                portal.confirm.assert_not_called()

    def test_portal_failures(self):
        self.check_stage("Portal 登入")

    def test_token_failures(self):
        self.check_stage("取得 SPC 權杖")

    def test_master_data_failures(self):
        self.check_stage("SPC 唯讀驗證")


if __name__ == "__main__":
    unittest.main()
