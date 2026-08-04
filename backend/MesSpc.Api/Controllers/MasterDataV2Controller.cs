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
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? controlScope = null, [FromQuery] bool? configuredOnly = null)
    {
        var query = db.Processes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(controlScope) && !string.Equals(controlScope, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedScope = controlScope.Trim().ToUpperInvariant();
            query = query.Where(x => x.ControlScope == normalizedScope);
            if (configuredOnly == true)
            {
                var processIds = await db.PartProcessCharacteristics
                    .Where(x => x.ControlScope == normalizedScope)
                    .Select(x => x.ProcessId)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(x => processIds.Contains(x.Id));
            }
        }
        return Ok(await query.OrderBy(x => x.Id).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Create(Process req)
    {
        req.ControlScope = NormalizeControlScope(req.ControlScope);
        db.Processes.Add(req); await db.SaveChangesAsync(); return Ok(req);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Process req)
    {
        var x = await db.Processes.FindAsync(id); if (x is null) return NotFound();
        x.ProcessCode = req.ProcessCode; x.ProcessName = req.ProcessName; x.ControlScope = NormalizeControlScope(req.ControlScope); x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }

    private static string NormalizeControlScope(string? scope)
    {
        var normalized = (scope ?? "PRODUCT").Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "PRODUCT" : normalized;
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
                x.Operator,
                x.RecheckValue,
                x.AdjustAction,
                x.AdjustAmount
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
    [HttpGet("{id:int}/tanks")]
    public async Task<IActionResult> GetTanks(int id)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound();
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == machine.MachineCode);
        if (line is null) return Ok(Array.Empty<Tank>());
        return Ok(await db.Tanks.Where(x => x.LineId == line.Id).OrderBy(x => x.TankCode).ToListAsync());
    }

    [HttpPost("{id:int}/tanks")]
    public async Task<IActionResult> CreateTank(int id, MachineTankRequest req)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound("找不到指定機台。");
        if (string.IsNullOrWhiteSpace(req.TankCode) && string.IsNullOrWhiteSpace(req.TankName))
            return BadRequest("槽體／槽位代碼或名稱至少填寫一項。");
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == machine.MachineCode);
        var existing = line is null ? [] : await db.Tanks.Where(x => x.LineId == line.Id)
            .Select(x => new MachineTankRequest { Id = x.Id, TankCode = x.TankCode, TankName = x.TankName, IsActive = x.IsActive }).ToListAsync();
        existing.Add(req);
        await SyncMachineTanksAsync(machine, existing);
        line = await db.ProductionLines.FirstAsync(x => x.LineCode == machine.MachineCode);
        var cleanCode = string.IsNullOrWhiteSpace(req.TankCode) ? req.TankName!.Trim() : req.TankCode.Trim();
        var fullCode = cleanCode.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase) ? cleanCode : $"{machine.MachineCode}-{cleanCode}";
        return Ok(await db.Tanks.FirstAsync(x => x.LineId == line.Id && x.TankCode == fullCode));
    }

    [HttpPost] public async Task<IActionResult> Create(MachineSaveRequest req)
    {
        var machine = ToMachine(req);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();
        await SyncMachineTanksAsync(machine, req.Tanks);
        return Ok(machine);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MachineSaveRequest req)
    {
        var x = await db.Machines.FindAsync(id); if (x is null) return NotFound();
        var oldCode = x.MachineCode;
        x.MachineCode = req.MachineCode; x.MachineName = req.MachineName; x.ProcessId = req.ProcessId; x.Location = req.Location; x.Status = req.Status; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync();
        await SyncMachineTanksAsync(x, req.Tanks, oldCode);
        return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Machines.FindAsync(id); if (x is null) return NotFound(); db.Machines.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private static Machine ToMachine(MachineSaveRequest req) => new()
    {
        MachineCode = req.MachineCode,
        MachineName = req.MachineName,
        ProcessId = req.ProcessId,
        Location = req.Location,
        Status = req.Status,
        IsEnabled = req.IsEnabled
    };

    private async Task SyncMachineTanksAsync(Machine machine, List<MachineTankRequest>? tanks, string? oldMachineCode = null)
    {
        tanks ??= [];

        var searchCode = oldMachineCode ?? machine.MachineCode;
        var line = await db.ProductionLines.FirstOrDefaultAsync(x => x.LineCode == searchCode);
        if (line is null)
        {
            var factory = await db.Factories.FirstOrDefaultAsync();
            if (factory is null)
            {
                var plant = await db.Plants.FirstOrDefaultAsync();
                if (plant is null)
                {
                    plant = new Plant { PlantCode = "PLT-01", PlantName = "Main Plant" };
                    db.Plants.Add(plant);
                    await db.SaveChangesAsync();
                }

                factory = new Factory { FactoryCode = "FAC-01", FactoryName = "Main Factory", PlantId = plant.Id };
                db.Factories.Add(factory);
                await db.SaveChangesAsync();
            }

            line = new ProductionLine
            {
                LineCode = machine.MachineCode,
                LineName = machine.MachineName,
                FactoryId = factory.Id,
                IsActive = machine.IsEnabled
            };
            db.ProductionLines.Add(line);
            await db.SaveChangesAsync();
        }
        else
        {
            line.LineCode = machine.MachineCode;
            line.LineName = machine.MachineName;
            line.IsActive = machine.IsEnabled;
            await db.SaveChangesAsync();
        }

        var existingTanks = await db.Tanks.Where(x => x.LineId == line.Id).ToListAsync();
        var incomingIds = tanks.Select(t => t.Id).Where(id => id > 0).ToHashSet();
        var tanksToRemove = existingTanks.Where(t => !incomingIds.Contains(t.Id)).ToList();
        if (tanksToRemove.Count > 0)
        {
            db.Tanks.RemoveRange(tanksToRemove);
        }

        foreach (var tankReq in tanks.Where(x => !string.IsNullOrWhiteSpace(x.TankName) || !string.IsNullOrWhiteSpace(x.TankCode)))
        {
            var cleanName = string.IsNullOrWhiteSpace(tankReq.TankName) ? tankReq.TankCode!.Trim() : tankReq.TankName!.Trim();
            var cleanCode = string.IsNullOrWhiteSpace(tankReq.TankCode) ? cleanName : tankReq.TankCode!.Trim();
            var tankCode = cleanCode.StartsWith(machine.MachineCode + "-", StringComparison.OrdinalIgnoreCase)
                ? cleanCode
                : $"{machine.MachineCode}-{cleanCode}";

            var tank = tankReq.Id > 0
                ? await db.Tanks.FirstOrDefaultAsync(x => x.Id == tankReq.Id)
                : await db.Tanks.FirstOrDefaultAsync(x => x.LineId == line.Id && x.TankCode == tankCode);

            if (tank is null)
            {
                tank = new Tank { LineId = line.Id, TankCode = tankCode, TankName = cleanName, IsActive = tankReq.IsActive };
                db.Tanks.Add(tank);
            }
            else
            {
                tank.LineId = line.Id;
                tank.TankCode = tankCode;
                tank.TankName = cleanName;
                tank.IsActive = tankReq.IsActive;
            }
        }

        await db.SaveChangesAsync();
    }
}

