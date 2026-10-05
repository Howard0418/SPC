using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Security;
using System.Text.Json;

namespace MesSpc.Api.Controllers;

[ApiController, Route("api/v1/operators/directory-comparison"), Authorize(Roles = UserRoles.Editor)]
public class OperatorDirectoryComparisonController(AppDbContext db, IHttpClientFactory clients, IConfiguration configuration,
    ILogger<OperatorDirectoryComparisonController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PortalDirectory:ApiBaseUrl"]?.TrimEnd('/');
        var key = configuration["Auth:PortalSsoKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(key))
            return StatusCode(503, new { message = "Portal 人員比對服務尚未設定。" });

        var client = clients.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/spc-directory/quality-users");
        request.Headers.TryAddWithoutValidation("X-SPC-Sync-Key", key);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return StatusCode(502, new { message = $"Portal 人員比對服務回應失敗（{(int)response.StatusCode}）。" });
        var portalUsers = await response.Content.ReadFromJsonAsync<List<PortalUser>>(cancellationToken: cancellationToken) ?? [];
        var operators = await db.Operators.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var used = new HashSet<int>();
        var rows = new List<object>();

        foreach (var portal in portalUsers)
        {
            var candidates = operators.Where(x =>
                Same(x.Username, portal.AdAccount) || Same(x.OperatorCode, portal.EmployeeNo)).ToList();
            if (candidates.Count > 1)
            {
                rows.Add(ToRow("Conflict", portal, null, "AD 帳號與工號對應到不同 SPC 人員"));
                continue;
            }
            if (candidates.Count == 0)
            {
                rows.Add(ToRow("PortalOnly", portal, null, "尚未建立 SPC 人員"));
                continue;
            }
            var op = candidates[0]; used.Add(op.Id);
            var different = !Same(op.Username, portal.AdAccount) || !Same(op.OperatorCode, portal.EmployeeNo)
                || !Same(op.OperatorName, portal.DisplayName) || !Same(op.Department, portal.Department)
                || !Same(op.Email, portal.Email);
            rows.Add(ToRow(different ? "Different" : "Matched", portal, op,
                different ? "基本資料不同；SPC 權限不列入差異" : null));
        }
        foreach (var op in operators.Where(x => !used.Contains(x.Id)))
            rows.Add(ToRow("SpcOnly", null, op, "Portal 品保人員中沒有對應資料"));

        return Ok(new { comparedAt = DateTime.UtcNow, portalCount = portalUsers.Count, spcCount = operators.Count,
            summary = rows.GroupBy(x => (string)x.GetType().GetProperty("status")!.GetValue(x)!).ToDictionary(x => x.Key, x => x.Count()), rows });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(SyncRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 100)
            return BadRequest(new { message = "請選擇 1 至 100 位人員。" });
        var portalResult = await LoadPortalUsers(cancellationToken);
        if (portalResult.Error is not null) return portalResult.Error;
        var portalUsers = portalResult.Users!;
        var results = new List<object>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in request.Items.DistinctBy(x => x.PortalUserId))
        {
            var portal = portalUsers.FirstOrDefault(x => x.PortalUserId == item.PortalUserId);
            if (portal is null) { results.Add(new { item.PortalUserId, success = false, message = "Portal 品保人員不存在。" }); continue; }
            var candidates = await db.Operators.Where(x =>
                x.Username == portal.AdAccount || (!string.IsNullOrWhiteSpace(portal.EmployeeNo) && x.OperatorCode == portal.EmployeeNo)).ToListAsync(cancellationToken);
            Domain.Entities.Operator? op = null;
            if (item.SpcOperatorId.HasValue)
                op = await db.Operators.FindAsync([item.SpcOperatorId.Value], cancellationToken);
            else if (candidates.Count == 1)
                op = candidates[0];
            else if (candidates.Count > 1)
            { results.Add(new { item.PortalUserId, success = false, message = "資料衝突，請先指定正確的 SPC 人員。" }); continue; }

            var code = string.IsNullOrWhiteSpace(portal.EmployeeNo) ? portal.AdAccount.ToUpperInvariant() : portal.EmployeeNo.Trim();
            var account = portal.AdAccount.Trim().ToLowerInvariant();
            var collision = await db.Operators.AsNoTracking().AnyAsync(x =>
                (x.OperatorCode == code || x.Username == account) && (op == null || x.Id != op.Id), cancellationToken);
            if (collision) { results.Add(new { item.PortalUserId, success = false, message = "AD 帳號或工號已被其他 SPC 人員使用。" }); continue; }

            var before = op is null ? null : JsonSerializer.Serialize(new { op.Id, op.OperatorCode, op.OperatorName, op.Department, op.Email, op.Username, op.Role, op.IsActive });
            var action = op is null ? "Created" : "Updated";
            if (op is null)
            {
                op = new Domain.Entities.Operator { OperatorCode = code, OperatorName = portal.DisplayName?.Trim() ?? account,
                    Department = portal.Department?.Trim(), Email = portal.Email?.Trim(), Username = account,
                    Role = UserRoles.Viewer, IsActive = true, CreatedBy = User.Identity?.Name ?? "DirectorySync" };
                db.Operators.Add(op);
            }
            else
            {
                op.OperatorCode = code; op.Username = account; op.OperatorName = portal.DisplayName?.Trim() ?? account;
                op.Department = portal.Department?.Trim(); op.Email = portal.Email?.Trim();
                op.UpdatedAt = DateTime.UtcNow; op.UpdatedBy = User.Identity?.Name ?? "DirectorySync";
            }
            await db.SaveChangesAsync(cancellationToken);
            var after = JsonSerializer.Serialize(new { op.Id, op.OperatorCode, op.OperatorName, op.Department, op.Email, op.Username, op.Role, op.IsActive });
            logger.LogInformation("Portal SPC operator sync. Action={Action}, PortalUserId={PortalUserId}, OperatorId={OperatorId}, ExecutedBy={ExecutedBy}, Before={Before}, After={After}",
                action, portal.PortalUserId, op.Id, User.Identity?.Name, before, after);
            results.Add(new { item.PortalUserId, spcOperatorId = op.Id, success = true, action });
        }
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { results, succeeded = results.Count(x => (bool)x.GetType().GetProperty("success")!.GetValue(x)!) });
    }

    private async Task<(List<PortalUser>? Users, IActionResult? Error)> LoadPortalUsers(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PortalDirectory:ApiBaseUrl"]?.TrimEnd('/');
        var key = configuration["Auth:PortalSsoKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(key))
            return (null, StatusCode(503, new { message = "Portal 人員比對服務尚未設定。" }));
        var client = clients.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/spc-directory/quality-users");
        message.Headers.TryAddWithoutValidation("X-SPC-Sync-Key", key);
        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (null, StatusCode(502, new { message = $"Portal 人員比對服務回應失敗（{(int)response.StatusCode}）。" }));
        return (await response.Content.ReadFromJsonAsync<List<PortalUser>>(cancellationToken: cancellationToken) ?? [], null);
    }

    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static object ToRow(string status, PortalUser? p, Domain.Entities.Operator? o, string? message) => new
    {
        status, message, portalUserId = p?.PortalUserId, spcOperatorId = o?.Id,
        adAccount = p?.AdAccount ?? o?.Username, employeeNo = p?.EmployeeNo ?? o?.OperatorCode,
        portalName = p?.DisplayName, spcName = o?.OperatorName, portalDepartment = p?.Department,
        spcDepartment = o?.Department, portalEmail = p?.Email, spcEmail = o?.Email,
        portalIsActive = p?.IsActive, spcIsActive = o?.IsActive, spcRole = o?.Role
    };
    public sealed record PortalUser(int PortalUserId, string AdAccount, string? EmployeeNo, string? DisplayName,
        string? Department, string? Email, bool IsActive, Guid? AdObjectGuid, DateTime? LastSyncedAt);
    public sealed record SyncItem(int PortalUserId, int? SpcOperatorId = null);
    public sealed record SyncRequest(List<SyncItem>? Items);
}
