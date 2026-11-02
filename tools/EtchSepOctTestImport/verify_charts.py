"""Reconcile test chart API raw IDs/values and Xbar-S subgroups with the reviewed manifest."""
import json
import statistics
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import sys

root=Path(sys.argv[1]);base='https://spctest.pmr.com.tw'
def get(path):
 with urllib.request.urlopen(base+path,timeout=30)as r:
  if r.status!=200:raise ValueError('HTTP failure')
  return json.load(r)
if get('/api/version')['environment']!='test':raise ValueError('Test guard')
manifest=json.loads((root/'manifest.json').read_text(encoding='utf-8'));verified=json.loads((root/'verification.json').read_text(encoding='utf-8'));maps={m['id']:m for m in json.loads((root/'maps.json').read_text(encoding='utf-8'))}
reports={f"ETCH:{r['reportDate']}:{r['lineCode']}":r for r in manifest['reports']};records=verified['results'];output=[]
isolatedKeys={f"ETCH:2026-{r['sheet'][:2]}-{r['sheet'][2:]}:{r['lineCode']}"for k in ('excluded','quarantined')for r in manifest.get(k,[])if len(r.get('sheet',''))==4}
def check(task):
 ppc,month=task;relevant=[r for r in records if ppc in r['ppcIds']and r['ReportDate'].startswith(month)];expected={s['Id']:s['MeasuredValue']for r in relevant for s in r['storedRows']if s['PartProcessCharacteristicId']==ppc}
 end=month+('-30'if month.endswith('09')else'-31');chart=get(f'/api/v1/spc/chart?ppcId={ppc}&startDate={month}-01&endDate={end}')
 points=chart['rawDataPoints'];actual={p['variableMeasurementId']:p['value']for p in points}
 extras=[p for p in points if p['variableMeasurementId']not in expected]
 if not set(expected)<=set(actual)or any(abs(actual[i]-v)>1e-8 for i,v in expected.items())or any(p['lotNo']not in isolatedKeys for p in extras):raise ValueError(f'Chart ID/value mismatch {ppc}/{month}, expected={len(expected)}, actual={len(actual)}, extra={len(extras)}')
 code=maps[ppc]['characteristic']['characteristicCode'];checked=0
 if code in ('ETCH_A_AVG','ETCH_B_AVG'):
  subgroups=chart['chartData']['points'];bykey={p['lotNo']:p for p in subgroups}
  if not {r['key']for r in relevant}<=set(bykey)or not(set(bykey)-{r['key']for r in relevant})<=isolatedKeys:raise ValueError('Subgroup key mismatch')
  for r in relevant:
   values=[float(p['beforeValue'])-float(p['afterValue'])for p in reports[r['key']]['points']if p['side']==('A'if code=='ETCH_A_AVG'else'B')];p=bykey[r['key']]
   if p['n']!=len(values)or abs(p['xbar']-statistics.mean(values))>1e-8 or abs(p['stdDev']-statistics.stdev(values))>1e-8:raise ValueError('Xbar/S mismatch')
   checked+=1
 (root/f'chart-{ppc}-{month}.json').write_text(json.dumps(chart,ensure_ascii=False),encoding='utf-8')
 return dict(ppcId=ppc,month=month,rawPoints=len(expected),subgroupsChecked=checked,matched=True,existingIsolatedSourcePoints=len(extras),existingIsolatedKeys=sorted({p['lotNo']for p in extras}))
with ThreadPoolExecutor(max_workers=4)as pool:output=list(pool.map(check,[(ppc,month)for ppc in sorted(maps)for month in ('2026-09','2026-10')]))
(root/'chart-verification.json').write_text(json.dumps(dict(groups=len(output),rawPoints=sum(x['rawPoints']for x in output),subgroupsChecked=sum(x['subgroupsChecked']for x in output),existingIsolatedSourcePoints=sum(x['existingIsolatedSourcePoints']for x in output),results=output),indent=2),encoding='utf-8')
print('Chart groups:',len(output),'raw points:',sum(x['rawPoints']for x in output),'source Xbar/S subgroups:',sum(x['subgroupsChecked']for x in output))
