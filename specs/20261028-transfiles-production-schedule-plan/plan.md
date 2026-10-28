# 技術與執行計畫
- 規格：[v1](spec.md)；本次僅規劃，不實作／設定／發布。

## 系統與檔案責任
- TransFiles：沿connection_settings.py、batch_convert_gui.py、scheduled_import.py、chemical_direct_writer.py／ps1、etch_upload.py、三類既有解析器；未來新增headless runner及排程設定管理，保留舊交付EXE與現行測試流程。
- SPC：正式寫入契約、環境／actor／範圍驗證、日期公式追溯、統計狀態、Particle批次與圖表。正式咬蝕入口應有明確用途及授權，不把etch-import-test改成不受限的通用寫入。
- Portal：咬蝕雙站一致性及正式認證；非互動認證採核准的執行身分，不要求排程彈出GUI登入、不存長效使用者token、不冒用個人actor。
- TransFiles非Git repo；本規格在SPC集中版控，工具docs／TODO只引用。未來程式交付沿最小patch／模組索引方式，不把整份來源／機敏設定／EXE塞進SPC。

## 資料與狀態契約
- 設定版本化：三類source profile、環境、檔案模式、排程時間、時區、啟用／模式、回補範圍、重試／筆數上限、輸出位置。敏感憑證走核准Windows認證／保管機制，不寫JSON。
- 路徑檢查必須使用實際排程身分，優先UNC。來源不變連續檢查＋唯讀快照hash，讀取中變動拒用；只處理指定範圍，不自動掃整個共享磁碟。
- 每次run與每個來源／業務單位分開狀態：planned／validating／previewed／committing／imported／pending-statistics／failed／conflict／excluded／skipped／partial。新hash不等於新業務資料；失敗與只掃描不能更新已成功檢查點。
- 藥液沿單筆初複驗交易及receipt，咬蝕沿完整日報與兩站Pending→SUCCESS續傳，落塵沿完整預覽批次；不宣稱多檔／跨兩DB全域交易。有部分落地時記partial，只重試未完成單位，整檔不可假報成功。
- 檢核總和以同一粒度對帳；檔案數、日報份數、點數、量測列數／案件數分開，空白與0、合法排除與錯誤分開。

## 測試與依賴
先001／002共用基礎，再依序003藥液、004咬蝕、005落塵，後006可靠runner、007排程設定、008交付與正式分階段驗收。每次只做一件，不併行正式資料／權限／migration工作；核心先測試再實作。
各Task至少覆蓋Happy Path／Boundary／Invalid／Regression，見[tasks](tasks.md)。測試場景依實際契約使用隔離DB／mock；正式證據另外記錄，不能以測試成功取代正式寫入查回。

## 發布與回復
- 本次無應用變更，build／測試站發布／EXE打包不適用；不執行任何既有scheduled_import命令或Windows排程安裝。
- 未來SPC變更驗證後依AGENTS發布測試；Portal沿其根發布腳本；TransFiles新EXE獨立交付，留舊版與設定版本／hash，避免覆寫執行中工具。
- 正式網站／DB權限／排程安裝與啟用／正式小批匯入均須在具體目標、來源、備份與回復驗證後另行授權，本次規劃不是其授權。
- 緊急停止：停用排程與profile、停止發起新工作，保留已落地批次／trace／日誌；還原本件應用及相容設定。正式資料依run／batch ID核對補正或受控還原，不工具端直接刪除。
- 原進度表只增加規劃登錄與10/28規劃里程碑，不畫未來開發工期，不改已完成或暫緩工作。
