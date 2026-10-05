using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace MesSpc.Api.Services.Calibration;

public sealed class CalibrationImportRow
{
    public int RowNumber { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string RawCycle { get; init; } = "";
    public string RawLastDate { get; init; } = "";
    public string RawNextDate { get; init; } = "";
    public string Location { get; init; } = "";
    public string CalibrationMethod { get; init; } = "";
    public string MeasurementSpecification { get; init; } = "";
    public string Precision { get; init; } = "";
    public string Remarks { get; init; } = "";
    public string CalibrationStandard { get; init; } = "";
    public string AcceptanceCriteria { get; init; } = "";

    public int? CycleMonths { get; set; }
    public DateOnly? LastCalibrationDate { get; set; }
    public DateOnly? NextCalibrationDate { get; set; }
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public string Status { get; set; } = "Ready";
}
public record CalibrationImportPreview(string Hash, string SheetName, List<CalibrationImportRow> Rows);

/// <summary>唯讀 OOXML 範本解析：只讀第一張表及公式儲存值，不計算公式或載入外部連結。</summary>
public static class CalibrationImportParser
{
    public const int MaxFileBytes = 10 * 1024 * 1024;
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly string[] DateFormats = ["yyyy-M-d", "yyyy-MM-dd", "yyyy/M/d", "yyyy/MM/dd", "yyyy.M.d", "yyyy.MM.dd"];
    private sealed record Cell(string Text, string Type, bool Formula, bool HasValue);

    /// <summary>讀取檔案、驗證範本、日期及同檔重複，回傳每列錯誤與警示。</summary>
    public static CalibrationImportPreview Parse(byte[] bytes, TimeProvider clock)
    {
        if (bytes.Length is 0 or > MaxFileBytes) throw new CalibrationException("請上傳 10 MB 以內的 .xlsx 檔案。");
        try { return Read(bytes, clock); }
        catch (CalibrationException) { throw; }
        catch (Exception ex) when (ex is InvalidDataException or XmlException or ArgumentException or InvalidOperationException or OverflowException)
        { throw new CalibrationException("無法讀取 Excel 結構，請使用有效、未加密的 .xlsx 範本。"); }
    }

