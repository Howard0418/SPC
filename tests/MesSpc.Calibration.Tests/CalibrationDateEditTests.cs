using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Calibration.Tests;

public sealed class CalibrationDateEditTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AppDbContext db;
    private readonly InstrumentCalibrationService service;
    public CalibrationDateEditTests()
    {
        connection.Open();
        db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Operators.Add(new Operator { Id=1, OperatorCode="QA", OperatorName="QA", IsActive=true });
        db.SaveChanges();
        service = new(db,TimeProvider.System);
    }
    private static InstrumentInput Input(DateOnly? last=null,DateOnly? next=null) =>
        new("EDIT-01","日期編輯測試","品保",1,12,last,next,"Active",[],true,null,"");

    [Theory]
    [InlineData(null,"2026-09-01")]
    [InlineData("2026-08-01","2026-09-01")]
    [InlineData("2026-08-01",null)]
    public async Task DirectDateEditsPersistAndAuditWithoutRecalculatingDue(string? original,string? changed)
    {
        DateOnly? before=original is null ? null : DateOnly.Parse(original);
        DateOnly? after=changed is null ? null : DateOnly.Parse(changed);
        var i=await service.SaveInstrumentAsync(null,Input(before,new(2027,1,1)),"qa",default);
        var cycle=i.CurrentCycleId;
        await service.SaveInstrumentAsync(i.Id,Input(after,i.NextCalibrationDate) with { Version=i.Version },"editor",default);
        db.ChangeTracker.Clear();
        var saved=await db.Set<CalibrationInstrument>().SingleAsync();
        Assert.Equal(after,saved.LastCalibrationDate);
        Assert.Equal(new DateOnly(2027,1,1),saved.NextCalibrationDate);
        Assert.Equal(cycle,saved.CurrentCycleId);
        Assert.Null(saved.LatestResult);
        Assert.Empty(await db.Set<InstrumentCalibrationRecord>().ToListAsync());
        var audit=await db.Set<CalibrationAuditLog>().SingleAsync(x=>x.Action=="InstrumentUpdated");
        Assert.Equal("editor",audit.Actor); Assert.NotEqual(default,audit.CreatedAt);
        Assert.Equal(before?.ToString("yyyy-MM-dd"),JsonDocument.Parse(audit.BeforeJson).RootElement.GetProperty("LastCalibrationDate").GetString());
        Assert.Equal(after?.ToString("yyyy-MM-dd"),JsonDocument.Parse(audit.AfterJson).RootElement.GetProperty("LastCalibrationDate").GetString());
    }

    [Fact]
    public async Task ExistingHistoryAndResultStayUnchangedWhenMasterDateIsEdited()
    {
        var i=await service.SaveInstrumentAsync(null,Input(null,new(2027,1,1)),"qa",default);
        await service.RecordAsync(i.Id,new(new(2026,8,1),"Passed",null,null,null,Guid.NewGuid(),i.Version),"qa",default);
        await service.RecordAsync(i.Id,new(new(2026,8,2),"Failed",null,null,null,Guid.NewGuid(),i.Version),"qa",default);
        var history=JsonSerializer.Serialize(await db.Set<InstrumentCalibrationRecord>().AsNoTracking().OrderBy(x=>x.Id).ToListAsync());
        var cycle=i.CurrentCycleId; var due=i.NextCalibrationDate;
        await service.SaveInstrumentAsync(i.Id,Input(new(2026,9,1),due) with { Version=i.Version },"qa",default);
        Assert.Equal("Failed",i.LatestResult); Assert.Equal(cycle,i.CurrentCycleId); Assert.Equal(due,i.NextCalibrationDate);
        Assert.Equal(history,JsonSerializer.Serialize(await db.Set<InstrumentCalibrationRecord>().AsNoTracking().OrderBy(x=>x.Id).ToListAsync()));
    }

    [Fact]
    public async Task ChangingLastOnlyKeepsNotificationStatesAndChangingDueCancelsUnsent()
    {
        var i=await service.SaveInstrumentAsync(null,Input(new(2026,8,1),new(2027,1,1)),"qa",default);
        foreach(var state in new[]{"Pending","Failed","Sent"}) db.Add(new CalibrationNotification
        { InstrumentId=i.Id,CycleId=i.CurrentCycleId,DueDate=i.NextCalibrationDate!.Value,Stage="BEFORE-30",RecipientKey=state,State=state });
        await db.SaveChangesAsync();
        await service.SaveInstrumentAsync(i.Id,Input(new(2026,9,1),i.NextCalibrationDate) with { Version=i.Version },"qa",default);
        Assert.Equal(new[]{"Failed","Pending","Sent"},await db.Set<CalibrationNotification>().OrderBy(x=>x.State).Select(x=>x.State).ToArrayAsync());
        await service.SaveInstrumentAsync(i.Id,Input(new(2026,9,2),new(2027,2,1)) with { Version=i.Version,Reason="日期更正" },"qa",default);
        Assert.Equal(2,await db.Set<CalibrationNotification>().CountAsync(x=>x.State=="Cancelled"));
        Assert.Equal(1,await db.Set<CalibrationNotification>().CountAsync(x=>x.State=="Sent"));
    }

    [Theory]
    [InlineData("future")][InlineData("order")][InlineData("reason")][InlineData("version")]
    public async Task InvalidDateEditsLeaveDataAndAuditUnchanged(string failure)
    {
        var i=await service.SaveInstrumentAsync(null,Input(new(2026,8,1),new(2027,1,1)),"qa",default);
        var req=Input(new(2026,9,1),i.NextCalibrationDate) with { Version=i.Version };
        req=failure switch {
            "future"=>req with {LastCalibrationDate=CalibrationRules.Today(TimeProvider.System.GetUtcNow()).AddDays(1)},
            "order"=>req with {NextCalibrationDate=new(2026,9,1),Reason="日期更正"},
            "reason"=>req with {NextCalibrationDate=new(2027,2,1)},
            _=>req with {Version=Guid.NewGuid()}
        };
        await Assert.ThrowsAsync<CalibrationException>(()=>service.SaveInstrumentAsync(i.Id,req,"qa",default));
        db.ChangeTracker.Clear();
        var saved=await db.Set<CalibrationInstrument>().SingleAsync();
        Assert.Equal(new DateOnly(2026,8,1),saved.LastCalibrationDate);
        Assert.Equal(new DateOnly(2027,1,1),saved.NextCalibrationDate);
        Assert.Equal(1,await db.Set<CalibrationAuditLog>().CountAsync());
    }

    [Fact]
    public async Task FillingBothBlankDatesAndEditingOtherFieldsRemainSupported()
    {
        var i=await service.SaveInstrumentAsync(null,Input(),"qa",default);
        await service.SaveInstrumentAsync(i.Id,Input(new(2026,9,1),new(2027,9,1)) with { Version=i.Version },"qa",default);
        await service.SaveInstrumentAsync(i.Id,Input(i.LastCalibrationDate,i.NextCalibrationDate) with { Version=i.Version,Location="實驗室" },"qa",default);
        Assert.Equal("實驗室",i.Location); Assert.Equal(new DateOnly(2026,9,1),i.LastCalibrationDate);
    }
    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
