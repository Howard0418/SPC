# 驗證紀錄

- 來源 SHA-256 F634868A1AD1B432C7AA9E107BE0D76B1F9851AE38A244BE3754D17EAC348C7C。依唯一配對與現行 PMR_SPC_TEST 唯讀重查，14 筆 PPC 共 21 欄缺漏（調整 14 欄、調整量 7 欄）。
- 三參數齊備候選 10 筆／16 欄，PPC 2444、2445、2447、2449、2458、2564、2570、2587、2588、2591。使用 SPC 現行引擎試算，共執行 46 組，包括門檻邊界。PPC 2587 稀釋保留已核准的 22，既有兩式只在空白時補入。
- 缺必要稀釋管制值 4 筆／5 欄，PPC 2518、2565、2568、2571。Excel 在這些規則參照的管制值為 NA，等待該分支的業務確認，禁止代入 LSL／USL 或自行猜值。
- 測試庫 COPY_ONLY／CHECKSUM 備份 PMR_SPC_TEST_MISSING_ADJUST_20261122-213237.bak 已完成，SQL 及 Windows 整合帳號執行 RESTORE VERIFYONLY WITH CHECKSUM 均回報 master 的 CREATE DATABASE 權限不足。待具權限身分核對同一份備份，尚未套用公式、寫入業務資料或發布。
