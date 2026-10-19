# 計畫
1. 唯讀核對兩台正式主機 IIS、實際版本、應用設定的非敏感環境欄位與 migration 差異。
2. 整理來源與待發布範圍，必要 build/test；Portal 用既有 publish-all 產生隔離包，SPC 用 Release publish 與正式專用前端設定，不複製測試網址/設定。
3. 對正式 schema 變更及其他阻擋先列明具體核准項，不自行正式改資料。
4. 核對 manifest/檔案 hash，正式主機備份網站/IIS，確認 DB 回復界線；先 SPC 再 Portal API/Web，逐件驗證。
5. 同步文件、必要來源提交與上傳；不提交套件、備份、憑證或測試輸出。
Rollback：使用同一 releaseId 備份回復應用；若有 DB 變更，需事先核定獨立 DB 備份/相容性回復，不以網站備份替代。
