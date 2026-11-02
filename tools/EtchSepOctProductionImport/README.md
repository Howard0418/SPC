# 正式九十月咬蝕：僅新增
只在使用者明確授權正式資料匯入時使用。此工具沒有Portal連線、不修改主檔、不部署網站；不是TransFiles GUI交付。

- 固定`release/production/backend/appsettings.json`、`production`、172.16.110.16／PMR_SPC_2026，並核對正式8443 API環境；正式守衛有對應單元測試。
- 本機證據目錄`manifest.json`須由測試通過的同一原始Excel hash產生，含完整日報及provenance；只允許2026-09／10且日線不重複。
- `--preflight <證據目錄>`唯讀：任何已存在的ETCH日線鍵直接略過，沒有宣稱其內容必然相同；其餘以既有嚴格服務預覽，主檔不符或衝突隔離。
- `--apply <證據目錄> --confirm-database=PMR_SPC_2026 --confirm-source-sha256=<完整manifest hash>`才可寫入；再次預覽、COPY_ONLY／CHECKSUM及RESTORE VERIFYONLY後，使用真實Windows操作者及`Sync(allowUpdate:false)`僅新增。每報重送須Unchanged、同批次，既有核心資料指紋與主檔／政策／啟用F保持。
- `--verify <證據目錄>`只讀核對可驗證主檔的原始點位／SampleNo／速率；不寫入。此模式為核對資料會讀既有報表，因此其ready／blocked分類不同於正式防重預覽，不用它當可再次匯入清單。
- 修正版仍依原ETCH日期／線別業務鍵防重，不能改檔名或來源hash重複寫入。若沒有可新增資料，`--preflight`應顯示0；不可為了重跑而刪除正式資料。
- JSON、manifest、備份、修正Excel、截圖及bin／obj只留本機，不提交。回復須限定此次新增batch／ID並核對其後正式寫入，不直接還原整庫。

[本次授權、部分結果及限制](../../specs/20261102-production-import-insert-only/verification.md)。
