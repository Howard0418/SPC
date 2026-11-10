# 驗證紀錄

- 設定模組測試4項通過：預設值、零類型拒絕、保存重讀、類型篩選。
- 四支PowerShell檔案語法解析0錯誤；桌面捷徑 `PMR 每日匯入開關.lnk` 已建立。
- 11/09正式唯讀：只選藥液為67筆existing_same、咬蝕0；只選咬蝕為3份existing_same、藥液0；兩者均NoChange且未寫入。
- 總開關關閉時runner回報Disabled，未掃描來源或連線資料庫；之後已恢復總開關、藥液與咬蝕量皆啟用，排程Ready。
- 設定檔：`C:\ProgramData\PMR\TransFilesDaily\settings.json`；不含密碼。回復方式為移除桌面捷徑與設定檔，runner會回到兩類皆啟用預設。
- 網站、API、schema及正式資料均未修改，網站發布不適用。
- 最終兩類皆啟用後，以Windows排程再觸發一次：`LastTaskResult=0`、summary=`NoChange`、chemical=true、etching=true，下一次仍為2026-11-11 09:00。
- 中央進度表已備份並更新：登錄244、核心未完成33、完成209、SPC相關9；Artifact公式掃描及OpenXML SDK驗證0錯誤。