public class MachineSaveRequest
{
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<MachineTankRequest>? Tanks { get; set; }
}

public class MachineTankRequest
{
    public int Id { get; set; }
    public string? TankCode { get; set; }
    public string? TankName { get; set; }
    public bool IsActive { get; set; } = true;
}

[ApiController]
[Route("api/characteristics")]
[Route("api/v1/characteristics")]
public class QualityCharacteristicsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.QualityCharacteristics.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(QualityCharacteristic req) { req.ControlScope = NormalizeScope(req.ControlScope); req.InputMode = NormalizeInputMode(req.InputMode); req.ValueLabel = string.IsNullOrWhiteSpace(req.ValueLabel) ? "量測值" : req.ValueLabel.Trim(); req.DecimalPlaces = Math.Clamp(req.DecimalPlaces, 0, 8); db.QualityCharacteristics.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, QualityCharacteristic req)
    {
        var x = await db.QualityCharacteristics.FindAsync(id); if (x is null) return NotFound();
        x.CharacteristicCode = req.CharacteristicCode; x.CharacteristicName = req.CharacteristicName; x.ControlScope = NormalizeScope(req.ControlScope); x.DataCategory = req.DataCategory; x.Unit = req.Unit;
        x.InputMode = NormalizeInputMode(req.InputMode); x.ValueLabel = string.IsNullOrWhiteSpace(req.ValueLabel) ? "量測值" : req.ValueLabel.Trim(); x.DecimalPlaces = Math.Clamp(req.DecimalPlaces, 0, 8);
        x.DefaultChartTypeId = req.DefaultChartTypeId; x.IsSpcEnabled = req.IsSpcEnabled; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.QualityCharacteristics.FindAsync(id); if (x is null) return NotFound(); db.QualityCharacteristics.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private static string NormalizeScope(string? scope) => string.IsNullOrWhiteSpace(scope) ? "PRODUCT" : scope.Trim().ToUpperInvariant();
    private static string NormalizeInputMode(string? mode) => mode?.Trim().ToUpperInvariant() switch { "FORMULA" => "FORMULA", "RECORD_ONLY" => "RECORD_ONLY", _ => "DIRECT" };
}

