# 計畫
1. 先建立測試，再新增 ProductionRelease.psm1、Invoke-ProductionRelease.ps1、manifest 範本與手冊。
2. 固定白名單正式主機/IIS 路徑，Plan/Apply/Rollback 三模式；服務與 SQL 適配器供隔離測試注入，CLI 不暴露替換參數。
3. 一次更新一台主機的一套系統；跨系統先 SPC 後 Portal，每套獨立備份及驗證，無分散式交易。
4. 檔案套用只接受 SHA256 已核對清單；環境、附件、私有資料保留；失敗復原原檔，不自動刪新增檔及 DB。
5. 文件與來源提交上傳；工具沒有網站產品改動，測試站發布不適用。
