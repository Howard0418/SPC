using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Models;

namespace MesSpc.Api.Tests;

public class OverallStatisticsTests
{
    private static readonly DateTime Day = new(2026, 10, 28);
    private static ControlChartResult Chart(params object[] points) => new() { ChartData = new { points } };
    private static object Point(long id, bool ooc = false, bool oos = false, bool excluded = false) => new
    { variableMeasurementId = id, measuredAt = Day, outOfControl = ooc, outOfSpec = oos, isExcluded = excluded };
    private static AlertEvent Alert(int id, long measurement, string status, bool ack = false) => new()
    { Id = id, VariableMeasurementId = measurement, Status = status, IsAcknowledged = ack, OccurredAt = Day.AddMinutes(id) };

    [Fact]
    public void CountsItemsAndCasesSeparately_OverlapAndDuplicateAreNotDoubleCounted()
    {
        var charts = new[] {
            new OverallChart(1, Chart(Point(1,true,true),Point(2,true),Point(3,true),Point(1,true,true))),
            new OverallChart(2, Chart(Point(4,true),Point(5,true))) };
        var result = OverallStatistics.Calculate(10, 2, charts,
            [Alert(1,1,"Open"),Alert(2,1,"Closed"),Alert(3,2,"InProgress"),Alert(4,3,"Open",true)]);
        Assert.Equal(10, result.ControlItemCount);
        Assert.Equal(12, result.RegisteredItemCount);
        Assert.Equal(2, result.DisabledItemCount);
        Assert.Equal(2, result.OocItemCount);
        Assert.Equal(5, result.OocCount);
        Assert.Equal(1, result.OosItemCount);
        Assert.Equal(1, result.OosCount);
        Assert.Equal(5, result.AbnormalCount);
        Assert.Equal(2, result.HandledCount);
        Assert.Equal(3, result.UnhandledCount);
    }

    [Fact]
    public void ExcludedAndNormalPointsDoNotCount_EmptyIsZero()
    {
        var r = OverallStatistics.Calculate(3, 1, [new(1, Chart(Point(1,true,true,true),Point(2)))], []);
        Assert.Equal(0, r.AbnormalCount);
        Assert.Equal(0, r.OocItemCount);
        Assert.Equal(0, r.OosCount);
        Assert.Equal(0, OverallStatistics.Calculate(0,0,[],[]).UnhandledCount);
    }

    [Fact]
    public void SecondaryImrSignalMergesWithCorrespondingPrimaryPoint()
    {
        var chart = Chart(Point(1),Point(2,false,true)) with {
            ChartType = "I_MR", SecondaryChartData = new { points = new[] { new { index=2, outOfControl=true } } }
        };
        var r = OverallStatistics.Calculate(1,0,[new(1,chart)],[Alert(1,2,"Closed")]);
        Assert.Equal(1,r.OocCount); Assert.Equal(1,r.OosCount);
        Assert.Equal(1,r.AbnormalCount); Assert.Equal(1,r.HandledCount);
    }

    [Fact]
    public void SubgroupRequiresAllRelatedAlertCasesClosed_AndDifferentPpcsStaySeparate()
    {
        var chart = Chart(Point(1,true)) with { RawDataPoints = new[] {
            new SpcDataPoint { MeasuredAt=Day, VariableMeasurementId=1 },
            new SpcDataPoint { MeasuredAt=Day, VariableMeasurementId=2 }
        }, ChartType="XBAR_S" };
        var r=OverallStatistics.Calculate(2,0,[new(1,chart),new(2,Chart(Point(3,true)))],
            [Alert(1,1,"Closed"),Alert(2,2,"InProgress")]);
        Assert.Equal(2,r.AbnormalCount); Assert.Equal(0,r.HandledCount);
        r=OverallStatistics.Calculate(1,0,[new(1,chart)],[Alert(1,1,"Closed"),Alert(2,2,"Closed")]);
        Assert.Equal(1,r.HandledCount);
    }

    [Fact]
    public void AttributeAndTrendPointsHaveIndependentIdentity_AndLatestReopenedAlertIsPending()
    {
        var attr = Chart(new { attributeMeasurementId=1L, measuredAt=Day, outOfControl=true });
        var trend = new ControlChartResult { RawDataPoints = new[] {
            new SpcDataPoint { VariableMeasurementId=1, MeasuredAt=Day, IsOutOfSpec=true }
        } };
        var alerts = new[] {Alert(1,1,"Closed"),Alert(2,1,"Open"),new AlertEvent {
            Id=3,AttributeMeasurementId=1,Status="Closed",OccurredAt=Day }};
        var r=OverallStatistics.Calculate(2,0,[new(1,attr),new(2,trend)],alerts);
        Assert.Equal(2,r.AbnormalCount); Assert.Equal(1,r.HandledCount); Assert.Equal(1,r.UnhandledCount);
    }
}
