using ClosedXML.Excel;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Auditing;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public sealed class AuditEventQueryServiceTests
{
    private static (AppDbContext Db, AuditEventQueryService Service) Fixture(DateTime now)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return (db, new AuditEventQueryService(db, new FixedTime(now)));
    }

    private static AuditEvent Row(long id, DateTime time, string actor = "qa", string page = "analysis.spc.overall",
        string code = "read.query.execute", string outcome = "Succeeded", string? target = null) => new()
    {
        Id = id, EventId = Guid.NewGuid(), OccurredAtUtc = time, ReceivedAtUtc = time, Environment = "Test",
        Category = "Read", EventCode = code, Outcome = outcome, ActorType = "User", ActorAssurance = "Jwt",
        ActorOperatorId = 7, ActorAccountSnapshot = actor, ActorOperatorCodeSnapshot = actor, ActorDisplayNameSnapshot = actor,
        ActorRoleSnapshot = "Editor", PageKey = page, RouteTemplate = "/spc", TargetType = target,
        Source = "Api", DetailsJson = "{\"secret\":\"not-returned\"}", SessionIdHash = new string('A', 64),
        DeduplicationHash = new string('B', 64)
    };

    [Fact]
    public async Task QueryFiltersAndCursorReturnStableNonOverlappingPages()
    {
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        var f = Fixture(now); await using var db = f.Db;
        db.AuditEvents.AddRange(Row(1, now.AddMinutes(-3)), Row(2, now.AddMinutes(-2)), Row(3, now.AddMinutes(-1)),
            Row(4, now.AddMinutes(-1), actor: "other"));
        await db.SaveChangesAsync();
        var first = await f.Service.QueryAsync(new(Actor: "qa", PageSize: 2));
        var second = await f.Service.QueryAsync(new(Actor: "qa", Cursor: first.NextCursor, PageSize: 2));
        Assert.Equal([3L, 2L], first.Items.Select(x => x.Id));
        Assert.Equal([1L], second.Items.Select(x => x.Id));
        Assert.Null(second.NextCursor);
        Assert.All(first.Items, x => Assert.Equal("qa", x.ActorAccount));
    }

    [Fact]
    public async Task DefaultSevenDaysAndMaximumThirtyOneDaysAreEnforced()
    {
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        var f = Fixture(now); await using var db = f.Db;
        db.AuditEvents.AddRange(Row(1, now.AddDays(-7)), Row(2, now.AddDays(-8))); await db.SaveChangesAsync();
        Assert.Single((await f.Service.QueryAsync(new())).Items);
        await Assert.ThrowsAsync<AuditQueryValidationException>(() => f.Service.QueryAsync(new(now.AddDays(-32), now)));
        await Assert.ThrowsAsync<AuditQueryValidationException>(() => f.Service.QueryAsync(new(Cursor: "not-a-cursor")));
        await Assert.ThrowsAsync<AuditQueryValidationException>(() => f.Service.QueryAsync(new(PageSize: 101)));
    }

    [Fact]
    public void WorkbookNeutralizesFormulaLikeTextAndOmitsSensitiveFields()
    {
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        var bytes = AuditEventQueryService.BuildWorkbook([new(1, Guid.NewGuid(), now, "Test", "Read", "read.query.execute",
            "Succeeded", null, "User", 7, "=cmd", "@operator", "+name", "Editor", null, "analysis.spc.overall",
            "/spc", "Lot", "-danger", "GET", 200, "Api")]);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("稽核紀錄");
        Assert.Equal("=cmd", sheet.Cell(2, 8).GetString());
        Assert.Equal("@operator", sheet.Cell(2, 9).GetString());
        Assert.Equal("+name", sheet.Cell(2, 10).GetString());
        Assert.Equal("-danger", sheet.Cell(2, 15).GetString());
        Assert.All(new[] { 8, 9, 10, 15 }, column => Assert.False(sheet.Cell(2, column).HasFormula));
        Assert.DoesNotContain(sheet.Row(1).CellsUsed().Select(x => x.GetString()), x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) || x.Contains("Details", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterAuditSummaryContainsNamesButNeverValues()
    {
        var fields = AuditEventQueryService.FilterFields(new(Actor: "secret-user", PageKey: "analysis.spc.overall", Target: "secret-lot"));
        Assert.Equal(["Actor", "PageKey", "Target"], fields);
        Assert.DoesNotContain("secret-user", fields); Assert.DoesNotContain("secret-lot", fields);
    }

    [Fact]
    public async Task QueryReturnsTargetIdentityAndOnlyAllowlistedChangeSummary()
    {
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        var f = Fixture(now); await using var db = f.Db;
        db.Operators.Add(new() { Id = 27, OperatorCode = "0027", OperatorName = "目標人員", Username = "target", Role = "Editor", IsActive = true });
        var row = Row(1, now, code: "security.operator.change_permissions", target: "Operator");
        row.Category = "Security"; row.TargetId = "27";
        row.DetailsJson = "{\"SchemaVersion\":1,\"ChangedFields\":[\"PagePermissions\"],\"PermissionCodes\":[\"admin.audit.read\",\"admin.audit.export\"]}";
        db.AuditEvents.Add(row); await db.SaveChangesAsync();

        var item = Assert.Single((await f.Service.QueryAsync(new())).Items);
        Assert.Equal("目標人員（0027）", item.TargetDisplayName);
        Assert.Equal(["PagePermissions"], item.Summary!.ChangedFields);
        Assert.Equal(["admin.audit.export", "admin.audit.read"], item.Summary.PermissionCodes);
        Assert.DoesNotContain("DetailsJson", System.Text.Json.JsonSerializer.Serialize(item));
    }

    [Fact]
    public void MalformedOrUnknownDetailFieldsAreNotExposed()
    {
        var summary = AuditEventQueryService.ParseSafeSummary("{\"SchemaVersion\":1,\"ChangedFields\":[\"Role\"],\"secret\":\"do-not-return\"}");
        Assert.Equal(["Role"], summary!.ChangedFields);
        Assert.DoesNotContain("secret", System.Text.Json.JsonSerializer.Serialize(summary));
        Assert.Null(AuditEventQueryService.ParseSafeSummary("not-json"));
    }

    private sealed class FixedTime(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc);
    }
}
