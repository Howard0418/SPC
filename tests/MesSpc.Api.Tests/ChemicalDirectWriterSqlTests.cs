#if CHEMICAL_TRACE_SQL_TESTS
using System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
namespace MesSpc.Api.Tests;
public class ChemicalDirectWriterSqlTests
{
 [Fact] public async Task DirectWriterEnforcesEvidencePermissionsIdempotenceAndAtomicRollback()
 {
        var connection=Environment.GetEnvironmentVariable("SPC_FORMULA_SCHEMA_TEST_CONNECTION")??throw new InvalidOperationException("Isolation required");
        if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("SPC_FORMULA_SCHEMA_VERIFY_"))throw new InvalidOperationException("Isolation required");
        connection=new SqlConnectionStringBuilder(connection){Pooling=false}.ConnectionString;
        AppDbContext Open()=>new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        var protection=new EphemeralDataProtectionProvider();
        ChemicalFormulaScheduleService Schedule(AppDbContext db)=>new(db,protection,TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
        await using var seed=Open();await seed.Database.OpenConnectionAsync();
        await using(var command=seed.Database.GetDbConnection().CreateCommand()){
            command.CommandText="SELECT CAST(value AS nvarchar(100)) FROM sys.extended_properties WHERE class=0 AND name=N'SPC.FormulaTrace.Isolation'";
            (await command.ExecuteScalarAsync()).Should().Be("TASK-002");
        }
        await seed.Database.MigrateAsync();
        await seed.GetService<IMigrator>().MigrateAsync("20261020040256_AddChemicalFormulaTraceSchema");
        await seed.Database.MigrateAsync();var code="T4-"+Guid.NewGuid().ToString("N")[..8];
        var process=new Process{ProcessCode=code,ControlScope="CHEM"};var characteristic=new QualityCharacteristic{CharacteristicCode=code,ControlScope="CHEM",InputMode="FORMULA"};
        seed.AddRange(process,characteristic);await seed.SaveChangesAsync();
        var machine=new Machine{MachineCode=code,ProcessId=process.Id};
        var line=new ProductionLine{LineCode=code,FactoryId=await seed.Factories.Select(x=>x.Id).FirstAsync()};
        seed.AddRange(machine,line);await seed.SaveChangesAsync();
        var tank=new Tank{LineId=line.Id,TankCode=code,TankName=code};seed.Add(tank);await seed.SaveChangesAsync();
        var chart=new ControlChartType{ChartGroupId=await seed.ControlChartGroups.Select(x=>x.Id).FirstAsync(),ChartTypeCode=code,ChartTypeName=code};seed.Add(chart);await seed.SaveChangesAsync();
        var ppc=new PartProcessCharacteristic{ChartTypeId=chart.Id,ProcessId=process.Id,MachineId=machine.Id,TankId=tank.Id,CharacteristicId=characteristic.Id,ControlScope="CHEM"};
        seed.Add(ppc);await seed.SaveChangesAsync();
        var formula=new ChemicalAnalysisFormulaVersion{PartProcessCharacteristicId=ppc.Id,VersionNo=1,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\",\"decimalPlaces\":2}"};
        seed.Add(formula);await seed.SaveChangesAsync();
        await Schedule(seed).PublishAsync(ppc.Id,new(null,"TASK004 isolation",[new(formula.Id,null,"2026-10-01",null)]),"Task005Writer",default);
        // Prove the receipt hook also works with the SQL write gate, without enabling any real PPC.
        await seed.Database.ExecuteSqlInterpolatedAsync($"UPDATE ChemicalFormulaPolicies SET IsEnabled=1 WHERE PartProcessCharacteristicId={ppc.Id}");

        async Task<(Guid a,Guid b)> Receipts(string primary,string key,string at="2026-10-20T12:00:00+08:00"){
            var input=new ChemicalPreviewInput(at,primary,null,key);
            var a=await Schedule(seed).PreviewAsync(ppc.Id,input,"Task005Writer",default);
            var b=await Schedule(seed).PreviewAsync(ppc.Id,input with{Primary="2.3456"},"Task005Writer",default);
            return ((await Schedule(seed).PrepareAsync(ppc.Id,new(input,a.Token),"Task005Writer",default)).Id,
                (await Schedule(seed).PrepareAsync(ppc.Id,new(input with{Primary="2.3456"},b.Token),"Task005Writer",default)).Id);
        }
        var key="{\"kind\":\"TransFilesChemical-v1\",\"fileHash\":\""+new string('a',64)+"\",\"sheet\":\"Raw\",\"row\":2,\"samplingPhase\":\"OPEN\",\"samplingStage\":\"GENERAL\"}";
        var receipts=await Receipts("1.2345",key);
        async Task<long> Commit((Guid a,Guid b) r){
            await using var conn=new SqlConnection(connection);await conn.OpenAsync();await using var command=conn.CreateCommand();
            command.CommandText="EXECUTE AS USER='Task005Writer'; BEGIN TRY EXEC dbo.CommitChemicalMeasurementWithTrace @InitialReceiptId=@a,@RecheckReceiptId=@b,@Actor=@actor; REVERT; END TRY BEGIN CATCH REVERT; THROW; END CATCH;";
            command.Parameters.AddWithValue("@a",r.a);command.Parameters.AddWithValue("@b",r.b);command.Parameters.AddWithValue("@actor","Task005Writer");
            await using var reader=await command.ExecuteReaderAsync();await reader.ReadAsync();return reader.GetInt64(reader.GetOrdinal("measurementId"));
        }
        await seed.Database.ExecuteSqlRawAsync("CREATE USER Task005Writer WITHOUT LOGIN; ALTER ROLE SpcChemicalDirectWriter ADD MEMBER Task005Writer;");
        try{
            var pair=await Task.WhenAll(Commit(receipts),Commit(receipts));pair[0].Should().Be(pair[1]);
            await using var verify=Open();var vm=await verify.VariableMeasurements.SingleAsync(x=>x.Id==pair[0]);
            vm.MeasuredValue.Should().Be(2.47);vm.RecheckValue.Should().Be(4.69);
            (await verify.ChemicalCalculationTraces.CountAsync(x=>x.VariableMeasurementId==vm.Id)).Should().Be(2);
            (await verify.ChemicalStatisticsJobs.CountAsync(x=>x.VariableMeasurementId==vm.Id)).Should().Be(1);
            // Incorrect actor cannot use another person's receipt, even with execute permission.
            Func<Task> wrongActor=()=>verify.Database.ExecuteSqlInterpolatedAsync($"EXECUTE AS USER='Task005Writer'; EXEC dbo.CommitChemicalMeasurementWithTrace @InitialReceiptId={receipts.a},@RecheckReceiptId={receipts.b},@Actor=N'other'; REVERT;");
            await wrongActor.Should().ThrowAsync<SqlException>().Where(x=>x.Number==51220);
            await verify.Database.CloseConnectionAsync();
            var retry=await Receipts("1.2345",key);(await Commit(retry)).Should().Be(vm.Id);
            var changed=await Receipts("9",key);Func<Task> conflict=()=>Commit(changed);await conflict.Should().ThrowAsync<SqlException>();
            // The writer cannot insert invented evidence or modify measurements.
            Func<Task> denied=()=>verify.Database.ExecuteSqlRawAsync("EXECUTE AS USER='Task005Writer'; UPDATE VariableMeasurements SET MeasuredValue=0; REVERT;");
            await denied.Should().ThrowAsync<SqlException>().Where(x=>x.Number==229);
            // Dedicated connection for impersonation test above, reset even after denied command.
            await verify.Database.CloseConnectionAsync();
            var lateKey=key.Replace("\"row\":2","\"row\":3").Replace("OPEN","MIDDLE");
            var late=await Receipts("3.1",lateKey);
            var before=await seed.VariableMeasurements.CountAsync();
            await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_Task005_FailJob ON ChemicalStatisticsJobs AFTER INSERT AS BEGIN THROW 51221,'Forced isolated failure',1; END");
            try{Func<Task> failure=()=>Commit(late);await failure.Should().ThrowAsync<SqlException>();
                (await seed.VariableMeasurements.CountAsync()).Should().Be(before);
                (await seed.ChemicalCalculationTraces.CountAsync(x=>x.ReceiptId==late.a)).Should().Be(0);
            }finally{await seed.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_Task005_FailJob");}
            // Unconsumed expired evidence must not write anything.
            var original=await seed.ChemicalCalculationReceipts.AsNoTracking().SingleAsync(x=>x.Id==late.a);
            var expired=new ChemicalCalculationReceipt{Id=Guid.NewGuid(),PartProcessCharacteristicId=ppc.Id,EffectivityId=original.EffectivityId,
                MeasuredAtUtc=original.MeasuredAtUtc,CreatedAtUtc=DateTime.UtcNow.AddMinutes(-20),ExpiresAtUtc=DateTime.UtcNow.AddMinutes(-1),
                Actor=original.Actor,InputHash=original.InputHash,SourceKeyHash=original.SourceKeyHash,EngineVersion=original.EngineVersion,PrecisionPolicy=original.PrecisionPolicy,PayloadJson=original.PayloadJson};
            seed.Add(expired);await seed.SaveChangesAsync();
            Func<Task> expiry=()=>Commit((expired.Id,late.b));await expiry.Should().ThrowAsync<SqlException>().Where(x=>x.Number==51215);
            // Preparing before a publication must not silently use the replacement schedule.
            var revision=await seed.ChemicalFormulaPolicies.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==ppc.Id).Select(x=>x.ActiveScheduleRevisionId).SingleAsync();
            await Schedule(seed).PublishAsync(ppc.Id,new(revision,"republish same periods",[new(formula.Id,null,"2026-10-01",null)]),"Task005Writer",default);
            Func<Task> stale=()=>Commit(late);await stale.Should().ThrowAsync<SqlException>().Where(x=>x.Number==51216);
            // Two consumers/retries must not duplicate results.
            var job=await seed.ChemicalStatisticsJobs.AsNoTracking().SingleAsync(x=>x.VariableMeasurementId==vm.Id);
            async Task<bool> Process(){await using var db=Open();return await new ChemicalStatisticsProcessor(db,new SpcService(db,new ChemicalPortalMeasurementTests.FakeEmail(),new ConfigurationBuilder().Build()),NullLogger<ChemicalStatisticsProcessor>.Instance).ProcessOneAsync(job.Id,default);}
            (await Task.WhenAll(Process(),Process())).Count(x=>x).Should().Be(1);
            (await seed.SpcCalculationResults.CountAsync(x=>x.VariableMeasurementId==vm.Id)).Should().Be(1);
            await using var statusConnection=new SqlConnection(connection);await statusConnection.OpenAsync();
            await using var status=statusConnection.CreateCommand();
            status.CommandText="EXECUTE AS USER='Task005Writer'; EXEC dbo.GetChemicalDirectImportStatus @ReceiptId=@receipt,@Actor=N'Task005Writer'; REVERT;";
            status.Parameters.AddWithValue("@receipt",receipts.a);
            await using var statusReader=await status.ExecuteReaderAsync();(await statusReader.ReadAsync()).Should().BeTrue();
            statusReader.GetString(statusReader.GetOrdinal("statisticsStatus")).Should().Be("Completed");
            var evidence=(await new ChemicalTraceQueryService(seed).GetAsync(ppc.Id,null,vm.Id,0,0,default))!;
            evidence.Rows.Single().Calculations.Should().HaveCount(2);
            evidence.Rows.Single().Calculations.Single(x=>x.Phase=="Initial").RawResult.Should().Be("2.469");
            (await new ChemicalTraceQueryService(seed).GetAsync(ppc.Id,null,null,0,0,default))!.Rows.Should().Contain(x=>x.MeasurementId==vm.Id);
            // One file crossing two effective dates: the same real DB path keeps both actual versions.
            var oldDateFormula=new ChemicalAnalysisFormulaVersion{PartProcessCharacteristicId=ppc.Id,VersionNo=2,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*3\",\"decimalPlaces\":2}"};
            seed.Add(oldDateFormula);await seed.SaveChangesAsync();
            var currentRevision=await seed.ChemicalFormulaPolicies.AsNoTracking().Where(x=>x.PartProcessCharacteristicId==ppc.Id).Select(x=>x.ActiveScheduleRevisionId).SingleAsync();
            await Schedule(seed).PublishAsync(ppc.Id,new(currentRevision,"cross-date file",[new(oldDateFormula.Id,null,"2026-10-01","2026-10-18"),new(formula.Id,null,"2026-10-19",null)]),"Task005Writer",default);
            var earlier=await Receipts("1.2345",key.Replace("\"row\":2","\"row\":4"),"2026-10-18T12:00:00+08:00");
            var earlierVm=await Commit(earlier);
            var mixed=(await new ChemicalTraceQueryService(seed).GetAsync(ppc.Id,null,null,0,0,default))!;
            mixed.MultipleFormulaVersions.Should().BeTrue();
            mixed.Rows.Single(x=>x.MeasurementId==earlierVm).Calculations.Single(x=>x.Phase=="Initial").StoredResult.Should().Be("3.7");
            mixed.Rows.Single(x=>x.MeasurementId==vm.Id).Calculations.Single(x=>x.Phase=="Initial").StoredResult.Should().Be("2.47");
        }finally{await seed.Database.ExecuteSqlRawAsync("ALTER ROLE SpcChemicalDirectWriter DROP MEMBER Task005Writer; DROP USER Task005Writer;");}
 }
}
#endif
