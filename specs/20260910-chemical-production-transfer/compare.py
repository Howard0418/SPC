"""Offline, read-only diff for explicitly documented chemical import batches."""
import csv
import json
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path

ROOT = Path(__file__).resolve().parent
BATCHES = {
    'BFADDEA3-D5DD-4CB8-8761-3AC8A3432AE7',
    '9438B6C2-2B75-40C4-B78C-017147A2A364',
    '79CEEB2E-CFC6-46B2-9032-5CF832A3238B',
}
VALUES = ['MeasuredValue', 'RecheckValue', 'AdjustAction', 'AdjustAmount',
          'Operator', 'SampleNo', 'LotNo', 'SerialNo', 'WorkOrderNo',
          'SubLotNo', 'ParentLotNo', 'SideCode']

def mapping_key(m):
    return tuple(m.get(k) for k in ('ControlScope', 'ProcessCode', 'MachineCode',
                                  'TankCode', 'CharacteristicCode', 'Unit'))

def date_key(v):
    if v['PortalDailyDate']:
        return v['PortalDailyDate'][:10]
    # This API import stored the Excel wall-clock value without timezone conversion.
    return datetime.fromisoformat(v['MeasuredAt']).date().isoformat()

def matching(source, target):
    if source['SamplingPhase'] != target['SamplingPhase']:
        return False
    if source['PortalDailyDate'] or target['PortalDailyDate']:
        return date_key(source) == date_key(target)
    return all(source[k] == target[k] for k in ('MeasuredAt','LotNo','SampleNo'))

def test():
    v = dict(PortalDailyDate=None, MeasuredAt='2026-08-03T01:10:00',
             SamplingPhase='GENERAL', LotNo='', SampleNo=1)
    assert date_key(v) == '2026-08-03'
    assert matching(v,dict(v))
    assert not matching(v,dict(v, MeasuredAt='2026-08-03T02:10:00'))
    assert matching(v,dict(v, PortalDailyDate='2026-08-03T00:00:00'))
    assert not matching(v,dict(v, SamplingPhase='MIDDLE'))
    assert date_key(dict(v,MeasuredAt='2026-08-02T17:10:00')) == '2026-08-02'
    assert len(BATCHES)==3 and 'BDBB9730-FBB3-4D64-833C-944B40FE2F81' not in BATCHES
    assert mapping_key({'Unit':'g/L'}) != mapping_key({'Unit':'ml/L'})
    print('PASS: time boundary, daily/API duplicate, independent shifts, source allowlist, unit mapping')

def main():
    load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
    sources=load('test-measurements.json'); targets=load('production-measurements.json')
    sm={m['Id']:m for m in load('test-mappings.json')}
    pm=defaultdict(list)
    for m in load('production-mappings.json'): pm[mapping_key(m)].append(m)
    tv=defaultdict(list)
    for v in targets: tv[v['PartProcessCharacteristicId']].append(v)
    details=defaultdict(list)
    for d in load('test-details.json'):
        if d['UploadBatchId'] not in BATCHES or not d['IsValid'] or d['IsDeleted']: continue
        payload=json.loads(d['PayloadJson'])
        raw=payload.get('MeasuredAt','')
        try:
            dt=datetime.strptime(raw,'%Y%m%d %H:%M:%S')
        except ValueError:
            continue
        key=(d['UploadBatchId'],int(payload['ResolvedMappingId']),dt.isoformat(),float(payload['MeasuredValue']))
        details[key].append(d)
    report=[]; plan=[]
    for s in sources:
        m=sm[s['PartProcessCharacteristicId']]
        entry=dict(SourceId=s['Id'],SourceBatchId=s['UploadBatchId'],Line=m['MachineCode'],
                   Tank=m['TankName'],Characteristic=m['CharacteristicName'],Unit=m['Unit'],
                   Date=date_key(s),Phase=s['SamplingPhase'],Value=s['MeasuredValue'],
                   TargetMappingId='',TargetId='',Status='',Differences='')
        if s['UploadBatchId'] not in BATCHES or s['IsDeleted']:
            entry['Status']='ExcludedSource'
        else:
            maps=pm[mapping_key(m)]
            if len(maps)!=1 or not maps[0]['IsEnabled']:
                entry['Status']='MissingOrAmbiguousMapping'
            else:
                tm=maps[0]; entry['TargetMappingId']=tm['Id']
                matches=[t for t in tv[tm['Id']] if matching(s,t)]
                if matches:
                    entry['TargetId']=';'.join(str(t['Id']) for t in matches)
                    diffs=sorted({k for t in matches for k in VALUES if s.get(k)!=t.get(k)})
                    entry['Differences']=';'.join(diffs)
                    entry['Status']='Conflict' if diffs or len(matches)>1 else 'Existing'
                elif any(s.get(k) is not None for k in ('ChemicalId','SlotId')) or s['PartId']!=0:
                    entry['Status']='UnsupportedReference'
                else:
                    evidence=details[(s['UploadBatchId'],s['PartProcessCharacteristicId'],s['MeasuredAt'],s['MeasuredValue'])]
                    if len(evidence)!=1:
                        entry['Status']='MissingOrAmbiguousImportEvidence'
                    else:
                        entry['Status']='Insert'
                        plan.append(dict(Source=s,TargetMapping=tm,SourceDetail=evidence[0]))
                        tv[tm['Id']].append(s)
        report.append(entry)
    with (ROOT/'difference.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(report[0])); w.writeheader(); w.writerows(report)
    (ROOT/'insert-plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
    summary=dict(StatusCounts=dict(Counter(r['Status'] for r in report)),
                 InsertByLine=dict(Counter(r['Line'] for r in report if r['Status']=='Insert')),
                 SourceMeasurements=len(sources),TargetMeasurements=len(targets))
    (ROOT/'difference-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(summary,ensure_ascii=True))

if __name__=='__main__':
    test()
    if '--self-test' not in sys.argv: main()
