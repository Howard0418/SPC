using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services;

/// <summary>Reads complete, date-filtered graphs without creating alerts or persisting calculations.</summary>
public sealed class OverallStatisticsService(AppDbContext db, SpcService spc)
{
    public async Task<OverallSummary> GetAsync(DateTime start, DateTime end, CancellationToken ct = default)
    {
        var enabled = db.PartProcessCharacteristics.AsNoTracking().Where(x=>x.IsEnabled);
        var count = await enabled.CountAsync(ct);
        var disabled = await db.PartProcessCharacteristics.CountAsync(x=>!x.IsEnabled,ct);
        var until = end.Date.AddDays(1);
        var variables = db.VariableMeasurements.AsNoTracking().Where(x=>x.MeasuredAt>=start.Date && x.MeasuredAt<until
            && enabled.Any(p=>p.Id==x.PartProcessCharacteristicId));
        var attributes = db.AttributeMeasurements.AsNoTracking().Where(x=>x.MeasuredAt>=start.Date && x.MeasuredAt<until
            && enabled.Any(p=>p.Id==x.PartProcessCharacteristicId));
        var ids = await variables.Select(x=>x.PartProcessCharacteristicId)
            .Union(attributes.Select(x=>x.PartProcessCharacteristicId)).OrderBy(x=>x).ToListAsync(ct);
        var charts = new List<OverallChart>();
        var unanalyzed = 0;
        foreach(var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            var chart = await spc.GetInteractiveChartAsync(id,null,start.Date,end.Date,ct:ct,includeAllPoints:true);
            if(chart is not null) charts.Add(new(id,chart));
            else unanalyzed++;
        }
        var alerts = await db.AlertEvents.AsNoTracking().Where(a =>
            (a.VariableMeasurementId.HasValue && variables.Any(v=>v.Id==a.VariableMeasurementId.Value)) ||
            (a.AttributeMeasurementId.HasValue && attributes.Any(v=>v.Id==a.AttributeMeasurementId.Value))).ToListAsync(ct);
        return OverallStatistics.Calculate(count,disabled,charts,alerts) with { UnanalyzedItemCount = unanalyzed };
    }
}
