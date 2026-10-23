# 偏好持久化驗證設計（尚未實作）
- [方案](preferences-plan.md)｜[TASK-03規格](../../specs/20261024-i18n-task03/spec.md)｜[契約案例](examples/preference-api-cases.json)。
- 本Task只檢查文件與JSON案例一致性；下列為TASK-09/跨站介接要執行的驗證，未聲稱已跑API/DB或隔離演練。

| 類別 | 必要案例 | 可觀察結果 |
|---|---|---|
| Happy | Portal個人無偏好→首次保存→重登入→清除 | null/zh-TW→指定locale持久化→仍本人值→回預設；角色不變 |
| Happy | AD與NFC都映射同Users.Id | 同一偏好，不按登入輸入工號另建一份 |
| Boundary | 三個精確locale、同值PUT、原無列DELETE | 合法保存/無意義寫入可no-op/204 |
| Boundary | 首次同時建立、兩裝置不同revision | 唯一UserId，失敗412，不默默last-write-wins |
| Invalid | null/空字串/alias/大小寫/尾空白/超長 | 400且不變更舊值；DB binary/長度CHECK亦拒絕 |
| Invalid | query/body userId/帳號/工號或admin代改 | 拒絕，不可寫別人；路徑無任意ID |
| Invalid | 未登入/缺NameIdentifier/User停用或已刪 | 401/403，沒有偏好寫入或新增User |
| Invalid | cookie跨來源或缺防偽token | CSRF拒絕；既有正常same-origin proxy仍可用 |
| Invalid | 直接SPC JWT呼叫Portal、KM未驗證帳號 | 不接受為Portal本人，不自行查工號猜身分 |
| Invalid | local_guard共用帳號PUT | 依提案403，本頁session可變語言；政策待確認 |
| Invalid | DB離線、回應格式錯誤 | 安全失敗、不假稱保存；UI/表單/查詢狀態保留 |
| Regression | A登出B登入、相同工號/同名、不同環境 | 不沿用A偏好或把A的pending寫給B，Test/Production分開 |
| Regression | 網路亂序GET或儲存回應 | 新明確選擇不被舊結果覆蓋；成功revision與當前subject配對 |
| Regression | ETag過期/刪除與PUT競爭 | 412；使用者選擇後才條件式重試 |
| Regression | 省略新增偏好表的舊版應用回復 | 舊路徑照常，資料/角色/SMTP/SSO不改 |
| Regression | 三語相同數值/日期/圖表/文件權限 | API數值/商業日/ACL一致；僅顯示改語言 |

## 後續測試層級
- 單元：locale allowlist/subject解析/快取scope/回應亂序狀態機；失敗不可寫。
- API整合：真實cookie/CSRF中介層、只本人、多使用者、NameIdentifier失效、ETag/412/428，不只直接呼叫Controller。
- SQL Server隔離庫：實際EF migration/rowversion/unique/FK/CHECK及跨request transaction並發；不連正式製造錯誤。
- Browser：Portal/Razor/Mobile/既有Vue2各入口、未送出表單、語言切換/失敗/重登入/換人；SPC/KM介接完成後才測跨站。
- 部署：目標/備份/設定hash/舊版回復核對，無schema授權時不可部署含新migration的應用，避免啟動自動遷移。
