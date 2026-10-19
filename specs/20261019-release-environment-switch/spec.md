# 發布環境切換與遠端入口
ID：RELEASE-ENVIRONMENT-SWITCH-20261019；狀態：工具完成；四組真實Plan通過；本次不Apply／Rollback。
主系統 SPC；相依 Portal；不改 schema、角色、binding、不實際 Apply／Rollback。
使用者確認以同一工具選擇測試／正式，兩環境位於不同電腦。
## 已核對基準
Test：PC-PA16560048.pmr.com.tw（172.16.110.27）；SPC SpcSingleTest，D:/SPC/release/test/backend＋frontend，單一 pool／site，DB PMR_SPC_TEST；Portal PmrPortalApi pool=PmrPortal 與 PmrPortalWeb，D:/PmrPortal/publish/portal-api＋portal-web，DB PMR_PORTAL_TEST。
Production：沿用已驗證兩台正式主機與路徑；禁止 D:/Sites/PmrPortal。
## 需求／BDD
- AC1 Given 選擇 Test／Production 與 SPC／Portal When 產包／執行 Then 只接受相同 manifest 環境及固定白名單，不自動更名環境或猜路徑。
- AC2 Given 遠端入口 When Plan Then 本地驗證交付 hash，選定固定主機，安全暫存必要工具／交付，實際主機再次驗證 IIS、DB；不停止服務、備份或遷移；WhatIf 不連線不寫入。
- AC3 Given 已授權 Apply／Rollback When 遠端執行 Then 沿用既有備份／授權／回復；拒絕交叉環境及錯誤主機，不儲存帳密或變更 WinRM trust。
- AC4 Given SPC 測試同一 site/pool 兩個內容目錄 When 核對 Then API／Web 目錄均綁同站 backend 物理路徑，避免誤認獨立 SpcWeb。
- AC5 Given 本次只開發工具 When 測試 Then 用隔離替身驗證傳輸與參數、既有測試回歸；允許只讀真實 Plan 驗證，不執行站台更新。
## 相容與限制
保留既有正式入口，只允許 Production；新增共用 server engine 及環境切換遠端入口。相同 API 交付可依環境產 manifest；SPC 前端目前包含環境 API／Portal URL，需相同來源版本分環境 build，不宣稱既有前端 ZIP 能直接跨環境。未來 runtime config 不屬本次範圍。
## 測試規劃
Happy：四種profile、遠端正確傳輸／執行；Boundary：同一 pool、32位元工作機／64位元IIS；Invalid：錯環境／錯host／hash／授權；Regression：舊正式 wrapper、SQL備份與回復、設定保留。
Rollback：還原工具來源，不需要變更網站／DB。
