# FPC 軟板製造廠 SPC、生產履歷與 MES 系統整合差距分析 (Gap Analysis)

## 1. 文件閱讀摘要

經盤點既有專案文件（包含 `/ai_docs` 內的架構與資料庫設計、`README`、以及系統核心程式碼 `Models.cs` 等），重點摘要如下：

1. **基礎架構**：系統採用 ASP.NET Core Web API + Vue 3 + EF Core，具備成熟的軟刪除、稽核欄位 (`BaseEntity`) 及獨立解耦的 `SpcEngine` 統計引擎。
2. **MES 整合定位**：目前系統自帶 `WorkOrder`, `Part`, `Process`, `Machine` 等主檔，違反「MES 為唯一 Source of Truth」的設計原則。必須將這些主檔降級為唯讀副本，由 MES 驅動同步。
3. **FPC 產業特化需求**：現有系統缺乏針對 FPC 高度客製化的追溯維度。例如：缺乏正反面 (Front/Back Side) 隔離、缺乏 `Line -> Tank -> Slot` 階層、缺乏拆批 (Split Lot) 支援、以及缺乏藥水 (Chemical) 與咬蝕量 (PT/QE) 的關聯。
4. **追溯性 (Traceability)**：現有的 `VariableMeasurement` 僅擁有字串型態的 `LotNo`，完全無法建構出從工單到客訴的完整 `Product Genealogy` (產品系譜)。

---

## 2. 現有功能盤點

| 功能 | 已存在 | 部分存在 | 不存在 | 相關檔案/資料表 | 說明 |
|---|---|---|---|---|---|
| **MES Interface** | | | ❌ | 無 | 目前無自動同步機制，僅能人工 API/Excel 上傳 |
| **WorkOrder** | | ⚠️ | | `WorkOrder` | 表格存在，但缺乏 MES 聯動與強關聯追溯 |
| **Lot** | | ⚠️ | | `VariableMeasurement.LotNo` | 僅做為字串存在量測表中，無獨立實體與生命週期 |
| **SubLot** | | | ❌ | 無 | 完全缺失 |
| **SplitLot** | | | ❌ | 無 | 缺乏父子批號關聯與拆批歷程表 |
| **Process** | ✔️ | | | `Process` | 存在，需改由 MES 同步 |
| **Station** | ✔️ | | | `Station` | 存在，需改由 MES 同步 |
| **Line** | | | ❌ | 無 | 缺失，目前僅有 `Machine` 但概念不符 FPC 產線定義 |
| **Machine** | | ⚠️ | | `Machine` | FPC 中 Machine = Line，現有定義需重構對應 |
| **Tank** | | | ❌ | 無 | 濕製程/電鍍關鍵槽位缺失 |
| **Slot** | | | ❌ | 無 | 槽位內的掛具/位置缺失 |
| **Chemical** | | | ❌ | 無 | 藥水資料完全缺失 |
| **Operator** | ✔️ | | | `Operator` | 存在，需改由 MES 同步 |
| **Inspection** | ✔️ | | | `VariableMeasurement` 等 | 已有檢驗紀錄，但缺少 FPC 維度標記 |
| **MeasurementData**| ✔️ | | | `VariableMeasurement` | 資料表完整，需擴充 Side, Tank, Chemical 等 FK |
| **SPC** | ✔️ | | | `SpcCalculationResult` | 具備計算結果存檔 |
| **Control Chart** | ✔️ | | | 前端 / `SpcEngine` | 支援繪製多種管制圖 |
| **Cp / Cpk** | ✔️ | | | `SpcEngine` | 自動計算製程能力 |
| **AbnormalEvent** | ✔️ | | | `AlertEvent` | 支援異常事件與通知 |
| **Dashboard** | ✔️ | | | 前端 Dashboard | 基礎戰情看板已存在 |
| **Traceability** | | | ❌ | 無 | 無階層式履歷架構 |
| **Product Genealogy**| | | ❌ | 無 | 無法從工單展開完整系譜樹 |
| **Customer Complaint Traceability** | | | ❌ | 無 | 無法由客訴反查完整生產歷程 |
| **Import Template** | | | ❌ | 無 | 缺乏供 PT/QE 與藥水使用的匯入範本機制 |

---

## 3. Entity 分析

現有 Entity 主要圍繞基本的單機台 SPC 檢驗，**嚴重缺乏 FPC 深度追溯所需的實體**。
必須新增與重構的 Entity 包含：
- **Lot 生命週期**：新增 `LotMaster`, `LotSplitHistory`（支援 Parent/Child，禁止 Merge）。
- **FPC 產線拓撲**：重定義 Machine 為 `Line`，新增 `Tank`, `Slot`, `SlotSequence`, `SlotParameter`。
- **履歷軌跡**：新增 `LotSlotHistory` 紀錄批號流經的槽位與時間。
- **藥水與咬蝕**：新增 `Chemical` 主檔（無批號管理），以及專門對應 PT/QE 咬蝕量的實體或檢驗類別。

