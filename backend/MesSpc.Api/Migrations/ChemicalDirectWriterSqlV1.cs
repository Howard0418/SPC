namespace MesSpc.Api.Migrations;

// Frozen protocol migration SQL; changes require a subsequent migration.
internal static class ChemicalDirectWriterSqlV1
{
 internal static readonly string[] Up = [
"""
CREATE PROCEDURE dbo.CommitChemicalMeasurementWithTrace
 @InitialReceiptId uniqueidentifier,@RecheckReceiptId uniqueidentifier=NULL,@Actor nvarchar(100)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 DECLARE @principal nvarchar(256)=USER_NAME();
 IF CHARINDEX('\',@principal)>0 SET @principal=RIGHT(@principal,CHARINDEX('\',REVERSE(@principal))-1);
 IF CHARINDEX('@',@principal)>0 SET @principal=LEFT(@principal,CHARINDEX('@',@principal)-1);
 IF LOWER(@principal)<>LOWER(@Actor) OR @Actor IS NULL THROW 51220,'Database principal must match receipt actor.',1;

 IF @@TRANCOUNT<>0 THROW 51210,'Standalone chemical import transaction required.',1;
 BEGIN TRY
  BEGIN TRANSACTION;
  DECLARE @ppc int,@source nvarchar(64),@input nvarchar(64),@payload nvarchar(max),@at datetime2,@expiry datetime2,@effect bigint;
  SELECT @ppc=PartProcessCharacteristicId,@source=SourceKeyHash,@input=InputHash,@payload=PayloadJson,@at=MeasuredAtUtc,@expiry=ExpiresAtUtc,@effect=EffectivityId
   FROM ChemicalCalculationReceipts WHERE Id=@InitialReceiptId AND Actor=@Actor;
  IF @ppc IS NULL THROW 51211,'Unknown calculation receipt or actor.',1;
  DECLARE @lock int;
  SELECT @lock=Id FROM PartProcessCharacteristics WITH(UPDLOCK,HOLDLOCK)
   WHERE Id=@ppc AND IsEnabled=1 AND IsDeleted=0 AND UPPER(ControlScope) IN('CHEM','CHEMICAL');
  IF @lock IS NULL THROW 51211,'Chemical mapping unavailable.',1;
  DECLARE @key nvarchar(1000)=JSON_VALUE(@payload,'$.originalInput.SourceKey');
  DECLARE @shift nvarchar(16)=JSON_VALUE(@key,'$.samplingPhase'),@stage nvarchar(16)=JSON_VALUE(@key,'$.samplingStage');
  IF ISJSON(@key)<>1 OR JSON_VALUE(@key,'$.kind')<>'TransFilesChemical-v1'
    OR LEN(ISNULL(JSON_VALUE(@key,'$.fileHash'),''))<>64 OR ISNULL(TRY_CONVERT(int,JSON_VALUE(@key,'$.row')),0)<2
    OR NULLIF(JSON_VALUE(@key,'$.sheet'),'') IS NULL
    OR @shift NOT IN('OPEN','MIDDLE') OR @shift IS NULL OR @stage NOT IN('GENERAL','OPEN','CLOSE') OR @stage IS NULL
   THROW 51212,'Invalid source metadata, shift or stage.',1;
  DECLARE @machine nvarchar(100);
  SELECT @machine=m.MachineCode FROM PartProcessCharacteristics p LEFT JOIN Machines m ON m.Id=p.MachineId WHERE p.Id=@ppc;
  IF (UPPER(ISNULL(@machine,'')) IN('N1','N2') AND @stage NOT IN('OPEN','CLOSE'))
    OR (UPPER(ISNULL(@machine,'')) NOT IN('N1','N2') AND @stage<>'GENERAL')
   THROW 51212,'Invalid chemical sampling stage.',1;
  DECLARE @rePayload nvarchar(max),@reInput nvarchar(64),@reExpiry datetime2,@reEffect bigint;
  IF @RecheckReceiptId IS NOT NULL
  BEGIN
   SELECT @rePayload=PayloadJson,@reInput=InputHash,@reExpiry=ExpiresAtUtc,@reEffect=EffectivityId
    FROM ChemicalCalculationReceipts WHERE Id=@RecheckReceiptId AND PartProcessCharacteristicId=@ppc AND Actor=@Actor AND SourceKeyHash=@source AND MeasuredAtUtc=@at;
   IF @rePayload IS NULL OR @RecheckReceiptId=@InitialReceiptId THROW 51213,'Recheck receipt mismatch.',1;
  END
  DECLARE @existing bigint,@oldReceipt uniqueidentifier,@batch uniqueidentifier;
  SELECT @existing=Id,@oldReceipt=ChemicalReceiptId,@batch=UploadBatchId FROM VariableMeasurements WITH(UPDLOCK,HOLDLOCK)
   WHERE PartProcessCharacteristicId=@ppc AND ChemicalSourceKeyHash=@source;
  IF @existing IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM ChemicalCalculationReceipts WHERE Id=@oldReceipt AND InputHash=@input AND Actor=@Actor)
    OR (@RecheckReceiptId IS NULL AND EXISTS(SELECT 1 FROM ChemicalCalculationTraces WHERE VariableMeasurementId=@existing AND Phase='Recheck'))
    OR (@RecheckReceiptId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM ChemicalCalculationTraces t JOIN ChemicalCalculationReceipts r ON r.Id=t.ReceiptId
       WHERE t.VariableMeasurementId=@existing AND t.Phase='Recheck' AND r.InputHash=@reInput))
    THROW 51214,'Source already imported with different content; explicit correction required.',1;
   COMMIT;
   SELECT @existing measurementId,@batch uploadBatchId,@oldReceipt receiptId,CAST(1 AS bit) alreadyProcessed,
    (SELECT TOP(1) Status FROM ChemicalStatisticsJobs WHERE VariableMeasurementId=@existing ORDER BY CreatedAtUtc DESC) statisticsStatus;
   RETURN;
  END
  IF @expiry<=SYSUTCDATETIME() OR @reExpiry<=SYSUTCDATETIME() THROW 51215,'Calculation receipt expired; preview again.',1;
  IF NOT EXISTS(SELECT 1 FROM ChemicalFormulaPolicies p JOIN ChemicalFormulaEffectivities e ON e.ScheduleRevisionId=p.ActiveScheduleRevisionId
      WHERE p.PartProcessCharacteristicId=@ppc AND e.Id=@effect)
    OR (@RecheckReceiptId IS NOT NULL AND @reEffect<>@effect)
   THROW 51216,'Schedule changed; preview again.',1;
  DECLARE @lsl float,@usl float,@target float,@segment bigint;
  IF (SELECT COUNT(*) FROM ControlLimitSegments WHERE IsDeleted=0 AND PartProcessCharacteristicId=@ppc AND StartDate<=@at AND (EndDate IS NULL OR EndDate>=@at))>1
   THROW 51216,'Specification periods overlap; preview again.',1;
  SELECT @lsl=p.LSL,@usl=p.USL,@target=p.TargetValue FROM PartProcessCharacteristics p WHERE p.Id=@ppc;
  SELECT TOP(1) @segment=Id,@lsl=COALESCE(LSL,@lsl),@usl=COALESCE(USL,@usl),@target=COALESCE(TargetValue,@target)
   FROM ControlLimitSegments WHERE IsDeleted=0 AND PartProcessCharacteristicId=@ppc AND StartDate<=@at AND (EndDate IS NULL OR EndDate>=@at) ORDER BY StartDate DESC,Id DESC;
  IF EXISTS(SELECT @segment,@lsl,@usl,@target EXCEPT SELECT TRY_CONVERT(bigint,JSON_VALUE(@payload,'$.segmentId')),
      TRY_CONVERT(float,JSON_VALUE(@payload,'$.lsl')),TRY_CONVERT(float,JSON_VALUE(@payload,'$.usl')),TRY_CONVERT(float,JSON_VALUE(@payload,'$.target')))
   THROW 51216,'Specification changed; preview again.',1;
  IF EXISTS(SELECT 1 FROM ChemicalCalculationTraces WHERE ReceiptId IN(@InitialReceiptId,@RecheckReceiptId))
   THROW 51217,'Calculation receipt already consumed.',1;
  DECLARE @day date=CONVERT(date,DATEADD(hour,8,@at));
  IF EXISTS(SELECT 1 FROM VariableMeasurements WITH(UPDLOCK,HOLDLOCK) WHERE PartProcessCharacteristicId=@ppc AND PortalDailyDate=@day AND SamplingPhase=@shift AND SamplingStage=@stage AND IsDeleted=0)
   THROW 51218,'Daily measurement exists; explicit correction required.',1;
  DECLARE @value float=TRY_CONVERT(float,JSON_VALUE(@payload,'$.result.Stored')),@raw nvarchar(100)=JSON_VALUE(@payload,'$.result.Raw');
  DECLARE @reValue float=TRY_CONVERT(float,JSON_VALUE(@rePayload,'$.result.Stored'));
  IF @value IS NULL OR @raw IS NULL OR (@RecheckReceiptId IS NOT NULL AND @reValue IS NULL) THROW 51219,'Invalid server calculation result.',1;
  SET @batch=NEWID();
  INSERT UploadBatches(UploadBatchId,UploadType,SourceType,ImportStatus,OriginalFileName,FileHash,TotalRows,ValidRows,ErrorRows,IsExcluded,ConfirmedAt,CreatedAt,CreatedBy,IsDeleted)
   VALUES(@batch,'Variable','TransFilesChemical','StatisticsPending','TransFilesChemical-v1',@input,1,1,0,0,SYSUTCDATETIME(),SYSUTCDATETIME(),@Actor,0);
  -- Existing query pages use string-valued input fields; never store bearer preview tokens.
  DECLARE @detail nvarchar(max)=(SELECT CONVERT(nvarchar(20),@ppc) PpcId,'CHEM' ControlScope,JSON_VALUE(@payload,'$.originalInput.Primary') TitrationValue,
   JSON_VALUE(@payload,'$.originalInput.Secondary') SecondaryTitrationValue,JSON_VALUE(@rePayload,'$.originalInput.Primary') RecheckTitrationValue,
   JSON_VALUE(@rePayload,'$.originalInput.Secondary') RecheckSecondaryTitrationValue,JSON_VALUE(@payload,'$.result.Stored') MeasuredValue,
   JSON_VALUE(@rePayload,'$.result.Stored') RecheckValue,JSON_VALUE(@payload,'$.originalInput.MeasuredAt') MeasuredAt,@shift SamplingPhase,@stage SamplingStage,@Actor Operator,@key SourceMetadata
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
  INSERT UploadDetails(UploadBatchId,RowNo,PayloadJson,IsValid,CreatedAt,CreatedBy,IsDeleted) VALUES(@batch,1,@detail,1,SYSUTCDATETIME(),@Actor,0);
  INSERT VariableMeasurements(UploadBatchId,PartId,ProcessId,MachineId,CharacteristicId,PartProcessCharacteristicId,LineId,TankId,SlotId,SampleNo,
   MeasuredValue,MeasuredAt,PortalDailyDate,SamplingPhase,SamplingStage,Operator,RecheckValue,AdjustAction,AdjustAmount,SourceType,SideCode,
   ChemicalReceiptId,ChemicalSourceKeyHash,SourceReference,CreatedAt,CreatedBy,IsDeleted)
   SELECT @batch,COALESCE(p.PartId,0),p.ProcessId,p.MachineId,p.CharacteristicId,p.Id,t.LineId,p.TankId,p.SlotId,1,
    @value,@at,@day,@shift,@stage,@Actor,@reValue,
    JSON_VALUE(COALESCE(@rePayload,@payload),'$.result.Adjustment'),JSON_VALUE(COALESCE(@rePayload,@payload),'$.result.AdjustmentAmount'),3,0,
    @InitialReceiptId,@source,'TransFilesChemical-v1',SYSUTCDATETIME(),@Actor,0
    FROM PartProcessCharacteristics p LEFT JOIN Tanks t ON t.Id=p.TankId WHERE p.Id=@ppc;
  DECLARE @vm bigint=SCOPE_IDENTITY();
  INSERT ChemicalCalculationTraces(VariableMeasurementId,ReceiptId,Phase,VersionNo,RawResultCanonical,StoredResultCanonical,CreatedAtUtc)
   VALUES(@vm,@InitialReceiptId,'Initial',1,@raw,JSON_VALUE(@payload,'$.result.Stored'),SYSUTCDATETIME());
  IF @RecheckReceiptId IS NOT NULL INSERT ChemicalCalculationTraces(VariableMeasurementId,ReceiptId,Phase,VersionNo,RawResultCanonical,StoredResultCanonical,CreatedAtUtc)
   VALUES(@vm,@RecheckReceiptId,'Recheck',1,JSON_VALUE(@rePayload,'$.result.Raw'),JSON_VALUE(@rePayload,'$.result.Stored'),SYSUTCDATETIME());
  INSERT ChemicalStatisticsJobs(Id,VariableMeasurementId,ReceiptId,Status,Attempts,CreatedAtUtc)
   VALUES(NEWID(),@vm,@InitialReceiptId,'Pending',0,SYSUTCDATETIME());
  COMMIT;
  SELECT @vm measurementId,@batch uploadBatchId,@InitialReceiptId receiptId,CAST(0 AS bit) alreadyProcessed,'Pending' statisticsStatus;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END
""",
"""
CREATE PROCEDURE dbo.GetChemicalDirectImportStatus @ReceiptId uniqueidentifier,@Actor nvarchar(100)
AS BEGIN SET NOCOUNT ON;
 DECLARE @principal nvarchar(256)=USER_NAME();
 IF CHARINDEX('\',@principal)>0 SET @principal=RIGHT(@principal,CHARINDEX('\',REVERSE(@principal))-1);
 IF CHARINDEX('@',@principal)>0 SET @principal=LEFT(@principal,CHARINDEX('@',@principal)-1);
 IF LOWER(@principal)<>LOWER(@Actor) OR @Actor IS NULL THROW 51220,'Database principal must match receipt actor.',1;

 SELECT v.Id measurementId,v.UploadBatchId uploadBatchId,j.Status statisticsStatus,j.Attempts,j.LastError
 FROM ChemicalCalculationReceipts r JOIN VariableMeasurements v ON v.ChemicalReceiptId=r.Id
 JOIN ChemicalStatisticsJobs j ON j.VariableMeasurementId=v.Id AND j.ReceiptId=r.Id WHERE r.Id=@ReceiptId AND r.Actor=@Actor;
END
""",
"GRANT EXECUTE ON dbo.CommitChemicalMeasurementWithTrace TO SpcChemicalDirectWriter; GRANT EXECUTE ON dbo.GetChemicalDirectImportStatus TO SpcChemicalDirectWriter;"
 ];
 internal static readonly string[] Down = ["DROP PROCEDURE IF EXISTS dbo.CommitChemicalMeasurementWithTrace; DROP PROCEDURE IF EXISTS dbo.GetChemicalDirectImportStatus;"];
}