[ApiController]
[Route("api/part-process-characteristics")]
[Route("api/v1/part-process-characteristics")]
public class PartProcessCharacteristicsController(AppDbContext db) : ControllerBase
{
    private const string ItemRuleGroupCodePrefix = "PPC_RULES_";
    private const string ChartTypeRuleGroupCodePrefix = "CT_RULES_";

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await db.PartProcessCharacteristics
            .AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Machine)
            .Include(x => x.Tank)
            .Include(x => x.Slot)
            .Include(x => x.Characteristic)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var ids = items.Select(x => x.Id).ToList();
        var variableCounts = await db.VariableMeasurements
            .AsNoTracking()
            .Where(x => ids.Contains(x.PartProcessCharacteristicId))
            .GroupBy(x => x.PartProcessCharacteristicId)
            .Select(g => new { PartProcessCharacteristicId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartProcessCharacteristicId, x => x.Count);
        var attributeCounts = await db.AttributeMeasurements
            .AsNoTracking()
            .Where(x => ids.Contains(x.PartProcessCharacteristicId))
            .GroupBy(x => x.PartProcessCharacteristicId)
            .Select(g => new { PartProcessCharacteristicId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PartProcessCharacteristicId, x => x.Count);

        return Ok(items.Select(x =>
        {
            var variableCount = variableCounts.GetValueOrDefault(x.Id);
            var attributeCount = attributeCounts.GetValueOrDefault(x.Id);
            return new
            {
                x.Id,
                x.ControlScope,
                x.PartId,
                x.ProcessId,
                x.MachineId,
                x.TankId,
                x.SlotId,
                x.CharacteristicId,
                x.Unit,
                x.USL,
                x.LSL,
                x.UCL,
                x.CL,
                x.LCL,
                x.TargetValue,
                x.SampleSize,
                x.DisplayMode,
                x.ChartTypeId,
                x.RuleGroupId,
                x.FormulaConfigJson,
                x.IsRequired,
                x.IsEnabled,
                x.Part,
                x.Process,
                x.Machine,
                x.Tank,
                x.Slot,
                x.Characteristic,
                VariableMeasurementCount = variableCount,
                AttributeMeasurementCount = attributeCount,
                MeasurementCount = variableCount + attributeCount
            };
        }));
    }
    [HttpPost]
    public async Task<IActionResult> Create(PartProcessCharacteristic req)
    {
        var validation = await ValidateScopeAsync(req);
        if (validation is not null) return BadRequest(validation);
        var displayValidation = await ValidateAndNormalizeDisplayModeAsync(req);
        if (displayValidation is not null) return BadRequest(displayValidation);
        req.Unit = CleanUnit(req.Unit);
        req.FormulaConfigJson = CleanFormulaConfig(req.FormulaConfigJson);
        db.PartProcessCharacteristics.Add(req);
        await db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PartProcessCharacteristic req)
    {
        var x = await db.PartProcessCharacteristics.FindAsync(id); if (x is null) return NotFound();
        var validation = await ValidateScopeAsync(req);
        if (validation is not null) return BadRequest(validation);
        var displayValidation = await ValidateAndNormalizeDisplayModeAsync(req);
        if (displayValidation is not null) return BadRequest(displayValidation);
        x.ControlScope = NormalizeScope(req.ControlScope); x.PartId = req.PartId; x.ProcessId = req.ProcessId; x.MachineId = req.MachineId; x.TankId = req.TankId; x.SlotId = req.SlotId; x.CharacteristicId = req.CharacteristicId;
        x.Unit = CleanUnit(req.Unit);
        x.USL = req.USL; x.LSL = req.LSL; x.UCL = req.UCL; x.CL = req.CL; x.LCL = req.LCL; x.TargetValue = req.TargetValue;
        x.SampleSize = req.SampleSize; x.DisplayMode = req.DisplayMode; x.ChartTypeId = req.ChartTypeId; x.FormulaConfigJson = CleanFormulaConfig(req.FormulaConfigJson); x.IsRequired = req.IsRequired; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }

    [HttpGet("{id:int}/rules")]
    public async Task<IActionResult> GetRules(int id)
    {
        var item = await db.PartProcessCharacteristics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var selectedRules = item.RuleGroupId.HasValue
            ? await db.SpcRules.AsNoTracking()
                .Where(x => x.RuleGroupId == item.RuleGroupId.Value && x.IsEnabled)
                .Select(x => x.RuleCode)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return Ok(new ItemRulesResponse(
            item.Id,
            item.RuleGroupId,
            (await GetRuleLibraryAsync()).Select(x => new ItemRuleOption(
                x.RuleCode,
                x.RuleName,
                x.Priority,
                selectedRules.Contains(x.RuleCode))).ToList()));
    }

    [HttpPut("{id:int}/rules")]
    public async Task<IActionResult> UpdateRules(int id, ItemRulesUpdateRequest req)
    {
        var item = await db.PartProcessCharacteristics.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        return BadRequest(new { message = "所有管制項目統一使用 WE 八大規則，不支援項目專屬規則。" });
    }

    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.PartProcessCharacteristics.FindAsync(id); if (x is null) return NotFound(); db.PartProcessCharacteristics.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private async Task<List<ItemRuleTemplate>> GetRuleLibraryAsync()
    {
        var rules = await db.SpcRules.AsNoTracking()
            .Where(x => x.IsEnabled)
            .Join(db.SpcRuleGroups.AsNoTracking().Where(g => g.IsEnabled
                    && !g.RuleGroupCode.StartsWith(ChartTypeRuleGroupCodePrefix)
                    && !g.RuleGroupCode.StartsWith(ItemRuleGroupCodePrefix)),
                rule => rule.RuleGroupId,
                group => group.Id,
                (rule, group) => rule)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return rules
            .GroupBy(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(x => new ItemRuleTemplate(x.RuleCode, x.RuleName, x.Priority, x.RuleConfigJson))
            .ToList();
    }

    private static string? CleanUnit(string? unit) =>
        string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

    private static string? CleanFormulaConfig(string? formulaConfigJson)
    {
        if (string.IsNullOrWhiteSpace(formulaConfigJson)) return null;
        using var document = System.Text.Json.JsonDocument.Parse(formulaConfigJson);
        return document.RootElement.GetRawText();
    }

    private static string NormalizeScope(string? scope)
    {
        var normalized = (scope ?? "PRODUCT").Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "PRODUCT" : normalized;
    }

    private async Task<string?> ValidateScopeAsync(PartProcessCharacteristic req)
    {
        req.ControlScope = NormalizeScope(req.ControlScope);
        if (req.ProcessId <= 0) return "工站製程為必填。";
        if (req.CharacteristicId <= 0) return "品質特性為必填。";
        var selectedGroup = await ResolveSelectedGroupAsync(req);
        if (selectedGroup is null) return "找不到業務範圍對應的管制圖大類別。";
        if (!GroupMatchesScope(selectedGroup, req.ControlScope))
            return "管制圖大類與所選業務範圍不一致。";
        if (selectedGroup.RequiresPart && (!req.PartId.HasValue || req.PartId.Value <= 0))
            return $"{selectedGroup.GroupName}管制項目必須選擇產品料號。";
        if (selectedGroup.RequiresMachine && (!req.MachineId.HasValue || req.MachineId.Value <= 0))
            return $"{selectedGroup.GroupName}管制項目必須選擇線別/機台。";
        if (selectedGroup.RequiresTank && (!req.TankId.HasValue || req.TankId.Value <= 0))
            return $"{selectedGroup.GroupName}管制項目必須選擇槽體。";
        if (!selectedGroup.RequiresPart) req.PartId = null;
        if (!selectedGroup.RequiresMachine) req.MachineId = null;
        if (!selectedGroup.RequiresTank) req.TankId = null;
        if (req.SlotId.HasValue)
        {
            if (!req.TankId.HasValue) return "選擇槽位前必須先選擇槽體。";
            var slotMatchesTank = await db.Slots.AsNoTracking()
                .AnyAsync(x => x.Id == req.SlotId.Value && x.TankId == req.TankId.Value);
            if (!slotMatchesTank) return "槽位不屬於所選槽體。";
        }
        if (!string.IsNullOrWhiteSpace(req.FormulaConfigJson))
        {
            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(req.FormulaConfigJson);
            }
            catch (System.Text.Json.JsonException)
            {
                return "公式配置格式不正確。";
            }
        }
        return null;
    }

    private async Task<string?> ValidateAndNormalizeDisplayModeAsync(PartProcessCharacteristic req)
    {
        var characteristic = await db.QualityCharacteristics
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.CharacteristicId);
        if (characteristic is null) return "找不到指定的品質特性。";
        if (!string.Equals(characteristic.ControlScope, req.ControlScope, StringComparison.OrdinalIgnoreCase))
            return "品質特性與所選管制類型不一致。";

        var selectedGroup = await ResolveSelectedGroupAsync(req);
        if (selectedGroup is null) return "找不到管制類型對應的管制圖大類別。";
        if (!GroupMatchesScope(selectedGroup, req.ControlScope))
            return "管制圖大類與所選業務範圍不一致。";
        req.DisplayMode = selectedGroup.GroupType == "TREND_CHART" ? "TREND_CHART" : "CONTROL_CHART";

        var processMatchesScope = await db.Processes.AsNoTracking()
            .AnyAsync(x => x.Id == req.ProcessId && x.ControlScope == req.ControlScope && x.IsEnabled);
        if (!processMatchesScope) return "工站製程與所選管制類型不一致。";

        if (req.DisplayMode == "TREND_CHART")
        {
            req.ChartTypeId = null;
            req.FormulaConfigJson = null;
            req.UCL = null;
            req.CL = null;
            req.LCL = null;
            return null;
        }

        if (!req.ChartTypeId.HasValue)
            return "選擇管制圖顯示時，必須指定 SPC 管制圖類型。";

        if (req.ChartTypeId.HasValue)
        {
            var chartType = await (
                from type in db.ControlChartTypes.AsNoTracking()
                join groupInfo in db.ControlChartGroups.AsNoTracking()
                    on type.ChartGroupId equals groupInfo.Id
                where type.Id == req.ChartTypeId.Value && type.IsEnabled
                select new { Type = type, groupInfo.GroupType }
            ).FirstOrDefaultAsync();
            if (chartType is null) return "找不到指定的 SPC 管制圖類型。";
            if (!chartType.Type.DataCategory.Equals(characteristic.DataCategory, StringComparison.OrdinalIgnoreCase))
                return "管制圖類型與品質特性的資料型態不一致。";
            if (!string.Equals(chartType.GroupType, req.DisplayMode, StringComparison.OrdinalIgnoreCase))
                return req.DisplayMode == "TREND_CHART"
                    ? "趨勢圖顯示只能選擇趨勢圖類型，不能同時選管制圖。"
                    : "管制圖顯示只能選擇管制圖類型，不能同時選趨勢圖。";
        }

        return null;
    }

    private async Task<ControlChartGroup?> ResolveSelectedGroupAsync(PartProcessCharacteristic req)
    {
        if (req.ChartTypeId.HasValue)
        {
            return await (
                from type in db.ControlChartTypes.AsNoTracking()
                join groupInfo in db.ControlChartGroups.AsNoTracking() on type.ChartGroupId equals groupInfo.Id
                where type.Id == req.ChartTypeId.Value && type.IsEnabled && groupInfo.IsEnabled
                select groupInfo).FirstOrDefaultAsync();
        }

        var scope = NormalizeScope(req.ControlScope);
        var mode = (req.DisplayMode ?? "CONTROL_CHART").Trim().ToUpperInvariant();
        var legacyGroupCode = scope switch
        {
            "CHEMICAL" or "CHEM" => "CHEM",
            "PRODUCT" => "PROD",
            "PROCESS" => "PROC",
            _ => scope
        };
        return await db.ControlChartGroups.AsNoTracking()
            .Where(x => x.IsEnabled && x.GroupType == mode
                && (x.BusinessScopeCode == scope
                    || (x.BusinessScopeCode == "" && x.GroupCode == legacyGroupCode)))
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();
    }

    private static bool GroupMatchesScope(ControlChartGroup group, string scope)
    {
        var configuredScope = string.IsNullOrWhiteSpace(group.BusinessScopeCode)
            ? group.GroupCode.Trim().ToUpperInvariant() switch
            {
                "CHEM" or "CHEM_TREND" => "CHEM",
                "PROD" => "PRODUCT",
                "PROC" => "PROCESS",
                var code => code
            }
            : group.BusinessScopeCode.Trim().ToUpperInvariant();
        return string.Equals(configuredScope, NormalizeScope(scope), StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ItemRuleTemplate(string RuleCode, string RuleName, int Priority, string? RuleConfigJson);
    public sealed record ItemRuleOption(string RuleCode, string RuleName, int Priority, bool IsSelected);
    public sealed record ItemRulesResponse(int PartProcessCharacteristicId, int? RuleGroupId, List<ItemRuleOption> Rules);
    public sealed record ItemRulesUpdateRequest(List<string>? SelectedRuleCodes);
}

[ApiController]
[Route("api/control-chart-groups")]
[Route("api/v1/control-chart-groups")]
public class ControlChartGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? groupType = null)
    {
        var query = db.ControlChartGroups.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(groupType))
        {
            var normalizedType = groupType.Trim().ToUpperInvariant();
            query = query.Where(x => x.GroupType == normalizedType);
        }
        return Ok(await query.OrderBy(x => x.Id).ToListAsync());
    }
    [HttpPost]
    public async Task<IActionResult> Create(ControlChartGroup req)
    {
        NormalizeBusinessRules(req);
        db.ControlChartGroups.Add(req); await db.SaveChangesAsync(); return Ok(req);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlChartGroup req)
    {
        var x = await db.ControlChartGroups.FindAsync(id); if (x is null) return NotFound();
        NormalizeBusinessRules(req);
        x.GroupCode = req.GroupCode; x.GroupName = req.GroupName; x.GroupType = req.GroupType;
        x.BusinessScopeCode = req.BusinessScopeCode; x.RequiresPart = req.RequiresPart;
        x.RequiresMachine = req.RequiresMachine; x.RequiresTank = req.RequiresTank;
        x.Description = req.Description; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpDelete("{id:int}")] 
    public async Task<IActionResult> Delete(int id) 
    { 
        var x = await db.ControlChartGroups.FindAsync(id); if (x is null) return NotFound(); 
        db.ControlChartGroups.Remove(x); await db.SaveChangesAsync(); return NoContent(); 
    }

    private static void NormalizeBusinessRules(ControlChartGroup group)
    {
        group.GroupCode = group.GroupCode.Trim().ToUpperInvariant();
        group.GroupType = string.IsNullOrWhiteSpace(group.GroupType)
            ? "CONTROL_CHART" : group.GroupType.Trim().ToUpperInvariant();
        group.BusinessScopeCode = string.IsNullOrWhiteSpace(group.BusinessScopeCode)
            ? group.GroupCode : group.BusinessScopeCode.Trim().ToUpperInvariant();
    }
}



