using FluentAssertions;
using MesSpc.Api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Tests;

public class ApiErrorResponseFactoryTests
{
    [Fact]
    public void FromException_ShouldHideDetails_WhenDetailsDisabled()
    {
        var response = ApiErrorResponseFactory.FromException(
            new InvalidOperationException("Sensitive SQL connection failed", new Exception("inner-secret")),
            includeDetails: false);

        response.Success.Should().BeFalse();
        response.Code.Should().Be(ApiErrorResponseFactory.UnexpectedErrorCode);
        response.Message.Should().NotContain("Sensitive SQL");
        response.Detail.Should().BeNull();
    }

    [Fact]
    public void FromException_ShouldIncludeDetails_WhenDetailsEnabled()
    {
        var response = ApiErrorResponseFactory.FromException(
            new InvalidOperationException("Sensitive diagnostic message", new Exception("inner-diagnostic")),
            includeDetails: true);

        response.Message.Should().Contain("Sensitive diagnostic message");
        response.Detail.Should().Be("inner-diagnostic");
    }

    [Fact]
    public void FromException_ShouldHideDatabaseInnerDetail_WhenDetailsDisabled()
    {
        var exception = new DbUpdateException(
            "outer db error",
            new InvalidOperationException("FOREIGN KEY constraint failed on dbo.SecretTable"));

        var response = ApiErrorResponseFactory.FromException(exception, includeDetails: false);

        response.Code.Should().Be(ApiErrorResponseFactory.ForeignKeyConstraintCode);
        response.Message.Should().NotContain("SecretTable");
        response.Detail.Should().BeNull();
    }
}
