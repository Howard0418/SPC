$ErrorActionPreference='Stop'
$cn=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=tempdb;Integrated Security=True;Encrypt=False');$cn.Open()
try{
 $cmd=$cn.CreateCommand();$cmd.CommandText=@'
CREATE TABLE #Machines(Id int PRIMARY KEY,MachineCode nvarchar(10));
CREATE TABLE #PartProcessCharacteristics(Id int PRIMARY KEY,MachineId int,ControlScope nvarchar(20));
CREATE TABLE #VariableMeasurements(Id int PRIMARY KEY,PartProcessCharacteristicId int,SamplingPhase nvarchar(16),SamplingStage nvarchar(16),MeasuredValue decimal(12,3));
INSERT #Machines VALUES(1,'N1'),(2,'N2'),(3,'C1');
INSERT #PartProcessCharacteristics VALUES(1,1,'CHEM'),(2,2,'CHEM'),(3,3,'CHEM');
INSERT #VariableMeasurements VALUES(1,1,'OPEN','GENERAL',10.004),(2,1,'CLOSE','GENERAL',10.004),(3,1,'MIDDLE','GENERAL',10.004),(4,2,'MIDDLE','GENERAL',10.004),(5,3,'MIDDLE','GENERAL',10.004),(6,1,'OPEN','OPEN',10.004);
'@;$cmd.ExecuteNonQuery()|Out-Null
 $sql=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'legacy-stage.sql')).Replace('dbo.VariableMeasurements','#VariableMeasurements').Replace('dbo.PartProcessCharacteristics','#PartProcessCharacteristics').Replace('dbo.Machines','#Machines')
 $cmd.CommandText=$sql;if([int]$cmd.ExecuteScalar() -ne 4){throw 'Expected exactly four changes'}
 $cmd.CommandText=@'
SELECT COUNT(*) FROM #VariableMeasurements WHERE MeasuredValue<>10.004
 OR (Id=1 AND (SamplingPhase<>'OPEN' OR SamplingStage<>'OPEN'))
 OR (Id=2 AND (SamplingPhase<>'OPEN' OR SamplingStage<>'CLOSE'))
 OR (Id IN(3,4) AND (SamplingPhase<>'MIDDLE' OR SamplingStage<>'CLOSE'))
 OR (Id=5 AND (SamplingPhase<>'MIDDLE' OR SamplingStage<>'GENERAL'))
 OR (Id=6 AND (SamplingPhase<>'OPEN' OR SamplingStage<>'OPEN'));
'@;if([int]$cmd.ExecuteScalar() -ne 0){throw 'Mapping or unrelated data mismatch'}
 $cmd.CommandText=$sql;if([int]$cmd.ExecuteScalar() -ne 0){throw 'Rerun changed already classified rows'}
 'PASS: six mapping/preservation cases and idempotence on SQL Server temporary tables.'
}finally{$cn.Dispose()}
