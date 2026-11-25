using System.Security.Claims;
using System.Security.Cryptography;

namespace MesSpc.Api.Services;

public static class DatabaseRefreshRehearsalApi
{
    private static readonly string Digest = Convert.ToHexString(
        SHA256.HashData("SPC synthetic refresh rehearsal v1"u8));

    public static bool CanRegister(string hostEnvironment, string? appEnvironment,
        bool authEnabled, bool enabled, int operatorId) =>
        string.Equals(hostEnvironment, Environments.Development, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(appEnvironment, "local", StringComparison.OrdinalIgnoreCase) &&
        authEnabled && enabled && operatorId > 0;

    public static void MapDatabaseRefreshRehearsal(this WebApplication app, bool authEnabled)
    {
        var settings = app.Configuration.GetSection("DatabaseRefreshRehearsal");
        var operatorId = settings.GetValue<int>("OperatorId");
        if (!CanRegister(app.Environment.EnvironmentName, app.Configuration["AppEnvironment"],
            authEnabled, settings.GetValue<bool>("Enabled"), operatorId))
            return;

        var queue = settings["QueueDirectory"];
        if (string.IsNullOrWhiteSpace(queue) || !Path.IsPathFullyQualified(queue) || !Directory.Exists(queue))
            throw new InvalidOperationException("The rehearsal queue must be an existing absolute directory.");

        var tickets = new DatabaseRefreshConfirmation(TimeProvider.System);
        var handoff = new DatabaseRefreshHandoff(queue);
        var routes = app.MapGroup("/api/database-refresh-rehearsal")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == operatorId));

        routes.MapPost("/preflight", () => Results.Ok(new
        {
            mode = "synthetic-rehearsal",
            confirmationId = tickets.Issue(operatorId, Digest)
        }));

        routes.MapPost("/jobs/{confirmationId:guid}", (Guid confirmationId) =>
        {
            var job = handoff.Submit(tickets, operatorId, confirmationId, Digest);
            return job is null
                ? Results.Conflict()
                : Results.Accepted($"/api/database-refresh-rehearsal/jobs/{job.Id}", new { job.Id, job.Status });
        });

        routes.MapGet("/jobs/{id:guid}", (Guid id) =>
        {
            var job = handoff.Find(operatorId, id);
            return job is null ? Results.NotFound() : Results.Ok(new { job.Id, job.Status });
        });
    }
}
