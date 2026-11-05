"""Approved September/October source identity and revision precedence only.

No database access. These rules do not waive date, time, formula, or F validation.
"""
import re

ORDER={
 '2026-09-30':('2026-09-30.xlsx','2026-09-30更槽.xlsx'),
 '2026-10-06':('2026-10-06.xlsx','2026-10-06-更槽.xlsx'),
 '2026-10-27':('2026-10-27.xlsx','2026-10-27-更槽 PT2.xlsx'),
}

def authoritative_line(day,sheet,raw_line):
 normalized=re.sub(r'\s+','',sheet).replace('（','(').replace('）',')')
 return 'N1' if day=='2026-09-02' and normalized=='N1(開)' else raw_line

def select_file(day,file_names):
 files=set(file_names)
 if not files:raise ValueError('No source file')
 if len(files)==1:return next(iter(files))
 ordered=ORDER.get(day)
 if ordered is None or not files.issubset(ordered):raise ValueError('Source order not approved')
 return next(name for name in reversed(ordered) if name in files)
