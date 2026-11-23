function Get-RefreshCatalog {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ConfigurationPath,
        [Parameter(Mandatory)][ValidateSet('PMR_SPC_2026', 'PMR_SPC_TEST')][string]$ExpectedDatabase
    )

    $config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
    $connectionString = $config.ConnectionStrings.SqlServer -replace '(?i)trust\s+server\s+certificate', 'TrustServerCertificate'
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    if ($builder.InitialCatalog -cne $ExpectedDatabase -or $builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$') {
        throw 'Unexpected SQL endpoint or database.'
    }

    $sql = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $sql.Open()
        if ($sql.Database -cne $ExpectedDatabase) { throw 'Live database identity does not match.' }
        $cmd = $sql.CreateCommand()
        $cmd.CommandTimeout = 30
        $cmd.CommandText = @'
SELECT DB_NAME(), CAST(SERVERPROPERTY('ServerName') AS nvarchar(128)),
       CAST(DATABASEPROPERTYEX(DB_NAME(),'Collation') AS nvarchar(128)),
       snapshot_isolation_state_desc, is_read_committed_snapshot_on,
       CONVERT(bit, COALESCE(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION'),0))
FROM sys.databases WHERE database_id = DB_ID();

SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;

SELECT t.name,
       CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(
         (SELECT c.column_id,c.name,c.system_type_id,c.max_length,c.precision,c.scale,c.collation_name,
                 c.is_nullable,c.is_identity,c.is_computed,dc.definition AS default_definition,
                 cc.definition AS computed_definition,
                 CONVERT(nvarchar(128),ic.seed_value) AS identity_seed,
                 CONVERT(nvarchar(128),ic.increment_value) AS identity_increment
          FROM sys.columns c
          LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
          LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
          LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id
          WHERE c.object_id=t.object_id ORDER BY c.column_id FOR JSON PATH, INCLUDE_NULL_VALUES),N'|',
         (SELECT k.name,k.definition,k.is_disabled,k.is_not_trusted
          FROM sys.check_constraints k WHERE k.parent_object_id=t.object_id ORDER BY k.name FOR JSON PATH),N'|',
         (SELECT tr.name,tr.is_disabled,tr.is_instead_of_trigger
          FROM sys.triggers tr WHERE tr.parent_id=t.object_id ORDER BY tr.name FOR JSON PATH),N'|',
         (SELECT i.name,i.type,i.is_unique,i.is_primary_key,i.is_disabled,i.filter_definition,
                 ix.index_column_id,ix.key_ordinal,ix.is_descending_key,ix.is_included_column,c.name AS column_name
          FROM sys.indexes i JOIN sys.index_columns ix ON ix.object_id=i.object_id AND ix.index_id=i.index_id
          JOIN sys.columns c ON c.object_id=ix.object_id AND c.column_id=ix.column_id
          WHERE i.object_id=t.object_id ORDER BY i.name,ix.index_column_id FOR JSON PATH),N'|',
         (SELECT fk.name,fk.is_disabled,fk.is_not_trusted,fk.delete_referential_action,fk.update_referential_action,
                 col.constraint_column_id,p.name AS parent_column,o.name AS target_table,r.name AS target_column
          FROM sys.foreign_keys fk JOIN sys.foreign_key_columns col ON col.constraint_object_id=fk.object_id
          JOIN sys.columns p ON p.object_id=col.parent_object_id AND p.column_id=col.parent_column_id
          JOIN sys.objects o ON o.object_id=col.referenced_object_id
          JOIN sys.columns r ON r.object_id=col.referenced_object_id AND r.column_id=col.referenced_column_id
          WHERE fk.parent_object_id=t.object_id ORDER BY fk.name,col.constraint_column_id FOR JSON PATH))),2),
       COALESCE((SELECT SUM(CONVERT(bigint,p.rows)) FROM sys.partitions p WHERE p.object_id=t.object_id AND p.index_id<2),0)
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE s.name='dbo' AND t.is_ms_shipped=0 ORDER BY t.name;

SELECT fk.name, child.name, parent.name
FROM sys.foreign_keys fk
JOIN sys.tables child ON child.object_id = fk.parent_object_id
JOIN sys.tables parent ON parent.object_id = fk.referenced_object_id
WHERE child.schema_id = SCHEMA_ID('dbo') OR parent.schema_id = SCHEMA_ID('dbo')
ORDER BY fk.name;
'@
        $reader = $cmd.ExecuteReader()
        if (!$reader.Read()) { throw 'Database metadata is missing.' }
        $database = $reader.GetString(0)
        $server = $reader.GetString(1)
        $collation = $reader.GetString(2)
        $snapshot = $reader.GetString(3)
        $rcsi = $reader.GetBoolean(4)
        if (!$reader.GetBoolean(5)) {
            throw [System.UnauthorizedAccessException]::new('Complete catalogue visibility requires database VIEW DEFINITION.')
        }
        [void]$reader.NextResult()
        $migrations = @(while ($reader.Read()) { $reader.GetString(0) })
        [void]$reader.NextResult()
        $tables = @(while ($reader.Read()) {
            [pscustomobject]@{ Name = $reader.GetString(0); Signature = $reader.GetString(1); Rows = $reader.GetInt64(2) }
        })
        if (!$reader.NextResult()) { throw 'Foreign key catalogue is missing.' }
        $foreignKeys = @(while ($reader.Read()) {
            [pscustomobject]@{ Name = $reader.GetString(0); Child = $reader.GetString(1); Parent = $reader.GetString(2) }
        })
        [pscustomobject]@{
            Database = $database; Server = $server; Collation = $collation
            SnapshotIsolation = $snapshot; ReadCommittedSnapshot = $rcsi
            Migrations = $migrations; Tables = $tables
            ForeignKeys = $foreignKeys
        }
    }
    catch [System.UnauthorizedAccessException] { throw "Incomplete catalogue visibility for $ExpectedDatabase. Database VIEW DEFINITION is required." }
    catch { throw "Read-only catalogue query failed for $ExpectedDatabase. Connection details withheld." }
    finally { $sql.Dispose() }
}
