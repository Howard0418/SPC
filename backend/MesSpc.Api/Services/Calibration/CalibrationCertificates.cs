using System.Security.Cryptography;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Calibration;

/// <summary>校正證書採私有儲存；不提供可繞過授權的靜態網址。</summary>
public sealed class CalibrationCertificates(AppDbContext db, IWebHostEnvironment env, IConfiguration config, TimeProvider clock, InstrumentCalibrationService service)
{
    public const long MaxBytes = 10*1024*1024;
    public static bool ValidContent(string ext, byte[] bytes) => ext.ToLowerInvariant() switch
    {
        ".pdf" => bytes.AsSpan().StartsWith("%PDF-"u8),
        ".jpg" or ".jpeg" => bytes.Length>=3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255,
        ".png" => bytes.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}), _ => false
    };
    public string Root => Path.GetFullPath(config["Calibration:CertificateRoot"] ?? Path.Combine(env.ContentRootPath,"private-data","calibration-certificates"));
    public string Resolve(string key)
    {
        if (Path.GetFileName(key)!=key || key.Contains('/') || key.Contains('\\')) throw new CalibrationException("無效附件路徑。");
        return Path.Combine(Root,key);
    }
    public async Task<CalibrationCertificate> UploadAsync(int recordId,IFormFile file,string actor,CancellationToken ct)
    {
        var record = await db.Set<InstrumentCalibrationRecord>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==recordId,ct) ?? throw new CalibrationException("校正紀錄不存在。",404,"NOT_FOUND");
        if (file.Length<=0 || file.Length>MaxBytes) throw new CalibrationException("附件需為非空檔案且不超過 10 MB。",413,"FILE_SIZE");
        var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var source=file.OpenReadStream(); using var memory=new MemoryStream();
        var buffer=new byte[81920]; int read;
        while((read=await source.ReadAsync(buffer,ct))>0) { if(memory.Length+read>MaxBytes) throw new CalibrationException("附件超過 10 MB。",413,"FILE_SIZE"); await memory.WriteAsync(buffer.AsMemory(0,read),ct); }
        var bytes=memory.ToArray();
        if(!ValidContent(ext,bytes)) throw new CalibrationException("僅接受有效 PDF／JPEG／PNG。",415,"FILE_TYPE");
        var key=$"{Guid.NewGuid():N}{ext}"; Directory.CreateDirectory(Root); var path=Resolve(key);
        await File.WriteAllBytesAsync(path,bytes,ct);
        var row=new CalibrationCertificate { CalibrationRecordId=recordId, StorageKey=key, OriginalName=Path.GetFileName(file.FileName),
            ContentType=ext==".pdf"?"application/pdf":ext==".png"?"image/png":"image/jpeg", Size=bytes.Length,
            Hash=Convert.ToHexString(SHA256.HashData(bytes)), CreatedBy=actor, CreatedAt=clock.GetUtcNow().UtcDateTime };
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.Add(row); service.Audit(record.InstrumentId,actor,"CertificateAdded",null,new { recordId,row.OriginalName,row.Hash },null);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return row;
        }
        catch { File.Delete(path); throw; }
    }
}
