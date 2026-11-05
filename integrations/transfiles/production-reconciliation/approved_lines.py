"""Exact source-key contract for a user-approved worksheet line column."""
def build_approved_index(rows,known_lines):
 result={}
 for row in rows:
  key=(row['file'],row['sheet'],row['cell']);line=str(row.get('approved') or '').strip().upper()
  if not all(key) or not line or line not in known_lines:raise ValueError('Missing source key or unknown/blank approved line')
  if key in result and result[key]!=line:raise ValueError('Conflicting approved lines for the same source cell')
  result[key]=line
 return result

def resolve_line(index,file,sheet,cell,original):
 """Only an exact approved source key can override its original line."""
 return index.get((file,sheet,cell),original)
