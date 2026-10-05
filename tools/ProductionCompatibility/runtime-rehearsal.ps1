$ErrorActionPreference='Stop'
$root='D:\SPC\release-staging\production-compatibility-20260918'
$candidate=Join-Path $root 'candidate';$out=Join-Path $root 'rehearsal'
$key=[guid]::NewGuid().ToString('N')+[guid]::NewGuid().ToString('N')
$configs=@{
 'spc-api'=@{ConnectionStrings=@{SqlServer='Server=localhost;Database=PMR_COMPAT_SPC_20260918;Integrated Security=True;Encrypt=False'};Auth=@{Enabled=$true;JwtKey=$key};AppEnvironment='production';SeedDatabase=$false;Calibration=@{DeliveryEnabled=$false};Logging=@{LogLevel=@{Default='Warning'}}}
 'portal-api'=@{ConnectionStrings=@{Production='Server=localhost;Database=PMR_COMPAT_PORTAL_20260918;Integrated Security=True;Encrypt=False'};EnvironmentSwitch=@{Target='Production'};SPC_API_BASE='http://127.0.0.1:18081/';SpcSso=@{WebBaseUrl='http://127.0.0.1:18083';SharedKey=$key};Logging=@{LogLevel=@{Default='Warning'}}}
 'portal-web'=@{PortalApi=@{BaseUrl='http://127.0.0.1:18091/'};Logging=@{LogLevel=@{Default='Warning'}}}
}
$apps=@(@{Name='spc-api';Dll='MesSpc.Api.dll';Port=18081;Url='/api/version'},@{Name='portal-api';Dll='PmrPortal.Api.dll';Port=18091;Url='/health'},@{Name='portal-web';Dll='PmrPortal.Web.dll';Port=18080;Url='/Login'})
$processes=@();$results=@()
try{
 foreach($app in $apps){
  $dir=Join-Path $candidate $app.Name
  # Candidate-only files; discard source connection strings and notification destinations.
  Get-ChildItem -LiteralPath $dir -Filter 'appsettings*.json' -File|ForEach-Object{[IO.File]::WriteAllText($_.FullName,'{}')}
  $configs[$app.Name]|ConvertTo-Json -Depth 8|Set-Content "$dir\appsettings.json" -Encoding utf8
  if(Get-NetTCPConnection -LocalPort $app.Port -State Listen -ErrorAction SilentlyContinue){throw 'Rehearsal port is already in use'}
  $p=Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @($app.Dll,'--urls',"http://127.0.0.1:$($app.Port)",'--environment','Production') -WorkingDirectory $dir -WindowStyle Hidden -PassThru -RedirectStandardOutput "$out\$($app.Name).stdout.log" -RedirectStandardError "$out\$($app.Name).stderr.log"
  $processes+=$p
  $response=$null
  for($i=0;$i -lt 30;$i++){
   Start-Sleep -Milliseconds 500;$p.Refresh();if($p.HasExited){throw "$($app.Name) exited; inspect rehearsal log"}
   try{$response=Invoke-WebRequest "http://127.0.0.1:$($app.Port)$($app.Url)" -UseBasicParsing -TimeoutSec 3;if($response.StatusCode -eq 200){break}}catch{}
  }
  if(!$response -or $response.StatusCode -ne 200){throw "$($app.Name) health failed"}
  $results+=@{App=$app.Name;Http=[int]$response.StatusCode;ProductionEnvironment=$true;LoopbackOnly=$true}
 }
 function B64($bytes){[Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')}
 $header=B64 ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
 $claims=@{sub='compatibility';role='Admin';exp=[DateTimeOffset]::UtcNow.AddMinutes(3).ToUnixTimeSeconds()}|ConvertTo-Json -Compress
 $body=B64 ([Text.Encoding]::UTF8.GetBytes($claims));$unsigned="$header.$body"
 $hmac=[Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($key));$token=$unsigned+'.'+(B64 ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))))
 $cn=[System.Data.SqlClient.SqlConnection]::new($configs['spc-api'].ConnectionStrings.SqlServer);$cn.Open()
 try{
  $cmd=$cn.CreateCommand();$cmd.CommandText="SELECT m.Id,m.MachineCode,CONVERT(varchar(10),COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date)),23) AS ReportDate,COUNT(*) AS N FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId JOIN Machines m ON m.Id=p.MachineId WHERE v.SamplingPhase='MIDDLE' AND v.SamplingStage='CLOSE' GROUP BY m.Id,m.MachineCode,COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date))"
  $t=[System.Data.DataTable]::new();$t.Load($cmd.ExecuteReader())
  foreach($row in $t.Rows){
   $url="http://127.0.0.1:18081/api/v1/manual-measurements/daily?machineId=$($row.Id)&date=$($row.ReportDate)&samplingPhase=MIDDLE&samplingStage=CLOSE"
   $response=Invoke-WebRequest $url -Headers @{Authorization="Bearer $token"} -UseBasicParsing
   $data=$response.Content|ConvertFrom-Json
   $rows=if($data.data){$data.data.rows}else{$data.rows}
   # Preserve response evidence without authentication tokens.
   $response.Content|Set-Content "$out\daily-$($row.MachineCode)-$($row.ReportDate).json" -Encoding utf8
   if($response.Content -notmatch '"exists"\s*:\s*true'){throw 'Converted middle/close daily report not found'}
  }
  $results+=@{App='spc-daily';Reports=$t.Rows.Count;ConvertedMiddleCloseReadable=$true}
 }finally{$cn.Dispose()}
 $results|ConvertTo-Json -Depth 5|Set-Content "$out\runtime-result.json" -Encoding utf8
 'Loopback Production-mode startup passed for SPC API, Portal API and Portal Web; middle/close daily reports found.'
}finally{
 foreach($p in $processes){$p.Refresh();if(!$p.HasExited){Stop-Process -Id $p.Id -Force}}
 # Ensure these rehearsal configs never masquerade as production deployment settings.
 foreach($app in $apps){[IO.File]::WriteAllText((Join-Path $candidate "$($app.Name)\appsettings.json"),'{}')}
}
