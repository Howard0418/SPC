using Microsoft.Extensions.Configuration;
using MES.SPC.E2ETests.Fixtures;

namespace MES.SPC.E2ETests.Utilities;

public static class ConfigReader
{
    public static TestSettings Load()
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddEnvironmentVariables(prefix: "E2E_")
            .Build();

        var settings = new TestSettings();
        cfg.Bind(settings);
        return settings;
    }
}

