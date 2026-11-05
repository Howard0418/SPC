import unittest
from approved_lines import build_approved_index, resolve_line
class ApprovedLinesTests(unittest.TestCase):
 def test_approved_column_overrides_original_and_sheet(self):
  idx=build_approved_index([{'file':'a','sheet':'N1 (收)','cell':'J7','original':'N1','approved':'N2'}],{'N1','N2'});self.assertEqual(idx[('a','N1 (收)','J7')],'N2')
 def test_blank_rejected(self):
  with self.assertRaises(ValueError):build_approved_index([dict(file='a',sheet='N1',cell='J7',approved='')],{'N1'})
 def test_unknown_rejected(self):
  with self.assertRaises(ValueError):build_approved_index([dict(file='a',sheet='N1',cell='J7',approved='N9')],{'N1'})
 def test_conflicting_source_key_rejected(self):
  with self.assertRaises(ValueError):build_approved_index([dict(file='a',sheet='N1',cell='J7',approved=x) for x in ['N1','N2']],{'N1','N2'})
 def test_identical_duplicate_is_idempotent(self):
  r=dict(file='a',sheet='N1',cell='J7',approved='N1');self.assertEqual(len(build_approved_index([r,r],{'N1'})),1)
 def test_resolution_uses_exact_approved_key_before_source_header(self):
  self.assertEqual(resolve_line({('a','N1 (收)','J7'):'N2'},'a','N1 (收)','J7','N1'),'N2')
 def test_unapproved_source_keeps_its_original_line(self):
  self.assertEqual(resolve_line({('a','N1 (收)','J7'):'N2'},'b','N1 (收)','J7','N1'),'N1')
