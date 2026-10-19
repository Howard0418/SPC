# 驗證
- C# 核心24項、metadata規則6項、PowerShell既有39＋新增5項通過。
- EXE 真實 SPC／Portal 發布來源產包成功；來源 API migration 靜態清單與既有 EF catalogue 完全一致（SPC50、Portal27）。
- EXE Test／Portal只讀查詢現行版本並產包成功；主畫面與製作視窗離線繪製檢視通過。
- 無 API assembly 執行、DB連線或網站更新；本次未Apply／Rollback。工具交付型變更，測試站發布不適用。
- 回復方式：使用前版EXE；既有應用、DB與交付包未變更。失敗的.incomplete目錄不作交付。
