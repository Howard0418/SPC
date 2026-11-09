# Portal 正式發布 smoke 端點隔離修正驗證

日期：2026-11-09

## 結果

- Root cause 1：Portal manifest誤用SPC專用`/api/etch-import-test/preview`，已改為Portal自有唯讀`GET /api/PortalLinks`，匿名預期401。
- Root cause 2：Portal Production API基底使用會回307的HTTP 81，但發布smoke固定`MaximumRedirection=0`；已改為實際`https://login.pmr.com.tw:8443`。
- 測試先紅後綠：`ReleaseEnvironmentSwitch.Tests.ps1` 15項、`ProductionRelease.Tests.ps1` 21項通過；SPC manifest原咬蝕量端點保持。

## 正式發布

- R1／R2於`start-and-smoke`失敗並回復原檔；發布工具回復smoke同受錯誤端點影響而停住集區，已立即恢復原版本服務。沒有migration或資料異動。
- R3完成後發現API包雖版本同為1.0.218但為不同編譯雜湊；依只更新Web範圍立即執行R4，以發布前保存的API包還原原正式API，保留Web 1.0.248。
- R4狀態`completed`，API／Web集區Started；API 1.0.218.0、SHA256 `F8AAF4BEDE6101F608C2E3E63FB4532A736B5323D3B3A947E90AC7531E96D7FA`與發布前相同，Web 1.0.248.0。
- `PMR_PORTAL_UAT` COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY通過，無migration。
- 正式首頁、`app-version`及實際`site.css` HTTP 200，HTML／CSS MIME正確；CSS含1440px、96vw、fit-content。API health為`ok`、database=`ok`、dbEnvironment=`production`；匿名PortalLinks回401。
- 電腦操作工具原生pipe不可用，未取得正式登入後畫面；列為人工確認，不宣稱自動瀏覽器驗收完成。

## 進度表

- 更新前備份：`Voice/備份/進度表-生日賀卡正式發布前-20261109-170940.xlsx`，SHA256 `916CF224198E82B61C8E430304102868F1A4A0BD2DCFF82CC54546A904C07966`。
- Portal摘要、11/09正式里程碑與小工作270列已同步；核心未完成41、SPC相關18、完成198、人工12。
- Artifact重開／渲染與公式錯誤掃描通過；ZIP 13 entries、OpenXML schema 0錯誤。指定原檔輸出SHA256 `8A1EEC243C3583AE8977332292F7C2D481B8E13406882C3CE482E26657EFC4AF`。未執行Microsoft Excel人工開啟。
