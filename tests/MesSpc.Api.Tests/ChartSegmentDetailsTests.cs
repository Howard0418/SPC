using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class ChartSegmentDetailsTests
{
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task SegmentKeepsExactDatesAndSeparatesFallbackAndUnprovenVersions()
    {
        await using var db = CreateDb();
        var ppc = new PartProcessCharacteristic { ControlScope = "CHEM", USL = 20, LSL = 2, UCL = 18,
            ChemicalAnalysisConfigJson = "{\"enabled\":true,\"concentrationFormula\":\"Primary*2\"}" };
        db.Add(ppc); await db.SaveChangesAsync();
        var segment = new ControlLimitSegment { PartProcessCharacteristicId = ppc.Id,
            StartDate = new(2026, 10, 1, 8, 0, 0), EndDate = new(2026, 10, 20, 17, 0, 0), USL = 15, CL = 10 };
        db.Add(segment); await db.SaveChangesAsync();
        db.ChemicalAnalysisFormulaVersions.Add(new() { PartProcessCharacteristicId = ppc.Id, VersionNo = 1,
            ConfigJson = "{\"concentrationFormula\":\"Primary\"}", ChangedAt = new(2026, 10, 21), Reason = "WI-001" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var count = await db.SpecificationLimitVersions.CountAsync();
        var detail = await new ChartSegmentDetailsService(db).GetAsync(ppc.Id, segment.Id, 0, 0, default);
        detail.Should().NotBeNull();
        detail!.StartDate.Should().Be(segment.StartDate); detail.EndDate.Should().Be(segment.EndDate);
        detail.Limits["USL"].Should().Be(new SegmentLimitValue(15, "Segment"));
        detail.Limits["LSL"].Should().Be(new SegmentLimitValue(2, "Base"));
        detail.Limits["CL"].Value.Should().Be(10);
        detail.FormulaHistory.Single().Reason.Should().Be("WI-001");
        detail.FormulaHistory.Single().ChangedAt.Kind.Should().Be(DateTimeKind.Utc);
        detail.FormulaVersionStatus.Should().Be("Unknown"); detail.FTableVersionStatus.Should().Be("Unknown");
        detail.ControlLimitVersionStatus.Should().Be("NotRecorded");
        detail.SpecificationHistory.Should().OnlyContain(x => x.SourceSegmentId == segment.Id);
        db.ChangeTracker.Entries().Should().BeEmpty();
        (await db.SpecificationLimitVersions.CountAsync()).Should().Be(count);
    }

    [Fact]
    public async Task HistoryPagesAreScopedBeforePagingAndNeverSelectedByModificationDate()
    {
        await using var db = CreateDb();
        var ppc = new PartProcessCharacteristic { ControlScope = "CHEM" };
        var other = new PartProcessCharacteristic { ControlScope = "CHEM" };
        db.AddRange(ppc, other); await db.SaveChangesAsync();
        var segment = new ControlLimitSegment { PartProcessCharacteristicId = ppc.Id, StartDate = new(2026, 1, 1) };
        db.Add(segment); await db.SaveChangesAsync();
        for (var i = 1; i <= 25; i++)
        {
            db.ChemicalAnalysisFormulaVersions.Add(new() { PartProcessCharacteristicId = ppc.Id, VersionNo = i, ConfigJson = "{}" });
            db.ChemicalAnalysisFormulaVersions.Add(new() { PartProcessCharacteristicId = other.Id, VersionNo = 100 + i, ConfigJson = "{}" });
            db.SpecificationLimitVersions.Add(new() { PartProcessCharacteristicId = ppc.Id, SourceKind = "Segment",
                SourceSegmentId = segment.Id, VersionNo = i + 100, SnapshotJson = "{}" });
            db.SpecificationLimitVersions.Add(new() { PartProcessCharacteristicId = ppc.Id, SourceKind = "Base",
                VersionNo = i + 200, SnapshotJson = "{}" });
        }
        await db.SaveChangesAsync();
        var service = new ChartSegmentDetailsService(db);
        var first = await service.GetAsync(ppc.Id, segment.Id, 0, 0, default);
        first!.FormulaHistory.Should().HaveCount(20); first.HasMoreFormula.Should().BeTrue();
        first.FormulaHistory[0].VersionNo.Should().Be(25);
        first.SpecificationHistory.Should().HaveCount(20); first.HasMoreSpecification.Should().BeTrue();
        first.SpecificationHistory[0].VersionNo.Should().Be(125);
        var next = await service.GetAsync(ppc.Id, segment.Id, 20, 0, default);
        next!.FormulaHistory.Should().HaveCount(5); next.HasMoreFormula.Should().BeFalse();
        next.FormulaHistory[0].VersionNo.Should().Be(5);
        (await service.GetAsync(other.Id, segment.Id, 0, 0, default)).Should().BeNull();
        var specNext = await service.GetAsync(ppc.Id, segment.Id, 0, 20, default);
        specNext!.SpecificationHistory.Should().HaveCount(6); specNext.HasMoreSpecification.Should().BeFalse();
        specNext.SpecificationHistory.Should().OnlyContain(x => x.SourceSegmentId == segment.Id);
    }

    [Fact]
    public async Task BaseWithoutSegmentsAndNonChemicalHaveHonestEmptyStates()
    {
        await using var db = CreateDb();
        var ppc = new PartProcessCharacteristic { ControlScope = "PRODUCT" };
        db.Add(ppc); await db.SaveChangesAsync();
        var service = new ChartSegmentDetailsService(db);
        var detail = await service.GetAsync(ppc.Id, null, 0, 0, default);
        detail!.FormulaVersionStatus.Should().Be("NotApplicable"); detail.FormulaHistory.Should().BeEmpty();
        detail.FTableVersionStatus.Should().Be("NotApplicable"); detail.SpecificationHistory.Should().BeEmpty();
        detail.StartDate.Should().BeNull(); detail.SegmentId.Should().BeNull();
        (await service.GetAsync(999, null, 0, 0, default)).Should().BeNull();
        ppc.IsEnabled = false; await db.SaveChangesAsync();
        (await service.GetAsync(ppc.Id, null, 0, 0, default)).Should().BeNull();
    }

    [Theory]
    [InlineData(-1, 0)] [InlineData(0, -1)]
    public async Task InvalidPaginationIsRejected(int formulaSkip, int specificationSkip)
    {
        await using var db = CreateDb();
        var action = () => new ChartSegmentDetailsService(db).GetAsync(1, null, formulaSkip, specificationSkip, default);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ControllerRejectsInvalidAndMissingScopesAndRequiresAuthentication()
    {
        await using var db = CreateDb();
        var controller = new ChartSegmentDetailsController(new(db), NullLogger<ChartSegmentDetailsController>.Instance);
        (await controller.Get(1, default, 0)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Get(1, default, null, -1)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Get(1, default, null, 0, -1)).Should().BeOfType<BadRequestObjectResult>();
        (await controller.Get(1, default)).Should().BeOfType<NotFoundObjectResult>();
        typeof(ChartSegmentDetailsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Should().NotBeEmpty();
    }
}
