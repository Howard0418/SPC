using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/parts")]
[Route("api/v1/parts")]
public class PartsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Parts.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Part req) { req.UpdatedAt = DateTime.UtcNow; db.Parts.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Part req)
    {
        var x = await db.Parts.FindAsync(id); if (x is null) return NotFound();
        x.PartNo = req.PartNo; x.PartName = req.PartName; x.Specification = req.Specification; x.Customer = req.Customer; x.IsEnabled = req.IsEnabled; x.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Parts.FindAsync(id); if (x is null) return NotFound(); db.Parts.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/processes")]
[Route("api/v1/processes")]
public class ProcessesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Processes.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Process req) { db.Processes.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Process req)
    {
        var x = await db.Processes.FindAsync(id); if (x is null) return NotFound();
        x.ProcessCode = req.ProcessCode; x.ProcessName = req.ProcessName; x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }

    [HttpGet("{id:int}/measurements")]
    public async Task<IActionResult> GetMeasurements(int id, [FromQuery] string type = "variable", [FromQuery] int take = 200)
    {
        var process = await db.Processes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (process is null) return NotFound();

        take = Math.Clamp(take, 1, 1000);
        if (string.Equals(type, "attribute", StringComparison.OrdinalIgnoreCase))
        {
            var rows = await db.AttributeMeasurements
                .AsNoTracking()
                .Where(x => x.ProcessId == id)
                .OrderByDescending(x => x.MeasuredAt)
                .Take(take)
                .Select(x => new
                {
                    dataType = "Attribute",
                    x.Id,
                    x.UploadBatchId,
                    x.PartProcessCharacteristicId,
                    x.PartId,
                    x.ProcessId,
                    x.MachineId,
                    x.CharacteristicId,
                    x.LotNo,
                    x.SampleNo,
                    value = (double?)null,
                    x.InspectedQty,
                    x.DefectQty,
                    x.DefectCount,
                    x.UnitCount,
                    x.MeasuredAt,
                    x.Operator
                })
                .ToListAsync();

            return Ok(new { process, total = await db.AttributeMeasurements.CountAsync(x => x.ProcessId == id), rows });
        }

        var variableRows = await db.VariableMeasurements
            .AsNoTracking()
            .Where(x => x.ProcessId == id)
            .OrderByDescending(x => x.MeasuredAt)
            .Take(take)
            .Select(x => new
            {
                dataType = "Variable",
                x.Id,
                x.UploadBatchId,
                x.PartProcessCharacteristicId,
                x.PartId,
                x.ProcessId,
                x.MachineId,
                x.CharacteristicId,
                x.LotNo,
                x.SerialNo,
                x.SampleNo,
                value = (double?)x.MeasuredValue,
                inspectedQty = (int?)null,
                defectQty = (int?)null,
                defectCount = (int?)null,
                unitCount = (int?)null,
                x.MeasuredAt,
                x.Operator
            })
            .ToListAsync();

        return Ok(new { process, total = await db.VariableMeasurements.CountAsync(x => x.ProcessId == id), rows = variableRows });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await db.Processes.FindAsync(id);
        if (x is null) return NotFound();

        // ── 刪除前檢查所有關聯資料 ──
        var machines = await db.Machines
            .Where(m => m.ProcessId == id)
            .Select(m => new { m.Id, Label = $"生產機台：{m.MachineCode} ({m.MachineName})" })
            .ToListAsync();

        var ppcs = await db.PartProcessCharacteristics
            .Include(p => p.Part)
            .Include(p => p.Characteristic)
            .Where(p => p.ProcessId == id)
            .Select(p => new
            {
                p.Id,
                Label = $"SPC 管制項目：[{(p.ControlScope == "PRODUCT" ? p.Part!.PartNo : p.ControlScope)}] × [{p.Characteristic!.CharacteristicCode}]"
            })
            .ToListAsync();

        var varMeasurements = await db.VariableMeasurements
            .Where(v => v.ProcessId == id)
            .GroupBy(v => v.ProcessId)
            .Select(g => new { Count = g.Count() })
            .FirstOrDefaultAsync();

        var attrMeasurements = await db.AttributeMeasurements
            .Where(a => a.ProcessId == id)
            .GroupBy(a => a.ProcessId)
            .Select(g => new { Count = g.Count() })
            .FirstOrDefaultAsync();

        var alerts = await db.AlertEvents
            .Where(a => a.ProcessId == id)
            .GroupBy(a => a.ProcessId)
            .Select(g => new { Count = g.Count() })
            .FirstOrDefaultAsync();

        var relatedItems = new List<string>();
        relatedItems.AddRange(machines.Select(m => m.Label));
        relatedItems.AddRange(ppcs.Select(p => p.Label));
        if (varMeasurements?.Count > 0)
            relatedItems.Add($"計量型量測記錄：共 {varMeasurements.Count} 筆");
        if (attrMeasurements?.Count > 0)
            relatedItems.Add($"計數型量測記錄：共 {attrMeasurements.Count} 筆");
        if (alerts?.Count > 0)
            relatedItems.Add($"SPC 異常通報記錄：共 {alerts.Count} 筆");

        if (relatedItems.Count > 0)
        {
            return Conflict(new
            {
                message = $"無法刪除工站製程「{x.ProcessCode} ({x.ProcessName})」，因為以下資料正在使用此製程：",
                relatedItems
            });
        }

        db.Processes.Remove(x);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/machines")]
[Route("api/v1/machines")]
public class MachinesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Machines.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Machine req) { db.Machines.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Machine req)
    {
        var x = await db.Machines.FindAsync(id); if (x is null) return NotFound();
        x.MachineCode = req.MachineCode; x.MachineName = req.MachineName; x.ProcessId = req.ProcessId; x.Location = req.Location; x.Status = req.Status; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Machines.FindAsync(id); if (x is null) return NotFound(); db.Machines.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/characteristics")]
[Route("api/v1/characteristics")]
public class QualityCharacteristicsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.QualityCharacteristics.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(QualityCharacteristic req) { db.QualityCharacteristics.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QualityCharacteristic req)
    {
        var x = await db.QualityCharacteristics.FindAsync(id); if (x is null) return NotFound();
        x.CharacteristicCode = req.CharacteristicCode; x.CharacteristicName = req.CharacteristicName; x.DataCategory = req.DataCategory; x.Unit = req.Unit; x.DefaultChartTypeId = req.DefaultChartTypeId; x.IsSpcEnabled = req.IsSpcEnabled; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.QualityCharacteristics.FindAsync(id); if (x is null) return NotFound(); db.QualityCharacteristics.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/part-process-characteristics")]
[Route("api/v1/part-process-characteristics")]
public class PartProcessCharacteristicsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.PartProcessCharacteristics.Include(x => x.Part).Include(x => x.Process).Include(x => x.Characteristic).OrderBy(x => x.Id).ToListAsync());
    [HttpPost]
    public async Task<IActionResult> Create(PartProcessCharacteristic req)
    {
        var validation = ValidateScope(req);
        if (validation is not null) return BadRequest(validation);
        req.RuleGroupId = null;
        db.PartProcessCharacteristics.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PartProcessCharacteristic req)
    {
        var x = await db.PartProcessCharacteristics.FindAsync(id); if (x is null) return NotFound();
        var validation = ValidateScope(req);
        if (validation is not null) return BadRequest(validation);
        x.ControlScope = NormalizeScope(req.ControlScope); x.PartId = x.ControlScope == "PRODUCT" ? req.PartId : null; x.ProcessId = req.ProcessId; x.CharacteristicId = req.CharacteristicId;
        x.USL = req.USL; x.LSL = req.LSL; x.UCL = req.UCL; x.CL = req.CL; x.LCL = req.LCL; x.TargetValue = req.TargetValue;
        x.SampleSize = req.SampleSize; x.ChartTypeId = req.ChartTypeId; x.IsRequired = req.IsRequired; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.PartProcessCharacteristics.FindAsync(id); if (x is null) return NotFound(); db.PartProcessCharacteristics.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private static string NormalizeScope(string? scope)
    {
        var normalized = (scope ?? "PRODUCT").Trim().ToUpperInvariant();
        return normalized is "PROCESS" or "CHEMICAL" or "PRODUCT" ? normalized : "PRODUCT";
    }

    private static string? ValidateScope(PartProcessCharacteristic req)
    {
        req.ControlScope = NormalizeScope(req.ControlScope);
        if (req.ProcessId <= 0) return "工站製程為必填。";
        if (req.CharacteristicId <= 0) return "品質特性為必填。";
        if (req.ControlScope == "PRODUCT")
        {
            if (!req.PartId.HasValue || req.PartId.Value <= 0) return "產品管制項目必須選擇產品料號。";
        }
        else
        {
            req.PartId = null;
        }
        return null;
    }
}

[ApiController]
[Route("api/control-chart-groups")]
[Route("api/v1/control-chart-groups")]
public class ControlChartGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.ControlChartGroups.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(ControlChartGroup req) { db.ControlChartGroups.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlChartGroup req)
    {
        var x = await db.ControlChartGroups.FindAsync(id); if (x is null) return NotFound();
        x.GroupCode = req.GroupCode; x.GroupName = req.GroupName; x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.ControlChartGroups.FindAsync(id); if (x is null) return NotFound(); db.ControlChartGroups.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/control-chart-categories")]
[Route("api/v1/control-chart-categories")]
public class ControlChartCategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.ControlChartCategories.OrderBy(x => x.Id).ToListAsync());
    [HttpPost]
    public async Task<IActionResult> Create(ControlChartCategory req)
    {
        if (await db.ControlChartCategories.IgnoreQueryFilters().AnyAsync(c => c.ChartGroupId == req.ChartGroupId && c.CategoryCode == req.CategoryCode))
        {
            return BadRequest(new { message = $"在大群組下已存在相同類別代號 '{req.CategoryCode}' 的中分類。" });
        }
        db.ControlChartCategories.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlChartCategory req)
    {
        var x = await db.ControlChartCategories.FindAsync(id); if (x is null) return NotFound();
        if (await db.ControlChartCategories.IgnoreQueryFilters().AnyAsync(c => c.Id != id && c.ChartGroupId == req.ChartGroupId && c.CategoryCode == req.CategoryCode))
        {
            return BadRequest(new { message = $"在大群組下已存在相同類別代號 '{req.CategoryCode}' 的中分類。" });
        }
        x.ChartGroupId = req.ChartGroupId; x.CategoryCode = req.CategoryCode; x.CategoryName = req.CategoryName; x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.ControlChartCategories.FindAsync(id); if (x is null) return NotFound(); db.ControlChartCategories.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/control-chart-types")]
[Route("api/v1/control-chart-types")]
public class ControlChartTypesController(AppDbContext db) : ControllerBase
{
    private const string ChartTypeRuleGroupCodePrefix = "CT_RULES_";

    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.ControlChartTypes.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(ControlChartType req) { db.ControlChartTypes.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlChartType req)
    {
        var x = await db.ControlChartTypes.FindAsync(id); if (x is null) return NotFound();
        x.ChartCategoryId = req.ChartCategoryId; x.ChartTypeCode = req.ChartTypeCode; x.ChartTypeName = req.ChartTypeName; x.DataCategory = req.DataCategory; x.RequiredSampleSize = req.RequiredSampleSize; x.RuleGroupId = req.RuleGroupId; x.Description = req.Description; x.FormulaConfigJson = req.FormulaConfigJson; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpGet("{id:int}/rules")]
    public async Task<IActionResult> GetRules(int id)
    {
        var chartType = await db.ControlChartTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (chartType is null) return NotFound();

        var selectedRules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (chartType.RuleGroupId.HasValue)
        {
            selectedRules = await db.SpcRules.AsNoTracking()
                .Where(x => x.RuleGroupId == chartType.RuleGroupId.Value && x.IsEnabled)
                .Select(x => x.RuleCode)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        }

        return Ok(new ChartTypeRulesResponse(
            chartType.Id,
            chartType.RuleGroupId,
            (await GetRuleLibraryAsync()).Select(x => new ChartTypeRuleOption(
                x.RuleCode,
                x.RuleName,
                x.Priority,
                selectedRules.Contains(x.RuleCode))).ToList()));
    }

    [HttpPut("{id:int}/rules")]
    public async Task<IActionResult> UpdateRules(int id, ChartTypeRulesUpdateRequest req)
    {
        var chartType = await db.ControlChartTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (chartType is null) return NotFound();

        var selectedRuleCodes = (req.SelectedRuleCodes ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ruleLibrary = await GetRuleLibraryAsync();
        var validRuleCodes = ruleLibrary.Select(x => x.RuleCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalidRuleCodes = selectedRuleCodes.Where(x => !validRuleCodes.Contains(x)).ToList();
        if (invalidRuleCodes.Count > 0)
        {
            return BadRequest(new { message = "包含不支援的管制規則。", invalidRuleCodes });
        }

        if (selectedRuleCodes.Count == 0)
        {
            chartType.RuleGroupId = null;
            await db.SaveChangesAsync();
            return await GetRules(id);
        }

        var ruleGroupCode = $"{ChartTypeRuleGroupCodePrefix}{chartType.Id}";
        var ruleGroup = await db.SpcRuleGroups.FirstOrDefaultAsync(x => x.RuleGroupCode == ruleGroupCode);
        if (ruleGroup is null)
        {
            ruleGroup = new SpcRuleGroup
            {
                RuleGroupCode = ruleGroupCode,
                RuleGroupName = $"{chartType.ChartTypeName} 管制規則",
                Description = $"管制圖小分類 {chartType.ChartTypeCode} 專用的 8 大管制規則勾選設定。",
                IsEnabled = true
            };
            db.SpcRuleGroups.Add(ruleGroup);
            await db.SaveChangesAsync();
        }
        else
        {
            ruleGroup.RuleGroupName = $"{chartType.ChartTypeName} 管制規則";
            ruleGroup.Description = $"管制圖小分類 {chartType.ChartTypeCode} 專用的 8 大管制規則勾選設定。";
            ruleGroup.IsEnabled = true;
        }

        var existingRules = await db.SpcRules.Where(x => x.RuleGroupId == ruleGroup.Id).ToListAsync();
        foreach (var template in ruleLibrary)
        {
            var rule = existingRules.FirstOrDefault(x => x.RuleCode == template.RuleCode);
            if (rule is null)
            {
                db.SpcRules.Add(new SpcRule
                {
                    RuleGroupId = ruleGroup.Id,
                    RuleCode = template.RuleCode,
                    RuleName = template.RuleName,
                    RuleConfigJson = template.RuleConfigJson,
                    Priority = template.Priority,
                    IsEnabled = selectedRuleCodes.Contains(template.RuleCode)
                });
                continue;
            }

            rule.RuleName = template.RuleName;
            rule.RuleConfigJson = template.RuleConfigJson;
            rule.Priority = template.Priority;
            rule.IsEnabled = selectedRuleCodes.Contains(template.RuleCode);
        }

        chartType.RuleGroupId = ruleGroup.Id;
        await db.SaveChangesAsync();

        return await GetRules(id);
    }

    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.ControlChartTypes.FindAsync(id); if (x is null) return NotFound(); db.ControlChartTypes.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private async Task<List<SpcRuleTemplate>> GetRuleLibraryAsync()
    {
        var rules = await db.SpcRules.AsNoTracking()
            .Where(x => x.IsEnabled)
            .Join(db.SpcRuleGroups.AsNoTracking().Where(g => g.IsEnabled && !g.RuleGroupCode.StartsWith(ChartTypeRuleGroupCodePrefix)),
                rule => rule.RuleGroupId,
                group => group.Id,
                (rule, group) => rule)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return rules
            .GroupBy(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(x => new SpcRuleTemplate(x.RuleCode, x.RuleName, x.Priority, x.RuleConfigJson))
            .ToList();
    }

    private sealed record SpcRuleTemplate(string RuleCode, string RuleName, int Priority, string? RuleConfigJson);
    public sealed record ChartTypeRuleOption(string RuleCode, string RuleName, int Priority, bool IsSelected);
    public sealed record ChartTypeRulesResponse(int ChartTypeId, int? RuleGroupId, List<ChartTypeRuleOption> Rules);
    public sealed record ChartTypeRulesUpdateRequest(List<string>? SelectedRuleCodes);
}

[ApiController]
[Route("api/operators")]
[Route("api/v1/operators")]
public class OperatorsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Operators.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(Operator req) { db.Operators.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Operator req)
    {
        var x = await db.Operators.FindAsync(id); if (x is null) return NotFound();
        x.OperatorCode = req.OperatorCode; x.OperatorName = req.OperatorName; x.Department = req.Department; x.Email = req.Email; x.IsActive = req.IsActive;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Operators.FindAsync(id); if (x is null) return NotFound(); db.Operators.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/spc-rule-groups")]
[Route("api/v1/spc-rule-groups")]
public class SpcRuleGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.SpcRuleGroups.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(SpcRuleGroup req) { db.SpcRuleGroups.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SpcRuleGroup req)
    {
        var x = await db.SpcRuleGroups.FindAsync(id); if (x is null) return NotFound();
        x.RuleGroupCode = req.RuleGroupCode; x.RuleGroupName = req.RuleGroupName; x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.SpcRuleGroups.FindAsync(id); if (x is null) return NotFound(); db.SpcRuleGroups.Remove(x); await db.SaveChangesAsync(); return NoContent(); }
}

[ApiController]
[Route("api/spc-rules")]
[Route("api/v1/spc-rules")]
public class SpcRulesController(AppDbContext db) : ControllerBase
{
    private const string DefaultRuleGroupCode = "WE";
    private const string ChartTypeRuleGroupCodePrefix = "CT_RULES_";

    [HttpGet] 
    public async Task<IActionResult> Get([FromQuery] int? groupId, [FromQuery] bool libraryOnly = false) 
    {
        var q = db.SpcRules.AsQueryable();
        if (groupId.HasValue) q = q.Where(x => x.RuleGroupId == groupId.Value);
        if (libraryOnly)
        {
            var libraryRules = await q.Join(db.SpcRuleGroups.Where(g => g.IsEnabled && !g.RuleGroupCode.StartsWith(ChartTypeRuleGroupCodePrefix)),
                rule => rule.RuleGroupId,
                group => group.Id,
                (rule, group) => rule)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return Ok(libraryRules
                .GroupBy(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList());
        }
        return Ok(await q.OrderBy(x => x.Priority).ThenBy(x => x.Id).ToListAsync());
    }
    
    [HttpPost] 
    public async Task<IActionResult> Create(SpcRule req) 
    { 
        if (req.RuleGroupId <= 0)
        {
            req.RuleGroupId = await EnsureDefaultRuleGroupIdAsync();
        }
        db.SpcRules.Add(req); 
        await db.SaveChangesAsync(); 
        return Ok(req); 
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SpcRule req)
    {
        var x = await db.SpcRules.FindAsync(id); 
        if (x is null) return NotFound();
        x.RuleGroupId = req.RuleGroupId;
        x.RuleCode = req.RuleCode;
        x.RuleName = req.RuleName;
        x.RuleConfigJson = req.RuleConfigJson;
        x.Priority = req.Priority;
        x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); 
        return Ok(x);
    }
    
    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> Delete(int id) 
    { 
        var x = await db.SpcRules.FindAsync(id); 
        if (x is null) return NotFound(); 
        db.SpcRules.Remove(x); 
        await db.SaveChangesAsync(); 
        return NoContent(); 
    }

    private async Task<int> EnsureDefaultRuleGroupIdAsync()
    {
        var group = await db.SpcRuleGroups.FirstOrDefaultAsync(x => x.RuleGroupCode == DefaultRuleGroupCode);
        if (group is not null) return group.Id;

        group = new SpcRuleGroup
        {
            RuleGroupCode = DefaultRuleGroupCode,
            RuleGroupName = "SPC 異常檢驗規則庫",
            Description = "系統預設規則庫；管制圖小分類可從此規則庫勾選要套用的異常檢驗規則。",
            IsEnabled = true
        };
        db.SpcRuleGroups.Add(group);
        await db.SaveChangesAsync();
        return group.Id;
    }
}
