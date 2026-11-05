"""彙整全來源藥液日報preview；只讀Excel並輸出JSON，不寫資料庫。"""
from __future__ import annotations

import argparse
import hashlib
import json
from concurrent.futures import ProcessPoolExecutor
from decimal import Decimal
from pathlib import Path
from typing import Any

from chemical_daily_parser_v2 import parse_report
from chemical_excel_reconcile import calculate, convert_formula, number


TOLERANCE = Decimal("0.00000001")


def _read_report(task):
    root,source=task;path=Path(root)/source["relativePath"]
    try:
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest.lower()!=source["sha256"].lower():raise SourceHashMismatch(f"來源SHA已變更：manifest={source['sha256']} actual={digest}")
        return {"ok":True,"report":parse_report(path,source)}
    except Exception as exc:
        return {"ok":False,"code":"SOURCE_HASH_MISMATCH" if isinstance(exc,SourceHashMismatch) else "FILE_READ_ERROR","message":str(exc)}


def _error(source: dict[str, Any], code: str, reason: str, **location: Any) -> dict[str, Any]:
    return {"sourceFile": source.get("relativePath"), "reportDate": source.get("reportDate"), "shift": source.get("shift"), "code": code, "reason": reason, **location}


def _validate_phase(row: dict[str, Any], phase_name: str, phase: dict[str, Any] | None, f_values: dict[str, Any]) -> list[dict[str, Any]]:
    if phase is None or phase["mode"] == "DIRECT":
        return []
    errors: list[dict[str, Any]] = []
    source = {"relativePath": row["sourceFile"], "reportDate": row["reportDate"], "shift": row["shift"]}
    if not phase.get("formula"):
        return [_error(source,"FORMULA_MISSING","滴定數有值但濃度公式缺少",sheet=row["sheet"],sourceRow=row["sourceRow"],phase=phase_name,cell=phase["sourceCells"]["concentration"])]
    column = ''.join(filter(str.isalpha, phase["sourceCells"]["titration"]))
    secondary_column = ''.join(filter(str.isalpha, phase["sourceCells"].get("secondary") or "")) or None
    try:
        expression, references = convert_formula(phase["formula"], row["sourceRow"], column=column, secondary_column=secondary_column)
        missing = [reference for reference in references if f"F!{reference}" not in f_values]
        if missing:
            return [_error(source,"F_VALUE_MISSING","來源F表缺少："+'、'.join(missing),sheet=row["sheet"],sourceRow=row["sourceRow"],phase=phase_name)]
        calculated = calculate(expression, phase["titration"], {reference:f_values[f"F!{reference}"] for reference in references}, secondary=phase.get("secondary"))
        phase["normalizedFormula"] = expression
        phase["calculatedConcentration"] = str(calculated)
        if phase.get("concentration") is None:
            errors.append(_error(source,"FORMULA_RESULT_MISSING","Excel未保存濃度計算結果，請開啟Excel重算並儲存",sheet=row["sheet"],sourceRow=row["sourceRow"],phase=phase_name,cell=phase["sourceCells"]["concentration"]))
        elif abs(calculated-number(phase["concentration"])) > TOLERANCE:
            errors.append(_error(source,"FORMULA_RESULT_MISMATCH",f"Excel濃度{phase['concentration']}與重算{calculated}差異超過1e-8",sheet=row["sheet"],sourceRow=row["sourceRow"],phase=phase_name,cell=phase["sourceCells"]["concentration"]))
    except ValueError as exc:
        errors.append(_error(source,"FORMULA_INVALID",str(exc),sheet=row["sheet"],sourceRow=row["sourceRow"],phase=phase_name,cell=phase["sourceCells"]["concentration"]))
    return errors


