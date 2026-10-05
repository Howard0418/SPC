using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MesSpc.Api.Controllers;
[ApiController][Route("api/equipment-monitor")]
public class EquipmentMonitorController(AppDbContext db, ChameleonStatusService chameleon) : ControllerBase
{
 [HttpGet("dashboard")] public async Task<IActionResult> Dashboard(CancellationToken ct)
 {
  var mappings=await db.EquipmentPointMappings.AsNoTracking().Where(x=>x.IsPinned&&x.IsEnabled).OrderBy(x=>x.SortOrder).ThenBy(x=>x.DisplayName).ToListAsync(ct);
  var groups=mappings.GroupBy(x=>new{x.SourceId,x.EquipmentId}); var rows=new List<object>();
  foreach(var g in groups){ var read=await chameleon.GetLatestPointsAsync(g.Key.SourceId,g.Key.EquipmentId,ct); var values=read.Points.ToDictionary(x=>x.ChannelId); foreach(var m in g){ if(!values.TryGetValue(m.ChannelId,out var p))continue; var numeric=double.TryParse(p.Value,out var n)?n:(double?)null; var state=numeric is null?"unknown":m.WarningLow.HasValue&&numeric<m.WarningLow||m.WarningHigh.HasValue&&numeric>m.WarningHigh?"alert":m.WarningLow.HasValue||m.WarningHigh.HasValue?"normal":"unlimited"; rows.Add(new{m.SourceId,m.EquipmentId,m.ChannelId,name=m.DisplayName??p.ChannelName??m.ChannelId,p.Value,p.ValueType,p.Category,m.Unit,m.WarningLow,m.WarningHigh,state,read.LastUpdatedAt}); }}
  return Ok(rows);
 }
}
