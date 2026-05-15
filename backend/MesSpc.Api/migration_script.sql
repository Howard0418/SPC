IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AlertEvents] (
    [Id] INTEGER NOT NULL,
    [OccurredAt] TEXT NOT NULL,
    [ProductId] INTEGER NOT NULL,
    [StationId] INTEGER NOT NULL,
    [InspectionItemId] INTEGER NOT NULL,
    [ActualValue] REAL NULL,
    [AlertType] INTEGER NOT NULL,
    [Message] TEXT NOT NULL,
    [BatchId] INTEGER NULL,
    [MeasurementValueId] INTEGER NULL,
    [IsAcknowledged] INTEGER NOT NULL,
    CONSTRAINT [PK_AlertEvents] PRIMARY KEY ([Id])
);

CREATE TABLE [FormulaDefinitions] (
    [Id] INTEGER NOT NULL,
    [FormulaCode] TEXT NOT NULL,
    [DisplayName] TEXT NOT NULL,
    [Expression] TEXT NOT NULL,
    [IsBuiltIn] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_FormulaDefinitions] PRIMARY KEY ([Id])
);

CREATE TABLE [InspectionItems] (
    [Id] INTEGER NOT NULL,
    [ItemCode] TEXT NOT NULL,
    [ItemName] TEXT NOT NULL,
    [DataType] INTEGER NOT NULL,
    [Unit] TEXT NULL,
    [Usl] REAL NULL,
    [Lsl] REAL NULL,
    [Ucl] REAL NULL,
    [Lcl] REAL NULL,
    [TargetValue] REAL NULL,
    [IsSpcEnabled] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_InspectionItems] PRIMARY KEY ([Id])
);

CREATE TABLE [MeasurementBatches] (
    [Id] INTEGER NOT NULL,
    [BatchNo] TEXT NOT NULL,
    [ProductId] INTEGER NOT NULL,
    [StationId] INTEGER NOT NULL,
    [MeasuredAt] TEXT NOT NULL,
    [OperatorName] TEXT NULL,
    [SourceType] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_MeasurementBatches] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] INTEGER NOT NULL,
    [ProductCode] TEXT NOT NULL,
    [ProductName] TEXT NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductStationItems] (
    [Id] INTEGER NOT NULL,
    [ProductId] INTEGER NOT NULL,
    [StationId] INTEGER NOT NULL,
    [InspectionItemId] INTEGER NOT NULL,
    [SampleSize] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_ProductStationItems] PRIMARY KEY ([Id])
);

CREATE TABLE [Stations] (
    [Id] INTEGER NOT NULL,
    [StationCode] TEXT NOT NULL,
    [StationName] TEXT NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_Stations] PRIMARY KEY ([Id])
);

CREATE TABLE [MeasurementValues] (
    [Id] INTEGER NOT NULL,
    [BatchId] INTEGER NOT NULL,
    [InspectionItemId] INTEGER NOT NULL,
    [SampleNo] INTEGER NOT NULL,
    [ValueNumeric] REAL NULL,
    [ValueText] TEXT NULL,
    [ValueBool] INTEGER NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_MeasurementValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MeasurementValues_MeasurementBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [MeasurementBatches] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_FormulaDefinitions_FormulaCode] ON [FormulaDefinitions] ([FormulaCode]);

CREATE UNIQUE INDEX [IX_InspectionItems_ItemCode] ON [InspectionItems] ([ItemCode]);

CREATE INDEX [IX_MeasurementValues_BatchId] ON [MeasurementValues] ([BatchId]);

CREATE UNIQUE INDEX [IX_Products_ProductCode] ON [Products] ([ProductCode]);

CREATE UNIQUE INDEX [IX_ProductStationItems_ProductId_StationId_InspectionItemId] ON [ProductStationItems] ([ProductId], [StationId], [InspectionItemId]);

