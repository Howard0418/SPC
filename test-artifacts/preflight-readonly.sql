SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

SELECT
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    SYSDATETIMEOFFSET() AS CheckedAt,
    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Updateability') AS nvarchar(60)) AS Updateability;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    COALESCE(r.ApproximateRows, 0) AS ApproximateRows,
    COALESCE(sz.AllocatedMB, 0) AS AllocatedMB
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
OUTER APPLY
(
    SELECT SUM(p.rows) AS ApproximateRows
    FROM sys.partitions p
    WHERE p.object_id = t.object_id AND p.index_id IN (0, 1)
) r
OUTER APPLY
(
    SELECT CAST(SUM(a.total_pages) * 8.0 / 1024 AS decimal(18, 2)) AS AllocatedMB
    FROM sys.partitions p
    JOIN sys.allocation_units a ON a.container_id = p.partition_id
    WHERE p.object_id = t.object_id
) sz
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;

SELECT
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
ORDER BY ParentTable, ChildTable, ForeignKeyName;

SELECT
    OBJECT_SCHEMA_NAME(ic.object_id) AS SchemaName,
    OBJECT_NAME(ic.object_id) AS TableName,
    c.name AS IdentityColumn,
    ic.seed_value AS SeedValue,
    ic.increment_value AS IncrementValue,
    ic.last_value AS LastValue
FROM sys.identity_columns ic
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
ORDER BY SchemaName, TableName;
