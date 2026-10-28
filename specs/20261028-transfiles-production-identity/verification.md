# TASK-001 執行紀錄
- 2026-10-28；狀態：契約與工具來源環境防護已完成測試；主機／帳號、正式HTTPS與正式庫最小權限已核准並驗證。非互動Portal認證／憑證保管待確認，001未結案；002～008未開始。

## 已完成
- 已比對[原規劃](../20261028-transfiles-production-schedule-plan/spec.md)、connection_settings／connection_test、Portal帳密登入、ChemicalFormulaScheduleController及ChemicalDirectWriterSqlV1的actor／USER_NAME要求，寫成[正式目標／身分／最小權限契約](identity-contract.md)。沒有預先認定gMSA或既有個人測試帳號可供正式無人值守使用。
- 新增4個隔離測試方法、12個正反情境，舊碼出現12個失敗；來源修改後全部通過。現有GUI測試登入函式現在先匿名GET SPC `/api/version`，環境不符／缺失／非字串／未知／無法取得時，在Portal client建立／login／token之前拒絕。
- 正向test與production fixture維持Portal登入→SPC token→CHEM唯讀主檔；version請求headers無Authorization或帳密。不是新增正式寫入入口，也不以version環境當DB／角色／授權證據。
- 回歸：test_connection_settings／test_tls_context／test_etch_upload／test_batch_convert_gui／test_chemical_direct_writer共61通過；既有GUI故障回呼4通過（4階段×3類網路錯誤，安全提示／按鈕恢復），新增環境防護4通過。合計69項，相關py_compile通過。
- 既有GUI故障測試fixture改成先回version成功，再於所測階段失敗；新增環境核對失敗情境，並核對只發出version GET／不登入。不核銷舊EXE人工故障驗收。
- TransFiles非Git repo，實際兩檔變更保存為[最小patch](../../integrations/transfiles/production-identity/README.md)；獨立來源副本通過apply-check、apply及UTF-8／LF正規化逐字比較。CRLF來源及patch換行差異已處理，不以無法apply的產物交付。

## 正式環境唯讀診斷
- 使用者指定172.16.119.140／ihao_ting；Kerberos唯讀核對實際主機PMR-SPC-SRV、完整帳號PMR\ihao_ting及同一Windows SID。既有SQL Server WINDOWS_LOGIN已啟用、預設庫PMR_SPC_TEST；PMR_SPC_2026沒有對應database principal。沒有建立user／role／grant。
- 以現有應用連線只查身分中繼資料；另以目前Windows身分新建Integrated Security連線回18456。遠端工作階段只有Impersonation，結果可能含Kerberos double-hop，不能據此宣稱本機排程整合認證失敗或自行啟用委派。
- IIS紀錄證實Python請求到SpcWeb 443並回406；正式SpcApi實際只綁HTTP 8081，SpcWeb沒有web application或rewrite rule。主機內直接GET 8081 `/api/version`回200、environment=production、version=0.1.90；這不符合工具HTTPS契約，不能寫入核准profile。
- 8443未監聽，主機已有含私鑰、涵蓋`*.pmr.com.tw`且有效的憑證。已在[契約](identity-contract.md)記錄最小HTTPS 8443與正式庫受控程序角色候選；尚未變更IIS／憑證／防火牆／資料庫。

## 尚未完成／實際環境限制
- 尚待確認Portal非互動認證與憑證保管方式；使用者指定個人帳號及本次HTTPS／DB授權不等於允許保存密碼或建立排程。
- 使用合成輸入、Mock Portal factory及現有TLS helper，只匿名GET已知正式SPC HTTPS `/api/version`，測試誤選test應在登入前拒絕。初次沙箱未取得可確認回應；允許網路後實際HTTP406，沒有取得environment，因此只確認未知／失敗時未建立Portal client，不宣稱真實錯置環境已回production或正式登入成功。
- HTTPS406原因已定位並建立核准API端點；本件未把正式URL或密碼寫入本機profile，後續設定仍須配合非互動認證與安全保管。沒有修改SpcWeb 443、停用TLS驗證或以HTTP繞過。
- 不建立帳號、角色／grant、資料庫連線、prepare／preview／confirm或Windows工作；沒有讀取／保存真實帳密與權杖。69測試均mock／暫存，不是正式正向寫入或排程驗收。

