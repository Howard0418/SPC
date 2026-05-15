using MesSpc.Api.Services;
using Xunit;

namespace MesSpc.Api.Tests.Services;

public class FormulaEngineServiceTests
{
    private readonly FormulaEngineService _service = new();

    [Fact]
    public void EvaluateByExpression_ShouldEvaluateDynamicFormula()
    {
        var values = new double[] { 10, 12, 11 };
        var usl = 15.0;
        var lsl = 5.0;

        // Dynamic formula
        var expression = "((USL - AVG) + (AVG - LSL)) / 2";
        
        var result = _service.EvaluateByExpression(expression, values, usl, lsl);

        // AVG is 11. (15 - 11) + (11 - 5) / 2 = (4 + 6) / 2 = 5
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void EvaluateByExpression_ShouldSupportPredefinedNames()
    {
        var values = new double[] { 10, 12, 11 }; // AVG = 11, MAX = 12, MIN = 10, RANGE = 2, SUM = 33
        
        Assert.Equal(11.0, _service.EvaluateByExpression("AVG", values));
        Assert.Equal(12.0, _service.EvaluateByExpression("MAX", values));
        Assert.Equal(10.0, _service.EvaluateByExpression("MIN", values));
        Assert.Equal(2.0, _service.EvaluateByExpression("RANGE", values));
        Assert.Equal(33.0, _service.EvaluateByExpression("SUM", values));
    }

    [Fact]
    public void EvaluateByExpression_ShouldHandleBuiltInCpk()
    {
        var values = new double[] { 10, 12, 11, 13, 14 }; // stdev > 0
        var result = _service.EvaluateByExpression("CPK", values, 20, 0);
        Assert.True(result > 0);
    }
}
