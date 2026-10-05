SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @actor nvarchar(max) = N'SDD-20260929-chemical-dust-monitoring';
DECLARE @dustGroupId int;

MERGE dbo.ControlChartGroups WITH (HOLDLOCK) AS target
USING (
    SELECT
        N'DUST' AS GroupCode,
        N'落塵監控' AS GroupName,
        N'CONTROL_CHART' AS GroupType,
        N'DUST' AS BusinessScopeCode,
        CAST(0 AS bit) AS RequiresPart,
        CAST(0 AS bit) AS RequiresMachine,
        CAST(0 AS bit) AS RequiresTank,
        N'落塵監控專用群組，供 0.5um/1um/5um/10um C-chart 與 U-chart 使用。' AS Description,
        CAST(1 AS bit) AS IsEnabled
) AS source
ON target.GroupCode = source.GroupCode
WHEN MATCHED THEN
    UPDATE SET
        GroupName = source.GroupName,
        GroupType = source.GroupType,
        BusinessScopeCode = source.BusinessScopeCode,
        RequiresPart = source.RequiresPart,
        RequiresMachine = source.RequiresMachine,
        RequiresTank = source.RequiresTank,
        Description = source.Description,
        IsEnabled = source.IsEnabled,
        IsDeleted = 0,
        UpdatedAt = @now,
        UpdatedBy = @actor
WHEN NOT MATCHED THEN
    INSERT (
        GroupCode,
        GroupName,
        GroupType,
        BusinessScopeCode,
        RequiresPart,
        RequiresMachine,
        RequiresTank,
        Description,
        IsEnabled,
        CreatedAt,
        CreatedBy,
        IsDeleted
    )
    VALUES (
        source.GroupCode,
        source.GroupName,
        source.GroupType,
        source.BusinessScopeCode,
        source.RequiresPart,
        source.RequiresMachine,
        source.RequiresTank,
        source.Description,
        source.IsEnabled,
        @now,
        @actor,
        0
    );

SELECT @dustGroupId = Id
FROM dbo.ControlChartGroups
WHERE GroupCode = N'DUST';

MERGE dbo.ControlChartTypes WITH (HOLDLOCK) AS target
USING (
    SELECT
        @dustGroupId AS ChartGroupId,
        N'DUST_C' AS ChartTypeCode,
        N'落塵缺點數圖' AS ChartTypeName,
        N'Attribute' AS DataCategory,
        CAST(1 AS int) AS RequiredSampleSize,
        CAST(NULL AS int) AS RuleGroupId,
        N'落塵監控 C-chart，固定用於 0.5um/1um/5um/10um 缺點數資料。' AS Description,
        CAST(NULL AS nvarchar(max)) AS FormulaConfigJson,
        CAST(1 AS bit) AS IsEnabled
    UNION ALL
    SELECT
        @dustGroupId,
        N'DUST_U',
        N'落塵單位缺點數圖',
        N'Attribute',
        CAST(1 AS int),
        CAST(NULL AS int),
        N'落塵監控 U-chart，固定用於 0.5um/1um/5um/10um 單位缺點數資料。',
        CAST(NULL AS nvarchar(max)),
        CAST(1 AS bit)
) AS source
ON target.ChartTypeCode = source.ChartTypeCode
WHEN MATCHED THEN
    UPDATE SET
        ChartGroupId = source.ChartGroupId,
        ChartTypeName = source.ChartTypeName,
        DataCategory = source.DataCategory,
        RequiredSampleSize = source.RequiredSampleSize,
        RuleGroupId = source.RuleGroupId,
        Description = source.Description,
        FormulaConfigJson = source.FormulaConfigJson,
        IsEnabled = source.IsEnabled,
        IsDeleted = 0,
        UpdatedAt = @now,
        UpdatedBy = @actor
WHEN NOT MATCHED THEN
    INSERT (
        ChartGroupId,
        ChartTypeCode,
        ChartTypeName,
        DataCategory,
        RequiredSampleSize,
        RuleGroupId,
        Description,
        FormulaConfigJson,
        IsEnabled,
        CreatedAt,
        CreatedBy,
        IsDeleted
    )
    VALUES (
        source.ChartGroupId,
        source.ChartTypeCode,
        source.ChartTypeName,
        source.DataCategory,
        source.RequiredSampleSize,
        source.RuleGroupId,
        source.Description,
        source.FormulaConfigJson,
        source.IsEnabled,
        @now,
        @actor,
        0
    );

COMMIT TRANSACTION;

SELECT
    g.Id AS GroupId,
    g.GroupCode,
    g.GroupName,
    g.BusinessScopeCode,
    g.RequiresMachine,
    t.Id AS ChartTypeId,
    t.ChartTypeCode,
    t.ChartTypeName,
    t.DataCategory,
    t.RequiredSampleSize,
    t.IsEnabled
FROM dbo.ControlChartGroups AS g
LEFT JOIN dbo.ControlChartTypes AS t
    ON t.ChartGroupId = g.Id
WHERE g.GroupCode = N'DUST'
ORDER BY t.ChartTypeCode;
