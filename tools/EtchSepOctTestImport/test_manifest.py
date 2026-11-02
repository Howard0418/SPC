import unittest
from select_manifest import select_months

class Tests(unittest.TestCase):
 def test_boundaries_and_outside(self):
  m={'reports':[{'reportDate':d,'lineCode':'PT1'}for d in ['2026-08-31','2026-09-01','2026-09-30','2026-10-01','2026-10-31','2026-11-01']]}
  self.assertEqual([r['reportDate']for r in select_months(m)['reports']],['2026-09-01','2026-09-30','2026-10-01','2026-10-31'])
 def test_duplicate_rejected(self):
  with self.assertRaises(ValueError):select_months({'reports':[{'reportDate':'2026-09-01','lineCode':'PT1'}]*2})
 def test_invalid_date_rejected(self):
  with self.assertRaises(ValueError):select_months({'reports':[{'reportDate':'2026-09-31','lineCode':'PT1'}]})
 def test_no_candidates_rejected(self):
  with self.assertRaises(ValueError):select_months({'reports':[]})
 def test_preserve_exclusions_and_source(self):
  m={'source':'original.xlsx','reports':[{'reportDate':'2026-09-01','lineCode':'PT1'}],'quarantined':[{'sheet':'0902'},{'sheet':'0831'}],'excluded':[{'sheet':'1001'}]}
  r=select_months(m);self.assertEqual(r['source'],m['source']);self.assertEqual(len(r['quarantined']),1);self.assertEqual(len(m['quarantined']),2)

if __name__=='__main__':unittest.main()
