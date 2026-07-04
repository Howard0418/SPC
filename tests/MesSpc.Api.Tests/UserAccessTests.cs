using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MesSpc.Api.Tests;

public class UserAccessTests
{
    [Theory]
    [InlineData("Viewer", "Viewer")]
    [InlineData("viewer", "Viewer")]
    [InlineData("Editor", "Editor")]
    [InlineData("unknown", "Viewer")]
    public void NormalizeRole_Should_Return_Supported_Role(string input, string expected)
    {
        UserRoles.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void PasswordHasher_Should_Verify_Correct_Password_Only()
    {
        var hasher = new UserPasswordHasher();
        var hash = hasher.Hash("SafePassword123!");

        hash.Should().NotContain("SafePassword123!");
        hasher.Verify("SafePassword123!", hash).Should().BeTrue();
        hasher.Verify("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Imported_Operator_Should_Default_To_Editor_Without_Login_Password()
    {
        var user = ImportedOperatorFactory.Create("OP-7788");

        user.OperatorCode.Should().Be("OP-7788");
        user.OperatorName.Should().Be("OP-7788");
        user.Username.Should().Be("OP-7788");
        user.Role.Should().Be(UserRoles.Editor);
        user.PasswordHash.Should().BeNull();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ViewerWriteGuard_Should_Block_Post_And_Allow_Get()
    {
        var nextCalled = false;
        var middleware = new ViewerWriteGuardMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var viewer = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "viewer"), new Claim(ClaimTypes.Role, UserRoles.Viewer)],
            "test"));

        var post = new DefaultHttpContext { User = viewer };
        post.Request.Method = HttpMethods.Post;
        post.Request.Path = "/api/operators";
        await middleware.InvokeAsync(post);
        post.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();

        var get = new DefaultHttpContext { User = viewer };
        get.Request.Method = HttpMethods.Get;
        get.Request.Path = "/api/operators";
        await middleware.InvokeAsync(get);
        nextCalled.Should().BeTrue();
    }
}
