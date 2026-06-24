SET XACT_ABORT ON;
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
(N'C1'),
(N'C3'),
(N'C5'),
(N'CN'),
(N'DP'),
(N'DV'),
(N'DV2'),
(N'N1'),
(N'N2'),
(N'PT'),
(N'PT2'),
(N'QE'),
(N'QE2'),
(N'ST'),
(N'ST2');

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
(N'C1', N'C1'),
(N'C3', N'C3'),
(N'C5', N'C5'),
(N'CN', N'CN'),
(N'DP', N'DP'),
(N'DV', N'DV'),
(N'DV2', N'DV2'),
(N'N1', N'N1'),
(N'N2', N'N2'),
(N'PT', N'PT'),
(N'PT2', N'PT2'),
(N'QE', N'QE'),
(N'QE2', N'QE2'),
(N'ST', N'ST'),
(N'ST2', N'ST2');

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
(N'C1', N'C1', N'銅1', N'C1-銅1'),
(N'C1', N'C1', N'銅2', N'C1-銅2'),
(N'C3', N'C3', N'剝掛槽', N'C3-剝掛槽'),
(N'C3', N'C3', N'清潔槽', N'C3-清潔槽'),
(N'C3', N'C3', N'酸浸槽', N'C3-酸浸槽'),
(N'C3', N'C3', N'鍍銅槽', N'C3-鍍銅槽'),
(N'C5', N'C5', N'剝掛槽', N'C5-剝掛槽'),
(N'C5', N'C5', N'清潔槽', N'C5-清潔槽'),
(N'C5', N'C5', N'酸浸槽', N'C5-酸浸槽'),
(N'CN', N'CN', N'鹼洗1', N'CN-鹼洗1'),
(N'CN', N'CN', N'鹼洗2', N'CN-鹼洗2'),
(N'DP', N'DP', N'抗氧化', N'DP-抗氧化'),
(N'DP', N'DP', N'清潔槽', N'DP-清潔槽'),
(N'DP', N'DP', N'酸洗槽', N'DP-酸洗槽'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽'),
(N'DV', N'DV', N'新液洗 New liquid washing', N'DV-新液洗 New liquid washing'),
(N'DV', N'DV', N'液洗 Liquid washing', N'DV-液洗 Liquid washing'),
(N'DV', N'DV', N'添加桶 Add Tank', N'DV-添加桶 Add Tank'),
(N'DV', N'DV', N'顯影槽Developer Tank', N'DV-顯影槽Developer Tank'),
(N'DV2', N'DV2', N'新液洗 New liquid washing', N'DV2-新液洗 New liquid washing'),
(N'DV2', N'DV2', N'液洗 Liquid washing', N'DV2-液洗 Liquid washing'),
(N'DV2', N'DV2', N'添加桶 Add Tank', N'DV2-添加桶 Add Tank'),
(N'DV2', N'DV2', N'顯影槽Developer Tank', N'DV2-顯影槽Developer Tank'),
(N'N1', N'N1', N'促進', N'N1-促進'),
(N'N1', N'N1', N'催化', N'N1-催化'),
(N'N1', N'N1', N'表面處理', N'N1-表面處理'),
(N'N1', N'N1', N'調節', N'N1-調節'),
(N'N1', N'N1', N'預浸', N'N1-預浸'),
(N'N2', N'N2', N'促進', N'N2-促進'),
(N'N2', N'N2', N'催化', N'N2-催化'),
(N'N2', N'N2', N'化鎳', N'N2-化鎳'),
(N'N2', N'N2', N'表面處理', N'N2-表面處理'),
(N'N2', N'N2', N'調節', N'N2-調節'),
(N'N2', N'N2', N'預浸', N'N2-預浸'),
(N'PT', N'PT', N'抗氧化 Anti-oxidation', N'PT-抗氧化 Anti-oxidation'),
(N'PT', N'PT', N'脫脂槽 Degreasing', N'PT-脫脂槽 Degreasing'),
(N'PT', N'PT', N'銅 Copper', N'PT-銅 Copper'),
(N'PT', N'PT', N'鹽酸洗 HCL', N'PT-鹽酸洗 HCL'),
(N'PT2', N'PT2', N'抗氧化 Anti-oxidation', N'PT2-抗氧化 Anti-oxidation'),
(N'PT2', N'PT2', N'脫脂槽 Degreasing', N'PT2-脫脂槽 Degreasing'),
(N'PT2', N'PT2', N'銅 Copper', N'PT2-銅 Copper'),
(N'PT2', N'PT2', N'鹽酸洗 HCL', N'PT2-鹽酸洗 HCL'),
(N'QE', N'QE', N'NS-357', N'QE-NS-357'),
(N'QE', N'QE', N'抗氧化 Anti-oxidation', N'QE-抗氧化 Anti-oxidation'),
(N'QE', N'QE', N'硫酸洗 H2SO4', N'QE-硫酸洗 H2SO4'),
(N'QE', N'QE', N'線外配槽', N'QE-線外配槽'),
(N'QE', N'QE', N'蝕刻主槽 Main Etching Tank', N'QE-蝕刻主槽 Main Etching Tank'),
(N'QE', N'QE', N'銅 copper', N'QE-銅 copper'),
(N'QE2', N'QE2', N'硫酸洗 H2SO4', N'QE2-硫酸洗 H2SO4'),
(N'QE2', N'QE2', N'蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'QE2-蝕刻(3)槽(鎳) Etching Tank (3) – Nickel'),
(N'QE2', N'QE2', N'蝕刻主槽(銅) Main Etching Tank', N'QE2-蝕刻主槽(銅) Main Etching Tank'),
(N'ST', N'ST', N'NS-357', N'ST-NS-357'),
(N'ST', N'ST', N'ST小槽 New liquid washing', N'ST-ST小槽 New liquid washing'),
(N'ST', N'ST', N'剝膜 (1) 槽 Stripping Tank (1)', N'ST-剝膜 (1) 槽 Stripping Tank (1)'),
(N'ST', N'ST', N'剝膜 (2) 槽Stripping Tank (2)', N'ST-剝膜 (2) 槽Stripping Tank (2)'),
(N'ST', N'ST', N'剝膜 小槽Resist Stripping Small Tank', N'ST-剝膜 小槽Resist Stripping Small Tank'),
(N'ST', N'ST', N'剝膜 添加桶 Resist Stripping Additive Tank', N'ST-剝膜 添加桶 Resist Stripping Additive Tank'),
(N'ST', N'ST', N'新液洗 New liquid washing', N'ST-新液洗 New liquid washing'),
(N'ST', N'ST', N'液洗 Liquid washing', N'ST-液洗 Liquid washing'),
(N'ST', N'ST', N'液洗(2) Liquid washing', N'ST-液洗(2) Liquid washing'),
(N'ST', N'ST', N'液洗(3) New liquid washing', N'ST-液洗(3) New liquid washing'),
(N'ST', N'ST', N'添加桶', N'ST-添加桶'),
(N'ST', N'ST', N'添加桶 New liquid washing', N'ST-添加桶 New liquid washing'),
(N'ST', N'ST', N'硫酸洗 H2SO4', N'ST-硫酸洗 H2SO4'),
(N'ST', N'ST', N'硫酸洗(4) H2SO3', N'ST-硫酸洗(4) H2SO3'),
(N'ST', N'ST', N'線外配槽', N'ST-線外配槽'),
(N'ST', N'ST', N'銅 copper', N'ST-銅 copper'),
(N'ST2', N'ST2', N'剝膜 (2) 槽 Stripping Tank (2)', N'ST2-剝膜 (2) 槽 Stripping Tank (2)'),
(N'ST2', N'ST2', N'剝膜 (3) 槽 Stripping Tank (3)', N'ST2-剝膜 (3) 槽 Stripping Tank (3)'),
(N'ST2', N'ST2', N'硫酸洗(4) H2SO3', N'ST2-硫酸洗(4) H2SO3');

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
(N'比重', N'比重'),
(N'主劑 (%)', N'主劑 (%)'),
(N'主劑 (ml/L)', N'主劑 (ml/L)'),
(N'氯離子', N'氯離子'),
(N'氯離子(ppm)', N'氯離子(ppm)'),
(N'硝酸 (%)', N'硝酸 (%)'),
(N'硫酸', N'硫酸'),
(N'硫酸 (%)', N'硫酸 (%)'),
(N'硫酸 (ml/L)', N'硫酸 (ml/L)'),
(N'溫度 (℃)', N'溫度 (℃)'),
(N'碳酸鈉 (%)', N'碳酸鈉 (%)'),
(N'酸 (%)', N'酸 (%)'),
(N'酸洗', N'酸洗'),
(N'酸洗 (%)', N'酸洗 (%)'),
(N'銅離子', N'銅離子'),
(N'銅離子 (g/L)', N'銅離子 (g/L)'),
(N'雙氧水', N'雙氧水'),
(N'雙氧水 (%)', N'雙氧水 (%)'),
(N'鹼洗 (%)', N'鹼洗 (%)'),
(N'鹼洗 (ml/L)', N'鹼洗 (ml/L)'),
(N'AC-RF (ml/L)', N'AC-RF (ml/L)'),
(N'BCNa ((g/L)', N'BCNa ((g/L)'),
(N'CHO ((g/L)', N'CHO ((g/L)'),
(N'CHO (%)', N'CHO (%)'),
(N'CHO (g/L)', N'CHO (g/L)'),
(N'CHO (ml/L)', N'CHO (ml/L)'),
(N'CuSO4‧5H2O (g/L)', N'CuSO4‧5H2O (g/L)'),
(N'DP333 (ml/L)', N'DP333 (ml/L)'),
(N'HO (%)', N'HO (%)'),
(N'NaH2PO2‧H2O (g/L)', N'NaH2PO2‧H2O (g/L)'),
(N'Ni (g/L)', N'Ni (g/L)'),
(N'NKH (g/L)', N'NKH (g/L)'),
(N'NNH (%)', N'NNH (%)'),
(N'Normality (N)', N'Normality (N)'),
(N'P200 (ml/L)', N'P200 (ml/L)'),
(N'P300 (g/L)', N'P300 (g/L)'),
(N'P400 (ml/L)', N'P400 (ml/L)'),
(N'P500A (ml/L)', N'P500A (ml/L)'),
(N'PH', N'PH'),
(N'S RF-A (ml/L)', N'S RF-A (ml/L)'),
(N'S RF-B (ml/L)', N'S RF-B (ml/L)'),
(N'SF', N'SF'),
(N'SnCl2 (g/L)', N'SnCl2 (g/L)'),
(N'SPS (g/L)', N'SPS (g/L)');

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
(N'C1', N'C1', N'銅1', N'C1-銅1', N'氯離子(ppm)'),
(N'C1', N'C1', N'銅1', N'C1-銅1', N'CHO (g/L)'),
(N'C1', N'C1', N'銅1', N'C1-銅1', N'CuSO4‧5H2O (g/L)'),
(N'C1', N'C1', N'銅1', N'C1-銅1', N'S RF-A (ml/L)'),
(N'C1', N'C1', N'銅1', N'C1-銅1', N'S RF-B (ml/L)'),
(N'C1', N'C1', N'銅2', N'C1-銅2', N'氯離子(ppm)'),
(N'C1', N'C1', N'銅2', N'C1-銅2', N'CHO (g/L)'),
(N'C1', N'C1', N'銅2', N'C1-銅2', N'CuSO4‧5H2O (g/L)'),
(N'C1', N'C1', N'銅2', N'C1-銅2', N'S RF-A (ml/L)'),
(N'C1', N'C1', N'銅2', N'C1-銅2', N'S RF-B (ml/L)'),
(N'C3', N'C3', N'剝掛槽', N'C3-剝掛槽', N'硫酸'),
(N'C3', N'C3', N'剝掛槽', N'C3-剝掛槽', N'銅離子'),
(N'C3', N'C3', N'剝掛槽', N'C3-剝掛槽', N'雙氧水'),
(N'C3', N'C3', N'清潔槽', N'C3-清潔槽', N'CHO (ml/L)'),
(N'C3', N'C3', N'酸浸槽', N'C3-酸浸槽', N'硫酸 (ml/L)'),
(N'C3', N'C3', N'鍍銅槽', N'C3-鍍銅槽', N'氯離子'),
(N'C3', N'C3', N'鍍銅槽', N'C3-鍍銅槽', N'硫酸'),
(N'C3', N'C3', N'鍍銅槽', N'C3-鍍銅槽', N'銅離子'),
(N'C3', N'C3', N'鍍銅槽', N'C3-鍍銅槽', N'SF'),
(N'C5', N'C5', N'剝掛槽', N'C5-剝掛槽', N'硫酸'),
(N'C5', N'C5', N'剝掛槽', N'C5-剝掛槽', N'銅離子'),
(N'C5', N'C5', N'剝掛槽', N'C5-剝掛槽', N'雙氧水'),
(N'C5', N'C5', N'清潔槽', N'C5-清潔槽', N'DP333 (ml/L)'),
(N'C5', N'C5', N'酸浸槽', N'C5-酸浸槽', N'硫酸 (%)'),
(N'CN', N'CN', N'鹼洗1', N'CN-鹼洗1', N'BCNa ((g/L)'),
(N'CN', N'CN', N'鹼洗1', N'CN-鹼洗1', N'CHO ((g/L)'),
(N'CN', N'CN', N'鹼洗2', N'CN-鹼洗2', N'BCNa ((g/L)'),
(N'CN', N'CN', N'鹼洗2', N'CN-鹼洗2', N'CHO ((g/L)'),
(N'DP', N'DP', N'抗氧化', N'DP-抗氧化', N'PH'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽', N'主劑 (%)'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽', N'主劑 (ml/L)'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽', N'溫度 (℃)'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽', N'PH'),
(N'DP', N'DP', N'除鈀槽', N'DP-除鈀槽', N'SPS (g/L)'),
(N'DP', N'DP', N'清潔槽', N'DP-清潔槽', N'鹼洗 (%)'),
(N'DP', N'DP', N'清潔槽', N'DP-清潔槽', N'鹼洗 (ml/L)'),
(N'DP', N'DP', N'酸洗槽', N'DP-酸洗槽', N'酸洗 (%)'),
(N'DV', N'DV', N'液洗 Liquid washing', N'DV-液洗 Liquid washing', N'碳酸鈉 (%)'),
(N'DV', N'DV', N'液洗 Liquid washing', N'DV-液洗 Liquid washing', N'PH'),
(N'DV', N'DV', N'添加桶 Add Tank', N'DV-添加桶 Add Tank', N'碳酸鈉 (%)'),
(N'DV', N'DV', N'添加桶 Add Tank', N'DV-添加桶 Add Tank', N'PH'),
(N'DV', N'DV', N'新液洗 New liquid washing', N'DV-新液洗 New liquid washing', N'碳酸鈉 (%)'),
(N'DV', N'DV', N'新液洗 New liquid washing', N'DV-新液洗 New liquid washing', N'PH'),
(N'DV', N'DV', N'顯影槽Developer Tank', N'DV-顯影槽Developer Tank', N'碳酸鈉 (%)'),
(N'DV', N'DV', N'顯影槽Developer Tank', N'DV-顯影槽Developer Tank', N'PH'),
(N'DV2', N'DV2', N'液洗 Liquid washing', N'DV2-液洗 Liquid washing', N'碳酸鈉 (%)'),
(N'DV2', N'DV2', N'液洗 Liquid washing', N'DV2-液洗 Liquid washing', N'PH'),
(N'DV2', N'DV2', N'添加桶 Add Tank', N'DV2-添加桶 Add Tank', N'碳酸鈉 (%)'),
(N'DV2', N'DV2', N'添加桶 Add Tank', N'DV2-添加桶 Add Tank', N'PH'),
(N'DV2', N'DV2', N'新液洗 New liquid washing', N'DV2-新液洗 New liquid washing', N'碳酸鈉 (%)'),
(N'DV2', N'DV2', N'新液洗 New liquid washing', N'DV2-新液洗 New liquid washing', N'PH'),
(N'DV2', N'DV2', N'顯影槽Developer Tank', N'DV2-顯影槽Developer Tank', N'碳酸鈉 (%)'),
(N'DV2', N'DV2', N'顯影槽Developer Tank', N'DV2-顯影槽Developer Tank', N'PH'),
(N'N1', N'N1', N'表面處理', N'N1-表面處理', N'NKH (g/L)'),
(N'N1', N'N1', N'表面處理', N'N1-表面處理', N'NNH (%)'),
(N'N1', N'N1', N'表面處理', N'N1-表面處理', N'PH'),
(N'N1', N'N1', N'促進', N'N1-促進', N'P500A (ml/L)'),
(N'N1', N'N1', N'促進', N'N1-促進', N'SnCl2 (g/L)'),
(N'N1', N'N1', N'催化', N'N1-催化', N'AC-RF (ml/L)'),
(N'N1', N'N1', N'催化', N'N1-催化', N'Normality (N)'),
(N'N1', N'N1', N'催化', N'N1-催化', N'SnCl2 (g/L)'),
(N'N1', N'N1', N'預浸', N'N1-預浸', N'Normality (N)'),
(N'N1', N'N1', N'調節', N'N1-調節', N'P200 (ml/L)'),
(N'N1', N'N1', N'調節', N'N1-調節', N'PH'),
(N'N2', N'N2', N'化鎳', N'N2-化鎳', N'NaH2PO2‧H2O (g/L)'),
(N'N2', N'N2', N'化鎳', N'N2-化鎳', N'Ni (g/L)'),
(N'N2', N'N2', N'化鎳', N'N2-化鎳', N'PH'),
(N'N2', N'N2', N'表面處理', N'N2-表面處理', N'NKH (g/L)'),
(N'N2', N'N2', N'表面處理', N'N2-表面處理', N'NNH (%)'),
(N'N2', N'N2', N'表面處理', N'N2-表面處理', N'PH'),
(N'N2', N'N2', N'促進', N'N2-促進', N'P500A (ml/L)'),
(N'N2', N'N2', N'促進', N'N2-促進', N'SnCl2 (g/L)'),
(N'N2', N'N2', N'催化', N'N2-催化', N'P400 (ml/L)'),
(N'N2', N'N2', N'催化', N'N2-催化', N'SnCl2 (g/L)'),
(N'N2', N'N2', N'預浸', N'N2-預浸', N'P300 (g/L)'),
(N'N2', N'N2', N'調節', N'N2-調節', N'P200 (ml/L)'),
(N'N2', N'N2', N'調節', N'N2-調節', N'PH'),
(N'PT', N'PT', N'抗氧化 Anti-oxidation', N'PT-抗氧化 Anti-oxidation', N'PH'),
(N'PT', N'PT', N'脫脂槽 Degreasing', N'PT-脫脂槽 Degreasing', N'CHO (%)'),
(N'PT', N'PT', N'銅 Copper', N'PT-銅 Copper', N'比重'),
(N'PT', N'PT', N'銅 Copper', N'PT-銅 Copper', N'溫度 (℃)'),
(N'PT', N'PT', N'銅 Copper', N'PT-銅 Copper', N'銅離子 (g/L)'),
(N'PT', N'PT', N'鹽酸洗 HCL', N'PT-鹽酸洗 HCL', N'銅離子 (g/L)'),
(N'PT', N'PT', N'鹽酸洗 HCL', N'PT-鹽酸洗 HCL', N'CHO (%)'),
(N'PT2', N'PT2', N'抗氧化 Anti-oxidation', N'PT2-抗氧化 Anti-oxidation', N'PH'),
(N'PT2', N'PT2', N'脫脂槽 Degreasing', N'PT2-脫脂槽 Degreasing', N'CHO (%)'),
(N'PT2', N'PT2', N'銅 Copper', N'PT2-銅 Copper', N'比重'),
(N'PT2', N'PT2', N'銅 Copper', N'PT2-銅 Copper', N'銅離子 (g/L)'),
(N'PT2', N'PT2', N'鹽酸洗 HCL', N'PT2-鹽酸洗 HCL', N'銅離子 (g/L)'),
(N'PT2', N'PT2', N'鹽酸洗 HCL', N'PT2-鹽酸洗 HCL', N'CHO (%)'),
(N'QE', N'QE', N'抗氧化 Anti-oxidation', N'QE-抗氧化 Anti-oxidation', N'PH'),
(N'QE', N'QE', N'硫酸洗 H2SO4', N'QE-硫酸洗 H2SO4', N'酸洗'),
(N'QE', N'QE', N'硫酸洗 H2SO4', N'QE-硫酸洗 H2SO4', N'CHO (%)'),
(N'QE', N'QE', N'蝕刻主槽 Main Etching Tank', N'QE-蝕刻主槽 Main Etching Tank', N'酸 (%)'),
(N'QE', N'QE', N'蝕刻主槽 Main Etching Tank', N'QE-蝕刻主槽 Main Etching Tank', N'銅離子 (g/L)'),
(N'QE', N'QE', N'蝕刻主槽 Main Etching Tank', N'QE-蝕刻主槽 Main Etching Tank', N'雙氧水 (%)'),
(N'QE', N'QE', N'銅 copper', N'QE-銅 copper', N'酸 (%)'),
(N'QE', N'QE', N'銅 copper', N'QE-銅 copper', N'銅離子 (g/L)'),
(N'QE', N'QE', N'銅 copper', N'QE-銅 copper', N'雙氧水 (%)'),
(N'QE', N'QE', N'線外配槽', N'QE-線外配槽', N'硝酸 (%)'),
(N'QE', N'QE', N'線外配槽', N'QE-線外配槽', N'酸洗'),
(N'QE', N'QE', N'線外配槽', N'QE-線外配槽', N'雙氧水 (%)'),
(N'QE', N'QE', N'NS-357', N'QE-NS-357', N'硝酸 (%)'),
(N'QE', N'QE', N'NS-357', N'QE-NS-357', N'銅離子 (g/L)'),
(N'QE', N'QE', N'NS-357', N'QE-NS-357', N'雙氧水 (%)'),
(N'QE2', N'QE2', N'硫酸洗 H2SO4', N'QE2-硫酸洗 H2SO4', N'酸洗'),
(N'QE2', N'QE2', N'蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'QE2-蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'硝酸 (%)'),
(N'QE2', N'QE2', N'蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'QE2-蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'銅離子 (g/L)'),
(N'QE2', N'QE2', N'蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'QE2-蝕刻(3)槽(鎳) Etching Tank (3) – Nickel', N'雙氧水 (%)'),
(N'QE2', N'QE2', N'蝕刻主槽(銅) Main Etching Tank', N'QE2-蝕刻主槽(銅) Main Etching Tank', N'酸 (%)'),
(N'QE2', N'QE2', N'蝕刻主槽(銅) Main Etching Tank', N'QE2-蝕刻主槽(銅) Main Etching Tank', N'銅離子 (g/L)'),
(N'QE2', N'QE2', N'蝕刻主槽(銅) Main Etching Tank', N'QE2-蝕刻主槽(銅) Main Etching Tank', N'雙氧水 (%)'),
(N'ST', N'ST', N'剝膜 (1) 槽 Stripping Tank (1)', N'ST-剝膜 (1) 槽 Stripping Tank (1)', N'HO (%)'),
(N'ST', N'ST', N'剝膜 (2) 槽Stripping Tank (2)', N'ST-剝膜 (2) 槽Stripping Tank (2)', N'HO (%)'),
(N'ST', N'ST', N'剝膜 小槽Resist Stripping Small Tank', N'ST-剝膜 小槽Resist Stripping Small Tank', N'HO (%)'),
(N'ST', N'ST', N'剝膜 添加桶 Resist Stripping Additive Tank', N'ST-剝膜 添加桶 Resist Stripping Additive Tank', N'HO (%)'),
(N'ST', N'ST', N'液洗 Liquid washing', N'ST-液洗 Liquid washing', N'HO (%)'),
(N'ST', N'ST', N'液洗(2) Liquid washing', N'ST-液洗(2) Liquid washing', N'HO (%)'),
(N'ST', N'ST', N'液洗(3) New liquid washing', N'ST-液洗(3) New liquid washing', N'HO (%)'),
(N'ST', N'ST', N'添加桶', N'ST-添加桶', N'HO (%)'),
(N'ST', N'ST', N'添加桶 New liquid washing', N'ST-添加桶 New liquid washing', N'HO (%)'),
(N'ST', N'ST', N'硫酸洗 H2SO4', N'ST-硫酸洗 H2SO4', N'酸洗'),
(N'ST', N'ST', N'硫酸洗 H2SO4', N'ST-硫酸洗 H2SO4', N'CHO (%)'),
(N'ST', N'ST', N'硫酸洗(4) H2SO3', N'ST-硫酸洗(4) H2SO3', N'CHO (%)'),
(N'ST', N'ST', N'新液洗 New liquid washing', N'ST-新液洗 New liquid washing', N'HO (%)'),
(N'ST', N'ST', N'銅 copper', N'ST-銅 copper', N'酸 (%)'),
(N'ST', N'ST', N'銅 copper', N'ST-銅 copper', N'銅離子 (g/L)'),
(N'ST', N'ST', N'銅 copper', N'ST-銅 copper', N'雙氧水 (%)'),
(N'ST', N'ST', N'線外配槽', N'ST-線外配槽', N'HO (%)'),
(N'ST', N'ST', N'NS-357', N'ST-NS-357', N'硝酸 (%)'),
(N'ST', N'ST', N'NS-357', N'ST-NS-357', N'雙氧水 (%)'),
(N'ST', N'ST', N'ST小槽 New liquid washing', N'ST-ST小槽 New liquid washing', N'HO (%)'),
(N'ST2', N'ST2', N'剝膜 (2) 槽 Stripping Tank (2)', N'ST2-剝膜 (2) 槽 Stripping Tank (2)', N'HO (%)'),
(N'ST2', N'ST2', N'剝膜 (3) 槽 Stripping Tank (3)', N'ST2-剝膜 (3) 槽 Stripping Tank (3)', N'HO (%)'),
(N'ST2', N'ST2', N'硫酸洗(4) H2SO3', N'ST2-硫酸洗(4) H2SO3', N'CHO (%)');

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
