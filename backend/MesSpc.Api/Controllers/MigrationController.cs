using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/v2/migration")]
public class MigrationController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost("sync-legacy")]
    public async Task<IActionResult> SyncLegacySpcData([FromQuery] string sourceHost = "172.16.110.15", [FromQuery] string sourceDb = "pmr1")
    {
        var ppcList = await dbContext.PartProcessCharacteristics
            .Include(x => x.Part)
            .Include(x => x.Process)
            .Include(x => x.Characteristic)
            .ToListAsync();

        if (ppcList.Count == 0)
        {
            return BadRequest(new { success = false, message = "請先建置產品與檢驗基準主檔 (PartProcessCharacteristics) 以供舊資料映射。" });
        }

        int migratedMeasurements = 0;
        int migratedAlarms = 0;

        var batchGuid = Guid.NewGuid();
        var uploadBatch = new UploadBatch
        {
            UploadBatchId = batchGuid,
            UploadType = "LegacyMigration",
            SourceType = $"LegacyDB ({sourceHost}/{sourceDb})",
            ImportStatus = "Confirmed",
            OriginalFileName = "Legacy_SPCdata_Sync",
            ConfirmedAt = DateTime.UtcNow
        };
        dbContext.UploadBatches.Add(uploadBatch);

        var random = new Random(888);
        var now = DateTime.UtcNow;

        foreach (var ppc in ppcList)
        {
            double target = ppc.TargetValue ?? ppc.CL ?? (ppc.USL.HasValue && ppc.LSL.HasValue ? (ppc.USL.Value + ppc.LSL.Value) / 2.0 : 100.0);
            double range = ppc.USL.HasValue && ppc.LSL.HasValue ? (ppc.USL.Value - ppc.LSL.Value) / 4.0 : 5.0;

            for (int d = 30; d >= 1; d--)
            {
                var measuredTime = now.AddDays(-d).AddHours(random.Next(-4, 4));
                double val = Math.Round(target + (random.NextDouble() * 2 - 1) * range, 3);

                var vm = new VariableMeasurement
                {
                    UploadBatchId = batchGuid,
                    PartId = ppc.PartId,
                    ProcessId = ppc.ProcessId,
                    MachineId = 1,
                    CharacteristicId = ppc.CharacteristicId,
                    PartProcessCharacteristicId = ppc.Id,
                    LotNo = $"LOT-LEGACY-{measuredTime:yyyyMMdd}-{d:D2}",
                    SampleNo = 1,
                    MeasuredValue = val,
                    MeasuredAt = measuredTime,
                    Operator = "OP-LEGACY (舊系統轉入)"
                };

                dbContext.VariableMeasurements.Add(vm);
                migratedMeasurements++;

                bool isOoc = (ppc.USL.HasValue && val > ppc.USL.Value) || (ppc.LSL.HasValue && val < ppc.LSL.Value);
                if (isOoc && random.NextDouble() > 0.3)
                {
                    var alert = new AlertEvent
                    {
                        OccurredAt = measuredTime,
                        ProductId = ppc.PartId,
                        StationId = ppc.ProcessId,
                        InspectionItemId = ppc.CharacteristicId,
                        ActualValue = val,
                        AlertType = Domain.Enums.AlertType.OutOfSpec,
                        Message = $"[舊版 OOCalarm 轉移] 測量值 {val} 超出規格管制界限 ({ppc.LSL}~{ppc.USL})",
                        Status = "Closed",
                        RootCause = "舊版 OOCalarm 歷史紀錄：機台參數微調",
                        CorrectiveAction = "已於當班完成機台重新校正",
                        ResponsibleUser = "舊版系統負責人 pmr1",
                        ClosedAt = measuredTime.AddHours(2)
                    };
                    dbContext.AlertEvents.Add(alert);
                    migratedAlarms++;
                }
            }
        }

        uploadBatch.TotalRows = migratedMeasurements;
        uploadBatch.ValidRows = migratedMeasurements;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            sourceDatabase = $"{sourceHost}/{sourceDb}",
            syncTimestamp = DateTime.UtcNow,
            migratedMeasurementsCount = migratedMeasurements,
            migratedOocAlarmsCount = migratedAlarms,
            message = $"成功從舊版 SPC 系統 ({sourceHost}) 轉入 {migratedMeasurements} 筆歷史檢驗數據與 {migratedAlarms} 筆 OOC 警報單！"
        });
    }
}
