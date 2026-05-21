using System.Text.Json;

namespace MES.SPC.E2ETests.Utilities;

public sealed class E2ETestRunContext
{
    public string Suffix { get; } = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    public string GroupCode => $"E2E-GRP-{Suffix}";
    public string GroupName => $"E2E大分類-{Suffix}";
    public string CategoryCode => $"E2E-CAT-{Suffix}";
    public string CategoryName => $"E2E中分類-{Suffix}";
    public string TypeCode => $"E2E-TYPE-{Suffix}";
    public string TypeName => $"E2E小分類-{Suffix}";
    public string OperatorCode => $"E2E-OP-{Suffix}";
    public string OperatorName => $"E2E作業員-{Suffix}";

    public int? GroupId { get; set; }
    public int? CategoryId { get; set; }
    public int? TypeId { get; set; }
    public int? OperatorId { get; set; }
    public Guid? UploadBatchId { get; set; }
}

public static class E2ETestDataBuilder
{
    public static string BuildVariableImportJson(string operatorCode, int rowCount = 25)
    {
        var rows = new List<Dictionary<string, string>>();
        var lot = $"L-E2E-FLOW-{DateTime.UtcNow:yyyyMMddHHmmss}";
        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PartNo"] = "P-1001",
                ["ProcessCode"] = "ST-01",
                ["MachineCode"] = "M-01",
                ["CharacteristicCode"] = "LEN-001",
                ["MeasuredValue"] = (10.0 + i * 0.01).ToString("F2"),
                ["MeasuredAt"] = DateTime.UtcNow.AddMinutes(-rowCount + i).ToString("yyyy-MM-ddTHH:mm:ss"),
                ["Operator"] = operatorCode,
                ["LotNo"] = lot,
                ["SampleNo"] = ((i % 5) + 1).ToString()
            });
        }
        return JsonSerializer.Serialize(rows);
    }
}
