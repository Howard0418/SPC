using MesSpc.Api.Services.Calibration;
using MesSpc.Api.Services.Localization;
namespace MesSpc.Api.Tests;
public class ImportRowUiMessageTests
{
    [Fact]
    public void LegacyEvidenceMayContainRawDiagnosticButSanitizedDescriptorDoesNotCopyIt()
    {
        var raw=new List<string>();var tracker=new IndexedApiUiMessages();tracker.Add(raw,"old evidence SQL-secret",ApiUiError.Create("FAILED","spc.api.unexpected"));
        Assert.Equal("old evidence SQL-secret",Assert.Single(raw));Assert.DoesNotContain("SQL-secret",Assert.Single(tracker.Snapshot(raw)).Descriptor.Message);
    }
    [Fact]
    public void IndexedRowMetadataRetainsOriginalArraysAndRejectsStaleIndexes()
    {
        var row=new CalibrationImportRow();row.AddError("original error","spc.api.calibrationVersionConflict");row.AddWarning("original warning","spc.api.calibrationPreviewChanged");
        Assert.Equal("original error",Assert.Single(row.Errors));Assert.Equal(0,Assert.Single(row.ErrorMessages).Index);Assert.Equal("original warning",Assert.Single(row.Warnings));Assert.Single(row.WarningMessages);
        row.Errors[0]="changed source";Assert.Empty(row.ErrorMessages);
    }
    [Fact]
    public void StoredCodeDisplayDoesNotModifyLegacyMessageOrGuessUnknownCode()
    {
        var known=UploadErrorUi.Describe("COUNT_NEGATIVE","raw legacy value","count");Assert.NotNull(known);Assert.Equal("raw legacy value",known.Message);Assert.Equal("spc.api.importCountNegative",known.MessageKey);
        Assert.Null(UploadErrorUi.Describe("UNKNOWN_CODE","Count cannot be negative.","count"));
    }
}
