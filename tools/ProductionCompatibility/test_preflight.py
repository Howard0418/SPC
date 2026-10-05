import unittest
from preflight import evaluate

class PreflightTests(unittest.TestCase):
    def baseline(self):
        return dict(expected_versions={'web':'1.0.209'}, actual_versions={'web':'1.0.209'},
                    pending_migrations=['stage'], approved_migrations=['stage'],
                    schema_rehearsed=True, legacy_data_rehearsed=True,
                    runtime_reviewed=True, rollback_ready=True)
    def test_accepts_only_reviewed_candidate(self):
        self.assertEqual([],evaluate(self.baseline()))
    def test_blocks_unapproved_migration(self):
        data=self.baseline();data['pending_migrations'].append('equipment')
        self.assertIn('UNAPPROVED_MIGRATION:equipment',evaluate(data))
    def test_blocks_changed_production_version(self):
        data=self.baseline();data['actual_versions']['web']='1.0.210'
        self.assertIn('BASELINE_CHANGED:web',evaluate(data))
    def test_requires_schema_data_runtime_and_rollback_evidence(self):
        data=self.baseline()
        for name in ('schema_rehearsed','legacy_data_rehearsed','runtime_reviewed','rollback_ready'):data[name]=False
        self.assertEqual(4,len(evaluate(data)))
    def test_missing_evidence_fails_closed(self):
        self.assertTrue(evaluate({}))
if __name__=='__main__':unittest.main()
