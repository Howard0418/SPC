import datetime
import hashlib
import os
import shutil
import subprocess
import urllib.request
from pathlib import Path

stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
target = Path(r"D:\SPC\release\test\frontend")
if not (target / "index.html").exists() or not (target / "web.config").exists():
    raise SystemExit("unexpected test frontend directory")
# appcmd needs elevation in this session; directory is the known SpcWeb test path.

webconfig = target / "web.config"
if not webconfig.exists():
    raise SystemExit("missing web.config")
web_hash = hashlib.sha256(webconfig.read_bytes()).hexdigest()
src = Path(r"D:\SPC\frontend\mes-spc-web\dist")
if not (src / "index.html").exists():
    raise SystemExit("missing dist/index.html")

stage = Path(r"D:\SPC\release-staging") / ("calibration-ui-colors-" + stamp) / "frontend"
if stage.exists():
    raise SystemExit("stage exists")
stage.mkdir(parents=True)
for p in src.rglob("*"):
    if p.is_file():
        dest = stage / p.relative_to(src)
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(p, dest)

backup = Path(r"D:\SPC\release\test") / ("frontend.backup-" + stamp)
if backup.exists():
    raise SystemExit("backup exists")
shutil.copytree(target, backup, ignore=lambda d, names: ["logs"] if "logs" in names else [])

copied = 0
for p in stage.rglob("*"):
    if not p.is_file() or p.name.lower() == "web.config":
        continue
    dest = target / p.relative_to(stage)
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, dest)
    copied += 1

new_hash = hashlib.sha256(webconfig.read_bytes()).hexdigest()
if new_hash != web_hash:
    raise SystemExit("web.config changed")
index = (target / "index.html").read_text(encoding="utf-8", errors="ignore")
if "index-Db_91rql.js" not in index:
    raise SystemExit("new bundle not in index.html")
status = urllib.request.urlopen("http://172.16.110.27:8083/", timeout=15).status
print("STAMP=" + stamp)
print("BACKUP=" + str(backup))
print("COPIED=" + str(copied))
print("WEB_OK=" + str(status))