    private static CalibrationImportPreview Read(byte[] bytes, TimeProvider clock)
    {
        using var stream = new MemoryStream(bytes, false);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        if (zip.Entries.Count > 2000 || zip.Entries.Sum(x => x.Length) > 50L * 1024 * 1024)
            throw new CalibrationException("Excel 解壓大小或檔案項目過多，請縮小檔案。");
        XDocument Xml(string name)
        {
            var entry = zip.GetEntry(name) ?? throw new CalibrationException("Excel 缺少必要的工作表結構。");
            using var input = entry.Open();
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null, MaxCharactersInDocument=50L*1024*1024 });
            return XDocument.Load(reader);
        }
        var workbook = Xml("xl/workbook.xml");
        var first = workbook.Root?.Element(S+"sheets")?.Elements(S+"sheet").FirstOrDefault()
            ?? throw new CalibrationException("Excel 沒有工作表。");
        var relation = Xml("xl/_rels/workbook.xml.rels").Root?.Elements().SingleOrDefault(x => (string?)x.Attribute("Id") == (string?)first.Attribute(R+"id"));
        var target = (string?)relation?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target) || (string?)relation?.Attribute("TargetMode") == "External")
            throw new CalibrationException("第一個工作表必須為檔案內的工作表。");
        var path = new Uri(new Uri("https://xlsx.invalid/xl/workbook.xml"), target);
        if (path.Host != "xlsx.invalid" || !path.AbsolutePath.StartsWith("/xl/", StringComparison.Ordinal))
            throw new CalibrationException("工作表路徑無效。");
        var sheet = Xml(Uri.UnescapeDataString(path.AbsolutePath.TrimStart('/')));
        var shared = zip.GetEntry("xl/sharedStrings.xml") is null ? [] : Xml("xl/sharedStrings.xml").Root!.Elements(S+"si")
            .Select(x => string.Concat(x.Descendants(S+"t").Select(t => t.Value))).ToArray();
        var date1904 = (string?)workbook.Root?.Element(S+"workbookPr")?.Attribute("date1904") is "1" or "true";
        var xmlRows = sheet.Root?.Element(S+"sheetData")?.Elements(S+"row").ToList() ?? [];
        if (xmlRows.Count > 10005) throw new CalibrationException("工作表超過 10,000 筆資料列限制。");
        var cells = new Dictionary<string, Cell>(StringComparer.Ordinal);
        foreach (var c in xmlRows.SelectMany(x=>x.Elements(S+"c")))
        {
            var address = (string?)c.Attribute("r") ?? throw new CalibrationException("儲存格缺少位置。");
            var type = (string?)c.Attribute("t") ?? "n";
            var text = c.Element(S+"v")?.Value ?? "";
            if (type == "s")
            {
                if (!int.TryParse(text, out var index) || index<0 || index>=shared.Length) throw new CalibrationException("共用字串索引無效。");
                text = shared[index];
            }
            if (type == "inlineStr") text = string.Concat(c.Element(S+"is")?.Descendants(S+"t").Select(t=>t.Value) ?? []);
            if (!cells.TryAdd(address, new(text.Trim(),type,c.Element(S+"f") is not null,c.Element(S+"v") is not null || type=="inlineStr")))
                throw new CalibrationException("工作表含重複儲存格位置。");
        }
        Cell Get(string address) => cells.GetValueOrDefault(address) ?? new("","n",false,false);
        foreach (var (address, label) in new[] { ("A5","儀器設備編號"), ("B5","儀器設備名稱"), ("H5","放置地點"), ("I5","校驗週期"), ("J5","校驗方式"), ("Z4","校驗日期"), ("AA4","下次校驗日期") })
            if (Compact(Get(address).Text)!=label) throw new CalibrationException($"範本表頭不符：{address} 應為「{label}」。");
        var detailColumns = new[] { ("F","量測規格"), ("G","精度"), ("AB","備註"), ("AC","校驗規範"), ("AD","允收標準") };
        foreach (var (column, label) in detailColumns)
        {
            var header = Compact(Get(column+"5").Text);
            if (header.Length > 0 && header != label) throw new CalibrationException($"範本表頭不符：{column}5 應為「{label}」。");
        }
        var rows = new List<CalibrationImportRow>();
        foreach (var xmlRow in xmlRows)
        {
            if (!int.TryParse((string?)xmlRow.Attribute("r"),out var number) || number<1) throw new CalibrationException("工作表列號無效。");
            if (number<=5) continue;
            var code=Get($"A{number}"); var name=Get($"B{number}"); var location=Get($"H{number}"); var cycle=Get($"I{number}"); var method=Get($"J{number}"); var last=Get($"Z{number}"); var next=Get($"AA{number}");
            if (Compact(code.Text) is "制定日期:" or "制定日期：") break;
            if (new[] { code,name,cycle,last,next }.All(x=>x.Text.Length==0 && !x.Formula)) continue;
            var row = new CalibrationImportRow { RowNumber=number, Code=code.Text.ToUpperInvariant(), Name=name.Text,
                Location=location.Text, CalibrationMethod=method.Text,
                MeasurementSpecification=Get($"F{number}").Text, Precision=Get($"G{number}").Text, Remarks=Get($"AB{number}").Text, CalibrationStandard=Get($"AC{number}").Text, AcceptanceCriteria=Get($"AD{number}").Text, RawCycle=cycle.Text, RawLastDate=last.Text, RawNextDate=next.Text };
            rows.Add(row);
            if (row.Code.Length is <1 or >80) row.Errors.Add("儀器編號必填，最多 80 字。");
            if (row.Name.Length is <1 or >200) row.Errors.Add("儀器名稱必填，最多 200 字。");
            if (row.Location.Length > 100) row.Errors.Add("放置地點最多 100 字。");
            if (row.CalibrationMethod.Length > 100) row.Errors.Add("校驗方式最多 100 字。");
            if (code.Formula || name.Formula || cycle.Formula || location.Formula) row.Errors.Add("編號、名稱、放置地點、週期需為固定值，請先將公式轉成值。");
            if (method.Formula)
            {
                if (method.Text.Length==0) row.Errors.Add("校驗方式公式沒有儲存值，請在 Excel 重算儲存。");
                else row.Warnings.Add("校驗方式使用公式儲存值，請確認 Excel 已重算儲存。");
            }
            foreach (var (column,label) in detailColumns)
            {
                var cell=Get($"{column}{number}");
                if (cell.Text.Length>0 && Compact(Get(column+"5").Text)!=label) row.Errors.Add($"{column}5 缺少「{label}」表頭，無法安全讀取資料。");
                if (cell.Text.Length>2000) row.Errors.Add($"{label}最多 2000 字。");
                if (cell.Type=="e" || (cell.Formula && !cell.HasValue)) row.Errors.Add($"{label}公式無有效儲存值，請在 Excel 重算儲存。");
                else if (cell.Formula) row.Warnings.Add($"{label}使用公式儲存值，請確認 Excel 已重算儲存。");
            }
            var match = Regex.Match(Compact(cycle.Text), @"^1次/(?<n>\d*)(?<unit>年|月)$", RegexOptions.CultureInvariant);
            if (match.Success && int.TryParse(match.Groups["n"].Value.Length==0 ? "1" : match.Groups["n"].Value,out var count) && count>0 && count<=120)
            {
                var months = count * (match.Groups["unit"].Value=="年" ? 12 : 1);
                if (months<=120) row.CycleMonths=months;
            }
            if (!row.CycleMonths.HasValue)
            {
                var raw = Compact(cycle.Text);
                if (raw is "--" or "免校" or "")
                {
                    row.CycleMonths = 12;
                    row.Warnings.Add("校驗週期無法辨識或為免校/--，先以 12 月建檔，請自行修正。");
                }
                else row.Errors.Add("校驗週期無法辨識或超過 1～120 月；請改為 1次/N年或1次/N月。");
            }
            row.LastCalibrationDate=Date(last,"上次校正日",false,date1904,row);
            row.NextCalibrationDate=Date(next,"下次到期日",false,date1904,row);
            if (row.LastCalibrationDate>CalibrationRules.Today(clock.GetUtcNow())) row.Errors.Add("上次校正不可為未來日期。");
            if (row.LastCalibrationDate.HasValue && row.NextCalibrationDate<=row.LastCalibrationDate) row.Errors.Add("下次日期必須晚於上次日期。");
            if (row.CycleMonths.HasValue && row.LastCalibrationDate.HasValue && row.NextCalibrationDate.HasValue)
            {
                var lastDate = row.LastCalibrationDate.Value;
                if (lastDate.Year + (lastDate.Month - 1 + row.CycleMonths.Value)/12 <= 9999 &&
                    CalibrationRules.NextDue(lastDate,row.CycleMonths.Value)!=row.NextCalibrationDate)
                    row.Warnings.Add("下次日期與月週期推算不同；保留 Excel 的明確到期日。");
            }
        }
        foreach (var group in rows.Where(x=>x.Code.Length>0).GroupBy(x=>x.Code).Where(x=>x.Count()>1))
            foreach (var row in group) row.Errors.Add("同檔儀器編號重複，請先排除重複資料。");
        foreach (var row in rows) row.Status = row.Errors.Count>0 ? "Invalid" : "Ready";
        if (rows.Count==0) throw new CalibrationException("第一個工作表沒有儀器資料。");
        return new(Convert.ToHexString(SHA256.HashData(bytes)), (string?)first.Attribute("name") ?? "", rows);
    }

    private static string Compact(string value) => Regex.Replace(value,@"\s+", "");
    private static DateOnly? Date(Cell cell,string label,bool required,bool date1904,CalibrationImportRow row)
    {
        if (cell.Formula)
        {
            if (!cell.HasValue || cell.Text.Length==0) { row.Errors.Add($"{label}公式沒有儲存值，請在 Excel 重算儲存。"); return null; }
            row.Warnings.Add($"{label}使用公式儲存值，請確認 Excel 已重算儲存。");
        }
        if (cell.Text.Length==0 || IsDeferredDate(cell.Text))
        {
            if (cell.Text.Length>0) row.Warnings.Add($"{label}「{cell.Text}」先留空，請匯入後自行建立日期。");
            if (required) row.Errors.Add($"{label}必填。");
            return null;
        }
        if (cell.Type is "n" && double.TryParse(cell.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var serial))
        {
            if (double.IsFinite(serial) && serial==Math.Truncate(serial) && serial>=(date1904 ? 0 : 1) && serial<3000000 && (date1904 || serial!=60))
            {
                try
                {
                    var origin = date1904 ? new DateOnly(1904,1,1) : new DateOnly(1899,12,31);
                    return origin.AddDays((int)serial - (!date1904 && serial>60 ? 1 : 0));
                }
                catch (ArgumentOutOfRangeException) { /* reported below as a row validation error */ }
            }
        }
        else if (cell.Type is "s" or "inlineStr" or "str" or "d" &&
                 DateOnly.TryParseExact(cell.Text,DateFormats,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date) && date!=default)
            return date;
        row.Errors.Add($"{label}「{cell.Text}」無法辨識，請填明確西元年月日。");
        return null;
    }
    private static bool IsDeferredDate(string text)
    {
        var compact = Compact(text);
        return compact is "-" or "--" or "未校" || compact.StartsWith("預計", StringComparison.Ordinal);
    }
}
