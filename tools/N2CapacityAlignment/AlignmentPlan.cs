using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace N2CapacityAlignment;

public sealed record CapacityDefinition(
    int PpcId,
    string Machine,
    string Tank,
    string Characteristic,
    decimal Capacity,
    string OldToken,
    string NewToken,
    int ReplacementCount,
    string BeforeFormulaHash,
    string AfterFormulaHash,
    string SampleConcentration,
    string ExpectedAdjustmentAmount,
    string SampleBeforeFormula);

public sealed record AlignmentResult(string ConfigJson, string AmountFormula, bool Changed);

public static class AlignmentPlan
{
    public static IReadOnlyList<CapacityDefinition> Definitions { get; } =
    [
        new(2512, "N2", "調節", "P200", 600m, "*500", "*600", 3,
            "7EF76E55749B814BB616AC0634CCEA62568993F51A85EAEF0BCE65D35C8AA46B", "87AC90C208204F5E81ACBA610689A6781A45995DF1DB9AF97CFE3EF3C654CEB8",
            "140", "P200：6 L", "IFERROR(IF(Concentration<150,\"P200：\"&ROUND(((150-Concentration)*500/1000),1)&\" L\",IF(Concentration>165,\"排藥：\"&ROUND((Concentration-165)/Concentration*500,1)&\" L\"&\"\n補水：\"&ROUND((Concentration-165)/Concentration*500,1)&\" L\",\"\")),\"\")"),
        new(2514, "N2", "預浸", "P300", 250m, "*260/1000", "*250/1000", 1,
            "5580A08211908ADC7406AB97CA6BF6B2376F7E43DBB0D694D1918A8B0F96D88A", "A170B965C15B282AAF2BCCBBD76A25245AABAA6698CA1D090209113F7A932577",
            "250", "P300：2.5 L", "IFERROR(IF(Concentration<260,\"P300：\"&ROUND(((260-Concentration)*260/1000),1)&\" L\",IF(Concentration>260,\"排藥：\"&ROUND((Concentration-280)/Concentration*250,1)&\" L\"&\"補水：\"&ROUND((Concentration-280)/Concentration*250,1)&\" L\",\"\")),\"\")"),
        new(2515, "N2", "催化", "P400", 630m, "*380/1000", "*630/1000", 1,
            "B0DACB01B7CBC06D461468535C7DF7408EE0D364A12426F30E300C7C0A6285F4", "129AB463EC17458322BCE900B705B2C797253433A2E1728ABA11906ED31FA36C",
            "30", "P400：0.6 L", "IFERROR(IF(Concentration<31,\"P400：\"&ROUND(((31-Concentration)*380/1000),1)&\" L\",\"\"),\"\")"),
        new(2516, "N2", "催化", "氯化亞錫", 630m, "*380/1000", "*630/1000", 1,
            "47569B0DDFBA439DAA155722D34C7528198680883717D473DEF4698CA0154CAF", "3844C8EE78A3668D0EFFC7FD67B4D5101FAFF347D22DC66C2DCDA3F16B6C7BFD",
            "4", "安定劑：0.6 L", "IFERROR(IF(Concentration<5,\"安定劑：\"&ROUND(((5-Concentration)*380/1000),1)&\" L\",\"\"),\"\")"),
        new(2519, "N2", "化鎳", "鎳", 1200m, "*900", "*1200", 3,
            "96344A08DD9827623A1E62D571C24F95066EF9BEA1D70AA56F6BCF9BB84FFE11", "E1B2D50F167100F4ABD73F2AC49BA026CDCB3005EC3C594A3AE16A2869956A41",
            "4", "A劑：8.8 L", "IFERROR(IF(Concentration<4.8,\"A劑：\"&ROUND(((4.8-Concentration)*900*9.2/1000),1)&\" L\",IF(Concentration>5,\"排藥：\"&ROUND((Concentration-5)/Concentration*900,1)&\" L\"&\"\n補水：\"&ROUND((Concentration-5)/Concentration*900,1)&\" L\",\"\")),\"\")"),
        new(2550, "N2", "表處", "氫氧化鉀", 1000m, "*600", "*1000", 3,
            "EE982544825853C8A7C50094D6E3D30C73B1E092787DB6EFF375A2960700D533", "517BFFD0CEC36AE9D469703DF9B292F0A5A74208ABC0123B8C19D2BE340EC778",
            "19", "KOH：1.5 L", "IFERROR(IF(Concentration<20,\"KOH：\"&ROUND(((20-Concentration)*600/0.6525/1000),1)&\" L\",IF(Concentration>22,\"排藥：\"&ROUND((Concentration-22)/Concentration*600,1)&\" L\"&\"\n補水：\"&ROUND((Concentration-22)/Concentration*600,1)&\" L\",\"\")),\"\")"),
        new(2594, "N2", "化鎳", "亞磷酸鈉", 1200m, "*900", "*1200", 3,
            "FA37D7AC35BF9CC311DF724ACC0D31F2A21F50DCCC3205E9CC05AD4254124C84", "70C148F0CA8084713C4444FCD93BC59AA693EF775CF392A12C4ECB3F5D3ABABB",
            "18", "A劑：1 L", "IF(Concentration<18.5,\"A劑：\"&ROUND(((18.5-Concentration)*900*1.62/1000),1)&\" L\",IF(Concentration>20,\"排藥：\"&ROUND((Concentration-20)/Concentration*900,1)&\" L\"&\" 補水：\"&ROUND((Concentration-20)/Concentration*900,1)&\" L\",\"\"))")
    ];

    public static AlignmentResult AlignConfig(string configJson, CapacityDefinition definition)
    {
        var root = JsonNode.Parse(configJson)?.AsObject()
            ?? throw new InvalidOperationException($"PPC {definition.PpcId} config is not an object.");
        var capacity = root["tankCapacityLiters"]?.GetValue<decimal?>();
        if (capacity != definition.Capacity)
            throw new InvalidOperationException($"PPC {definition.PpcId} tank capacity changed from approved {definition.Capacity.ToString(CultureInfo.InvariantCulture)}.");
        var formula = root["adjustmentAmountFormula"]?.GetValue<string>() ?? "";
        var currentHash = Hash(formula);
        if (currentHash == definition.AfterFormulaHash)
            return new(root.ToJsonString(), formula, false);
        if (currentHash != definition.BeforeFormulaHash)
            throw new InvalidOperationException($"PPC {definition.PpcId} amount formula differs from the reviewed source.");
        if (Count(formula, definition.OldToken) != definition.ReplacementCount)
            throw new InvalidOperationException($"PPC {definition.PpcId} reviewed token count changed.");
        var aligned = formula.Replace(definition.OldToken, definition.NewToken, StringComparison.Ordinal);
        if (Hash(aligned) != definition.AfterFormulaHash)
            throw new InvalidOperationException($"PPC {definition.PpcId} candidate hash differs from the approved result.");
        root["adjustmentAmountFormula"] = aligned;
        return new(root.ToJsonString(), aligned, true);
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static int Count(string source, string token)
    {
        var count = 0;
        for (var index = 0; (index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0; index += token.Length) count++;
        return count;
    }
}
