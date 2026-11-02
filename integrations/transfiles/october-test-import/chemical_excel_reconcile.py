"""Read-only, restricted Excel arithmetic translation and independent Decimal check.

This module never publishes formulas, activates F tables or imports measurements.
Only a same-row primary input and explicit local F cells are accepted.
"""
import ast
import re
from decimal import Decimal, InvalidOperation, localcontext

F_REF=re.compile(r"(?:'F'|F)!\$?([A-Z]{1,3})\$?(\d+)",re.I)

def number(value):
 try:
  result=Decimal(str(value))
  if not result.is_finite():raise InvalidOperation()
  return result
 except (InvalidOperation,ValueError):raise ValueError('原始值／F／結果必須是有限數值。') from None

def convert_formula(formula,row,column='H',secondary_column=None):
 expression=str(formula or '').strip()
 if not expression.startswith('='):raise ValueError('缺少Excel原始公式。')
 expression=expression[1:]
 ref=rf'\$?{column}\$?{row}'
 guard=ref if secondary_column is None else rf'(?:{ref}|\$?{secondary_column}\$?{row})'
 wrapper=re.fullmatch(rf'IF\(\s*{guard}\s*=\s*""\s*,\s*""\s*,(.*)\)',expression,re.I)
 if wrapper:expression=wrapper.group(1)
 expression=re.sub(rf'(?<![A-Z0-9_]){ref}(?!\d)','Primary',expression,flags=re.I)
 if secondary_column is not None:
  expression=re.sub(rf'(?<![A-Z0-9_])\$?{secondary_column}\$?{row}(?!\d)','Secondary',expression,flags=re.I)
 refs=sorted({m.group(1).upper()+m.group(2) for m in F_REF.finditer(expression)})
 expression=F_REF.sub(lambda m:'F!$'+m.group(1).upper()+'$'+m.group(2),expression)
 # Parse and validate even if the input is zero; no Excel functions or arbitrary code.
 _tree(expression)
 return expression,refs

def _tree(expression):
 safe=F_REF.sub(lambda m:'F_'+m.group(1).upper()+m.group(2),expression)
 try:tree=ast.parse(safe.strip(),mode='eval')
 except (SyntaxError,ValueError):raise ValueError('不支援的公式；只接受同列原始值、F表及四則運算。') from None
 for node in ast.walk(tree):
  if isinstance(node,(ast.Expression,ast.Load,ast.BinOp,ast.UnaryOp,ast.Add,ast.Sub,ast.Mult,ast.Div,ast.UAdd,ast.USub)):continue
  if isinstance(node,ast.Constant) and type(node.value) in (int,float):continue
  if isinstance(node,ast.Name) and (node.id in ('Primary','Secondary') or re.fullmatch(r'F_[A-Z]{1,3}\d+',node.id)):continue
  raise ValueError('公式含跨列／其他欄位／外部引用或不支援函數。')
 return tree,safe

def formula_key(expression):
 return ast.dump(_tree(expression)[0],include_attributes=False)

def recheck_column(headers):
 columns=[index+1 for index,value in enumerate(headers) if value and '滴定' in str(value)]
 if len(columns)!=2 or columns[0]!=8:raise ValueError('滴定初複驗欄位缺漏或不唯一。')
 return columns[1]

def calculate(expression,primary,f_values,secondary=None):
 tree,safe=_tree(expression)
 def walk(node):
  if isinstance(node,ast.Expression):return walk(node.body)
  if isinstance(node,ast.Constant):return number(ast.get_source_segment(safe,node))
  if isinstance(node,ast.Name):
   if node.id=='Primary':return number(primary)
   if node.id=='Secondary':return number(secondary)
   if node.id[2:] not in f_values:raise ValueError('缺少F表儲存格：'+node.id[2:])
   return number(f_values[node.id[2:]])
  if isinstance(node,ast.UnaryOp):return -walk(node.operand) if isinstance(node.op,ast.USub) else walk(node.operand)
  left,right=walk(node.left),walk(node.right)
  if isinstance(node.op,ast.Add):return left+right
  if isinstance(node.op,ast.Sub):return left-right
  if isinstance(node.op,ast.Mult):return left*right
  if not right:raise ValueError('公式除以0。')
  return left/right
 with localcontext() as context:
  context.prec=40
  return walk(tree)
