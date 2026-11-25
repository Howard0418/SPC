# SPC 編輯者英文快速指南英文畫面更新驗證

- 工作代號：`SPC-DOC-EDITOR-QUICK-EN-SCREENSHOT-1125`
- 驗證日期：2026-11-25
- 結果：通過

## 交付結果

- 交付檔：`SPC_Editor_Quick_Guide_EN_V1.0.docx`
- 頁數：9 頁
- 圖片：8 張
- SPC 畫面：正式站英文模式的管制圖、趨勢、週月報及異常處理畫面。
- Portal 畫面：2 張有明確標示的英文文件視圖；圖片上方及圖說均註明為文件用途，未宣稱正式 Portal 已完整英文化。
- SHA-256：`C5C6665512D8A304358C14DF04A5742C71F22E61B801B3FED3B6B4A99D065CE2`

## 驗證紀錄

| 項目 | 結果 |
|---|---|
| DOCX 封裝完整性 | 通過；ZIP `testzip` 無錯誤 |
| 頁數與圖片數 | 通過；9 頁、8 個 inline shapes、8 個 media |
| 全頁渲染 | 通過；LibreOffice／Poppler 產生 9 張頁面 PNG |
| 逐頁目視 | 通過；無裁切、重疊、溢位、空白頁或不可讀內容 |
| 英文內容 | 通過；文件本文 CJK 字元數 0，必要章節皆存在 |
| Portal 畫面揭露 | 通過；兩張文件用英文視圖均有明確標示 |
| 機敏資訊 | 通過；未發現姓名、帳號、公司郵件、SMTP 主機或本機路徑 |
| 外部連結 | 通過；DOCX relationships 無 `TargetMode="External"` |
| 交付一致性 | 通過；工作檔與 Voice 交付檔 SHA-256 相同 |

## 發布狀態

- 文件型工作，程式 build、網站發布及資料庫發布不適用。
- 未修改正式站、資料、權限、通知或設定。

## 回復方式

- 如需回復，以工作暫存區的 `SPC_Editor_Quick_Guide_EN_V1.0.before.docx` 覆蓋交付檔。
