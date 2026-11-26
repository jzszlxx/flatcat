#!/usr/bin/env bash
# 用系统自带的 .NET Framework 4.8 编译器构建，无需安装 SDK / MSBuild
set -e
cd "$(dirname "$0")"

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
if [ ! -f "$CSC" ]; then
  echo "找不到 csc.exe：$CSC"
  exit 1
fi

# csc 是 Windows 程序：先切到 src 目录、用纯文件名传源文件，
# 否则 MSYS 的路径转换会吃掉路径中间的目录段。
ROOT="$(cygpath -m "$PWD")"
export MSYS2_ARG_CONV_EXCL="*"
mkdir -p out

cd src

"$CSC" \
  /nologo \
  /target:winexe \
  /platform:x64 \
  /optimize+ \
  /codepage:65001 \
  "/out:$ROOT/out/FlatCat.exe" \
  "/win32icon:$ROOT/res/flatcat.ico" \
  "/resource:$ROOT/res/purr.wav,purr.wav" \
  "/resource:$ROOT/res/catmeow.wav,catmeow.wav" \
  /r:System.dll \
  /r:System.Core.dll \
  /r:System.Drawing.dll \
  /r:System.Windows.Forms.dll \
  /r:System.Runtime.Serialization.dll \
  "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll" \
  "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" \
  Program.cs PetForm.cs CatPainter.cs Core.cs SettingsForm.cs Capture.cs Media.cs Anim.cs QuoteBank.cs QuoteIo.cs

echo "构建完成: $ROOT/out/FlatCat.exe"
ls -l "$ROOT/out/FlatCat.exe"