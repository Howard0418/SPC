# 正式執行身分與最小權限契約
- TASK-001／v1；契約與正式最小執行身分驗證已完成，尚未授權排程或正式匯入。引用[主規格](spec.md)。

## 目標核對
|層次|測試|正式|拒絕條件|
|---|---|---|---|
|SPC|spctest.pmr.com.tw／environment=test／PMR_SPC_TEST|已核對正式API位於PMR-SPC-SRV，environment=production／PMR_SPC_2026；對外HTTPS網址須由核准profile提供|URL格式不符、實際environment不符／未知、目標DB不符不得preview／prepare／confirm。|
|Portal|portaltest.pmr.com.tw／Target=Test／PMR_PORTAL_TEST|既有PMR-WEB正式API，Target=Production／PMR_PORTAL_UAT；對外HTTPS網址須由核准profile提供|只登入成功不等於正式匯入權限；Portal→SPC上游必須同環境，不能把test-only端點改網址當正式路徑。|
|執行端|既有個人Windows測試授權只限測試|PMR-SPC-SRV（172.16.119.140）／PMR\ihao_ting；SQL Server登入既有且啟用，預設庫仍為PMR_SPC_TEST，PMR_SPC_2026尚無使用者映射|認證到期、正式庫無映射、檔案來源無讀取權限、API／DB actor不一致則拒絕，不用管理者兜底。|

## 正式HTTPS與最小變更結果
- 2026/10/28唯讀確認：正式SpcApi只有HTTP `172.16.119.140:8081`；HTTPS 443屬SpcWeb靜態前端，沒有`/api`反向代理，因此對`https://PMR-SPC-SRV.pmr.com.tw/api/version`的406不是API環境回應。工具維持HTTPS及禁止8081的安全規則，不降級TLS或繞過憑證。
- 使用者另行授權後，SpcApi已新增hostname `PMR-SPC-SRV.pmr.com.tw`的HTTPS 8443 SNI綁定，使用既有`*.pmr.com.tw`憑證；防火牆只允許實際工作端`172.16.110.27`及Portal主機`172.16.119.130`進入8443。Portal上游已改走完整TLS HTTPS，核准profile目標為`https://PMR-SPC-SRV.pmr.com.tw:8443/api`；不改SpcWeb 443或前端路由。
- 正式資料庫已建立`PMR\ihao_ting`對應user並加入既有`SpcChemicalDirectWriter`受控程序角色；未改SQL登入預設庫、未授予db_owner、直接DML或任意SELECT。`EXECUTE AS USER`驗證兩個受控程序EXECUTE=1，VariableMeasurements的SELECT／INSERT／UPDATE／DELETE=0。
- 遠端WinRM整合連線18456可能受Kerberos double-hop影響，不能當成本機排程失敗證據；後續須在執行主機以實際排程登入型態驗證，不啟用CredSSP、不索取或保存使用者密碼。

匿名version只提供應用環境，不能證明資料庫、角色或正式授權；DB核對與權限證據須來自該執行身分的受控唯讀檢查，不能以應用管理連線查到INSERT=1代替個人／排程資格。

## 各類權限
|類別|API身分及允許操作|DB邊界|證據／授權要求|
|---|---|---|---|
|藥液|核准SPC身分讀主檔／日期預覽，Editor對應prepare；receipt actor由伺服器認證主體產生|Windows整合認證、僅execute受控commit／status程序；不授予db_owner、直接DML或任意SELECT|現有程序正規化DB USER_NAME的DOMAIN\\user／user@domain後，須與receipt actor一致；不偽造@Actor或換共用管理者連線。正式client接入留003。|
|咬蝕|Portal核准品保／SPC權限及正式受控端點，維持上游SPC權杖、兩站同步狀態|工具不直接用兩個正式DB管理連線；寫入交正式服務之限定操作|現行兩站test-only守衛保留，正式路徑由004提供；同run／同日報鍵與actor全程可追溯。|
|落塵|核准SPC匯入權限，僅Particle preview／查詢／批次確認等必要操作|工具不直接INSERT Particle表；走既有批次契約|限定來源／粒徑／位置與批次，重複／衝突策略沿服務；正式adapter留005。|
|來源與日誌|執行帳號僅需來源讀取權、專用報告／狀態目錄寫入權|無刪除／改寫來源需求|GUI與排程帳號須分別核對UNC可讀；禁止把所有共享資料夾或來源修改權當預設。|

## 非互動認證的必要決策
- 現有Portal是帳號／密碼登入，已以PMR\ihao_ting CurrentUser DPAPI保存並完成非互動登入／SSO唯讀驗證；不能將當次瀏覽器cookie／長期JWT當排程憑證，也不能宣稱現有gMSA可直接登入此API。
- 優先沿核准Windows執行身分與受控憑證保管；若沿個人／服務帳號帳密，須確認組織允許無人值守、帳密可更新及最小角色。憑證不得寫入JSON、參數命令列、日誌或Git。
- 如果選gMSA／服務身分但Portal不支援，先列出認證支援改動及actor對應再另審，不私自改AD／SSO或建立帳號；不能讓流程只在登入者GUI開啟時才成功。
- 正式執行資格需綁定：主機、Windows主體、Portal/SPC認證主體、允許資料類別／來源／日期範圍、執行窗口、授權依據。這些屬後續可驗證設定，不由本機JSON自己宣告即視為服務端授權。

## 驗證與拒絕矩陣
- 主機／帳號、正式HTTPS、正式庫映射、非互動Portal認證、SSO launch與唯讀主檔已通過；錯API environment／DB→拒絕；僅Viewer→不可prepare／confirm；API actor與DB主體不同→拒絕；來源不可讀→略過／失敗並指出原因；認證過期→停止該run、不重送寫入。
- 所有查核記錄分開標示「設定格式」「實際環境」「身分一致」「角色／權限」「來源範圍」「正式啟用授權」；前兩項通過不代表後四項通過。
- 正向身分驗證先在隔離／測試環境，正式只在核准後唯讀核對角色／程序及錯誤拒絕，實際小批寫入依008授權與回復範圍，不以核對時的管理者讀取結果冒稱本人已通過。
- 本件回復只涉及工具來源防護，沒有正式角色、資料或排程可撤回；未授權狀態持續停用，不自動接下一個Task。
