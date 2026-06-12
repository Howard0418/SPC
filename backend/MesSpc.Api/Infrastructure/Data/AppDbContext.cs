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
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<StationOperationSession> StationOperationSessions => Set<StationOperationSession>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<QualityCharacteristic> QualityCharacteristics => Set<QualityCharacteristic>();
    public DbSet<PartProcessCharacteristic> PartProcessCharacteristics => Set<PartProcessCharacteristic>();
    public DbSet<ControlChartGroup> ControlChartGroups => Set<ControlChartGroup>();
    public DbSet<ControlChartCategory> ControlChartCategories => Set<ControlChartCategory>();
    public DbSet<ControlChartType> ControlChartTypes => Set<ControlChartType>();
    public DbSet<UploadBatch> UploadBatches => Set<UploadBatch>();
    public DbSet<UploadDetail> UploadDetails => Set<UploadDetail>();
    public DbSet<UploadError> UploadErrors => Set<UploadError>();
    public DbSet<VariableMeasurement> VariableMeasurements => Set<VariableMeasurement>();
    public DbSet<AttributeMeasurement> AttributeMeasurements => Set<AttributeMeasurement>();
    public DbSet<SpcRuleGroup> SpcRuleGroups => Set<SpcRuleGroup>();
    public DbSet<SpcRule> SpcRules => Set<SpcRule>();
    public DbSet<SpcCalculationResult> SpcCalculationResults => Set<SpcCalculationResult>();

    // New Organizational DB Sets
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Chemical> Chemicals => Set<Chemical>();
    public DbSet<MesSyncMessage> MesSyncMessages => Set<MesSyncMessage>();
    public DbSet<LotMaster> LotMasters => Set<LotMaster>();
    public DbSet<LotSplitHistory> LotSplitHistories => Set<LotSplitHistory>();
    public DbSet<Tank> Tanks => Set<Tank>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<SlotParameter> SlotParameters => Set<SlotParameter>();
    public DbSet<LotSlotHistory> LotSlotHistories => Set<LotSlotHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasIndex(x => x.ProductCode).IsUnique();
        modelBuilder.Entity<Station>().HasIndex(x => x.StationCode).IsUnique();
        modelBuilder.Entity<InspectionItem>().HasIndex(x => x.ItemCode).IsUnique();
        modelBuilder.Entity<FormulaDefinition>().HasIndex(x => x.FormulaCode).IsUnique();
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
        modelBuilder.Entity<PartProcessCharacteristic>().HasIndex(x => new { x.ControlScope, x.PartId, x.ProcessId, x.CharacteristicId }).IsUnique();
        modelBuilder.Entity<PartProcessCharacteristic>()
            .HasIndex(x => new { x.ControlScope, x.ProcessId, x.CharacteristicId })
            .IsUnique()
            .HasFilter("[PartId] IS NULL");
        modelBuilder.Entity<ControlChartGroup>().HasKey(x => x.Id);
        modelBuilder.Entity<ControlChartGroup>().HasIndex(x => x.GroupCode).IsUnique();
        modelBuilder.Entity<ControlChartCategory>().HasKey(x => x.Id);
        modelBuilder.Entity<ControlChartCategory>().HasIndex(x => new { x.ChartGroupId, x.CategoryCode }).IsUnique();
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
        modelBuilder.Entity<VariableMeasurement>().HasIndex(x => new { x.PartId, x.ProcessId, x.CharacteristicId, x.MeasuredAt });
        modelBuilder.Entity<AttributeMeasurement>().HasKey(x => x.Id);
        modelBuilder.Entity<AttributeMeasurement>().HasIndex(x => new { x.PartId, x.ProcessId, x.CharacteristicId, x.MeasuredAt });
        modelBuilder.Entity<SpcCalculationResult>().HasKey(x => x.Id);
        modelBuilder.Entity<SpcCalculationResult>().HasIndex(x => new { x.PartProcessCharacteristicId, x.CalculatedAt });

        // Organizational Data Indexes
        modelBuilder.Entity<Plant>().HasIndex(x => x.PlantCode).IsUnique();
        modelBuilder.Entity<Factory>().HasIndex(x => x.FactoryCode).IsUnique();
        modelBuilder.Entity<ProductionLine>().HasIndex(x => x.LineCode).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(x => x.UnitCode).IsUnique();
        modelBuilder.Entity<Shift>().HasIndex(x => x.ShiftCode).IsUnique();
        modelBuilder.Entity<Operator>().HasIndex(x => x.OperatorCode).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierCode).IsUnique();
        modelBuilder.Entity<Chemical>().HasIndex(x => x.ChemicalCode).IsUnique();

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
        modelBuilder.Entity<ControlChartCategory>()
            .HasOne<ControlChartGroup>()
            .WithMany()
            .HasForeignKey(x => x.ChartGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ControlChartType>()
            .HasOne<ControlChartCategory>()
            .WithMany()
            .HasForeignKey(x => x.ChartCategoryId)
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