CREATE UNIQUE INDEX [IX_Stations_StationCode] ON [Stations] ([StationCode]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260507050816_InitialCreate', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MeasurementBatches] ADD [LotNo] nvarchar(450) NULL;

ALTER TABLE [MeasurementBatches] ADD [SerialNo] nvarchar(450) NULL;

ALTER TABLE [MeasurementBatches] ADD [StationOperationSessionId] int NULL;

ALTER TABLE [MeasurementBatches] ADD [WorkOrderId] int NULL;

ALTER TABLE [AlertEvents] ADD [ClosedAt] datetime2 NULL;

ALTER TABLE [AlertEvents] ADD [CorrectiveAction] nvarchar(max) NULL;

ALTER TABLE [AlertEvents] ADD [ResponsibleUser] nvarchar(max) NULL;

ALTER TABLE [AlertEvents] ADD [RootCause] nvarchar(max) NULL;

ALTER TABLE [AlertEvents] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [AlertEvents] ADD [UpdatedAt] datetime2 NULL;

CREATE TABLE [StationOperationSessions] (
    [Id] int NOT NULL IDENTITY,
    [WorkOrderId] int NOT NULL,
    [StationId] int NOT NULL,
    [LotNo] nvarchar(450) NULL,
    [SerialNo] nvarchar(450) NULL,
    [OperatorName] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [EndedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StationOperationSessions] PRIMARY KEY ([Id])
);

CREATE TABLE [WorkOrders] (
    [Id] int NOT NULL IDENTITY,
    [WorkOrderNo] nvarchar(450) NOT NULL,
    [ProductId] int NOT NULL,
    [PlannedQty] int NOT NULL,
    [ActualQty] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [PlannedStartTime] datetime2 NULL,
    [PlannedEndTime] datetime2 NULL,
    [ActualStartTime] datetime2 NULL,
    [ActualEndTime] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_MeasurementBatches_LotNo] ON [MeasurementBatches] ([LotNo]);

CREATE INDEX [IX_MeasurementBatches_SerialNo] ON [MeasurementBatches] ([SerialNo]);

CREATE INDEX [IX_MeasurementBatches_WorkOrderId] ON [MeasurementBatches] ([WorkOrderId]);

CREATE INDEX [IX_StationOperationSessions_LotNo] ON [StationOperationSessions] ([LotNo]);

CREATE INDEX [IX_StationOperationSessions_SerialNo] ON [StationOperationSessions] ([SerialNo]);

CREATE INDEX [IX_StationOperationSessions_WorkOrderId] ON [StationOperationSessions] ([WorkOrderId]);

CREATE UNIQUE INDEX [IX_WorkOrders_WorkOrderNo] ON [WorkOrders] ([WorkOrderNo]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260507085750_V2_Phase1_WorkOrder_StationOps_AlertWorkflow', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [ControlChartGroups] (
    [ChartGroupId] int NOT NULL IDENTITY,
    [GroupCode] nvarchar(450) NOT NULL,
    [GroupName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_ControlChartGroups] PRIMARY KEY ([ChartGroupId])
);

CREATE TABLE [Parts] (
    [PartId] int NOT NULL IDENTITY,
    [PartNo] nvarchar(450) NOT NULL,
    [PartName] nvarchar(max) NOT NULL,
    [Specification] nvarchar(max) NULL,
    [Customer] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Parts] PRIMARY KEY ([PartId])
);

CREATE TABLE [Processes] (
    [ProcessId] int NOT NULL IDENTITY,
    [ProcessCode] nvarchar(450) NOT NULL,
    [ProcessName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_Processes] PRIMARY KEY ([ProcessId])
);

CREATE TABLE [SpcRuleGroups] (
    [RuleGroupId] int NOT NULL IDENTITY,
    [RuleGroupCode] nvarchar(450) NOT NULL,
    [RuleGroupName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_SpcRuleGroups] PRIMARY KEY ([RuleGroupId])
);

CREATE TABLE [UploadBatches] (
    [UploadBatchId] uniqueidentifier NOT NULL,
    [UploadType] nvarchar(max) NOT NULL,
    [SourceType] nvarchar(max) NOT NULL,
    [ImportStatus] nvarchar(max) NOT NULL,
    [OriginalFileName] nvarchar(max) NULL,
    [TotalRows] int NOT NULL,
    [ValidRows] int NOT NULL,
    [ErrorRows] int NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ConfirmedAt] datetime2 NULL,
    CONSTRAINT [PK_UploadBatches] PRIMARY KEY ([UploadBatchId])
);

CREATE TABLE [ControlChartCategories] (
    [ChartCategoryId] int NOT NULL IDENTITY,
    [ChartGroupId] int NOT NULL,
    [CategoryCode] nvarchar(450) NOT NULL,
    [CategoryName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_ControlChartCategories] PRIMARY KEY ([ChartCategoryId]),
    CONSTRAINT [FK_ControlChartCategories_ControlChartGroups_ChartGroupId] FOREIGN KEY ([ChartGroupId]) REFERENCES [ControlChartGroups] ([ChartGroupId]) ON DELETE NO ACTION
);

