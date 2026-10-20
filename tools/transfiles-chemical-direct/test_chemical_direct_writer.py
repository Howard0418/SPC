import unittest
from chemical_direct_writer import parse_row, preview_rows, confirm_rows
class Tests(unittest.TestCase):
 def row(self,**changes):
  x=dict(PpcId="1",MeasuredAt="2026-10-18T23:59:00+08:00",Primary="1.23456789",SamplingPhase="OPEN",SamplingStage="GENERAL");x.update(changes);return x
 def test_original_and_date(self):
  row=parse_row(self.row(),"a"*64,"Raw",2);self.assertEqual(row["input"]["primary"],"1.23456789");self.assertIn("+08:00",row["input"]["measuredAt"])
 def test_invalid(self):
  for changes in [dict(Primary="NaN"),dict(Primary=""),dict(MeasuredAt="2026-02-30"),dict(MeasuredAt="2026-10-18"),dict(PpcId="0"),dict(SamplingPhase="BAD")]:
   with self.subTest(changes=changes),self.assertRaises(ValueError):parse_row(self.row(**changes),"a"*64,"Raw",2)
 def test_preview_no_write_confirm_independent_rows(self):
  calls=[]
  def api(path,body):
   calls.append(path)
   if path.endswith("prepare"):return dict(receiptId="receipt")
   return dict(token="preview",result=dict(Stored="2.47"))
  rows=[parse_row(self.row(),"a"*64,"Raw",2),parse_row(self.row(MeasuredAt="2026-10-19T00:00:00+08:00",Primary="3"),"a"*64,"Raw",3)]
  previews=preview_rows(rows,api);self.assertTrue(all(x.endswith("preview") for x in calls));db=[]
  result=confirm_rows(previews,api,lambda a,b:(db.append((a,b)) or dict(statisticsStatus="Pending")))
  self.assertEqual(len(db),2);self.assertEqual(result[0]["statisticsStatus"],"Pending")
 def test_repeated_receipt_status_uses_committed_receipt(self):
  def api(path,body):return dict(token="x",result={},receiptId="new-unconsumed")
  previews=preview_rows([parse_row(self.row(),"a"*64,"Raw",2)],api)
  result=confirm_rows(previews,api,lambda a,b:dict(receiptId="old-committed",alreadyProcessed=True))
  self.assertEqual(result[0]["initialReceiptId"],"old-committed")
 def test_db_failure_isolated_no_api_import(self):
  def api(path,body):return dict(token="x",result={},receiptId="r")
  preview=preview_rows([parse_row(self.row(),"a"*64,"Raw",2)],api)
  def fail(a,b):raise RuntimeError("safe")
  result=confirm_rows(preview,api,fail);self.assertIn("error",result[0])
if __name__=="__main__":unittest.main()
