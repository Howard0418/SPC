# 任務
- 功能 ID：20261005-single-iis-site
- 規格版本：1
- [規格](spec.md)｜[計畫](plan.md)

- [x] T-001：分析目前 Vue/API/IIS/URL/CORS/SSO/JWT/SQL 架構並提出最小方案。（R-001～R-008）
- [x] T-002：建立單站部署規格、任務與 rollback 計畫；不修改產品程式。（R-008）
- [x] T-003：將前端 API base 預設調整為相對 `/api`。（R-002）
- [x] T-004：修正手動組 API URL 的匯出路徑，避免 `/api/api`。（R-003）
- [x] T-005：建置並檢查前端資產、後端 publish 與 static file fallback。（AC-001～AC-004）
- [ ] T-006：規劃並執行測試站單一 IIS Site 設定；不發布正式站。（AC-001、AC-007、AC-008）
- [ ] T-007：執行登入、Portal SSO、JWT、SQL、401、403、Vue refresh、mixed content smoke。（AC-004～AC-007）
- [ ] T-008：同步需求索引、變更紀錄與驗證證據。（AC-008）