CREATE TABLE [Machines] (
    [MachineId] int NOT NULL IDENTITY,
    [MachineCode] nvarchar(450) NOT NULL,
    [MachineName] nvarchar(max) NOT NULL,
    [ProcessId] int NOT NULL,
    [Location] nvarchar(max) NULL,
    [Status] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_Machines] PRIMARY KEY ([MachineId]),
    CONSTRAINT [FK_Machines_Processes_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [Processes] ([ProcessId]) ON DELETE NO ACTION
);

CREATE TABLE [SpcRules] (
    [RuleId] int NOT NULL IDENTITY,
    [RuleGroupId] int NOT NULL,
    [RuleCode] nvarchar(450) NOT NULL,
    [RuleName] nvarchar(max) NOT NULL,
    [RuleConfigJson] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_SpcRules] PRIMARY KEY ([RuleId]),
    CONSTRAINT [FK_SpcRules_SpcRuleGroups_RuleGroupId] FOREIGN KEY ([RuleGroupId]) REFERENCES [SpcRuleGroups] ([RuleGroupId]) ON DELETE CASCADE
);

CREATE TABLE [UploadDetails] (
    [UploadDetailId] bigint NOT NULL IDENTITY,
    [UploadBatchId] uniqueidentifier NOT NULL,
    [RowNo] int NOT NULL,
    [PayloadJson] nvarchar(max) NOT NULL,
    [IsValid] bit NOT NULL,
    CONSTRAINT [PK_UploadDetails] PRIMARY KEY ([UploadDetailId]),
    CONSTRAINT [FK_UploadDetails_UploadBatches_UploadBatchId] FOREIGN KEY ([UploadBatchId]) REFERENCES [UploadBatches] ([UploadBatchId]) ON DELETE CASCADE
);

CREATE TABLE [ControlChartTypes] (
    [ChartTypeId] int NOT NULL IDENTITY,
    [ChartCategoryId] int NOT NULL,
    [ChartTypeCode] nvarchar(450) NOT NULL,
    [ChartTypeName] nvarchar(max) NOT NULL,
    [DataCategory] nvarchar(max) NOT NULL,
    [RequiredSampleSize] int NULL,
    [Description] nvarchar(max) NULL,
    [FormulaConfigJson] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_ControlChartTypes] PRIMARY KEY ([ChartTypeId]),
    CONSTRAINT [FK_ControlChartTypes_ControlChartCategories_ChartCategoryId] FOREIGN KEY ([ChartCategoryId]) REFERENCES [ControlChartCategories] ([ChartCategoryId]) ON DELETE NO ACTION
);

CREATE TABLE [UploadErrors] (
    [UploadErrorId] bigint NOT NULL IDENTITY,
    [UploadBatchId] uniqueidentifier NOT NULL,
    [UploadDetailId] bigint NULL,
    [RowNo] int NULL,
    [FieldName] nvarchar(max) NULL,
    [ErrorCode] nvarchar(max) NOT NULL,
    [ErrorMessage] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_UploadErrors] PRIMARY KEY ([UploadErrorId]),
    CONSTRAINT [FK_UploadErrors_UploadBatches_UploadBatchId] FOREIGN KEY ([UploadBatchId]) REFERENCES [UploadBatches] ([UploadBatchId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UploadErrors_UploadDetails_UploadDetailId] FOREIGN KEY ([UploadDetailId]) REFERENCES [UploadDetails] ([UploadDetailId]) ON DELETE SET NULL
);

CREATE TABLE [QualityCharacteristics] (
    [CharacteristicId] int NOT NULL IDENTITY,
    [CharacteristicCode] nvarchar(450) NOT NULL,
    [CharacteristicName] nvarchar(max) NOT NULL,
    [DataCategory] nvarchar(max) NOT NULL,
    [Unit] nvarchar(max) NULL,
    [DefaultChartTypeId] int NULL,
    [IsSpcEnabled] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_QualityCharacteristics] PRIMARY KEY ([CharacteristicId]),
    CONSTRAINT [FK_QualityCharacteristics_ControlChartTypes_DefaultChartTypeId] FOREIGN KEY ([DefaultChartTypeId]) REFERENCES [ControlChartTypes] ([ChartTypeId]) ON DELETE SET NULL
);

