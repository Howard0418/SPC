using MesSpc.Api.Services;
using Xunit;

namespace MesSpc.Api.Tests;

public sealed class DatabaseRefreshHandoffTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private const string Digest = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void SeparateInstancesShareOneJobAndResult()
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var ticket = tickets.Issue(27, Digest);
        var api = new DatabaseRefreshHandoff(directory);
        var runner = new DatabaseRefreshHandoff(directory);

        Assert.Null(api.Submit(tickets, 28, ticket, Digest));
        Assert.Equal("queued", api.Submit(tickets, 27, ticket, Digest)?.Status);
        Assert.Equal(ticket, api.Submit(tickets, 27, ticket, Digest)?.Id);
        Assert.Null(api.Find(28, ticket));
        Assert.Equal("running", runner.Claim()?.Status);
        Assert.Null(runner.Claim());
        Assert.True(runner.Complete(ticket, false));
        Assert.False(runner.Complete(ticket, true));
        Assert.Equal("failed", api.Find(27, ticket)?.Status);
    }

    [Fact]
    public void BusyHandoffPreservesNextConfirmation()
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var first = tickets.Issue(27, Digest);
        var second = tickets.Issue(28, Digest);
        var api = new DatabaseRefreshHandoff(directory);
        Assert.NotNull(api.Submit(tickets, 27, first, Digest));
        Assert.Null(api.Submit(tickets, 28, second, Digest));
        Assert.NotNull(api.Claim());
        Assert.True(api.Complete(first, true));
        Assert.Equal("succeeded", api.Find(27, first)?.Status);
        Assert.NotNull(api.Submit(tickets, 28, second, Digest));
    }

    [Fact]
    public void LiveHandoffRejectsAnotherJobForAnAlreadyProcessedProof()
    {
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var handoff = new DatabaseRefreshHandoff(directory);
        var first = tickets.Issue(27, Digest);
        Assert.NotNull(handoff.Submit(tickets, 27, first, Digest, singleJobPerDigest: true));
        Assert.NotNull(handoff.Claim());
        Assert.True(handoff.Complete(first, true));
        Assert.NotNull(handoff.Submit(tickets, 27, first, Digest, singleJobPerDigest: true));
        var second = tickets.Issue(27, Digest);
        Assert.Null(handoff.Submit(tickets, 27, second, Digest, singleJobPerDigest: true));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
