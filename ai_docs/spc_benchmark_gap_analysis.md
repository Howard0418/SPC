# FPC Smart Factory SPC 系統 Benchmark 與 Gap Analysis

本報告以資深 Smart Factory / MES / SPC 顧問角度，針對目前本專案的 SPC 系統，對標市面上成熟 SPC 系統（包含 Minitab Real-Time SPC、Datalyzer SPC、Ellistat SPC、Sepasoft SPC 等）進行完整的 Benchmark 分析。分析範圍聚焦於軟性電路板（FPC）製造業的獨特製程與品管需求。

---

## 1. 現有 SPC 功能盤點

| 功能模組 | 已存在 | 部分存在 | 不存在 | 檔案位置 / 實作狀態 |
| :--- | :---: | :---: | :---: | :--- |
| **基礎 SPC 圖表** | | ✓ | | `SpcEngine`, `SpcChartView.vue` (支援 Xbar-R, 單點趨勢，部分計數型) |
| **製程能力指標 (Cp/Cpk/Pp/Ppk)** | ✓ | | | `SpcEngine.cs` (已實作核心演算法) |
| **Western Electric Rules (WECO)** | ✓ | | | `SeedData.cs` (規則 1~8) / `SpcEngine.cs` |
| **品質主檔設定 (Part/Process/Machine)**| ✓ | | | `Models.cs`, `MasterDataV2Controller.cs` |
| **FPC 專用：正反面 (Top/Bottom)** | | ✓ | | `MeasurementValue` (依賴 `SideCode`) |
| **FPC 專用：槽體與槽位設定 (Tank/Slot)**| ✓ | | | `TraceabilityMasterController.cs`, `TraceabilityMasterView.vue` |
| **產品系譜圖 (Product Genealogy)** | | ✓ | | `GenealogyController.cs`, `GenealogyView.vue` (支援 Lot/WO 關聯) |
| **MES 介接 (Staging / API)** | | ✓ | | `MesSyncMessages` / `MigrationController.cs` (非同步同步機制) |
| **異常處置閉環 (Alerts Workflow)** | | ✓ | | `AlertsWorkflowView.vue` (支援 Root Cause & Corrective Action) |
| **QMS: 稽核與文件管理** | | | ✓ | 尚無相關實作 |

---

## 2. SPC Benchmark

與 Minitab Real-Time、Datalyzer、Sepasoft 等業界標準相比：

### A. 基礎 SPC
- **Xbar-R / X-MR**: 【已存在】核心計算已完備，符合標準。
- **P / NP / C / U Chart**: 【部分存在】資料結構已具備 (AttributeMeasurement)，前端展示與計算邏輯尚待補齊完整圖表切換。
- **Cp / Cpk / Pp / Ppk**: 【已存在】支援基礎能力指標計算。
- **Western Electric Rules**: 【已存在】支援動態啟用多條檢驗規則。
- **自動警報**: 【已存在】OOC 發生時產生 Alert Event，並支援 SMTP 發送。

### B. FPC 專用功能
*FPC 具備雙面佈線、連續性濕製程等特性，與一般機械加工 SPC 不同。*
- **正反面區分**: 【部分存在】透過擴充欄位處理，尚缺前端一鍵切換正反面比對圖。
- **PT / QE 咬蝕量**: 【不存在】尚未針對蝕刻速率 (Etch Rate) 提供專用的差值計算公式。
- **藥水管理 (Chemical)**: 【部分存在】具備 Chemical Daily Report 骨架，但缺乏濃度衰減曲線之 SPC。
- **Tank / Slot**: 【已存在】追溯主檔已完成。
- **生產線履歷**: 【部分存在】

### C. Traceability
- **WorkOrder / Lot**: 【已存在】
- **SubLot / SplitLot**: 【部分存在】資料庫支援 `ParentLotId` 與 `LotSplitHistory`，但前端分批操作介面未完善。
- **Material / Chemical**: 【部分存在】有主檔，但物料綁定至批號的 Tracking 較弱。
- **Machine / Tank / Slot**: 【已存在】`LotSlotHistory` 已實作紀錄批號過帳節點。

### D. Product Genealogy (輸入批號後的查詢能力)
- **用哪些原料 / 藥水**: 【不存在】BOM / Recipe 展開尚未與 Genealogy 視圖深度結合。
- **經過哪些槽位**: 【已存在】可查出經歷之 Machine 與 Tank/Slot。
- **哪些 SPC / 檢驗異常**: 【已存在】Genealogy View 右側可顯示對應的 Quality Events。

### E. MES Integration
- **API / Staging Table**: 【已存在】具備 `MesSyncMessages` 進行 Event Queue 處理。
- **排程同步**: 【不存在】未實作 WorkOrder 狀態與機台即時排程同步。
- **Event**: 【已存在】

### F. Dashboard
- **良率 / 不良率 / Cpk 趨勢**: 【部分存在】首頁有高階圖表，但缺乏多維度 (By 廠別/線別) 深入鑽取 (Drill-down)。
- **異常排行**: 【部分存在】有 Alerts 清單，缺 Pareto Chart (柏拉圖)。
- **Tank / Chemical 異常排行**: 【不存在】

### G. QMS
- **CAPA / 8D Report**: 【部分存在】透過 `AlertsWorkflowView` 達成 D3~D6 (Root Cause, Action) 管理，缺完整 8D 報告列印與核准流程。
- **客訴管理 / 稽核管理 / 文件管理**: 【不存在】

