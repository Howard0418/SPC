# 驗證與目前限制
## 完成
- AC1：正式Portal PMR_PORTAL_UAT 已套用25項migration；候選無待遷移。正式SPC PMR_SPC_2026待8項，已依候選EF metadata產生8份離線SQL，沒有連線或執行migration。
- Portal API正式PDB173份文件中3份既有來源不同：SpcLaunchController、SpcProxyController、EtchAmountReport。另有新測試匯入功能，正式環境會拒絕，沿用既有驗證。
- AC2部分：新增Portal8例通過（日期/班別/階段代理、無Token拒絕、100點咬蝕代理與409保留、登入4分支、首頁404容錯）；SPC SQLite1例通過（上傳→確認→daily讀取→更新原ID，分析/複驗3位及中班/收線保留）。沒有啟動實際API背景服務或向正式寫入。
- 發布前檢核5例通過：未核准migration、正式版號改變、缺schema/資料/啟動/回復證據均拒絕。工具不能代替發布前實際查核。
- 三專案Release編譯通過，沿用既有前端9例及歷史4例，不重跑。
- [測試證據](../../release-staging/production-compatibility-20260918/tests/compatibility.trx)、[3位保存](../../release-staging/production-compatibility-20260918/tests/precision-roundtrip.trx)。
- [候選清單](../../release-staging/production-compatibility-20260918/candidate.json)、[SQL清單](../../release-staging/production-compatibility-20260918/migrations/inventory.json)、[編譯雜湊](../../release-staging/production-compatibility-20260918/build-hashes.json)。
## 尚未完成／需確認
- AC2未全部通過：SQL Server正式schema→候選的遷移/啟動演練尚未執行。SQLite與mock HTTP只驗證契約，不能宣稱等同正式整合測試。
- 8項SPC待遷移包含完整校正模組6項及設備來源1項，現有啟動會全部套用。超過單純精度/藥液相容範圍，先確認納入完整模組或改作限定包，再演練。
- 舊OPEN/CLOSE正式已470筆，與先前460不同；N2中班30依既有確認歸收線，N1中班7筆階段未確認，先不猜測。
- AC3：候選與檢核工具已準備，但不具可發布資格，[preflight](../../release-staging/production-compatibility-20260918/preflight-result.json)為ready=false。尚未產生正式可覆蓋包、未備份正式DB、未執行資料轉換。
- 未更新應用版本；未發布測試或正式，因本次僅新增隔離測試與離線工具，無網站實作修正需求。
## 下一步
確認額外模組/schema範圍及N1中班歸屬後，先完成隔離SQL Server遷移與啟動演練、正式資料轉換預覽，再交付可核准發布包。本批不發布正式的限制繼續有效。
