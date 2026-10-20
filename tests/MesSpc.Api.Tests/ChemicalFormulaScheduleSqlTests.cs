#if CHEMICAL_TRACE_SQL_TESTS
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Api.Tests;

public class ChemicalFormulaScheduleSqlTests
{
    [Fact]
    public async Task RealPublicationLocksStaleWriterAndReceiptsAndRollback()
    {
        var connection=Environment.GetEnvironmentVariable("SPC_FORMULA_SCHEMA_TEST_CONNECTION") ?? throw new InvalidOperationException("Isolation required");
        if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("SPC_FORMULA_SCHEMA_VERIFY_")) throw new InvalidOperationException("Isolation required");
        AppDbContext Open() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        var protection=new EphemeralDataProtectionProvider();
        ChemicalFormulaScheduleService Svc(AppDbContext db) => new(db,protection,TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
        await using var seed=Open();
        await seed.Database.OpenConnectionAsync();
        await using(var cmd=seed.Database.GetDbConnection().CreateCommand()) {
            cmd.CommandText="SELECT CAST(value AS nvarchar(100)) FROM sys.extended_properties WHERE class=0 AND name=N'SPC.FormulaTrace.Isolation'";
            (await cmd.ExecuteScalarAsync()).Should().Be("TASK-002");
        }
        await seed.Database.MigrateAsync();
        var suffix=Guid.NewGuid().ToString("N")[..8];
        var process=new Process { ProcessCode="SCH-"+suffix };
        var characteristic=new QualityCharacteristic { CharacteristicCode="SCH-"+suffix };
        seed.AddRange(process,characteristic); await seed.SaveChangesAsync();
        var ppc=new PartProcessCharacteristic { ProcessId=process.Id,CharacteristicId=characteristic.Id,ControlScope="CHEM" };
        seed.Add(ppc); await seed.SaveChangesAsync();
        var formula=new ChemicalAnalysisFormulaVersion { PartProcessCharacteristicId=ppc.Id,VersionNo=1,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\"}" };
        var f=new ChemicalFTableVersionHistory { VersionNo=900000+ppc.Id,VersionCode="SCH-"+suffix,CellsJson="[{\"CellAddress\":\"F!B3\",\"NumericValue\":2}]" };
        seed.AddRange(formula,f); await seed.SaveChangesAsync();
        var request=new ChemicalPublishSchedule(null,"SQL isolated",[new(formula.Id,f.Id,"2026-10-01",null)]);
        async Task<bool> Publish() {
            await using var db=Open();
            try { await Svc(db).PublishAsync(ppc.Id,request,"editor",default); return true; }
            catch(ChemicalPreviewConflictException) { return false; }
        }
        (await Task.WhenAll(Publish(),Publish())).Count(x=>x).Should().Be(1);
        await using var verify=Open(); var service=Svc(verify);
        var preview=await service.PreviewAsync(ppc.Id,new("2026-10-20T12:00:00+08:00","1.2345",null,"sql-"+suffix),"editor",default);
        var receipt=await service.PrepareAsync(ppc.Id,new(new("2026-10-20T12:00:00+08:00","1.2345",null,"sql-"+suffix),preview.Token),"editor",default);
        receipt.PayloadJson.Should().Contain("2.469");
        (await verify.ChemicalFormulaPolicies.SingleAsync(x=>x.PartProcessCharacteristicId==ppc.Id)).IsEnabled.Should().BeFalse();
        (await verify.ChemicalCalculationReceipts.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id)).Should().Be(1);
        (await verify.VariableMeasurements.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id)).Should().Be(0);
        // A rejected replacement must not create a partial header, period or pointer.
        var before=await verify.ChemicalFormulaScheduleRevisions.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id);
        Func<Task> bad=()=>service.PublishAsync(ppc.Id,new(preview.RevisionId,"bad",[new(formula.Id,f.Id,"2027-01-01",null)]),"editor",default);
        await bad.Should().ThrowAsync<ArgumentException>();
        (await verify.ChemicalFormulaScheduleRevisions.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id)).Should().Be(before);
        // Pinned actual F address shape and old snapshot immutable.
        Func<Task> tamper=async()=>await verify.Database.ExecuteSqlInterpolatedAsync($"UPDATE ChemicalFTableVersionHistories SET CellsJson='[]' WHERE Id={f.Id}");
        await tamper.Should().ThrowAsync<SqlException>().Where(x=>x.Number==51105);
    }
}
#endif
