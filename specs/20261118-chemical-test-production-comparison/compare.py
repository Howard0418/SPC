import json
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

root = Path(__file__).resolve().parent
test = json.loads((root / 'data' / 'test.json').read_text(encoding='utf-8-sig'))
prod = json.loads((root / 'data' / 'production.json').read_text(encoding='utf-8-sig'))

def sval(v):
    if v is None: return ''
    if isinstance(v, bool): return '1' if v else '0'
    return str(v)

def key(row, fields): return tuple(sval(row.get(x)) for x in fields)
def payload(row, fields): return tuple(sval(row.get(x)) for x in fields)

line_fields = ['MappingTotal','EnabledMappings','Tanks','Characteristics','FormulaPolicies','EnabledFormulaPolicies','MeasurementsTotal','MeasurementsActive','MeasurementsDeleted','FirstMeasuredAt','LastMeasuredAt','Receipts','Traces']
test_lines = {x['MachineCode']: x for x in test['lineSummary']}
prod_lines = {x['MachineCode']: x for x in prod['lineSummary']}
line_summary = []
for code in sorted(set(test_lines) | set(prod_lines)):
    t, p = test_lines.get(code, {}), prod_lines.get(code, {})
    row = {'Line': code, 'InTest': bool(t), 'InProduction': bool(p)}
    for f in line_fields:
        row[f'Test_{f}'] = t.get(f)
        row[f'Production_{f}'] = p.get(f)
        if isinstance(t.get(f), (int,float)) and isinstance(p.get(f), (int,float)):
            row[f'Delta_{f}'] = t[f] - p[f]
    line_summary.append(row)

mapping_key = ['MachineCode','ProcessCode','TankCode','CharacteristicCode']
mapping_fields = ['MachineName','ProcessName','TankName','CharacteristicName','Unit','IsEnabled','IsDeleted','USL','LSL','TargetValue','UCL','CL','LCL','SampleSize','ChartTypeId','IsRequired','DisplayMode','SequenceNo','DecimalPlaces','InputMode','ValueLabel','FormulaConfigHash','AnalysisConfigHash','HasPolicy','PolicyEnabled','ActiveRevisionNo']
def compare_rows(test_rows, prod_rows, key_fields, compare_fields, category):
    tm, pm = defaultdict(list), defaultdict(list)
    for x in test_rows: tm[key(x,key_fields)].append(x)
    for x in prod_rows: pm[key(x,key_fields)].append(x)
    out=[]
    for k in sorted(set(tm)|set(pm)):
        tl, pl = tm.get(k,[]), pm.get(k,[])
        if not tl or not pl:
            out.append({'Category':category,'Difference':'TestOnly' if tl else 'ProductionOnly',**dict(zip(key_fields,k)),'TestCount':len(tl),'ProductionCount':len(pl),'Field':'(record)','TestValue':len(tl) or None,'ProductionValue':len(pl) or None})
            continue
        if len(tl)!=len(pl):
            out.append({'Category':category,'Difference':'DuplicateCount',**dict(zip(key_fields,k)),'TestCount':len(tl),'ProductionCount':len(pl),'Field':'(record count)','TestValue':len(tl),'ProductionValue':len(pl)})
        ta,tb=tl[0],pl[0]
        for f in compare_fields:
            if sval(ta.get(f)) != sval(tb.get(f)):
                out.append({'Category':category,'Difference':'FieldValue',**dict(zip(key_fields,k)),'TestCount':len(tl),'ProductionCount':len(pl),'Field':f,'TestValue':ta.get(f),'ProductionValue':tb.get(f)})
    return out

master_differences = compare_rows(test['mappings'],prod['mappings'],mapping_key,mapping_fields,'Master')

eff_key = mapping_key + ['EffectiveFromUtc','EffectiveUntilUtcExclusive']
eff_fields = ['RevisionNo','FormulaVersionNo','FormulaConfigHash','FHistoryVersionNo','FVersionCode','FCellsHash']
formula_differences = compare_rows(test['effectivities'],prod['effectivities'],eff_key,eff_fields,'Effectivity')
fref_key = mapping_key + ['VersionCode','NormalizedCellAddress']
fref_fields = ['DisplayName','SourceFormula','SourceSheet','SourceRow','ReferenceContext']
formula_differences += compare_rows(test['fReferences'],prod['fReferences'],fref_key,fref_fields,'FReference')

measurement_id_fields = mapping_key + ['MeasuredAt','SampleNo','SamplingPhase','SamplingStage','PortalDailyDate']
measurement_value_fields = ['MeasuredValue','RecheckValue','SourceType','SourceReference','AdjustAction','AdjustAmount','Operator','IsDeleted','SourceKeyHash','HasReceipt','TraceCount']
measurement_core_fields = ['MeasuredValue','RecheckValue']
tm, pm = defaultdict(list), defaultdict(list)
for x in test['measurements']: tm[key(x,measurement_id_fields)].append(x)
for x in prod['measurements']: pm[key(x,measurement_id_fields)].append(x)

