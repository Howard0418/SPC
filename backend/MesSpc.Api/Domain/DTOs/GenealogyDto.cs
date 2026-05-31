namespace MesSpc.Api.Domain.DTOs;

public class LotNodeDto
{
    public long Id { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public string? SubLotNo { get; set; }
    public int CurrentQty { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WorkOrderNo { get; set; }
}

public class SlotHistoryDto
{
    public string LineCode { get; set; } = string.Empty;
    public string TankCode { get; set; } = string.Empty;
    public string SlotCode { get; set; } = string.Empty;
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string? Operator { get; set; }
}

public class InspectionEventDto
{
    public string Type { get; set; } = string.Empty; // Variable, Attribute, SpcAlert
    public DateTime EventTime { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public double? MeasuredValue { get; set; }
    public string? ChemicalName { get; set; }
    public string? Status { get; set; }
}

public class LotGenealogyResponse
{
    public LotNodeDto CurrentLot { get; set; } = new();
    public LotNodeDto? ParentLot { get; set; }
    public List<LotNodeDto> ChildLots { get; set; } = new();
    public List<SlotHistoryDto> RoutingHistory { get; set; } = new();
    public List<InspectionEventDto> QualityEvents { get; set; } = new();
}

public class WorkOrderGenealogyResponse
{
    public string WorkOrderNo { get; set; } = string.Empty;
    public List<LotNodeDto> RootLots { get; set; } = new();
}
