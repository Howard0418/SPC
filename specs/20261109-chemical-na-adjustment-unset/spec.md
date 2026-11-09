# 藥液NA調整條件保留未設定

- 父工作：`SPC-CHEM-F-CONSISTENCY-1103-TASK-001`；本階段只處理測試區四個已確認來源為文字`NA`的調整條件。
- 目標PPC：2515、2516、2518、2619。來源`U13/U14/U16`為`NA`，不得當成0、USL或臆造數字。
- Given P400／安定劑仍有低值添加規則，When 移除NA上限分支，Then 保留低值添加與添加量，濃度式、複驗式、精度及其他設定不變。
- Given N2促進氯化亞錫只有`Concentration>U16`更槽規則，When U16為NA且更槽工作已取消，Then 調整式與調整量式均為未設定。
- Given config不含指定未定義參數、PPC不在白名單或設定已漂移，Then 不修改或明確拒絕；相同結果重送0異動。
- 先以純planner紅綠測試保護Happy、Boundary、Invalid與Regression，再對測試庫備份、版本留痕、交易套用及重驗。正式區不在本階段授權範圍。

## 驗證結果（2026-11-09）

- `ChemicalUnsetConcentrationTests`：20/20 通過；Release build 0警告／0錯誤。
- dry-run 精準命中4項；以既有測試維護身分完成 `PMR_SPC_TEST` 的 `COPY_ONLY/CHECKSUM` 備份及 `RESTORE VERIFYONLY` 後，交易更新4項並新增4筆公式版本。
- PPC 2515、2516、2619保留低值添加與添加量，移除未定義的U13／U14上限分支；PPC 2518的純U16更槽規則改為未設定。4項濃度式與複驗式保持。
- 量測、Receipt、Trace、F表、排程及管制線等8組受保護資料指紋前後相同；重跑 `planned=0 / remaining=0`。
- 測試API `0.1.115 / test` 的health為ok。無網站檔案發布，正式資料庫未異動；父工作仍保留其他兩式、EXE及正式畫面待辦。
