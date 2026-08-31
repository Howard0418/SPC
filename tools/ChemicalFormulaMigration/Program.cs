using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
var patchC5Cleaner = args.Contains("--patch-c5-cleaner", StringComparer.OrdinalIgnoreCase);
var configArg = args.FirstOrDefault(x => x.StartsWith("--config=", StringComparison.OrdinalIgnoreCase));
var ruleArg = args.FirstOrDefault(x => x.StartsWith("--rule=", StringComparison.OrdinalIgnoreCase));
var configPath = configArg?[9..] ?? Path.Combine(root, "release", "test", "backend", "appsettings.json");
var rulesPath = Path.GetFullPath(Path.Combine(root, "..", "PmrPortal", "src", "PmrPortal.Api", "ChemicalAnalysisRules.json"));
var reportPath = Path.Combine(root, "reports", "chemical-formula-migration", "formula-migration.csv");

using var config = JsonDocument.Parse(File.ReadAllText(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()
    ?? throw new InvalidOperationException("找不到 ConnectionStrings:SqlServer。");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
await using var db = new AppDbContext(options);
var database = db.Database.GetDbConnection().Database;
if (apply && !args.Contains($"--confirm-database={database}", StringComparer.OrdinalIgnoreCase))
    throw new InvalidOperationException($"安全防護：套用時必須加上 --confirm-database={database}。");

var rules = JsonSerializer.Deserialize<List<LegacyRule>>(File.ReadAllText(rulesPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
if (ruleArg is not null)
{
    var ruleId = ruleArg[7..];
    rules = rules.Where(x => string.Equals(x.Id, ruleId, StringComparison.OrdinalIgnoreCase)).ToList();
    if (rules.Count != 1) throw new InvalidOperationException($"找不到唯一規則：{ruleId}。");
}
var candidates = await db.PartProcessCharacteristics.AsNoTracking()
    .Where(x => x.IsEnabled && (x.ControlScope == "CHEM" || x.ControlScope == "CHEMICAL" || x.ControlScope == "CHEM_TREND"))
    .Include(x => x.Machine).Include(x => x.Tank).Include(x => x.Characteristic)
    .Select(x => new Candidate(x.Id,
        x.Machine != null ? x.Machine.MachineCode : "", x.Machine != null ? x.Machine.MachineName : "",
        x.Tank != null ? x.Tank.TankCode : "", x.Tank != null ? x.Tank.TankName : "",
        x.Characteristic != null ? x.Characteristic.CharacteristicCode : "",
        x.Characteristic != null ? x.Characteristic.CharacteristicName : "", x.Unit,
        x.LSL, x.USL, x.TargetValue, x.ChemicalAnalysisConfigJson)).ToListAsync();
if (patchC5Cleaner)
{
    const string adjustmentFormula = "IF(Concentration<LSL,\"添加\",IF(Concentration>USL,\"稀釋\",\"\"))";
    const string adjustmentAmountFormula = "IFERROR(IF(Concentration>USL,\"排液：\"&ROUND(1100*(1-Target/Concentration),0)&\" L\"&\" 補水：\"&ROUND(1100*(1-Target/Concentration),0)&\" L\",IF(Concentration<LSL,\"DP333：\"&ROUND((Target-Concentration)*1100/1000,1)&\" L\",\"\")),\"\")";
    foreach (var test in new[]
    {
        (Concentration: 70m, Adjustment: "添加", Amount: "DP333：33 L"),
        (Concentration: 100m, Adjustment: "", Amount: ""),
        (Concentration: 130m, Adjustment: "稀釋", Amount: "排液：254 L 補水：254 L")
    })
    {
        var cells = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            { ["CONCENTRATION"] = test.Concentration, ["LSL"] = 90m, ["TARGET"] = 100m, ["USL"] = 110m };
        if (Formula.EvaluateText(adjustmentFormula, cells) != test.Adjustment
            || Formula.EvaluateText(adjustmentAmountFormula, cells) != test.Amount)
            throw new InvalidOperationException($"C5 公式邊界驗證失敗：Concentration={test.Concentration}。");
    }
    var matches = candidates.Where(x => string.Equals(x.MachineCode, "C5", StringComparison.OrdinalIgnoreCase)
        && Canon(x.TankName) == Canon("清潔") && Canon(x.CharacteristicName) == Canon("硫酸")
        && Canon(x.Unit) == Canon("ml/L")).ToList();
    if (matches.Count != 1) throw new InvalidOperationException($"C5／清潔／硫酸／ml/L 應唯一對應，實際為 {matches.Count} 筆。");
    var target = matches[0];
    var desiredConfig = JsonSerializer.Serialize(new
    {
        enabled = true,
        version = "2",
        primaryInputLabel = "滴定值",
        secondaryInputLabel = "",
        concentrationFormula = "Primary * 8.48 * 0.995",
        adjustmentFormula,
        adjustmentAmountFormula,
        decimalPlaces = 2
    });
    Console.WriteLine($"資料庫：{database}；PPC：{target.Id}；模式：{(apply ? "APPLY" : "DRY-RUN")}");
    var alreadyMatches = string.Equals(target.ExistingConfig, desiredConfig, StringComparison.Ordinal)
        && target.Lsl == 90d && target.Target == 100d && target.Usl == 110d;
    Console.WriteLine(alreadyMatches
        ? "狀態：已符合" : "狀態：需要更新");
    if (apply && !alreadyMatches)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        var rollbackPath = Path.Combine(Path.GetDirectoryName(reportPath)!,
            $"{database}-C5-cleaner-rollback-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(rollbackPath, JsonSerializer.Serialize(new
        {
            database,
            ppcId = target.Id,
            previousConfig = target.ExistingConfig,
            previousLsl = target.Lsl,
            previousTarget = target.Target,
            previousUsl = target.Usl
        }, new JsonSerializerOptions { WriteIndented = true }));
        var entity = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == target.Id);
        entity.ChemicalAnalysisConfigJson = desiredConfig;
        entity.LSL = 90d;
        entity.TargetValue = 100d;
        entity.USL = 110d;
        await db.SaveChangesAsync();
        Console.WriteLine($"已更新；回復檔：{rollbackPath}");
    }
    return;
}
if (args.Contains("--inventory", StringComparer.OrdinalIgnoreCase))
{
    foreach (var group in candidates.GroupBy(x => new { x.MachineCode, x.MachineName }).OrderBy(x => x.Key.MachineCode))
    {
        Console.WriteLine($"[{group.Key.MachineCode}] {group.Key.MachineName}: {group.Count()} 筆");
        foreach (var item in group.OrderBy(x => x.TankName).ThenBy(x => x.CharacteristicName))
            Console.WriteLine($"  PPC {item.Id}: {item.TankCode}/{item.TankName} | {item.CharacteristicCode}/{item.CharacteristicName} | {item.Unit}");
    }
    return;
}

var results = new List<Result>();
foreach (var rule in rules)
{
    var converted = ConvertRule(rule);
    var validation = Validate(rule, converted);
    var matches = candidates.Where(x => MatchLine(rule.Line, x) && MatchName(rule.Tank, x.TankCode, x.TankName)
        && MatchChemical(rule.Characteristic, x.CharacteristicCode, x.CharacteristicName)
        && MatchUnit(rule.Unit, x.Unit)).ToList();
    var match = matches.Count == 1 ? matches[0] : null;
    var isPh = Canon(rule.Characteristic) is "PH" or "酸鹼值";
    var existing = !string.IsNullOrWhiteSpace(match?.ExistingConfig);
    var existingValidation = existing ? ValidateExisting(rule, match!.ExistingConfig!) : new Validation(false, 0, "尚未設定");
    var status = converted.Error is not null ? "CONVERSION_FAILED"
        : !validation.Ok ? "VALIDATION_FAILED"
        : matches.Count == 0 ? "UNMATCHED"
        : matches.Count > 1 ? "AMBIGUOUS"
        : isPh ? "SKIP_PH"
        : existing && existingValidation.Ok ? "SKIP_EXISTING_VERIFIED"
        : existing ? "EXISTING_DIFFERENT"
        : "READY";
    results.Add(new(rule, converted, existing ? existingValidation : validation, match, matches.Count, status));
}

if (apply)
{
    var conflictingIds = results.Where(x => x.Match is not null)
        .GroupBy(x => x.Match!.Id).Where(x => x.Count() > 1 && x.Select(y => y.Converted.Amount).Distinct().Count() > 1)
        .Select(x => x.Key).ToHashSet();
    var ready = results.Where(x => x.Match is not null && (x.Status == "READY"
        || x.Status == "EXISTING_DIFFERENT" && !conflictingIds.Contains(x.Match.Id))).ToList();
    var ids = ready.Select(x => x.Match!.Id).ToArray();
    var entities = await db.PartProcessCharacteristics.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
    foreach (var row in ready)
    {
        var rule = row.Rule; var converted = row.Converted;
        entities[row.Match!.Id].ChemicalAnalysisConfigJson = JsonSerializer.Serialize(new
        {
            enabled = true, version = $"legacy-{rule.Version}", primaryInputLabel = "滴定值",
            secondaryInputLabel = rule.RequiresSecondaryInput ? "第二讀值" : "",
            concentrationFormula = converted.Concentration, adjustmentFormula = converted.Adjustment,
            adjustmentAmountFormula = converted.Amount, decimalPlaces = 2,
            migratedFrom = rule.Id, migratedAt = DateTimeOffset.Now
        });
    }
    await db.SaveChangesAsync();
    Console.WriteLine($"已套用 {ready.Count} 筆至 {database}。");
}

Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
await using (var writer = new StreamWriter(reportPath, false, new UTF8Encoding(true)))
{
    var headers = new[] { "舊規則", "線別", "槽位", "分析項目", "單位", "狀態", "SPC設定ID", "候選數", "分析公式", "調整公式", "調整量公式", "驗證組數", "驗證說明", "轉換錯誤" };
    await writer.WriteLineAsync(string.Join(',', headers.Select(Csv)));
    foreach (var x in results) await writer.WriteLineAsync(string.Join(',', new object?[] { x.Rule.Id, Clean(x.Rule.Line), x.Rule.Tank,
        x.Rule.Characteristic, x.Rule.Unit, x.Status, x.Match?.Id, x.MatchCount, x.Converted.Concentration,
        x.Converted.Adjustment, x.Converted.Amount, x.Validation.Cases, x.Validation.Message, x.Converted.Error }.Select(Csv)));
}
Console.WriteLine($"資料庫：{database}");
Console.WriteLine($"模式：{(apply ? "APPLY" : "DRY-RUN")}");
Console.WriteLine($"規則：{rules.Count}；報告：{reportPath}");
foreach (var group in results.GroupBy(x => x.Status).OrderBy(x => x.Key)) Console.WriteLine($"{group.Key}: {group.Count()}");

static Converted ConvertRule(LegacyRule rule)
{
    var concentration = $"ROUND(({Fmt(rule.Constant)})+({Fmt(rule.PrimaryCoefficient)}*Primary)+({Fmt(rule.SecondaryCoefficient)}*Secondary),8)";
    try
    {
        string? Translate(string? formula)
        {
            if (string.IsNullOrWhiteSpace(formula)) return "";
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(rule.ConcentrationCell)) map[Cell(rule.ConcentrationCell)] = "Concentration";
            if (!string.IsNullOrWhiteSpace(rule.AdjustmentCell)) map[Cell(rule.AdjustmentCell)] = "Concentration";
            foreach (var pair in rule.CellValues) map.TryAdd(Cell(pair.Key), Fmt(pair.Value));
            foreach (var pair in rule.GlobalValues) map.TryAdd(Cell(pair.Key), Fmt(pair.Value));
            var pieces = Regex.Split(formula, "(\"(?:\"\"|[^\"])*\")");
            for (var i = 0; i < pieces.Length; i += 2)
                pieces[i] = Regex.Replace(pieces[i], @"(?<![A-Za-z0-9_])(?:[A-Za-z0-9_]+!)?\$?[A-Za-z]+\$?\d+", m =>
                    map.TryGetValue(Cell(m.Value), out var value) ? value : "BLANK");
            return string.Concat(pieces);
        }
        return new(concentration, Translate(rule.AdjustmentFormula), Translate(rule.AdjustmentAmountFormula), null);
    }
    catch (Exception ex) { return new(concentration, "", "", ex.Message); }
}

