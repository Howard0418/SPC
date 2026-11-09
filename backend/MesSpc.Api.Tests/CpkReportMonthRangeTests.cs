using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class CpkReportMonthRangeTests
{
    [Fact]
    public void Parse_UsesTaipeiMonthAndExclusiveUtcUpperBound()
    {
        var range = CpkReportMonthRange.Parse("2026-10", new DateTime(2026, 11, 9, 3, 0, 0, DateTimeKind.Utc));

        Assert.Equal("2026-10", range.Label);
        Assert.Equal(new DateTime(2026, 9, 30, 16, 0, 0, DateTimeKind.Utc), range.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 31, 16, 0, 0, DateTimeKind.Utc), range.UntilUtc);
    }

    [Fact]
    public void Parse_WithoutMonthUsesCurrentTaipeiMonth()
    {
        var range = CpkReportMonthRange.Parse(null, new DateTime(2026, 10, 31, 16, 30, 0, DateTimeKind.Utc));

        Assert.Equal("2026-11", range.Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026/10")]
    [InlineData("2026-13")]
    [InlineData("26-10")]
    public void Parse_InvalidMonthIsRejected(string month)
    {
        Assert.Throws<ArgumentException>(() => CpkReportMonthRange.Parse(month, DateTime.UtcNow));
    }
}
