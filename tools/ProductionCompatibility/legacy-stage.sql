-- Run only after AddChemicalSamplingStage and after a verified backup.
-- All mappings are explicitly approved: legacy OPEN/CLOSE => morning opening/closing;
-- N1/N2 MIDDLE => middle shift closing. Already classified rows are untouched.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE v SET SamplingStage=CASE WHEN v.SamplingPhase='OPEN' THEN 'OPEN' ELSE 'CLOSE' END,
             SamplingPhase=CASE WHEN v.SamplingPhase='MIDDLE' THEN 'MIDDLE' ELSE 'OPEN' END
FROM dbo.VariableMeasurements v
JOIN dbo.PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN dbo.Machines m ON m.Id=p.MachineId
WHERE p.ControlScope='CHEM' AND m.MachineCode IN ('N1','N2')
  AND v.SamplingStage='GENERAL' AND v.SamplingPhase IN ('OPEN','CLOSE','MIDDLE');
SELECT @@ROWCOUNT AS ChangedRows;
COMMIT;