static Validation Validate(LegacyRule rule, Converted converted)
{
    if (converted.Error is not null) return new(false, 0, converted.Error);
    try
    {
        var desired = new List<decimal> { 0m, 1m };
        if (rule.ExcelLsl is decimal lsl) desired.AddRange([lsl - 1m, lsl, lsl + 0.01m]);
        if (rule.ExcelUsl is decimal usl) desired.AddRange([usl - 0.01m, usl, usl + 1m]);
        var inputs = desired.Distinct().Where(x => rule.PrimaryCoefficient != 0)
            .Select(x => (x - rule.Constant) / rule.PrimaryCoefficient).ToList();
        var count = 0;
        foreach (var primary in inputs)
        {
            var oldConcentration = decimal.Round(rule.Constant + rule.PrimaryCoefficient * primary, 8, MidpointRounding.AwayFromZero);
            var oldCells = rule.CellValues.Concat(rule.GlobalValues).ToDictionary(x => Cell(x.Key), x => x.Value, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(rule.ConcentrationCell)) oldCells[Cell(rule.ConcentrationCell)] = oldConcentration;
            var oldAdjustment = Formula.EvaluateText(rule.AdjustmentFormula, oldCells);
            if (!string.IsNullOrWhiteSpace(rule.AdjustmentCell) && !string.IsNullOrEmpty(oldAdjustment)) oldCells[Cell(rule.AdjustmentCell)] = 1m;
            var oldAmount = Formula.EvaluateText(rule.AdjustmentAmountFormula, oldCells);
            var newCells = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["PRIMARY"] = primary, ["SECONDARY"] = 0,
                ["CONCENTRATION"] = oldConcentration, ["LSL"] = rule.ExcelLsl ?? 0, ["USL"] = rule.ExcelUsl ?? 0,
                ["TARGET"] = rule.ExcelLsl.HasValue && rule.ExcelUsl.HasValue ? (rule.ExcelLsl.Value + rule.ExcelUsl.Value) / 2 : 0 };
            var newConcentration = Formula.EvaluateNumber(converted.Concentration, newCells);
            var newAdjustment = Formula.EvaluateText(converted.Adjustment, newCells);
            var newAmount = Formula.EvaluateText(converted.Amount, newCells);
            if (Math.Abs(newConcentration - oldConcentration) > 0.00000001m || newAdjustment != oldAdjustment || newAmount != oldAmount)
                return new(false, count + 1, $"Primary={primary}: old={oldConcentration}/{oldAdjustment}/{oldAmount}; new={newConcentration}/{newAdjustment}/{newAmount}");
            count++;
        }
        return new(true, count, "逐組一致");
    }
    catch (Exception ex) { return new(false, 0, ex.Message); }
}

