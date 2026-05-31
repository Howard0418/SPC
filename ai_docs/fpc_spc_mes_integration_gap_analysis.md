# FPC 軟板製造廠 MES 與 SPC 系統整合之差距分析 (GAP Analysis)

## 1. 既有文件閱讀摘要

經過查閱專案根目錄與 `/ai_docs` 內部的系統架構、資料庫設計與 API 規格等文件，摘要如下：

- **`README.md` & `development_context.md`**：專案目前處於 MVP 階段，完成度涵蓋資料接收 (手動/Excel)、SPC 計算引擎、Xbar-R/I-MR 圖與異常警報 (OOC/OOS)。
- **`01_current_architecture.md`**：採用前後端分離（Vue 3 + .NET 10 Web API + EF Core），SpcEngine 被設計為高度解耦的純運算核心。
- **`02_database_design.md` & `Models.cs`**：所有實體繼承自 `BaseEntity`（具備軟刪除與稽核欄位）。現有 SPC 內部主檔有 `Part`, `Process`, `Machine`, `QualityCharacteristic` 等。但在交易紀錄 `VariableMeasurement` 內，針對生產履歷的關聯僅有字串型態的 `LotNo` 與 `SerialNo`。
- **`03_backend_api.md` & `05_spc_engine_design.md`**：具備強大的運算能力（Western Electric 四大規則、Cp/Cpk），但尚未具備與外部 MES 系統自動化對接的 API（目前仰賴 Excel 匯入或人工操作）。
- **`09_development_rules.md`**：嚴格規範不直接改 Code，需透過 EF Core Migration 修改資料庫，並且維持現有架構的一致性。
- **`/agent_skills`**：僅存在一份通用文件產生器規範，未有針對 MES 介接的特殊客製化規則，故以既有開發規範與本分析為準。

---

## 2. 現有系統功能盤點

| 功能 | 已存在 | 部分存在 | 不存在 | 相關檔案/資料表/API | 說明 |
|---|---|---|---|---|---|
| **MES 介接** | | | ❌ | 無 | 目前僅支援 API 手動上傳或 Excel 匯入，無專屬 MES Webhook 或 Staging 同步機制。 |
| **工單接收** | | ⚠️ | | `WorkOrder` | 表格存在，但缺乏從 MES 同步的介面。 |
| **Lot 接收** | | ⚠️ | | `VariableMeasurement.LotNo` | 目前僅以字串欄位記錄，缺乏完整的 Lot 生命週期與屬性表。 |
| **Sub Lot 接收** | | | ❌ | 無 | 資料庫無 Sub Lot 欄位。 |
| **拆批接收** | | | ❌ | 無 | 缺乏拆批關聯表（Parent-Child）。 |
| **製程接收** | ✔️ | | | `Process` | 表格存在，需改由 MES 驅動。 |
| **機台接收** | ✔️ | | | `Machine` | 表格存在，需改由 MES 驅動。 |
| **槽位接收** | | | ❌ | 無 | FPC 關鍵的 Tank / Slot 未於量測數據或主檔中定義。 |
| **原料批號接收** | | | ❌ | 無 | 無 MaterialLot 紀錄，無法追溯銅箔基板(FCCL)或覆蓋膜(CVL)批號。 |
| **操作員接收** | | ⚠️ | | `Operator` | 表格存在，但量測表中多直接記錄字串 `Operator`。 |
| **檢驗資料** | ✔️ | | | `VariableMeasurement` | 已有完善的計量/計數型量測表。 |
| **SPC 量測資料** | ✔️ | | | `SpcCalculationResult` | 已存在並關聯至量測數據。 |
| **管制圖** | ✔️ | | | `SpcEngine` / `SpcChartView` | 支援 Xbar-R, Xbar-S, I-MR 等。 |
| **Cp / Cpk** | ✔️ | | | `SpcEngine` | 自動計算短期與長期能力。 |
| **異常判定/通知**| ✔️ | | | `AlertEvent`, `SmtpEmailNotificationService` | 支援西方電氣規則判定與 Email 通報。 |
| **工單/Lot追溯**| | ⚠️ | | `MeasurementBatch` | 僅靠字串反查，無強關聯結構，無法產生樹狀追溯。 |
| **原料/機/槽追溯**| | | ❌ | 無 | 缺乏 MaterialLot 與 Tank/Slot 關聯。 |
| **客訴追溯** | | | ❌ | 無 | 缺乏由客訴批號反查完整生產履歷與檢驗數據的視圖。 |
| **Dashboard** | ✔️ | | | 前端 Dashboard | 已具備基礎異常監控與指標顯示。 |
| **BI 匯出** | | ⚠️ | | 無專用 BI 視圖 | 可透過 EF Core 查出，但無扁平化 (Flattened) 資料倉儲視圖供 BI 直連。 |

