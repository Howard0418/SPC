# 技術計畫
- 功能 ID：SPC-CHEM-REFERENCE-HISTORY-20261019；版本1；[規格](spec.md)

## 系統責任與受影響檔案
SPC前端兩頁ChemicalAnalysisOverviewView.vue／PartProcessCharacteristicsView.vue、必要API DTO／版本服務長度驗證及對應tests。沿用PUT與既有chemical-analysis-formula-versions GET及restore，不新增另一資料來源。

## 實作安排
1. 先核對實際部署與原碼差異，確認已存在部分與缺口；依規範先在ai_docs/10_change_log.md記錄實作目的，核心驗證先補測試。
2. 共用既有Reason上限500與PPC ID；設定頁新增輸入與payload，總覽補只讀歷史入口、空／錯誤狀態與目前版本依據展示，保留既有回復流程。
3. 前後端超長拒絕、備註重置、未變公式提示及批次部分失敗回歸；若現有行為有缺口，只處理本工作必要檔案。

## 相容性、驗證及風險
- DB影響：預期無migration，沿用Reason nvarchar(500)，不改既有歷史。
- Happy Path：兩頁交替修改、同一版本來源／備註，對應AC-001／002／005。
- Boundary：0／500／501字、空歷史、僅備註異動、分頁切換與多筆部分成功，對應AC-002～004。
- Invalid Input：長度、無效JSON、錯誤PPC／非CHEM、未授權、HTML文字及API失敗，對應AC-003～005。
- Regression：版本連續／回復、公式數值、批次草稿、F表不受影響；執行相關版本服務／Controller測試、前端行為測試、必要backend build及frontend build，再做兩入口人工驗收。

## 發布與回復
本次僅規劃，無建置／部署。未來此單一小工作完成必要驗證後依規範發布SPC release/test，核對實際測試目標；不發布D:/Sites/PmrPortal。正式發布需另行授權，不沿用前次版本發布授權。
發布前備份相關前後端產物與設定；失敗回復同批程式備份，資料不刪除、不整庫回復；歷史新增保留。若出現migration先停下更新計畫，不與其他DB／IIS高風險工作並行。

## 實際執行更新
2026-10-19使用者確認後完成本案。無migration，測試發布SpcSingleTest（D:/SPC/release/test/backend）與同級frontend，API0.1.61／Web0.1.64。實際結果及限制見[驗證](verification.md)。
