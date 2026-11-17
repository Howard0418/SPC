# 規劃驗證

## 2026-11-17 TASK-002再次規劃確認
- 本輪需求與既有TASK-002相同，核對現行SPC總覽兩式欄位為adjustmentFormula／adjustmentAmountFormula；補明表頭、展示格、編輯控制與保存原值的驗收範圍。
- 沿用同代號及P2待辦，不新增工作、不變更優先度／實作／發布／驗收狀態，不設定開工日期；既有中央進度表306的規劃入池狀態保持。此次僅文件釐清，進度表及原生里程碑更新不適用，不補造新的規劃事件或宣稱本輪改寫Excel。
- 內容及連結檢查通過；本輪未附新截圖。應用程式build、回歸、網站發布均未執行（僅規劃，不適用）。未完成總數保持46件。
- 2026-11-17已定位Portal Pages/Spc/VariableMeasurementEntry.cshtml靜態／動態查詢、批次表頭、單筆與確認區；SPC ChemicalAnalysisOverviewView.vue調整公式兩欄。
- 與既有初複驗、調整計算及保存需求比對，規劃只隱藏呈現，不覆蓋已確認計算行為。
- 本次沒有新截圖，需求格式可用；工作為P2，排於使用阻擋、安全及資料正確性工作後，不立即實作。
- 內容／連結檢查通過；build、應用程式測試、發布未執行，規劃階段不適用。
- 原進度表已備份並同步小工作305／306與11/17原生規劃里程碑，僅標規劃入池，不標實作／發布完成。原116圖形名稱保留，新增1個規劃里程碑；17,400個非本件既有儲存格值保持，公式錯誤0、候選與原檔OpenXML0錯誤、重讀／渲染通過。未執行Excel人工開檔或列印。
- 核心未完成45件、Portal相關17件；本件兩項P2不自動開始實作，其他暫停工作保持。

## TASK-001測試區發布（2026-11-17）
- 使用者追加授權先執行後，依順序只實作Portal新增／查詢。歷史規劃「未實作」以本節為準；002保持規劃。
- 專用screen class隱藏查詢表頭及資料格、新增單筆及批次、確認資訊；單筆預覽摘要不再列兩項。保留DOM控制項、所有計算與保存payload，print保留原欄位。
- 新可見性測試先重現12個可見批次表頭而非10；修正後37/37相關行為回歸通過，涵蓋保存／複驗／日期與班別隔離／歷史查詢。補充單筆及確認區computed display檢查後聚焦2/2通過；歷史screen14欄、print16欄；初驗調整值在複驗與confirm payload仍保持。
- 從乾淨HEAD來源只加入本件cshtml，用scripts/publish-all.ps1發布測試API1.0.248／Web1.0.287，Release建置通過；Web頁面版本/app-version實際1.0.287.0，前台版本已更新。
- 測試Login200、site.css200/text/css、site.js200/text/javascript；同源/api/auth/me匿名401確認通道可達；實際Web上游http://172.16.110.27:8091/health=200/ok/test，已部署API DLL版本1.0.248.0。
- API／Web的appsettings.json及web.config前後雜湊相同；更新前備份D:/PmrPortal/.hide-adjust-001-20261117/backup-api（246）、backup-web（286）。回復採備份程式payload並保留環境設定及資料；正式未發布。
- 原進度表小工作305與Portal摘要／原生測試里程碑同步，306仍已規劃。原117圖形名稱保持、新增1個；17,564個非本件儲存格值一致，公式0錯誤、候選與原檔OpenXML0錯誤，重讀／渲染通過；未做Excel人工開檔與列印。
- 使用者實際登入測試畫面待確認，未宣稱實站本人驗收或三語全部逐頁人工驗收；核心45／Portal相關17保持。

