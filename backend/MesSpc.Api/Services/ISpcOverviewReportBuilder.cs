namespace MesSpc.Api.Services;

public interface ISpcOverviewReportBuilder
{
    Task<byte[]> BuildExcelAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default, string? uiLocale = null);
}
