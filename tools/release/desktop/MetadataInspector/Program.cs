using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal static class Rules {
 internal static bool IsMigration(string? id,string? context,string expected,bool concrete,string baseType) {
  if(id==null || context==null || context.Split(',')[0].Trim()!=expected)return false;
  if(!concrete || baseType!="Microsoft.EntityFrameworkCore.Migrations.Migration")throw new InvalidDataException("Unsupported migration inheritance; provide reviewed EF catalogue instead.");
  if(!Regex.IsMatch(id,@"^\d{14}_[A-Za-z0-9_]+$"))throw new InvalidDataException("Invalid migration attribute ID.");
  return true;
 }
 internal static int Tests() {
  int count=0;string ctx="Expected.Context";
  void Check(bool ok){if(!ok)throw new Exception("Metadata rule assertion failed");count++;}
  Check(IsMigration("20261008183026_AddRemarks",ctx,ctx,true,"Microsoft.EntityFrameworkCore.Migrations.Migration"));
  Check(!IsMigration("20261008183026_FTable",null,ctx,true,"Microsoft.EntityFrameworkCore.Migrations.Migration"));
  Check(!IsMigration("20261008183026_Other","Other.Context",ctx,true,"Microsoft.EntityFrameworkCore.Migrations.Migration"));
  Check(!IsMigration(null,ctx,ctx,true,"Microsoft.EntityFrameworkCore.Migrations.Migration"));
  bool rejected=false;try{IsMigration("bad",ctx,ctx,true,"Microsoft.EntityFrameworkCore.Migrations.Migration");}catch{rejected=true;}Check(rejected);
  rejected=false;try{IsMigration("20261008183026_Test",ctx,ctx,true,"Unknown.Base");}catch{rejected=true;}Check(rejected);
  Console.WriteLine($"TOTAL {count} metadata rules passed");return 0;
 }
}
internal sealed class InspectionResult {
 public string ApiVersion {get;set;}="";public string SourceCommit {get;set;}="";public string ApiSha256 {get;set;}="";public string[] MigrationIds {get;set;}=[];
}
[JsonSerializable(typeof(InspectionResult))]
internal partial class InspectorJson:JsonSerializerContext {}
internal static class Program {
 static string TypeName(MetadataReader reader,EntityHandle h) {if(h.IsNil)return "";return h.Kind switch {
  HandleKind.TypeReference=>Name(reader,reader.GetTypeReference((TypeReferenceHandle)h).Namespace,reader.GetTypeReference((TypeReferenceHandle)h).Name),
  HandleKind.TypeDefinition=>Name(reader,reader.GetTypeDefinition((TypeDefinitionHandle)h).Namespace,reader.GetTypeDefinition((TypeDefinitionHandle)h).Name),_=>"unsupported"
 };}
 static string Name(MetadataReader r,StringHandle ns,StringHandle n)=>r.GetString(ns)+"."+r.GetString(n);
 static string AttributeName(MetadataReader r,CustomAttribute a)=>a.Constructor.Kind switch {
  HandleKind.MemberReference=>TypeName(r,r.GetMemberReference((MemberReferenceHandle)a.Constructor).Parent),
  HandleKind.MethodDefinition=>TypeName(r,r.GetMethodDefinition((MethodDefinitionHandle)a.Constructor).GetDeclaringType()),_=>"unsupported"
 };
 static string? Value(MetadataReader r,CustomAttribute a){var b=r.GetBlobReader(a.Value);if(b.ReadUInt16()!=1)throw new InvalidDataException("Invalid custom attribute blob");return b.ReadSerializedString();}
 public static int Main(string[] args) {
  try {
   if(args.Length==1 && args[0]=="--self-test")return Rules.Tests();
   if(args.Length!=3 || args[0] is not ("SPC" or "Portal"))throw new ArgumentException("Expected System API-DLL output-json");
   string expected=args[0]=="SPC"?"MesSpc.Api.Infrastructure.Data.AppDbContext":"PmrPortal.Api.Data.AppDbContext";
   byte[] bytes=File.ReadAllBytes(args[1]);using var stream=new MemoryStream(bytes);using var pe=new PEReader(stream);
   if(!pe.HasMetadata)throw new InvalidDataException("Managed API metadata required");var reader=pe.GetMetadataReader();var assembly=reader.GetAssemblyDefinition();
   string assemblyName=reader.GetString(assembly.Name);if(assemblyName!=(args[0]=="SPC"?"MesSpc.Api":"PmrPortal.Api"))throw new InvalidDataException("Wrong API assembly for selected system");
   var result=new InspectionResult {ApiVersion=$"{assembly.Version.Major}.{assembly.Version.Minor}.{assembly.Version.Build}",ApiSha256=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()};
   foreach(var handle in assembly.GetCustomAttributes()){var a=reader.GetCustomAttribute(handle);if(AttributeName(reader,a)=="System.Reflection.AssemblyInformationalVersionAttribute"){var match=Regex.Match(Value(reader,a)??"",@"(?:\+|\.)((?:[a-fA-F0-9]){40})(?:$|[^a-fA-F0-9])");if(match.Success)result.SourceCommit=match.Groups[1].Value.ToLowerInvariant();}}
   var ids=new List<string>();foreach(var h in reader.TypeDefinitions){var type=reader.GetTypeDefinition(h);string? id=null,context=null;foreach(var handle in type.GetCustomAttributes()){var a=reader.GetCustomAttribute(handle);string name=AttributeName(reader,a);if(name=="Microsoft.EntityFrameworkCore.Migrations.MigrationAttribute")id=Value(reader,a);if(name=="Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute")context=Value(reader,a);}
    if(Rules.IsMigration(id,context,expected,(type.Attributes&TypeAttributes.Abstract)==0 && type.GetGenericParameters().Count==0,TypeName(reader,type.BaseType)))ids.Add(id!);
   }
   if(ids.Count==0 || ids.Distinct().Count()!=ids.Count)throw new InvalidDataException("Empty or duplicate EF migration catalogue");result.MigrationIds=ids.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
   File.WriteAllText(args[2],JsonSerializer.Serialize(result,InspectorJson.Default.InspectionResult));Console.WriteLine("Static API metadata inspected; no assembly executed or database opened.");return 0;
  }catch(Exception ex){Console.Error.WriteLine("Metadata inspection failed: "+ex.GetType().Name+" "+ex.Message);return 1;}
 }
}
