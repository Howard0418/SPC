# SPC正式最新版核對
- 功能ID：SPC-PRODUCTION-LATEST-CHECK-20261019；狀態：DONE（確認正式已是最新，無需重新部署）。
- 授權：使用者指定正式站、目前已驗證最新版，同意正式更新。主系統SPC，Portal／TransFiles不變。
- 判定原則：先核對實際版本、已驗證來源／產物及環境，若正式已是最新則不以較舊測試包降版，不為重新發布任意遞增版本。

## 核對與結果（2026-10-19）
- 正式主機PMR-SPC-SRV；SpcApi／SpcWeb均Started，路徑C:/inetpub/wwwroot/production/backend、frontend正確。
- 正式API DLL FileVersion0.1.60.0，實際API /api/version為0.1.60，/api/health回200。
- 正式API主DLL hash與前次已驗證交付包一致；前端交付包4個非環境設定檔（含index及其資產）逐檔hash全部相同，對應已驗證Web0.1.63。檔案hash只在記憶體核對，未提交證據JSON或設定。
- 來源git diff be89ae94..HEAD -- backend/MesSpc.Api frontend/mes-spc-web無差異；後續變更為工具／文件等，不代表有新版SPC產品可發布。
- 測試站https://SPCTEST.pmr.com.tw/api/version與發布DLL為0.1.59；測試前端index引用的實際bundle版本字面值0.1.62。測試環境較正式舊，不將其套到正式。
- 前次正式發布及驗證參考：[紀錄](../20261019-spc-portal-production-release/verification.md)。已驗證正式包標示API0.1.60／Web0.1.63。

## 驗收／任務
- [x] Given正式更新授權 When比較實際正式與已驗證交付包 Then確認正式已是最新，無需要套用的新內容。
- [x] Given測試版本較正式舊 When挑選發布來源 Then拒絕將較舊測試包視為最新版，不停止服務或降版。
- [x] 記錄實際版本、健康、來源與產物一致性，完成文件檢查與提交上傳。

## 發布、限制與回復
本次僅唯讀核對與文件：未重新部署／重啟IIS、未執行DB遷移、資料修補或備份，原正式版本保持不變；build及程式測試不適用。文件內容與連結檢查完成。無應用變更需rollback；前次正式備份保留。未修改測試站以追齊正式版本，亦未執行真人登入業務驗收。
