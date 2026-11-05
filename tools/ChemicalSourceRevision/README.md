# 核准後檔的8筆正式原ID修訂

[規格](../../specs/20261105-chemical-source-precedence/spec.md)。固定8個正式ID及manifest SHA；只處理此次已核准、可唯一對應、沒有Receipt／Trace／SourceKey與啟用公式政策的舊紀錄。新日期公式量測不可使用此工具，不能繞過prepare／commit協定。

`dotnet run --project tools/ChemicalSourceRevision -- CONFIG MANIFEST EVIDENCE_DIR plan|apply|verify`

驗證來源SHA、正式環境／API、業務鍵／日／原時間、前值及rowversion；5個來源式依原F快照用SPC引擎試算。正式COPY_ONLY／CHECKSUM與VERIFYONLY後，Serializable交易保留原ID、時間及總數，UploadBatch／Detail存原值、新值、核准依據、來源式／F快照，不偽造歷史取樣時間或Receipt。替換目標統計／告警並抑制通知，缺圖型明列；提交前非目標、主檔、公式F、排程、Receipt／Trace指紋須保持。交易失敗全部回復。

verify應為pending0、remaining0。0新增、不重匯，不修改來源檔或網站；原始證據只留本機，不提交。回復依稽核before按原ID受控修訂，整庫回復需另核後續寫入。
