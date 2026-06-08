SET NOCOUNT ON;

DECLARE @Prefixes TABLE (Prefix nvarchar(80) NOT NULL);
INSERT INTO @Prefixes (Prefix)
VALUES
  (N'E2E-%'),
  (N'TEST-%'),
  (N'MANUAL-%'),
  (N'LOT-UI-%'),
  (N'FULL-E2E-%');

DECLARE @UploadBatchIds TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @UploadBatchIds (Id)
SELECT DISTINCT ub.UploadBatchId
FROM UploadBatches ub
WHERE EXISTS (
    SELECT 1 FROM @Prefixes p
    WHERE ub.OriginalFileName LIKE p.Prefix
       OR ub.UploadType LIKE p.Prefix
       OR ub.SourceType LIKE p.Prefix
);

INSERT INTO @UploadBatchIds (Id)
SELECT DISTINCT vm.UploadBatchId
FROM VariableMeasurements vm
WHERE vm.UploadBatchId IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM @Prefixes p
      WHERE vm.LotNo LIKE p.Prefix
         OR vm.SerialNo LIKE p.Prefix
         OR vm.Operator LIKE p.Prefix
  )
  AND NOT EXISTS (SELECT 1 FROM @UploadBatchIds x WHERE x.Id = vm.UploadBatchId);

INSERT INTO @UploadBatchIds (Id)
SELECT DISTINCT am.UploadBatchId
FROM AttributeMeasurements am
WHERE am.UploadBatchId IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM @Prefixes p
      WHERE am.LotNo LIKE p.Prefix
         OR am.WorkOrderNo LIKE p.Prefix
         OR am.ParentLotNo LIKE p.Prefix
         OR am.SubLotNo LIKE p.Prefix
         OR am.SourceReference LIKE p.Prefix
         OR am.Operator LIKE p.Prefix
  )
  AND NOT EXISTS (SELECT 1 FROM @UploadBatchIds x WHERE x.Id = am.UploadBatchId);

DECLARE @PartIds TABLE (Id int PRIMARY KEY);
INSERT INTO @PartIds (Id)
SELECT DISTINCT p.Id
FROM Parts p
WHERE EXISTS (SELECT 1 FROM @Prefixes px WHERE p.PartNo LIKE px.Prefix OR p.PartName LIKE px.Prefix);

DECLARE @ProcessIds TABLE (Id int PRIMARY KEY);
INSERT INTO @ProcessIds (Id)
SELECT DISTINCT p.Id
FROM Processes p
WHERE EXISTS (SELECT 1 FROM @Prefixes px WHERE p.ProcessCode LIKE px.Prefix OR p.ProcessName LIKE px.Prefix);

DECLARE @CharacteristicIds TABLE (Id int PRIMARY KEY);
INSERT INTO @CharacteristicIds (Id)
SELECT DISTINCT c.Id
FROM QualityCharacteristics c
WHERE EXISTS (SELECT 1 FROM @Prefixes px WHERE c.CharacteristicCode LIKE px.Prefix OR c.CharacteristicName LIKE px.Prefix);

DECLARE @PpcIds TABLE (Id int PRIMARY KEY);
INSERT INTO @PpcIds (Id)
SELECT DISTINCT ppc.Id
FROM PartProcessCharacteristics ppc
WHERE ppc.PartId IN (SELECT Id FROM @PartIds)
   OR ppc.ProcessId IN (SELECT Id FROM @ProcessIds)
   OR ppc.CharacteristicId IN (SELECT Id FROM @CharacteristicIds);

SELECT 'UploadBatches' AS TableName, COUNT(*) AS RowsToDelete FROM UploadBatches WHERE UploadBatchId IN (SELECT Id FROM @UploadBatchIds)
UNION ALL SELECT 'VariableMeasurements', COUNT(*) FROM VariableMeasurements WHERE UploadBatchId IN (SELECT Id FROM @UploadBatchIds) OR PartProcessCharacteristicId IN (SELECT Id FROM @PpcIds) OR EXISTS (SELECT 1 FROM @Prefixes p WHERE LotNo LIKE p.Prefix OR SerialNo LIKE p.Prefix OR WorkOrderNo LIKE p.Prefix OR ParentLotNo LIKE p.Prefix OR SubLotNo LIKE p.Prefix OR SourceReference LIKE p.Prefix OR Operator LIKE p.Prefix)
UNION ALL SELECT 'AttributeMeasurements', COUNT(*) FROM AttributeMeasurements WHERE UploadBatchId IN (SELECT Id FROM @UploadBatchIds) OR PartProcessCharacteristicId IN (SELECT Id FROM @PpcIds) OR EXISTS (SELECT 1 FROM @Prefixes p WHERE LotNo LIKE p.Prefix OR WorkOrderNo LIKE p.Prefix OR ParentLotNo LIKE p.Prefix OR SubLotNo LIKE p.Prefix OR SourceReference LIKE p.Prefix OR Operator LIKE p.Prefix)
UNION ALL SELECT 'SpcCalculationResults', COUNT(*) FROM SpcCalculationResults WHERE UploadBatchId IN (SELECT Id FROM @UploadBatchIds) OR PartProcessCharacteristicId IN (SELECT Id FROM @PpcIds)
UNION ALL SELECT 'AlertEvents', COUNT(*) FROM AlertEvents WHERE UploadBatchId IN (SELECT Id FROM @UploadBatchIds) OR PartId IN (SELECT Id FROM @PartIds) OR ProcessId IN (SELECT Id FROM @ProcessIds) OR CharacteristicId IN (SELECT Id FROM @CharacteristicIds) OR EXISTS (SELECT 1 FROM @Prefixes p WHERE Message LIKE p.Prefix OR RootCause LIKE p.Prefix OR CorrectiveAction LIKE p.Prefix)
UNION ALL SELECT 'PartProcessCharacteristics', COUNT(*) FROM PartProcessCharacteristics WHERE Id IN (SELECT Id FROM @PpcIds)
UNION ALL SELECT 'Machines', COUNT(*) FROM Machines WHERE ProcessId IN (SELECT Id FROM @ProcessIds) OR EXISTS (SELECT 1 FROM @Prefixes p WHERE MachineCode LIKE p.Prefix OR MachineName LIKE p.Prefix)
UNION ALL SELECT 'QualityCharacteristics', COUNT(*) FROM QualityCharacteristics WHERE Id IN (SELECT Id FROM @CharacteristicIds)
UNION ALL SELECT 'Processes', COUNT(*) FROM Processes WHERE Id IN (SELECT Id FROM @ProcessIds)
UNION ALL SELECT 'Parts', COUNT(*) FROM Parts WHERE Id IN (SELECT Id FROM @PartIds)
ORDER BY TableName;
