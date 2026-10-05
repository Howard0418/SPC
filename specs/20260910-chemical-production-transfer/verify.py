"""Independent post-commit verification against fresh SQL inventory."""
import json
from pathlib import Path

p=Path(__file__).resolve().parent
def load(name): return json.loads((p/name).read_text(encoding='utf-8'))
before=load('production-measurements.json')
after=load('post-transfer/production-measurements.json')
by_id={v['Id']:v for v in after}
assert len(before)==813 and len(after)==2082
assert all(by_id[v['Id']]==v for v in before), 'Existing production measurement changed'
for suffix in ('measurements','mappings','batches','details','migrations'):
    assert load(f'test-{suffix}.json')==load(f'post-transfer/test-{suffix}.json'), f'Test {suffix} changed'
for suffix in ('mappings','migrations','schema'):
    assert load(f'production-{suffix}.json')==load(f'post-transfer/production-{suffix}.json'), f'Production {suffix} changed'
old_batches={b['UploadBatchId']:b for b in load('production-batches.json')}
new_batches={b['UploadBatchId']:b for b in load('post-transfer/production-batches.json')}
assert all(new_batches[k]==v for k,v in old_batches.items())
old_details={d['Id']:d for d in load('production-details.json')}
new_details={d['Id']:d for d in load('post-transfer/production-details.json')}
assert all(new_details[k]==v for k,v in old_details.items())
batch='5F6964D5-F2CA-4B9C-9506-40613273830E'
ids={i['SourceId']:i['TargetId'] for i in load('inserted-ids.json')}
plan=load('insert-plan.json')
assert len(ids)==1269 and len(set(ids.values()))==1269
new=[v for v in after if v['UploadBatchId']==batch]
assert len(new)==1269 and set(v['Id'] for v in new)==set(ids.values())
assert not set(ids.values()) & set(v['Id'] for v in before)
changed_fields={'Id','UploadBatchId','CreatedAt','CreatedBy','UpdatedAt','UpdatedBy','RowVersion',
                'PartProcessCharacteristicId','ProcessId','MachineId','TankId','LineId','CharacteristicId'}
for row in plan:
    s=row['Source'];t=by_id[ids[s['Id']]];m=row['TargetMapping']
    for k,v in s.items():
        if k not in changed_fields: assert t[k]==v, (s['Id'],k)
    for tk,mk in [('PartProcessCharacteristicId','Id'),('ProcessId','ProcessId'),('MachineId','MachineId'),
                  ('TankId','TankId'),('LineId','TankLineId'),('CharacteristicId','CharacteristicId')]:
        assert t[tk]==m[mk]
new_payloads=[json.loads(d['PayloadJson']) for d in new_details.values() if d['UploadBatchId']==batch]
assert len(new_payloads)==1269
assert {int(d['TransferSourceMeasurementId']) for d in new_payloads}==set(ids)
assert all(d['TransferSourceDatabase']=='PMR_SPC_TEST' for d in new_payloads)
assert all(d['TransferSourceBatchId']=='79ceeb2e-cfc6-46b2-9032-5cf832a3238b' for d in new_payloads)
result=dict(Status='Passed',OriginalProductionMeasurementsUnchanged=813,Inserted=1269,
            FinalProductionMeasurements=2082,SourceMeasurementsUnchanged=1838,
            NewBatchId=batch,PayloadTraceabilityVerified=1269,
            MeasurementValuesTimesPhasesAndReferencesVerified=1269,
            ProductionMappingsAndSchemaUnchanged=True,OriginalBatchesAndScopedDetailsUnchanged=True)
(p/'post-verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
