using System.Data.Common;
using MesSpc.Api.Services;

public class EtchPreviewFailureResponseTests
{
    [Fact]
    public void DatabaseException_ReturnsSafe503WithTraceId()
    {
        var result = EtchPreviewFailureResponses.Create(new TestDbException("Server=secret; SQL detail"), "trace-123");

        Assert.Equal(503, result.StatusCode);
        Assert.Equal("ETCH_PREVIEW_DATABASE_FAILURE", result.ErrorCode);
        Assert.Contains("trace-123", result.Message);
        Assert.DoesNotContain("secret", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQL detail", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnexpectedException_ReturnsSafe500WithTraceId()
    {
        var result = EtchPreviewFailureResponses.Create(new InvalidCastException("internal detail"), "trace-456");

        Assert.Equal(500, result.StatusCode);
        Assert.Equal("ETCH_PREVIEW_SERVER_FAILURE", result.ErrorCode);
        Assert.Contains("trace-456", result.Message);
        Assert.DoesNotContain("internal detail", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestDbException(string message) : DbException(message);
}
