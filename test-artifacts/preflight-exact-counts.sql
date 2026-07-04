SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT 'AlertEvents' AS TableName, COUNT_BIG(*) AS TotalRows, SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) AS SoftDeletedRows FROM dbo.AlertEvents
UNION ALL SELECT 'AttributeMeasurements', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.AttributeMeasurements
UNION ALL SELECT 'Chemicals', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Chemicals
UNION ALL SELECT 'ControlChartCategories', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ControlChartCategories
UNION ALL SELECT 'ControlChartGroups', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ControlChartGroups
UNION ALL SELECT 'ControlChartTypes', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ControlChartTypes
UNION ALL SELECT 'ControlLimitSegments', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ControlLimitSegments
UNION ALL SELECT 'Customers', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Customers
UNION ALL SELECT 'Factories', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Factories
UNION ALL SELECT 'FormulaDefinitions', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.FormulaDefinitions
UNION ALL SELECT 'InspectionItems', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.InspectionItems
UNION ALL SELECT 'LotMasters', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.LotMasters
UNION ALL SELECT 'LotSlotHistories', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.LotSlotHistories
UNION ALL SELECT 'LotSplitHistories', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.LotSplitHistories
UNION ALL SELECT 'Machines', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Machines
UNION ALL SELECT 'MeasurementBatches', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.MeasurementBatches
UNION ALL SELECT 'MeasurementValues', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.MeasurementValues
UNION ALL SELECT 'MesSyncMessages', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.MesSyncMessages
UNION ALL SELECT 'Operators', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Operators
UNION ALL SELECT 'PartProcessCharacteristics', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.PartProcessCharacteristics
UNION ALL SELECT 'Parts', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Parts
UNION ALL SELECT 'Plants', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Plants
UNION ALL SELECT 'Processes', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Processes
UNION ALL SELECT 'ProductionLines', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ProductionLines
UNION ALL SELECT 'Products', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Products
UNION ALL SELECT 'ProductStationItems', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.ProductStationItems
UNION ALL SELECT 'QualityCharacteristics', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.QualityCharacteristics
UNION ALL SELECT 'Shifts', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Shifts
UNION ALL SELECT 'SlotParameters', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.SlotParameters
UNION ALL SELECT 'Slots', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Slots
UNION ALL SELECT 'SpcCalculationResults', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.SpcCalculationResults
UNION ALL SELECT 'SpcRuleGroups', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.SpcRuleGroups
UNION ALL SELECT 'SpcRules', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.SpcRules
UNION ALL SELECT 'StationOperationSessions', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.StationOperationSessions
UNION ALL SELECT 'Stations', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Stations
UNION ALL SELECT 'Suppliers', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Suppliers
UNION ALL SELECT 'Tanks', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Tanks
UNION ALL SELECT 'Units', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.Units
UNION ALL SELECT 'UploadBatches', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.UploadBatches
UNION ALL SELECT 'UploadDetails', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.UploadDetails
UNION ALL SELECT 'UploadErrors', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.UploadErrors
UNION ALL SELECT 'VariableMeasurements', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.VariableMeasurements
UNION ALL SELECT 'WorkOrders', COUNT_BIG(*), SUM(CASE WHEN IsDeleted = 1 THEN CAST(1 AS bigint) ELSE 0 END) FROM dbo.WorkOrders
ORDER BY TableName;
