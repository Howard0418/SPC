namespace NovemberTestImport;

public sealed record NovemberImportTarget(bool IsProduction,string Environment,string SpcDatabase,string PortalDatabase,string Mode,DateOnly? DailyDate)
{
 public static NovemberImportTarget Parse(string[] args,string reconciliationSha256)
 {
  var mode=args.ElementAtOrDefault(0)??"--plan";
  var production=mode.StartsWith("--prod-",StringComparison.Ordinal);
  var normalized=production?"--"+mode[7..]:mode;
  if(normalized is not ("--plan" or "--apply" or "--verify"))throw new ArgumentException("--plan|--apply|--verify|--prod-plan|--prod-apply|--prod-verify");
  var dailyValue=args.FirstOrDefault(x=>x.StartsWith("--daily-date=",StringComparison.Ordinal))?[13..];
  DateOnly? dailyDate=null;
  if(dailyValue is not null)
  {
   if(!production||!DateOnly.TryParseExact(dailyValue,"yyyy-MM-dd",out var parsed))throw new InvalidOperationException("--daily-date requires production mode and yyyy-MM-dd.");
   dailyDate=parsed;
  }
  var target=production
   ?new NovemberImportTarget(true,"production","PMR_SPC_2026","PMR_PORTAL_UAT",normalized,dailyDate)
   :new NovemberImportTarget(false,"test","PMR_SPC_TEST","PMR_PORTAL_TEST",normalized,null);
  if(production&&normalized=="--apply")
  {
   string? Value(string name)=>args.FirstOrDefault(x=>x.StartsWith(name+"=",StringComparison.Ordinal))?[(name.Length+1)..];
   if(Value("--confirm-spc")!="PMR_SPC_2026"||Value("--confirm-portal")!="PMR_PORTAL_UAT"||Value("--confirm-reconciliation-sha256")!=reconciliationSha256)
    throw new InvalidOperationException("Production apply requires exact database and reconciliation SHA-256 confirmations.");
  }
  return target;
 }
}