---

## 4. Table 分析

資料表 (Table) 的最嚴重 Gap 在於**核心交易紀錄表 (`MeasurementData`, `SpcCalculationResult`) 缺乏追溯維度**。
必須擴充以下欄位至核心資料表中：
- **履歷維度**：`WorkOrderId`, `LotNo`, `SubLotNo`, `ParentLotNo`
- **產線維度**：`LineCode`, `TankCode`, `SlotCode`
- **工藝維度**：`ChemicalCode`, `SideCode` (Front/Back)
- **來源追溯**：`SourceType` (MES, Excel, CSV, Manual, API), `SourceReference` (如 MES單號、API RequestId)

---

## 5. API 分析

目前 API 多為標準 CRUD 或直接接受量測數據。
**缺少的核心 API**：
1. **MES 同步 API**：供 MES 或中繼排程呼叫的 Master Data 與 Lot Status 同步 API。
2. **Genealogy API**：輸入批號，以 JSON Tree 格式回傳所有履歷、站點、機槽、檢測、SPC 與異常事件。
3. **Import Template API**：提供匯入範本的 CRUD，讓使用者無需重複設定上下限與機槽關係。

---

## 6. Service 分析

- `SpcEngine` 目前會將所有傳入的同檢驗項目資料視為同一母體。
- **重大修改需求**：在資料分群 (Subgrouping) 階段，必須強制導入 `SideCode` 的過濾。**正反面資料絕對不得混合計算 SPC 與 Cp/Cpk**，否則會產生嚴重的統計失真。

---

## 7. UI 分析

現有前端 (Vue) 畫面功能較為扁平。
**必須新增之 UI 模組**：
- **Product Genealogy Viewer**：視覺化的樹狀圖或時間軸，展示批號的拆批歷程與站點流轉。
- **Traceability Filter**：在 SPC Chart 與 Dashboard 加上 Side, Line, Tank, Chemical 等 FPC 專屬篩選器。
- **Import Template Manager**：讓使用者設定預設的 PT/QE/Chemical 匯入範本檔。

---

## 8. MES Integration 分析

為滿足「MES 為唯一 Source of Truth」且穩定運作，評估以下方案：
- **REST API**：適合即時量測與客訴回傳。
- **SQL View**：適合單向查詢，但不具備持久化。
- **Scheduled Sync**：排程同步，適合主檔。
- **Event Driven**：架構複雜。
- **Staging Table (中繼表)**：極度安全、防呆。

**💡 第一版最佳方案**：
採用 **Staging Table + REST API** 混合模式。
- **主檔與 Lot 拆批狀態**：由 MES 寫入 SPC 的 Staging Table，SPC 背景服務定期拉取轉為唯讀副本。
- **即時數據 (AOI/電測/品質)**：透過 REST API 以夾帶 `SourceType` 與 `SourceReference` 的方式寫入。

---

## 9. Traceability 分析

目前系統 **不支援** 以下的階層追溯要求：
`WorkOrder` → `Lot` → `SubLot` → `Process` → `Line` → `Tank` → `Slot` → `Chemical` → `Inspection` → `SPC` → `AbnormalEvent`。

目前中斷於：
1. 缺乏 `SubLot` 紀錄。
2. 缺乏 `Line → Tank → Slot` 的設備階層 (目前僅有一層 Machine)。
3. 完全缺乏 `Chemical` 藥水紀錄。

---

## 10. Product Genealogy 分析

輸入 `WorkOrder`, `Lot`, `SubLot`, 客訴或出貨批號後，**目前的系統無法查出系譜**。
目標 Genealogy 必須能一鍵查出：
1. **來源**：工單、母批 (ParentLot)、拆批歷程。
2. **生產歷程**：途經哪些 Process、Line、Tank、Slot、接觸哪些 Chemical。
3. **檢驗歷程**：關聯的 PT/QE 咬蝕量、AOI、電測資料。
4. **品質控制**：當下的 SPC 管制圖與異常事件。

---

## 11. 計量性資料分析

**涵蓋項目**：CHO, BCNa, PT咬蝕量, QE咬蝕量, 銅厚, 線寬, 厚度, 溫度, PH。
**現有支援度**：
- ✔️ Xbar-R, X-MR (I-MR), Cp, Cpk 核心引擎已支援。
- ❌ 缺乏「匯入範本」(Import Template)，每次上傳藥水或咬蝕量需重複設定。
- ❌ 缺乏 `SideCode` (正反面) 與 `Chemical` 的維度隔離。

