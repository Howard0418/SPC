import unittest
from source_precedence import authoritative_line,select_file

class SourcePrecedenceTests(unittest.TestCase):
 def test_n1_open_approved_override(self):self.assertEqual(authoritative_line('2026-09-02','N1 (開)','N2'),'N1')
 def test_no_expansion_to_close_or_other_day(self):
  self.assertEqual(authoritative_line('2026-09-02','N1 (收)','N2'),'N2');self.assertEqual(authoritative_line('2026-09-03','N1 (開)','N2'),'N2')
 def test_n2_unchanged(self):self.assertEqual(authoritative_line('2026-09-02','N2 (開)','N2'),'N2')
 def test_approved_later_file_selected(self):self.assertEqual(select_file('2026-10-27',['2026-10-27.xlsx','2026-10-27-更槽 PT2.xlsx']),'2026-10-27-更槽 PT2.xlsx')
 def test_unknown_order_rejected(self):
  with self.assertRaises(ValueError):select_file('2026-10-28',['2026-10-28.xlsx','2026-10-28修訂.xlsx'])
 def test_unknown_extra_file_rejected(self):
  with self.assertRaises(ValueError):select_file('2026-10-27',['2026-10-27.xlsx','2026-10-27-更槽 PT2.xlsx','other.xlsx'])
 def test_single_file_preserved(self):self.assertEqual(select_file('2026-09-02',['2026-09-02.xlsx']),'2026-09-02.xlsx')
 def test_empty_rejected(self):
  with self.assertRaises(ValueError):select_file('2026-09-02',[])
if __name__=='__main__':unittest.main()
