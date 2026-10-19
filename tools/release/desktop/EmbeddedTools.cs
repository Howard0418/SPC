using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;
namespace Pmr.ReleaseUpdater {
 internal static class EmbeddedTools {
  internal static readonly string[] Names={"Invoke-Release.ps1","ReleaseRemote.psm1","ProductionRelease.psm1","Invoke-ReleaseOnServer.ps1","Start-ReleaseWorker.ps1","Invoke-DesktopRelease.ps1","Invoke-DesktopBundle.ps1","New-ProductionReleaseBundle.ps1","MetadataInspector.exe"};
  internal static byte[] Read(string name) {
   using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Updater."+name)) {
    if(stream==null)throw new InvalidDataException("內建資源缺少："+name);
    using(var memory=new MemoryStream()){stream.CopyTo(memory);return memory.ToArray();}
   }
  }
  internal static string Text(string name) {return new UTF8Encoding(false,true).GetString(Read(name)).TrimStart('\uFEFF');}
  internal static void VerifyAll() {
   var hashes=Json.Object(new JavaScriptSerializer().DeserializeObject(Text("asset-hashes.json")));
   foreach(string name in Names)if(!String.Equals(ManifestInfo.HashBytes(Read(name)),Json.Text(hashes,name),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("內建工具雜湊驗證失敗。");
   if(!String.Equals(ManifestInfo.HashBytes(Read("profiles.json")),Json.Text(hashes,"profiles.json"),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("環境清單驗證失敗。");
  }
  static void NoReparse(string path) {
   for(string current=Path.GetFullPath(path);!String.IsNullOrEmpty(current);current=Path.GetDirectoryName(current)) {
    if((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("工具暫存目錄不可使用連結。");
    if(Path.GetDirectoryName(current)==current)break;
   }
  }
  static void Secure(string path) {
   var acl=new DirectorySecurity();acl.SetAccessRuleProtection(true,false);
   var admins=new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid,null);var system=new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null);
   acl.SetOwner(admins);
   foreach(var sid in new[]{admins,system})acl.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
   new DirectoryInfo(path).SetAccessControl(acl);
  }
  internal static void VerifyDirectory(string root) {
   NoReparse(root);foreach(string name in Names){string path=Path.Combine(root,name);NoReparse(path);if(ManifestInfo.HashBytes(File.ReadAllBytes(path))!=ManifestInfo.HashBytes(Read(name)))throw new InvalidDataException("工具檔案已變更，請重新啟動程式。");}
  }
  internal static string Extract() {
   VerifyAll();
   string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"PMR-Release-Updater");
   NoReparse(root);Directory.CreateDirectory(root);Secure(root);
   string dir=Path.Combine(root,"1.1.0-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);Secure(dir);
   foreach(string name in Names) {
    string path=Path.Combine(dir,name);using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){byte[] bytes=Read(name);output.Write(bytes,0,bytes.Length);}
    if(ManifestInfo.HashBytes(File.ReadAllBytes(path))!=ManifestInfo.HashBytes(Read(name)))throw new IOException("工具寫入驗證失敗。");
   }
   return dir;
  }
 }
}
