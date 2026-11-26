# -*- coding: utf-8 -*-
"""打发布包：README/LICENSE/build.sh/exe + src + res + tools，zip 到 out/FlatCat-<ver>.zip"""
import os
import shutil
import sys
import zipfile

VER = sys.argv[1] if len(sys.argv) > 1 else "1.5"
BASE = os.path.abspath(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PKG = os.path.join(BASE, "out", "pkg" + VER.replace(".", ""))

if os.path.isdir(PKG):
    shutil.rmtree(PKG)
for d in ("src", "res", "tools"):
    os.makedirs(os.path.join(PKG, d))

def cp(rel, dest=None):
    src = os.path.join(BASE, rel.replace("/", os.sep))
    dst = os.path.join(PKG, (dest or rel).replace("/", os.sep))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copy2(src, dst)

for f in ("README.md", "LICENSE", "build.sh"):
    cp(f)
cp("out/FlatCat.exe", "FlatCat.exe")
for f in sorted(os.listdir(os.path.join(BASE, "src"))):
    if f.endswith(".cs"):
        cp("src/" + f)
for f in sorted(os.listdir(os.path.join(BASE, "res"))):
    cp("res/" + f)
for f in ("gen_assets.py", "verify.py", "grabwin.py"):
    cp("tools/" + f)

# dist 是仓库里跟踪的成品位置，同步一份
shutil.copy2(os.path.join(BASE, "out", "FlatCat.exe"),
             os.path.join(BASE, "dist", "FlatCat.exe"))

zpath = os.path.join(BASE, "out", "FlatCat-" + VER + ".zip")
if os.path.exists(zpath):
    os.remove(zpath)
z = zipfile.ZipFile(zpath, "w", zipfile.ZIP_DEFLATED)
count = 0
for dp, dn, fs in os.walk(PKG):
    for f in sorted(fs):
        p = os.path.join(dp, f)
        z.write(p, os.path.relpath(p, PKG).replace(os.sep, "/"))
        count += 1
z.close()
print("packed %d files -> %s (%d bytes)" % (count, zpath, os.path.getsize(zpath)))