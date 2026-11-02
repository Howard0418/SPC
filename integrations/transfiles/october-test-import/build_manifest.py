"""Build a read-only October manifest; no network API or database writes.

Usage: python build_manifest.py --tool-root PATH --output-dir PATH
"""
import argparse
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from datetime import date
from pathlib import Path

def build(tool_root,output):
 sys.path.insert(0,str(tool_root))
 import openpyxl
 from chemical_daily_parser import parse_chemical_daily_report
 from chemical_excel_reconcile import convert_formula,calculate,number,formula_key,recheck_column
 cfg=json.loads((tool_root/'scheduled_import_config.json').read_text(encoding='utf-8-sig'))
 source=Path(cfg['chemical_source']);rows=[];files=[]
 for path in sorted(source.rglob('*.xlsx')):
  match=re.search(r'(?<!\d)(2026)-(\d{1,2})-(\d{1,2})(?!\d)',path.stem)
  if path.name.startswith('~$') or not match:continue
  day=date(*map(int,match.groups()))
  if not date(2026,10,1)<=day<=date(2026,10,31):continue
  digest=hashlib.sha256(path.read_bytes()).hexdigest()
  parsed=parse_chemical_daily_report(path)
  formulas=openpyxl.load_workbook(path,data_only=False,read_only=True)
  cached=openpyxl.load_workbook(path,data_only=True,read_only=True)
  f_values={c.coordinate:c.value for r in cached['F'].iter_rows() for c in r if c.value is not None} if 'F' in cached.sheetnames else {}
  try:
   for original in parsed:
    row={**original,'file':str(path),'fileHash':digest,'fileDay':day.isoformat(),'samplingPhase':'MIDDLE' if '中班' in path.stem else 'OPEN','samplingStage':'OPEN' if '開' in original['sheet'] else 'CLOSE' if '收' in original['sheet'] else 'GENERAL','testTimeConvention':'Report-day test fixture; not actual sampling time','status':'blocked'}
    rows.append(row)
    try:
     n=row.get('sourceRow')
     if not n:raise ValueError(row.get('reason','無量測來源列'))
     if row['measurementDatetime'][:10]!=day.isoformat():raise ValueError('表內日期與檔名報表日不一致。')
     expression,refs=convert_formula(row['formula'],n)
     raw=number(row['value']);expected=number(row['formulaResult'])
     used={ref:str(number(f_values[ref])) for ref in refs if ref in f_values}
     calculated=calculate(expression,raw,used)
     if abs(calculated-expected)>number('0.00000001'):raise ValueError('Excel快取與原公式／F獨立重算不一致。')
     lower=number(row['lower']);upper=number(row['upper'])
     if lower>upper:raise ValueError('規格上下限倒置。')
     row.update(expression=expression,fCells=used,primary=str(raw),excel=str(expected),decimalResult=str(calculated),lower=str(lower),upper=str(upper))
     units=re.findall(r'[（(]([^()（）]+)[）)]',row['analysisItem'])
     row['unit']=units[0] if units else ''
     header=list(next(formulas[row['sheet']].iter_rows(min_row=6,max_row=6,max_col=22,values_only=True)))
     re_column=recheck_column(header)
     recheck=cached[row['sheet']].cell(n,re_column).value
     row['recheckValue']=recheck
     # Determine M/N titration column by header, never mistake adjustment text for recheck.
     if recheck is not None and str(recheck).strip() not in ('','-','－','—','.','。'):
      recheck_raw=number(recheck)
      candidates=[]
      re_letter=openpyxl.utils.get_column_letter(re_column)
      for cells in formulas[row['sheet']].iter_rows(min_row=n,max_row=n,min_col=re_column+1,max_col=22):
       for cell in cells:
        if isinstance(cell.value,str) and cell.value.startswith('=') and re.search(rf'(?<![A-Z0-9_]){re_letter}\$?{n}(?!\d)',cell.value,re.I):candidates.append(cell)
      if len(candidates)!=1:raise ValueError('複驗公式缺漏或不唯一，不能沿用初驗結果。')
      cell=candidates[0];re_expression,re_refs=convert_formula(cell.value,n,re_letter)
      if formula_key(re_expression)!=formula_key(expression):raise ValueError('初複驗公式不同，需確認取樣／換算規則。')
      used.update({ref:str(number(f_values[ref])) for ref in re_refs if ref in f_values})
      re_expected=number(cached[row['sheet']][cell.coordinate].value)
      re_calculated=calculate(re_expression,recheck_raw,used)
      if abs(re_calculated-re_expected)>number('0.00000001'):raise ValueError('複驗快取與原公式／F獨立重算不一致。')
      row.update(recheckPrimary=str(recheck_raw),recheckExcel=str(re_expected),recheckDecimalResult=str(re_calculated),recheckSourceFormula=cell.value)
     row['status']='validated'
    except (ValueError,KeyError,TypeError) as error:row['issue']=str(error)
  finally:formulas.close();cached.close()
  if digest!=hashlib.sha256(path.read_bytes()).hexdigest():raise RuntimeError('來源檔案在讀取期間變更，停止。')
  files.append(dict(file=str(path),sha256=digest,rows=len(parsed),sourceUnchanged=True))
 groups=defaultdict(list)
 for row in rows:
  if row['status']=='validated':groups[(row['fileDay'],row['line'],row['slot'],row['analysisItem'],row['samplingPhase'],row['samplingStage'])].append(row)
 for key,group in groups.items():
  signatures={json.dumps({k:r.get(k) for k in ('primary','excel','expression','fCells','lower','upper','recheckPrimary','recheckExcel')},sort_keys=True) for r in group}
  if len(signatures)>1:
   for r in group:r.update(status='blocked',issue='同日同項目有不同來源值／公式／F／規格；缺少時段或版本優先依據。')
  else:
   for r in group[1:]:r.update(status='duplicate_identical',issue='同日同項目內容完全相同，保留全部來源但不重複匯入。')
 summary=dict(files=len(files),rows=len(rows),statuses=dict(Counter(r['status'] for r in rows)),issues=dict(Counter(r.get('issue') for r in rows if r['status']=='blocked')),rechecksValidated=sum(r['status']=='validated' and 'recheckPrimary' in r for r in rows),month='2026-10',databaseWritten=False)
 output.mkdir(parents=True,exist_ok=True)
 (output/'manifest.json').write_text(json.dumps(dict(summary=summary,files=files,rows=rows),ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(summary,ensure_ascii=True))

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--tool-root',type=Path,required=True);parser.add_argument('--output-dir',type=Path,required=True)
 args=parser.parse_args();build(args.tool_root,args.output_dir)
