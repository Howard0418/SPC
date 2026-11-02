"""Read-only selection of the two authorized etch report months."""
import copy
from datetime import date

def select_months(manifest):
 result=copy.deepcopy(manifest);reports=[];keys=set()
 for r in result['reports']:
  day=date.fromisoformat(r['reportDate'])
  if day.year!=2026 or day.month not in (9,10):continue
  key=(r['reportDate'],r['lineCode'])
  if key in keys:raise ValueError('Duplicate day/line')
  keys.add(key);reports.append(r)
 if not reports:raise ValueError('No authorized candidates')
 result['reports']=reports
 for key in ('quarantined','excluded','skipped'):result[key]=[r for r in result.get(key,[])if r.get('sheet','')[:2]in ('09','10')]
 return result
