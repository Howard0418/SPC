using System.Data.Common;

namespace MesSpc.Api.Services;

public sealed record EtchPreviewFailureResponse(int StatusCode, string ErrorCode, string Message);

public static class EtchPreviewFailureResponses
{
    public static EtchPreviewFailureResponse Create(Exception exception, string traceId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var suffix = $"，請提供追蹤編號 {traceId} 給系統管理員。";

        return exception is DbException
            ? new(503, "ETCH_PREVIEW_DATABASE_FAILURE", "SPC 預覽資料庫交易暫時無法完成 [ETCH_PREVIEW_DATABASE_FAILURE]" + suffix)
            : new(500, "ETCH_PREVIEW_SERVER_FAILURE", "SPC 預覽發生未預期錯誤 [ETCH_PREVIEW_SERVER_FAILURE]" + suffix);
    }
}
