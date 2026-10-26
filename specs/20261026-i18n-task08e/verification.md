# I18N-TASK-08E 驗證與回復

2026-10-27 DONE。2026-10-26先建核心測試，當時缺少workbook helper及metadata等8個編譯錯誤為預期Red；完成實作後通過。依10-27最新授權只完成SPC，其他系統與共用驗收維持暫停。

## 驗收結果
- R1/R2/R3：`ReportLocalizationTests`16項（含三語workbook、CPK、popup query、郵件、fake controller及非法metadata），加原排程3、既有郵件10、隔離HTTP6，合計35項通過。使用InMemory／fake sender，不連正式DB、不真寄送。
- Workbook驗證工作表／表頭、25欄序、數值cell type、0／null規格、raw原名／備註、公式前綴仍為文字、freeze/style保持。穩定chart kind及計算方式metadata僅於顯示時翻譯，非法metadata回原文。
- CPK驗證0筆、1筆、零變異、1.33／1.0／0.97門檻與原顏色、原料號與名稱、精度及檔名；原資料結果／資料庫不增寫。原月份未篩量測的問題另列REPORT-DATA-VERIFY-001，維持相容不代表認可其業務正確性。
- R4：手動寄送依明示locale、背景預設繁中；HTML編碼姓名、保留附件bytes／檔名／API displayType與periodLabel／結果、已知false安全metadata，未知錯誤保留。原排程LastSent與失敗重試3項通過。
- 前端Node語系測試121項通過；2733鍵×3語完整與參數／protected tokens／draft hash一致。新增62鍵、186列draft，真人審稿未執行，不冒稱審核完成。
- 前端本機瀏覽器報表3＋CPK popup1＋失敗即時切語1，5項通過。下載期間／DTO／原檔名及原bytes、切語無新寄信請求、非法metadata仍為原文。
- 藥液59項、校驗內容及隔離郵件HTTP18項通過（後者10-27補驗）；全體校驗另有CAL-VERIFY-001／002既有差異，本批未宣稱整套全綠，未改權限或補造原始工作簿。
- build:test與Release dotnet publish通過。機器CSV／匯入、Migration controller來源與HEAD相同，未改格式、schema、計算公式或SQL篩選。

## 測試站發布
- 唯讀preflight：環境test，172.16.110.16／PMR_SPC_TEST，pending migrations=[]。
- 實際IIS SpcSingleTest/的physicalPath為D:/SPC/release/test/backend，pool SpcSingleTest；只停止該pool完成完整前後端備份後覆蓋94 runtime檔，前端assets先、index最後。4個設定／資料保護檔雜湊不變；pool已恢復。
- 發布版本Web0.1.107／API0.1.85。真實HTTPS /api/version及/api/health皆200、test／ok；4個前端檔案SHA256與dist完全一致。
- 發布後上述5個報表瀏覽器驗收全通過，所有業務API隔離fixture、不真寄信；同源路由14項通過。
- 正式站未更新。無schema migration、無業務DB寫入。

## Rollback
- 完整原版本備份：D:/SPC/.codex-tmp/i18n-task08e/backup/backend及frontend，備份逐檔SHA256已核對；對應Web0.1.106／API0.1.84。
- 需要回復時先再核對同一IIS test physicalPath／pool與AppEnvironment=test，僅停SpcSingleTest，將上述完整備份還原至同一release/test前後端，保留設定／資料保護檔，重啟後驗version／health及前端雜湊；不操作正式站、不需DB回復。本次未演練覆寫回退。
- 本機檢查與發布證據留於ignored .codex-tmp/i18n-task08e與對應Playwright輸出，未提交產物、機敏設定或證據JSON。

## 待辦
SPC三語化開發批次完成；TASK09～21共13件（Portal6／KM4／共用3）依使用者指示維持暫停。TASK20仍需真人三語審稿，TASK21最終跨站交付仍未完成。正式發布另行授權。