---

## 12. 計數性資料分析

**涵蓋項目**：AOI缺陷數, 開路, 短路, 殘銅, 缺口, 電測不良, 不良數, 良率。
**現有支援度**：
- ✔️ `AttributeMeasurement` 實體已存在。
- ❌ 缺乏計數型專用管制圖：目前系統主要處理計量型，必須於 `SpcEngine` 擴充支援 **P Chart, NP Chart, C Chart, U Chart**。

---

## 13. 匯入架構分析

針對 Excel/CSV 雙資料來源模式與手動輸入，目前的上傳架構需要大改：
- 必須實作 **Import Template** 實體，記憶特定檢驗的 `Line`, `Tank`, `Slot`, 規格界限 (USL/LSL) 與單位。使用者匯入時只需選擇範本，不需重複配置。
- 所有的上傳與匯入動作必須強制寫入 `SourceType` (Excel/CSV/Manual) 與 `SourceReference` (匯入檔名/批次)。

---

## 14. 成熟度評估

- **Traceability：Level 1** (僅具字串批號，無系譜架構與設備/藥水關聯)
- **MES Integration：Level 0** (系統未與 MES 建立唯一 Source of Truth 連結)
- **SPC：Level 3** (具備計量型管制圖與異常判定，但欠缺計數型管制圖擴充)

*(Level 0=無, 1=初步, 2=部分關聯, 3=自動化計算, 4=視覺化與預警, 5=全自動根因分析)*

---

## 15. GAP Analysis 分類

### 【必須補強】
1. **FPC 產線拓撲與藥水追溯**：重構機台實體，新增 `Line`, `Tank`, `Slot`, `LotSlotHistory`, `Chemical`。
2. **正反面資料隔離**：量測資料全面新增 `SideCode`，並於 SPC 計算時強制分組。
3. **拆批邏輯實作**：新增 `LotMaster` 與 `LotSplitHistory`，支援 Parent/Child 追溯，絕對禁止合批。
4. **追溯性欄位擴充**：所有資料表需加入 `SourceType`, `SourceReference`, `WorkOrderNo`, `SubLotNo`。
5. **計數型管制圖擴充**：於 `SpcEngine` 實作 P, NP, C, U 管制圖。

### 【建議補強】
1. **Import Template 機制**：開發匯入範本管理，簡化 PT/QE 與藥水檢驗資料上傳。
2. **Product Genealogy 模組**：開發後端追溯 API 與前端樹狀圖視覺化介面。
3. **MES Staging 介接**：建立 MES 主檔與拆批狀態的中繼表同步服務。

### 【未來擴充】
1. **動態槽位參數分析**：結合 `SlotParameter` (溫度、濃度) 與品質資料的關聯分析。
2. **Event Driven 架構**：導入 Message Queue 處理即時 MES 事件。

---

## 16. Roadmap

為降低風險並穩健轉型，規劃 7 階段導入藍圖：

- **Phase 1: MES 同步 (MES Synchronization)**
  建構 Staging Tables 接收 MES 主檔，升級 DB 欄位加入 `SourceType`, `SourceReference`, `SideCode`、`Chemical` 等，確立 MES 為唯一 Source of Truth。
- **Phase 2: Lot/SubLot 拆批追溯 (Lot/SubLot Traceability)**
  建立 `LotMaster` 與 `LotSplitHistory`，實作單向拆批邏輯，禁止合批。
- **Phase 3: 深度追溯 (Deep Traceability)**
  完成 `Line -> Tank -> Slot` 架構，並透過 `LotSlotHistory` 建立批號流轉的強關聯。
- **Phase 4: 產品系譜 (Product Genealogy)**
  開發 Genealogy 引擎與前端視覺化，達成從工單一鍵反查原料、設備、SPC 及異常事件。
- **Phase 5: 計量性與計數性資料平台 (Variable & Attribute Data Platform)**
  擴充 SpcEngine 以支援 P/NP/C/U 管制圖；實作 Import Template 模組，加速 PT/QE/AOI 的 Excel/CSV 匯入。
- **Phase 6: 戰情看板 (Dashboard)**
  基於新的追溯維度 (Side, Line, Tank, SubLot) 翻新 Dashboard 篩選器與呈現邏輯。
- **Phase 7: Power BI 整合 (Power BI Integration)**
  建立扁平化 (Flattened) 的 Data Warehouse Views，供 Power BI 產生客製化進階報表。
