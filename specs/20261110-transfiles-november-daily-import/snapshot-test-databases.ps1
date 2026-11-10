param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Query-Json([string]$ConnectionString,[string]$ExpectedDatabase,[string]$Sql){
    $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new(($ConnectionString -replace 'Trust Server Certificate','TrustServerCertificate'))
    if($builder.DataSource -notmatch '^172\.16\.110\.16' -or $builder.InitialCatalog -ne $ExpectedDatabase){throw "Unexpected target: $($builder.DataSource)/$($builder.InitialCatalog)"}
    $connection=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    $connection.Open()
    try{
        $guard=$connection.CreateCommand();$guard.CommandText='SELECT DB_NAME()';if([string]$guard.ExecuteScalar() -ne $ExpectedDatabase){throw 'Actual database guard failed'}
        $command=$connection.CreateCommand();$command.CommandTimeout=120;$command.CommandText=$Sql
        $reader=$command.ExecuteReader();$text=[Text.StringBuilder]::new()
        try{while($reader.Read()){if(-not $reader.IsDBNull(0)){[void]$text.Append($reader.GetString(0))}}}finally{$reader.Dispose()}
        if($text.Length -eq 0){'[]'}else{$text.ToString()}
    }finally{$connection.Dispose()}
}

$spc=Get-Content 'D:\SPC\release\test\backend\appsettings.json' -Raw|ConvertFrom-Json
if($spc.AppEnvironment -ne 'test'){throw 'SPC environment guard failed'}
$spcJson=Query-Json $spc.ConnectionStrings.SqlServer 'PMR_SPC_TEST' @"
SELECT
 (SELECT p.Id ppcId,m.MachineCode line,t.TankName slot,c.CharacteristicName analysisItem
  FROM PartProcessCharacteristics p JOIN Machines m ON m.Id=p.MachineId JOIN Tanks t ON t.Id=p.TankId JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
  WHERE p.IsDeleted=0 AND p.IsEnabled=1 AND p.ControlScope='CHEM' FOR JSON PATH) masters,
 (SELECT v.Id id,v.PartProcessCharacteristicId ppcId,CONVERT(varchar(10),COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date)),23) reportDate,
    COALESCE(NULLIF(v.SamplingPhase,''),'MORNING') shift,COALESCE(NULLIF(v.SamplingStage,''),'GENERAL') samplingStage,
    v.MeasuredValue value,v.RecheckValue recheckValue,v.SourceReference sourceReference
  FROM VariableMeasurements v WHERE v.IsDeleted=0 AND COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date))>='2026-11-01' AND COALESCE(v.PortalDailyDate,CAST(v.MeasuredAt AS date))<'2026-11-11'
  FOR JSON PATH) measurements
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'spc-test-snapshot.json'),$spcJson,[Text.UTF8Encoding]::new($false))

$portal=Get-Content 'D:\PmrPortal\src\PmrPortal.Api\appsettings.json' -Raw|ConvertFrom-Json
$portalJson=Query-Json $portal.ConnectionStrings.Test 'PMR_PORTAL_TEST' @"
SELECT r.Id id,CONVERT(varchar(10),r.ReportDate,23) reportDate,r.LineCode lineCode,r.LineSpeed lineSpeed,r.OperatorName operatorName,
 JSON_QUERY((SELECT p.Side side,p.RepeatNo repeatNo,p.OpNo opNo,p.BeforeValue beforeValue,p.AfterValue afterValue FROM etch_amount_points p WHERE p.ReportId=r.Id ORDER BY p.Side,p.RepeatNo,p.OpNo FOR JSON PATH)) points
FROM etch_amount_reports r WHERE r.ReportDate>='2026-11-01' AND r.ReportDate<'2026-11-11' FOR JSON PATH
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'portal-test-snapshot.json'),$portalJson,[Text.UTF8Encoding]::new($false))
Write-Output 'Read-only snapshots completed: PMR_SPC_TEST and PMR_PORTAL_TEST.'
