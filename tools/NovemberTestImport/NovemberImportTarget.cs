namespace NovemberTestImport;

public sealed record NovemberImportTarget(bool IsProduction,string Environment,string SpcDatabase,string PortalDatabase,string Mode)
{
 public static NovemberImportTarget Parse(string[] args,string reconciliationSha256)
 {
  var mode=args.ElementAtOrDefault(0)??"--plan";
  var production=mode.StartsWith("--prod-",StringComparison.Ordinal);
  var normalized=production?"--"+mode[7..]:mode;
  if(normalized is not ("--plan" or "--apply" or "--verify"))throw new ArgumentException("--plan|--apply|--verify|--prod-plan|--prod-apply|--prod-verify");
  var target=production
   ?new NovemberImportTarget(true,"production","PMR_SPC_2026","PMR_PORTAL_UAT",normalized)
   :new NovemberImportTarget(false,"test","PMR_SPC_TEST","PMR_PORTAL_TEST",normalized);
  if(production&&normalized=="--apply")
  {
   string? Value(string name)=>args.FirstOrDefault(x=>x.StartsWith(name+"=",StringComparison.Ordinal))?[(name.Length+1)..];
   if(Value("--confirm-spc")!="PMR_SPC_2026"||Value("--confirm-portal")!="PMR_PORTAL_UAT"||Value("--confirm-reconciliation-sha256")!=reconciliationSha256)
    throw new InvalidOperationException("Production apply requires exact database and reconciliation SHA-256 confirmations.");
  }
  return target;
 }
}
