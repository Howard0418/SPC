using MesSpc.Api.Services;

namespace MesSpc.Chemical.Tests;

public class AuthenticationStartupPlannerTests
{
    [Fact]
    public void JwtKey_ConfiguresScheme_WhenGlobalAuthIsDisabled()
    {
        Assert.True(AuthenticationStartupPlanner.ShouldConfigureJwt("test-key"));
        Assert.False(AuthenticationStartupPlanner.RequiresJwtKey(false, "test-key"));
    }

    [Fact]
    public void GlobalAuth_RequiresJwtKey()
    {
        Assert.True(AuthenticationStartupPlanner.RequiresJwtKey(true, ""));
        Assert.False(AuthenticationStartupPlanner.ShouldConfigureJwt(""));
    }
}
