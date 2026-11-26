# 任務

- 功能 ID：`20261125-spc-navigation-etch-particle-improvements`
- 規格版本：11
- [規格](spec.md)｜[計畫](plan.md)
- 目前狀態：八項小工作均完成測試、自動化與測試站發布；正式站未授權、未發布。

## P0：安全與權限一致

- [x] `SPC-PERM-NAV-ALIGN-1125`：建立選單／權限目錄／路由的一對一頁面登錄，加入舊權限相容與儲存升級標記，驗證既有有效頁面不變及稽核不可自行加權。（R-001、R-002；AC-001、AC-002）

## P1：高頻資料錄入

- [x] `PORTAL-ETCH-PASTE-1125`：Portal 咬蝕量日報 A/B side 厚度格線支援 Excel 矩形貼上、整次驗證與錯誤座標，保留現有 COM、計算、儲存及 SPC 同步契約。（R-003、R-004；AC-003、AC-004）

- [x] `SPC-PARTICLE-PASTE-ENTRY-1125`：新增落塵監控資料輸入頁與獨立權限，支援雙層表頭、第三列起貼上、R1～R12 Long Format preview／confirm、重複保護及既有 C/U 分析回歸。（R-005～R-008；AC-005～AC-008；依賴 `SPC-PERM-NAV-ALIGN-1125`）

## P2：輸入頁版面辨識

- [x] `SPC-PARTICLE-ENTRY-LAYOUT-1125`：在落塵監控資料輸入頁先顯示有框線的 R1～R12 量測位置配置圖，再顯示有框線的 Excel 貼上區；保留原貼上／預覽／確認行為。（R-009；AC-009；依賴 `SPC-PARTICLE-PASTE-ENTRY-1125`）

- [x] `SPC-PARTICLE-ENTRY-GRID-1125`：將 Excel 貼上區改為黃色雙層 49 欄格線，可在格線直接貼上並顯示對應資料；保留原 preview／confirm 行為。（R-010；AC-010；依賴 `SPC-PARTICLE-PASTE-ENTRY-1125`）

- [x] `SPC-PARTICLE-EXCEL-FORM-GRID-1125`：依指定 Excel 工作表 B24:BB 更新為 50 個邏輯欄位，支援 49／50／53 欄貼上、日期同格換行、備註、格內修正與 preview 後確認寫入。（R-011；AC-011；依賴 `SPC-PARTICLE-ENTRY-GRID-1125`）

- [x] `SPC-PARTICLE-GRID-PASTE-FOCUS-1126`：空白狀態顯示 50 個獨立格線；點任一空白格後格線取得焦點，Ctrl+V 可由 activeElement 進入既有 parser；已有草稿的多欄貼上亦可整批重貼。（R-012；AC-012；依賴 `SPC-PARTICLE-EXCEL-FORM-GRID-1125`）

- [x] `SPC-PARTICLE-EMPTY-INPUT-ALIGN-1126`：空白列 50 格各加入內縮白底輸入框外觀，方便對照日期、R1～R12 四粒徑及備註位置；保留整列貼上與既有儲存流程。（R-013；AC-013；依賴 `SPC-PARTICLE-GRID-PASTE-FOCUS-1126`）

## 每件共同收尾

- [x] P0 已完成自動化、建置、Web `0.1.193`／API `0.1.177` 測試站發布、連線驗證、文件同步與回復備份。
- [x] Portal 咬蝕貼上已完成自動化、建置、Web `1.0.364`／API `1.0.325` 測試站發布及連線驗證；正式未授權。
- [x] 落塵貼上完成對應自動化、建置、測試站 Web `0.1.194`／API `0.1.178` 版本遞增及連線驗證；正式發布另行授權。
- [x] 更新 spec／verification、`docs/requirements.md`、`TODO.md`、`CHANGELOG_CUSTOM.md`，只提交該小工作必要檔案並推送目前分支。
- [x] 配置圖版面完成 Chromium／三語／建置、SPC 測試站 Web `0.1.195` 版本遞增、靜態圖片與 Web／API 連線驗證；API 維持 `0.1.178/test`，正式發布另行授權。
- [x] 貼上格線完成 Chromium／三語／建置、SPC 測試站 Web 最終 `0.1.197` 版本遞增及 Web／API 連線驗證；API 維持 `0.1.178/test`，正式發布另行授權。
- [x] 指定 Excel 表單格線完成 parser 5 項、Particle 語系 3 項、Chromium 4 項、三語及 build；SPC 測試站 Web `0.1.198` 已發布，API 維持 `0.1.178/test`，正式發布另行授權。
- [x] 空白格線與真實貼上焦點完成 Chromium 4 項、Particle／parser 8 項、三語及 build；SPC 測試站 Web `0.1.199` 已發布，API 維持 `0.1.178/test`，正式發布另行授權。
- [x] 空白輸入框對齊完成 Chromium 4 項、Particle／parser 8 項、三語及 build；SPC 測試站 Web `0.1.200` 已發布，API 維持 `0.1.178/test`，正式發布另行授權。
