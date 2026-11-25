using MesSpc.Api.Services;

namespace MesSpc.Api.Tests;

public sealed class DatabaseRefreshRehearsalApiTests
{
    [Theory]
    [InlineData("Development", "local", true, true, 27, true)]
    [InlineData("Development", "local", true, false, 27, false)]
    [InlineData("Development", "local", false, true, 27, false)]
    [InlineData("Development", "production", true, true, 27, false)]
    [InlineData("Development", "test", true, true, 27, false)]
    [InlineData("Production", "local", true, true, 27, false)]
    [InlineData("Development", "local", true, true, 0, false)]
    public void RegistrationRequiresExplicitLocalAuthenticatedOptIn(
        string hostEnvironment, string appEnvironment, bool auth, bool enabled, int operatorId, bool expected)
    {
        Assert.Equal(expected, DatabaseRefreshRehearsalApi.CanRegister(
            hostEnvironment, appEnvironment, auth, enabled, operatorId));
    }
}
