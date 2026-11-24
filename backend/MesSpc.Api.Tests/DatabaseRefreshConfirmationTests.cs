using MesSpc.Api.Services;
using Xunit;

namespace MesSpc.Api.Tests;

public sealed class DatabaseRefreshConfirmationTests
{
    private const string Digest = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string Target = "PMR_SPC_TEST";

    [Fact]
    public void WrongDetailsLeaveTicketAvailableToOriginalOperator()
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var ticket = tickets.Issue(27, Digest);
        Assert.False(tickets.TryConsume(28, ticket, Target, Digest));
        Assert.False(tickets.TryConsume(27, ticket, "PMR_SPC_2026", Digest));
        Assert.False(tickets.TryConsume(27, ticket, Target, new string('B', 64)));
        Assert.True(tickets.TryConsume(27, ticket, Target, Digest));
        Assert.False(tickets.TryConsume(27, ticket, Target, Digest));
    }

    [Fact]
    public async Task ConcurrentConfirmationSucceedsOnce()
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var ticket = tickets.Issue(27, Digest);
        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => tickets.TryConsume(27, ticket, Target, Digest))));
        Assert.Single(results, result => result);
    }

    [Fact]
    public void OnlyLatestTicketIsValidAndExpirationBoundaryRejects()
    {
        var clock = new TestClock();
        var tickets = new DatabaseRefreshConfirmation(clock);
        var previous = tickets.Issue(27, Digest);
        var latest = tickets.Issue(27, Digest);
        Assert.False(tickets.TryConsume(27, previous, Target, Digest));
        clock.Now = clock.Now.AddMinutes(10);
        Assert.False(tickets.TryConsume(27, latest, Target, Digest));
    }

    [Theory]
    [InlineData(0, Digest)]
    [InlineData(27, "invalid")]
    public void InvalidPreflightCannotIssueTicket(int operatorId, string digest)
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        Assert.ThrowsAny<ArgumentException>(() => tickets.Issue(operatorId, digest));
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
