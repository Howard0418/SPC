# I18N-TASK-01 驗證及進度

- 日期：2026-10-22；環境：三套本機原始碼；規格版本：1。
- 進度：TASK-01 分析完成；TASK-02～21 未開始，等待使用者確認。
- 實作：無程式變更；DB／Migration／安裝／網站發布：均未執行。

| 驗收 | 方法與證據 | 結果 |
|---|---|---|
| AC-001 | 讀三套 AGENTS／SDD／需求／相關 changelog、KM progress；解析 SPC lock、三套 csproj、Layout／入口；列舉指定 UI 目錄 | 完成；版本及 35／9／72＋Mobile9／44 清單見 analysis／inventory |
| AC-002 | 檢查 User／Operator、KM Database／Models、Portal proxy／SSO、SPC Router／JWT、KM auth 及官方 localization 文件 | 分析完成；實庫未核對，跨站偏好方案未實作 |
| AC-003 | 檢查計算器／規則／Invariant 格式／圖表標籤／Excel／CSV、KM documents／LIKE 搜尋及 CanViewAsync | 分析完成；計算與 UI 尚未跑三語驗收 |
| AC-004 | 文件內容／本次連結存在性及 staged diff 範圍檢查 | 5份新文件／10個本機連結存在性通過，無替代字元，git diff --check通過；僅文件，無發布 |

## 未執行與限制

- 沒有三語功能可操作，故 build／程式測試／UI 截圖／測試站發布不適用本次文件工作。
- 未連線資料庫／正式站、不讀密碼、不寫測試資料；schema 現況僅能由程式／遷移來源推論。
- 來源目錄列舉非逐字人工文字審核；路由可達性、翻譯條目數、實際排版仍待後續各包。
- 翻譯準確率尚未測量，未宣稱 ≥97%；人工審核者及郵件／排程語言政策待定。
- 對來源找不到的功能只記錄未見，不推定已存在或保證不存在。

## 文件同步與回復

主規格及報告存 SPC，更新 TODO／docs/requirements／CHANGELOG_CUSTOM；Portal／KM 程式及文件不改。純文件可用後續修訂回復；網站及資料庫無改動可回復。完成後停止，不繼續 TASK-02。
