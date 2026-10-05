using EtchTrialImport;

namespace MesSpc.Chemical.Tests;

public class EtchImportKeyTests
{
    [Fact]
    public void SourceReferenceIncludesEtchPrefixDateAndLine()
    {
        Assert.Equal(
            "ETCH:2026-09-01:PT1",
            EtchImportKey.SourceReference(new DateTime(2026, 9, 1), "PT1"));
    }
}
