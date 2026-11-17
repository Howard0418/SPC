# 分析項目界限總覽全寬修正

日期：2026-11-17；主系統：SPC；追蹤：SPC-LIMITS-BULK-1104-TASK-001 的畫面子項。

## 需求與計畫

依使用者截圖，表格只有 min-width，寬螢幕時未填滿容器。僅為 AnalysisLimitsOverviewView 表格加入 width:100%，保留1380px最小寬、手機橫向捲動、13欄、六界限編輯／預檢／儲存。Portal、API、DB、公式與權限不變；不新增重複核心工作。

## BDD 與驗證規劃

- Given 1920px桌面 When 開啟總覽 Then 表格與容器內寬差不超過2px。
- Given 390px手機 When 開啟總覽 Then 表格最小寬1380px且容器可橫向捲動。
- Given 跨頁修改六界限 When 預檢並套用 Then 原批次流程保持；非法95%仍阻止套用。
- Happy Path／Boundary由上述桌機、手機覆蓋；Invalid Input及Regression沿用既有Playwright批次測試。

## 任務與發布

- [x] 版面、既有測試補充與test build。
- [x] 測試Web由0.1.132升0.1.133，API維持0.1.132；驗證首頁／主資產／同源API及test環境。
- [x] 同步文件、原進度表。父件既有AC-008未因本次改版自動完成。

## 實際驗證

2026-11-17：前端工具3／3、Playwright2／2通過，涵蓋1920px桌機表格與容器差≤2px、390px手機1380px最小寬及橫向捲動、跨頁兩筆預檢／套用與95%格式拒絕。i18n 2863鍵×3及隔離來源test build通過，排除工作區既有TrendChart修改。

測試站 https://SPCTEST.pmr.com.tw 首頁200/text/html；目前index-IwElEUs9.js為200/application/javascript且含Web0.1.133，index-m59bBhiY.css為200/text/css；瀏覽器實際同源/api/health及/api/version均200、API0.1.132／test。無API部署，正式未更新。

原進度表已備份並更新小工作總表第268列及原生測試里程碑401～404列；18,175個非目標既有儲存格值保持、121個原物件保留並新增1個里程碑，候選與取代後原檔OpenXmlErrors=0，重匯讀回一致。Excel人工開啟／列印未執行。父件保持待畫面驗收，核心46件不變。

回復點：D:/SPC/.analysis-limits-full-width-20261117/backup-frontend（Web0.1.132）。原始Excel備份留同來源備份目錄，驗證輸出與發布產物不提交。

回復：發布前備份測試frontend，可還原整包；正式未授權，不更新。