[ApiController]
[Route("api/operators")]
[Route("api/v1/operators")]
public class OperatorsController(
    AppDbContext db,
    MesSpc.Api.Services.Security.UserPasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await db.Operators.OrderBy(x => x.Id).ToListAsync();
        return Ok(users.Select(ToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> Create(OperatorRequest req)
    {
        var validation = await ValidateRequestAsync(req);
        if (validation is not null) return validation;

        var user = new Operator
        {
            OperatorCode = req.OperatorCode.Trim(),
            OperatorName = req.OperatorName.Trim(),
            Department = req.Department?.Trim(),
            Email = req.Email?.Trim(),
            Username = NullIfWhiteSpace(req.Username),
            Role = MesSpc.Api.Services.Security.UserRoles.Normalize(req.Role),
            IsActive = req.IsActive,
            PasswordHash = string.IsNullOrWhiteSpace(req.Password) ? null : passwordHasher.Hash(req.Password)
        };
        db.Operators.Add(user);
        await db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OperatorRequest req)
    {
        var x = await db.Operators.FindAsync(id); if (x is null) return NotFound();
        var validation = await ValidateRequestAsync(req, id);
        if (validation is not null) return validation;

        x.OperatorCode = req.OperatorCode.Trim();
        x.OperatorName = req.OperatorName.Trim();
        x.Department = req.Department?.Trim();
        x.Email = req.Email?.Trim();
        x.Username = NullIfWhiteSpace(req.Username);
        x.Role = MesSpc.Api.Services.Security.UserRoles.Normalize(req.Role);
        x.IsActive = req.IsActive;
        if (!string.IsNullOrWhiteSpace(req.Password)) x.PasswordHash = passwordHasher.Hash(req.Password);
        x.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ToResponse(x));
    }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.Operators.FindAsync(id); if (x is null) return NotFound(); db.Operators.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private async Task<IActionResult?> ValidateRequestAsync(OperatorRequest req, int? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(req.OperatorCode) || string.IsNullOrWhiteSpace(req.OperatorName))
            return BadRequest(new { message = "作業員工號與姓名皆為必填欄位。" });
        if (!string.IsNullOrWhiteSpace(req.Username))
        {
            var username = req.Username.Trim();
            if (await db.Operators.AnyAsync(x => x.Username == username && (!currentId.HasValue || x.Id != currentId.Value)))
                return Conflict(new { message = $"登入帳號「{username}」已存在。" });
        }
        if (await db.Operators.AnyAsync(x => x.OperatorCode == req.OperatorCode.Trim() && (!currentId.HasValue || x.Id != currentId.Value)))
            return Conflict(new { message = $"工號「{req.OperatorCode.Trim()}」已存在。" });
        return null;
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object ToResponse(Operator x) => new
    {
        x.Id,
        x.OperatorCode,
        x.OperatorName,
        x.Department,
        x.Email,
        x.Username,
        Role = MesSpc.Api.Services.Security.UserRoles.Normalize(x.Role),
        x.IsActive,
        HasPassword = !string.IsNullOrWhiteSpace(x.PasswordHash),
        x.CreatedAt,
        x.UpdatedAt
    };

    public record OperatorRequest(
        string OperatorCode,
        string OperatorName,
        string? Department,
        string? Email,
        string? Username,
        string? Password,
        string? Role,
        bool IsActive = true);
}



[ApiController]
[Route("api/control-chart-types")]
[Route("api/v1/control-chart-types")]
public class ControlChartTypesController(AppDbContext db) : ControllerBase
{
    private const string ChartTypeRuleGroupCodePrefix = "CT_RULES_";
    private const string ItemRuleGroupCodePrefix = "PPC_RULES_";

    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.ControlChartTypes.OrderBy(x => x.Id).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(ControlChartType req) { db.ControlChartTypes.Add(req); await db.SaveChangesAsync(); return Ok(req); }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ControlChartType req)
    {
        var x = await db.ControlChartTypes.FindAsync(id); if (x is null) return NotFound();
        x.ChartGroupId = req.ChartGroupId; x.ChartTypeCode = req.ChartTypeCode; x.ChartTypeName = req.ChartTypeName; x.DataCategory = req.DataCategory; x.RequiredSampleSize = req.RequiredSampleSize; x.RuleGroupId = req.RuleGroupId; x.Description = req.Description; x.FormulaConfigJson = req.FormulaConfigJson; x.IsEnabled = req.IsEnabled;
        await db.SaveChangesAsync(); return Ok(x);
    }
    [HttpPatch("{id:int}/name")]
    public async Task<IActionResult> UpdateName(int id, UpdateChartTypeNameRequest req)
    {
        var name = req.ChartTypeName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("管制圖顯示名稱不可空白。");
        var x = await db.ControlChartTypes.FindAsync(id);
        if (x is null) return NotFound();
        x.ChartTypeName = name;
        await db.SaveChangesAsync();
        return Ok(x);
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
        return BadRequest(new { message = "所有管制圖統一使用 WE 八大規則，不支援管制圖專屬規則。" });
    }

    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { var x = await db.ControlChartTypes.FindAsync(id); if (x is null) return NotFound(); db.ControlChartTypes.Remove(x); await db.SaveChangesAsync(); return NoContent(); }

    private async Task<List<SpcRuleTemplate>> GetRuleLibraryAsync()
    {
        var rules = await db.SpcRules.AsNoTracking()
            .Where(x => x.IsEnabled)
            .Join(db.SpcRuleGroups.AsNoTracking().Where(g => g.IsEnabled
                    && !g.RuleGroupCode.StartsWith(ChartTypeRuleGroupCodePrefix)
                    && !g.RuleGroupCode.StartsWith(ItemRuleGroupCodePrefix)),
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

public record UpdateChartTypeNameRequest(string ChartTypeName);

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
    private const string ItemRuleGroupCodePrefix = "PPC_RULES_";
    private static readonly HashSet<string> AllowedRuleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rule1_Over3Sigma",
        "Rule2_9SameSide",
        "Rule3_6Trend",
        "Rule4_14Alternating",
        "Rule5_2Of3Over2Sigma",
        "Rule6_4Of5Over1Sigma",
        "Rule7_15Within1Sigma",
        "Rule8_8Outside1Sigma"
    };

    [HttpGet] 
    public async Task<IActionResult> Get([FromQuery] int? groupId, [FromQuery] bool libraryOnly = false) 
    {
        var q = db.SpcRules.AsQueryable();
        if (groupId.HasValue) q = q.Where(x => x.RuleGroupId == groupId.Value);
        if (libraryOnly)
        {
            var libraryRules = await q.Join(db.SpcRuleGroups.Where(g => g.IsEnabled
                    && !g.RuleGroupCode.StartsWith(ChartTypeRuleGroupCodePrefix)
                    && !g.RuleGroupCode.StartsWith(ItemRuleGroupCodePrefix)),
                rule => rule.RuleGroupId,
                group => group.Id,
                (rule, group) => rule)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return Ok(libraryRules
                .Where(x => AllowedRuleCodes.Contains(x.RuleCode))
                .GroupBy(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList());
        }
        return Ok(await q.OrderBy(x => x.Priority).ThenBy(x => x.Id).ToListAsync());
    }
    
    [HttpPost] 
    public async Task<IActionResult> Create(SpcRule req) 
    { 
        return BadRequest("SPC 管制規則固定為西方電氣規則 1～8，不可新增其他規則。");
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SpcRule req)
    {
        var x = await db.SpcRules.FindAsync(id); 
        if (x is null) return NotFound();
        if (!AllowedRuleCodes.Contains(x.RuleCode))
            return BadRequest("只允許西方電氣規則 1～8。");
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
        return BadRequest("西方電氣規則 1～8 為系統固定規則，不可刪除。");
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
