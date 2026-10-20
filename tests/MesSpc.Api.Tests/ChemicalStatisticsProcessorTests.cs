using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
namespace MesSpc.Api.Tests;
public class ChemicalStatisticsProcessorTests
{
    [Fact]
    public async Task MissingChartRetriesWithoutPartialResultsThenCompletesOnlyOnce()
    {
        await using var f=await ChemicalPortalMeasurementTests.Fixture.Create();
        await f.Service.ConfirmAsync(new(Guid.NewGuid(),[await f.Row("1.2345",null)]),"editor",default);
        var vm=await f.Db.VariableMeasurements.SingleAsync();
        var job=new ChemicalStatisticsJob{Id=Guid.NewGuid(),VariableMeasurementId=vm.Id,ReceiptId=vm.ChemicalReceiptId!.Value};
        f.Db.Add(job);await f.Db.SaveChangesAsync();
        var processor=new ChemicalStatisticsProcessor(f.Db,new SpcService(f.Db,f.Mail,new ConfigurationBuilder().Build()),NullLogger<ChemicalStatisticsProcessor>.Instance);
        (await processor.ProcessOneAsync(job.Id,default)).Should().BeFalse();
        f.Db.ChangeTracker.Clear();var failed=await f.Db.ChemicalStatisticsJobs.SingleAsync();
        failed.Status.Should().Be("Failed");failed.Attempts.Should().Be(1);failed.NextAttemptAtUtc.Should().NotBeNull();
        (await f.Db.SpcCalculationResults.CountAsync()).Should().Be(0);
        f.Db.Add(new ControlChartType{Id=1,ChartTypeCode="I",ChartTypeName="I"});
        var ppc=await f.Db.PartProcessCharacteristics.SingleAsync();ppc.ChartTypeId=1;await f.Db.SaveChangesAsync();
        // Manual retry after fixing the chart, without reimporting measurement.
        failed.NextAttemptAtUtc=DateTime.UtcNow.AddSeconds(-1);await f.Db.SaveChangesAsync();
        (await processor.ProcessOneAsync(job.Id,default)).Should().BeTrue();
        (await processor.ProcessOneAsync(job.Id,default)).Should().BeFalse();
        (await f.Db.SpcCalculationResults.CountAsync()).Should().Be(1);
        (await f.Db.ChemicalStatisticsJobs.SingleAsync()).Status.Should().Be("Completed");
    }
}
