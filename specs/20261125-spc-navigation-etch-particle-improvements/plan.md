# 技術計畫

- 功能 ID：`20261125-spc-navigation-etch-particle-improvements`
- 規格版本：8
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

### SPC

- 權限目錄：`backend/MesSpc.Api/Services/Security/SpcPagePermissions.cs`、權限目錄 API、角色／權限稽核與測試。
- 選單與路由：`frontend/mes-spc-web/src/App.vue`、`src/router/index.js`、三語系名稱及選單／守門測試。
- 落塵輸入：新增獨立 Vue 頁面及路由；沿用 `ParticleUploadService` preview／confirm、`UploadBatch`／`UploadDetail`／`UploadError`／`ParticleMeasurements`。
- 落塵分析：將 Location 白名單與篩選由 R1～R9 擴充 R1～R12；C/U-chart 計算不變。

### Portal

- 咬蝕輸入：`src/PmrPortal.Web/Pages/QualityAssurance/EtchAmountEntry.cshtml` 的格線事件、貼上 parser、錯誤提示與 Web 測試。
- 原有咬蝕 API、資料模型及同步 SPC payload 不變。
- Portal 只建立引用規格，主規格及工作計數維持在 SPC。

## 實作方式與需求對應

### 小工作 1：`SPC-PERM-NAV-ALIGN-1125`（P0）

1. 建立一份頁面登錄表：權限碼、route、三語 i18n key、群組、Viewer／Editor 預設、是否明示授權。
2. 左側選單、權限清單及 router guard 以同一登錄表或可自動比對的契約產生，消除 `/particle-monitoring` 借用 `analysis.spc` 等一碼多頁情況。
3. 對未帶新版標記的既有 `PagePermissionsJson` 套用舊版有效頁面相容解析；下一次保存時加入內部版本標記並改採精確清單，不做批次資料庫更新。
4. 稽核 read／export／evidence 維持 explicit-only，增加不得自我加權及前後有效權限測試。
5. 先完成此工作，後續落塵輸入才使用已對齊的 `data.particles` 權限。

### 小工作 2：`PORTAL-ETCH-PASTE-1125`（P1）

1. 先建立可由 Node 直接驗證的純 JavaScript parser／planner，針對目前 side 的可輸入格監聽 `paste`；依目前游標格與 clipboard Tab／換行矩形填入草稿。
2. 貼上只呼叫現有 `calculate()`，不直接呼叫儲存或 SPC 同步。
3. 先完整解析與驗證，再一次套用；任何超界或無效內容時整次不套用，顯示 side／組次／OP／BEFORE-AFTER。
4. 保留 COM、逐格輸入、Enter 移動、切換 side、日期／線別重載與原儲存 payload。

### 小工作 3：`SPC-PARTICLE-PASTE-ENTRY-1125`（P1，依賴小工作 1）

1. 新增 `/particle-monitoring/entry` 與 `data.particles`；既有 `/particle-monitoring` 改用獨立 `analysis.particles`。
2. 畫面固定雙層表頭與 R1～R12 × 四粒徑格線；支援從第三列或目前資料格貼上，預覽前轉成 Long Format。
3. 前端建立 `SourceType=WebPaste`、內容 SHA-256、合成來源列欄；後端仍重新驗證，不信任前端 parser。
4. 沿用 `POST /api/v1/uploads/particle/preview` 與 confirm；新增／調整 API 權限，預覽只寫 staging，確認才寫 `ParticleMeasurements`。
5. 白名單擴充到 R12，既有 R1～R9 資料與圖表測試作回歸；預設重複整批拒絕，可明示略過，不提供覆蓋。

### 小工作 4：`SPC-PARTICLE-ENTRY-LAYOUT-1125`（P2，依賴小工作 3）

1. 將使用者提供的 R1～R12 量測位置配置圖保存為 Web 靜態 PNG，不改圖內位置、編號或文字。
2. 在落塵輸入頁標題下新增有框線的配置圖區，圖片採等比例 `object-contain` 顯示；其下才是既有有框線的 Excel 貼上區。
3. 先新增 Chromium 版面測試，驗證圖片可載入、配置圖區位於貼上區上方、兩區框線存在；既有貼上、預覽及確認測試持續通過。
4. 本工作只遞增 SPC Web 版本；API 程式、資料庫、權限碼及匯入契約不變，測試站 API 版本保持並照常驗證。

### 小工作 5：`SPC-PARTICLE-ENTRY-GRID-1125`（P1，依賴小工作 3）

1. 將可見的大文字框替換成可聚焦、可接收貼上的固定格線；表頭使用黃色底、黑色框線，第一欄為日期時間／位置，後續為 R1～R12 × 0.5／1／5／10 µm。
2. 無資料時仍顯示完整表頭與一列貼上提示；貼上後沿用 `parseParticlePaste`，把合法資料列顯示在對應 49 格，日期與時間於第一欄分行呈現。
3. 貼上只建立前端草稿，不呼叫 API；原「解析貼上資料／建立預覽／清除未確認草稿」及 confirm 契約保持。
4. 先新增 Chromium 驗證表頭、欄數、貼上對位、清除及 preview 前零 API 寫入，再遞增 SPC Web 版本並發布測試站；API 版本維持。

### 小工作 6：`SPC-PARTICLE-EXCEL-FORM-GRID-1125`（P1，依賴小工作 5）

