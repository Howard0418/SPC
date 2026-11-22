# SPC-SIDEBAR-1122 左側導覽 16 px

SPC 共用側欄的導覽群組與連結調為 16 px，較長譯文可換行。保留既有路由、角色、手機堆疊與捲動。僅 Web，無資料庫及 API 異動。

- Given 打開 SPC 並顯示側欄，When 使用繁中、簡中或英文，Then 群組與連結的計算字級為 16 px，文字仍可換行且連結可操作。
- 驗證 testhost build、實際 Web／同源 API／8901。回復採前版 index 與資產備份。
