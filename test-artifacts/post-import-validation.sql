SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

DECLARE @BatchId uniqueidentifier = '7948ea51-030d-4e5b-8845-360ac1690ddc';

SELECT
    b.UploadBatchId,
    b.OriginalFileName,
    b.ImportStatus,
    b.TotalRows,
    b.ValidRows,
    b.ErrorRows,
    b.ConfirmedAt,
    (SELECT COUNT_BIG(*) FROM dbo.UploadDetails d WHERE d.UploadBatchId = b.UploadBatchId) AS DetailRows,
    (SELECT COUNT_BIG(*) FROM dbo.UploadErrors e WHERE e.UploadBatchId = b.UploadBatchId) AS UploadErrorRows,
    (SELECT COUNT_BIG(*) FROM dbo.VariableMeasurements v WHERE v.UploadBatchId = b.UploadBatchId) AS MeasurementRows,
    (SELECT COUNT_BIG(*) FROM dbo.SpcCalculationResults s WHERE s.UploadBatchId = b.UploadBatchId) AS SpcRows
FROM dbo.UploadBatches b
WHERE b.UploadBatchId = @BatchId;

SELECT
    (SELECT COUNT_BIG(*) FROM dbo.Plants) AS Plants,
    (SELECT COUNT_BIG(*) FROM dbo.Factories) AS Factories,
    (SELECT COUNT_BIG(*) FROM dbo.Processes) AS Processes,
    (SELECT COUNT_BIG(*) FROM dbo.Machines) AS Machines,
    (SELECT COUNT_BIG(*) FROM dbo.ProductionLines) AS ProductionLines,
    (SELECT COUNT_BIG(*) FROM dbo.Tanks) AS Tanks,
    (SELECT COUNT_BIG(*) FROM dbo.QualityCharacteristics) AS QualityCharacteristics,
    (SELECT COUNT_BIG(*) FROM dbo.PartProcessCharacteristics WHERE ControlScope = 'CHEMICAL') AS ChemicalMappings,
    (SELECT COUNT_BIG(*) FROM dbo.Operators) AS Operators;

SELECT
    SUM(CASE WHEN p.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingProcess,
    SUM(CASE WHEN m.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingMachine,
    SUM(CASE WHEN l.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingLine,
    SUM(CASE WHEN t.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingTank,
    SUM(CASE WHEN q.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingCharacteristic,
    SUM(CASE WHEN ppc.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingMapping,
    SUM(CASE WHEN s.Id IS NULL THEN CAST(1 AS bigint) ELSE 0 END) AS MissingSpcResult
FROM dbo.VariableMeasurements v
LEFT JOIN dbo.Processes p ON p.Id = v.ProcessId
LEFT JOIN dbo.Machines m ON m.Id = v.MachineId
LEFT JOIN dbo.ProductionLines l ON l.Id = v.LineId
LEFT JOIN dbo.Tanks t ON t.Id = v.TankId
LEFT JOIN dbo.QualityCharacteristics q ON q.Id = v.CharacteristicId
LEFT JOIN dbo.PartProcessCharacteristics ppc ON ppc.Id = v.PartProcessCharacteristicId
LEFT JOIN dbo.SpcCalculationResults s ON s.VariableMeasurementId = v.Id
WHERE v.UploadBatchId = @BatchId;

SELECT
    (SELECT COUNT_BIG(*) FROM dbo.ControlChartGroups) AS ChartGroups,
    (SELECT COUNT_BIG(*) FROM dbo.ControlChartCategories) AS ChartCategories,
    (SELECT COUNT_BIG(*) FROM dbo.ControlChartTypes) AS ChartTypes,
    (SELECT COUNT_BIG(*) FROM dbo.SpcRuleGroups) AS RuleGroups,
    (SELECT COUNT_BIG(*) FROM dbo.SpcRules) AS Rules,
    (SELECT COUNT_BIG(*) FROM dbo.__EFMigrationsHistory) AS AppliedMigrations;