1. 以指定 Excel 工作表的 B24:BB 為唯讀格式基準，將格線補為「日期時間／位置 + R1～R12 × 四粒徑 + 備註」50 個邏輯欄位。
2. parser 同時接受既有 49 欄、網站 50 欄及原 Excel 合併備註展開後的 53 欄；支援 Excel quoted TSV 與日期／時間同格換行，任何不相容列仍整批拒絕。
3. 貼上後的日期、48 個數值與備註可在格內修正；每次修正使舊 preview 失效，避免以舊預覽確認新畫面資料。
4. 備註沿用既有 API `Remark`，投影至同一來源列的所有非空白量測；仍須先建立預覽，再按確認寫入，不提供直接繞過預覽的儲存。
5. 先建立 parser 與 Chromium 回歸測試，再遞增 SPC Web 版本、發布測試站並驗證 Web／API／瀏覽器實際 API 基底；API 與資料庫不變。

### 小工作 7：`SPC-PARTICLE-GRID-PASTE-FOCUS-1126`（P0，依賴小工作 6）

1. 將無資料狀態的合併提示格改為 50 個獨立框線格，保留黃色雙層表頭與水平捲動。
2. 使用者點空白列任一格時，程式主動讓貼上格線取得焦點；Ctrl+V 從目前焦點派送既有 paste handler，不靠測試直接指定表格元素。
3. 已有草稿時，含 Tab 的多欄貼上視為整批 Excel 重新貼入；單一欄位文字維持原格內編輯，避免攔截一般備註或單格修正。
4. 先補 Chromium 測試驗證 50 格、滑鼠點擊、實際焦點與從 activeElement 貼上，再遞增 Web 版本、發布測試站並重驗 API 基底；API 與資料庫不變。

## API、檔案格式及資料模型

- 咬蝕：不新增 API；clipboard 僅轉成現有 `points[{side,repeatNo,opNo,beforeValue,afterValue}]` 草稿。
- Particle preview／confirm：沿用既有契約與表格；預期不需 schema migration。
- Particle Location 應用層白名單由 R1～R9 改為 R1～R12；查詢與圖表下拉同步。
- 新的 WebPaste 批次以登入者作 `CreatedBy`；不把未確認的「作業員」欄塞入 Remark。
- 權限資料若需相容補值，屬受控資料更新，必須先列前後有效頁面差異、備份及可逆腳本；正式區另行授權。

## 相容性與風險

- 最高風險為權限碼拆分後誤失權／誤擴權；以調整前後有效路由集合逐人比對，且不得和其他 AD／SSO／角色工作併行。
- 咬蝕貼上若列欄順序錯誤會導致計算錯位；採整次原子套用、錯誤座標及原 payload 回歸。
- Particle R10～R12 是既有契約擴充；不能改寫舊資料，也不能把空白當 0。
- 新貼上頁不得繞過 preview／confirm、重複保護或稽核；不得直接 POST 到量測表。
- 每個小工作各自遞增 Web/API 版本並單獨發布測試，不合併成一次高風險發布。

## 驗證安排

- AC-001／002：頁面登錄契約測試、Viewer／Editor／Administrator 路由矩陣、明示稽核權限、自我加權拒絕、既有人員有效頁面前後集合相同。
- AC-003／004：PT1／PT2／QE1／QE2，A/B side 全矩形及部分矩形貼上；CRLF/LF、空白、非數字、超界、負數、BEFORE<AFTER；原 COM／逐格／儲存／同步回歸。
- AC-005～007：含／不含兩列表頭、49 欄、部分空白、0、非整數、負值、錯誤時間、R10～R12、重複 reject／skip、preview 未確認不產生量測、confirm 冪等與失敗 rollback。
- AC-008：SPC API／Web 與 Portal API／Web 的焦點測試、必要完整回歸、Release build；測試站首頁、主 JS/CSS、health/version、瀏覽器實際 API 基底及登入後操作驗收。
- AC-009：Chromium 驗證配置圖 HTTP 200、圖片等比例顯示、配置圖區在貼上區上方且兩區框線非 0；重跑原落塵貼上預覽／確認案例。
- AC-010：Chromium 驗證雙層黃色表頭、12 組位置、48 個粒徑欄與一個日期時間欄；合法一列貼上後 R1/0.5 及 R12/10 對位，preview 前 API 未呼叫，清除後恢復空白列。
- AC-011：以指定 Excel 表頭與資料列 fixture 驗證 53→50 正規化、quoted newline 日期、備註傳遞、格內修正、舊 preview 失效及 preview／confirm 寫入順序；既有 49 欄貼上保持相容。
- AC-012：Chromium 驗證空白資料列 50 個獨立格線，點日期／量測／備註任一格後 table 為 activeElement，從 activeElement 派送 Excel paste 可對位；已有草稿時多欄貼上仍走整批 parser。
- 使用者原始截圖只作需求佐證，不提交到 Git；經校正且實際由網站使用的配置圖資納入版本控制。驗收截圖去識別後保存於本機證據目錄，不納入版本控制。

## 發布、備份及回復

- 本輪只規劃，發布不適用。
- 實作時三件小工作逐件完成、提交、推送及發布測試站；正式站每件仍需另行授權。
- 權限工作：發布前備份測試 DB 及 Web/API；保存 `PagePermissionsJson` 前後快照。回復舊程式及原 JSON。
- 咬蝕工作：無 schema；回復 Portal 前版 Web 資產，資料不需回復。
- Particle 工作：預期無 schema；回復 SPC 前版 Web/API。測試驗收批次以專用來源鍵識別，若需移除必須另經資料操作授權，不隨程式回復自動刪除。
