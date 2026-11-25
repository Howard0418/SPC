# SPC 檢視者英文快速指南英文畫面更新驗證

- 工作代號：`SPC-DOC-VIEWER-QUICK-EN-SCREENSHOT-1125`
- 驗證日期：2026-11-25
- 結果：通過

## 交付結果

- 交付檔：`SPC_Viewer_Quick_Guide_EN_V1.0.docx`
- 頁數：8 頁
- 圖片：7 張正式 SPC 英文模式畫面
- 內容：管制圖、趨勢、週月報、品質資料查詢及 OOS／OOC 唯讀查閱。
- SHA-256：`49348E8B59737BB761B43CEA415979770CA0E03BFC07F8778D1BD1C78F071858`

## 驗證紀錄

| 項目 | 結果 |
|---|---|
| DOCX 封裝完整性 | 通過；ZIP `testzip` 無錯誤 |
| 頁數與圖片數 | 通過；8 頁、7 個 inline shapes、7 個 media |
| 全頁渲染 | 通過；LibreOffice／Poppler 產生 8 張頁面 PNG |
| 逐頁目視 | 通過；無裁切、重疊、溢位、空白頁或不可讀內容 |
| 英文內容 | 通過；文件本文 CJK 字元數 0，必要章節皆存在 |
| 唯讀角色界線 | 通過；禁止自行授權、異常確認、主檔修改與來源資料覆寫 |
| 機敏資訊 | 通過；未發現姓名、帳號、公司郵件、SMTP 主機或本機路徑 |
| 外部連結 | 通過；DOCX relationships 無 `TargetMode="External"` |
| 交付一致性 | 通過；工作檔與 Voice 交付檔 SHA-256 相同 |

## 發布狀態

- 文件型工作，程式 build、網站發布及資料庫發布不適用。
- 未修改正式站、資料、權限、通知或設定。

## 回復方式

- 如需回復，以工作暫存區的 `SPC_Viewer_Quick_Guide_EN_V1.0.before.docx` 覆蓋交付檔。
