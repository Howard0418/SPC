#if CHEMICAL_TRACE_SQL_TESTS
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

public class ChemicalPortalMeasurementSqlTests
{
    [Fact]
    public async Task SqlGateInitialRecheckConcurrentRetryAndLateFailureRollback()
    {
        var connection=Environment.GetEnvironmentVariable("SPC_FORMULA_SCHEMA_TEST_CONNECTION")??throw new InvalidOperationException("Isolation required");
        if(!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("SPC_FORMULA_SCHEMA_VERIFY_"))throw new InvalidOperationException("Isolation required");
        AppDbContext Open()=>new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        var protection=new EphemeralDataProtectionProvider();
        ChemicalFormulaScheduleService Schedule(AppDbContext db)=>new(db,protection,TimeProvider.System,NullLogger<ChemicalFormulaScheduleService>.Instance);
        ChemicalPortalMeasurementService Service(AppDbContext db)=>new(db,Schedule(db),new UploadService(db,new SpcService(db,new ChemicalPortalMeasurementTests.FakeEmail(),new ConfigurationBuilder().Build())),NullLogger<ChemicalPortalMeasurementService>.Instance);
        await using var seed=Open();await seed.Database.OpenConnectionAsync();
        await using(var command=seed.Database.GetDbConnection().CreateCommand()){
            command.CommandText="SELECT CAST(value AS nvarchar(100)) FROM sys.extended_properties WHERE class=0 AND name=N'SPC.FormulaTrace.Isolation'";
            (await command.ExecuteScalarAsync()).Should().Be("TASK-002");
        }
        await seed.Database.MigrateAsync();var code="T4-"+Guid.NewGuid().ToString("N")[..8];
        var process=new Process{ProcessCode=code,ControlScope="CHEM"};var characteristic=new QualityCharacteristic{CharacteristicCode=code,ControlScope="CHEM",InputMode="FORMULA"};
        seed.AddRange(process,characteristic);await seed.SaveChangesAsync();
        var machine=new Machine{MachineCode=code,ProcessId=process.Id};
        var line=new ProductionLine{LineCode=code,FactoryId=await seed.Factories.Select(x=>x.Id).FirstAsync()};
        seed.AddRange(machine,line);await seed.SaveChangesAsync();
        var tank=new Tank{LineId=line.Id,TankCode=code,TankName=code};seed.Add(tank);await seed.SaveChangesAsync();
        var ppc=new PartProcessCharacteristic{ProcessId=process.Id,MachineId=machine.Id,TankId=tank.Id,CharacteristicId=characteristic.Id,ControlScope="CHEM"};
        seed.Add(ppc);await seed.SaveChangesAsync();
        var formula=new ChemicalAnalysisFormulaVersion{PartProcessCharacteristicId=ppc.Id,VersionNo=1,ConfigJson="{\"enabled\":true,\"concentrationFormula\":\"Primary*2\",\"decimalPlaces\":2}"};
        seed.Add(formula);await seed.SaveChangesAsync();
        await Schedule(seed).PublishAsync(ppc.Id,new(null,"TASK004 isolation",[new(formula.Id,null,"2026-10-01",null)]),"TASK004_SQL",default);
        // Prove the receipt hook also works with the SQL write gate, without enabling any real PPC.
        await seed.Database.ExecuteSqlInterpolatedAsync($"UPDATE ChemicalFormulaPolicies SET IsEnabled=1 WHERE PartProcessCharacteristicId={ppc.Id}");
        async Task<Dictionary<string,string?>> Row(string initial,string recheck){
            var service=Service(seed);var input=new ChemicalPortalPreviewRequest(ppc.Id,"2026-10-20T12:00:00+08:00",initial,null,"OPEN","GENERAL");
            var a=await service.PreviewAsync(input,"TASK004_SQL",default);var b=await service.PreviewAsync(input with{Primary=recheck},"TASK004_SQL",default);
            return new(){["PpcId"]=ppc.Id.ToString(),["ControlScope"]="CHEM",["ProcessCode"]=code,["MachineCode"]=code,["TankCode"]=code,["CharacteristicCode"]=code,
                ["MeasuredAt"]=input.MeasuredAt,["TitrationValue"]=initial,["RecheckTitrationValue"]=recheck,["MeasuredValue"]="999",["RecheckValue"]="999",["SampleNo"]="1",
                ["Operator"]="TASK004_SQL",["SamplingPhase"]="OPEN",["SamplingStage"]="GENERAL",["ChemicalPreviewToken"]=a.Token,["ChemicalRecheckPreviewToken"]=b.Token};
        }
        var request=new ChemicalPortalConfirmRequest(Guid.NewGuid(),[await Row("1.2345","2.3456")]);
        async Task<ChemicalPortalConfirmed> Confirm(){await using var db=Open();return await Service(db).ConfirmAsync(request,"TASK004_SQL",default);}
        var results=await Task.WhenAll(Confirm(),Confirm());results.Count(x=>x.AlreadyProcessed).Should().Be(1);
        await using var verify=Open();var vm=await verify.VariableMeasurements.AsNoTracking().SingleAsync(x=>x.PartProcessCharacteristicId==ppc.Id);
        vm.MeasuredValue.Should().Be(2.47);vm.RecheckValue.Should().Be(4.69);
        (await verify.ChemicalCalculationTraces.CountAsync(x=>x.VariableMeasurementId==vm.Id)).Should().Be(2);
        var receiptCount=await verify.ChemicalCalculationReceipts.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id);
        var failed=new ChemicalPortalConfirmRequest(Guid.NewGuid(),[await Row("3.1","4.2")]);
        // Inject a SQL failure after receipt issuance, measurement update and the initial trace have already executed.
        await verify.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_Task004_RejectRecheck ON ChemicalCalculationTraces AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Phase='Recheck') THROW 51201,'TASK004 forced late failure',1; END");
        try{
            await using var faultDb=Open();Func<Task> act=()=>Service(faultDb).ConfirmAsync(failed,"TASK004_SQL",default);
            await act.Should().ThrowAsync<DbUpdateException>().Where(x=>x.InnerException is SqlException && ((SqlException)x.InnerException).Number==51201);
            (await verify.UploadBatches.CountAsync(x=>x.UploadBatchId==failed.RequestId)).Should().Be(0);
            (await verify.ChemicalCalculationReceipts.CountAsync(x=>x.PartProcessCharacteristicId==ppc.Id)).Should().Be(receiptCount);
            (await verify.ChemicalCalculationTraces.CountAsync(x=>x.VariableMeasurementId==vm.Id)).Should().Be(2);
            (await verify.VariableMeasurements.AsNoTracking().SingleAsync(x=>x.Id==vm.Id)).MeasuredValue.Should().Be(2.47);
        }finally{await verify.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_Task004_RejectRecheck");}
    }
}
#endif
