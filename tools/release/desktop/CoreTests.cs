using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace Pmr.ReleaseUpdater {
 internal static class CoreTests {
  static int count;
  static BundleDraft CreateDraft(string root) {var api=Path.Combine(root,"api-input");var web=Path.Combine(root,"web-input");Directory.CreateDirectory(api);Directory.CreateDirectory(web);return new BundleDraft {EnvironmentName="Test",SystemName="SPC",ApiDirectory=api,WebDirectory=web,OutputParent=root,ReleaseId="BUNDLE-001",ApiVersion="1.0.0",WebVersion="1.0.1",PreviousApiVersion="0.9.0",SourceCommit=new string('a',40),ApiSha256=""};}
  static void Check(string name, Action action) { action(); count++; Console.WriteLine("PASS " + name); }
  static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
  static void Reject(Action action) { bool rejected=false; try { action(); } catch { rejected=true; } Assert(rejected); }
  public static int Main() {
   string root=Path.Combine(Path.GetTempPath(),"updater-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
   try {
    string profiles="[{\"Environment\":\"Test\",\"System\":\"SPC\",\"HostName\":\"TEST-HOST\",\"Database\":\"TEST-DB\",\"Targets\":[{\"Role\":\"Api\",\"Path\":\"D:/test/api\"},{\"Role\":\"Web\",\"Path\":\"D:/test/web\"}]}]";
    ProfileCatalog catalog=new ProfileCatalog(profiles);
    string path=Path.Combine(root,"量測 manifest.json");
    var manifest=new Dictionary<string,object> { {"schemaVersion",1},{"environment","Test"},{"system","SPC"},{"host","TEST-HOST"},{"releaseId","TEST-001"},{"database",new Dictionary<string,object>{{"name","TEST-DB"}}},{"packages",new object[]{new Dictionary<string,object>{{"path","D:/test/api"},{"version","1.0.0"}},new Dictionary<string,object>{{"path","D:/test/web"},{"version","1.0.1"}}}} };
    Action save=()=>File.WriteAllText(path,new JavaScriptSerializer().Serialize(manifest),new UTF8Encoding(true)); save();
    ManifestInfo info=null;
    Check("BOM Unicode manifest and versions",()=>{info=ManifestInfo.Load(path,"Test","SPC",catalog);Assert(info.ApiVersion=="1.0.0" && info.WebVersion=="1.0.1");});
    Check("wrong environment blocked",()=>Reject(()=>ManifestInfo.Load(path,"Production","SPC",catalog)));
    Check("wrong host blocked",()=>{manifest["host"]="WRONG";save();Reject(()=>ManifestInfo.Load(path,"Test","SPC",catalog));manifest["host"]="TEST-HOST";save();});
    Check("wrong database blocked",()=>{manifest["database"]=new Dictionary<string,object>{{"name","PROD-DB"}};save();Reject(()=>ManifestInfo.Load(path,"Test","SPC",catalog));manifest["database"]=new Dictionary<string,object>{{"name","TEST-DB"}};save();});
    info=ManifestInfo.Load(path,"Test","SPC",catalog);
    ReleaseGate gate=new ReleaseGate();
    Check("Plan needs no authorization",()=>gate.Require("Plan",info,false,false));
    Check("Apply requires successful Plan",()=>Reject(()=>gate.Require("Apply",info,true,true)));
    gate.RecordPlan(info);
    Check("Apply requires both authorizations",()=>{Reject(()=>gate.Require("Apply",info,false,true));Reject(()=>gate.Require("Apply",info,true,false));});
    Check("Apply after Plan and authorization",()=>gate.Require("Apply",info,true,true));
    Check("changed manifest invalidates Plan",()=>{manifest["releaseId"]="TEST-002";save();ManifestInfo changed=ManifestInfo.Load(path,"Test","SPC",catalog);Reject(()=>gate.Require("Apply",changed,true,true));});
    Check("rollback does not need pending-migration Plan",()=>new ReleaseGate().Require("Rollback",info,true,true));
    Check("failed Plan reset prevents update",()=>{gate.Reset();Reject(()=>gate.Require("Apply",info,true,true));});
    Check("unsupported operation blocked",()=>Reject(()=>gate.Require("Delete",info,true,true)));
    Check("request roundtrip cannot inject commands",()=>{
     string dangerous="D:\\交付 包\\a' ; Stop-Service x #\n$() &.json";
     string json=ReleaseCommand.CreateRequest("Test","SPC","Plan",dangerous,false,false);
     var parsed=(Dictionary<string,object>)new JavaScriptSerializer().DeserializeObject(json);
     Assert((string)parsed["ManifestPath"]==dangerous);
     string encoded=ReleaseCommand.EncodeCommand("C:\\safe' tools\\Invoke-DesktopRelease.ps1",json);
     string command=Encoding.Unicode.GetString(Convert.FromBase64String(encoded));
     Assert(!command.Contains(dangerous));Assert(command.Contains("safe'' tools"));
    });
    Check("request rejects arbitrary switches",()=>Reject(()=>ReleaseCommand.CreateRequest("Test;evil","SPC","Plan",path,false,false)));
    Check("Windows PowerShell module path is isolated",()=>{
     var start=ReleaseCommand.StartInfo(root,ReleaseCommand.CreateRequest("Test","SPC","Plan",path,false,false));
     Assert(start.EnvironmentVariables["PSModulePath"].Contains(@"WindowsPowerShell\v1.0\Modules"));Assert(!start.EnvironmentVariables["PSModulePath"].Contains(@"PowerShell\7"));
    });
    Check("child failure never becomes success",()=>{
     File.WriteAllText(Path.Combine(root,"Invoke-DesktopRelease.ps1"),"exit 9",new UTF8Encoding(true));
     using(var process=new System.Diagnostics.Process {StartInfo=ReleaseCommand.StartInfo(root,ReleaseCommand.CreateRequest("Test","SPC","Plan",path,false,false))}){
      process.Start();var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();process.WaitForExit();output.Wait();error.Wait();Assert(process.ExitCode!=0);
     }
    });
    Check("serialized progress is suppressed",()=>{
     Assert(ReleaseLog.Lines("#< CLIXML").Length==0);Assert(ReleaseLog.Lines("<Objs xmlns='http://schemas.microsoft.com/powershell/2004/04'><Obj S='progress'><MS>loading</MS></Obj></Objs>").Length==0);
    });
    Check("serialized errors are preserved",()=>{
     var lines=ReleaseLog.Lines("<Objs xmlns='http://schemas.microsoft.com/powershell/2004/04'><S S='Error'>failed_x000A_details</S></Objs>");Assert(lines.Length==1 && lines[0]=="failed\ndetails");
    });
    Check("malformed logs remain visible",()=>{string text="<Objs error";Assert(ReleaseLog.Lines(text)[0]==text);});
    Check("bundle requires real source identity",()=>{
     var draft=CreateDraft(root);draft.SourceCommit="";
     Reject(()=>draft.Validate());draft.SourceCommit=new string('a',40);draft.Validate();
    });
    Check("bundle SQL and migration IDs must be paired",()=>{
     var draft=CreateDraft(root);draft.MigrationSql="x.sql";Reject(()=>draft.Validate());draft.MigrationSql="";draft.MigrationIds="20261008183026_AddSpcPointRemarks";Reject(()=>draft.Validate());
    });
    Check("bundle output cannot overwrite an existing delivery",()=>{
     var draft=CreateDraft(root);Directory.CreateDirectory(draft.OutputDirectory);Reject(()=>draft.Validate());
    });
    Check("Portal requires current Web version",()=>{var draft=CreateDraft(root);draft.ReleaseId="BUNDLE-002";draft.SystemName="Portal";draft.PreviousWebVersion="";Reject(()=>draft.Validate());});
    Check("embedded tools hashes match",()=>{EmbeddedTools.VerifyAll();});
    Console.WriteLine("TOTAL "+count+" passed");return 0;
   } catch(Exception ex){Console.Error.WriteLine(ex.ToString());return 1;}
   finally { if(root.StartsWith(Path.GetTempPath(),StringComparison.OrdinalIgnoreCase) && Path.GetFileName(root).StartsWith("updater-tests-")) Directory.Delete(root,true); }
  }
 }
}
