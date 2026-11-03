"""Isolated etch completeness tests: mocked workbooks/HTTP, temporary source bytes; no live DB or network."""
import copy
import hashlib
import json
import unittest
from datetime import date
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import Mock, patch
from openpyxl.utils import get_column_letter
import etch_reports
from etch_upload import EtchImportClient
from scheduled_import import ScheduledImportConfig, scan_etching_source


def block(line='PT1'):
    start, ops, speedcol = etch_reports.LAYOUT[line]
    values = {etch_reports.line_label_cell(line): {'PT1':'PT-01','PT2':'PT-02','QE1':'QE-01','QE2':'QE-02'}[line]}
    formulas = {}
    for br, ar, er in [(4,14,33),(9,19,38)]:
        for repeat in range(5):
            for op in range(ops):
                b,a,e = [f'{get_column_letter(start+op)}{row+repeat}' for row in (br,ar,er)]
                values.update({b:2.0,a:1.0,e:1.0}); formulas[e] = f'={b}-{a}'
    values[f'{get_column_letter(speedcol)}27'] = 1.5
    values[f'{get_column_letter(speedcol)}28'] = 1.5 / {'PT1':.975,'PT2':.975,'QE1':1.944,'QE2':2.268}[line]
    for row in (43,44): values[f'{get_column_letter(start)}{row}'] = 1.0
    formulas[f'{get_column_letter(start)}43'] = f'=AVERAGE({get_column_letter(start)}33:{get_column_letter(start+ops-1)}37)'
    formulas[f'{get_column_letter(start)}44'] = f'=AVERAGE({get_column_letter(start)}38:{get_column_letter(start+ops-1)}42)'
    return values, formulas


class Book:
    def __init__(self, sheets): self.sheets=sheets; self.sheetnames=list(sheets)
    def __getitem__(self, key): return self.sheets[key]
    def close(self): pass


class Sheet:
    def __init__(self, cells): self.cells=cells
    def iter_rows(self, min_row, max_row, max_col, values_only):
        return [[self.cells.get(f'{get_column_letter(c)}{r}') for c in range(1,max_col+1)] for r in range(min_row,max_row+1)]


def parse_fixture(path, sheets, **kwargs):
    vb=Book({k:Sheet(v) for k,(v,f) in sheets.items()}); fb=Book({k:Sheet(v|f) for k,(v,f) in sheets.items()})
    with patch('etch_reports.openpyxl.load_workbook', side_effect=[fb,vb]):
        return etch_reports.extract_reports(path, today=date(2026,11,3), **kwargs)


def report(line='PT1'):
    n=etch_reports.LAYOUT[line][1]
    return dict(reportDate='2026-10-28',lineCode=line,lineSpeed=1.5,operatorName='fixture',
        points=[dict(side=s,repeatNo=r,opNo=o,beforeValue=2,afterValue=1) for s in ('A','B') for r in range(1,6) for o in range(1,n+1)])


def receipt(r, state='ALREADY_EXISTS', verified=True, stored=None):
    n=etch_reports.LAYOUT[r['lineCode']][1]*5
    return dict(success=True,environment='test',contractVersion=1,data=dict(state=state,
        businessKey=f"ETCH:{r['reportDate']}:{r['lineCode']}",lineCode=r['lineCode'],reportDate=r['reportDate']+'T00:00:00',
        sampleCountPerSide=n,pointCount=n*2+1,storedPointCount=(n*2+1 if state=='ALREADY_EXISTS' else 0) if stored is None else stored,
        verified=verified,batchId='00000000-0000-0000-0000-000000000001',additionalExistingRowCount=0,message='fixture',
        measurementNo=r.get('measurementNo',1),storedMeasurementNo=1 if state=='REPLACE_FIRST' else r.get('measurementNo',1)))


