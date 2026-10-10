using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Security;

public static class ApiErrorResponseFactory
{
    public const string UnexpectedErrorCode = "UNEXPECTED_ERROR";
    public const string DatabaseUpdateErrorCode = "DATABASE_UPDATE_FAILED";
    public const string ForeignKeyConstraintCode = "FOREIGN_KEY_CONSTRAINT";

    public static ApiErrorResponse FromException(Exception? exception, bool includeDetails)
    {
        if (exception is DbUpdateException dbUpdateException)
        {
            return FromDbUpdateException(dbUpdateException, includeDetails);
        }

        var message = includeDetails
            ? exception?.Message ?? "伺服器發生未預期的錯誤"
            : "伺服器發生未預期的錯誤，請聯絡系統管理員。";

        return new ApiErrorResponse(
            Success: false,
            Code: UnexpectedErrorCode,
            Message: message,
            Detail: includeDetails ? exception?.InnerException?.Message : null);
    }

    private static ApiErrorResponse FromDbUpdateException(DbUpdateException exception, bool includeDetails)
    {
        var rawDetail = exception.InnerException?.Message ?? exception.Message;
        var isForeignKey = rawDetail.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || rawDetail.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase);

        if (isForeignKey)
        {
            return new ApiErrorResponse(
                Success: false,
                Code: ForeignKeyConstraintCode,
                Message: "此資料已被其他記錄關聯，請先刪除或解除相關聯的資料後再操作。",
                Detail: includeDetails ? rawDetail : null);
        }

        return new ApiErrorResponse(
            Success: false,
            Code: DatabaseUpdateErrorCode,
            Message: includeDetails ? "資料庫更新失敗：" + rawDetail : "資料庫更新失敗，請聯絡系統管理員。",
            Detail: includeDetails ? rawDetail : null);
    }
}

public sealed record ApiErrorResponse(bool Success, string Code, string Message, string? Detail);
