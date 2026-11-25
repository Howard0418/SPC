# 任務

- 功能 ID：`20261125-spc-navigation-etch-particle-improvements`
- 規格版本：2
- [規格](spec.md)｜[計畫](plan.md)
- 目前狀態：P0 權限一致化完成；兩件 P1 仍未實作。

## P0：安全與權限一致

- [x] `SPC-PERM-NAV-ALIGN-1125`：建立選單／權限目錄／路由的一對一頁面登錄，加入舊權限相容與儲存升級標記，驗證既有有效頁面不變及稽核不可自行加權。（R-001、R-002；AC-001、AC-002）

## P1：高頻資料錄入

- [ ] `PORTAL-ETCH-PASTE-1125`：Portal 咬蝕量日報 A/B side 厚度格線支援 Excel 矩形貼上、整次驗證與錯誤座標，保留現有 COM、計算、儲存及 SPC 同步契約。（R-003、R-004；AC-003、AC-004）

- [ ] `SPC-PARTICLE-PASTE-ENTRY-1125`：新增落塵監控資料輸入頁與獨立權限，支援雙層表頭、第三列起貼上、R1～R12 Long Format preview／confirm、重複保護及既有 C/U 分析回歸。（R-005～R-008；AC-005～AC-008；依賴 `SPC-PERM-NAV-ALIGN-1125`）

## 每件共同收尾

- [x] P0 已完成自動化、建置、Web `0.1.193`／API `0.1.177` 測試站發布、連線驗證、文件同步與回復備份。
- [ ] 完成對應自動化、建置、測試站 Web/API 版本遞增及連線驗證；正式發布另行授權。
- [ ] 更新 spec／verification、`docs/requirements.md`、`TODO.md`、`CHANGELOG_CUSTOM.md`，只提交該小工作必要檔案並推送目前分支。
