using MesSpc.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Infrastructure.Data;

public static class CalibrationModelConfiguration
{
    public static void ConfigureCalibration(this ModelBuilder b)
    {
        var i = b.Entity<CalibrationInstrument>();
        i.ToTable("CalibrationInstruments", t => t.HasComment("儀器校正主檔"));
        i.HasIndex(x => x.Code).IsUnique();
        i.Property(x => x.Code).HasMaxLength(80); i.Property(x => x.Name).HasMaxLength(200);
        i.Property(x => x.Department).HasMaxLength(100); i.Property(x => x.Location).HasMaxLength(100);
        i.Property(x => x.CalibrationMethod).HasMaxLength(100); i.Property(x => x.UsageStatus).HasMaxLength(30);
        i.Property(x => x.MeasurementSpecification).HasMaxLength(2000);
        i.Property(x => x.Precision).HasMaxLength(2000);
        i.Property(x => x.Remarks).HasMaxLength(2000);
        i.Property(x => x.CalibrationStandard).HasMaxLength(2000);
        i.Property(x => x.AcceptanceCriteria).HasMaxLength(2000);
        i.Property(x => x.Version).IsConcurrencyToken();
        i.HasOne<Operator>().WithMany().HasForeignKey(x => x.CustodianOperatorId).OnDelete(DeleteBehavior.Restrict);
        var r = b.Entity<InstrumentCalibrationRecord>();
        r.ToTable("InstrumentCalibrationRecords", t => t.HasComment("不可覆寫的校正歷史"));
        r.HasIndex(x => new { x.InstrumentId, x.RequestId }).IsUnique();
        r.HasOne<CalibrationInstrument>().WithMany().HasForeignKey(x => x.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        r.HasOne<InstrumentCalibrationRecord>().WithMany().HasForeignKey(x => x.CorrectsRecordId).OnDelete(DeleteBehavior.Restrict);
        var c = b.Entity<CalibrationCertificate>();
        c.ToTable("CalibrationCertificates", t => t.HasComment("校正證書私有檔案索引"));
        c.HasOne<InstrumentCalibrationRecord>().WithMany().HasForeignKey(x => x.CalibrationRecordId).OnDelete(DeleteBehavior.Restrict);
        var s = b.Entity<CalibrationNotificationSetting>();
        s.ToTable("CalibrationNotificationSettings", t => t.HasComment("校正提醒設定"));
        s.Property(x => x.Id).ValueGeneratedNever(); s.Property(x => x.Version).IsConcurrencyToken();
        s.Property(x => x.NotificationChannel).HasMaxLength(20).HasDefaultValue("Email");
        s.Property(x => x.ChatWebhookProtected).HasMaxLength(8192);
        var n = b.Entity<CalibrationNotification>();
        n.Property(x => x.Channel).HasMaxLength(20).HasDefaultValue("Email");
        n.ToTable("CalibrationNotifications", t => t.HasComment("逐收件人校正通知工作"));
        n.Property(x => x.Stage).HasMaxLength(40); n.Property(x => x.RecipientKey).HasMaxLength(254);
        n.Property(x => x.State).HasMaxLength(20); n.Property(x => x.Version).IsConcurrencyToken();
        n.HasIndex(x => new { x.InstrumentId, x.CycleId, x.DueDate, x.Stage, x.RecipientKey }).IsUnique();
        n.HasIndex(x => new { x.State, x.NextAttemptAt });
        n.HasOne<CalibrationInstrument>().WithMany().HasForeignKey(x => x.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        var a = b.Entity<CalibrationNotificationAttempt>();
        a.ToTable("CalibrationNotificationAttempts", t => t.HasComment("通知逐次嘗試與結果"));
        a.HasOne<CalibrationNotification>().WithMany().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CalibrationAuditLog>().ToTable("CalibrationAuditLogs", t => t.HasComment("校正異動稽核"));
        foreach (var type in new[] { typeof(CalibrationInstrument), typeof(InstrumentCalibrationRecord), typeof(CalibrationCertificate), typeof(CalibrationNotificationSetting), typeof(CalibrationNotification), typeof(CalibrationNotificationAttempt), typeof(CalibrationAuditLog) })
            foreach (var property in b.Entity(type).Metadata.GetProperties()) property.SetComment($"校正模組 {property.Name}");
    }
}
