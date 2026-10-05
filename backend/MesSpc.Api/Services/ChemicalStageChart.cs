using MesSpc.Api.Domain.Entities;
namespace MesSpc.Api.Services;
/// <summary>One stable sequence per chemical mapping; retain all shifts and stages.</summary>
public static class ChemicalStageChart
{
    public static List<VariableMeasurement> Order(IEnumerable<VariableMeasurement> rows)=>rows
        .OrderBy(x=>(x.PortalDailyDate??x.MeasuredAt).Date)
        .ThenBy(x=>x.SamplingStage switch{"OPEN"=>0,"CLOSE"=>1,_=>2})
        .ThenBy(x=>x.SamplingPhase switch{"OPEN"=>0,"MIDDLE"=>1,_=>2})
        .ThenBy(x=>x.MeasuredAt).ThenBy(x=>x.Id).ToList();
}