CREATE TABLE [PartProcessCharacteristics] (
    [Id] int NOT NULL IDENTITY,
    [PartId] int NOT NULL,
    [ProcessId] int NOT NULL,
    [CharacteristicId] int NOT NULL,
    [USL] float NULL,
    [LSL] float NULL,
    [UCL] float NULL,
    [CL] float NULL,
    [LCL] float NULL,
    [TargetValue] float NULL,
    [SampleSize] int NOT NULL,
    [ChartTypeId] int NULL,
    [RuleGroupId] int NULL,
    [IsRequired] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_PartProcessCharacteristics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PartProcessCharacteristics_ControlChartTypes_ChartTypeId] FOREIGN KEY ([ChartTypeId]) REFERENCES [ControlChartTypes] ([ChartTypeId]) ON DELETE SET NULL,
    CONSTRAINT [FK_PartProcessCharacteristics_Parts_PartId] FOREIGN KEY ([PartId]) REFERENCES [Parts] ([PartId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PartProcessCharacteristics_Processes_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [Processes] ([ProcessId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PartProcessCharacteristics_QualityCharacteristics_CharacteristicId] FOREIGN KEY ([CharacteristicId]) REFERENCES [QualityCharacteristics] ([CharacteristicId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PartProcessCharacteristics_SpcRuleGroups_RuleGroupId] FOREIGN KEY ([RuleGroupId]) REFERENCES [SpcRuleGroups] ([RuleGroupId]) ON DELETE SET NULL
);

CREATE TABLE [AttributeMeasurements] (
    [Id] bigint NOT NULL IDENTITY,
    [UploadBatchId] uniqueidentifier NOT NULL,
    [PartId] int NOT NULL,
    [ProcessId] int NOT NULL,
    [MachineId] int NOT NULL,
    [CharacteristicId] int NOT NULL,
    [PartProcessCharacteristicId] int NOT NULL,
    [LotNo] nvarchar(max) NULL,
    [SampleNo] int NOT NULL,
    [InspectedQty] int NULL,
    [DefectQty] int NULL,
    [DefectCount] int NULL,
    [UnitCount] int NULL,
    [MeasuredAt] datetime2 NOT NULL,
    [Operator] nvarchar(max) NULL,
    CONSTRAINT [PK_AttributeMeasurements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttributeMeasurements_PartProcessCharacteristics_PartProcessCharacteristicId] FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttributeMeasurements_UploadBatches_UploadBatchId] FOREIGN KEY ([UploadBatchId]) REFERENCES [UploadBatches] ([UploadBatchId]) ON DELETE NO ACTION
);

CREATE TABLE [SpcCalculationResults] (
    [SpcResultId] bigint NOT NULL IDENTITY,
    [UploadBatchId] uniqueidentifier NOT NULL,
    [DataCategory] nvarchar(max) NOT NULL,
    [VariableMeasurementId] bigint NULL,
    [AttributeMeasurementId] bigint NULL,
    [PartProcessCharacteristicId] int NOT NULL,
    [ChartTypeId] int NOT NULL,
    [RuleGroupId] int NULL,
    [StatisticName] nvarchar(max) NULL,
    [StatisticValue] float NULL,
    [USL] float NULL,
    [LSL] float NULL,
    [UCL] float NULL,
    [CL] float NULL,
    [LCL] float NULL,
    [IsOutOfSpec] bit NOT NULL,
    [IsOutOfControl] bit NOT NULL,
    [ViolatedRulesJson] nvarchar(max) NULL,
    [CalculatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SpcCalculationResults] PRIMARY KEY ([SpcResultId]),
    CONSTRAINT [FK_SpcCalculationResults_ControlChartTypes_ChartTypeId] FOREIGN KEY ([ChartTypeId]) REFERENCES [ControlChartTypes] ([ChartTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SpcCalculationResults_PartProcessCharacteristics_PartProcessCharacteristicId] FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SpcCalculationResults_UploadBatches_UploadBatchId] FOREIGN KEY ([UploadBatchId]) REFERENCES [UploadBatches] ([UploadBatchId]) ON DELETE NO ACTION
);

CREATE TABLE [VariableMeasurements] (
    [Id] bigint NOT NULL IDENTITY,
    [UploadBatchId] uniqueidentifier NOT NULL,
    [PartId] int NOT NULL,
    [ProcessId] int NOT NULL,
    [MachineId] int NOT NULL,
    [CharacteristicId] int NOT NULL,
    [PartProcessCharacteristicId] int NOT NULL,
    [LotNo] nvarchar(max) NULL,
    [SerialNo] nvarchar(max) NULL,
    [SampleNo] int NOT NULL,
    [MeasuredValue] float NOT NULL,
    [MeasuredAt] datetime2 NOT NULL,
    [Operator] nvarchar(max) NULL,
    CONSTRAINT [PK_VariableMeasurements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VariableMeasurements_PartProcessCharacteristics_PartProcessCharacteristicId] FOREIGN KEY ([PartProcessCharacteristicId]) REFERENCES [PartProcessCharacteristics] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VariableMeasurements_UploadBatches_UploadBatchId] FOREIGN KEY ([UploadBatchId]) REFERENCES [UploadBatches] ([UploadBatchId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AttributeMeasurements_PartId_ProcessId_CharacteristicId_MeasuredAt] ON [AttributeMeasurements] ([PartId], [ProcessId], [CharacteristicId], [MeasuredAt]);

CREATE INDEX [IX_AttributeMeasurements_PartProcessCharacteristicId] ON [AttributeMeasurements] ([PartProcessCharacteristicId]);

CREATE INDEX [IX_AttributeMeasurements_UploadBatchId] ON [AttributeMeasurements] ([UploadBatchId]);

CREATE UNIQUE INDEX [IX_ControlChartCategories_ChartGroupId_CategoryCode] ON [ControlChartCategories] ([ChartGroupId], [CategoryCode]);

CREATE UNIQUE INDEX [IX_ControlChartGroups_GroupCode] ON [ControlChartGroups] ([GroupCode]);

CREATE INDEX [IX_ControlChartTypes_ChartCategoryId] ON [ControlChartTypes] ([ChartCategoryId]);

CREATE UNIQUE INDEX [IX_ControlChartTypes_ChartTypeCode] ON [ControlChartTypes] ([ChartTypeCode]);

CREATE UNIQUE INDEX [IX_Machines_MachineCode] ON [Machines] ([MachineCode]);

CREATE INDEX [IX_Machines_ProcessId] ON [Machines] ([ProcessId]);

CREATE INDEX [IX_PartProcessCharacteristics_CharacteristicId] ON [PartProcessCharacteristics] ([CharacteristicId]);

CREATE INDEX [IX_PartProcessCharacteristics_ChartTypeId] ON [PartProcessCharacteristics] ([ChartTypeId]);

CREATE UNIQUE INDEX [IX_PartProcessCharacteristics_PartId_ProcessId_CharacteristicId] ON [PartProcessCharacteristics] ([PartId], [ProcessId], [CharacteristicId]);

CREATE INDEX [IX_PartProcessCharacteristics_ProcessId] ON [PartProcessCharacteristics] ([ProcessId]);

CREATE INDEX [IX_PartProcessCharacteristics_RuleGroupId] ON [PartProcessCharacteristics] ([RuleGroupId]);

CREATE UNIQUE INDEX [IX_Parts_PartNo] ON [Parts] ([PartNo]);

CREATE UNIQUE INDEX [IX_Processes_ProcessCode] ON [Processes] ([ProcessCode]);

CREATE UNIQUE INDEX [IX_QualityCharacteristics_CharacteristicCode] ON [QualityCharacteristics] ([CharacteristicCode]);

CREATE INDEX [IX_QualityCharacteristics_DefaultChartTypeId] ON [QualityCharacteristics] ([DefaultChartTypeId]);

CREATE INDEX [IX_SpcCalculationResults_ChartTypeId] ON [SpcCalculationResults] ([ChartTypeId]);

CREATE INDEX [IX_SpcCalculationResults_PartProcessCharacteristicId_CalculatedAt] ON [SpcCalculationResults] ([PartProcessCharacteristicId], [CalculatedAt]);

CREATE INDEX [IX_SpcCalculationResults_UploadBatchId] ON [SpcCalculationResults] ([UploadBatchId]);

CREATE UNIQUE INDEX [IX_SpcRuleGroups_RuleGroupCode] ON [SpcRuleGroups] ([RuleGroupCode]);

CREATE UNIQUE INDEX [IX_SpcRules_RuleGroupId_RuleCode] ON [SpcRules] ([RuleGroupId], [RuleCode]);

CREATE UNIQUE INDEX [IX_UploadDetails_UploadBatchId_RowNo] ON [UploadDetails] ([UploadBatchId], [RowNo]);

CREATE INDEX [IX_UploadErrors_UploadBatchId] ON [UploadErrors] ([UploadBatchId]);

CREATE INDEX [IX_UploadErrors_UploadDetailId] ON [UploadErrors] ([UploadDetailId]);

CREATE INDEX [IX_VariableMeasurements_PartId_ProcessId_CharacteristicId_MeasuredAt] ON [VariableMeasurements] ([PartId], [ProcessId], [CharacteristicId], [MeasuredAt]);

CREATE INDEX [IX_VariableMeasurements_PartProcessCharacteristicId] ON [VariableMeasurements] ([PartProcessCharacteristicId]);

CREATE INDEX [IX_VariableMeasurements_UploadBatchId] ON [VariableMeasurements] ([UploadBatchId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508040029_V3_MasterDataAndUploads', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513023909_InitialSqlServer', N'10.0.7');

COMMIT;
GO