## 進度、交付與回復
- 原進度表已同步：TransFiles第5階段摘要／同表G29與S29、222列001狀態「進行中，待身分確認」及日期2026/10/28，新增藍色原生來源測試標記312；原25個物件及其他專案儲存格保持。
- 核心未完成仍34、完成171、登錄221、人工11／核銷4；原表結構／列印範圍保持。ZIP、OpenXML SDK0錯誤、Artifact重開計數及公式錯誤0、LibreOffice重開／12頁列印通過，未宣稱Microsoft Excel人工驗收。
- 備份Voice/備份/進度表_轉檔001身分核對前_20261028-145931.xlsx；原路徑SHA256 20F8901F4AAD1D2FAE6C94E39B16CC02B7A0FED58E82FBEFFC0BE44132951F0D。取代前核對未變，原路徑再讀及OpenXML0錯誤；需回復先核對沒有後續更新。
- 程式回復：使用本機.codex-tmp/transfiles-identity-20261028的兩個before.py，或核對無後續修改後反向patch；不刪資料／改角色。舊EXE未覆蓋、未重新打包，008才處理打包／正式驗收。
- SPC／Portal沒有產品程式變更，網站build／測試站與正式發布不適用。工具來源已實作不等於舊EXE已更新；文件與來源patch依本件提交上傳。

## 續作進度同步
- 原進度表已將001更新為「進行中，待HTTPS／權限核准」，主表補上主機身分已核的橘色原生里程碑313；原26個圖形逐一保持，其他工作列及核心34／完成171／人工11／核銷4計數不變。
- 更新前來源SHA256仍為20F8901F4AAD1D2FAE6C94E39B16CC02B7A0FED58E82FBEFFC0BE44132951F0D；備份`Voice/備份/進度表_轉檔001主機身分核對前_20261028-152217.xlsx`，取代後SHA256為355100964FB97C488873E4A0D417D2EBFA895E63ED3B691E9F15CB6C28439C3E。OpenXML SDK 0錯誤、Artifact重開／公式錯誤0、LibreOffice重開／13頁列印及變更區目視通過；未宣稱Microsoft Excel人工驗收。

## 正式HTTPS與最小權限發布
- 使用者明確允許後，先以IIS backup建立`TransFiles001-20261028-152600`；SpcApi新增`172.16.119.140:8443:PMR-SPC-SRV.pmr.com.tw`的HTTPS SNI綁定，沿用有效wildcard憑證。防火牆初設LocalSubnet無法涵蓋工作端；較寬`172.16.0.0/16`被安全審核拒絕，最後只允許實際工作端`172.16.110.27`，沒有擴及其他來源。
- 從工作端以Python預設TLS驗證GET `https://PMR-SPC-SRV.pmr.com.tw:8443/api/version`回200、environment=production、version=0.1.90；沒有關閉憑證驗證。TransFiles `normalize_api_url`接受並正規化此HTTPS 8443 `/api`網址。
- 同一授權下於PMR_SPC_2026建立`PMR\ihao_ting` database user並加入既有`SpcChemicalDirectWriter`。`EXECUTE AS USER`驗證角色成員=1、兩個受控程序EXECUTE=1，VariableMeasurements SELECT／INSERT／UPDATE／DELETE均=0；沒有執行程序、prepare／confirm或任何資料寫入。
- 回復：先停用使用此端點的後續設定；移除`SPC API HTTPS 8443`防火牆規則、HTTPS binding與對應SNI SSL binding，或以IIS backup回復。DB回復須先`ALTER ROLE SpcChemicalDirectWriter DROP MEMBER [PMR\ihao_ting]`，再`DROP USER [PMR\ihao_ting]`；不刪SQL Server既有login。執行回復前須確認沒有後續工作依賴。
- 原進度表再同步為「進行中，待Portal非互動認證」，補綠色原生里程碑314；原27圖形保持加1，核心34／完成171／人工11／核銷4不變。更新前SHA256 355100964FB97C488873E4A0D417D2EBFA895E63ED3B691E9F15CB6C28439C3E，備份`Voice/備份/進度表_轉檔001正式HTTPS權限前_20261028-152918.xlsx`；更新後SHA256 562F261C7CFDCB7F3898A4C692E45AC12E0BA337280BBF485804E2F76C0C84DF。OpenXML 0錯誤、Artifact重開／公式錯誤0、LibreOffice重開／13頁列印及變更區目視通過。

