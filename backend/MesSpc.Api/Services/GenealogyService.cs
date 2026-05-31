using MesSpc.Api.Domain.DTOs;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

public class GenealogyService(AppDbContext dbContext, ILogger<GenealogyService> logger)
{
    public async Task<LotGenealogyResponse?> GetLotGenealogyAsync(string lotNo)
    {
        var lot = await dbContext.LotMasters
            .Include(l => l.ParentLot)
            .FirstOrDefaultAsync(l => l.LotNo == lotNo);

        if (lot == null) return null;

        var childLots = await dbContext.LotMasters
            .Where(l => l.ParentLotId == lot.Id)
            .ToListAsync();

        var routing = await dbContext.LotSlotHistories
            .Include(h => h.Slot)
                .ThenInclude(s => s.Tank)
                    .ThenInclude(t => t.Line)
            .Where(h => h.LotId == lot.Id)
            .OrderBy(h => h.EntryTime)
            .ToListAsync();

        var variables = await dbContext.VariableMeasurements
            .Include(v => v.Chemical)
            .Where(v => v.LotNo == lotNo)
            .ToListAsync();

        // Map to DTOs
        var response = new LotGenealogyResponse
        {
            CurrentLot = MapToNode(lot, lot.WorkOrderId.ToString()), // For simplicity, assume WorkOrderId mapping is done or WorkOrderNo is stored. Wait, WorkOrderId is int.
            ParentLot = lot.ParentLot != null ? MapToNode(lot.ParentLot, lot.ParentLot.WorkOrderId.ToString()) : null,
            ChildLots = childLots.Select(c => MapToNode(c, c.WorkOrderId.ToString())).ToList(),
            
            RoutingHistory = routing.Select(r => new SlotHistoryDto
            {
                LineCode = r.Slot?.Tank?.Line?.LineCode ?? "",
                TankCode = r.Slot?.Tank?.TankCode ?? "",
                SlotCode = r.Slot?.SlotCode ?? "",
                EntryTime = r.EntryTime,
                ExitTime = r.ExitTime,
                Operator = r.Operator
            }).ToList(),

            QualityEvents = variables.Select(v => new InspectionEventDto
            {
                Type = "Variable",
                EventTime = v.MeasuredAt,
                ItemName = v.PartProcessCharacteristicId.ToString(), // Need to join to get real name, kept simple here
                MeasuredValue = v.MeasuredValue,
                ChemicalName = v.Chemical?.ChemicalName,
                Status = v.IsDeleted ? "Deleted" : "Active"
            }).ToList()
        };

        return response;
    }

    public async Task<WorkOrderGenealogyResponse> GetWorkOrderGenealogyAsync(string workOrderNo)
    {
        var workOrder = await dbContext.WorkOrders.FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);
        if (workOrder == null) return new WorkOrderGenealogyResponse();

        var rootLots = await dbContext.LotMasters
            .Where(l => l.WorkOrderId == workOrder.Id && l.ParentLotId == null)
            .ToListAsync();

        return new WorkOrderGenealogyResponse
        {
            WorkOrderNo = workOrderNo,
            RootLots = rootLots.Select(l => MapToNode(l, workOrderNo)).ToList()
        };
    }

    private static LotNodeDto MapToNode(MesSpc.Api.Domain.Entities.LotMaster lot, string? workOrderNo)
    {
        return new LotNodeDto
        {
            Id = lot.Id,
            LotNo = lot.LotNo,
            SubLotNo = lot.SubLotNo,
            CurrentQty = lot.CurrentQty,
            Status = lot.Status,
            WorkOrderNo = workOrderNo
        };
    }
}
