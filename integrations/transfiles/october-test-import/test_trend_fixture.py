import copy
import unittest
from build_trend_manifest import select_fixture

class Tests(unittest.TestCase):
 def row(self, **kwargs):
  return dict(status='validated',line='DV',slot='顯影',analysisItem='碳酸鈉',expression='Primary*F!B3',fCells={'B3':'0.99'},lower='0.5',upper='2',file='source.xlsx',fileDay='2026-09-01',**kwargs)
 def manifest(self):
  return dict(summary={'month':'2026-09'},files=[{'file':'source.xlsx','sha256':'a'*64}],rows=[self.row(recheckPrimary='1')])
 def test_only_validated_explicit_group(self):
  m=self.manifest();m['rows'] += [dict(self.row(),status='blocked'),dict(self.row(),line='N1')];before=copy.deepcopy(m)
  result=select_fixture(m);self.assertEqual(len(result['rows']),1);self.assertEqual(m,before);self.assertEqual(result['summary']['purpose'],'trend-ui')
 def test_multiple_formula_groups_rejected(self):
  m=self.manifest();m['rows'].append(dict(self.row(),expression='Primary*2'))
  with self.assertRaises(ValueError):select_fixture(m)
 def test_wrong_month_rejected(self):
  m=self.manifest();m['summary']['month']='2026-10'
  with self.assertRaises(ValueError):select_fixture(m)
 def test_missing_recheck_or_f_rejected(self):
  for key in ['recheckPrimary','fCells']:
   m=self.manifest();m['rows'][0].pop(key)
   with self.assertRaises(ValueError):select_fixture(m)
 def test_count_bound_and_date(self):
  m=self.manifest();m['rows']*=32
  with self.assertRaises(ValueError):select_fixture(m)
  m=self.manifest();m['rows'][0]['fileDay']='2026-10-01'
  with self.assertRaises(ValueError):select_fixture(m)

if __name__=='__main__':unittest.main()
