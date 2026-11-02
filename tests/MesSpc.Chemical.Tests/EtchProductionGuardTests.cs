using EtchProduction;
public class EtchProductionGuardTests
{
 [Fact] public void ApplyRequiresExactDatabaseAndSourceConfirmation(){
  Assert.Throws<InvalidOperationException>(()=>ProductionGuard.RequireApplyConfirmation("--apply",[],"abc"));
  Assert.Throws<InvalidOperationException>(()=>ProductionGuard.RequireApplyConfirmation("--apply",["--confirm-database=PMR_SPC_TEST","--confirm-source-sha256=abc"],"abc"));
  Assert.Throws<InvalidOperationException>(()=>ProductionGuard.RequireApplyConfirmation("--apply",["--confirm-database=PMR_SPC_2026","--confirm-source-sha256=old"],"abc"));
  ProductionGuard.RequireApplyConfirmation("--apply",["--confirm-database=PMR_SPC_2026","--confirm-source-sha256=abc"],"abc");
 }
 [Theory][InlineData("test","172.16.110.16","PMR_SPC_2026")][InlineData("production","other","PMR_SPC_2026")][InlineData("production","172.16.110.16","PMR_SPC_TEST")]
 public void WrongEnvironmentRejected(string env,string host,string db)=>Assert.Throws<InvalidOperationException>(()=>ProductionGuard.RequireDatabase(env,$"Server={host};Database={db};Integrated Security=true"));
 [Fact]public void ExactProductionAccepted()=>ProductionGuard.RequireDatabase("production","Server=172.16.110.16;Database=PMR_SPC_2026;Integrated Security=true");
 [Fact]public void PreflightDoesNotRequireApplyConfirmation()=>ProductionGuard.RequireApplyConfirmation("--preflight",[],"abc");
}
