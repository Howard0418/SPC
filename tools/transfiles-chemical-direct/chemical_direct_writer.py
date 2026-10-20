"""Date-formula raw input -> SPC preview/prepare -> controlled SQL procedure. Test only."""
import copy
import hashlib
import json
import os
import re
import subprocess
import urllib.request
import urllib.error
from datetime import datetime
from decimal import Decimal,InvalidOperation
from pathlib import Path
from tls_context import create_pmr_ssl_context


def parse_row(row,file_hash,sheet,row_number):
 def raw(name,required=False):
  value=str(row.get(name) if row.get(name) is not None else "").strip()
  if not value and not required:return None
  try:
   number=Decimal(value)
   if not number.is_finite() or len(value)>100:raise InvalidOperation()
  except InvalidOperation:raise ValueError(f"{name}須填有效十進位原始值，不可只填濃度。") from None
  return value
 try:ppc=int(str(row.get("PpcId","")))
 except ValueError:raise ValueError("PpcId須填SPC測試管制項目編號。") from None
 if ppc<=0:raise ValueError("PpcId須大於0。")
 at=str(row.get("MeasuredAt", "")).strip()
 if not re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?(?:Z|[+-]\d{2}:\d{2})",at):raise ValueError("MeasuredAt須含明確時區，例如2026-10-18T09:10:00+08:00；不可猜日期。")
 try:datetime.fromisoformat(at.replace("Z","+00:00"))
 except ValueError:raise ValueError("MeasuredAt日期或時間不存在。") from None
 shift=str(row.get("SamplingPhase",""));stage=str(row.get("SamplingStage") or "GENERAL")
 if shift not in ("OPEN","MIDDLE") or stage not in ("GENERAL","OPEN","CLOSE"):raise ValueError("SamplingPhase填OPEN/MIDDLE；SamplingStage填GENERAL或N1/N2的OPEN/CLOSE。")
 if not re.fullmatch(r"[a-fA-F0-9]{64}",file_hash) or row_number<2:raise ValueError("缺來源檔案SHA256或Excel列號。")
 key=json.dumps(dict(kind="TransFilesChemical-v1",fileHash=file_hash,sheet=sheet,row=row_number,samplingPhase=shift,samplingStage=stage),ensure_ascii=False,separators=(",",":"))
 if len(key)>1000:raise ValueError("工作表名稱過長。")
 return dict(ppcId=ppc,input=dict(measuredAt=at,primary=raw("Primary",True),secondary=raw("Secondary"),sourceKey=key),
             recheckPrimary=raw("RecheckPrimary"),recheckSecondary=raw("RecheckSecondary"),sheet=sheet,row=row_number)


def load_rows(path):
 import openpyxl
 path=Path(path);digest=hashlib.sha256(path.read_bytes()).hexdigest();rows=[];errors=[]
 with openpyxl.load_workbook(path,data_only=False,read_only=True) as workbook:
  for sheet in workbook:
   values=sheet.iter_rows(values_only=True);headers=[str(x or "").strip() for x in next(values,())]
   for index,values_row in enumerate(values,2):
    if not any(x is not None for x in values_row):continue
    try:rows.append(parse_row(dict(zip(headers,values_row)),digest,sheet.title,index))
    except ValueError as ex:errors.append(dict(sheet=sheet.title,row=index,error=str(ex)))
 return rows,errors


def preview_rows(rows,api):
 results=[]
 for source in rows:
  row=copy.deepcopy(source)
  try:
   base=f"/v1/part-process-characteristics/{row['ppcId']}/chemical-formula-schedule"
   row["initialPreview"]=api(base+"/preview",row["input"])
   if row["recheckPrimary"]:
    inp={**row["input"],"primary":row["recheckPrimary"],"secondary":row["recheckSecondary"]}
    row["recheckInput"]=inp;row["recheckPreview"]=api(base+"/preview",inp)
  except (ValueError,RuntimeError) as error:row["error"]=str(error)[:500]
  except Exception:row["error"]="無法預覽此列；請核對權限、日期排程、PpcId與原始值後重新預覽。"
  results.append(row)
 return results