static Validation ValidateExisting(LegacyRule rule, string json)
{
    try
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        string Read(string name) => root.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
        return Validate(rule, new Converted(Read("concentrationFormula"), Read("adjustmentFormula"), Read("adjustmentAmountFormula"), null));
    }
    catch (Exception ex) { return new(false, 0, $"既有設定無法驗證：{ex.Message}"); }
}

static bool MatchLine(string source, Candidate c)
{
    var code = Canon(Regex.Match(source, @"\(([^)]+)\)").Groups[1].Value);
    code = code switch { "PT" => "PT1", "QE" => "QE1", "ST" => "ST1", "DV" => "DV1", _ => code };
    return code == Canon(c.MachineCode);
}
static bool MatchName(string source, string code, string name)
{
    static string Tank(string value)
    {
        var normalized = Canon(value);
        if (normalized == "鹽酸洗") return "酸洗";
        return normalized.EndsWith("槽", StringComparison.Ordinal) ? normalized[..^1] : normalized;
    }
    return Tank(source) == Tank(code) || Tank(source) == Tank(name);
}
static bool MatchChemical(string source, string code, string name)
{
    var groups = new[] { new[] { "H2SO4", "硫酸" }, new[] { "SPS", "過硫酸鈉" }, new[] { "KOH", "氫氧化鉀" },
        new[] { "HCL", "鹽酸" }, new[] { "H2O2", "雙氧水", "過氧化氫" }, new[] { "NA2CO3", "碳酸鈉" },
        new[] { "CU", "CU2", "CU2+", "銅離子", "硫酸銅" }, new[] { "CL", "CL-", "氯離子" },
        new[] { "SNCL2", "氯化亞錫" }, new[] { "NI", "鎳" }, new[] { "NA3PO3", "亞磷酸鈉" },
        new[] { "NAH2PO2", "次磷酸鈉" }, new[] { "HNO3", "硝酸" }, new[] { "PROPORTION", "比重" },
        new[] { "打氣", "曝氣" }, new[] { "ACRF", "P400" }, new[] { "PH", "酸鹼值" } };
    var a = Canon(source); var values = new[] { Canon(code), Canon(name) };
    var group = groups.FirstOrDefault(g => g.Select(Canon).Contains(a));
    return group is null ? values.Contains(a) : group.Select(Canon).Any(values.Contains);
}
static bool MatchUnit(string? a, string? b) => string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) || Canon(a) == Canon(b);
static string Clean(string? value) => (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
static string Canon(string? value) => Regex.Replace((value ?? "").Normalize(NormalizationForm.FormKC).ToUpperInvariant(), @"[\s\p{P}\p{S}]", "");
static string Cell(string value) => value.Replace("$", "").ToUpperInvariant();
static string Fmt(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
static string Csv(object? value) => $"\"{(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"")}\"";

sealed record LegacyRule(string Id, string Version, string Sheet, int Row, string Line, string Tank, string Characteristic,
    string? Unit, string Specification, decimal? ExcelLsl, decimal? ExcelUsl, bool RequiresSecondaryInput,
    decimal PrimaryCoefficient, decimal SecondaryCoefficient, decimal Constant, string SourceFormula,
    string? ConcentrationCell, string? AdjustmentCell, string? AdjustmentFormula, string? AdjustmentAmountFormula,
    Dictionary<string, decimal> CellValues, Dictionary<string, decimal> GlobalValues);
sealed record Candidate(int Id, string MachineCode, string MachineName, string TankCode, string TankName,
    string CharacteristicCode, string CharacteristicName, string? Unit, double? Lsl, double? Usl, double? Target, string? ExistingConfig);
sealed record Converted(string Concentration, string? Adjustment, string? Amount, string? Error);
sealed record Validation(bool Ok, int Cases, string Message);
sealed record Result(LegacyRule Rule, Converted Converted, Validation Validation, Candidate? Match, int MatchCount, string Status);

static class Formula
{
    public static decimal EvaluateNumber(string? expression, IReadOnlyDictionary<string, decimal> cells)
    { var value = new Parser(expression ?? "", cells).Parse(); return value.Number ?? throw new InvalidOperationException("結果非數字"); }
    public static string EvaluateText(string? expression, IReadOnlyDictionary<string, decimal> cells)
    { if (string.IsNullOrWhiteSpace(expression)) return ""; try { return new Parser(expression, cells).Parse().TextValue; } catch { return ""; } }
    internal readonly record struct Value(decimal? Number = null, string? Text = null, bool? Boolean = null)
    { public decimal Num => Number ?? (Boolean == true ? 1 : 0); public bool Bool => Boolean ?? (Number != 0 || !string.IsNullOrEmpty(Text)); public string TextValue => Text ?? (Boolean.HasValue ? (Boolean.Value ? "TRUE" : "FALSE") : Number?.ToString("0.########", CultureInfo.InvariantCulture) ?? ""); }
    internal sealed class Parser(string s, IReadOnlyDictionary<string, decimal> cells)
    {
        int p; public Value Parse() { var v = Comparison(); Skip(); if (p != s.Length) throw new FormatException(); return v; }
        Value Comparison() { var l = Concat(); Skip(); var op = Op("<=", ">=", "<>", "=", "<", ">"); if (op is null) return l; var r = Concat(); var cmp = l.Number.HasValue && r.Number.HasValue ? l.Num.CompareTo(r.Num) : string.Compare(l.TextValue, r.TextValue, StringComparison.OrdinalIgnoreCase); return new(Boolean: op switch { "=" => cmp == 0, "<>" => cmp != 0, "<" => cmp < 0, ">" => cmp > 0, "<=" => cmp <= 0, ">=" => cmp >= 0, _ => false }); }
        Value Concat() { var v = Add(); while (Try('&')) v = new(Text: v.TextValue + Add().TextValue); return v; }
        Value Add() { var v = Mul(); while (true) { if (Try('+')) v = new(Number: v.Num + Mul().Num); else if (Try('-')) v = new(Number: v.Num - Mul().Num); else return v; } }
        Value Mul() { var v = Unary(); while (true) { if (Try('*')) v = new(Number: v.Num * Unary().Num); else if (Try('/')) v = new(Number: v.Num / Unary().Num); else return v; } }
        Value Unary() { if (Try('+')) return Unary(); if (Try('-')) return new(Number: -Unary().Num); var v = Primary(); while (Try('%')) v = new(Number: v.Num / 100); return v; }
        Value Primary() { Skip(); if (Try('(')) { var v = Comparison(); Need(')'); return v; } if (Peek() == '"') return new(Text: Str()); if (char.IsDigit(Peek()) || Peek() == '.') return new(Number: Num()); var id = Id(); Skip(); if (Try('(')) return Func(id); return cells.TryGetValue(id.Replace("$", "").ToUpperInvariant(), out var n) ? new(Number: n) : new(Text: ""); }
        Value Func(string name) { name = name.ToUpperInvariant(); if (name == "IF") { var c = Comparison().Bool; Need(','); var y = Comparison(); Need(','); var n = Comparison(); Need(')'); return c ? y : n; } if (name == "IFERROR") { Value v; try { v = Comparison(); } catch { Arg(); v = default; } Need(','); var f = Comparison(); Need(')'); return v.Number.HasValue || v.Text is not null || v.Boolean.HasValue ? v : f; } if (name == "ROUND") { var n = Comparison().Num; Need(','); var d = (int)Comparison().Num; Need(')'); return new(Number: decimal.Round(n, Math.Clamp(d, 0, 28), MidpointRounding.AwayFromZero)); } throw new NotSupportedException(name); }
        void Arg() { var d = 0; var q = false; while (p < s.Length) { var c = s[p]; if (c == '"') q = !q; if (!q) { if (c == '(') d++; else if (c == ')') d--; else if (c == ',' && d == 0) return; } p++; } }
        string Id() { Skip(); var b = p; while (p < s.Length && (char.IsLetterOrDigit(s[p]) || s[p] is '$' or '!' or '_' or '.')) p++; if (b == p) throw new FormatException(); return s[b..p]; }
        string Str() { Need('"'); var b = new StringBuilder(); while (p < s.Length) { var c = s[p++]; if (c != '"') { b.Append(c); continue; } if (p < s.Length && s[p] == '"') { b.Append('"'); p++; continue; } return b.ToString(); } throw new FormatException(); }
        decimal Num() { var b = p; while (p < s.Length && (char.IsDigit(s[p]) || s[p] == '.')) p++; return decimal.Parse(s[b..p], CultureInfo.InvariantCulture); }
        string? Op(params string[] a) { Skip(); foreach (var x in a) if (s.AsSpan(p).StartsWith(x)) { p += x.Length; return x; } return null; }
        bool Try(char c) { Skip(); if (Peek() != c) return false; p++; return true; } void Need(char c) { if (!Try(c)) throw new FormatException(); } char Peek() => p < s.Length ? s[p] : '\0'; void Skip() { while (p < s.Length && char.IsWhiteSpace(s[p])) p++; }
    }
}
