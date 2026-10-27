using MesSpc.Api.Controllers;
namespace MesSpc.Api.Services.Localization;

/// <summary>Pure master-name presentation; never changes saved fields, lookup identity or parsing culture.</summary>
public static class CharacteristicNamePresentation
{
    public static string Name(string? chinese, string? english, string? code, string? locale = null)
    {
        var names = ApiUiMessageCatalog.NormalizeLocale(locale) == "en-US" ? new[] { english, chinese, code } : new[] { chinese, english, code };
        return names.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? ApiUiMessageCatalog.Shared.Format("spc.api.reportUnnamedCharacteristic", locale);
    }

    public static string Summary(ChartSummaryDto item, string? locale = null) =>
        item.CharacteristicName is not null || item.CharacteristicNameEn is not null || item.CharacteristicCode is not null
            ? Name(item.CharacteristicName, item.CharacteristicNameEn, item.CharacteristicCode, locale) : item.ChartName;
}
