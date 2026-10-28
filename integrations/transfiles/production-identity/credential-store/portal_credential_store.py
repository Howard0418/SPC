"""Windows CurrentUser DPAPI storage for the unattended Portal credential.

The password is accepted only through an interactive prompt by the CLI.  The
library API exists for the runner and tests; callers must not log its result.
"""
from __future__ import annotations

import argparse
import base64
import ctypes
from ctypes import wintypes
from datetime import datetime, timezone
import getpass
import json
import os
from pathlib import Path
import subprocess
import sys
import uuid


PURPOSE = "PMR.TransFiles.Portal"
VERSION = 1
CRYPTPROTECT_UI_FORBIDDEN = 0x1


class CredentialStoreError(RuntimeError):
    pass


class _DataBlob(ctypes.Structure):
    _fields_ = [("cbData", wintypes.DWORD), ("pbData", ctypes.POINTER(ctypes.c_byte))]


def default_credential_path() -> Path:
    root = os.environ.get("LOCALAPPDATA")
    if not root:
        raise CredentialStoreError("找不到目前Windows使用者的LOCALAPPDATA。")
    return Path(root) / "PMR" / "TransFiles" / "portal.credential.json"


def _blob(data: bytes) -> tuple[_DataBlob, ctypes.Array]:
    buffer = ctypes.create_string_buffer(data)
    return _DataBlob(len(data), ctypes.cast(buffer, ctypes.POINTER(ctypes.c_byte))), buffer


def _protect(plaintext: bytes) -> bytes:
    if os.name != "nt":
        raise CredentialStoreError("DPAPI憑證庫只支援Windows。")
    source, source_buffer = _blob(plaintext)
    entropy, entropy_buffer = _blob(PURPOSE.encode("utf-8"))
    output = _DataBlob()
    crypt32 = ctypes.windll.crypt32
    kernel32 = ctypes.windll.kernel32
    ok = crypt32.CryptProtectData(
        ctypes.byref(source), PURPOSE, ctypes.byref(entropy), None, None,
        CRYPTPROTECT_UI_FORBIDDEN, ctypes.byref(output),
    )
    _ = source_buffer, entropy_buffer
    if not ok:
        raise CredentialStoreError("Windows DPAPI加密失敗。")
    try:
        return ctypes.string_at(output.pbData, output.cbData)
    finally:
        kernel32.LocalFree(output.pbData)


def _unprotect(ciphertext: bytes) -> bytes:
    if os.name != "nt":
        raise CredentialStoreError("DPAPI憑證庫只支援Windows。")
    source, source_buffer = _blob(ciphertext)
    entropy, entropy_buffer = _blob(PURPOSE.encode("utf-8"))
    output = _DataBlob()
    crypt32 = ctypes.windll.crypt32
    kernel32 = ctypes.windll.kernel32
    ok = crypt32.CryptUnprotectData(
        ctypes.byref(source), None, ctypes.byref(entropy), None, None,
        CRYPTPROTECT_UI_FORBIDDEN, ctypes.byref(output),
    )
    _ = source_buffer, entropy_buffer
    if not ok:
        raise CredentialStoreError("憑證無法由目前Windows身分解密。")
    try:
        return ctypes.string_at(output.pbData, output.cbData)
    finally:
        kernel32.LocalFree(output.pbData)


def _restrict_acl(path: Path) -> None:
    try:
        identity = subprocess.run(
            ["whoami.exe"], check=True, capture_output=True, text=True,
            encoding="utf-8", errors="replace",
        ).stdout.strip()
        if not identity:
            raise OSError("empty identity")
        result = subprocess.run(
            [
                "icacls.exe", str(path), "/inheritance:r", "/grant:r",
                f"{identity}:(F)", "SYSTEM:(F)",
            ],
            check=False, capture_output=True, text=True,
            encoding="utf-8", errors="replace",
        )
        if result.returncode != 0:
            raise OSError("icacls failed")
    except Exception as exc:
        raise CredentialStoreError("無法套用憑證檔最小ACL，未保存新憑證。") from exc


def provision_portal_credential(path: Path, account: str, password: str) -> dict[str, str]:
    path = Path(path)
    account = str(account or "").strip()
    if not account:
        raise CredentialStoreError("Portal帳號不可空白。")
    if not password:
        raise CredentialStoreError("Portal密碼不可空白。")
    try:
        ciphertext = _protect(password.encode("utf-8"))
    except CredentialStoreError:
        raise
    except Exception as exc:
        raise CredentialStoreError("Windows DPAPI加密失敗。") from exc
    payload = {
        "version": VERSION,
        "purpose": PURPOSE,
        "account": account,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "ciphertext": base64.b64encode(ciphertext).decode("ascii"),
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    try:
        with temporary.open("x", encoding="utf-8") as stream:
            json.dump(payload, stream, ensure_ascii=False, indent=2)
            stream.flush()
            os.fsync(stream.fileno())
        _restrict_acl(temporary)
        os.replace(temporary, path)
    except Exception:
        temporary.unlink(missing_ok=True)
        raise
    return {"account": account, "path": str(path)}


def load_portal_credential(path: Path) -> tuple[str, str]:
    try:
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        if data.get("version") != VERSION:
            raise CredentialStoreError("憑證檔版本不支援。")
        if data.get("purpose") != PURPOSE:
            raise CredentialStoreError("憑證用途不符。")
        account = str(data.get("account") or "").strip()
        if not account:
            raise CredentialStoreError("憑證檔缺少Portal帳號。")
        ciphertext = base64.b64decode(data.get("ciphertext") or "", validate=True)
        password = _unprotect(ciphertext).decode("utf-8")
        if not password:
            raise CredentialStoreError("解密後的Portal密碼為空白。")
        return account, password
    except CredentialStoreError:
        raise
    except Exception as exc:
        raise CredentialStoreError("憑證檔格式錯誤或無法由目前Windows身分解密。") from exc


def validate_portal_credential(path: Path) -> dict[str, object]:
    account, password = load_portal_credential(path)
    try:
        return {"account": account, "decryptable": bool(password)}
    finally:
        password = None


def remove_portal_credential(path: Path) -> bool:
    path = Path(path)
    if not path.exists():
        return False
    path.unlink()
    return True


def _provision_interactively(path: Path) -> dict[str, str]:
    default_account = os.environ.get("USERNAME", "")
    entered = input(f"Portal帳號 [{default_account}]：").strip()
    account = entered or default_account
    first = getpass.getpass("Portal密碼：")
    second = getpass.getpass("再次輸入Portal密碼：")
    if first != second:
        raise CredentialStoreError("兩次密碼不一致，未保存。")
    try:
        return provision_portal_credential(path, account, first)
    finally:
        first = None
        second = None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="TransFiles Portal DPAPI憑證庫")
    parser.add_argument("action", choices=("provision", "validate", "remove"))
    parser.add_argument("--path", type=Path, default=None, help="覆寫憑證檔位置；不得放在Git或共用資料夾")
    args = parser.parse_args(argv)
    path = args.path or default_credential_path()
    try:
        if args.action == "provision":
            result = _provision_interactively(path)
            print(f"已保存Portal憑證：account={result['account']} path={result['path']}")
        elif args.action == "validate":
            result = validate_portal_credential(path)
            print(f"Portal憑證可解密：account={result['account']} path={path}")
        else:
            print("已移除Portal憑證。" if remove_portal_credential(path) else "Portal憑證不存在。")
        return 0
    except CredentialStoreError as exc:
        print(f"憑證操作失敗：{exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
