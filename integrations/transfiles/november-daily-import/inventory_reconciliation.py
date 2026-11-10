"""Pure, read-only business-key/content-hash reconciliation helpers."""
from __future__ import annotations
import hashlib, json
from collections import defaultdict
from typing import Any, Iterable

def content_hash(record: dict[str, Any]) -> str:
    def normalize(value: Any) -> Any:
        if isinstance(value, (int, float)) and not isinstance(value, bool): return round(float(value), 8)
        if isinstance(value, dict): return {key: normalize(item) for key, item in value.items()}
        if isinstance(value, list): return [normalize(item) for item in value]
        return value
    payload=normalize({key:value for key,value in record.items() if key not in {'key','source'}})
    canonical=json.dumps(payload,ensure_ascii=False,sort_keys=True,separators=(',',':'),default=str)
    return hashlib.sha256(canonical.encode('utf-8')).hexdigest()

def reconcile_records(source: Iterable[dict[str, Any]], existing: Iterable[dict[str, Any]]) -> list[dict[str, Any]]:
    existing_by_key: dict[str,list[dict[str,Any]]]=defaultdict(list)
    for row in existing: existing_by_key[str(row['key'])].append(row)
    results=[]
    for row in source:
        key=str(row['key']);matches=existing_by_key.get(key,[]);source_hash=content_hash(row)
        classification=('insert_candidate' if not matches else 'duplicate_existing_key' if len(matches)>1 else 'existing_same' if content_hash(matches[0])==source_hash else 'same_key_different_content')
        results.append({'key':key,'classification':classification,'sourceHash':source_hash,'existingHashes':[content_hash(x) for x in matches],'source':row.get('source'),'sourceRecord':row,'existingRecords':matches})
    return results
