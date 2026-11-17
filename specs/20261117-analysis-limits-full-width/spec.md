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

回復：發布前備份frontend，可還原整包。

## 正式發布計畫（2026-11-17使用者授權）

使用者確認全寬修正可以並授權正式發布。正式既有Web0.1.127／API0.1.126；來源9f76c060的既有正式build與目前index-CtYchrlm.js／index-BAUG2F0C.css相符。以該來源隔離產包，僅套入本次表格w-full及Web0.1.134，排除其餘尚未授權測試版修改及工作區TrendChart修改。

正式目標：PMR-SPC-SRV.pmr.com.tw／SpcWeb／C:/inetpub/wwwroot/production/frontend；僅靜態Web更新、不停止或更新API、不觸碰DB、設定及Portal。先備份並核對雜湊，資產先於index套用，失敗回復原index及原資產。核對Web首頁／主JS／CSS、前台版本及實際API基底https://PMR-SPC-SRV.pmr.com.tw:8443/api的health／version／production與瀏覽器Origin的CORS回應。測試站保持Web0.1.133。

- [x] 正式隔離build／回歸與受控Web發布。
- [x] 正式連線、原進度表及文件同步；提交上傳見Git紀錄。

### 正式執行結果

- 原正式JS／CSS SHA256與9f76c060既有build完全一致；隔離正式包只變更AnalysisLimitsOverviewView的w-full及套件版本。工具3項／Playwright2項、2863鍵×3及production build通過。
- 正式Web0.1.127→0.1.134，API0.1.126／production保持；測試仍Web0.1.133／API0.1.132。備份位於正式主機C:/SPC-Releases/limits-full-width-20261117-135718/backup-frontend，40個原檔雜湊核對通過，傳輸及套用雜湊一致，web.config／API DLL／API appsettings雜湊保持。沒有API重新啟動、migration或資料寫入。
- 正式首頁200/text/html；主JS index-9yX2EDJF.js為200/application/javascript且含前台綁定版本0.1.134，CSS index-BDMgng6u.css為200/text/css。實際API基底8443的health/version皆200、0.1.126／production；帶正式Web Origin的CORS通過，Chromium由正式同源資產文件fetch兩端點皆200，無TLS／CORS／用戶端埠阻擋。
- 未登入首頁正確導向Portal SSO；沒有使用正式帳號登入，故本次不宣稱正式登入後業務畫面已人工驗收。全寬子項已有使用者測試確認，父件其他AC-008驗收維持待辦。
- 原進度表小工作總表268列及405～408列正式里程碑已同步；18,323個非目標既有值及122個原物件保持，新增1個正式里程碑。備份、公式錯誤0、候選與取代後原檔OpenXmlErrors=0及重匯通過；Excel人工開啟／列印未執行。核心46件保持，不把父件整案誤標完成。

正式回復：將上述備份的index.html回復即可切回原資產；保留原hash資產且未刪除，API維持0.1.126。需完整回復時以同批備份覆蓋原frontend並重驗首頁／資產／API；本次未發生回復。
