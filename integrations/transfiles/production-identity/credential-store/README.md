## Portal CurrentUser DPAPI credential store

- Source delivery for `TRANSFILES-PROD-1028-TASK-001`; deployed PowerShell SHA-256: `E1D093C76BA488E5F480288B6FBDFE20BEAEC965211DD10FE8C76D713C714D39`.
- Formal host tool: `C:\ProgramData\PMR\TransFiles\credential-tools\Provision-TransFilesPortalCredential.ps1`.
- Interactive provision command (run locally on PMR-SPC-SRV as `PMR\ihao_ting`):
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\ProgramData\PMR\TransFiles\credential-tools\Provision-TransFilesPortalCredential.ps1 -Action Provision`
- Validate uses `-Action Validate`; rollback uses `-Action Remove`. No command prints the password or ciphertext.
- Default credential file is `%LOCALAPPDATA%\PMR\TransFiles\portal.credential.json`, encrypted with Windows DPAPI CurrentUser plus purpose entropy and ACL-limited to the current identity and SYSTEM.
- Tests: `python -m unittest test_portal_credential_store.py`; 7 passed including Python-to-Windows-PowerShell DPAPI compatibility.
