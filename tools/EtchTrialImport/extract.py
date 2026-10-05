"""Read-only September-1 trial manifest. Never writes the source workbook."""
from pathlib import Path
import hashlib, json, sys
import openpyxl
from openpyxl.utils import get_column_letter

LAYOUT = {"PT1": (2,5,5), "PT2": (8,10,11), "QE1": (19,5,22), "QE2": (25,10,28)}

def parse_line(values, formulas, line, sha):
    start, ops, speedcol = LAYOUT[line]
    points, sources = [], []
    for side, before_row, after_row, amount_row in [("A",4,14,33),("B",9,19,38)]:
        for repeat in range(5):
            for op in range(ops):
                col=get_column_letter(start+op)
                b,a,e=f"{col}{before_row+repeat}",f"{col}{after_row+repeat}",f"{col}{amount_row+repeat}"
                before,after=values.get(b),values.get(a)
                if not all(isinstance(x,(int,float)) and not isinstance(x,bool) for x in (before,after)):
                    raise ValueError(f"{line}: missing {b}/{a}")
                if before<after or min(before,after)<0: raise ValueError(f"{line}: negative {e}")
                if formulas.get(e)!=f"={b}-{a}": raise ValueError(f"{line}: unexpected formula {e}")
                if not isinstance(values.get(e),(int,float)) or abs(values[e]-(before-after))>1e-8:
                    raise ValueError(f"{line}: stale cache {e}")
                points.append(dict(side=side,repeatNo=repeat+1,opNo=op+1,beforeValue=round(before,6),afterValue=round(after,6)))
                sources.append(dict(beforeCell=b,afterCell=a,amountCell=e,formula=formulas[e],cachedAmount=values[e]))
    speedcell=f"{get_column_letter(speedcol)}27";speed=values.get(speedcell)
    if not isinstance(speed,(int,float)) or speed<=0: raise ValueError(f"{line}: invalid actual speed")
    return dict(reportDate="2026-09-01",lineCode=line,lineSpeed=speed,operatorName="歷史匯入（原量測人員未記錄）",points=points,
        provenance=dict(sheet="0901",fileSha256=sha,speedCell=speedcell,points=sources,
            summary={f"{get_column_letter(c)}{r}":dict(formula=formulas.get(f"{get_column_letter(c)}{r}"),cached=values.get(f"{get_column_letter(c)}{r}")) for c,r in [(start,43),(start,44),(speedcol,28)]}))

def parse_sheet(values, formulas, sheet, sha):
    from datetime import datetime
    date=datetime.strptime('2026'+sheet,'%Y%m%d')
    if date.month!=9 or len(sheet)!=4: raise ValueError('September only')
    reports,excluded=[],[]
    for line,(start,ops,_) in LAYOUT.items():
        raw=[values.get(f'{get_column_letter(c)}{r}') for c in range(start,start+ops) for r in range(4,24)]
        if all(x is None for x in raw): continue
        try: report=parse_line(values,formulas,line,sha)
        except ValueError as error:
            if not any(kind in str(error) for kind in (': negative ', ': missing ')): raise
            excluded.append(dict(sheet=sheet,lineCode=line,reason=str(error)));continue
        report['reportDate']=date.strftime('%Y-%m-%d');report['provenance']['sheet']=sheet
        reports.append(report)
    return reports,excluded

if __name__=="__main__":
    source=Path(sys.argv[1]);destination=Path(sys.argv[2]);sha=hashlib.sha256(source.read_bytes()).hexdigest()
    w=openpyxl.load_workbook(source,read_only=True,data_only=False);v=openpyxl.load_workbook(source,read_only=True,data_only=True)
    def cells(ws):return {f"{get_column_letter(c)}{r}":x for r,row in enumerate(ws.iter_rows(min_row=1,max_row=44,max_col=34,values_only=True),1) for c,x in enumerate(row,1)}
    records=[];excluded=[]
    sheets=sorted(s for s in w.sheetnames if len(s)==4 and s.startswith('09')) if '--month' in sys.argv else ['0901']
    for sheet in sheets:
        parsed,quarantined=parse_sheet(cells(v[sheet]),cells(w[sheet]),sheet,sha)
        records.extend(parsed);excluded.extend(quarantined)
    destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_text(json.dumps(dict(source=str(source.resolve()),sha256=sha,reports=records,quarantined=excluded),ensure_ascii=False,indent=2,default=str),encoding='utf-8')
    w.close();v.close()
    assert hashlib.sha256(source.read_bytes()).hexdigest()==sha
    print(f"{len(records)} reports, {sum(len(r['points']) for r in records)} points, {len(excluded)} quarantined; source unchanged")
