# SPC 日期公式示範資料工具
只允許 `PMR_SPC_TEST`，以獨立`DEMO-FORMULA-20261021`主檔示範12日/兩公式/兩規格段/一異常。使用既有服務，不直接偽造計算憑證，無migration或角色變更、無真實寄信、不刪資料。

從SPC根目錄執行（設定檔不提交）：
```powershell
dotnet run --project tools/testing/SpcFormulaDemo -- --write-test-demo D:/SPC/release/test/backend/appsettings.json
```
工具會對既有資料寫入，只在使用者授權建立示範時執行。固定代碼及每日requestId提供重跑保護；已有requestId略過，不用重跑來覆蓋使用者後續修改。執行輸出含示範ID及圖表，請導向ignored staging，不提交產物。

操作與清除限制：[驗證文件](../../../specs/20261021-formula-chart-demo/verification.md)。本工具呼叫PortalConfirm服務，並非TransFiles直接DB流程驗收。
