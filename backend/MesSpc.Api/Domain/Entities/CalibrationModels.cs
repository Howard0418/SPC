namespace MesSpc.Api.Domain.Entities;

public class CalibrationInstrument
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string? Location { get; set; }
    public string? CalibrationMethod { get; set; }
    public string? MeasurementSpecification { get; set; }
    
    public string? Precision { get; set; }
    public string? Remarks { get; set; }
    public string? CalibrationStandard { get; set; }
    public string? AcceptanceCriteria { get; set; }

    public int CustodianOperatorId { get; set; }
    public int CycleMonths { get; set; } = 12;
    public DateOnly? LastCalibrationDate { get; set; }
    public DateOnly? NextCalibrationDate { get; set; }
    public string UsageStatus { get; set; } = "Active";
    public string? LatestResult { get; set; }
    public Guid CurrentCycleId { get; set; } = Guid.NewGuid();
    public Guid Version { get; set; } = Guid.NewGuid();
    public string RecipientOperatorIdsJson { get; set; } = "[]";
    public bool IncludeCustodian { get; set; } = true;
    public string? NotificationIssue { get; set; }
}

public class InstrumentCalibrationRecord
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public DateOnly CalibrationDate { get; set; }
    public string Result { get; set; } = "Passed";
    public DateOnly PreviousDueDate { get; set; }
    public DateOnly NextDueDate { get; set; }
    public string Reason { get; set; } = "";
    public int? CorrectsRecordId { get; set; }
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CalibrationCertificate
{
    public int Id { get; set; }
    public int CalibrationRecordId { get; set; }
    public string StorageKey { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string Hash { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CalibrationNotificationSetting
{
    public string NotificationChannel { get; set; } = "Email";
    public string? ChatWebhookProtected { get; set; }
    public int Id { get; set; } = 1;
    public string ReminderDaysJson { get; set; } = "[30,7,0]";
    public bool IsEnabled { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

public class CalibrationNotification
{
    public string Channel { get; set; } = "Email";
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public Guid CycleId { get; set; }
    public DateOnly DueDate { get; set; }
    public string Stage { get; set; } = "";
    public string RecipientKey { get; set; } = "";
    public string RecipientEmail { get; set; } = "";
    public string State { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime? SentAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}

public class CalibrationNotificationAttempt
{
    public int Id { get; set; }
    public int NotificationId { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string Result { get; set; } = "";
    public string? ErrorCode { get; set; }
}

public class CalibrationAuditLog
{
    public int Id { get; set; }
    public int? InstrumentId { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string AfterJson { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
