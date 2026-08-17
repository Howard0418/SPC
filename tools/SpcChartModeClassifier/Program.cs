using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = Encoding.UTF8;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var inputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Directory.GetFiles(Path.Combine(root, "sample-data"), "SPC月報-7月*.xlsx").Single();
var outputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(root, "reports", "chart-mode-classification", "分類比對確認.csv");

var configPath = Path.Combine(root, "backend", "MesSpc.Api", "appsettings.json");
using var config = JsonDocument.Parse(File.ReadAllText(configPath));
var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("SqlServer").GetString()
    ?? throw new InvalidOperationException("找不到 ConnectionStrings:SqlServer。");

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
await using var db = new AppDbContext(options);
var databaseName = db.Database.GetDbConnection().Database;
if (!string.Equals(databaseName, "PMR_SPC_2026", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException($"安全防護：預期 PMR_SPC_2026，實際為 {databaseName}。");

var candidates = await db.PartProcessCharacteristics.AsNoTracking()
    .Where(x => (x.ControlScope == "CHEM" || x.ControlScope == "CHEMICAL" || x.ControlScope == "CHEM_TREND") && x.IsEnabled)
    .Include(x => x.Machine)
    .Include(x => x.Tank)
    .Include(x => x.Characteristic)
    .Select(x => new Candidate(
        x.Id,
        x.MachineId,
        x.Machine != null ? x.Machine.MachineCode : "",
        x.Machine != null ? x.Machine.MachineName : "",
        x.TankId,
        x.Tank != null ? x.Tank.TankCode : "",
        x.Tank != null ? x.Tank.TankName : "",
        x.CharacteristicId,
        x.Characteristic != null ? x.Characteristic.CharacteristicCode : "",
        x.Characteristic != null ? x.Characteristic.CharacteristicName : "",
        x.Unit,
        x.USL,
        x.LSL,
        x.DisplayMode,
        x.ChartTypeId))
    .ToListAsync();

using var workbook = new XLWorkbook(inputPath);
var sheet = workbook.Worksheets.FirstOrDefault(x => Normalize(x.Cell(1, 1).GetString()) == Normalize("管制類別"))
    ?? workbook.Worksheets.FirstOrDefault(x => Normalize(x.Cell(2, 2).GetString()) == Normalize("管制類別"))
    ?? throw new InvalidOperationException("找不到含『管制類別』的工作表。");
var headerRow = Normalize(sheet.Cell(1, 1).GetString()) == Normalize("管制類別") ? 1 : 2;
var headerMap = sheet.Row(headerRow).CellsUsed().ToDictionary(x => Normalize(x.GetString()), x => x.Address.ColumnNumber);
int Col(string name) => headerMap.TryGetValue(Normalize(name), out var col) ? col : 0;
var categoryCol = Col("管制類別");
var lineCol = Col("製程線別");
var chartNoCol = Col("Chart No");
var chartNameCol = Col("Chart Name");
var uslCol = Col("USL");
var lslCol = Col("LSL");
if (categoryCol == 0 || lineCol == 0 || chartNameCol == 0)
    throw new InvalidOperationException("缺少管制類別、製程線別或 Chart Name 欄位。");

var rows = new List<PreviewRow>();
foreach (var row in sheet.RowsUsed().Where(x => x.RowNumber() > headerRow))
{
    var category = row.Cell(categoryCol).GetString().Trim();
    var line = row.Cell(lineCol).GetString().Trim();
    var chartNo = chartNoCol == 0 ? "" : row.Cell(chartNoCol).GetString().Trim();
    var chartName = row.Cell(chartNameCol).GetString().Trim();
    if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(line) && string.IsNullOrWhiteSpace(chartName)) continue;

    var desiredMode = category switch
    {
        "藥液管制" => "CONTROL_CHART",
        "藥液趨勢監控" => "TREND_CHART",
        "製程管制" => "CONTROL_CHART",
        _ => ""
    };
    if (category == "製程管制")
    {
        rows.Add(PreviewRow.Skipped(row.RowNumber(), category, line, chartNo, chartName, desiredMode, "本次只分類藥液項目"));
        continue;
    }
    if (string.IsNullOrWhiteSpace(desiredMode))
    {
        rows.Add(PreviewRow.Unmatched(row.RowNumber(), category, line, chartNo, chartName, "管制類別空白或不支援"));
        continue;
    }

    var usl = uslCol == 0 ? null : ReadDouble(row.Cell(uslCol));
    var lsl = lslCol == 0 ? null : ReadDouble(row.Cell(lslCol));
    var scored = candidates
        .Select(x => new Scored(x, Score(line, chartName, usl, lsl, x)))
        .Where(x => x.Score.Total > 0)
        .OrderByDescending(x => x.Score.Total)
        .ThenBy(x => x.Candidate.Id)
        .ToList();
    if (scored.Count == 0)
    {
        rows.Add(PreviewRow.Unmatched(row.RowNumber(), category, line, chartNo, chartName, "找不到候選項目"));
        continue;
    }

    var best = scored[0];
    var secondScore = scored.Count > 1 ? scored[1].Score.Total : 0;
    var sameTarget = scored.Where(x => x.Score.Total == best.Score.Total).ToList();
    var distinctTargets = sameTarget.Select(x => (x.Candidate.MachineId, x.Candidate.TankId, x.Candidate.CharacteristicId)).Distinct().Count();
    var highConfidence = best.Score.Total >= 85 && best.Score.Total - secondScore >= 10 && distinctTargets == 1;
    var status = highConfidence ? "AUTO_MATCH" : "NEEDS_REVIEW";
    var reason = best.Score.Reason + (highConfidence ? "" : $"；第二候選 {secondScore} 分或存在同分候選");
    var topCandidates = string.Join(" | ", scored.Take(5).Select(Describe));
    rows.Add(new PreviewRow(row.RowNumber(), category, line, chartNo, chartName, status,
        best.Candidate.Id, best.Candidate.MachineCode, best.Candidate.MachineName,
        best.Candidate.TankCode, best.Candidate.TankName,
        best.Candidate.CharacteristicCode, best.Candidate.CharacteristicName,
        best.Candidate.Unit ?? "", best.Score.Total, reason,
        best.Candidate.DisplayMode, desiredMode,
        best.Candidate.DisplayMode == desiredMode ? "NO_CHANGE" : "PENDING",
        "否", topCandidates));
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
var headers = new[] { "Excel列", "Excel管制類別", "Excel線別", "Chart No", "Chart Name", "比對狀態",
    "SPC設定ID", "SPC線別代碼", "SPC線別名稱", "SPC槽位代碼", "SPC槽位名稱", "SPC分析項目代碼", "SPC分析項目名稱",
    "單位", "信心分數", "比對說明", "現有模式", "預計模式", "變更狀態", "是否套用", "前五名候選" };
using (var writer = new StreamWriter(outputPath, false, new UTF8Encoding(true)))
{
    await writer.WriteLineAsync(string.Join(',', headers.Select(Csv)));
    foreach (var row in rows)
        await writer.WriteLineAsync(string.Join(',', row.Values().Select(Csv)));
}

Console.WriteLine($"資料庫：{databaseName}");
Console.WriteLine($"來源：{inputPath}");
Console.WriteLine($"輸出：{outputPath}");
foreach (var group in rows.GroupBy(x => x.MatchStatus).OrderBy(x => x.Key))
    Console.WriteLine($"{group.Key}: {group.Count()}");

static double? ReadDouble(IXLCell cell)
    => cell.TryGetValue<double>(out var value) ? value : null;

static ScoreResult Score(string sourceLine, string chartName, double? usl, double? lsl, Candidate c)
{
    var line = CanonicalLine(sourceLine);
    var code = CanonicalLine(c.MachineCode);
    var name = Normalize(c.MachineName);
    var chart = Normalize(chartName);
    var total = 0;
    var reasons = new List<string>();
    if (line == code) { total += 40; reasons.Add("線別代碼精準"); }
    else if (!string.IsNullOrEmpty(name) && chart.Contains(name)) { total += 32; reasons.Add("線別名稱包含"); }

    var segments = Regex.Split(chartName, @"[-_／/－–—]+")
        .Select(Normalize).Where(x => x.Length > 0).ToArray();
    var itemToken = segments.LastOrDefault() ?? "";
    var locationTokens = segments.Skip(1).SkipLast(1).ToArray();
    var tank = Normalize(c.TankName).Replace("表處", "表面處理");
    locationTokens = locationTokens.Select(x => x.Replace("表處", "表面處理")).ToArray();
    if (!string.IsNullOrEmpty(tank) && locationTokens.Any(x => x == tank)) { total += 25; reasons.Add("槽位名稱精準"); }
    else if (!string.IsNullOrEmpty(tank) && tank.Length >= 2 && locationTokens.Any(x => x.Contains(tank) || tank.Contains(x)))
    { total += 18; reasons.Add("槽位名稱包含"); }

    var charVariants = ChemicalVariants(c.CharacteristicName);
    if (charVariants.Any(x => x.Length > 0 && itemToken == x)) { total += 35; reasons.Add("分析項目末段精準"); }
    else if (charVariants.Any(x => x.Length >= 3 && itemToken.Length >= 3 && (itemToken.Contains(x) || x.Contains(itemToken))))
    { total += 24; reasons.Add("分析項目末段包含"); }

    var specMatches = 0;
    if (usl.HasValue && c.USL.HasValue && NearlyEqual(usl.Value, c.USL.Value)) specMatches++;
    if (lsl.HasValue && c.LSL.HasValue && NearlyEqual(lsl.Value, c.LSL.Value)) specMatches++;
    if (specMatches == 2) { total += 10; reasons.Add("規格上下限相同"); }
    else if (specMatches == 1) { total += 4; reasons.Add("一側規格相同"); }
    return new ScoreResult(total, string.Join('、', reasons));
}

static IEnumerable<string> ChemicalVariants(string value)
{
    var normalized = Normalize(value);
    var groups = new[]
    {
        new[] { "硫酸", "H2SO4" }, new[] { "鹽酸", "HCL" }, new[] { "銅離子", "CU2", "CU" },
        new[] { "雙氧水", "過氧化氫", "H2O2" }, new[] { "氫氧化鉀", "KOH" },
        new[] { "碳酸鈉", "NA2CO3" }, new[] { "氯離子", "CL" }, new[] { "硝酸", "HNO3" }
    };
    var match = groups.FirstOrDefault(x => x.Select(Normalize).Contains(normalized));
    return (match ?? new[] { value }).Select(Normalize).Distinct();
}

static string CanonicalLine(string value)
{
    var line = Normalize(value);
    return line switch { "PT" => "PT1", "QE" => "QE1", "ST" => "ST1", "DV" => "DV1", _ => line };
}

static string Normalize(string? value)
    => Regex.Replace((value ?? "").Normalize(NormalizationForm.FormKC).ToUpperInvariant(), @"[\s\p{P}\p{S}]", "");

static bool NearlyEqual(double a, double b) => Math.Abs(a - b) <= Math.Max(0.000001, Math.Max(Math.Abs(a), Math.Abs(b)) * 0.000001);
static string Describe(Scored x) => $"{x.Candidate.Id}:{x.Candidate.MachineCode}/{x.Candidate.TankName}/{x.Candidate.CharacteristicName}/{x.Candidate.Unit} ({x.Score.Total})";
static string Csv(object? value)
{
    var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    return $"\"{text.Replace("\"", "\"\"")}\"";
}