measurement_differences=[]
common_exact=0
common_keys=0
common_core_equal=0
common_core_different=0
for k in sorted(set(tm)|set(pm)):
    tl,pl=tm.get(k,[]),pm.get(k,[])
    line=k[0]
    base=dict(zip(measurement_id_fields,k))
    if not tl or not pl:
        src=tl or pl
        for x in src:
            measurement_differences.append({'Difference':'TestOnly' if tl else 'ProductionOnly',**base,**{f:x.get(f) for f in measurement_value_fields}})
        continue
    common_keys += 1
    tc=Counter(payload(x,measurement_value_fields) for x in tl)
    pc=Counter(payload(x,measurement_value_fields) for x in pl)
    core_equal = Counter(payload(x,measurement_core_fields) for x in tl) == Counter(payload(x,measurement_core_fields) for x in pl)
    if core_equal: common_core_equal += 1
    else: common_core_different += 1
    common_exact += sum((tc & pc).values())
    if tc == pc: continue
    measurement_differences.append({'Difference':'SameKeyDifferentPayload',**base,'CoreValueDifference':not core_equal,'TestValues':json.dumps([{f:x.get(f) for f in measurement_value_fields} for x in tl],ensure_ascii=False),'ProductionValues':json.dumps([{f:x.get(f) for f in measurement_value_fields} for x in pl],ensure_ascii=False)})

line_measurement_diff=[]
for code in sorted(set(test_lines)|set(prod_lines)):
    td=[x for x in measurement_differences if x['MachineCode']==code and x['Difference']=='TestOnly']
    pd=[x for x in measurement_differences if x['MachineCode']==code and x['Difference']=='ProductionOnly']
    vd=[x for x in measurement_differences if x['MachineCode']==code and x['Difference']=='SameKeyDifferentPayload']
    value_diff=[x for x in vd if x.get('CoreValueDifference')]
    tdates=[x['MeasuredAt'] for x in test['measurements'] if x['MachineCode']==code and not x['IsDeleted']]
    pdates=[x['MeasuredAt'] for x in prod['measurements'] if x['MachineCode']==code and not x['IsDeleted']]
    common_start=max(min(tdates) if tdates else '',min(pdates) if pdates else '') if tdates and pdates else ''
    common_end=min(max(tdates) if tdates else '',max(pdates) if pdates else '') if tdates and pdates else ''
    overlap_test=sum(1 for x in test['measurements'] if x['MachineCode']==code and common_start <= x['MeasuredAt'] <= common_end) if common_start else 0
    overlap_prod=sum(1 for x in prod['measurements'] if x['MachineCode']==code and common_start <= x['MeasuredAt'] <= common_end) if common_start else 0
    active_t=defaultdict(list); active_p=defaultdict(list)
    for x in test['measurements']:
        if x['MachineCode']==code and not x['IsDeleted']: active_t[key(x,measurement_id_fields)].append(x)
    for x in prod['measurements']:
        if x['MachineCode']==code and not x['IsDeleted']: active_p[key(x,measurement_id_fields)].append(x)
    active_common=set(active_t)&set(active_p)
    active_equal=sum(1 for k in active_common if Counter(payload(x,measurement_core_fields) for x in active_t[k])==Counter(payload(x,measurement_core_fields) for x in active_p[k]))
    line_measurement_diff.append({'Line':code,'CommonStart':common_start,'CommonEnd':common_end,'TestRowsInOverlap':overlap_test,'ProductionRowsInOverlap':overlap_prod,'ActiveCommonKeys':len(active_common),'ActiveValueEqualKeys':active_equal,'ActiveValueDifferentKeys':len(active_common)-active_equal,'ActiveTestOnlyKeys':len(set(active_t)-set(active_p)),'ActiveProductionOnlyKeys':len(set(active_p)-set(active_t)),'AllTestOnlyRows':len(td),'AllProductionOnlyRows':len(pd),'AllSameKeyDifferentPayload':len(vd),'AllSameKeyValueDifferent':len(value_diff),'AllSameKeyMetadataOnlyDifferent':len(vd)-len(value_diff)})

result={
 'generatedAt':datetime.now().isoformat(timespec='seconds'),
 'environments':{'test':test['environment'],'production':prod['environment']},
 'lineSummary':line_summary,
 'masterDifferences':master_differences,
 'formulaDifferences':formula_differences,
 'measurementLineSummary':line_measurement_diff,
 'measurementDifferences':measurement_differences,
 'controls':{
   'testMappings':len(test['mappings']),'productionMappings':len(prod['mappings']),
   'testEffectivities':len(test['effectivities']),'productionEffectivities':len(prod['effectivities']),
   'testFReferences':len(test['fReferences']),'productionFReferences':len(prod['fReferences']),
   'testMeasurements':len(test['measurements']),'productionMeasurements':len(prod['measurements']),
   'masterDifferenceRows':len(master_differences),'formulaDifferenceRows':len(formula_differences),
   'measurementDifferenceRows':len(measurement_differences),'commonMeasurementKeys':common_keys,'commonExactRows':common_exact,
   'commonCoreEqualKeys':common_core_equal,'commonCoreDifferentKeys':common_core_different
 }}
result['controls']['activeCommonKeys']=sum(x['ActiveCommonKeys'] for x in line_measurement_diff)
result['controls']['activeValueEqualKeys']=sum(x['ActiveValueEqualKeys'] for x in line_measurement_diff)
result['controls']['activeValueDifferentKeys']=sum(x['ActiveValueDifferentKeys'] for x in line_measurement_diff)
(root/'comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2,default=str),encoding='utf-8')
print(json.dumps(result['controls'],ensure_ascii=False,indent=2))