---

## 3. 功能成熟度評分 (0 ~ 5 分)

- **SPC**: **3.5** (統計引擎完備，已超越基本 MES 的 SPC，追平標準套裝 SPC 的底層能力，但在多圖表聯動切換略遜一籌)
- **Traceability**: **3.0** (骨架齊全，槽位追溯概念先進，但現場報工卡控與物料批號綁定尚未落地)
- **Product Genealogy**: **3.0** (已具備視覺化樹狀圖與品質事件關聯，超越許多傳統 MES，但缺少 BOM / Chemical 關聯)
- **MES Integration**: **2.5** (非同步架構良好，但涵蓋的事件類型尚少)
- **Dashboard**: **2.0** (基礎資訊呈現，缺乏自訂報表與進階柏拉圖)
- **QMS**: **1.5** (具備基礎異常閉環，但缺乏完整 ISO/IATF 要求的稽核管理)

---

## 4. GAP Analysis

### 【立即需要】(0-3 個月內補齊)
1. **Attribute Control Charts (計數型管制圖)**：完整實作 P / NP / C / U 圖的視覺化。
2. **柏拉圖 (Pareto Chart) 與異常排行**：Dashboard 必須能看見 Top 5 異常機台/槽位/製程。
3. **正反面分析比較**：FPC 痛點，必須能在同一張 SPC 疊加正反面量測點作比較。

### 【6個月內補齊】
1. **Etch Rate (PT/QE 咬蝕量) 特殊公式計算**：結合進料厚度與出料厚度自動計算蝕刻量 SPC。
2. **Chemical Tracking (藥液管理 SPC)**：將化學濃度分析 (Titration) 納入 SPC 管理。
3. **完整的 8D Report 匯出**：讓客訴或重大異常能直接產出標準 8D PDF 報告。

### 【1年內補齊】
1. **進階 Product Genealogy**：整合 ERP BOM 表，追溯至上游銅箔基板 (FCCL) 批號。
2. **自動化報表排程**：定時產出品質週報/月報並自動派送。
3. **高階資料採礦分析**：找出最佳良率對應的機台與藥水參數組合 (Golden Route)。

---

## 5. FPC 最佳實務 (Best Practices for Flexible PCB)

對於軟性電路板工廠，SPC 與 QMS 系統的優先級如下：

*   **必須有 (Must-Have)**：
    *   **雙面/多層結構的品質管控**：FPC 常見雙面板或多層板，同一製程在 Top/Bottom 的變異經常不同，系統必須能將正反面或不同層 (L1, L2) 的 SPC 分開或重疊比對。
    *   **濕製程槽位追溯 (Tank/Slot)**：FPC 經過大量的化學槽 (除膠渣、化銅、電鍍)，產品品質與「在哪個槽泡了多久」絕對相關。
    *   **脹縮比 (Scaling) 管控**：FPC 基材極易變形，壓合前後的脹縮比 SPC 是控制良率的命脈。

*   **建議有 (Should-Have)**：
    *   **藥水濃度與添加紀錄連動**：將化驗室的藥水濃度 SPC 與產線的良率 SPC 疊加比對，尋找最佳操作區間 (Window)。
    *   **Roll-to-Roll (捲對捲) 追溯**：FPC 常以卷軸生產，系統需支援 Roll ID 取代傳統的 Panel Lot ID，並支援 X/Y 軸座標的 Mapping。

*   **未來進階功能 (Advanced)**：
    *   根據 SPC 趨勢即時回饋 (Feed-forward / Feed-backward) 自動建議或修改下游客戶的曝光機或雷射機參數，達成自動化補償。

---

## 6. Roadmap 與系統完成度

### 完成度概估
- **SPC**：**70%** (引擎強大，缺特定圖表 UI 與進階分析)
- **Traceability**：**60%** (主檔與模型完備，缺前端操作深度)
- **MES**：**40%** (具備資料橋接框架，缺全面業務邏輯)
- **Genealogy**：**50%** (UI 框架優良，缺 BOM 與 Chemical 深度)
- **Dashboard**：**30%** (僅有雛形)
- **QMS**：**20%** (僅有 Alerts Workflow)

### Phase 1: SPC 完備與基礎追溯 (Current ~ M3)
- 補齊 P/NP/C/U 管制圖。
- 實作柏拉圖與異常排行榜 Dashboard。
- 完善 FPC 正反面、脹縮比的 SPC 特殊視覺化。

### Phase 2: 濕製程深化與 QMS 整合 (M4 ~ M6)
- 化學藥液濃度 SPC 與添加紀錄管理。
- 咬蝕量 (Etch Rate) 自動計算與管制。
- 將 Alerts Workflow 升級為完整的 CAPA / 8D 簽核系統。

### Phase 3: MES 深度整合與 Genealogy 擴展 (M7 ~ M9)
- 與 MES 生產排程即時同步。
- Genealogy 視圖加入原料批號 (BOM) 與藥液狀態追溯。
- 支援 Roll-to-Roll 的座標級品質追溯。

### Phase 4: 參數優化與進階分析 (M10 ~ M12)
- SPC 跨廠區/跨產線總體良率比較與自動診斷。
- 大數據關聯分析：找出最佳良率對應的機台與藥水參數組合 (Golden Route)。
- SPC 儀表板與 ERP 成本系統連動分析。
