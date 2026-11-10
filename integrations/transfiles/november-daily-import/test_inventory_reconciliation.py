import unittest
from inventory_reconciliation import content_hash,reconcile_records

class Tests(unittest.TestCase):
    def test_classes(self):
        self.assertEqual(reconcile_records([{'key':'A','value':1}],[])[0]['classification'],'insert_candidate')
        self.assertEqual(reconcile_records([{'key':'A','value':1}],[{'key':'A','value':1.0}])[0]['classification'],'existing_same')
        self.assertEqual(reconcile_records([{'key':'A','value':1}],[{'key':'A','value':2}])[0]['classification'],'same_key_different_content')
        self.assertEqual(reconcile_records([{'key':'A','value':1}],[{'key':'A','value':1},{'key':'A','value':1}])[0]['classification'],'duplicate_existing_key')
    def test_float_tail_and_dictionary_order(self):
        self.assertEqual(content_hash({'b':2,'a':19.936000000000003}),content_hash({'a':19.936,'b':2.0}))

if __name__=='__main__':unittest.main()
