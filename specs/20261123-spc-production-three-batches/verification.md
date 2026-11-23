# 正式發布驗證

2026-11-23 台灣時間 12:00 起分批完成正式發布。現行 SPC Web 0.1.146、API 0.1.139/production，Portal Web 1.0.287、API 1.0.262，資料庫環境 production。

| 批次 | 版本變動 | 來源與回復點 |
| --- | --- | --- |
| A | SPC Web 0.1.143 → 0.1.144；API 0.1.137 | 74fa57b8，SPC-PROD-1123-A-WEB144 |
| B | SPC Web 0.1.144 → 0.1.145、API 0.1.137 → 0.1.138；Portal Web 1.0.285 → 1.0.286、API 1.0.260 → 1.0.261 | 9a661422／675207c，SPC-PROD-1123-B／PORTAL-PROD-1123-B-R2 |
| C | SPC Web 0.1.145 → 0.1.146、API 0.1.138 → 0.1.139；Portal Web 1.0.286 → 1.0.287、API 1.0.261 → 1.0.262 | ad9f5346／800f9af，SPC-PROD-1123-C／PORTAL-PROD-1123-C |

- A 正式 build、三語及 28 項焦點測試通過。Chromium 核對正式 Web 版號、主資產 200/MIME、真實 8443 API health/version 與 production。
- B SPC API／Web 與 Portal 建置通過。SPC 焦點 44 通過，另 1 項屬性圖排除測試在原正式基線也同樣失敗；Portal 固定備註 4 項通過。
- B 正式庫唯讀比較同一個十月區間。PPC 2437 維持 16 筆、規則標記 0、Cpk 2.6786；I 圖 UCL 99.78203→101.12694。PPC 2620 維持 750 筆、15 子組、Cpk 0.6311；Xbar UCL 0.31775→0.41861，規則標記 8→0，S 圖界限未變。兩樣本活躍排除為 0。未回填或重納既有排除量測。
- C SPC 30 項 API、6 項前端稽核、1 項 C1 槽位排序通過。Portal 調整代理限制原有品保角色。正式確認兩個 Portal 入口觸發匿名登入要求，登入後真人驗收待補。
- B、C 皆通過正式目標預檢與備份更新。C 在 SPC 正式庫套用 20261120134446_AllowBusinessActionClientEvent，約束已啟用且受 SQL Server 信任。資料庫 migration 為向後相容，站台回復採同批備份，保留約束。
- 最終 Chromium 核對 SPC 首頁、Web/API 前台版號、主 JS/CSS MIME、正式來源實際跨來源 8443 health/version；Portal 登入頁、app-version、site CSS/JS MIME、8443 health 與 production 均通過。Portal API 1.0.262 以部署 DLL 驗證。
- C 待辦：正式 Portal 缺製程角色，管制值異動的品保／製程通知未切換。SPC 分類收件功能已發布，正式未配置分類名單、未增改生效收件人或寄測試信。正式業務按鈕稽核程式及約束已上線，登入／瀏覽稽核開關未啟用且缺登入嘗試 HMAC 金鑰，故正式業務按鈕稽核尚未運作。
- 回復根目錄為各正式主機 C:/ProgramData/PMR-Releases/Production，依上表 release ID 使用原備份。完成正式業務與登入驗收前保留未完成狀態。

- 同日續作：製程角色及指定帳號關聯已建立。稽核啟用因正式執行身分未通過啟動預檢，仍待處理。[驗證](activation.md)

- 正式 SPC 稽核已啟用，Web 0.1.147／API 0.1.139。TCP/TLS、前台資產及正式匿名登入拒絕稽核已驗證；真人正向流程仍待驗。[續作](activation.md)

## 正式正向稽核查回

- 2026-11-23 唯讀確認正式安全稽核開關啟用，正式入口使用 Web 0.1.152 主資產，首頁和主要 JS／CSS HTTP 200、MIME 正確，8443 的健康與版本回應為 `0.1.140/production`。
- 正式庫累計成功且具作業人員識別的事件，包括 12 筆 SSO、76 筆頁面進入、8 筆業務按鈕、55 筆篩選、151 筆查詢及 3 筆權限變更。已確認正向登入與操作確實留下記錄。本次僅查回統計，未重新執行使用者操作、寫入資料或發布。
- 正式通知收件人依使用者指示晚些指定，仍待配置與投遞驗證。
