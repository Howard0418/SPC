namespace MES.SPC.E2ETests.Utilities;

public static class E2EUploadHelper
{
    /// <summary>含 1 筆錯誤（空料號）與 2 筆有效列的計量型 JSON 測試資料。</summary>
    public static string MixedVariableJson =>
        """
        [
          {"PartNo":"","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"LEN-001","MeasuredValue":"10.12","MeasuredAt":"2026-05-21T08:00:00","Operator":"OP-01","LotNo":"L-E2E-ERR-001","SampleNo":"1"},
          {"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"DEF-001","MeasuredValue":"0.05","MeasuredAt":"2026-05-21T08:01:00","Operator":"OP-01","LotNo":"L-E2E-ERR-002","SampleNo":"1"},
          {"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"LEN-001","MeasuredValue":"10.05","MeasuredAt":"2026-05-21T08:02:00","Operator":"OP-01","LotNo":"L-E2E-OK-001","SampleNo":"1"}
        ]
        """;

    /// <summary>全數通過校驗的計量型資料，用於確認匯入流程。</summary>
    public static string ValidVariableJson =>
        """
        [
          {"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"LEN-001","MeasuredValue":"10.02","MeasuredAt":"2026-05-21T09:00:00","Operator":"E2E-OP","LotNo":"L-E2E-OK-ONLY","SampleNo":"1"},
          {"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"LEN-001","MeasuredValue":"10.04","MeasuredAt":"2026-05-21T09:10:00","Operator":"E2E-OP","LotNo":"L-E2E-OK-ONLY","SampleNo":"2"}
        ]
        """;
}
