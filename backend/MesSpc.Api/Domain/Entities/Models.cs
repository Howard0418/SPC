using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MesSpc.Api.Domain.Enums;

namespace MesSpc.Api.Domain.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

public abstract class BaseEntity<T> : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T Id { get; set; } = default!;
}

// --- Enterprise Master Data ---

public class Plant : BaseEntity<int>
{
    public string PlantCode { get; set; } = string.Empty;
    public string PlantName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Factory : BaseEntity<int>
{
    public int PlantId { get; set; }
    public string FactoryCode { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductionLine : BaseEntity<int>
{
    public int FactoryId { get; set; }
    public string LineCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Unit : BaseEntity<int>
{
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Shift : BaseEntity<int>
{
    public string ShiftCode { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Operator : BaseEntity<int>
{
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = "Editor";
    public bool IsActive { get; set; } = true;
}

public class Customer : BaseEntity<int>
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Supplier : BaseEntity<int>
{
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Chemical : BaseEntity<int>
{
    public string ChemicalCode { get; set; } = string.Empty;
    public string ChemicalName { get; set; } = string.Empty;
    public string? ChemicalType { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MesSyncMessage : BaseEntity
{
    [Key]
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public string MessageType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = "Pending";
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

// --- Existing SPC Master Data (Refactored) ---

public class Product : BaseEntity<int>
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Station : BaseEntity<int>
{
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class InspectionItem : BaseEntity<int>
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public MesSpc.Api.Domain.Enums.DataType DataType { get; set; } = MesSpc.Api.Domain.Enums.DataType.Numeric;
    public string? Unit { get; set; }
    public double? Usl { get; set; }
    public double? Lsl { get; set; }
    public double? Ucl { get; set; }
    public double? Lcl { get; set; }
    public double? TargetValue { get; set; }
    public bool IsSpcEnabled { get; set; } = true;
}

public class ProductStationItem : BaseEntity<int>
{
    public int ProductId { get; set; }
    public int StationId { get; set; }
    public int InspectionItemId { get; set; }
    public int SampleSize { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class MeasurementBatch : BaseEntity<int>
{
    public string BatchNo { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int StationId { get; set; }
    public int? WorkOrderId { get; set; }
    public int? StationOperationSessionId { get; set; }
    public string? LotNo { get; set; }
    public string? SerialNo { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public string? OperatorName { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public bool IsExcluded { get; set; } = false;
    public List<MeasurementValue> Values { get; set; } = [];
}

public class MeasurementValue : BaseEntity<int>
{
    public int BatchId { get; set; }
    public int InspectionItemId { get; set; }
    public int SampleNo { get; set; } = 1;
    public double? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public bool? ValueBool { get; set; }
}

public class FormulaDefinition : BaseEntity<int>
{
    public string FormulaCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class AlertEvent : BaseEntity<int>
{
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int PartId { get; set; }
    public int ProcessId { get; set; }
    public int CharacteristicId { get; set; }
    public double? ActualValue { get; set; }
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? UploadBatchId { get; set; }
    public int? MeasurementBatchId { get; set; }
    public long? VariableMeasurementId { get; set; }
    public long? AttributeMeasurementId { get; set; }
    public bool IsAcknowledged { get; set; } = false;
    public string Status { get; set; } = "Open";
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? ResponsibleUser { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class WorkOrder : BaseEntity<int>
{
    public string WorkOrderNo { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int PlannedQty { get; set; }
    public int ActualQty { get; set; } = 0;
    public string Status { get; set; } = "Planned";
    public DateTime? PlannedStartTime { get; set; }
    public DateTime? PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
}

public class StationOperationSession : BaseEntity<int>
{
    public int WorkOrderId { get; set; }
    public int StationId { get; set; }
    public string? LotNo { get; set; }
    public string? SerialNo { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}

public class Part : BaseEntity<int>
{
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string? Customer { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class Process : BaseEntity<int>
{
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class Machine : BaseEntity<int>
{
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class QualityCharacteristic : BaseEntity<int>
{
    public string CharacteristicCode { get; set; } = string.Empty;
    public string CharacteristicName { get; set; } = string.Empty;
    public string DataCategory { get; set; } = "Variable";
    public string? Unit { get; set; }
    public int? DefaultChartTypeId { get; set; }
    public bool IsSpcEnabled { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}

public class PartProcessCharacteristic : BaseEntity<int>
{
    public string ControlScope { get; set; } = "PRODUCT";
    public int? PartId { get; set; }
    public int ProcessId { get; set; }
    public int? MachineId { get; set; }
    public int? TankId { get; set; }
    public int CharacteristicId { get; set; }
    public string? Unit { get; set; }
    public double? USL { get; set; }
    public double? LSL { get; set; }
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    public double? TargetValue { get; set; }
    public int SampleSize { get; set; } = 1;
    public int? ChartTypeId { get; set; }
    public string? FormulaConfigJson { get; set; }
    public int? RuleGroupId { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    [ForeignKey("PartId")]
    public virtual Part? Part { get; set; }
    [ForeignKey("ProcessId")]
    public virtual Process? Process { get; set; }
    [ForeignKey("MachineId")]
    public virtual Machine? Machine { get; set; }
    [ForeignKey("TankId")]
    public virtual Tank? Tank { get; set; }
    [ForeignKey("CharacteristicId")]
    public virtual QualityCharacteristic? Characteristic { get; set; }
}

public class ControlLimitSegment : BaseEntity<int>
{
    public int PartProcessCharacteristicId { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    
    public string? Note { get; set; }
    
    [ForeignKey("PartProcessCharacteristicId")]
    public virtual PartProcessCharacteristic? PartProcessCharacteristic { get; set; }
}

public class ControlChartGroup : BaseEntity<int>
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = "CONTROL_CHART";
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ControlChartCategory : BaseEntity<int>
{
    public int ChartGroupId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ControlChartType : BaseEntity<int>
{
    public int ChartCategoryId { get; set; }
    public string ChartTypeCode { get; set; } = string.Empty;
    public string ChartTypeName { get; set; } = string.Empty;
    public string DataCategory { get; set; } = "Variable";
    public int? RequiredSampleSize { get; set; }
    public int? RuleGroupId { get; set; }
    public string? Description { get; set; }
    public string? FormulaConfigJson { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UploadBatch : BaseEntity
{
    [Key]
    public Guid UploadBatchId { get; set; } = Guid.NewGuid();
    public string UploadType { get; set; } = "Variable";
    public string SourceType { get; set; } = "Api";
    public string ImportStatus { get; set; } = "Uploaded";
    public string? OriginalFileName { get; set; }
    public string? FileHash { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public bool IsExcluded { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
}

public class UploadDetail : BaseEntity<long>
{
    public Guid UploadBatchId { get; set; }
    public int RowNo { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
}

public class UploadError : BaseEntity<long>
{
    public Guid UploadBatchId { get; set; }
    public long? UploadDetailId { get; set; }
    public int? RowNo { get; set; }
    public string? FieldName { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public class VariableMeasurement : BaseEntity<long>
{
    public Guid UploadBatchId { get; set; }
    public int PartId { get; set; }
    public int ProcessId { get; set; }
    public int MachineId { get; set; }
    public int CharacteristicId { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? LotNo { get; set; }
    public string? SubLotNo { get; set; }
    public string? ParentLotNo { get; set; }
    public string? SerialNo { get; set; }
    public int? LineId { get; set; }
    public int? TankId { get; set; }
    public int? SlotId { get; set; }
    public int SampleNo { get; set; }
    public double MeasuredValue { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public string? Operator { get; set; }
    
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public string? SourceReference { get; set; }
    public SideCode SideCode { get; set; } = SideCode.None;
    public int? ChemicalId { get; set; }
    
    [ForeignKey("ChemicalId")]
    public virtual Chemical? Chemical { get; set; }
    [ForeignKey("LineId")]
    public virtual ProductionLine? Line { get; set; }
    [ForeignKey("TankId")]
    public virtual Tank? Tank { get; set; }
    [ForeignKey("SlotId")]
    public virtual Slot? Slot { get; set; }
}

public class AttributeMeasurement : BaseEntity<long>
{
    public Guid UploadBatchId { get; set; }
    public int PartId { get; set; }
    public int ProcessId { get; set; }
    public int MachineId { get; set; }
    public int CharacteristicId { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? LotNo { get; set; }
    public string? SubLotNo { get; set; }
    public string? ParentLotNo { get; set; }
    public int? LineId { get; set; }
    public int? TankId { get; set; }
    public int? SlotId { get; set; }
    public int SampleNo { get; set; }
    public int? InspectedQty { get; set; }
    public int? DefectQty { get; set; }
    public int? DefectCount { get; set; }
    public int? UnitCount { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public string? Operator { get; set; }
    
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public string? SourceReference { get; set; }
    public SideCode SideCode { get; set; } = SideCode.None;
    public int? ChemicalId { get; set; }
    
    [ForeignKey("ChemicalId")]
    public virtual Chemical? Chemical { get; set; }
    [ForeignKey("LineId")]
    public virtual ProductionLine? Line { get; set; }
    [ForeignKey("TankId")]
    public virtual Tank? Tank { get; set; }
    [ForeignKey("SlotId")]
    public virtual Slot? Slot { get; set; }
}

public class SpcRuleGroup : BaseEntity<int>
{
    public string RuleGroupCode { get; set; } = string.Empty;
    public string RuleGroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class SpcRule : BaseEntity<int>
{
    public int RuleGroupId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string? RuleConfigJson { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

public class SpcCalculationResult : BaseEntity<long>
{
    public Guid UploadBatchId { get; set; }
    public string DataCategory { get; set; } = "Variable";
    public long? VariableMeasurementId { get; set; }
    public long? AttributeMeasurementId { get; set; }
    public int PartProcessCharacteristicId { get; set; }
    public int ChartTypeId { get; set; }
    public int? RuleGroupId { get; set; }
    public string? StatisticName { get; set; }
    public double? StatisticValue { get; set; }
    public double? USL { get; set; }
    public double? LSL { get; set; }
    public double? UCL { get; set; }
    public double? CL { get; set; }
    public double? LCL { get; set; }
    public bool IsOutOfSpec { get; set; }
    public bool IsOutOfControl { get; set; }
    public string? ViolatedRulesJson { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

// --- Traceability ---

public class LotMaster : BaseEntity<long>
{
    public string LotNo { get; set; } = string.Empty;
    public string? SubLotNo { get; set; }
    public long? ParentLotId { get; set; }
    public int? WorkOrderId { get; set; }
    public int? PartId { get; set; }
    public int CurrentQty { get; set; }
    public string Status { get; set; } = "Active";

    [ForeignKey("ParentLotId")]
    public virtual LotMaster? ParentLot { get; set; }
}

public class LotSplitHistory : BaseEntity<long>
{
    public long SourceLotId { get; set; }
    public long TargetLotId { get; set; }
    public DateTime SplitTime { get; set; } = DateTime.UtcNow;
    public int SplitQty { get; set; }
    public string? SplitReason { get; set; }
    public string? SplitOperator { get; set; }

    [ForeignKey("SourceLotId")]
    public virtual LotMaster? SourceLot { get; set; }

    [ForeignKey("TargetLotId")]
    public virtual LotMaster? TargetLot { get; set; }
}

public class Tank : BaseEntity<int>
{
    public int LineId { get; set; }
    public string TankCode { get; set; } = string.Empty;
    public string TankName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey("LineId")]
    public virtual ProductionLine? Line { get; set; }
}

public class Slot : BaseEntity<int>
{
    public int TankId { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey("TankId")]
    public virtual Tank? Tank { get; set; }
}

public class SlotParameter : BaseEntity<int>
{
    public int SlotId { get; set; }
    public string ParameterCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public double? TargetValue { get; set; }
    public double? Usl { get; set; }
    public double? Lsl { get; set; }

    [ForeignKey("SlotId")]
    public virtual Slot? Slot { get; set; }
}

public class LotSlotHistory : BaseEntity<long>
{
    public long LotId { get; set; }
    public int SlotId { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string? Operator { get; set; }

    [ForeignKey("LotId")]
    public virtual LotMaster? Lot { get; set; }

    [ForeignKey("SlotId")]
    public virtual Slot? Slot { get; set; }
}
