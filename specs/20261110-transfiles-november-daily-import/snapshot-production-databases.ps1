param([Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$ReportDate)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
function Query-Json([string]$ConnectionString,[string]$ExpectedDatabase,[string]$Sql){
    $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new(($ConnectionString -replace 'Trust Server Certificate','TrustServerCertificate'))
    if($builder.DataSource -notmatch '^172\.16\.110\.16' -or $builder.InitialCatalog -ne $ExpectedDatabase){throw "Unexpected production target: $($builder.DataSource)/$($builder.InitialCatalog)"}
    $connection=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString);$connection.Open()
    try{$guard=$connection.CreateCommand();$guard.CommandText="SELECT DB_NAME(),CONVERT(nvarchar(20),SERVERPROPERTY('ProductVersion'))";$reader=$guard.ExecuteReader();$reader.Read()|Out-Null;if($reader.GetString(0)-ne $ExpectedDatabase){throw 'Actual production database guard failed'};$reader.Dispose()
        $command=$connection.CreateCommand();$command.CommandTimeout=180;$command.CommandText=$Sql;$r=$command.ExecuteReader();$text=[Text.StringBuilder]::new();try{while($r.Read()){if(-not $r.IsDBNull(0)){[void]$text.Append($r.GetString(0))}}}finally{$r.Dispose()};if($text.Length-eq 0){'[]'}else{$text.ToString()}
    }finally{$connection.Dispose()}
}
$spc=Get-Content 'D:\SPC\release\production\backend\appsettings.json' -Raw|ConvertFrom-Json
if($spc.AppEnvironment -ne 'production'){throw 'SPC production environment guard failed'}
$start='2026-11-01';$end='2026-11-11'
if($ReportDate){$parsed=[datetime]::MinValue;if(-not [datetime]::TryParseExact($ReportDate,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::None,[ref]$parsed)){throw 'ReportDate must be yyyy-MM-dd'};$start=$parsed.ToString('yyyy-MM-dd');$end=$parsed.AddDays(1).ToString('yyyy-MM-dd')}
$spcJson=Query-Json $spc.ConnectionStrings.SqlServer 'PMR_SPC_2026' @"
SELECT
 (SELECT p.Id ppcId,m.MachineCode line,t.TankName slot,c.CharacteristicName analysisItem
  FROM PartProcessCharacteristics p JOIN Machines m ON m.Id=p.MachineId JOIN Tanks t ON t.Id=p.TankId JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
  WHERE p.IsDeleted=0 AND p.IsEnabled=1 AND p.ControlScope='CHEM' FOR JSON PATH) masters,
 (SELECT v.Id id,v.PartProcessCharacteristicId ppcId,CONVERT(varchar(10),COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date)),23) reportDate,
    COALESCE(NULLIF(v.SamplingPhase,''),'MORNING') shift,COALESCE(NULLIF(v.SamplingStage,''),'GENERAL') samplingStage,
    v.MeasuredValue value,v.RecheckValue recheckValue,v.SourceReference sourceReference
  FROM VariableMeasurements v WHERE v.IsDeleted=0 AND COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date))>='$start' AND COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date))<'$end'
  FOR JSON PATH) measurements
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'spc-production-snapshot.json'),$spcJson,[Text.UTF8Encoding]::new($false))
$portal=Get-Content 'D:\PmrPortal\publish\portal-api\appsettings.json' -Raw|ConvertFrom-Json
$portalJson=Query-Json $portal.ConnectionStrings.Production 'PMR_PORTAL_UAT' @"
SELECT r.Id id,CONVERT(varchar(10),r.ReportDate,23) reportDate,r.LineCode lineCode,r.LineSpeed lineSpeed,r.OperatorName operatorName,
 JSON_QUERY((SELECT p.Side side,p.RepeatNo repeatNo,p.OpNo opNo,p.BeforeValue beforeValue,p.AfterValue afterValue FROM etch_amount_points p WHERE p.ReportId=r.Id ORDER BY p.Side,p.RepeatNo,p.OpNo FOR JSON PATH)) points
FROM etch_amount_reports r WHERE r.ReportDate>='$start' AND r.ReportDate<'$end' FOR JSON PATH
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'portal-production-snapshot.json'),$portalJson,[Text.UTF8Encoding]::new($false))
Write-Output 'Read-only production snapshots completed: PMR_SPC_2026 and PMR_PORTAL_UAT.'