## TASK-002測試發布（2026-11-17）
- 使用者接續指示先執行，原先僅規劃狀態由本節取代；只改ChemicalAnalysisOverviewView.vue scoped screen樣式，隱藏第6／7欄表頭、展示格及輸入控制，未修改任何計算、draft、payload或匯出程式。
- Playwright 11/11通過，1280／390寬度總覽顯示9欄；編輯時調整兩式不可見，濃度公式正常可編輯。保存後payload保留兩式、複驗公式及未知legacyKeep物件，濃度新值正確；print仍為11欄，原匯出功能不改。
- 初輪測試誤以為保存後自動退出編輯而尋找pre，現行行為仍保持編輯；已修正測試為先核對textarea再點完成。曾因兩輪測試共用server、首輪關閉造成連線失敗，已改單輪完整執行並11/11通過，不把該次失敗記為通過。
- 乾淨HEAD隔離來源只加入本件Vue及package版本，排除既有TrendChartView修改。npm run build:test與2863×3語系檢查通過；既有chunk-size／Browserslist資料年齡提示保留，未升級套件。
- 測試Web升0.1.131，已更新D:/SPC/release/test/frontend；更新前備份D:/SPC/.hide-adjust-002-20261117/backup-frontend，回復採該payload。API無修改，維持0.1.132。
- 實際https://SPCTEST.pmr.com.tw首頁200/text/html；主JS index-BiEo5pnE.js為200/application/javascript並含前台版本0.1.131，CSS index-BykfK_2R.css為200/text/css。前端實際同源/api/health與/api/version均200，API0.1.132／environment=test。正式未發布。
- 原進度表小工作306及原生測試里程碑同步；原119圖形名稱保持、新增1個。17,874個非本件儲存格值一致、公式0錯誤、候選及原檔OpenXML0錯誤，重讀／渲染通過；未做Excel人工開檔或列印。
- 核心未完成46件／Portal相關17件保持，本件仍待使用者登入測試總覽畫面確認，不宣稱本人已驗收。

## TASK-002全寬修正（2026-11-17）
- 使用者截圖指出表格右側空白；原因為隱藏兩欄後table仍採自動內容寬度。screen表格新增width100%，保留960px最小寬及外層overflow-x-auto；print及計算／保存程式未改。
- Playwright11/11通過；新增1920px viewport量測table寬度與父容器clientWidth差≤2px，搭配1280／390既有兩欄隱藏、濃度編輯保存、公式原值與print回歸。
- 隔離乾淨HEAD只加入本件Vue及版本，test build與2863×3語系檢查通過；既有chunk-size／Browserslist提示保留，不改無關套件。
- 測試Web更新0.1.132，備份D:/SPC/.hide-adjust-002-full-width-20261117/backup-frontend。首頁200/text/html、主JS index-DehbGvSi.js=200/application/javascript且含前台0.1.132，CSS index-4q7c1vbI.css=200/text/css；實際同源/api/health及/api/version=200，API0.1.132／test，無後端更新。
- 原進度表既有工作306補最新版本與寬度驗證，新增原生寬度修正里程碑；原120圖形名稱保持，18,027個非本件儲存格值一致，公式0錯誤，候選及原檔OpenXML0錯誤，重讀／渲染通過。未做Excel人工開檔／列印。
- 同一工作延續，不重複新增或核銷；核心46／Portal相關17保持。待使用者畫面確認，正式未發布。

## TASK-001正式發布與驗收（2026-11-17）
- 使用者確認 Portal 藥液新增／查詢頁隱藏調整與調整量可驗收並授權正式；TASK-002 SPC 總覽未納入本次發布。
- 由正式 API1.0.249／Web1.0.274 隔離基準套入 TASK-001；聚焦畫面2／2、正式基準API47／47、相容性16／16及Release publish通過。桌面／手機兩欄不可見、DOM與確認payload仍保留、print仍顯示原欄。
- 唯讀預檢通過後發布正式 API1.0.250／Web1.0.275；無 migration。正式 Login、site.css、site.js、API health均200且MIME正確，`/app-version`為1.0.275.0、API為production、匿名`/api/auth/me`為401。
- 正式API／Web DLL版本與候選SHA-256完全相符；資料庫備份`verified=true`。回復來源為正式發布目錄`C:/ProgramData/PMR-Releases/Production/portal-hide-chemical-adjust-20261117-r1`，可回復API1.0.249／Web1.0.274。
- TASK-001結案後核心未完成45件／Portal相關16件；TASK-002仍待畫面驗收與正式授權。
