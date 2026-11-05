using TestMeasurementCleanup;
public class LegacyArchiveGateTests
{
 const string Original="CREATE TRIGGER TR_ChemicalMeasurementGate ON VariableMeasurements AFTER INSERT, UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted i JOIN ChemicalFormulaPolicies p ON p.PartProcessCharacteristicId=i.PartProcessCharacteristicId WHERE p.IsEnabled=1 AND (i.ChemicalReceiptId IS NULL OR i.ChemicalSourceKeyHash IS NULL)) THROW 51110, 'Enabled chemical items require prepared evidence.', 1; END";
 [Fact]public void ArchiveExceptionComparesEveryBusinessColumnAndRequiresExistingLiveRow(){var sql=LegacyArchiveGate.Build(Original,["Id","IsDeleted","UpdatedAt","UpdatedBy","MeasuredValue","SourceReference"]);Assert.Contains("d.IsDeleted=0 AND i.IsDeleted=1",sql);Assert.Contains("SELECT i.[Id],i.[MeasuredValue],i.[SourceReference] EXCEPT SELECT d.[Id],d.[MeasuredValue],d.[SourceReference]",sql);Assert.StartsWith("ALTER TRIGGER",sql);Assert.DoesNotContain("i.[UpdatedBy]",sql);Assert.Contains("THROW 51110",sql);}
 [Fact]public void UnknownTriggerRejected()=>Assert.Throws<InvalidOperationException>(()=>LegacyArchiveGate.Build("other trigger",["Id","IsDeleted","UpdatedAt","UpdatedBy"]));
 [Fact]public void UnknownSchemaRejected()=>Assert.Throws<InvalidOperationException>(()=>LegacyArchiveGate.Build(Original,["Id","MeasuredValue"]));
}
