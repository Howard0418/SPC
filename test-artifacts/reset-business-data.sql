SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'PMR_SPC_2026'
    THROW 51000, 'Safety check failed: unexpected database.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM msdb.dbo.backupset bs
    JOIN msdb.dbo.backupmediafamily bmf ON bmf.media_set_id = bs.media_set_id
    WHERE bs.database_name = N'PMR_SPC_2026'
      AND bs.type = 'D'
      AND bs.has_backup_checksums = 1
      AND bmf.physical_device_name =
          N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_2026_before_full_reset_20260704.bak'
)
    THROW 51001, 'Safety check failed: verified pre-reset backup not found.', 1;

DECLARE @ChartGroups bigint = (SELECT COUNT_BIG(*) FROM dbo.ControlChartGroups);
DECLARE @ChartCategories bigint = (SELECT COUNT_BIG(*) FROM dbo.ControlChartCategories);
DECLARE @ChartTypes bigint = (SELECT COUNT_BIG(*) FROM dbo.ControlChartTypes);
DECLARE @RuleGroups bigint = (SELECT COUNT_BIG(*) FROM dbo.SpcRuleGroups);
DECLARE @Rules bigint = (SELECT COUNT_BIG(*) FROM dbo.SpcRules);
DECLARE @Formulas bigint = (SELECT COUNT_BIG(*) FROM dbo.FormulaDefinitions);

IF @ChartGroups = 0 OR @ChartCategories = 0 OR @ChartTypes = 0 OR @RuleGroups = 0 OR @Rules = 0
    THROW 51002, 'Safety check failed: required SPC baseline is missing.', 1;

DECLARE @LockResult int;
EXEC @LockResult = sys.sp_getapplock
    @Resource = N'PMR_SPC_2026_FULL_RESET',
    @LockMode = N'Exclusive',
    @LockOwner = N'Session',
    @LockTimeout = 10000;

IF @LockResult < 0
    THROW 51003, 'Could not acquire reset lock.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM dbo.ControlLimitSegments;
    DELETE FROM dbo.AlertEvents;
    DELETE FROM dbo.SpcCalculationResults;
    DELETE FROM dbo.VariableMeasurements;
    DELETE FROM dbo.AttributeMeasurements;

    DELETE FROM dbo.UploadErrors;
    DELETE FROM dbo.UploadDetails;
    DELETE FROM dbo.UploadBatches;

    DELETE FROM dbo.MeasurementValues;
    DELETE FROM dbo.MeasurementBatches;
    DELETE FROM dbo.StationOperationSessions;
    DELETE FROM dbo.WorkOrders;

    DELETE FROM dbo.LotSlotHistories;
    DELETE FROM dbo.SlotParameters;
    DELETE FROM dbo.LotSplitHistories;
    UPDATE dbo.LotMasters SET ParentLotId = NULL WHERE ParentLotId IS NOT NULL;
    DELETE FROM dbo.LotMasters;
    DELETE FROM dbo.MesSyncMessages;

    DELETE FROM dbo.PartProcessCharacteristics;
    DELETE FROM dbo.ProductStationItems;
    DELETE FROM dbo.Slots;
    DELETE FROM dbo.Tanks;
    DELETE FROM dbo.Machines;
    DELETE FROM dbo.QualityCharacteristics;
    DELETE FROM dbo.ProductionLines;
    DELETE FROM dbo.Factories;
    DELETE FROM dbo.Plants;
    DELETE FROM dbo.Parts;
    DELETE FROM dbo.Processes;

    DELETE FROM dbo.Products;
    DELETE FROM dbo.Stations;
    DELETE FROM dbo.InspectionItems;
    DELETE FROM dbo.Units;
    DELETE FROM dbo.Shifts;
    DELETE FROM dbo.Operators;
    DELETE FROM dbo.Customers;
    DELETE FROM dbo.Suppliers;
    DELETE FROM dbo.Chemicals;

    IF @ChartGroups <> (SELECT COUNT_BIG(*) FROM dbo.ControlChartGroups)
       OR @ChartCategories <> (SELECT COUNT_BIG(*) FROM dbo.ControlChartCategories)
       OR @ChartTypes <> (SELECT COUNT_BIG(*) FROM dbo.ControlChartTypes)
       OR @RuleGroups <> (SELECT COUNT_BIG(*) FROM dbo.SpcRuleGroups)
       OR @Rules <> (SELECT COUNT_BIG(*) FROM dbo.SpcRules)
       OR @Formulas <> (SELECT COUNT_BIG(*) FROM dbo.FormulaDefinitions)
        THROW 51004, 'SPC baseline changed during reset; rolling back.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    EXEC sys.sp_releaseapplock
        @Resource = N'PMR_SPC_2026_FULL_RESET',
        @LockOwner = N'Session';
    THROW;
END CATCH;

EXEC sys.sp_releaseapplock
    @Resource = N'PMR_SPC_2026_FULL_RESET',
    @LockOwner = N'Session';

SELECT 'ControlChartGroups' AS TableName, COUNT_BIG(*) AS RemainingRows FROM dbo.ControlChartGroups
UNION ALL SELECT 'ControlChartCategories', COUNT_BIG(*) FROM dbo.ControlChartCategories
UNION ALL SELECT 'ControlChartTypes', COUNT_BIG(*) FROM dbo.ControlChartTypes
UNION ALL SELECT 'SpcRuleGroups', COUNT_BIG(*) FROM dbo.SpcRuleGroups
UNION ALL SELECT 'SpcRules', COUNT_BIG(*) FROM dbo.SpcRules
UNION ALL SELECT 'FormulaDefinitions', COUNT_BIG(*) FROM dbo.FormulaDefinitions
UNION ALL SELECT 'VariableMeasurements', COUNT_BIG(*) FROM dbo.VariableMeasurements
UNION ALL SELECT 'AttributeMeasurements', COUNT_BIG(*) FROM dbo.AttributeMeasurements
UNION ALL SELECT 'SpcCalculationResults', COUNT_BIG(*) FROM dbo.SpcCalculationResults
UNION ALL SELECT 'UploadBatches', COUNT_BIG(*) FROM dbo.UploadBatches
UNION ALL SELECT 'Parts', COUNT_BIG(*) FROM dbo.Parts
UNION ALL SELECT 'Processes', COUNT_BIG(*) FROM dbo.Processes
UNION ALL SELECT 'Machines', COUNT_BIG(*) FROM dbo.Machines
UNION ALL SELECT 'QualityCharacteristics', COUNT_BIG(*) FROM dbo.QualityCharacteristics
UNION ALL SELECT 'PartProcessCharacteristics', COUNT_BIG(*) FROM dbo.PartProcessCharacteristics
UNION ALL SELECT 'Operators', COUNT_BIG(*) FROM dbo.Operators
ORDER BY TableName;
