using System.Net.Mail;
using System.Text.Json;

namespace MesSpc.Api.Services;

public sealed record AlertRecipientSelection(int[] OutOfSpec, int[] OutOfControl, int[] AdjustmentFormula, int[] ConcentrationFormula)
{
    public const string ConfigurationKey = "SpcAlertRecipientSelection";

    public static AlertRecipientSelection? Read(IConfiguration configuration)
    {
        var value = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonSerializer.Deserialize<AlertRecipientSelection>(value); }
        catch (JsonException) { return null; }
    }

    public static bool IsValidEmail(string? email) =>
        MailAddress.TryCreate(email?.Trim(), out var address) && address.Address == email!.Trim();
}
