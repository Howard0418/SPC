using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace Pmr.ReleaseUpdater {
 internal sealed class BundleForm:Form {
  readonly string env,sys;readonly Func<string> getTools;readonly ProfileCatalog catalog;
  readonly TextBox api=new TextBox(),web=new TextBox(),apiVersion=new TextBox(),webVersion=new TextBox(),oldApi=new TextBox(),oldWeb=new TextBox(),releaseId=new TextBox(),output=new TextBox(),commit=new TextBox(),sql=new TextBox(),ids=new TextBox(),log=new TextBox();
  readonly Button inspect=new Button(),readTarget=new Button(),create=new Button(),cancel=new Button();readonly Label status=new Label();bool busy;string apiHash="";
  internal string CreatedManifest {get;private set;}
  internal BundleForm(string environmentName,string systemName,ProfileCatalog profiles,Func<string> ensureTools) {
   env=environmentName;sys=systemName;catalog=profiles;getTools=ensureTools;
   Text="製作交付包 — "+(env=="Test"?"測試":"正式")+" / "+sys;Font=new Font("Microsoft JhengHei UI",10F);ClientSize=new Size(990,790);MinimumSize=new Size(900,760);StartPosition=FormStartPosition.CenterParent;BackColor=Color.White;
   var root=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=5};root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,270));root.RowStyles.Add(new RowStyle(SizeType.Absolute,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(root);
   var p=catalog.Get(env,sys);root.Controls.Add(new Label {Text="從已建置的API／Web資料夾製作完整交付包。\r\n目標："+p.HostName+"    "+p.Database,AutoSize=true,ForeColor=Color.FromArgb(21,51,71)},0,0);
   var fields=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,RowCount=7};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));for(int i=0;i<7;i++)fields.RowStyles.Add(new RowStyle(SizeType.Absolute,37));root.Controls.Add(fields,0,1);
   AddFolder(fields,0,"API發布資料夾",api,async()=>await Inspect());AddFolder(fields,1,"Web發布資料夾",web,()=>{ReadWebVersion();return Task.FromResult(0);});
   var versions=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};apiVersion.ReadOnly=true;apiVersion.Width=120;webVersion.Width=120;versions.Controls.Add(apiVersion);versions.Controls.Add(new Label {Text="Web新版本",AutoSize=true,Margin=new Padding(16,6,4,0)});versions.Controls.Add(webVersion);fields.Controls.Add(new Label {Text="API新版本",AutoSize=true,Margin=new Padding(0,6,0,0)},0,2);fields.Controls.Add(versions,1,2);inspect.Text="讀取來源資料";inspect.Dock=DockStyle.Fill;fields.Controls.Add(inspect,2,2);
   var previous=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};oldApi.Width=120;oldWeb.Width=120;previous.Controls.Add(oldApi);previous.Controls.Add(new Label {Text=sys=="Portal"?"現行Web版本":"SPC前端以備份／雜湊回復",AutoSize=true,Margin=new Padding(16,6,4,0)});if(sys=="Portal")previous.Controls.Add(oldWeb);fields.Controls.Add(new Label {Text="現行API版本",AutoSize=true,Margin=new Padding(0,6,0,0)},0,3);fields.Controls.Add(previous,1,3);readTarget.Text="從目標讀取現行版本";readTarget.Dock=DockStyle.Fill;fields.Controls.Add(readTarget,2,3);
   releaseId.Text=sys+"-"+env+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");AddText(fields,4,"交付編號",releaseId);fields.SetColumnSpan(releaseId,2);
   AddFolder(fields,5,"輸出父資料夾",output,null);
   AddText(fields,6,"來源commit",commit);fields.SetColumnSpan(commit,2);
   var database=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,RowCount=3};database.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));database.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));database.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));database.RowStyles.Add(new RowStyle(SizeType.Absolute,24));database.RowStyles.Add(new RowStyle(SizeType.Absolute,34));database.RowStyles.Add(new RowStyle(SizeType.Absolute,34));database.Controls.Add(new Label {Text="資料庫更新：沒有變更時留空；有變更時需已審查SQL及對應ID。",AutoSize=true},0,0);database.SetColumnSpan(database.GetControlFromPosition(0,0),3);AddText(database,1,"已審查SQL",sql);var sqlBrowse=new Button {Text="選擇SQL…",Dock=DockStyle.Fill};sqlBrowse.Click+=(s,e)=>{using(var d=new OpenFileDialog {Filter="SQL (*.sql)|*.sql",CheckFileExists=true})if(d.ShowDialog(this)==DialogResult.OK)sql.Text=d.FileName;};database.Controls.Add(sqlBrowse,2,1);AddText(database,2,"待遷移ID",ids);database.SetColumnSpan(ids,2);root.Controls.Add(database,0,2);
   var actions=new FlowLayoutPanel {Dock=DockStyle.Fill};create.Text="製作交付包";create.Size=new Size(160,35);create.BackColor=Color.FromArgb(15,110,126);create.ForeColor=Color.White;create.FlatStyle=FlatStyle.Flat;cancel.Text="取消";cancel.Size=new Size(90,35);status.AutoSize=false;status.Size=new Size(620,40);status.Margin=new Padding(12,5,0,0);actions.Controls.Add(create);actions.Controls.Add(cancel);actions.Controls.Add(status);root.Controls.Add(actions,0,3);
   log.Dock=DockStyle.Fill;log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.Font=new Font("Consolas",9F);root.Controls.Add(log,0,4);
   api.TextChanged+=(s,e)=>{apiVersion.Text="";commit.Text="";apiHash="";};web.TextChanged+=(s,e)=>{webVersion.Text="";ReadWebVersion();};
   inspect.Click+=async(s,e)=>await Inspect();readTarget.Click+=async(s,e)=>await ReadTarget();create.Click+=async(s,e)=>{if(await Make()){DialogResult=DialogResult.OK;Close();}};cancel.Click+=(s,e)=>Close();FormClosing+=(s,e)=>{if(busy){e.Cancel=true;status.Text="正在製作／讀取，請等候完成。";}};
   status.Text="先選擇API與Web已建置資料夾；版本與遷移清單會自動讀取。";
  }
  void AddText(TableLayoutPanel t,int row,string label,TextBox box){t.Controls.Add(new Label {Text=label,AutoSize=true,Margin=new Padding(0,6,0,0)},0,row);box.Dock=DockStyle.Fill;box.Margin=new Padding(0,3,10,3);t.Controls.Add(box,1,row);}
  void AddFolder(TableLayoutPanel t,int row,string label,TextBox box,Func<Task> after){AddText(t,row,label,box);var b=new Button {Text="選擇資料夾…",Dock=DockStyle.Fill};b.Click+=async(s,e)=>{using(var d=new FolderBrowserDialog {Description=label,ShowNewFolderButton=true})if(d.ShowDialog(this)==DialogResult.OK){box.Text=d.SelectedPath;if(after!=null)await after();}};t.Controls.Add(b,2,row);}
  void SetBusy(bool value){busy=value;foreach(Control c in Controls)EnableInputs(c,!value);log.Enabled=true;status.Enabled=true;}
  void EnableInputs(Control root,bool enabled){foreach(Control c in root.Controls){if(c is TextBox || c is Button)c.Enabled=enabled;EnableInputs(c,enabled);}}
  void Append(string text){if(text==null || IsDisposed)return;if(InvokeRequired){BeginInvoke(new Action<string>(Append),text);return;}foreach(string line in ReleaseLog.Lines(text))log.AppendText(line+Environment.NewLine);}
  Dictionary<string,object> Operation(string op){return new Dictionary<string,object>{{"Operation",op},{"Environment",env},{"System",sys},{"ApiDirectory",api.Text}};}
  async Task<Dictionary<string,object>> Run(Func<string,string> request){
   return await Task.Run(()=>{string tools=getTools();string report=Path.Combine(tools,"bundle-result-"+Guid.NewGuid().ToString("N")+".json");using(var process=new Process {StartInfo=ReleaseCommand.StartInfo(tools,request(report),"Invoke-DesktopBundle.ps1")}){
    process.OutputDataReceived+=(s,e)=>Append(e.Data);process.ErrorDataReceived+=(s,e)=>Append(e.Data);process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("操作失敗，請查看下方紀錄；未執行站台更新。");
   }return Json.Object(new JavaScriptSerializer {MaxJsonLength=8*1024*1024}.DeserializeObject(File.ReadAllText(report,Encoding.UTF8).TrimStart('\uFEFF')));});
  }
  async Task<bool> Inspect(){
   if(busy)return false;SetBusy(true);try{var request=Operation("InspectSource");var result=await Run(report=>{request["ReportPath"]=report;return new JavaScriptSerializer().Serialize(request);});apiVersion.Text=Json.Text(result,"ApiVersion");if(String.IsNullOrWhiteSpace(commit.Text) && result.ContainsKey("SourceCommit"))commit.Text=(string)result["SourceCommit"];apiHash=Json.Text(result,"ApiSha256");status.Text="API資料已讀取，遷移清單將自動納入；不需要新增JSON。";return true;}catch(Exception ex){status.Text=ex.Message;return false;}finally{SetBusy(false);}}
  void ReadWebVersion(){
   try{if(sys=="Portal"){string dll=Path.Combine(web.Text,"PmrPortal.Web.dll");if(File.Exists(dll)){var v=new Version(FileVersionInfo.GetVersionInfo(dll).FileVersion);webVersion.Text=v.Revision==0?v.ToString(3):v.ToString();}}else{
    string package=Path.Combine(web.Text,"package.json");if(!File.Exists(package) && new DirectoryInfo(web.Text).Name=="dist")package=Path.Combine(Directory.GetParent(web.Text).FullName,"package.json");if(File.Exists(package)){var row=Json.Object(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(package)));webVersion.Text=Json.Text(row,"version");}
   }}catch{ /* Missing optional metadata leaves the version field available for explicit entry. */ }
  }
  async Task ReadTarget(){if(busy)return;SetBusy(true);try{var request=Operation("ReadTargetVersions");var r=await Run(report=>{request["ReportPath"]=report;return new JavaScriptSerializer().Serialize(request);});oldApi.Text=Json.Text(r,"ApiVersion");if(sys=="Portal")oldWeb.Text=Json.Text(r,"WebVersion");status.Text="現行版本已讀取（僅讀取，未更新目標）。";}catch(Exception ex){status.Text=ex.Message+" 可手動填入已核對的現行版本。";}finally{SetBusy(false);}}
  internal async Task<bool> Make(){
   if(busy)return false;if(!Directory.Exists(api.Text) || !Directory.Exists(web.Text) || !Directory.Exists(output.Text)){status.Text="請先選擇API、Web已建置資料夾與輸出父資料夾。";return false;}if(!await Inspect())return false;
   var draft=new BundleDraft {EnvironmentName=env,SystemName=sys,ApiDirectory=api.Text,WebDirectory=web.Text,OutputParent=output.Text,ReleaseId=releaseId.Text,ApiVersion=apiVersion.Text,WebVersion=webVersion.Text,PreviousApiVersion=oldApi.Text,PreviousWebVersion=oldWeb.Text,SourceCommit=commit.Text,MigrationSql=sql.Text,MigrationIds=ids.Text,ApiSha256=apiHash};
   try{draft.Validate();}catch(Exception ex){status.Text=ex.Message;return false;}
   SetBusy(true);try{var result=await Run(report=>draft.Request(report));string path=Json.Text(result,"ManifestPath");ManifestInfo.Load(path,env,sys,catalog);CreatedManifest=path;status.Text="交付包完成，返回主畫面後請先預檢。";return true;}catch(Exception ex){status.Text=ex.Message;return false;}finally{SetBusy(false);}
  }
  internal async Task<int> VerifyCreate(string configPath,string report){
   var c=Json.Object(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(configPath,Encoding.UTF8).TrimStart('\uFEFF')));
   api.Text=Json.Text(c,"ApiDirectory");web.Text=Json.Text(c,"WebDirectory");webVersion.Text=Json.Text(c,"WebVersion");if(c.ContainsKey("PreviousApiVersion"))oldApi.Text=(string)c["PreviousApiVersion"];if(c.ContainsKey("PreviousWebVersion"))oldWeb.Text=(string)c["PreviousWebVersion"];releaseId.Text=Json.Text(c,"ReleaseId");output.Text=Json.Text(c,"OutputParent");
   if(c.ContainsKey("ReadTarget") && (bool)c["ReadTarget"])await ReadTarget();
   bool ok=await Make();File.WriteAllText(report,log.Text+Environment.NewLine+status.Text+Environment.NewLine+(CreatedManifest??""),new UTF8Encoding(true));return ok?0:1;
  }
 }
}