## DPAPI憑證工具
- 使用者允許CurrentUser DPAPI後，新增Python讀取／互動建檔模組及PowerShell 5.1互動工具；7項測試通過，涵蓋只存密文、用途拒絕、竄改錯誤消毒、ACL失敗不留新檔、移除冪等，以及Python產生密文可由PowerShell解密。py_compile通過，另以合成密碼完成真實DPAPI往返，檔案不含明文並已刪除。
- 正式主機無可用Python runtime，故部署純PowerShell腳本至`C:\ProgramData\PMR\TransFiles\credential-tools`；SHA256 `E1D093C76BA488E5F480288B6FBDFE20BEAEC965211DD10FE8C76D713C714D39`。目錄ACL：SYSTEM／Administrators FullControl、PMR\ihao_ting ReadAndExecute；沒有真實密碼、憑證檔或排程。
- 待本人登入PMR-SPC-SRV互動執行`powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\ProgramData\PMR\TransFiles\credential-tools\Provision-TransFilesPortalCredential.ps1 -Action Provision`，密碼輸入兩次且不顯示。完成後以`-Action Validate`只回帳號／可解密；未完成這一步前001保持進行中。
- 回復：`-Action Remove`移除本人LocalAppData憑證；刪除正式主機credential-tools目錄前先確認後續封裝未引用。來源交付見[integrations說明](../../integrations/transfiles/production-identity/credential-store/README.md)。
- 原進度表同步為「進行中，待本人DPAPI建檔」，補紫色原生里程碑315；原28圖形保持加1，核心34／完成171／人工11／核銷4不變。更新前SHA256 562F261C7CFDCB7F3898A4C692E45AC12E0BA337280BBF485804E2F76C0C84DF，備份`Voice/備份/進度表_轉檔001DPAPI部署前_20261028-153548.xlsx`；更新後SHA256 169C8DADB929E68D72A2046715DE8BFF416F057013747F2FDABED4D910883A83。OpenXML 0錯誤、Artifact重開／公式錯誤0、LibreOffice重開／13頁列印及變更區目視通過。

## 本人建檔與Portal HTTPS阻擋
- 使用者回覆已建立後，以PMR\ihao_ting的Kerberos工作階段執行正式腳本validate通過：account=ihao_ting、purpose與version正確、可由CurrentUser DPAPI解密；憑證檔ACL只有PMR\ihao_ting與SYSTEM FullControl。只輸出非機敏中繼資料，未輸出密文、密碼、Cookie或token。
- 正式登入前核對發現PmrPortalApi只有`http://172.16.119.130:81`；`https://login.pmr.com.tw`為PmrPortalWeb靜態前端，沒有web application或rewrite `/api`。依契約停止，未把密碼送往HTTP81，未呼叫login／spc-launch／SPC主檔。
- Portal主機8443未占用，已有含私鑰且涵蓋`*.pmr.com.tw`的有效憑證。最小候選：PmrPortalApi新增`172.16.119.130:8443:login.pmr.com.tw` HTTPS SNI，防火牆只允許排程主機`172.16.119.140`；核准後以`https://login.pmr.com.tw:8443/api`完成登入鏈。這是正式Portal IIS／防火牆變更，尚待另行授權。
- 原進度表同步為「進行中，Portal HTTPS阻擋」，補紅色原生阻擋里程碑316；原29圖形保持加1，核心34／完成171／人工11／核銷4不變。更新前SHA256 169C8DADB929E68D72A2046715DE8BFF416F057013747F2FDABED4D910883A83，備份`Voice/備份/進度表_轉檔001DPAPI驗證前_20261028-154606.xlsx`；更新後SHA256 6F761C3BEB89D280DB014149933AD857B0FBFD6AFA61CFC1EFE92CD0BED22F72。OpenXML 0錯誤、Artifact重開／公式錯誤0、LibreOffice重開／13頁列印及變更區目視通過。

