using NovemberTestImport;
using System;
using Xunit;
namespace NovemberTestImport.Tests;
public sealed class NovemberImportTargetTests
{
 [Fact] public void ProductionApplyRequiresExactDatabaseAndReconciliationHash()
 {
  Assert.Throws<InvalidOperationException>(()=>NovemberImportTarget.Parse(new[]{"--prod-apply"},"ABC"));
  var target=NovemberImportTarget.Parse(new[]{"--prod-apply","--confirm-spc=PMR_SPC_2026","--confirm-portal=PMR_PORTAL_UAT","--confirm-reconciliation-sha256=ABC"},"ABC");
  Assert.True(target.IsProduction);Assert.Equal("PMR_SPC_2026",target.SpcDatabase);Assert.Equal("PMR_PORTAL_UAT",target.PortalDatabase);
 }
 [Fact] public void TestModeCannotSelectProductionDatabases()
 {
  var target=NovemberImportTarget.Parse(new[]{"--apply"},"ABC");
  Assert.False(target.IsProduction);Assert.Equal("PMR_SPC_TEST",target.SpcDatabase);Assert.Equal("PMR_PORTAL_TEST",target.PortalDatabase);
 }
 [Fact] public void ProductionConfirmationIsCaseSensitiveAndHashBound()
 {
  Assert.Throws<InvalidOperationException>(()=>NovemberImportTarget.Parse(new[]{"--prod-apply","--confirm-spc=pmr_spc_2026","--confirm-portal=PMR_PORTAL_UAT","--confirm-reconciliation-sha256=ABC"},"ABC"));
  Assert.Throws<InvalidOperationException>(()=>NovemberImportTarget.Parse(new[]{"--prod-apply","--confirm-spc=PMR_SPC_2026","--confirm-portal=PMR_PORTAL_UAT","--confirm-reconciliation-sha256=OLD"},"ABC"));
 }
}
