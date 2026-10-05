# N2中班歸收線
使用者確認2026/9/11、9/14、9/16各10筆中班歸收線。範圍：PMR_SPC_TEST共30筆，CHEM/N2、SamplingPhase=MIDDLE、SamplingStage=GENERAL。
驗收：只將SamplingStage改CLOSE，中班/數值/日期/ID及其他業務欄位不變（SQL RowVersion正常遞增）；先備份、交易核對，API各日期載入10筆。
計畫：核對30筆→備份完整列→交易更新/逐欄驗證→已部署API唯讀載入→文件同步。正式庫缺階段欄位，仍沿用前案相容性處理，不在本批修改。
狀態：執行中。

## 完成與驗證
- 測試庫30筆已commit，3日各10筆；SamplingPhase=MIDDLE保留、SamplingStage=CLOSE，其他業務欄位逐筆相同，RowVersion由SQL正常遞增。
- 備份與結果：../../release-staging/n2-middle-close-20260918/test-20260918-093346/，含before.json/after.json/result.json。再次符合待修正條件0筆。
- 已部署SPC測試API實際GET daily：2026/9/11、9/14、9/16的N2/MIDDLE/CLOSE各exists=true、10筆，證據../../release-staging/n2-middle-close-20260918/api-verification.json。
- 需要回復時只依before.json中ID還原SamplingStage（先核對現在仍為本批結果），不覆寫其他欄位或整庫還原。
- 無程式修改，無需升版/發布；正式庫未動，仍待前案正式來源/Portal位置確認。
