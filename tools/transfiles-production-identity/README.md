# 正式目標／環境防護測試
設定TRANSFILES_SOURCE為既有TransFiles來源目錄，使用其Python執行`tools/transfiles-production-identity/test_environment_preflight.py -q`。

4項測試涵蓋：test／production正向順序、8種錯置／缺失回應、反向環境錯置、網路失敗。GET `/version`不得帶Authorization或帳密；失敗時不建立Portal client／login／token／主檔讀取，不呼叫preview／confirm。

全程Mock，不發HTTP、不登入、不觸及任何資料庫。這只是實際環境防護測試，不能證明正式身分、DB角色或無人值守認證已可使用。[TASK-001](../../specs/20261028-transfiles-production-identity/spec.md)。
