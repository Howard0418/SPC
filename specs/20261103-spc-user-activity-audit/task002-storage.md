# TASK-002：稽核儲存與共用writer

- 狀態：進行中；2026-11-03使用者授權「執行」。
- 基準：[已核准契約v1](event-contract-v1.md)；需求R-002、R-003（儲存邊界）、R-010、R-011。
- 主系統SPC；相依SQL Server，新增AuditEvents與角色／防修改trigger；無Portal、SSO、前端變更。正式發布另授權。

## 規格與計畫

1. 純新增AuditEvents，不繼承BaseEntity，不套soft delete或System預設actor，不設Operator外鍵以免刪主檔連帶刪證據。server產生EventId與UTC，環境由設定解析（實站Runtime為Production而應用標籤為test，不能使用Runtime推斷環境）。
2. 固定欄位、限長、事件／結果／來源allowlist、Details白名單及4KB UTF8上限；拒絕未知或敏感payload，錯誤訊息不包含原文。共用writer只供伺服器服務，無新增公開API。
3. StageAsync只把事件加入同一AppDbContext，不Save／commit，呼叫者與業務資料同交易。AppendAsync使用獨立context，只提交單一事件，禁止意外儲存呼叫者尚未提交資料。不得吞錯；一般／關鍵失敗策略由後續呼叫端依契約接入。
4. ClientEventId只適用已驗證頁面事件；去重key包含環境、operator、session hash、client id。同key同內容回既有事件；同key不同內容拒絕。SQL應用鎖與唯一filtered index支援併發，非頁面事件無client冪等key。DeduplicationHash僅比較語意內容，不包含每次傳輸會變的TraceId／CorrelationId／HTTP，也不是防竄改雜湊。
5. 新增時間／actor／event／target／correlation索引。EF阻擋update/delete；SQL trigger與分離writer role補強直接SQL。maintenance role無預設成員，後續008才設計受控清理。不可改既有runtime帳號／全站權限。

本件只建立SpcAuditWriter（SELECT／INSERT，DENY UPDATE／DELETE／ALTER）與SpcAuditMaintenance空角色，不自動變更既有帳號成員。既有高權限runtime仍可透過管理DDL繞過trigger，不能宣稱已防DBA竄改；003啟用收集前須再核對分離runtime與migration身分，不能以本件角色定義取代實際principal驗證。兩個安全trigger仍會阻擋一般直接UPDATE／DELETE，包含高權限連線。

## BDD與測試

- Happy：Given合法伺服器actor與事件，When追加，Then持久化server EventId／UTC／正確環境，白名單Details有效。
- Boundary：Given同session重送或併發頁面事件，When追加兩次，Then只有一列且EventId相同；不同session或actor不互相去重。
- Invalid：Given未知事件／來源／頁面、未驗證身分快照、空session、超長或敏感Details，When寫入，Then安全拒絕且無資料。
- Regression：Given業務資料與Stage事件共用transaction，Whenrollback或事件insert故障，Then兩者均無變更；獨立追加不提交既有context變更。
- Append-only：Given既存事件，WhenEF或直接SQL更新／刪除，Then拒絕；未啟用維護角色不能刪除。
- Migration：隔離SQL DB於前版→Up→Down→Up，舊表筆數／指紋不變；確認索引、約束、角色與trigger。Down只允許空AuditEvents，避免誤刪證據。

## 發布與回復

測試必要build/test與隔離migration通過後備份測試DB／API，發布測試API與新增migration；前端保持既有Web115。核對實際DB=PMR_SPC_TEST、應用環境=test、API版本及Web／JS／CSS MIME、health/version、瀏覽器實際API基底。回復前版API並保留稽核表；不對有事件的表執行Down。003～009不自動開始。

## 驗證紀錄

已先建立測試確認缺少writer類型而失敗，再實作；初次17項SQLite測試通過。隔離SQL以8個併發追加驗證僅一列，驗證直接SQL／writer角色禁止UPDATE／DELETE／ALTER、禁止Down刪有資料的表，以及同交易rollback／insert故障時全量既有表指紋不變。隔離SQL測試通過，建立的獨立SPC_AUDIT_VERIFY資料庫於finally清除；只含合成測試資料。

既有歷史migration無法從空庫直接建立（ControlScope歷史落差）；不擴張修舊migration。隔離測試用前一筆migration的精確TargetModel建立基準並記錄歷史，再跑本件Up／Down／Up；實際測試站另核對現有history與僅待本件migration。schema或writer完成不代表全站登入／進頁已開始留痕。

完整回歸：主測試342通過／8略過（含明示啟用的SQL隔離測試）；另一後端測試66通過／1略過，既有nullable警告2處。Release API0.1.98產包成功。實際測試DB盤點PMR_SPC_TEST／test，68個既有user table，59筆已套migration，本件為唯一待遷移ID。
