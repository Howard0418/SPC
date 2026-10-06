using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Existing DB Sets
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<InspectionItem> InspectionItems => Set<InspectionItem>();
    public DbSet<ProductStationItem> ProductStationItems => Set<ProductStationItem>();
    public DbSet<MeasurementBatch> MeasurementBatches => Set<MeasurementBatch>();
    public DbSet<MeasurementValue> MeasurementValues => Set<MeasurementValue>();
    public DbSet<FormulaDefinition> FormulaDefinitions => Set<FormulaDefinition>();
    public DbSet<ChemicalFTableVersion> ChemicalFTableVersions => Set<ChemicalFTableVersion>();
    public DbSet<ChemicalFTableCell> ChemicalFTableCells => Set<ChemicalFTableCell>();
    public DbSet<ChemicalFTableReference> ChemicalFTableReferences => Set<ChemicalFTableReference>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<StationOperationSession> StationOperationSessions => Set<StationOperationSession>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<QualityCharacteristic> QualityCharacteristics => Set<QualityCharacteristic>();
    public DbSet<PartProcessCharacteristic> PartProcessCharacteristics => Set<PartProcessCharacteristic>();
    public DbSet<ControlChartGroup> ControlChartGroups => Set<ControlChartGroup>();
    public DbSet<ControlChartType> ControlChartTypes => Set<ControlChartType>();
    public DbSet<UploadBatch> UploadBatches => Set<UploadBatch>();
    public DbSet<UploadDetail> UploadDetails => Set<UploadDetail>();
    public DbSet<UploadError> UploadErrors => Set<UploadError>();
    public DbSet<VariableMeasurement> VariableMeasurements => Set<VariableMeasurement>();
    public DbSet<AttributeMeasurement> AttributeMeasurements => Set<AttributeMeasurement>();
    public DbSet<ParticleMeasurement> ParticleMeasurements => Set<ParticleMeasurement>();
    public DbSet<SpcRuleGroup> SpcRuleGroups => Set<SpcRuleGroup>();
    public DbSet<SpcRule> SpcRules => Set<SpcRule>();
    public DbSet<SpcCalculationResult> SpcCalculationResults => Set<SpcCalculationResult>();
    public DbSet<SpcPointExclusion> SpcPointExclusions => Set<SpcPointExclusion>();
    public DbSet<ControlLimitSegment> ControlLimitSegments => Set<ControlLimitSegment>();

    // New Organizational DB Sets
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<SpcReportSchedule> SpcReportSchedules => Set<SpcReportSchedule>();
    public DbSet<SpcAlertNotificationSetting> SpcAlertNotificationSettings => Set<SpcAlertNotificationSetting>();
    public DbSet<ChameleonSourceSetting> ChameleonSourceSettings => Set<ChameleonSourceSetting>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Chemical> Chemicals => Set<Chemical>();
    public DbSet<EquipmentPointMapping> EquipmentPointMappings => Set<EquipmentPointMapping>();
    public DbSet<MesSyncMessage> MesSyncMessages => Set<MesSyncMessage>();
    public DbSet<LotMaster> LotMasters => Set<LotMaster>();
    public DbSet<LotSplitHistory> LotSplitHistories => Set<LotSplitHistory>();
    public DbSet<Tank> Tanks => Set<Tank>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<SlotParameter> SlotParameters => Set<SlotParameter>();
    public DbSet<LotSlotHistory> LotSlotHistories => Set<LotSlotHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureCalibration();
        modelBuilder.Entity<Product>().HasIndex(x => x.ProductCode).IsUnique();
        modelBuilder.Entity<Station>().HasIndex(x => x.StationCode).IsUnique();
        modelBuilder.Entity<InspectionItem>().HasIndex(x => x.ItemCode).IsUnique();
        modelBuilder.Entity<FormulaDefinition>().HasIndex(x => x.FormulaCode).IsUnique();
        modelBuilder.Entity<ChemicalFTableVersion>().HasIndex(x => x.VersionCode).IsUnique();
        modelBuilder.Entity<ChemicalFTableVersion>().Property(x => x.VersionCode).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableVersion>().Property(x => x.DisplayName).HasMaxLength(100);
        modelBuilder.Entity<ChemicalFTableVersion>().Property(x => x.SourceName).HasMaxLength(100);
        modelBuilder.Entity<ChemicalFTableVersion>().Property(x => x.SourcePath).HasMaxLength(500);
        modelBuilder.Entity<ChemicalFTableCell>().HasIndex(x => new { x.VersionId, x.NormalizedCellAddress }).IsUnique();
        modelBuilder.Entity<ChemicalFTableCell>().Property(x => x.SheetName).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableCell>().Property(x => x.CellAddress).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableCell>().Property(x => x.NormalizedCellAddress).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableCell>().Property(x => x.StandardSolution).HasMaxLength(100);
        modelBuilder.Entity<ChemicalFTableCell>().Property(x => x.NumericValue).HasColumnType("decimal(18,6)");
        modelBuilder.Entity<ChemicalFTableReference>().HasIndex(x => new { x.VersionId, x.NormalizedCellAddress });
        modelBuilder.Entity<ChemicalFTableReference>().HasIndex(x => x.PartProcessCharacteristicId);
        modelBuilder.Entity<ChemicalFTableReference>()
            .HasIndex(x => new { x.VersionId, x.PartProcessCharacteristicId, x.NormalizedCellAddress })
            .IsUnique();
        modelBuilder.Entity<ChemicalFTableReference>().Property(x => x.CellAddress).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableReference>().Property(x => x.NormalizedCellAddress).HasMaxLength(50);
        modelBuilder.Entity<ChemicalFTableReference>().Property(x => x.SourceSheet).HasMaxLength(100);
        modelBuilder.Entity<ChemicalFTableReference>().Property(x => x.ReferenceContext).HasMaxLength(200);
        modelBuilder.Entity<WorkOrder>().HasIndex(x => x.WorkOrderNo).IsUnique();
        modelBuilder.Entity<ProductStationItem>()
            .HasIndex(x => new { x.ProductId, x.StationId, x.InspectionItemId })
            .IsUnique();
        modelBuilder.Entity<MeasurementBatch>().HasIndex(x => x.WorkOrderId);
        modelBuilder.Entity<MeasurementBatch>().HasIndex(x => x.LotNo);
        modelBuilder.Entity<MeasurementBatch>().HasIndex(x => x.SerialNo);
        modelBuilder.Entity<StationOperationSession>().HasIndex(x => x.WorkOrderId);
        modelBuilder.Entity<StationOperationSession>().HasIndex(x => x.LotNo);
        modelBuilder.Entity<StationOperationSession>().HasIndex(x => x.SerialNo);

        modelBuilder.Entity<MeasurementBatch>()
            .HasMany(x => x.Values)
            .WithOne()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Updated Key Mappings to use 'Id'
        modelBuilder.Entity<Part>().HasKey(x => x.Id);
        modelBuilder.Entity<Part>().HasIndex(x => x.PartNo).IsUnique();
        modelBuilder.Entity<Process>().HasKey(x => x.Id);
        modelBuilder.Entity<Process>().HasIndex(x => x.ProcessCode).IsUnique();
        modelBuilder.Entity<Machine>().HasKey(x => x.Id);
        modelBuilder.Entity<Machine>().HasIndex(x => x.MachineCode).IsUnique();
        modelBuilder.Entity<Machine>().HasIndex(x => x.ProcessId);
        modelBuilder.Entity<QualityCharacteristic>().HasKey(x => x.Id);
        modelBuilder.Entity<QualityCharacteristic>().HasIndex(x => x.CharacteristicCode).IsUnique();
        modelBuilder.Entity<QualityCharacteristic>().Property(x => x.InputMode).HasMaxLength(20).HasDefaultValue("DIRECT");
        modelBuilder.Entity<QualityCharacteristic>().Property(x => x.ValueLabel).HasMaxLength(50).HasDefaultValue("量測值");
        modelBuilder.Entity<PartProcessCharacteristic>().HasIndex(x => new { x.ControlScope, x.PartId, x.ProcessId, x.MachineId, x.TankId, x.SlotId, x.CharacteristicId, x.Unit }).IsUnique();
        modelBuilder.Entity<PartProcessCharacteristic>().Property(x => x.Unit).HasMaxLength(50);
        modelBuilder.Entity<PartProcessCharacteristic>().Property(x => x.DisplayMode).HasMaxLength(30).HasDefaultValue("CONTROL_CHART");
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasIndex(x => new { x.ControlScope, x.ProcessId, x.MachineId, x.TankId, x.SlotId, x.CharacteristicId, x.Unit })
            .IsUnique()
            .HasFilter("[PartId] IS NULL");
        modelBuilder.Entity<ControlChartGroup>().HasKey(x => x.Id);
        modelBuilder.Entity<ControlChartGroup>().HasIndex(x => x.GroupCode).IsUnique();
        modelBuilder.Entity<ControlChartGroup>().Property(x => x.BusinessScopeCode).HasMaxLength(50);
        modelBuilder.Entity<ControlChartGroup>().HasIndex(x => x.BusinessScopeCode);
        modelBuilder.Entity<ControlChartType>().HasKey(x => x.Id);
        modelBuilder.Entity<ControlChartType>().HasIndex(x => x.ChartTypeCode).IsUnique();
        modelBuilder.Entity<SpcRuleGroup>().HasKey(x => x.Id);
        modelBuilder.Entity<SpcRuleGroup>().HasIndex(x => x.RuleGroupCode).IsUnique();
        modelBuilder.Entity<SpcRule>().HasKey(x => x.Id);
        modelBuilder.Entity<SpcRule>().HasIndex(x => new { x.RuleGroupId, x.RuleCode }).IsUnique();
        
        modelBuilder.Entity<UploadBatch>().HasKey(x => x.UploadBatchId);
        modelBuilder.Entity<UploadDetail>().HasKey(x => x.Id);
        modelBuilder.Entity<UploadDetail>().HasIndex(x => new { x.UploadBatchId, x.RowNo }).IsUnique();
        modelBuilder.Entity<UploadError>().HasKey(x => x.Id);
        modelBuilder.Entity<UploadError>().HasIndex(x => x.UploadBatchId);
        modelBuilder.Entity<VariableMeasurement>().HasKey(x => x.Id);
        modelBuilder.Entity<VariableMeasurement>().Property(x => x.AdjustAmount).HasMaxLength(1000);
        modelBuilder.Entity<VariableMeasurement>().Property(x => x.SamplingPhase).HasMaxLength(16).HasDefaultValue("GENERAL");
        modelBuilder.Entity<VariableMeasurement>().Property(x => x.SamplingStage).HasMaxLength(16).HasDefaultValue("GENERAL");
        modelBuilder.Entity<VariableMeasurement>().HasIndex(x => new { x.PartId, x.ProcessId, x.CharacteristicId, x.MeasuredAt });
        modelBuilder.Entity<VariableMeasurement>()
            .HasIndex(x => new { x.PartProcessCharacteristicId, x.PortalDailyDate, x.SamplingPhase, x.SamplingStage })
            .IsUnique()
            .HasFilter("[PortalDailyDate] IS NOT NULL");
        modelBuilder.Entity<AttributeMeasurement>().HasKey(x => x.Id);
        modelBuilder.Entity<AttributeMeasurement>().HasIndex(x => new { x.PartId, x.ProcessId, x.CharacteristicId, x.MeasuredAt });
        modelBuilder.Entity<ParticleMeasurement>().ToTable("ParticleMeasurements", table =>
        {
            table.HasComment("Particle monitoring measurements stored in long format.");
            table.HasCheckConstraint("CK_ParticleMeasurements_Count_NonNegative", "[Count] >= 0");
        });
        modelBuilder.Entity<ParticleMeasurement>().HasKey(x => x.Id);
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.UploadBatchId).HasComment("Upload batch that produced this measurement.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.MeasurementTime).HasColumnType("datetime2").HasComment("Measurement timestamp normalized to UTC.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.Location).HasMaxLength(16).HasComment("Particle monitoring location code, such as R1.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.ParticleSize).HasColumnType("decimal(6,3)").HasComment("Particle size in micrometers.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.Count).HasComment("Non-negative particle count.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SamplingVolume).HasColumnType("decimal(18,6)").HasComment("Optional sampling volume.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SamplingVolumeUnit).HasMaxLength(32).HasComment("Unit of the optional sampling volume.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SamplingDurationSeconds).HasColumnType("decimal(18,3)").HasComment("Optional sampling duration in seconds.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.DeviceCode).HasMaxLength(64).HasComment("Optional particle counter device code.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.Remark).HasMaxLength(500).HasComment("Optional measurement remark.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SourceSheet).HasMaxLength(128).HasComment("Original Excel worksheet name.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SourceRow).HasComment("Original Excel row number.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.SourceColumn).HasMaxLength(16).HasComment("Original Excel column label.");
        modelBuilder.Entity<ParticleMeasurement>().Property(x => x.RawValue).HasMaxLength(256).HasComment("Original cell value before normalization.");
        modelBuilder.Entity<ParticleMeasurement>().HasIndex(x => new { x.Location, x.ParticleSize, x.MeasurementTime });
        modelBuilder.Entity<ParticleMeasurement>().HasIndex(x => new { x.ParticleSize, x.MeasurementTime, x.Location });
        modelBuilder.Entity<ParticleMeasurement>().HasIndex(x => x.UploadBatchId);
        modelBuilder.Entity<ParticleMeasurement>()
            .HasIndex(x => new { x.UploadBatchId, x.SourceSheet, x.SourceRow, x.SourceColumn })
            .IsUnique();
        modelBuilder.Entity<SpcCalculationResult>().HasKey(x => x.Id);
        modelBuilder.Entity<SpcCalculationResult>().HasIndex(x => new { x.PartProcessCharacteristicId, x.CalculatedAt });
        modelBuilder.Entity<SpcPointExclusion>().ToTable("SpcPointExclusions", table =>
        {
            table.HasComment("Single chart point exclusion states for SPC control/trend charts.");
        });
        modelBuilder.Entity<SpcPointExclusion>().HasKey(x => x.Id);
        modelBuilder.Entity<SpcPointExclusion>().Property(x => x.PointScope).HasMaxLength(40).HasComment("VariableMeasurement, AttributeMeasurement, or Subgroup.");
        modelBuilder.Entity<SpcPointExclusion>().Property(x => x.PointKey).HasMaxLength(200).HasComment("Stable key for aggregate chart points such as Xbar subgroups.");
        modelBuilder.Entity<SpcPointExclusion>().Property(x => x.State).HasMaxLength(32).HasComment("ExcludedVisible or ExcludedHidden.");
        modelBuilder.Entity<SpcPointExclusion>().Property(x => x.Reason).HasMaxLength(500).HasComment("Optional reason for excluding the chart point.");
        modelBuilder.Entity<SpcPointExclusion>().Property(x => x.IsActive).HasDefaultValue(true).HasComment("Inactive rows are restored points kept for audit.");
        modelBuilder.Entity<SpcPointExclusion>().HasIndex(x => new { x.PointScope, x.VariableMeasurementId, x.IsActive });
        modelBuilder.Entity<SpcPointExclusion>().HasIndex(x => new { x.PointScope, x.AttributeMeasurementId, x.IsActive });
        modelBuilder.Entity<SpcPointExclusion>().HasIndex(x => new { x.PartProcessCharacteristicId, x.PointKey, x.IsActive });

        // Organizational Data Indexes
        modelBuilder.Entity<Plant>().HasIndex(x => x.PlantCode).IsUnique();
        modelBuilder.Entity<Factory>().HasIndex(x => x.FactoryCode).IsUnique();
        modelBuilder.Entity<ProductionLine>().HasIndex(x => x.LineCode).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(x => x.UnitCode).IsUnique();
        modelBuilder.Entity<Shift>().HasIndex(x => x.ShiftCode).IsUnique();
        modelBuilder.Entity<Operator>().HasIndex(x => x.OperatorCode).IsUnique();
        modelBuilder.Entity<Operator>().HasIndex(x => x.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
        modelBuilder.Entity<Operator>().Property(x => x.Role).HasMaxLength(20).HasDefaultValue("Editor");
        modelBuilder.Entity<Operator>().Property(x => x.Username).HasMaxLength(100);
        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Operator>().Property(x => x.PagePermissionsJson).HasColumnType("nvarchar(max)");
        }
        modelBuilder.Entity<SpcReportSchedule>().Property(x => x.ScheduleName).HasMaxLength(100);
        modelBuilder.Entity<SpcReportSchedule>().Property(x => x.Department).HasMaxLength(100);
        modelBuilder.Entity<ChameleonSourceSetting>().HasIndex(x => x.SourceId).IsUnique();
        modelBuilder.Entity<ChameleonSourceSetting>().Property(x => x.SourceId).HasMaxLength(64);
        modelBuilder.Entity<ChameleonSourceSetting>().Property(x => x.DisplayName).HasMaxLength(120);
        modelBuilder.Entity<ChameleonSourceSetting>().Property(x => x.BaseUrl).HasMaxLength(500);
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierCode).IsUnique();
        modelBuilder.Entity<Chemical>().HasIndex(x => x.ChemicalCode).IsUnique();
        modelBuilder.Entity<EquipmentPointMapping>().HasIndex(x => new { x.SourceId, x.EquipmentId, x.ChannelId }).IsUnique();
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.SourceId).HasMaxLength(64);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.EquipmentId).HasMaxLength(128);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.ChannelId).HasMaxLength(128);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.DisplayName).HasMaxLength(200);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.Unit).HasMaxLength(30);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.StatusRole).HasMaxLength(20);
        modelBuilder.Entity<EquipmentPointMapping>().Property(x => x.ActiveWhen).HasMaxLength(20);

        // Traceability Indexes
        modelBuilder.Entity<LotMaster>().HasIndex(x => x.LotNo).IsUnique();
        modelBuilder.Entity<LotSplitHistory>().HasIndex(x => x.TargetLotId).IsUnique();
        modelBuilder.Entity<Tank>().HasIndex(x => x.TankCode).IsUnique();
        modelBuilder.Entity<Slot>().HasIndex(x => x.SlotCode).IsUnique();

        // Foreign Key Constraints
        modelBuilder.Entity<Machine>()
            .HasOne<Process>()
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QualityCharacteristic>()
            .HasOne<ControlChartType>()
            .WithMany()
            .HasForeignKey(x => x.DefaultChartTypeId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne(x => x.Part)
            .WithMany()
            .HasForeignKey(x => x.PartId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne(x => x.Process)
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne(x => x.Machine)
            .WithMany()
            .HasForeignKey(x => x.MachineId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne(x => x.Tank)
            .WithMany()
            .HasForeignKey(x => x.TankId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne(x => x.Characteristic)
            .WithMany()
            .HasForeignKey(x => x.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne<ControlChartType>()
            .WithMany()
            .HasForeignKey(x => x.ChartTypeId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasOne<SpcRuleGroup>()
            .WithMany()
            .HasForeignKey(x => x.RuleGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ControlChartType>()
            .HasOne<ControlChartGroup>()
            .WithMany()
            .HasForeignKey(x => x.ChartGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ControlChartType>()
            .HasOne<SpcRuleGroup>()
            .WithMany()
            .HasForeignKey(x => x.RuleGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SpcRule>()
            .HasOne<SpcRuleGroup>()
            .WithMany()
            .HasForeignKey(x => x.RuleGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChemicalFTableCell>()
            .HasOne(x => x.Version)
            .WithMany()
            .HasForeignKey(x => x.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChemicalFTableReference>()
            .HasOne(x => x.Version)
            .WithMany()
            .HasForeignKey(x => x.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChemicalFTableReference>()
            .HasOne(x => x.PartProcessCharacteristic)
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UploadDetail>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UploadError>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UploadError>()
            .HasOne<UploadDetail>()
            .WithMany()
            .HasForeignKey(x => x.UploadDetailId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<VariableMeasurement>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VariableMeasurement>()
            .HasOne<PartProcessCharacteristic>()
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AttributeMeasurement>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AttributeMeasurement>()
            .HasOne<PartProcessCharacteristic>()
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ParticleMeasurement>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcCalculationResult>()
            .HasOne<UploadBatch>()
            .WithMany()
            .HasForeignKey(x => x.UploadBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcCalculationResult>()
            .HasOne<PartProcessCharacteristic>()
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcCalculationResult>()
            .HasOne<ControlChartType>()
            .WithMany()
            .HasForeignKey(x => x.ChartTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcPointExclusion>()
            .HasOne(x => x.PartProcessCharacteristic)
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcPointExclusion>()
            .HasOne(x => x.VariableMeasurement)
            .WithMany()
            .HasForeignKey(x => x.VariableMeasurementId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcPointExclusion>()
            .HasOne(x => x.AttributeMeasurement)
            .WithMany()
            .HasForeignKey(x => x.AttributeMeasurementId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpcPointExclusion>()
            .HasOne(x => x.MeasurementBatch)
            .WithMany()
            .HasForeignKey(x => x.MeasurementBatchId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LotSplitHistory>()
            .HasOne(x => x.TargetLot)
            .WithMany()
            .HasForeignKey(x => x.TargetLotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LotSplitHistory>()
            .HasOne(x => x.SourceLot)
            .WithMany()
            .HasForeignKey(x => x.SourceLotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LotSlotHistory>()
            .HasOne(x => x.Lot)
            .WithMany()
            .HasForeignKey(x => x.LotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LotSlotHistory>()
            .HasOne(x => x.Slot)
            .WithMany()
            .HasForeignKey(x => x.SlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // ControlLimitSegment Mapping
        modelBuilder.Entity<ControlLimitSegment>().HasKey(x => x.Id);
        modelBuilder.Entity<ControlLimitSegment>().HasIndex(x => new { x.PartProcessCharacteristicId, x.StartDate });
        modelBuilder.Entity<ControlLimitSegment>()
            .HasOne(x => x.PartProcessCharacteristic)
            .WithMany()
            .HasForeignKey(x => x.PartProcessCharacteristicId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global Query Filter for Soft Delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(GenerateIsDeletedFilter(entityType.ClrType));
            }
        }
    }

    private static dynamic GenerateIsDeletedFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var propertyMethod = typeof(EF).GetMethod("Property")!.MakeGenericMethod(typeof(bool));
        var isDeletedProperty = System.Linq.Expressions.Expression.Call(null, propertyMethod, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
        var compare = System.Linq.Expressions.Expression.Equal(isDeletedProperty, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(compare, parameter);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        HandleAuditFields();
        return base.SaveChanges();
    }

    private void HandleAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var now = DateTime.UtcNow;
        var user = "System"; // TODO: Get from IHttpContextAccessor or similar

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.CreatedBy = user;
            }
            else
            {
                entity.UpdatedAt = now;
                entity.UpdatedBy = user;
            }
        }
    }
}
