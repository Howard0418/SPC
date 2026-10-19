using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
namespace Pmr.ReleaseUpdater {
 internal sealed class BundleDraft {
  internal string EnvironmentName,SystemName,ApiDirectory,WebDirectory,OutputParent,ReleaseId,ApiVersion,WebVersion,PreviousApiVersion,PreviousWebVersion,SourceCommit,MigrationSql,MigrationIds,ApiSha256;
  internal string OutputDirectory {get{return Path.Combine(OutputParent,ReleaseId);}}
  internal void Validate() {
   if((EnvironmentName!="Test" && EnvironmentName!="Production") || (SystemName!="SPC" && SystemName!="Portal"))throw new InvalidDataException("環境或系統不正確。");
   if(!Directory.Exists(ApiDirectory) || !Directory.Exists(WebDirectory) || !Directory.Exists(OutputParent))throw new InvalidDataException("請選擇存在的API、Web及輸出資料夾。");
   if(String.Equals(Path.GetFullPath(ApiDirectory).TrimEnd('\\'),Path.GetFullPath(WebDirectory).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("API與Web必須選擇不同的已建置資料夾。");
   if(String.IsNullOrEmpty(ReleaseId) || !Regex.IsMatch(ReleaseId,@"^[A-Za-z0-9][A-Za-z0-9_-]{2,79}$"))throw new InvalidDataException("交付編號須為3至80位英數、底線或連字號。");
   if(String.IsNullOrEmpty(SourceCommit) || !Regex.IsMatch(SourceCommit,@"^[a-fA-F0-9]{40}$"))throw new InvalidDataException("來源缺少40位commit，請填入開發端提供的來源版本。");
   foreach(string value in new[]{ApiVersion,WebVersion,PreviousApiVersion})if(String.IsNullOrEmpty(value) || !Regex.IsMatch(value,@"^\d+\.\d+\.\d+(\.\d+)?$"))throw new InvalidDataException("請填寫正確的新版本與現行API版本。");
   if(SystemName=="Portal" && (String.IsNullOrEmpty(PreviousWebVersion) || !Regex.IsMatch(PreviousWebVersion,@"^\d+\.\d+\.\d+(\.\d+)?$")))throw new InvalidDataException("Portal需要現行Web版本供回復檢查。");
   if(String.IsNullOrWhiteSpace(MigrationSql)!=String.IsNullOrWhiteSpace(MigrationIds))throw new InvalidDataException("已審查SQL與待遷移ID須同時填寫，無資料庫變更時均留空。");
   if(Directory.Exists(OutputDirectory) || File.Exists(OutputDirectory))throw new InvalidDataException("交付編號的輸出資料夾已存在，請使用新的編號。");
  }
  internal string Request(string report) {
   Validate();return new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"Operation","CreateBundle"},{"Environment",EnvironmentName},{"System",SystemName},{"ApiDirectory",ApiDirectory},{"WebDirectory",WebDirectory},{"OutputParent",OutputParent},{"ReleaseId",ReleaseId},{"ApiVersion",ApiVersion},{"WebVersion",WebVersion},{"PreviousApiVersion",PreviousApiVersion},{"PreviousWebVersion",SystemName=="SPC"?"0.0.0":PreviousWebVersion},{"SourceCommit",SourceCommit},{"MigrationSql",MigrationSql??""},{"MigrationIds",MigrationIds??""},{"ApiSha256",ApiSha256??""},{"ReportPath",report}});
  }
 }
}
