using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;
namespace Pmr.ReleaseUpdater {
 internal sealed class TargetProfile {
  internal string EnvironmentName, SystemName, HostName, Database, ApiPath, WebPath;
 }
 internal sealed class ProfileCatalog {
  readonly Dictionary<string,TargetProfile> profiles=new Dictionary<string,TargetProfile>();
  internal ProfileCatalog(string json) {
   foreach(object item in Json.Array(new JavaScriptSerializer().DeserializeObject(json))) {
    var row=Json.Object(item); var profile=new TargetProfile {EnvironmentName=Json.Text(row,"Environment"),SystemName=Json.Text(row,"System"),HostName=Json.Text(row,"HostName"),Database=Json.Text(row,"Database")};
    foreach(object target in Json.Array(row["Targets"])) {var t=Json.Object(target);if(Json.Text(t,"Role")=="Api")profile.ApiPath=Json.Text(t,"Path");else if(Json.Text(t,"Role")=="Web")profile.WebPath=Json.Text(t,"Path");}
    profiles.Add(profile.EnvironmentName+"/"+profile.SystemName,profile);
   }
  }
  internal TargetProfile Get(string environment,string system) {TargetProfile p;if(!profiles.TryGetValue(environment+"/"+system,out p))throw new InvalidOperationException("不支援的環境或系統。");return p;}
 }
 internal static class Json {
  internal static Dictionary<string,object> Object(object value) {var result=value as Dictionary<string,object>;if(result==null)throw new InvalidDataException("交付包格式不正確。");return result;}
  internal static object[] Array(object value) {var result=value as object[];if(result==null)throw new InvalidDataException("交付清單格式不正確。");return result;}
  internal static string Text(Dictionary<string,object> row,string key) {object value;if(!row.TryGetValue(key,out value) || !(value is string) || String.IsNullOrWhiteSpace((string)value))throw new InvalidDataException("交付包缺少欄位："+key);return (string)value;}
 }
 internal sealed class ManifestInfo {
  internal string FullPath, EnvironmentName, SystemName, HostName, Database, ReleaseId, ApiVersion, WebVersion, Hash;
  internal string Stamp { get {return FullPath+"|"+EnvironmentName+"|"+SystemName+"|"+Hash;} }
  internal static ManifestInfo Load(string path,string environment,string system,ProfileCatalog catalog) {
   TargetProfile profile=catalog.Get(environment,system);
   string full=Path.GetFullPath(path);
   var f=new FileInfo(full);if(!f.Exists || f.Length>8*1024*1024 || !String.Equals(f.Extension,".json",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("請選擇有效的 manifest.json（上限 8MB）。");
   byte[] bytes=File.ReadAllBytes(full);string json=new UTF8Encoding(false,true).GetString(bytes).TrimStart('\uFEFF');
   var serializer=new JavaScriptSerializer {MaxJsonLength=8*1024*1024};var row=Json.Object(serializer.DeserializeObject(json));
   if(!row.ContainsKey("schemaVersion") || Convert.ToInt32(row["schemaVersion"])!=1)throw new InvalidDataException("不支援的交付包版本。");
   var result=new ManifestInfo {FullPath=full,EnvironmentName=Json.Text(row,"environment"),SystemName=Json.Text(row,"system"),HostName=Json.Text(row,"host"),ReleaseId=Json.Text(row,"releaseId"),Database=Json.Text(Json.Object(row["database"]),"name"),Hash=HashBytes(bytes)};
   if(result.EnvironmentName!=environment || result.SystemName!=system)throw new InvalidDataException("交付包與目前選擇的環境／系統不符。");
   if(!String.Equals(result.HostName,profile.HostName,StringComparison.OrdinalIgnoreCase) || result.Database!=profile.Database)throw new InvalidDataException("交付包主機或資料庫不在此環境白名單。");
   if(!Regex.IsMatch(result.ReleaseId,@"^[A-Za-z0-9][A-Za-z0-9_-]{2,79}$"))throw new InvalidDataException("交付編號不正確。");
   foreach(object item in Json.Array(row["packages"])) {
    var package=Json.Object(item);string target=Json.Text(package,"path");string version=Json.Text(package,"version");
    if(!Regex.IsMatch(version,@"^\d+\.\d+\.\d+(\.\d+)?$"))throw new InvalidDataException("版本格式不正確。");
    if(SamePath(target,profile.ApiPath)) {if(result.ApiVersion!=null)throw new InvalidDataException("重複 API 交付。");result.ApiVersion=version;}
    else if(SamePath(target,profile.WebPath)) {if(result.WebVersion!=null)throw new InvalidDataException("重複 Web 交付。");result.WebVersion=version;}
    else throw new InvalidDataException("交付目錄不在此環境白名單。");
   }
   if(result.ApiVersion==null || result.WebVersion==null)throw new InvalidDataException("交付包必須同時包含 API 與 Web。");
   return result;
  }
  static bool SamePath(string a,string b) {return String.Equals(Path.GetFullPath(a).TrimEnd('\\','/'),Path.GetFullPath(b).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);}
  internal static string HashBytes(byte[] bytes) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 }
 internal sealed class ReleaseGate {
  string stamp;
  internal void RecordPlan(ManifestInfo info) {stamp=info.Stamp;}
  internal void Reset() {stamp=null;}
  internal bool HasPlan(ManifestInfo info) {return info!=null && stamp==info.Stamp;}
  internal void Require(string mode,ManifestInfo info,bool application,bool database) {
   if(info==null)throw new InvalidOperationException("請先選擇交付包。");
   if(mode!="Plan" && mode!="Apply" && mode!="Rollback")throw new InvalidOperationException("不支援的操作。");
   if(mode=="Plan")return;
   if(!application || !database)throw new InvalidOperationException("請確認本次應用與資料庫／啟動操作授權。");
   if(mode=="Apply" && !HasPlan(info))throw new InvalidOperationException("請先對目前交付包完成預檢；交付包變更後須重新預檢。");
  }
 }
 internal static class ReleaseLog {
  internal static string[] Lines(string text) {
   if(text==null || text.Trim()=="#< CLIXML")return new string[0];
   if(!text.TrimStart().StartsWith("<Objs ",StringComparison.Ordinal))return new[]{text};
   try {
    var document=new XmlDocument {XmlResolver=null};var settings=new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};
    using(var reader=XmlReader.Create(new StringReader(text),settings))document.Load(reader);
    var output=new List<string>();foreach(XmlNode node in document.DocumentElement.ChildNodes) {
     if(node.NodeType!=XmlNodeType.Element)continue;
     string stream=node.Attributes["S"]==null?"":node.Attributes["S"].Value;
     if(stream=="progress")continue;
     if(node.LocalName=="S")output.Add(Regex.Replace(node.InnerText,@"_x([0-9a-fA-F]{4})_",m=>((char)Convert.ToInt32(m.Groups[1].Value,16)).ToString()));
     else output.Add(node.OuterXml);
    }return output.ToArray();
   }catch{return new[]{text};} // Preserve unknown or malformed records, never hide a possible error.
  }
 }
 internal static class ReleaseCommand {
  internal static string CreateRequest(string environment,string system,string mode,string path,bool application,bool database) {
   if((environment!="Test" && environment!="Production") || (system!="SPC" && system!="Portal") || (mode!="Plan" && mode!="Apply" && mode!="Rollback"))throw new InvalidOperationException("操作參數不正確。");
   return new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"Environment",environment},{"System",system},{"Mode",mode},{"ManifestPath",path},{"ApplicationAuthorized",application},{"DatabaseAuthorized",database}});
  }
  internal static string EncodeCommand(string bridge,string request) {
   string data=Convert.ToBase64String(Encoding.UTF8.GetBytes(request));
   string command="$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false); & '"+bridge.Replace("'","''")+"' -RequestBase64 '"+data+"'; if(!$?){exit 1}; exit $LASTEXITCODE";
   return Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
  }
  internal static ProcessStartInfo StartInfo(string toolRoot,string request) {
   string ps=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),@"System32\WindowsPowerShell\v1.0\powershell.exe");
   var start=new ProcessStartInfo {FileName=ps,Arguments="-NoProfile -NonInteractive -OutputFormat Text -EncodedCommand "+EncodeCommand(Path.Combine(toolRoot,"Invoke-DesktopRelease.ps1"),request),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=toolRoot};
   // A desktop EXE can inherit PowerShell 7's PSModulePath. The 5.1 worker needs its own modules.
   start.EnvironmentVariables["PSModulePath"]=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),@"System32\WindowsPowerShell\v1.0\Modules")+";"+Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"WindowsPowerShell\Modules")+";"+Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),@"WindowsPowerShell\Modules");
   return start;
  }
 }
}
