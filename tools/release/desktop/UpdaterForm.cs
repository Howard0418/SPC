using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly:AssemblyTitle("PMR 系統更新工具")]
[assembly:AssemblyVersion("1.1.0.0")]
[assembly:AssemblyFileVersion("1.1.0.0")]
namespace Pmr.ReleaseUpdater {
 internal sealed class UpdaterForm:Form {
  readonly ComboBox environment=new ComboBox(), system=new ComboBox();
  readonly TextBox manifest=new TextBox(), log=new TextBox();
  readonly Label target=new Label(), details=new Label(), status=new Label(), instruction=new Label();
  readonly Button makeBundle=new Button(), browse=new Button(), plan=new Button(), apply=new Button(), rollback=new Button();
  readonly CheckBox appAuthorized=new CheckBox(), dbAuthorized=new CheckBox();
  readonly ReleaseGate gate=new ReleaseGate();
  readonly ProfileCatalog catalog;
  bool busy;string toolRoot;ManifestInfo current;
  static readonly Color Navy=Color.FromArgb(21,51,71), Teal=Color.FromArgb(15,110,126);
  internal UpdaterForm() {
   EmbeddedTools.VerifyAll();catalog=new ProfileCatalog(EmbeddedTools.Text("profiles.json"));
   Text="PMR 系統更新工具 1.1.0";Font=new Font("Microsoft JhengHei UI",10F);BackColor=Color.White;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(990,770);MinimumSize=new Size(860,700);AutoScaleMode=AutoScaleMode.Dpi;
   var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(22,16,22,16)};
   foreach(float height in new[]{72F,70F,62F,98F,64F,50F})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));Controls.Add(root);
   var header=new Panel {Dock=DockStyle.Fill};header.Controls.Add(new Label {Text="PMR 系統更新工具",Font=new Font(Font.FontFamily,21F,FontStyle.Bold),ForeColor=Navy,AutoSize=true,Location=new Point(0,0)});header.Controls.Add(new Label {Text="選擇目標與交付包，預檢後再進行更新。",ForeColor=Color.DimGray,AutoSize=true,Location=new Point(2,42)});root.Controls.Add(header,0,0);
   var select=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,4,0,0)};
   select.Controls.Add(new Label {Text="環境",AutoSize=true,Margin=new Padding(0,7,8,0)});environment.DropDownStyle=ComboBoxStyle.DropDownList;environment.Items.AddRange(new object[]{"測試環境","正式環境"});environment.Width=140;environment.Margin=new Padding(0,0,24,0);select.Controls.Add(environment);
   select.Controls.Add(new Label {Text="系統",AutoSize=true,Margin=new Padding(0,7,8,0)});system.DropDownStyle=ComboBoxStyle.DropDownList;system.Items.AddRange(new object[]{"SPC","Portal"});system.Width=110;system.Margin=new Padding(0,0,20,0);select.Controls.Add(system);
   target.AutoSize=false;target.Size=new Size(480,55);target.ForeColor=Navy;target.Margin=new Padding(0,3,0,0);select.Controls.Add(target);root.Controls.Add(select,0,1);
   var package=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,RowCount=2};package.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));package.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));package.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));package.RowStyles.Add(new RowStyle(SizeType.Absolute,23));package.RowStyles.Add(new RowStyle(SizeType.Percent,100));package.Controls.Add(new Label {Text="交付包 manifest.json",AutoSize=true},0,0);manifest.Dock=DockStyle.Fill;manifest.Margin=new Padding(0,0,12,0);package.Controls.Add(manifest,0,1);browse.Text="選擇交付包…";browse.Dock=DockStyle.Fill;browse.Margin=new Padding(0,0,0,6);package.Controls.Add(browse,1,1);makeBundle.Text="製作交付包…";makeBundle.Dock=DockStyle.Fill;makeBundle.Margin=new Padding(8,0,0,6);package.Controls.Add(makeBundle,2,1);root.Controls.Add(package,0,2);
   var detailPanel=new Panel {Dock=DockStyle.Fill,BackColor=Color.FromArgb(240,246,249),Padding=new Padding(12,8,12,8),Margin=new Padding(0,4,0,7)};details.Dock=DockStyle.Fill;details.ForeColor=Navy;detailPanel.Controls.Add(details);root.Controls.Add(detailPanel,0,3);
   var permissions=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};appAuthorized.Text="已確認本次應用更新／服務重啟授權";dbAuthorized.Text="已確認本次資料庫更新／啟動授權";appAuthorized.AutoSize=true;dbAuthorized.AutoSize=true;permissions.Controls.Add(appAuthorized);permissions.Controls.Add(dbAuthorized);root.Controls.Add(permissions,0,4);
   var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};SetupButton(plan,"1  執行預檢",Teal);SetupButton(apply,"2  開始更新",Navy);SetupButton(rollback,"回復此批應用",Color.FromArgb(87,100,111));actions.Controls.Add(plan);actions.Controls.Add(apply);actions.Controls.Add(rollback);instruction.AutoSize=false;instruction.Size=new Size(400,38);instruction.Margin=new Padding(12,4,0,0);instruction.ForeColor=Color.DimGray;actions.Controls.Add(instruction);root.Controls.Add(actions,0,5);
   log.Dock=DockStyle.Fill;log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.WordWrap=false;log.Font=new Font("Consolas",10F);log.BackColor=Color.FromArgb(248,250,252);log.BorderStyle=BorderStyle.FixedSingle;root.Controls.Add(log,0,6);status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.ForeColor=Teal;root.Controls.Add(status,0,7);
   environment.SelectedIndexChanged+=(s,e)=>SelectionChanged();system.SelectedIndexChanged+=(s,e)=>SelectionChanged();manifest.TextChanged+=(s,e)=>SelectionChanged();appAuthorized.CheckedChanged+=(s,e)=>UpdateButtons();dbAuthorized.CheckedChanged+=(s,e)=>UpdateButtons();
   browse.Click+=(s,e)=>{using(var dialog=new OpenFileDialog {Title="選擇完整交付包的 manifest.json",Filter="交付設定 (*.json)|*.json",CheckFileExists=true,Multiselect=false,FileName="manifest.json"})if(dialog.ShowDialog(this)==DialogResult.OK)manifest.Text=dialog.FileName;};
   makeBundle.Click+=(s,e)=>{if(busy)return;using(var dialog=new BundleForm(SelectedEnvironment,SelectedSystem,catalog,EnsureTools)){if(dialog.ShowDialog(this)==DialogResult.OK){manifest.Text=dialog.CreatedManifest;status.Text="交付包已建立，請先執行預檢。";}}};
   plan.Click+=async(s,e)=>await Execute("Plan");apply.Click+=async(s,e)=>await Execute("Apply");rollback.Click+=async(s,e)=>await Execute("Rollback");
   FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show(this,"操作仍在執行，請等候結果後再關閉程式。","操作進行中",MessageBoxButtons.OK,MessageBoxIcon.Information);}};
   environment.SelectedIndex=0;system.SelectedIndex=0;SelectionChanged();
  }
  void SetupButton(Button button,string text,Color color) {button.Text=text;button.Size=new Size(145,37);button.FlatStyle=FlatStyle.Flat;button.BackColor=color;button.ForeColor=Color.White;button.Margin=new Padding(0,0,10,0);}
  string SelectedEnvironment {get{return environment.SelectedIndex==1?"Production":"Test";}}
  string SelectedSystem {get{return system.SelectedIndex==1?"Portal":"SPC";}}
  void SelectionChanged() {
   gate.Reset();appAuthorized.Checked=false;dbAuthorized.Checked=false;current=null;
   if(environment.SelectedIndex<0 || system.SelectedIndex<0)return;
   var profile=catalog.Get(SelectedEnvironment,SelectedSystem);target.Text=profile.HostName+Environment.NewLine+(SelectedEnvironment=="Production"?"正式環境 · 更新前請確認維護時間":"測試環境");target.ForeColor=SelectedEnvironment=="Production"?Color.FromArgb(164,72,22):Navy;
   if(String.IsNullOrWhiteSpace(manifest.Text)){details.Text="主機："+profile.HostName+Environment.NewLine+"資料庫："+profile.Database+Environment.NewLine+"請選擇此環境的完整交付包。";status.Text="尚未選擇交付包。";}
   else {try{current=ManifestInfo.Load(manifest.Text,SelectedEnvironment,SelectedSystem,catalog);ShowDetails(current);status.Text="交付包已選擇，請先執行預檢。";}catch(Exception ex){details.Text="交付包尚未通過核對："+ex.Message;status.Text="請確認環境、系統與交付包相符。";}}
   UpdateButtons();
  }
  string EnsureTools(){if(toolRoot==null)toolRoot=EmbeddedTools.Extract();EmbeddedTools.VerifyDirectory(toolRoot);return toolRoot;}
  void ShowDetails(ManifestInfo info) {details.Text="交付編號："+info.ReleaseId+"    API "+info.ApiVersion+" / Web "+info.WebVersion+Environment.NewLine+"主機："+info.HostName+Environment.NewLine+"資料庫："+info.Database;}
  void UpdateButtons() {
   bool valid=current!=null && !busy;plan.Enabled=valid;apply.Enabled=valid && gate.HasPlan(current) && appAuthorized.Checked && dbAuthorized.Checked;rollback.Enabled=valid && appAuthorized.Checked && dbAuthorized.Checked;
   environment.Enabled=!busy;system.Enabled=!busy;manifest.Enabled=!busy;browse.Enabled=!busy;makeBundle.Enabled=!busy;appAuthorized.Enabled=!busy;dbAuthorized.Enabled=!busy;
   instruction.Text=busy?"正在執行，請等候結果。":(current!=null && gate.HasPlan(current)?"預檢通過；確認授權後可更新。":"先預檢再更新；回復不還原資料庫。");
  }
  void Append(string text) {foreach(string line in ReleaseLog.Lines(text))AppendRaw(line);}
  void AppendRaw(string text) {
   if(IsDisposed || text==null)return;
   if(InvokeRequired){BeginInvoke(new Action<string>(AppendRaw),text);return;}
   if(log.TextLength>200000)log.Text=log.Text.Substring(log.TextLength-100000);
   log.AppendText(text+Environment.NewLine);log.SelectionStart=log.TextLength;log.ScrollToCaret();
  }
  internal async Task<int> VerifyPlanForTest(string path,string report) {
   var row=Json.Object(new System.Web.Script.Serialization.JavaScriptSerializer {MaxJsonLength=8*1024*1024}.DeserializeObject(File.ReadAllText(path,Encoding.UTF8).TrimStart('\uFEFF')));
   string env=Json.Text(row,"environment"), sys=Json.Text(row,"system");catalog.Get(env,sys);
   environment.SelectedIndex=env=="Production"?1:0;system.SelectedIndex=sys=="Portal"?1:0;manifest.Text=path;
   await Execute("Plan");File.WriteAllText(report,log.Text+Environment.NewLine+status.Text,new System.Text.UTF8Encoding(true));
   return gate.HasPlan(current)?0:1;
  }
  async Task Execute(string mode) {
   if(busy)return;ManifestInfo info;
   try {
    info=ManifestInfo.Load(manifest.Text,SelectedEnvironment,SelectedSystem,catalog);current=info;gate.Require(mode,info,appAuthorized.Checked,dbAuthorized.Checked);
    if(mode!="Plan") {
     string action=mode=="Apply"?"更新":"回復此批應用";
     string message="環境："+(info.EnvironmentName=="Production"?"正式":"測試")+Environment.NewLine+"系統："+info.SystemName+Environment.NewLine+"主機："+info.HostName+Environment.NewLine+"資料庫："+info.Database+Environment.NewLine+"交付："+info.ReleaseId+Environment.NewLine+"版本：API "+info.ApiVersion+" / Web "+info.WebVersion+Environment.NewLine+Environment.NewLine+(mode=="Apply"?"將先備份再更新，服務會短暫中斷。":"將依同批備份回復應用；資料庫 schema 與新增檔保留。")+Environment.NewLine+"確認開始"+action+"？";
     if(MessageBox.Show(this,message,"確認"+action,MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
    }
   }catch(Exception ex){gate.Reset();status.Text=ex.Message;UpdateButtons();return;}
   bool app=appAuthorized.Checked, db=dbAuthorized.Checked;busy=true;if(mode=="Plan")gate.Reset();UpdateButtons();status.Text="執行中："+mode;Append("── "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" "+mode+" / "+info.EnvironmentName+" / "+info.SystemName+" ──");
   try {
    string request=ReleaseCommand.CreateRequest(info.EnvironmentName,info.SystemName,mode,info.FullPath,mode!="Plan" && app,mode!="Plan" && db);
    int exitCode=await Task.Run(()=>{
     EnsureTools();
     using(var process=new Process {StartInfo=ReleaseCommand.StartInfo(toolRoot,request)}){
      process.OutputDataReceived+=(s,e)=>{if(e.Data!=null)Append(e.Data);};process.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)Append(e.Data);};
      if(!process.Start())throw new InvalidOperationException("無法啟動更新程序。");process.BeginOutputReadLine();process.BeginErrorReadLine();process.WaitForExit();return process.ExitCode;
     }
    });
    if(exitCode==0) {
     if(mode=="Plan") {var fresh=ManifestInfo.Load(info.FullPath,info.EnvironmentName,info.SystemName,catalog);if(fresh.Stamp!=info.Stamp)throw new InvalidOperationException("預檢期間交付包已變更，請重新預檢。");gate.RecordPlan(info);status.Text="預檢成功。尚未更新站台或資料庫。";}
     else {gate.Reset();status.Text=mode=="Apply"?"更新流程完成，請進行登入與業務驗收。":"應用回復完成，資料庫 schema 保留。";}
     Append("完成，結束碼 0。");
    }else {gate.Reset();status.Text="操作失敗，結束碼 "+exitCode+"；請查看上方結果及目標機紀錄。";Append(status.Text);}
   }catch(Exception ex){gate.Reset();status.Text="操作失敗："+ex.Message;Append(status.Text);}
   finally{busy=false;UpdateButtons();}
  }
 }
 internal static class Program {
  [STAThread] public static int Main(string[] args) {
   try {
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    if(args.Length==2 && args[0]=="--render-bundle") {var profiles=new ProfileCatalog(EmbeddedTools.Text("profiles.json"));using(var form=new BundleForm("Test","SPC",profiles,()=>{throw new InvalidOperationException("Render must not extract tools");})){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.GetFullPath(args[1]),System.Drawing.Imaging.ImageFormat.Png);}form.Close();}return 0;}
    if(args.Length==2 && args[0]=="--render") {using(var form=new UpdaterForm()){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.GetFullPath(args[1]),System.Drawing.Imaging.ImageFormat.Png);}form.Close();}return 0;}
    if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)){MessageBox.Show("請以系統管理員身分執行更新工具。","PMR 系統更新工具");return 1;}
    if(args.Length==3 && args[0]=="--verify-create") {
     var config=Json.Object(new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(File.ReadAllText(args[1],Encoding.UTF8).TrimStart('\uFEFF')));string env=Json.Text(config,"Environment"),sys=Json.Text(config,"System");var profiles=new ProfileCatalog(EmbeddedTools.Text("profiles.json"));int result=1;string tools=null;
     using(var form=new BundleForm(env,sys,profiles,()=>{if(tools==null)tools=EmbeddedTools.Extract();EmbeddedTools.VerifyDirectory(tools);return tools;})){
      form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Shown+=async(s,e)=>{try{result=await form.VerifyCreate(args[1],args[2]);}catch(Exception ex){File.WriteAllText(args[2],ex.ToString());}finally{form.Close();}};Application.Run(form);
     }return result;
    }
    if(args.Length==3 && args[0]=="--verify-plan") {
     int result=1;using(var form=new UpdaterForm()) {
      form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
      form.Shown+=async(s,e)=>{try{result=await form.VerifyPlanForTest(args[1],args[2]);}catch(Exception ex){File.WriteAllText(args[2],"Plan verification failed: "+ex.GetType().Name);}finally{form.Close();}};
      Application.Run(form);
     }return result;
    }
    Application.Run(new UpdaterForm());return 0;
   }catch(Exception ex){MessageBox.Show("更新工具無法啟動："+ex.Message,"PMR 系統更新工具",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
  }
 }
}
