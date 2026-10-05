# 功能規格：週月報表排行、AD/工號登入與 Cpk 公式
- 功能 ID：20260909-report-ranking-login-cpk
- 版本：1
- 狀態：實作中
- 涉及專案：SPC、PmrPortal
- 授權依據：2026-09-09 使用者需求；僅測試環境驗證與發布，正式環境不發布。
- 相關基準：SPC 現行需求基準及 2026-09-08 SPC/Portal SSO 變更紀錄。

## 目的、現況與證據
- 週月報表總表已有 OOS、Cpk（前端欄位對應後端 Ppk）欄位排序，但沒有獨立排行區塊。
- SPC Dashboard 已有低 Cpk 圖表，但不是週月報表，也沒有 OOS 排行切換。
- Cpk 計算在 `ProcessCapabilityCalculator` 已存在，雙邊規格使用組內標準差 sigmaWithin；Ppk 使用整體標準差 sigmaOverall。公式與 sigma 來源目前未在週月報表完整呈現。
- Portal 登入先以 Users.EmployeeNo 對應 AD 帳號，再以 AD 驗證；SPC 由 Portal SSO 傳遞 AD 帳號與工號。需強化識別值正規化與 SSO 相容性，保留既有登入流程。

## 範圍與非範圍
範圍：
- 週月報表新增 OOS/Cpk 指標與前 5/後 3 排行切換，沿用目前查詢結果，不新增資料表。
- 週月報表顯示 Cpk/Ppk 計算公式、平均值、sigma 來源與規格條件，明確標示目前報表的 Cpk 實際採用 Ppk 數值。
- 強化 Portal 工號/AD 帳號正規化及 SPC SSO 的識別欄位相容，避免 `DOMAIN\\account`、`account@domain`、大小寫與空白造成失敗。
- 登入成功與失敗維持既有稽核。

非範圍：
- 不修改 Cpk/Ppk 數學定義或既有資料。
- 不新增正式環境設定、資料庫 migration 或正式發布。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 週月報表可依 OOS 或 Cpk 排名，選擇高值前 5 或低值後 3。 | AC-001 | 週報/月報查詢後可切換四種排行模式，排名名稱、數值與資料筆數正確，空值置後。 |
| R-002 | 週月報表說明 Cpk/Ppk 計算方式與實際採用數值來源。 | AC-002 | 畫面顯示雙邊/單邊公式、平均值、sigmaWithin/sigmaOverall 定義及目前 Cpk 卡片採用 Ppk 的說明。 |
| R-003 | Portal 與 SPC 入口接受 AD 帳號或工號，格式差異不應阻擋既有帳號。 | AC-003 | `account`、`DOMAIN\\account`、`account@domain` 與工號在相同測試帳號下可完成 Portal 登入及 SPC SSO；重複/未授權帳號仍明確拒絕。 |

## 假設與邊界
- 排行只針對目前週/月查詢回傳的總表資料，不重新掃描資料庫。
- Cpk 顯示沿用現有需求：畫面標籤為 Cpk，但數值來源是後端 Ppk；公式區同時說明統計意義，避免混淆。
- 若沒有 USL/LSL 或有效能力值，排行顯示未提供並置於最後。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-09-09 | 初版。 |