def build_preview(source_root: Path, manifest: dict[str, Any], progress=None, workers: int = 1) -> dict[str, Any]:
    root=Path(source_root); rows=[]; errors=[]; files=[]
    conflict_keys={(item["reportDate"],item["shift"]) for item in manifest.get("conflicts",[])}
    included=manifest["included"]
    tasks=[(str(root),source) for source in included]
    if workers>1:
        executor=ProcessPoolExecutor(max_workers=workers);results=executor.map(_read_report,tasks,chunksize=2)
    else:
        executor=None;results=map(_read_report,tasks)
    for index,(source,result) in enumerate(zip(included,results),1):
        path=root/source["relativePath"]
        before=len(rows)
        try:
            if not result["ok"]:raise PreviewFileError(result["code"],result["message"])
            report=result["report"]
            for sheet_error in report["sheetErrors"]:
                errors.append(_error(source,sheet_error["code"],sheet_error["reason"],sheet=sheet_error.get("sheet"),cell=sheet_error.get("cell")))
            for row in report["rows"]:
                # 保存來源檔完整F快照，供測試DB日期排程建立與後續逐筆追溯；不以目前啟用F代替歷史值。
                row["sourceFValues"] = {key:value for key,value in sorted(report["fValues"].items())}
                row_errors=list(row.pop("errors",[]))
                row_errors += _validate_phase(row,"Initial",row["initial"],report["fValues"])
                row_errors += _validate_phase(row,"Recheck",row["recheck"],report["fValues"])
                for item in row_errors:
                    if "sourceFile" not in item:
                        item=_error(source,item["code"],item["reason"],sheet=row["sheet"],sourceRow=row["sourceRow"],cell=item.get("cell"))
                    errors.append(item)
                if row_errors: row["status"]="ERROR"
                elif (row["reportDate"],row["shift"]) in conflict_keys:
                    row["status"]="EXCLUDED_PRE_SEP_CONFLICT" if row["reportDate"]<"2026-09-01" else "SOURCE_CONFLICT"
                else: row["status"]="READY"
                rows.append(row)
            file_rows=rows[before:]
            status="PARSED_WITH_ERRORS" if report["sheetErrors"] else "PARSED"
            files.append({"fileName":source["fileName"],"relativePath":source["relativePath"],"reportDate":source["reportDate"],"shift":source["shift"],"sha256":source["sha256"],"status":status,"rowCount":len(file_rows),"readyRows":sum(r["status"]=="READY" for r in file_rows),"conflictRows":sum(r["status"]=="SOURCE_CONFLICT" for r in file_rows),"errorRows":sum(r["status"]=="ERROR" for r in file_rows),"sheetErrors":len(report["sheetErrors"])})
        except Exception as exc:
            code=exc.code if isinstance(exc,PreviewFileError) else "FILE_READ_ERROR"
            errors.append(_error(source,code,str(exc)))
            files.append({"fileName":source["fileName"],"relativePath":source["relativePath"],"reportDate":source["reportDate"],"shift":source["shift"],"sha256":source["sha256"],"status":"FILE_ERROR","rowCount":0,"readyRows":0,"conflictRows":0,"errorRows":1,"sheetErrors":0})
        if progress and (index % 25 == 0 or index == len(included)):progress(index,len(included))
    if executor:executor.shutdown()
    conflicts=[{**item,"policyAction":"EXCLUDE_NO_IMPORT" if item["reportDate"]<"2026-09-01" else "REQUIRES_APPROVAL"} for item in manifest.get("conflicts",[])]
    summary={"sourceFiles":len(included),"parsedFiles":sum(f["status"] in {"PARSED","PARSED_WITH_ERRORS"} for f in files),"parsedWithErrors":sum(f["status"]=="PARSED_WITH_ERRORS" for f in files),"fileErrors":sum(f["status"]=="FILE_ERROR" for f in files),"rows":len(rows),"readyRows":sum(r["status"]=="READY" for r in rows),"conflictRows":sum(r["status"]=="SOURCE_CONFLICT" for r in rows),"preSeptemberConflictExcludedRows":sum(r["status"]=="EXCLUDED_PRE_SEP_CONFLICT" for r in rows),"errorRows":sum(r["status"]=="ERROR" for r in rows),"errorEntries":len(errors),"excludedFiles":len(manifest.get("excluded",[])),"conflictGroups":len(conflicts),"preSeptemberConflictExcludedGroups":sum(x["policyAction"]=="EXCLUDE_NO_IMPORT" for x in conflicts),"approvalConflictGroups":sum(x["policyAction"]=="REQUIRES_APPROVAL" for x in conflicts),"databaseWritten":False}
    return {"schemaVersion":"chemical-preview-v2","summary":summary,"files":files,"rows":rows,"errors":errors,"excluded":manifest.get("excluded",[]),"conflicts":conflicts}


class SourceHashMismatch(ValueError):
    pass


class PreviewFileError(ValueError):
    def __init__(self,code,message):super().__init__(message);self.code=code


def main() -> int:
    parser=argparse.ArgumentParser();parser.add_argument("--source",type=Path,required=True);parser.add_argument("--manifest",type=Path,required=True);parser.add_argument("--output",type=Path,required=True);parser.add_argument("--workers",type=int,default=1);args=parser.parse_args()
    manifest=json.loads(args.manifest.read_text(encoding="utf-8"))
    preview=build_preview(args.source,manifest,lambda done,total:print(f"PROGRESS {done}/{total}",flush=True),workers=max(1,args.workers))
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(preview,ensure_ascii=False,indent=2,default=str)+"\n",encoding="utf-8")
    print(json.dumps(preview["summary"],ensure_ascii=False),flush=True);return 0


if __name__=="__main__":raise SystemExit(main())
