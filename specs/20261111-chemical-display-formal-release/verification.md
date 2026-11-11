# 驗證紀錄

- 功能 ID：`PORTAL-CHEM-RAW-INPUT-1111-FORMAL-RELEASE`
- 規格版本：1
- 日期／環境：2026-11-11／Production
- 實作狀態：已發布
- 驗證狀態：技術驗證通過；登入後業務畫面待驗
- 發布狀態：SPC API 0.1.125／Web 0.1.127、Portal API 1.0.230／Web 1.0.271 已發布至正式站

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 建立 bundle 並執行 Production Plan | 主機、IIS、DB、migration history 與 manifest 一致 | SPC、Portal 正式 Plan 均通過；白名單主機、Pool、目錄及 DB 符合 manifest。 | 通過 |
| AC-002 | Apply 前後備份、hash、設定與資料快照核對 | 設定與業務資料不變 | 兩站均完成受控備份、COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY；依授權套用 SPC 三項與 Portal 一項既有 migration。未執行量測、Receipt／Trace、公式、F 表、規格或匯入資料寫入。 | 通過 |
| AC-003 | 正式 Portal 載入已存日報 | 原始滴定值及第二輸入欄位符合規則 | 正式版本已部署；仍需由具權限使用者載入一份已存日報，確認原始值與單／雙輸入欄位。 | 待人工驗收 |
| AC-004 | 正式 Web／資產／API 實際連線 | HTTP、MIME、版本與環境皆正確 | 2026-11-11：Portal Login 200（HTML）／app-version 200 `1.0.271.0`／health 200、DB environment=`production`；SPC Web 200、主 JS `application/javascript`、CSS `text/css`、health/version 200 `0.1.125`、environment=`production`。瀏覽器登入頁亦顯示「正式環境 v1.0.271」。 | 通過 |

## 未執行項目、限制及後續

- 使用者登入後的日報畫面驗收尚待執行；此為唯一未完成驗收，不影響已完成的發布與連線驗證。

## 基準與變更紀錄更新位置

- 發布完成或受阻後更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md` 及進度表。
- 進度表已同步：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/進度表.xlsx` 的「小工作總表」與「專案進度表」均改為正式已發布、登入後日報畫面待人工驗收；更新前備份為 `備份/進度表_20261111_正式藥液日報發布前.xlsx`。ZIP 封裝、必要 OpenXML parts、公式錯誤掃描與回讀均通過；未做 Microsoft Excel 人工開啟。