---

## 3. 資料來源分析

**核心原則：MES 為 Source of Truth，SPC 不應提供主檔新增/修改介面給使用者，而應以唯讀副本 (Read-Only Replica) 方式同步。**

| 資料項目 | 目前來源 | 未來建議來源 | SPC是否需存副本 | 同步方式建議 | 備註 |
|---|---|---|---|---|---|
| WorkOrder | SPC API/手動 | MES | ✔️ 是 | 事件驅動 / API Push | SPC 需紀錄以供查詢與過濾 |
| LotNo | 手動輸入 | MES | ✔️ 是 | 事件驅動 / API Push | 需升級為實體表以紀錄狀態與拆批關聯 |
| SubLotNo | 無 | MES | ✔️ 是 | 隨 Lot 同步 | FPC 產業極為頻繁的拆批與追溯需求 |
| ParentLotNo | 無 | MES | ✔️ 是 | 隨 Lot 同步 | 用於建構追溯樹 (Traceability Tree) |
| Process/Station | SPC 主檔 | MES | ✔️ 是 | 每日排程 / Staging 表 | SPC 需配置管制特性，故必須有副本 |
| Machine | SPC 主檔 | MES | ✔️ 是 | 每日排程 / Staging 表 | 用於機台別 SPC 管制圖分析 |
| Tank / Slot | 無 | MES | ✔️ 是 | 隨機台主檔同步 | FPC 濕製程 (如鍍銅、蝕刻) 必備追溯維度 |
| Material(Lot) | 無 | MES | ✔️ 是 | 生產報工時 Push | 供客訴與異常發生時反查來料 |
| Operator | SPC 主檔 | MES | ✔️ 是 | 每日排程 / Staging 表 | 簡化為員工編號對照即可 |
| InspectionItem| SPC 主檔 | SPC 維護 | ✔️ (Master)| SPC 系統自理 | 檢驗標準通常由品管中心於 SPC 內維護 |
| MeasurementData| SPC/Excel | MES 檢測機台 | ✔️ (Master)| MES 推送或機台直連 | 資料直接落入 SPC |

---

## 4. MES 介接方式評估

| 介接方式 | 優點 | 缺點 | 建議情境 |
|---|---|---|---|
| **REST API (Push)** | 即時性高，架構現代化 | 若網路瞬斷易遺失資料，需實作 Retry 與 Idempotent。 | 適合實時量測數據與品質異常回傳 MES。 |
| **Staging Table (中繼表)**| 最安全、最無痛，MES 與 SPC 系統絕對解耦。 | 即時性較差 (依賴排程或 Trigger)，維護需 DBA。| **第一版強烈建議採用**。對於主檔與大量 Lot 狀態同步最穩定。 |
| **Event Queue (如 RabbitMQ)**| 最佳的非同步與解耦能力，高吞吐量。 | 架構複雜度大幅提升，維運成本高。 | 適合未來擴充。 |

**第一版建議 (Phase 1 導入)：**
針對「主檔與工單/Lot」採用 **Staging Table / SQL View**（由 MES 寫入，SPC 定期 Polling 或是由 SPC 建立 View 直接讀取 MES DB）。
針對「量測數據」採用 **REST API**，由 MES 於報工或機台連線時 Push 給 SPC。

---

## 5. Traceability 缺口

FPC 產業對於 Traceability 要求極高，目前 SPC 系統支援度存在嚴重缺口：

- ❌ **WorkOrder → Lot → SubLot**：目前無法呈現階層關係。
- ❌ **Lot → ParentLot / SubLot → ParentLot**：完全無此關聯欄位。
- ⚠️ **Lot → Process / Machine**：目前僅在單筆 Measurement 中有關聯，無整體 Lot 生命週期歷程紀錄 (Lot History)。
- ❌ **Lot → Tank / Slot**：無此欄位，無法追溯化學製程的槽位異常。
- ❌ **Lot → MaterialLot**：無法追溯該批次使用了哪一卷 FCCL 或哪一桶藥液。
- ⚠️ **客訴批號追溯**：目前只能依靠 SQL `LIKE` 搜尋 `LotNo`，無法利用樹狀結構快速關聯出「該批號的所有子批、父批、以及關聯的 SPC 異常紀錄」。

---

## 6. 拆批需求分析 (Split Lot)

FPC 生產過程中常因良率、分片或品質抽檢發生拆批。依據限制，**本系統絕對禁止合批 (Merge Lot)**。

