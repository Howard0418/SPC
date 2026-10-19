# SPC＋Portal 正式發布
- 功能 ID：SPC-PORTAL-PRODUCTION-RELEASE-20261019
- 日期：2026-10-19；狀態：正式發布完成（登入後人工操作待使用者驗收）
- 授權：使用者要求發布 SPC 與 Portal，並明確選擇「正式站（SPC＋Portal）」；既有授權持續，不重複要求相同發布授權。
- 主系統：SPC；相依：Portal；KM 本次不發布。
- 範圍：已確認功能的正式發布前核對、必要測試、可追溯套件、備份、環境設定保留、發布及 smoke/rollback。
- 資料庫：先唯讀比對正式 schema/migration 與來源；不將應用程式發布授權解讀為未知正式資料修補或未評估 migration 授權。若有待套用 migration，先列具體影響與備份/回復方式。
- 發布：遠端已驗證的正式 IIS；不使用本機測試站路徑，不使用 D:\Sites\PmrPortal。
- 現行更新工具為 SPC 非正式演練原型，仍保留 Production/Portal 目標防護；不更名環境或解除防護來規避限制。Portal 套件仍以 publish-all.ps1 產生至隔離 staging，再以核對目標的備份/部署流程處理。

## BDD
Given 使用者已授權正式發布 When 核對實際遠端 IIS/版本/DB Then 只選中正式站且列出部署順序、保留設定與回復方式。
Given 發布前必要驗證通過且相容性確認 When 套用套件 Then 先備份、保留環境與附件設定、核對來源 hash，再完成 API/首頁/登入 smoke。
Given 目標錯誤、備份失敗或未評估正式 migration When 發布流程執行 Then 停止覆寫，不以測試站或舊封存替代正式目標。
Given smoke 失敗 When 回復 Then 依同批備份回復並記錄結果，不刪除正式業務資料。

## 測試規劃
Happy Path：四組 API/Web 套件、版本、健康/登入；Boundary：既有未提交修正與遠端舊版本差異；Invalid：錯誤目標/缺備份/hash不同；Regression：登入SSO、取樣階段、咬蝕、正式資料/附件引用與自動 migration。

## 發布測試閘門修正
Portal 全量測試 12 失敗已定位測試資料/環境落差；允許僅調整驗證fixture、JSON斷言的Unicode表示與測試主機隔離，不修改產品權限或業務邏輯。修正後須全量重跑，不以略過失敗案例方式發布。

## 2026-10-19 使用者決定
使用者回覆「暫不更新正式資料庫」。因此不套用三筆 migration，也不啟動會自動遷移的新版正式應用；SPC/Portal 正式站均未覆寫。正式發布授權不代表已取得 DB 更新授權。已準備的隔離包保留，未標示發布完成。

## 2026-10-19 授權更正
使用者明確更正「我點錯了，要更新正式資料庫」，授權上述三筆 migration 與必要備份、發布、啟動及驗證；先前暫緩決定由此取代。不得擴張為其他資料修補。
