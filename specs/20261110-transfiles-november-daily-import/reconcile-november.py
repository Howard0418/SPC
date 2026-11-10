from __future__ import annotations
import argparse, json, re, sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, r"C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\TransFiles")
from inventory_reconciliation import reconcile_records


def canon(value):
    first=str(value or '').replace('\r','\n').split('\n')[0]
    return re.sub(r"[^0-9a-z\u4e00-\u9fff]+", "", first.lower())

def canon_item(value):
    first=str(value or '').replace('\r','\n').split('\n')[0]
    return canon(re.sub(r"\([^)]*\)|（[^）]*）|\[[^]]*\]",'',first))

def line_matches(source,machine):
    s,m=canon(source),canon(machine)
    return s==m or (s=='舊線' and m in {'pt1','qe1','st1','dv1'}) or (s=='新線' and m in {'pt2','qe2','st2','dv2'})

def source_line(row):
    line=canon(row.get('line'));sheet=canon(row.get('sheet'))
    if line in {'pt','qe','st','dv'} and sheet in {'舊線','新線'}:
        return line + ('1' if sheet=='舊線' else '2')
    return row.get('line')


def main():
    ap=argparse.ArgumentParser();ap.add_argument('source');ap.add_argument('spc');ap.add_argument('portal');ap.add_argument('output');a=ap.parse_args()
    source=json.loads(Path(a.source).read_text(encoding='utf-8'))
    spc=json.loads(Path(a.spc).read_text(encoding='utf-8'));portal=json.loads(Path(a.portal).read_text(encoding='utf-8'))
    masters=spc.get('masters') or [];measurements=spc.get('measurements') or []
    if isinstance(masters,str): masters=json.loads(masters)
    if isinstance(measurements,str): measurements=json.loads(measurements)
    master_index=defaultdict(list)
    for m in masters: master_index[(canon(m['slot']),canon_item(m['analysisItem']))].append(m)
    chemical=[];mapping_errors=[]
    for item in source['items']:
        if item.get('dataType')!='chemical': continue
        for row in item.get('rows',[]):
            if row.get('status') not in {'ready_for_preview','skipped_duplicate'}: continue
            candidates=[m for m in master_index[(canon(row.get('slot')),canon_item(row.get('analysisItem')))] if line_matches(source_line(row),m.get('line'))]
            if len(candidates)!=1:
                mapping_errors.append({'source':item.get('fileName'),'sheet':row.get('sheet'),'row':row.get('sourceRow'),'line':row.get('line'),'slot':row.get('slot'),'analysisItem':row.get('analysisItem'),'candidateCount':len(candidates)})
                continue
            m=candidates[0];day=str(row.get('measurementDatetime',''))[:10];shift='MIDDLE' if row.get('shift')=='MIDDLE' else 'OPEN';stage=row.get('samplingPhase') or 'GENERAL'
            value=row.get('formulaResult') if row.get('formula') else row.get('value')
            chemical.append({'key':f"{m['ppcId']}|{day}|{shift}|{stage}",'value':value,'recheckValue':row.get('recheckFormulaResult'),'source':{'file':item.get('fileName'),'sheet':row.get('sheet'),'row':row.get('sourceRow')}})
    existing_chemical=[{'key':f"{x['ppcId']}|{x['reportDate']}|{x['shift']}|{x['samplingStage']}",'value':x.get('value'),'recheckValue':x.get('recheckValue'),'source':{'id':x.get('id')}} for x in measurements]
    chemical_result=reconcile_records(chemical,existing_chemical)

    etch=[]
    for item in source['items']:
        if item.get('dataType')=='etching' and item.get('status')=='ready_for_preview':
            r=item['report'];points=[{k:p.get(k) for k in ('side','repeatNo','opNo','beforeValue','afterValue')} for p in r.get('points',[])]
            etch.append({'key':f"{r['reportDate']}|{r['lineCode']}",'lineSpeed':r.get('lineSpeed'),'operatorName':r.get('operatorName'),'points':points,'source':{'sheet':r.get('sheet')}})
    existing_etch=[]
    for r in portal:
        points=[{k:p.get(k) for k in ('side','repeatNo','opNo','beforeValue','afterValue')} for p in (r.get('points') or [])]
        existing_etch.append({'key':f"{r['reportDate']}|{r['lineCode']}",'lineSpeed':r.get('lineSpeed'),'operatorName':r.get('operatorName'),'points':points,'source':{'id':r.get('id')}})
    etch_result=reconcile_records(etch,existing_etch)
    result={'environment':'test','databases':['PMR_SPC_TEST','PMR_PORTAL_TEST'],'readOnly':True,
      'chemical':{'sourceRows':len(chemical),'mappingErrors':mapping_errors,'counts':dict(Counter(x['classification'] for x in chemical_result)),'rows':chemical_result},
      'etching':{'sourceReports':len(etch),'sourcePoints':sum(len(x['points']) for x in etch),'counts':dict(Counter(x['classification'] for x in etch_result)),'rows':etch_result}}
    Path(a.output).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'chemical':result['chemical']['counts'],'chemicalMappingErrors':len(mapping_errors),'etching':result['etching']['counts']},ensure_ascii=False))

if __name__=='__main__':main()
