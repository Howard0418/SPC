using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace MesSpc.Api.Services;

public static class DatabaseRefreshLiveApi
{
    public const string SourceDatabase = "PMR_SPC_2026";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record Preflight(Guid OperationId, string Source, string Target, DateTimeOffset SnapshotUtc,
        int ReplacementTables, long MeasurementRows, int Blockers);

    public static bool IsReady(Preflight? value, DateTimeOffset now) =>
        value is not null && value.OperationId != Guid.Empty &&
        value.Source == SourceDatabase && value.Target == DatabaseRefreshConfirmation.TargetDatabase &&
        value.SnapshotUtc <= now && value.SnapshotUtc > now.AddMinutes(-15) &&
        value.ReplacementTables == 69 && value.MeasurementRows > 0 && value.Blockers == 0;

    public static bool CanRegister(string hostEnvironment, string? appEnvironment, bool authEnabled,
        bool enabled, int operatorId) =>
        hostEnvironment == Environments.Production && appEnvironment == "production" &&
        authEnabled && enabled && operatorId > 0;

    public static void MapDatabaseRefreshLive(this WebApplication app, bool authEnabled)
    {
        var config = app.Configuration.GetSection("DatabaseRefreshLive");
        var operatorId = config.GetValue<int>("OperatorId");
        if (!CanRegister(app.Environment.EnvironmentName, app.Configuration["AppEnvironment"],
            authEnabled, config.GetValue<bool>("Enabled"), operatorId))
            return;

        var queue = ResolveDirectory(config["QueueDirectory"]);
        var directory = ResolveDirectory(config["PreflightDirectory"]);
        if (IsWithin(directory, queue) || IsWithin(queue, directory))
            throw new InvalidOperationException("Refresh preflight and queue must use separate directories.");

        var handoff = new DatabaseRefreshHandoff(queue);
        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var routes = app.MapGroup("/api/database-refresh")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) && id == operatorId));

        routes.MapGet("/availability", () => Results.Ok(new { available = true }));
        routes.MapPost("/preflight", () =>
        {
            var proof = LoadPreflight(directory);
            if (proof is null)
                return Results.Conflict();

            var (snapshot, digest) = proof.Value;
            return Results.Ok(new { snapshot, confirmationId = tickets.Issue(operatorId, digest) });
        });

        routes.MapPost("/jobs/{confirmationId:guid}", (Guid confirmationId, string target) =>
        {
            if (target != DatabaseRefreshConfirmation.TargetDatabase)
                return Results.BadRequest();

            var existing = handoff.Find(operatorId, confirmationId);
            if (existing is not null)
                return Accepted(existing);

            var proof = LoadPreflight(directory);
            if (proof is null)
                return Results.Conflict();

            var job = handoff.Submit(tickets, operatorId, confirmationId, proof.Value.Digest, singleJobPerDigest: true);
            if (job is null)
                return Results.Conflict();

            app.Logger.LogInformation("Database refresh {JobId} queued by operator {OperatorId}", job.Id, operatorId);
            return Accepted(job);
        });

        routes.MapGet("/jobs/{id:guid}", (Guid id) =>
        {
            var job = handoff.Find(operatorId, id);
            return job is null ? Results.NotFound() : Results.Ok(new { job.Id, job.Status });
        });
    }

    private static IResult Accepted(DatabaseRefreshHandoff.Job job) =>
        Results.Accepted($"/api/database-refresh/jobs/{job.Id}", new { job.Id, job.Status });

    private static bool IsWithin(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static string ResolveDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || !Directory.Exists(path))
            throw new InvalidOperationException("Refresh requires existing absolute directories.");

        var directory = new DirectoryInfo(Path.GetFullPath(path));
        for (var part = directory; part is not null; part = part.Parent)
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Refresh directory cannot use reparse points.");
        return Path.TrimEndingDirectorySeparator(directory.FullName);
    }

    private static (Preflight Snapshot, string Digest)? LoadPreflight(string directory)
    {
        var path = Path.Combine(directory, "preflight.json");
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                return null;
            using var stream = File.OpenRead(path);
            if (stream.Length is <= 0 or > 4096)
                return null;
            var data = new byte[(int)stream.Length];
            stream.ReadExactly(data);
            var snapshot = JsonSerializer.Deserialize<Preflight>(data, JsonOptions);
            return IsReady(snapshot, TimeProvider.System.GetUtcNow())
                ? (snapshot!, Convert.ToHexString(SHA256.HashData(data)))
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}
