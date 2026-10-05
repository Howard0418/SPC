"""Offline deployment review gate. Never connects, migrates, or deploys.

Consumes a reviewed manifest, prints blocking evidence gaps, exits nonzero if
the proposed package is not ready. This is not a substitute for live rechecks.
"""
import argparse
import json
from pathlib import Path

def evaluate(data):
    errors=[]
    expected=data.get('expected_versions',{})
    if not expected:errors.append('MISSING_BASELINE')
    for app,version in expected.items():
        if data.get('actual_versions',{}).get(app)!=version:errors.append('BASELINE_CHANGED:'+app)
    if 'pending_migrations' not in data or 'approved_migrations' not in data:
        errors.append('MISSING_MIGRATION_INVENTORY')
    else:
        errors.extend('UNAPPROVED_MIGRATION:'+x for x in sorted(set(data['pending_migrations'])-set(data['approved_migrations'])))
    for field in ('schema_rehearsed','legacy_data_rehearsed','runtime_reviewed','rollback_ready'):
        if data.get(field) is not True:errors.append('EVIDENCE_REQUIRED:'+field)
    return errors

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest',type=Path)
    args=parser.parse_args()
    failures=evaluate(json.loads(args.manifest.read_text(encoding='utf-8-sig')))
    print(json.dumps({'ready':not failures,'blockers':failures},ensure_ascii=False,indent=2))
    raise SystemExit(2 if failures else 0)
