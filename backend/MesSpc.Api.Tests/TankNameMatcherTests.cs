using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public class TankNameMatcherTests
{
    private sealed record Candidate(string Name, string Code);

    [Theory]
    [InlineData("清潔槽", "清潔")]
    [InlineData("TANK-000255", "000255")]
    [InlineData("表處", "表面處理")]
    public void NormalizedEquivalentNamesAreExactMatches(string source, string target)
        => Assert.Equal(1d, TankNameMatcher.Similarity(source, target));

    [Fact]
    public void UniqueHighScoreCandidateCanAutoMatch()
    {
        var candidates = new[]
        {
            new Candidate("清潔", "TANK-000255"),
            new Candidate("鍍銅", "TANK-000256")
        };
        var ranked = TankNameMatcher.Rank("清潔槽", candidates, x => x.Name, x => x.Code);
        Assert.Equal("清潔", TankNameMatcher.SelectUniqueAutoMatch(ranked)?.Name);
    }

    [Fact]
    public void SimilarCandidatesWithoutClearWinnerDoNotAutoMatch()
    {
        var candidates = new[]
        {
            new Candidate("酸洗1", "TANK-000255"),
            new Candidate("酸洗2", "TANK-000256")
        };
        var ranked = TankNameMatcher.Rank("酸洗", candidates, x => x.Name, x => x.Code);
        Assert.Null(TankNameMatcher.SelectUniqueAutoMatch(ranked));
    }
}
