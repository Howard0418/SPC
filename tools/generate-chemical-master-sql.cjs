const fs = require("fs");

const plan = JSON.parse(fs.readFileSync("sample-data/批次轉換結果_藥液_master_plan.json", "utf8"));

function sqlString(value) {
  if (value === null || value === undefined || value === "") return "NULL";
  return `N'${String(value).replace(/'/g, "''")}'`;
}

function values(rows, columns) {
  return rows
    .map((row) => `(${columns.map((col) => sqlString(row[col])).join(", ")})`)
    .join(",\n");
}

const processRows = plan.processes.map((process) => ({ process }));

const machineRows = plan.machines.map((row) => ({
  process: row.process,
  machine: row.machine,
}));

const tankRows = plan.machines.flatMap((row) =>
  row.tanks.map((tank) => ({
    process: row.process,
    machine: row.machine,
    tank,
    tankCode: `${row.machine}-${tank}`,
  })),
);

const characteristicRows = plan.characteristics.map((row) => ({
  code: row.code,
  name: row.name,
}));

const comboRows = plan.combos.map((row) => ({
  process: row.process,
  machine: row.machine,
  tank: row.tank,
  tankCode: `${row.machine}-${row.tank}`,
  characteristic: row.characteristic,
}));

const sql = `SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @plantId int;
DECLARE @factoryId int;
DECLARE @chartTypeId int;

SELECT @chartTypeId = Id
FROM ControlChartTypes
WHERE ChartTypeCode = 'I_MR' AND DataCategory = 'Variable' AND IsEnabled = 1;

IF @chartTypeId IS NULL
BEGIN
    THROW 51000, 'I_MR variable chart type not found.', 1;
END;

SELECT @plantId = TOP_ONE.Id
FROM (SELECT TOP 1 Id FROM Plants ORDER BY Id) TOP_ONE;

IF @plantId IS NULL
BEGIN
    INSERT INTO Plants (PlantCode, PlantName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (N'PLT-01', N'Main Plant', NULL, 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);
    SET @plantId = SCOPE_IDENTITY();
END;

SELECT @factoryId = TOP_ONE.Id
FROM (SELECT TOP 1 Id FROM Factories ORDER BY Id) TOP_ONE;

IF @factoryId IS NULL
BEGIN
    INSERT INTO Factories (PlantId, FactoryCode, FactoryName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (@plantId, N'FAC-01', N'Main Factory', NULL, 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);
    SET @factoryId = SCOPE_IDENTITY();
END;

DECLARE @processes table (ProcessCode nvarchar(100) NOT NULL PRIMARY KEY);
INSERT INTO @processes (ProcessCode)
VALUES
${values(processRows, ["process"])};

MERGE Processes AS target
USING @processes AS source
ON target.ProcessCode = source.ProcessCode
WHEN MATCHED THEN
    UPDATE SET ProcessName = source.ProcessCode + N' 藥液管制',
               Description = N'由批次轉換結果_藥液_計量型匯入.xlsx 重新建立',
               IsEnabled = 1,
               UpdatedAt = SYSUTCDATETIME(),
               UpdatedBy = N'System'
WHEN NOT MATCHED THEN
    INSERT (ProcessCode, ProcessName, Description, IsEnabled, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (source.ProcessCode, source.ProcessCode + N' 藥液管制', N'由批次轉換結果_藥液_計量型匯入.xlsx 重新建立', 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);

DECLARE @machines table (ProcessCode nvarchar(100) NOT NULL, MachineCode nvarchar(100) NOT NULL, PRIMARY KEY (ProcessCode, MachineCode));
INSERT INTO @machines (ProcessCode, MachineCode)
VALUES
${values(machineRows, ["process", "machine"])};

MERGE Machines AS target
USING (
    SELECT p.Id AS ProcessId, m.MachineCode
    FROM @machines m
    JOIN Processes p ON p.ProcessCode = m.ProcessCode
) AS source
ON target.ProcessId = source.ProcessId AND target.MachineCode = source.MachineCode
WHEN MATCHED THEN
    UPDATE SET MachineName = source.MachineCode,
               Status = N'Active',
               IsEnabled = 1,
               UpdatedAt = SYSUTCDATETIME(),
               UpdatedBy = N'System'
WHEN NOT MATCHED THEN
    INSERT (MachineCode, MachineName, ProcessId, Location, Status, IsEnabled, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (source.MachineCode, source.MachineCode, source.ProcessId, NULL, N'Active', 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);

MERGE ProductionLines AS target
USING (SELECT DISTINCT MachineCode FROM @machines) AS source
ON target.LineCode = source.MachineCode
WHEN MATCHED THEN
    UPDATE SET LineName = source.MachineCode,
               Description = N'藥液線別/機台',
               IsActive = 1,
               UpdatedAt = SYSUTCDATETIME(),
               UpdatedBy = N'System'
WHEN NOT MATCHED THEN
    INSERT (FactoryId, LineCode, LineName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (@factoryId, source.MachineCode, source.MachineCode, N'藥液線別/機台', 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);

DECLARE @tanks table (ProcessCode nvarchar(100) NOT NULL, MachineCode nvarchar(100) NOT NULL, TankName nvarchar(200) NOT NULL, TankCode nvarchar(300) NOT NULL);
INSERT INTO @tanks (ProcessCode, MachineCode, TankName, TankCode)
VALUES
${values(tankRows, ["process", "machine", "tank", "tankCode"])};

MERGE Tanks AS target
USING (
    SELECT l.Id AS LineId, t.TankCode, t.TankName
    FROM @tanks t
    JOIN ProductionLines l ON l.LineCode = t.MachineCode
) AS source
ON target.LineId = source.LineId AND target.TankCode = source.TankCode
WHEN MATCHED THEN
    UPDATE SET TankName = source.TankName,
               Description = N'由藥液匯入檔建立',
               IsActive = 1,
               UpdatedAt = SYSUTCDATETIME(),
               UpdatedBy = N'System'
WHEN NOT MATCHED THEN
    INSERT (LineId, TankCode, TankName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (source.LineId, source.TankCode, source.TankName, N'由藥液匯入檔建立', 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);

DECLARE @characteristics table (CharacteristicCode nvarchar(100) NOT NULL PRIMARY KEY, CharacteristicName nvarchar(200) NOT NULL);
INSERT INTO @characteristics (CharacteristicCode, CharacteristicName)
VALUES
${values(characteristicRows, ["code", "name"])};

MERGE QualityCharacteristics AS target
USING @characteristics AS source
ON target.CharacteristicCode = source.CharacteristicCode
WHEN MATCHED THEN
    UPDATE SET CharacteristicName = source.CharacteristicName,
               DataCategory = 'Variable',
               DefaultChartTypeId = @chartTypeId,
               IsSpcEnabled = 1,
               IsEnabled = 1,
               UpdatedAt = SYSUTCDATETIME(),
               UpdatedBy = N'System'
WHEN NOT MATCHED THEN
    INSERT (CharacteristicCode, CharacteristicName, DataCategory, Unit, DefaultChartTypeId, IsSpcEnabled, IsEnabled, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES (source.CharacteristicCode, source.CharacteristicName, 'Variable', NULL, @chartTypeId, 1, 1, SYSUTCDATETIME(), N'System', SYSUTCDATETIME(), N'System', 0);

DECLARE @combos table (ProcessCode nvarchar(100) NOT NULL, MachineCode nvarchar(100) NOT NULL, TankName nvarchar(200) NOT NULL, TankCode nvarchar(300) NOT NULL, CharacteristicCode nvarchar(100) NOT NULL);
INSERT INTO @combos (ProcessCode, MachineCode, TankName, TankCode, CharacteristicCode)
VALUES
${values(comboRows, ["process", "machine", "tank", "tankCode", "characteristic"])};

INSERT INTO PartProcessCharacteristics
(
    ControlScope,
    PartId,
    ProcessId,
    MachineId,
    TankId,
    CharacteristicId,
    USL,
    LSL,
    UCL,
    CL,
    LCL,
    TargetValue,
    SampleSize,
    ChartTypeId,
    RuleGroupId,
    IsRequired,
    IsEnabled,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy,
    IsDeleted
)
SELECT
    'CHEMICAL',
    NULL,
    p.Id,
    m.Id,
    tk.Id,
    qc.Id,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1,
    @chartTypeId,
    NULL,
    1,
    1,
    SYSUTCDATETIME(),
    N'System',
    SYSUTCDATETIME(),
    N'System',
    0
FROM @combos c
JOIN Processes p ON p.ProcessCode = c.ProcessCode
JOIN Machines m ON m.ProcessId = p.Id AND m.MachineCode = c.MachineCode
JOIN ProductionLines l ON l.LineCode = c.MachineCode
JOIN Tanks tk ON tk.LineId = l.Id AND tk.TankCode = c.TankCode
JOIN QualityCharacteristics qc ON qc.CharacteristicCode = c.CharacteristicCode
WHERE NOT EXISTS (
    SELECT 1
    FROM PartProcessCharacteristics existing
    WHERE existing.ControlScope = 'CHEMICAL'
      AND existing.ProcessId = p.Id
      AND existing.MachineId = m.Id
      AND existing.TankId = tk.Id
      AND existing.CharacteristicId = qc.Id
);

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM Processes WHERE ProcessCode IN (SELECT ProcessCode FROM @processes)) AS Processes,
    (SELECT COUNT(*) FROM Machines WHERE ProcessId IN (SELECT Id FROM Processes WHERE ProcessCode IN (SELECT ProcessCode FROM @processes))) AS Machines,
    (SELECT COUNT(*) FROM ProductionLines WHERE LineCode IN (SELECT MachineCode FROM @machines)) AS ProductionLines,
    (SELECT COUNT(*) FROM Tanks WHERE LineId IN (SELECT Id FROM ProductionLines WHERE LineCode IN (SELECT MachineCode FROM @machines))) AS Tanks,
    (SELECT COUNT(*) FROM PartProcessCharacteristics WHERE ProcessId IN (SELECT Id FROM Processes WHERE ProcessCode IN (SELECT ProcessCode FROM @processes)) AND ControlScope = 'CHEMICAL') AS ChemicalPpcs;
`;

fs.writeFileSync("test-artifacts/recreate-msap-chemical-master.sql", sql, "utf8");
console.log(JSON.stringify({
  machines: machineRows.length,
  tanks: tankRows.length,
  characteristics: characteristicRows.length,
  combos: comboRows.length,
}, null, 2));
