using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Calibration;

public record CalibrationImportOptions(string Hash, int[] SelectedRows, string Department, int CustodianOperatorId, string UsageStatus, bool IncludeCustodian);
public record CalibrationImportOutcome(int RowNumber, string Code, string Status, string Message);
public record CalibrationImportResult(int Added, int Skipped, int Failed, List<CalibrationImportOutcome> Rows);

/// <summary>匯入預覽與僅新增交易。提交重讀來源、重新驗證，唯一編號索引是競爭寫入最後防線。</summary>
public sealed class CalibrationImportService(AppDbContext db, InstrumentCalibrationService instruments, TimeProvider clock, ILogger<CalibrationImportService> logger)
{
    /// <summary>解析來源並以目前資料庫標示既有編號；不寫入。</summary>
    public async Task<CalibrationImportPreview> PreviewAsync(byte[] bytes, CancellationToken ct)
    {
        var preview = CalibrationImportParser.Parse(bytes,clock);
        var existing = (await db.Set<CalibrationInstrument>().AsNoTracking().Select(x=>x.Code).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in preview.Rows.Where(x=>existing.Contains(x.Code)))
        { row.Status = row.Errors.Count==0 ? "Existing" : "Invalid"; row.Warnings.Add("資料庫已存在相同編號，將略過、不覆寫。"); }
        return preview;
    }

    /// <summary>只接受來源列號與補齊欄位；新增、逐筆稽核與批次結果同交易，失敗全部回復。</summary>
    public async Task<CalibrationImportResult> CommitAsync(byte[] bytes, string fileName, CalibrationImportOptions options, string actor, CancellationToken ct)
    {
        var preview = CalibrationImportParser.Parse(bytes,clock);
        if (!string.Equals(preview.Hash,options.Hash,StringComparison.Ordinal)) throw new CalibrationException("檔案與預覽不同，請重新預覽。");
        if (options.SelectedRows is null || options.SelectedRows.Length==0 || options.SelectedRows.Distinct().Count()!=options.SelectedRows.Length)
            throw new CalibrationException("請選擇不重複的有效資料列。");
        var selected = preview.Rows.Where(x=>options.SelectedRows.Contains(x.RowNumber)).ToList();
        if (selected.Count!=options.SelectedRows.Length) throw new CalibrationException("所選列號不在預覽中，請重新預覽。");
        fileName = Path.GetFileName(fileName.Replace('\\','/'));
        if (fileName.Length>255) fileName=fileName[..255];
        logger.LogInformation("Calibration import requested by {Actor}: {File}, {Count} rows, SHA256 {Hash}",actor,fileName,selected.Count,preview.Hash);
        var outcomes = new List<CalibrationImportOutcome>();
        var inserted = new List<(CalibrationImportRow Source, CalibrationInstrument Entity)>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var row in selected)
            {
                if (row.Errors.Count>0) throw new CalibrationException($"第 {row.RowNumber} 列：{string.Join("；",row.Errors)}");
                if (await db.Set<CalibrationInstrument>().AsNoTracking().AnyAsync(x=>x.Code==row.Code,ct))
                { outcomes.Add(new(row.RowNumber,row.Code,"Skipped","編號已存在，未變更既有資料。")); continue; }
                var input = new InstrumentInput(row.Code,row.Name,options.Department,options.CustodianOperatorId,row.CycleMonths!.Value,
                    row.LastCalibrationDate,row.NextCalibrationDate,options.UsageStatus,[],options.IncludeCustodian,null,"Excel 主檔初始化",row.Location,row.CalibrationMethod,row.MeasurementSpecification,row.Precision,row.Remarks,row.CalibrationStandard,row.AcceptanceCriteria);
                try { await instruments.ValidateInputAsync(input,ct); }
                catch (CalibrationException ex) { throw new CalibrationException($"第 {row.RowNumber} 列：{ex.Message}"); }
                var entity = new CalibrationInstrument { Code=input.Code,Name=input.Name,Department=input.Department.Trim(),
                    Location=string.IsNullOrWhiteSpace(row.Location) ? null : row.Location.Trim(),
                    CalibrationMethod=string.IsNullOrWhiteSpace(row.CalibrationMethod) ? null : row.CalibrationMethod.Trim(),
                    MeasurementSpecification=string.IsNullOrWhiteSpace(row.MeasurementSpecification) ? null : row.MeasurementSpecification.Trim(),
                    Precision=string.IsNullOrWhiteSpace(row.Precision) ? null : row.Precision.Trim(),
                    Remarks=string.IsNullOrWhiteSpace(row.Remarks) ? null : row.Remarks.Trim(),
                    CalibrationStandard=string.IsNullOrWhiteSpace(row.CalibrationStandard) ? null : row.CalibrationStandard.Trim(),
                    AcceptanceCriteria=string.IsNullOrWhiteSpace(row.AcceptanceCriteria) ? null : row.AcceptanceCriteria.Trim(),
                    CustodianOperatorId=input.CustodianOperatorId,CycleMonths=input.CycleMonths,LastCalibrationDate=input.LastCalibrationDate,
                    NextCalibrationDate=input.NextCalibrationDate,UsageStatus=input.UsageStatus,IncludeCustodian=input.IncludeCustodian };
                db.Add(entity); inserted.Add((row,entity));
            }
            await db.SaveChangesAsync(ct);
            foreach (var (source, entity) in inserted)
            {
                outcomes.Add(new(source.RowNumber,source.Code,"Added","已新增儀器主檔，未建立校正結果。"));
                instruments.Audit(entity.Id,actor,"InstrumentImported",null,new { FileName=fileName,preview.Hash,source.RowNumber,Instrument=entity },"Excel 主檔初始化");
            }
            var result=Result(outcomes);
            instruments.Audit(null,actor,"InstrumentImportCompleted",null,new { FileName=fileName,preview.Hash,Result=result },"僅新增匯入");
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Calibration import completed: {Added} added, {Skipped} skipped",result.Added,result.Skipped);
            return result;
        }
        catch (Exception ex) when (ex is CalibrationException or DbUpdateException or System.Data.Common.DbException)
        {
            await tx.RollbackAsync(ct);
            // Rolled-back Added entities must not be re-saved with the failure audit.
            db.ChangeTracker.Clear();
            logger.LogWarning(ex,"Calibration import rolled back for {Actor}, SHA256 {Hash}",actor,preview.Hash);
            var reason = ex is CalibrationException ? ex.Message : "資料庫寫入失敗，本次新增已全部回復；請重新預覽再試。";
            var failed = Result(selected.Select(x=>new CalibrationImportOutcome(x.RowNumber,x.Code,"Failed",reason)).ToList());
            // End the completed transaction before a fresh, independent failure audit write.
            await tx.DisposeAsync();
            instruments.Audit(null,actor,"InstrumentImportFailed",null,new { FileName=fileName,preview.Hash,Result=failed },reason);
            await db.SaveChangesAsync(ct);
            return failed;
        }
    }
    private static CalibrationImportResult Result(List<CalibrationImportOutcome> rows)
        => new(rows.Count(x=>x.Status=="Added"),rows.Count(x=>x.Status=="Skipped"),rows.Count(x=>x.Status=="Failed"),rows.OrderBy(x=>x.RowNumber).ToList());
}
