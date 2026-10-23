# 技術計畫：I18N-TASK-05A v1
- [規格](spec.md)｜[任務](tasks.md)
## 檔案與方式
兩Vue View用global useI18n與common/spc.parts/spc.characteristics穩定key；明確顯示映射，不改models/列舉/送出路徑。前端驗證/成功訊息改存key/args，computed渲染讓既存訊息即時換語言；API原文字獨立保留。刪除confirm即時t插值。
新增LocalePicker與injection key，App provide原locale/changeLocale，兩抽屜復用；不重建router-view。ModuleGuide僅增加可選的toggle aria label，不翻其他模組。所有新字典/placeholder/hash納入draft帳本。
## 測試
先建使用者行為測試（預期三語標題尚未出現而失敗）。不變更業務核心，無需新業務服務單元測試；新增參數/metadata/靜態完整性測試。Happy Path三語CRUD mock；Boundary空清單/0筆/小數0和8/長文字；Invalid空白/拒絕/取消/字串參數；Regression篩選/抽屜草稿/狀態/原DTO/Viewer及既有shell測試。
所有瀏覽器API完整攔截，測試寫入僅fixture，禁止合成token或資料送實際後端。桌面三尺寸與390寬度三語。
## 發布與回復
只升SPC Web0.1.73，API沿用0.1.72。build:test通過後唯讀核對SpcSingleTest/AppEnvironment=test，備份前端逐檔SHA256、保護設定；assets先/index最後更新D:/SPC/release/test/frontend，不停pool/不改DB/不發布正式。
回復同批前端備份（Web0.1.72）並保留設定/現場資料；只提交必要檔案、上傳Develop後STOP。