目前 SPC 系統**完全不支援**拆批邏輯。必須補強以下機制：
- 必須建立 `LotMaster` 與 `LotSplitHistory` 表格。
- 必須記錄：`ParentLotNo`, `ChildLotNo`, `SplitTime`, `SplitQty`, `SourceProcess`, `TargetProcess`。
- **SPC 獨立分析原則**：Child Lot 產生後，其後續的 `MeasurementData` 必須綁定至 `ChildLotNo`，並獨立繪製管制圖。但在追溯報表時，必須能向上滾動 (Roll-up) 查詢 `ParentLotNo` 在前段製程的 SPC 狀況。

---

## 7. SPC 關聯分析

目前的 `VariableMeasurement` 記錄了：
`UploadBatchId`, `PartId`, `ProcessId`, `MachineId`, `CharacteristicId`, `LotNo` (字串), `SerialNo` (字串)。

**缺漏必須補齊的分析維度欄位**：
- `WorkOrderId`：以利按工單篩選 SPC 管制圖。
- `SubLotNo` / `ParentLotNo`：以利按精確批號篩選。
- `TankId` (或 `TankCode`)、`SlotId` (或 `SlotCode`)：用於藥液管制群組 (`CHEM`) 或電鍍製程分析。
- `MaterialLotNo`：用於分析不同原料批次是否造成製程能力 (Cpk) 偏移。

---

## 8. 成熟度評估

- **Traceability 成熟度：Level 1**
  *(0=無, 1=僅字串記錄, 2=具備正反向追溯, 3=包含人機料法環, 4=視覺化系譜圖, 5=全自動根因分析)*
- **MES Integration 成熟度：Level 0**
  *(0=無/人工, 1=匯入匯出, 2=排程中繼表, 3=API單向, 4=API雙向/即時, 5=事件驅動/中樞神經)*
- **SPC 成熟度：Level 3**
  *(0=無, 1=人工Excel, 2=基本管制圖, 3=自動判定/Western Electric, 4=即時預警停機, 5=AI參數回饋)*

---

## 9. GAP Analysis 分類

### 🚨 必須補強 (Must Have)
1. **資料庫擴充**：於 `VariableMeasurement` 等量測表中新增 `TankCode`, `SlotCode`, `MaterialLotNo`, `SubLotNo`, `WorkOrderNo` 欄位。
2. **拆批系譜建立**：建立 `LotMaster` 與 `LotSplitHistory` 實體，強制排除合批邏輯。
3. **MES 同步介面**：實作供 MES 寫入的 Staging Tables 模型或唯讀 API 供 SPC 定期化同步主檔 (Part, Process, Machine, WorkOrder)。

### 💡 建議補強 (Should Have)
1. **追溯視圖 (Traceability View)**：在資料庫建立遞迴 CTE View 或 Stored Procedure，輸入一個 `LotNo` 即可秒查其 Parent、Children 以及所有關聯的 OOC/OOS `AlertEvent`。
2. **前端篩選器增強**：SPC Chart 頁面增加「機台」、「槽位」、「原料批號」的 Filter，幫助工程師抓出異常真因。

### 🚀 未來擴充 (Could Have)
1. **BI 扁平化匯出**：建立供 Power BI / Tableau 直連的唯讀資料倉儲 (Data Warehouse) Schema。
2. **Event Sourcing**：未來 MES 與 SPC 皆導入 Kafka 或 RabbitMQ 進行全系統 Event Driven 整合。

---

## 10. 下一步建議 (最小風險導入順序)

1. **Phase 1: DB Schema 升級 (不影響現有運作)**
   - 利用 EF Core Migration 新增 `TankCode`, `SlotCode`, `MaterialLotNo` 等欄位至量測表。
   - 新增 `LotMaster` 與 `LotSplitHistory` 實體。
2. **Phase 2: Staging 介接建立**
   - 與 MES 團隊協議建立 DB Staging Table，SPC 撰寫 Background Service 每日/每小時定時載入/UPSERT 變更。
3. **Phase 3: 追溯 API 與前端實作**
   - 開發 `TraceabilityController`，支援樹狀系譜查詢。
   - 擴充 SPC 管制圖查詢條件。
4. **Phase 4: 即時整合**
   - 將測量數據的接收改由 MES 即時呼叫 `POST /api/v1/uploads/...`。

> **架構師提醒：**
> 在執行 Phase 1 之前，請務必由 MES 團隊確認他們能夠提供的資料粒度是否能涵蓋 FPC 濕製程的 Tank / Slot 層級，並確認他們發送 Split Lot 事件時的資料結構。
