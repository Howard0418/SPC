namespace MesSpc.Api.Services.Localization;

/// <summary>Display-only mappings from existing stored import codes. Never changes persisted validation or guesses keys from text.</summary>
public static class UploadErrorUi
{
    private static readonly IReadOnlyDictionary<string,string> Messages = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["SCOPE_NOT_CONFIGURED"]="importScopeMissing", ["MEASURED_VALUE_REQUIRED"]="importValueRequired",
        ["INVALID_MEASURED_VALUE"]="importValueInvalid", ["INVALID_MEASURED_AT"]="importMeasuredAtInvalid",
        ["PART_NOT_FOUND"]="importPartMissing", ["INVALID_SAMPLING_STAGE"]="chemicalSamplingStageInvalid",
        ["PROCESS_NOT_FOUND"]="importProcessMissing", ["MACHINE_NOT_FOUND"]="importMachineMissing",
        ["MACHINE_PROCESS_MISMATCH"]="importMachineProcessMismatch", ["TANK_REQUIRED"]="importTankRequired",
        ["TANK_NOT_FOUND"]="importTankMissing", ["CHAR_NOT_FOUND"]="importCharacteristicMissing",
        ["INVALID_DATA_CATEGORY"]="importDataCategoryMismatch", ["UNIT_MISMATCH"]="importUnitMismatch",
        ["MAPPING_NOT_FOUND"]="importMappingMissing", ["MAPPING_AMBIGUOUS"]="importMappingAmbiguous",
        ["INSPECTED_QTY_REQUIRED"]="importIntegerField", ["INVALID_DEFECT_QTY"]="importIntegerField",
        ["INVALID_DEFECT_COUNT"]="importIntegerField", ["INVALID_UNIT_COUNT"]="importIntegerField",
        ["MEASUREMENT_TIME_INVALID"]="importParticleTimeInvalid", ["LOCATION_UNSUPPORTED"]="importLocationInvalid",
        ["PARTICLE_SIZE_UNSUPPORTED"]="importParticleSizeInvalid", ["COUNT_INVALID"]="importCountInvalid",
        ["COUNT_NEGATIVE"]="importCountNegative", ["SOURCE_COORDINATE_INVALID"]="importSourceInvalid",
        ["DUPLICATE_SOURCE_CELL"]="importSourceDuplicate", ["INVALID_POSITIVE_DECIMAL"]="importPositiveDecimal"
    };
    private static readonly IReadOnlyDictionary<string,string> Suggestions = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["SCOPE_NOT_CONFIGURED"]="importFixScope", ["MEASURED_VALUE_REQUIRED"]="importFixValueRequired",
        ["INVALID_MEASURED_VALUE"]="importFixValueNumeric", ["INVALID_MEASURED_AT"]="importFixMeasuredAt",
        ["PART_NOT_FOUND"]="importFixPart", ["PROCESS_NOT_FOUND"]="importFixProcess",
        ["MACHINE_NOT_FOUND"]="importFixMachine", ["MACHINE_PROCESS_MISMATCH"]="importFixMachineProcess",
        ["INVALID_SAMPLING_STAGE"]="chemicalSamplingStageInvalid", ["TANK_REQUIRED"]="importFixTankRequired",
        ["TANK_NOT_FOUND"]="importFixTank", ["CHAR_NOT_FOUND"]="importFixCharacteristic",
        ["INVALID_DATA_CATEGORY"]="importFixDataCategory", ["UNIT_MISMATCH"]="importFixUnit",
        ["MAPPING_NOT_FOUND"]="importFixMapping", ["MAPPING_AMBIGUOUS"]="importFixAmbiguous"
    };

    /// <summary>Returns a safe summary for a known code, keeping the original message as legacy evidence.</summary>
    public static ApiUiError? Describe(string? code, string original, string? field)
    {
        if(code is null || !Messages.TryGetValue(code,out var key))return null;
        if(key is "importIntegerField" or "importPositiveDecimal")
        {
            if(string.IsNullOrWhiteSpace(field))return ApiUiError.Create(code,"spc.api.importFieldInvalid") with { Message=original };
            return ApiUiError.Create(code,"spc.api."+key,new Dictionary<string,string>{{"field",field}}) with { Message=original };
        }
        return ApiUiError.Create(code,"spc.api."+key) with { Message=original };
    }

    /// <summary>Formats the same explicit correction category without reading or changing historical master data.</summary>
    public static ApiUiError? Suggest(string? code, string original, string? field)
    {
        if(code is not null && Suggestions.TryGetValue(code,out var key))return ApiUiError.Create("IMPORT_SUGGESTION","spc.api."+key) with { Message=original };
        return string.IsNullOrWhiteSpace(field)?null:ApiUiError.Create("IMPORT_SUGGESTION","spc.api.importFixField",new Dictionary<string,string>{{"field",field}}) with { Message=original };
    }
}
