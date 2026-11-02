import unittest
from decimal import Decimal
from chemical_excel_reconcile import convert_formula, calculate

class Tests(unittest.TestCase):
 def test_explicit_secondary_same_row(self):
  expression,refs=convert_formula('=IF(I19="","",(H19-I19)*2.65)',19,secondary_column='I')
  self.assertEqual(expression,'(Primary-Secondary)*2.65')
  self.assertEqual(calculate(expression,'19.9',{},secondary='12.9'),Decimal('18.550'))
  with self.assertRaises(ValueError):calculate(expression,'19.9',{})
 def test_parentheses_equivalent_and_recheck_header(self):
  from chemical_excel_reconcile import formula_key,recheck_column
  self.assertEqual(formula_key('Primary*8.7*F!B3'),formula_key('(Primary)*8.7*F!$B$3'))
  self.assertEqual(recheck_column(['']*7+['滴定數(ml)']+['']*4+['滴定數(ml)']),13)
  self.assertEqual(recheck_column(['']*7+['滴定數(ml)']+['']*5+['滴定數(ml)']),14)
 def test_if_f_and_zero(self):
  formula,refs=convert_formula('=IF(H7="","",24.5*H7*F!$B$3)',7)
  self.assertEqual(formula,'24.5*Primary*F!$B$3');self.assertEqual(refs,['B3'])
  self.assertEqual(calculate(formula,'2.4',{'B3':'0.99'}),Decimal('58.212'))
  self.assertEqual(calculate(formula,'0',{'B3':'0.99'}),0)
 def test_recheck(self):
  formula,refs=convert_formula('=IF(M7="","",M7*2)',7,'M')
  self.assertEqual(calculate(formula,'3',{}),6)
 def test_reject_unsafe_or_ambiguous(self):
  for formula in ['=H8*2','=SUM(H7:H8)','=H7+I7','=H7/0',"=H7*'[other.xlsx]F'!B3"]:
   with self.subTest(formula=formula),self.assertRaises(ValueError):
    expression,_=convert_formula(formula,7);calculate(expression,'1',{})
 def test_missing_f_invalid_input(self):
  for primary,values in [('NaN',{'B3':1}),('1',{}),('',{'B3':1})]:
   with self.assertRaises(ValueError):calculate('Primary*F!B3',primary,values)
 def test_multiple_f_and_precision(self):
  expression,refs=convert_formula('=H7*(F!B3+F!B4)/3',7)
  self.assertEqual(refs,['B3','B4'])
  self.assertEqual(calculate(expression,'0.3',{'B3':'0.1','B4':'0.2'}),Decimal('0.03'))

if __name__=='__main__':unittest.main()
