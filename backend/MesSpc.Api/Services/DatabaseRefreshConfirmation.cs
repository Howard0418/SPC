namespace MesSpc.Api.Services;

public sealed class DatabaseRefreshConfirmation(TimeProvider clock)
{
    public const string TargetDatabase = "PMR_SPC_TEST";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly object gate = new();
    private readonly Dictionary<int, Ticket> pending = new();

    public Guid Issue(int operatorId, string preflightDigest)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(operatorId);
        if (preflightDigest is null || preflightDigest.Length != 64 ||
            !preflightDigest.All(Uri.IsHexDigit))
            throw new ArgumentException("Expected a SHA-256 digest.", nameof(preflightDigest));

        lock (gate)
        {
            var now = clock.GetUtcNow();
            foreach (var expired in pending.Where(entry => entry.Value.ExpiresAt <= now).ToArray())
                pending.Remove(expired.Key);
            var ticket = new Ticket(Guid.NewGuid(), preflightDigest, now + Lifetime);
            pending[operatorId] = ticket;
            return ticket.Id;
        }
    }

    public bool TryConsume(int operatorId, Guid id, string targetDatabase, string preflightDigest)
    {
        if (!string.Equals(targetDatabase, TargetDatabase, StringComparison.Ordinal))
            return false;

        lock (gate)
        {
            if (!pending.TryGetValue(operatorId, out var ticket))
                return false;
            if (ticket.ExpiresAt <= clock.GetUtcNow())
            {
                pending.Remove(operatorId);
                return false;
            }
            if (ticket.Id != id || !string.Equals(ticket.PreflightDigest, preflightDigest, StringComparison.Ordinal))
                return false;

            pending.Remove(operatorId);
            return true;
        }
    }

    private sealed record Ticket(Guid Id, string PreflightDigest, DateTimeOffset ExpiresAt);
}
