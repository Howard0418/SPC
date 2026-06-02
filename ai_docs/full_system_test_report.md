# Full System Test Report (FPC Smart Factory SPC)

## 1. 測試摘要
* **測試日期**：2026-05-31
* **測試範圍**：SPC 計算核心、匯入邏輯、Traceability (產品系譜圖)、Dashboard 柏拉圖。
* **測試環境**：本機開發環境 (.NET 8 API + Vite Web) + SQLite/SQL Server。
* **測試人員 / Agent**：Antigravity Test Architect
* **測試版本**：Phase 1 (v1.0.0-beta)

> [!SUCCESS]
> **自動化執行狀態：已順利連動**
> 本報告已涵蓋最新的 `MES.SPC.E2ETests` Playwright 自動化端到端測試結果。稍早發生的 `ProductUiTests.cs` 選擇器 (Locator) 失效問題，已成功修正並通過驗證。

---

## 2. 測試結果統計

| 類別 | 測試數 | 通過 | 失敗 | 阻塞 | 備註 |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **基礎功能** | 6 | 5 | 0 | 1 | 權限角色隔離功能尚未實作 (阻塞) |
| **SPC 核心** | 4 | 3 | 0 | 1 | 計數型圖表 (P/NP/C/U) 前端顯示不全 (阻塞) |
| **匯入機制** | 5 | 5 | 0 | 0 | Excel/CSV 解析穩定 |
| **Traceability** | 3 | 3 | 0 | 0 | 槽位追溯功能正常 |
| **Dashboard** | 4 | 4 | 0 | 0 | 柏拉圖與異常排行運作良好 |
| **異常處理** | 4 | 3 | 1 | 0 | 大量資料併發匯入時偶有效能瓶頸 (失敗) |
| **總計** | **26** | **23** | **1** | **2** | 整體良率約 88% |

---

## 3. 已執行測試清單 (重點摘錄)

| 測試編號 | 測試名稱 | 測試目的 | 測試結果 | 錯誤訊息 | 相關檔案 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| TC-BAS-004 | 新增主檔 | 確保品質參數可被正確寫入 | ✅ 通過 | 無 | `ProductUiTests.cs` |
| TC-SPC-001 | Xbar-R 管制圖 | 確保公式與 Minitab 一致 | ✅ 通過 | 無 | `SpcChartUiTests.cs` |
| TC-SPC-002 | 計數型圖表 | P/NP 圖表前端動態切換 | ⚠️ 阻塞 | 前端尚未實作計數型專用視圖 | `DashboardView.vue` |
| TC-VAR-001 | Excel 匯入 | 解析雙層表頭之計量資料 | ✅ 通過 | 無 | `AttributeUploadUiTests.cs` |
| TC-GEN-001 | Genealogy 展開 | 工單與批號樹狀結構視覺化 | ✅ 通過 | 無 | `GenealogyView.vue` |
| TC-DSH-001 | Dashboard 柏拉圖 | 驗證 80/20 法則排列與累加百分比 | ✅ 通過 | 無 | `DashboardAfterImportUiTests.cs` |
| TC-WF-001 | 異常單簽核 | 測試異常真因分析與對策流程 | ✅ 通過 | 無 | `FullFormalWorkflowUiTests.cs` |

---

## 4. 失敗項目分析

### [失敗] TC-EXC-003: 大量資料併發匯入
- **問題描述**：當兩位作業員同時透過 API 匯入超過 10,000 筆紀錄的大型 Excel 檔案時，系統偶發 Timeout。
- **重現步驟**：
  1. 準備兩份 5MB 以上之 CSV。
  2. 使用腳本同時對 `/api/v1/uploads/variable/csv` 發起 Request。
- **預期結果**：系統應將任務丟入背景佇列 (Background Queue)，並回傳 `ImportStatus = Pending`。
- **實際結果**：HTTP 回應超過 30 秒並拋出 504 Gateway Timeout 或 500 DB Context Timeout。
- **可能原因**：`UploadService.CreateVariableBatchAsync` 為同步解析並一次性呼叫 `db.SaveChangesAsync()`，導致連線池卡死。
- **建議修正方式**：實作背景 Worker 模式，API 先建立 UploadBatch 骨架並回傳 202 Accepted，背景慢慢做。
- **嚴重等級**：High (影響多線程上線效能)。

---

## 5. 風險項目 (Risks)

目前可能發生但尚未修正的系統風險：
1. **資料重複匯入**：前端匯入檔案若遇到網路斷線，使用者按 F5 重新匯入，可能導致 `VariableMeasurement` 內產生重複的量測數據，進而污染 SPC 管制圖。
2. **MES 同步容錯**：`MesSyncMessages` 表格目前的重試機制較為薄弱，若 MES 發生閃斷，可能會漏掉關鍵的 WorkOrder 狀態更新。
3. **Traceability 大數據查詢效能**：`GenealogyView` 一次性拉取整個樹狀結構，當工單數量破萬或 SubLot 拆分超過 5 層時，資料庫可能因遞迴查詢導致 CPU 飆高。

---

## 6. 建議改善 (Action Items)

### 【立即修正】
1. **實作計數型管制圖 (P/NP/C/U)**：這是 SPC 的基本功，目前前端缺乏對應視圖。
2. **阻擋重複匯入機制**：在 DB 層或 Service 層加入 `MD5(FileContent)` 雜湊比對，阻擋完全相同的檔案重新上傳。

### 【近期修正】
1. **背景處理匯入檔案**：將 Excel 解析與 DB 寫入改為 Hangfire 或 .NET HostedService。
2. **加入權限卡控 (Role-Based Access Control)**：確保只有管理員能修改主檔。

### 【未來優化】
1. **建立自動化 CI 測試流水線**：依照 `test_implementation_plan.md`，把 E2E 測試與單元測試建置起來。
2. **AI 異常預測**：在趨勢接近 UCL 前給出預警。

---

## 7. 測試結論

**結論：系統已具備 Beta 上線之潛力，但核心防呆機制需補強。**

- **是否可進入下一階段**：**可以**。針對 Phase 1 的「SPC 運算」與「Dashboard 呈現」已達可用程度。
- **哪些功能不可上線**：在【大檔案非同步處理】與【重複資料防呆】完成前，不建議全面開放給超過 50 條產線的作業員同時手動匯入。
- **哪些功能需補測**：需等前端計數型圖表完成後，補測 P/NP/C/U 管制圖。
- **哪些功能風險最高**：MES 雙向同步機制。若資料不一致，將影響現場 Lot 追溯，建議初期以「單向 (MES -> SPC)」為主，降低風險。
