import base64
import json
import os
from pathlib import Path
import subprocess
from tempfile import TemporaryDirectory
import unittest
from unittest.mock import patch

from portal_credential_store import (
    CredentialStoreError,
    load_portal_credential,
    provision_portal_credential,
    remove_portal_credential,
    validate_portal_credential,
)


class PortalCredentialStoreTests(unittest.TestCase):
    def test_provision_stores_only_metadata_and_dpapi_ciphertext(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            with patch("portal_credential_store._protect", return_value=b"cipher"), patch(
                "portal_credential_store._restrict_acl"
            ) as restrict:
                provision_portal_credential(path, "ihao_ting", "secret-value")

            data = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(data["purpose"], "PMR.TransFiles.Portal")
            self.assertEqual(data["account"], "ihao_ting")
            self.assertEqual(base64.b64decode(data["ciphertext"]), b"cipher")
            self.assertNotIn("secret-value", path.read_text(encoding="utf-8"))
            restrict.assert_called_once()
            protected_path = restrict.call_args.args[0]
            self.assertEqual(protected_path.parent, path.parent)
            self.assertTrue(protected_path.name.startswith(f".{path.name}."))

    def test_load_rejects_wrong_purpose_without_decrypting(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            path.write_text(json.dumps({"version": 1, "purpose": "wrong", "account": "a", "ciphertext": "YQ=="}), encoding="utf-8")
            with patch("portal_credential_store._unprotect") as unprotect:
                with self.assertRaisesRegex(CredentialStoreError, "用途"):
                    load_portal_credential(path)
            unprotect.assert_not_called()

    def test_validate_never_returns_password(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            path.write_text(json.dumps({"version": 1, "purpose": "PMR.TransFiles.Portal", "account": "ihao_ting", "ciphertext": "Y2lwaGVy"}), encoding="utf-8")
            with patch("portal_credential_store._unprotect", return_value=b"secret-value"):
                result = validate_portal_credential(path)
            self.assertEqual(result, {"account": "ihao_ting", "decryptable": True})
            self.assertNotIn("secret", repr(result))

    def test_tampered_ciphertext_is_sanitized(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            path.write_text(json.dumps({"version": 1, "purpose": "PMR.TransFiles.Portal", "account": "ihao_ting", "ciphertext": "YQ=="}), encoding="utf-8")
            with patch("portal_credential_store._unprotect", side_effect=OSError("cipher secret leaked")):
                with self.assertRaisesRegex(CredentialStoreError, "無法由目前Windows身分解密") as caught:
                    load_portal_credential(path)
            self.assertNotIn("secret", str(caught.exception))

    def test_acl_failure_removes_new_file(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            with patch("portal_credential_store._protect", return_value=b"cipher"), patch(
                "portal_credential_store._restrict_acl", side_effect=CredentialStoreError("ACL失敗")
            ):
                with self.assertRaises(CredentialStoreError):
                    provision_portal_credential(path, "ihao_ting", "secret-value")
            self.assertFalse(path.exists())

    def test_remove_is_idempotent(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            path.write_text("x", encoding="utf-8")
            self.assertTrue(remove_portal_credential(path))
            self.assertFalse(remove_portal_credential(path))

    @unittest.skipUnless(os.name == "nt", "Windows DPAPI only")
    def test_powershell_validator_reads_python_dpapi_format_without_password_output(self):
        with TemporaryDirectory() as temp_dir:
            path = Path(temp_dir) / "portal.credential.json"
            provision_portal_credential(path, "synthetic", "not-a-real-password")
            script = Path(__file__).with_name("Provision-TransFilesPortalCredential.ps1")
            result = subprocess.run(
                [
                    "powershell.exe", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(script),
                    "-Action", "Validate", "-Path", str(path),
                ],
                check=True, capture_output=True, text=True, encoding="utf-8", errors="replace",
            )
            self.assertIn("account=synthetic", result.stdout)
            self.assertNotIn("not-a-real-password", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
