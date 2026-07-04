using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services.Security;

public static class ImportedOperatorFactory
{
    public static Operator Create(string measurementOperator)
    {
        var code = measurementOperator.Trim();
        return new Operator
        {
            OperatorCode = code,
            OperatorName = code,
            Username = code,
            Role = UserRoles.Editor,
            IsActive = true,
            PasswordHash = null,
            CreatedBy = "measurement-import"
        };
    }
}