class ParseCompletenessTests(unittest.TestCase):
    def test_all_four_blocks_are_accounted_for_including_empty(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture')
            m=parse_fixture(p, {'1028':block()})
        self.assertEqual(4,len(m['inventory']))
        self.assertEqual({'READY':1,'EMPTY':3},m['coverage']['byStatus'])
        self.assertEqual(4,m['coverage']['totalBlocks'])

    def test_wrong_or_missing_line_label_is_quarantined(self):
        for label in ('QE-01', None, 'PT-02', 'PT1 extra'):
            with self.subTest(label=label), TemporaryDirectory() as d:
                p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block();v['A2']=label
                m=parse_fixture(p,{'1028':(v,f)})
                self.assertEqual([],m['reports']);self.assertEqual(1,len(m['quarantined']))
                self.assertIn('A2',m['quarantined'][0]['reason'])

    def test_explicit_temperature_annotation_retains_the_same_line_and_provenance(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block();v['A2']='PT-01(27度)'
            m=parse_fixture(p,{'1028':(v,f)})
            self.assertEqual(1,len(m['reports']));self.assertEqual('PT-01(27度)',m['reports'][0]['provenance']['sourceLine'])

    def test_second_measurement_label_is_explicit_and_second_source_wins(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block();v['A2']='PT-01-2'
            m=parse_fixture(p,{'1028':(v,f)})
            self.assertEqual(2,m['reports'][0]['measurementNo'])
        first=report();second=copy.deepcopy(first);second['measurementNo']=2;second['points'][0]['afterValue']=.8
        for inputs in ([first,second],[second,first]):
            merged=etch_reports.merge_reports(inputs);self.assertEqual(1,len(merged));self.assertEqual(2,merged[0]['measurementNo'])
            self.assertEqual(1,len(merged[0]['provenance']['supersededReports']))

    def test_x_and_date_scope_are_distinct_and_inclusive(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block();v['A2']='X-PT-01'
            m=parse_fixture(p,{'0930':block(),'1001':block(),'1028':(v,f),'1031':block(),'1101':block()},start_date='2026-10-01',end_date='2026-10-31')
        self.assertEqual(['2026-10-01','2026-10-31'],[x['reportDate'] for x in m['reports']])
        self.assertEqual(1,len(m['excluded']));self.assertEqual(8,m['coverage']['byStatus']['OUT_OF_SCOPE'])

    def test_b_mean_uses_all_samples_and_records_formula_warning(self):
        for line,cell,wrong,right in [('PT1','B44','=AVERAGE(B39:F42)','=AVERAGE(B38:F42)'),('PT2','H44','=AVERAGE(H39:Q42)','=AVERAGE(H38:Q42)')]:
            with self.subTest(line=line), TemporaryDirectory() as d:
                p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block(line);f[cell]=wrong
                m=parse_fixture(p,{'1028':(v,f)})
                self.assertEqual(1,len(m['reports']));self.assertEqual(right,m['warnings'][0]['suggestedFormula'])
                self.assertEqual(1,etch_reports.report_stats(m['reports'][0])['B']['mean'])

    def test_boolean_or_nonfinite_point_cache_is_not_a_measurement(self):
        for bad in (True,float('nan'),float('inf')):
            with self.subTest(bad=bad), TemporaryDirectory() as d:
                p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block();v['B33']=bad
                self.assertEqual([],parse_fixture(p,{'1028':(v,f)})['reports'])

    def test_nonzero_amount_without_paired_inputs_is_quarantined_not_empty(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');v,f=block()
            for row in range(4,24):
                for col in range(2,7):v.pop(f'{get_column_letter(col)}{row}',None)
            m=parse_fixture(p,{'1028':(v,f)})
            self.assertEqual(1,len(m['quarantined']))

    def test_source_precision_is_not_rounded_before_import(self):
        v,f=block();v['B4']=2.123456789;v['B33']=1.123456789
        r=etch_reports.parse_line(v,f,'PT1','hash')
        self.assertEqual(v['B4'],r['points'][0]['beforeValue'])

    def test_cross_file_duplicates_are_accounted_for_and_conflicts_block(self):
        r=report();a=dict(source='a.xlsx',sha256='a',reports=[r],quarantined=[],excluded=[],skipped=[])
        b=dict(a,source='b.xlsx',sha256='b',reports=[copy.deepcopy(r)])
        batch=etch_reports.audit_manifests([a,b]);self.assertEqual(1,len(batch['reports']));self.assertEqual(1,len(batch['duplicates']))
        b['reports'][0]['lineSpeed']=2
        with self.assertRaisesRegex(ValueError,'衝突'):etch_reports.audit_manifests([a,b])

    def test_inventory_detects_lost_ready_report(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');m=parse_fixture(p,{'1028':block()});m['reports']=[]
            with self.assertRaisesRegex(ValueError,'清冊'):etch_reports.audit_manifests([m])

    def test_dry_run_hash_is_not_proof_of_database_import(self):
        with TemporaryDirectory() as d:
            p=Path(d)/'咬蝕量_2026.xlsx';p.write_bytes(b'fixture');r=report();sha=hashlib.sha256(p.read_bytes()).hexdigest()
            state={'sources':{f"{p.resolve()}|{r['reportDate']}|{r['lineCode']}":{'sha256':sha}}}
            cfg=ScheduledImportConfig(etching_source=p)
            manifest=dict(sha256=sha,reports=[r],quarantined=[],excluded=[],skipped=[])
            with patch('scheduled_import.extract_reports',return_value=manifest): items=scan_etching_source(cfg,state)
        self.assertEqual('ready_for_preview',items[0]['status'])


class UploadCompletenessTests(unittest.TestCase):
    def client(self, inspections):
        c=EtchImportClient();c.request=Mock(return_value=dict(portal='new',spc='new'))
        c._spc_request=Mock(side_effect=lambda path,payload=None: {'environment':'test'} if path=='/version' else next(inspections))
        return c

    def test_existing_verified_business_key_never_confirms(self):
        r=report();c=self.client(iter([receipt(r),receipt(r)]));preview=c.preview([r]);c.request.reset_mock()
        result=c.confirm(preview)
        self.assertEqual('SKIPPED_EXISTS',result[0]['status']);c.request.assert_not_called()

    def test_first_failure_accounts_for_unprocessed_reports(self):
        c=self.client(iter([]));c._spc_request=Mock(side_effect=RuntimeError('HTTP 500'))
        p=c.preview([report('PT1'),report('PT2')])
        self.assertEqual(2,len(p));self.assertEqual('NOT_PROCESSED',p[1]['state'])
        with self.assertRaises(ValueError):c.confirm(p)

    def test_success_requires_matching_post_import_receipt(self):
        for bad_count in (50,52):
            with self.subTest(count=bad_count):
                r=report();c=self.client(iter([receipt(r,'NEW',False),receipt(r,stored=bad_count)]))
                p=c.preview([r]);c.request=Mock(return_value=dict(status='SUCCESS',unchanged=False))
                result=c.confirm(p);self.assertEqual('VERIFY_FAILED',result[0]['status'])

    def test_new_report_is_verified_after_confirmation(self):
        r=report('QE2');c=self.client(iter([receipt(r,'NEW',False),receipt(r)]))
        p=c.preview([r]);c.request=Mock(return_value=dict(status='SUCCESS',unchanged=False))
        rows=c.confirm(p);self.assertEqual('SUCCESS',rows[0]['status']);self.assertTrue(rows[0]['verified'])

    def test_second_measurement_replacement_is_explicit_and_verified(self):
        r=report();r['measurementNo']=2;c=self.client(iter([receipt(r,'REPLACE_FIRST',False,51),receipt(r)]))
        p=c.preview([r]);self.assertEqual('REPLACE_FIRST',p[0]['state'])
        c.request=Mock(return_value=dict(status='SUCCESS',unchanged=False))
        self.assertEqual('REPLACED_FIRST',c.confirm(p)[0]['status'])

    def test_wrong_environment_and_wrong_line_receipts_block(self):
        for field,bad in [('environment','production'),('contractVersion',0),('lineCode','PT2')]:
            with self.subTest(field=field):
                r=report();body=receipt(r)
                if field in body:body[field]=bad
                else:body['data'][field]=bad
                c=self.client(iter([body]));p=c.preview([r])
                self.assertIn('error',p[0]);c.request.assert_not_called()

    def test_source_changed_after_preview_blocks_before_confirm(self):
        with TemporaryDirectory() as d:
            source=Path(d)/'source.xlsx';source.write_bytes(b'original');r=report();c=self.client(iter([receipt(r,'NEW',False)]))
            sources=[dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest())]
            p=c.preview([r],sources=sources);source.write_bytes(b'changed');c.request.reset_mock()
            with self.assertRaisesRegex(ValueError,'來源'):c.confirm(p)
            c.request.assert_not_called()

    def test_mixed_environment_urls_are_rejected_before_login(self):
        with self.assertRaises(ValueError):EtchImportClient('https://login.pmr.com.tw:8443/api','https://spctest.pmr.com.tw/api','test')

    def test_second_measurement_receipt_cannot_claim_first_is_authoritative(self):
        r=report();r['measurementNo']=2;body=receipt(r);body['data']['storedMeasurementNo']=1
        c=self.client(iter([body]));self.assertIn('error',c.preview([r])[0])


class GuiCompletenessTests(unittest.TestCase):
    def app(self,path):
        from batch_convert_gui import ConversionGUI
        app=ConversionGUI.__new__(ConversionGUI);app.selected_output_dir=Path(path);app.output_file_paths=[]
        for name in ('log','update_status','enable_controls'):setattr(app,name,Mock())
        app.root=Mock();app.root.after.side_effect=lambda delay,callback:callback()
        app.ask_on_ui_thread=Mock(return_value=True);app.spc_environment_var=Mock(get=Mock(return_value='test'))
        app.etch_import_client=Mock();app.export_scope_var=Mock(get=Mock(return_value='全部'));app.upload_after_conversion=False
        return app

    def manifest(self,path):
        source=Path(path)/'fixture.xlsx';source.write_bytes(b'fixture')
        return dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),reports=[report('PT1'),report('QE1')],
            quarantined=[],excluded=[],skipped=[],warnings=[],inventory=[dict(source=str(source),sheet='1028',reportDate='2026-10-28',lineCode=l,status='READY' if l in ('PT1','QE1') else 'EMPTY') for l in etch_reports.LAYOUT])

    def test_gui_failed_preview_writes_every_day_line_as_pending(self):
        with TemporaryDirectory() as d:
            app=self.app(d);m=self.manifest(d)
            app.etch_import_client.preview.return_value=[dict(report=m['reports'][0],state='FAILED',error='HTTP 500'),dict(report=m['reports'][1],state='NOT_PROCESSED',error='尚未處理')]
            with patch('batch_convert_gui.messagebox.showerror'),self.assertRaises(ValueError):app.import_complete_reports([m],0)
            data=json.loads(next(Path(d).glob('咬蝕匯入結果_*.json')).read_text(encoding='utf-8'))
            self.assertEqual(2,data['accounted']);self.assertFalse(data['eligibleComplete']);app.etch_import_client.confirm.assert_not_called()

    def test_gui_missing_result_cannot_be_complete(self):
        with TemporaryDirectory() as d:
            app=self.app(d);m=self.manifest(d)
            app.etch_import_client.preview.return_value=[dict(report=r,state='READY') for r in m['reports']]
            app.etch_import_client.confirm.return_value=[dict(reportDate='2026-10-28',lineCode='PT1',status='SUCCESS',verified=True)]
            with patch('batch_convert_gui.messagebox.showinfo'):app.import_complete_reports([m],0)
            self.assertIn('未完整',app.update_status.call_args.args[0])
            data=json.loads(next(Path(d).glob('咬蝕匯入結果_*.json')).read_text(encoding='utf-8'));self.assertEqual(1,data['counts']['NOT_PROCESSED'])

    def test_gui_forwards_explicit_workbook_date_scope(self):
        with TemporaryDirectory() as d:
            app=self.app(d);m=self.manifest(d);app.etch_scope_snapshot=('2026-10-01','2026-10-31')
            with patch('batch_convert_gui.extract_reports',return_value=m) as parse,patch('batch_convert_gui.export_reports',return_value=([Path(d)/'report.json'],2,0)),patch('batch_convert_gui.messagebox.showinfo'):
                app.run_complete_etch_reports([Path(m['source'])])
            parse.assert_called_once_with(Path(m['source']),start_date='2026-10-01',end_date='2026-10-31')


if __name__=='__main__': unittest.main()
