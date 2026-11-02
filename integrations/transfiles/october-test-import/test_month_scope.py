import unittest
from datetime import date
from build_manifest import month_bounds, in_month

class Tests(unittest.TestCase):
 def test_september_boundaries(self):
  self.assertEqual(month_bounds('2026-09'),(date(2026,9,1),date(2026,9,30)))
  for day in [date(2026,9,1),date(2026,9,30)]:self.assertTrue(in_month(day,'2026-09'))
  for day in [date(2026,8,31),date(2026,10,1)]:self.assertFalse(in_month(day,'2026-09'))
 def test_october_unchanged(self):
  self.assertEqual(month_bounds('2026-10'),(date(2026,10,1),date(2026,10,31)))
  self.assertFalse(in_month(date(2026,9,30),'2026-10'))
 def test_unapproved_month_rejected(self):
  for month in ['2026-11','2025-09','2026-9','../2026-09','']:
   with self.assertRaises(ValueError):month_bounds(month)

if __name__=='__main__':unittest.main()