sealed record Candidate(int Id, int? MachineId, string MachineCode, string MachineName, int? TankId, string TankCode,
    string TankName, int CharacteristicId, string CharacteristicCode, string CharacteristicName, string? Unit,
    double? USL, double? LSL, string DisplayMode, int? ChartTypeId);
sealed record ScoreResult(int Total, string Reason);
sealed record Scored(Candidate Candidate, ScoreResult Score);
sealed record PreviewRow(int ExcelRow, string Category, string Line, string ChartNo, string ChartName, string MatchStatus,
    int? PpcId, string MachineCode, string MachineName, string TankCode, string TankName, string CharacteristicCode,
    string CharacteristicName, string Unit, int Score, string MatchReason, string CurrentMode, string DesiredMode,
    string ChangeStatus, string Apply, string Candidates)
{
    public static PreviewRow Skipped(int row, string category, string line, string chartNo, string chartName, string desired, string reason)
        => new(row, category, line, chartNo, chartName, "SKIPPED", null, "", "", "", "", "", "", "", 0, reason, "", desired, "SKIP", "否", "");
    public static PreviewRow Unmatched(int row, string category, string line, string chartNo, string chartName, string reason)
        => new(row, category, line, chartNo, chartName, "UNMATCHED", null, "", "", "", "", "", "", "", 0, reason, "", "", "UNRESOLVED", "否", "");
    public object?[] Values() => [ExcelRow, Category, Line, ChartNo, ChartName, MatchStatus, PpcId, MachineCode, MachineName,
        TankCode, TankName, CharacteristicCode, CharacteristicName, Unit, Score, MatchReason, CurrentMode, DesiredMode,
        ChangeStatus, Apply, Candidates];
}