## 正式Portal HTTPS發布與SSO上游阻擋
- 使用者明確允許後建立IIS backup `TransFiles001-Portal-20261028-154731`；PmrPortalApi新增`172.16.119.130:8443:login.pmr.com.tw` HTTPS SNI，使用既有wildcard憑證；防火牆只允許排程主機`172.16.119.140`。DNS、TCP與TLS從排程主機通過；未改Portal Web 443。
- DPAPI登入經新HTTPS成功，Cookie為Secure／HttpOnly且Path `/`；本人角色含quality_assurance。但登入回`spcConnected=false`，`spc-launch`回401。未輸出密碼、Cookie值或token，未呼叫SPC主檔、preview／confirm或資料寫入。
- 兩端SSO key皆存在且長度合格；Portal到SPC HTTP8081 TCP可建立，但HTTP版本請求逾時且SPC IIS無該來源紀錄。Portal→SPC HTTPS8443目前TCP=false，因SPC規則只允許工作端172.16.110.27；Portal仍設定HTTP上游。
- 下一個最小候選：SPC API HTTPS8443防火牆另允許Portal主機172.16.119.130，Portal `SPC_API_BASE`改為`https://PMR-SPC-SRV.pmr.com.tw:8443`，備份設定、回收PmrPortalApi後再驗證登入鏈。這涉及SPC防火牆與Portal正式設定，未包含於本次Portal HTTPS授權，尚未執行。
- Portal HTTPS回復：移除`PMR Portal API HTTPS 8443`規則、PmrPortalApi HTTPS binding與SNI SSL binding，或回復IIS backup；執行前確認後續未引用。
- 原進度表同步為「進行中，SSO上游HTTPS阻擋」，補綠色原生里程碑317；原30圖形保持並加1，總表221／核心未完成34／完成171／人工11／核銷4不變。更新前SHA256 `6F761C3BEB89D280DB014149933AD857B0FBFD6AFA61CFC1EFE92CD0BED22F72`，備份`Voice/進度表_轉檔001PortalHTTPS前_20261028-155838.xlsx`；更新後SHA256 `C74AECE238758474842065D0BEB609423DF9B9647B95E77F08F89E75F036880A`。OpenXML結構、Artifact重開、公式錯誤0與變更區目視通過；LibreOffice無回應且未產出PDF，已終止本次程序，因此不宣稱LibreOffice／Microsoft Excel相容性通過。

## SSO上游HTTPS完成驗證
- 使用者明確要求不得影響正式入口網站與SPC。變更前確認SPC API為`production/0.1.90`、Portal首頁200、PmrPortalApi App Pool為Started；先備份再執行，任一步失敗的腳本均會回復設定與防火牆。
- SPC既有`SPC API HTTPS 8443`規則只由`172.16.110.27`增加單一Portal來源`172.16.119.130`，未放寬網段。Portal主機經完整TLS驗證取得`production/0.1.90`後，建立IIS backup `TransFiles001-SsoHttps-20261028-160254`及`appsettings.json.TransFiles001-SsoHttps-20261028-160254.bak`，只把`SPC_API_BASE`改為`https://PMR-SPC-SRV.pmr.com.tw:8443`並回收PmrPortalApi。
- 變更後Portal App Pool為Started、入口首頁200、工作端與Portal端讀SPC version均正常。DPAPI正式登入回`spcConnected=true`，`spc-launch`成功；Token只以受保護WinRM安全字串傳遞，未輸出密碼、Cookie或Token。從Portal主機以Token唯讀GET `/api/processes?configuredOnly=true`回15筆；未呼叫preview／confirm、排程或資料寫入。
- 最終以明確UTF-8讀取並解析正式`appsettings.json`，JSON有效且`SPC_API_BASE`符合核准HTTPS；Portal首頁200、App Pool Started、Portal→SPC為`production/0.1.90`。先前一次Windows PowerShell預設編碼解析警告不代表檔案損壞，安全解析複驗已排除。
- 依使用者確認修正正式入口登入頁：備份 `TransFiles001-PortalWebApiBase-20261028-162739` 及 `C:\inetpub\wwwroot\publish\portal-web\appsettings.json.PortalApiBase-20261028-162739.bak` 後，僅將 Portal Web `PortalApi:BaseUrl` 由舊HTTP81改為`https://login.pmr.com.tw:8443/`；DefaultAppPool Started。登入頁200、API health 200、未登入`/api/auth/me`正確回401，未改SPC／DB／匯入。
- 回復方式：Portal以本次`appsettings.json`備份覆蓋並回收PmrPortalApi；SPC防火牆RemoteAddress回復為`172.16.110.27`。IIS整體設定亦可使用本次backup回復；回復前確認未有後續工作依賴。
- 原進度表將001改為「已完成／否」，沿用同日原生里程碑317且31個原生圖形全部保持；總表221／核心未完成33／完成172／人工11／核銷4。更新前SHA256 `C74AECE238758474842065D0BEB609423DF9B9647B95E77F08F89E75F036880A`，備份`Voice/備份/進度表_轉檔001完成前_20261028-160622.xlsx`；更新後SHA256 `66A378F4245AFA6321234961D227B8F82F80F55BA5C5326543F801AE9FC59E4F`。OpenXML SDK 0錯誤、Artifact原路徑重開／公式錯誤0及變更區目視通過；LibreOffice／Microsoft Excel本次未驗證，不冒稱通過。
