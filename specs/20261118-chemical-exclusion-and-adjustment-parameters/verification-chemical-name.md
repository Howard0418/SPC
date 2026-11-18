# 藥液欄位驗證

- 日期：2026-11-18
- 工作：`PORTAL-CHEM-ADJUST-PARAMETERS-1118-TASK-001`
- 結果：調整公式頁新增選填藥液名稱；前後端皆限制 100 字，保存於既有設定 JSON 並寫入公式版本歷程。
- 自動驗證：Portal API 293 項、Web 51 項、SPC 維護 12 項通過；Portal API／Web Release build、SPC API publish 與 Vue build 通過。
- 測試發布：Portal API 1.0.271／Web 1.0.309；SPC API 0.1.139／Web 0.1.145。Web、JS、CSS、health、version 與 test 環境標籤驗證通過。
- 人工驗收：待使用者於測試區選擇線別，輸入藥液名稱、儲存並重載確認。
- 正式區：未授權、未發布。
- 回復：Portal 使用發布前輸出；SPC 使用 `backup-control-notify-test-20261118-214836`。本次新建備份目錄因萬用字元搭配 LiteralPath 未複製內容，不作為回復來源。