def confirm_rows(previews,api,commit):
 results=[]
 for row in copy.deepcopy(previews):
  # Do not expose bearer preview tokens in reports.
  result=dict(sheet=row["sheet"],row=row["row"],ppcId=row["ppcId"])
  try:
   if row.get("error"):raise ValueError(row["error"])
   base=f"/v1/part-process-characteristics/{row['ppcId']}/chemical-formula-schedule/prepare"
   first=api(base,dict(input=row["input"],token=row["initialPreview"]["token"]))
   second=None
   if row.get("recheckPreview"):second=api(base,dict(input=row["recheckInput"],token=row["recheckPreview"]["token"]))
   if first.get("actor") and hasattr(commit,"actor"):commit.actor=first["actor"]
   result.update(commit(first["receiptId"],second["receiptId"] if second else None))
   result["initialReceiptId"]=result.get("receiptId",first["receiptId"])
  except (ValueError,RuntimeError) as error:result["error"]="本列未確認成功："+str(error)[:500]+"；先查回再重試，不改走舊匯入。"
  except Exception:result["error"]="本列未確認成功；先查來源是否已落地，再重新預覽／重送。缺原始值、過期、同日重複或版本衝突不得改走舊匯入。"
  results.append(result)
 return results


class SpcCalculationApi:
 def __init__(self,base_url,token):
  if base_url.rstrip('/').lower()!='https://spctest.pmr.com.tw/api':raise ValueError("日期公式直接匯入目前只開放SPC測試站。")
  self.base=base_url.rstrip('/');self.token=token
 def __call__(self,path,body):
  class NoRedirect(urllib.request.HTTPRedirectHandler):
   def redirect_request(self,*args,**kwargs):return None
  opener=urllib.request.build_opener(urllib.request.ProxyHandler({}),urllib.request.HTTPSHandler(context=create_pmr_ssl_context()),NoRedirect())
  request=urllib.request.Request(self.base+path,data=json.dumps(body,ensure_ascii=False).encode('utf-8'),headers={'Authorization':'Bearer '+self.token,'Content-Type':'application/json'})
  try:
   with opener.open(request,timeout=60) as response:result=json.load(response)
  except urllib.error.HTTPError as error:
   message="請核對登入權限、公式排程及原始值後重新預覽。"
   if error.code in (400,401,403,409):
    try:message=str(json.loads(error.read().decode('utf-8')).get('message') or message)[:500]
    except (ValueError,UnicodeError):pass
   raise RuntimeError(f"SPC HTTP {error.code}：{message}") from None
  except Exception:raise RuntimeError("SPC日期公式服務拒絕或無法連線，請核對登入／排程並重新預覽。") from None
  if not result.get('success'):raise RuntimeError("SPC日期公式處理失敗，請重新預覽。")
  return result['data']


class SqlDirectWriter:
 def __init__(self,actor,script_path=None):
  self.actor=actor;self.script=Path(script_path or Path(__file__).with_name('chemical_direct_write.ps1'))
  self.connection=os.environ.get('SPC_CHEMICAL_DB_CONNECTION')
  if not self.connection and os.name=='nt':
   import winreg
   try:
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER,'Environment') as key:self.connection=winreg.QueryValueEx(key,'SPC_CHEMICAL_DB_CONNECTION')[0]
   except OSError:pass
  if not self.connection:raise ValueError("尚未設定測試專用低權限Windows整合連線SPC_CHEMICAL_DB_CONNECTION；不會改走API匯入。")
 def __call__(self,initial,recheck=None):return self.run(dict(initialReceiptId=initial,recheckReceiptId=recheck,actor=self.actor))
 def status(self,receipt):return self.run(dict(receiptId=receipt,actor=self.actor,action='status'))
 def run(self,payload):
  result=subprocess.run(['powershell.exe','-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',str(self.script)],input=json.dumps(payload),text=True,encoding='utf-8',capture_output=True,env={**os.environ,'SPC_CHEMICAL_DB_CONNECTION':self.connection},creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0),timeout=90)
  if result.returncode:raise RuntimeError("受控資料庫寫入未確認成功；請檢查測試環境、Windows權限或來源衝突後查回／重試。")
  return json.loads(result.stdout.lstrip('\ufeff'))
