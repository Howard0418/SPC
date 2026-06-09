using MesSpc.Api.Domain.Entities;
using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Xunit;

namespace MesSpc.Api.Tests.SpcEngine.Rules;

public class SpcRuleEngineTests
{
    private readonly double _cl = 100.0;
    private readonly double _ucl = 130.0;
    private readonly double _lcl = 70.0;
    // Sigma = 10.0
    // 1 Sigma limits: 90, 110
    // 2 Sigma limits: 80, 120
    // 3 Sigma limits: 70, 130

    private SpcRule CreateRule(string ruleCode) => new SpcRule { RuleCode = ruleCode, RuleName = ruleCode, IsEnabled = true };

    [Fact]
    public void EvaluateRules_Rule1_Over3Sigma_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule1_Over3Sigma") };
        var values = new List<double> { 100, 105, 135 }; // 135 is > UCL (130)

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.NotNull(violations);
        Assert.Single(violations);
        Assert.Equal("Rule1_Over3Sigma", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule1_Over3Sigma_BelowLcl_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule1_Over3Sigma") };
        var values = new List<double> { 100, 105, 65 }; // 65 is < LCL (70)

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
    }

    [Fact]
    public void EvaluateRules_Rule2_9SameSide_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule2_9SameSide") };
        // 9 points above CL (100)
        var values = new List<double> { 105, 102, 103, 104, 101, 106, 102, 103, 105 };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule2_9SameSide", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule2_9SameSide_NoViolationIf8Points()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule2_9SameSide") };
        // 8 points above CL
        var values = new List<double> { 105, 102, 103, 104, 101, 106, 102, 103 };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void EvaluateRules_Rule3_6Trend_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule3_6Trend") };
        // 6 points steadily increasing
        var values = new List<double> { 100, 101, 102, 103, 104, 105 };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule3_6Trend", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule4_14Alternating_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule4_14Alternating") };
        // 14 points alternating up and down
        var values = new List<double> { 
            100, 105, 95, 105, 95, 105, 95, 
            105, 95, 105, 95, 105, 95, 105 
        };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule4_14Alternating", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule5_2Of3Over2Sigma_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule5_2Of3Over2Sigma") };
        // CL=100, UCL=130 => Sigma=10. 2 Sigma=120.
        // 2 out of 3 points > 120 (same side)
        var values = new List<double> { 100, 125, 105, 122 }; // The last 3 are 125, 105, 122 (125 and 122 are > 120)

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule5_2Of3Over2Sigma", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule6_4Of5Over1Sigma_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule6_4Of5Over1Sigma") };
        // CL=100, Sigma=10. 1 Sigma=110.
        // 4 out of 5 points > 110 (same side)
        var values = new List<double> { 115, 112, 118, 105, 111 }; // 115, 112, 118, 111 are > 110

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule6_4Of5Over1Sigma", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule7_15Within1Sigma_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule7_15Within1Sigma") };
        // CL=100, Sigma=10. Limits: 90 ~ 110
        // 15 consecutive points between 90 and 110
        var values = new List<double>();
        for (int i = 0; i < 15; i++)
        {
            values.Add(100 + (i % 2 == 0 ? 5 : -5)); // Oscillates between 105 and 95
        }

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule7_15Within1Sigma", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule8_8Outside1Sigma_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> { CreateRule("Rule8_8Outside1Sigma") };
        // CL=100, Sigma=10. Limits: 90 and 110. Outside means > 110 or < 90.
        // 8 consecutive points outside 1 sigma (either side)
        var values = new List<double> { 
            115, 85, 112, 88, 118, 82, 111, 89 
        };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule8_8Outside1Sigma", violations[0].RuleCode);
    }

    [Fact]
    public void EvaluateRules_Rule2_CustomRequiredPoints_ReturnsViolation()
    {
        // Arrange
        var rules = new List<SpcRule> 
        { 
            new SpcRule 
            { 
                RuleCode = "Rule2_9SameSide", 
                RuleName = "Rule2_9SameSide", 
                IsEnabled = true,
                RuleConfigJson = "{\"RequiredPoints\": 7}"
            } 
        };
        // 7 points above CL (100)
        var values = new List<double> { 105, 102, 103, 104, 101, 106, 102 };

        // Act
        var violations = SpcRuleEngine.EvaluateRules(values, rules, _cl, _ucl, _lcl);

        // Assert
        Assert.Single(violations);
        Assert.Equal("Rule2_9SameSide", violations[0].RuleCode);
    }
}
