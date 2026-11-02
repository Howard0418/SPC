"""Recover only the three explicit same-row I-input formulas from October evidence."""
import argparse,hashlib,json,sys,re
from pathlib import Path

def build(tool_root,source,output):
 sys.path.insert(0,str(tool_root))
 import openpyxl
 from chemical_excel_reconcile import convert_formula,calculate,number,recheck_column
 manifest=json.loads((source/'manifest.json').read_text(encoding='utf-8'));rows=[]
 for original in manifest['rows']:
  if original.get('status')!='blocked' or '不支援' not in original.get('issue',''):continue
  row=dict(original);path=Path(row['file'])
  if hashlib.sha256(path.read_bytes()).hexdigest()!=row['fileHash']:raise ValueError('來源已變更')
  workbook=openpyxl.load_workbook(path,read_only=True,data_only=True)
  try:
   sheet=workbook[row['sheet']];n=row['sourceRow'];secondary=number(sheet.cell(n,9).value)
   headers=list(next(sheet.iter_rows(min_row=6,max_row=6,max_col=22,values_only=True)))
   recheck=sheet.cell(n,recheck_column(headers)).value
   if recheck is not None and str(recheck).strip() not in ('','-','－','—','.','。'):raise ValueError('第二滴定值複驗未確認，不送出。')
   expression,refs=convert_formula(row['formula'],n,secondary_column='I')
   if refs:raise ValueError('本批第二滴定值來源意外有F引用，需另核對。')
   result=calculate(expression,row['value'],{},secondary=secondary);expected=number(row['formulaResult'])
   if abs(result-expected)>number('0.00000001'):raise ValueError('第二滴定值公式與Excel不一致。')
   units=re.findall(r'[（(]([^()（）]+)[）)]',row['analysisItem'])
   if len(units)!=1:raise ValueError('第二滴定值來源單位無法唯一辨識。')
   row.update(status='validated',expression=expression,primary=str(number(row['value'])),secondary=str(secondary),excel=str(expected),decimalResult=str(result),fCells={},lower=str(number(row['lower'])),upper=str(number(row['upper'])),unit=units[0],recheckValue=None)
   row.pop('issue',None);rows.append(row)
  finally:workbook.close()
 if len(rows)!=3:raise ValueError('Expected the three explicitly reviewed secondary-input rows')
 output.mkdir(parents=True,exist_ok=True)
 (output/'manifest.json').write_text(json.dumps({'summary':{'month':'2026-10','rows':len(rows)},'files':manifest['files'],'rows':rows},ensure_ascii=False,indent=2),encoding='utf-8')
 print('Explicit secondary formulas validated:',len(rows))

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--tool-root',type=Path,required=True);p.add_argument('--source',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args();build(a.tool_root,a.source,a.output_dir)
