using System.Text.Json;

namespace MesSpc.Api.Services;

public sealed class DatabaseRefreshHandoff(string directory)
{
    public sealed record Job(Guid Id, int OperatorId, string PreflightDigest, string Status)
    {
        public string TargetDatabase => DatabaseRefreshConfirmation.TargetDatabase;
    }

    public Job? Submit(DatabaseRefreshConfirmation tickets, int operatorId, Guid ticket, string digest,
        bool singleJobPerDigest = false)
    {
        using var lease = Acquire();
        var existing = Read(ticket);
        if (existing is not null)
            return existing.OperatorId == operatorId && existing.PreflightDigest == digest ? existing : null;

        if ((singleJobPerDigest && Jobs().Any(job => job.PreflightDigest == digest)) || Active() is not null ||
            !tickets.TryConsume(operatorId, ticket, DatabaseRefreshConfirmation.TargetDatabase, digest))
            return null;

        return Write(new Job(ticket, operatorId, digest, "queued"));
    }

    public Job? Find(int operatorId, Guid id)
    {
        using var lease = Acquire();
        var job = Read(id);
        return job?.OperatorId == operatorId ? job : null;
    }

    public Job? Claim()
    {
        using var lease = Acquire();
        var job = Active();
        return job?.Status == "queued" ? Write(job with { Status = "running" }) : null;
    }

    public bool Complete(Guid id, bool succeeded)
    {
        using var lease = Acquire();
        var job = Read(id);
        if (job?.Status != "running")
            return false;

        Write(job with { Status = succeeded ? "succeeded" : "failed" });
        return true;
    }

    private FileStream Acquire()
    {
        Directory.CreateDirectory(directory);
        return new FileStream(Path.Combine(directory, "handoff.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
    }

    private IEnumerable<Job> Jobs() => Directory.EnumerateFiles(directory, "*.json")
        .Select(file => JsonSerializer.Deserialize<Job>(File.ReadAllText(file))
            ?? throw new InvalidDataException("Missing refresh job."));

    private Job? Active() => Jobs().SingleOrDefault(job => job.Status is "queued" or "running");

    private string PathFor(Guid id) => Path.Combine(directory, $"{id:N}.json");

    private Job? Read(Guid id)
    {
        var path = PathFor(id);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Job>(File.ReadAllText(path))
                ?? throw new InvalidDataException("Missing refresh job.")
            : null;
    }

    private Job Write(Job job)
    {
        var path = PathFor(job.Id);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(job));
        File.Move(temporary, path, overwrite: true);
        return job;
    }
}
