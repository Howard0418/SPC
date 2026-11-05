import json
import tempfile
import unittest
from pathlib import Path
from build_manifest import resolve_source

class SourceRootTests(unittest.TestCase):
 def test_explicit_moved_root_overrides_old_config(self):
  with tempfile.TemporaryDirectory() as tmp:
   root=Path(tmp); source=root/'moved'; source.mkdir()
   (root/'scheduled_import_config.json').write_text(json.dumps({'chemical_source':str(root/'missing')}),encoding='utf-8')
   self.assertEqual(source,resolve_source(root,source))
 def test_default_preserves_configured_root(self):
  with tempfile.TemporaryDirectory() as tmp:
   root=Path(tmp); source=root/'source'; source.mkdir()
   (root/'scheduled_import_config.json').write_text(json.dumps({'chemical_source':str(source)}),encoding='utf-8')
   self.assertEqual(source,resolve_source(root,None))
 def test_missing_root_rejected(self):
  with tempfile.TemporaryDirectory() as tmp:
   with self.assertRaises(FileNotFoundError):resolve_source(Path(tmp),Path(tmp)/'missing')

if __name__=='__main__':unittest.main()
