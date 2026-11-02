"""Select one reviewed September group for a separately labelled trend UI fixture."""
import argparse
import copy
import json
from pathlib import Path

def select_fixture(manifest):
 if manifest['summary'].get('month')!='2026-09':raise ValueError('September only')
 rows=[copy.deepcopy(r) for r in manifest['rows'] if r.get('status')=='validated' and r.get('line')=='DV' and r.get('slot')=='顯影']
 if not 1<=len(rows)<=31:raise ValueError('One reviewed group, at most 31 rows')
 keys={json.dumps({k:r.get(k) for k in ('line','slot','analysisItem','expression','fCells','lower','upper')},sort_keys=True)for r in rows}
 if len(keys)!=1 or not all(r.get('fCells') and r.get('fileDay','').startswith('2026-09-')for r in rows)or not any(r.get('recheckPrimary')is not None for r in rows):raise ValueError('Require one formula/F group, September dates and recheck coverage')
 files=[copy.deepcopy(f)for f in manifest['files']if f['file']in {r['file']for r in rows}]
 if {f['file']for f in files}!={r['file']for r in rows}:raise ValueError('Missing source fingerprint')
 return dict(summary=dict(month='2026-09',purpose='trend-ui',rows=len(rows),databaseWritten=False),files=files,rows=rows)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args()
 result=select_fixture(json.loads((a.source/'manifest.json').read_text(encoding='utf-8')));a.output_dir.mkdir(parents=True,exist_ok=True);(a.output_dir/'manifest.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(result['summary'])
