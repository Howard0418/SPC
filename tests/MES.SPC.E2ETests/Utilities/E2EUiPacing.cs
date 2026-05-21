namespace MES.SPC.E2ETests.Utilities;

/// <summary>
/// E2E 步驟間暫停，讓有界面測試不會一閃而過。環境變數 E2E_STEP_DELAY_MS（預設 800）。
/// </summary>
public static class E2EUiPacing
{
    private static readonly int StepDelayMs = ParseDelay();

    public static Task PauseAsync(string? reason = null)
    {
        if (StepDelayMs <= 0) return Task.CompletedTask;
        return Task.Delay(StepDelayMs);
    }

    private static int ParseDelay()
    {
        var raw = Environment.GetEnvironmentVariable("E2E_STEP_DELAY_MS");
        return int.TryParse(raw, out var ms) ? Math.Max(0, ms) : 800;
    }
}
