# 規劃驗證
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
