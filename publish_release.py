#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""为 FlatCat 建 GitHub Release 并上传资产（本机 github.com 被阻断，只走 api/uploads）。

用法：
    python publish_release.py <token> [tag] [repo]
    set GITHUB_TOKEN=ghp_xxx && python publish_release.py

已存在同名 tag / release 时复用并覆盖同名资产。
"""
import json
import os
import sys
import urllib.error
import urllib.request

API = "https://api.github.com"
UPLOAD = "https://uploads.github.com"
DEFAULT_REPO = "jzszlxx/-"
DEFAULT_TAG = "v1.4.0"
TITLE = "FlatCat 1.4 · 截图 + 电台/本地音乐"
ASSETS = ["out/FlatCat.exe", "out/FlatCat-1.4.zip"]

BODY = """## FlatCat 1.4

一只会跟着你敲键盘抬爪的 2D 桌面宠物。透明置顶、点击穿透、不抢焦点，双击就能跑，不装运行时。

### 这一版新增

- **截图**：全屏 / 矩形选区 / 不规则套索三种。可以勾选「截图时隐藏小猫」，截之前先把自己藏起来，图里就没有它
- 截图存 PNG，文件名带时间戳不覆盖旧图，同时复制进剪贴板；默认放在 `图片\\FlatCat 截图`
- **电台**：5 个预置频道可直接播（AsiaFM 亚洲热歌台、1766 线上电台、Anison 动漫音乐台、台中古典音乐台、古典音乐台 FM 97.7），可新增自定义电台并持久化，音量 25/50/75/100%
- **本地音乐**：多选文件或整个文件夹（递归收录，最多 500 首），上一首 / 下一首 / 停止，一首放完自动接下一首
- 设置窗口扩到六页：外观 / 行为 / 提醒 / 系统 / 截图 / 媒体

### 修复

- 老配置文件缺少 v1.4 字段时，音量等设置会读成 0：加了 `v14init` 一次性迁移，补回出厂默认

### 已知限制

- 内置播放器只认 `http://` 流；`https://` 流在部分机器上会让 MediaPlayer 内部抛错，这类地址请用系统播放器
- 截图在锁屏、纯远程桌面等非交互会话里会是黑图，属系统限制
- 电台流来自第三方，可能失效；可在「媒体」页删除或换源

### 下载

- `FlatCat.exe`（约 217 KB，免安装，双击即用）
- `FlatCat-1.4.zip`（含 exe、源码、构建脚本、取证脚本）

MIT License · © 2026 认知觉醒能量站"""


def req(url, token, method="GET", body=None, ctype="application/json", raw=None):
    if not url.startswith("http"):
        url = API + url
    data = raw
    r = urllib.request.Request(url, method=method, data=data)
    r.add_header("Accept", "application/vnd.github+json")
    r.add_header("Authorization", "Bearer " + token)
    r.add_header("X-GitHub-Api-Version", "2022-11-28")
    r.add_header("User-Agent", "FlatCat-release")
    if ctype:
        r.add_header("Content-Type", ctype)
    try:
        with urllib.request.urlopen(r, timeout=180) as resp:
            text = resp.read().decode("utf-8", "replace")
            return resp.status, (json.loads(text) if text.strip() else {})
    except urllib.error.HTTPError as e:
        text = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(text)
        except Exception:
            return e.code, {"message": text[:300]}


def main():
    token = sys.argv[1].strip() if len(sys.argv) >= 2 and sys.argv[1].strip() else \
        os.environ.get("GITHUB_TOKEN", "").strip()
    tag = sys.argv[2].strip() if len(sys.argv) >= 3 and sys.argv[2].strip() else DEFAULT_TAG
    repo = sys.argv[3].strip() if len(sys.argv) >= 4 and sys.argv[3].strip() else DEFAULT_REPO
    if not token:
        print("缺少 token")
        return 2

    root = os.path.dirname(os.path.abspath(__file__))
    code, me = req("/user", token)
    if code != 200:
        print("token 无效：", code, me.get("message"))
        return 3
    print("已认证：%s" % me["login"])

    code, info = req("/repos/" + repo, token)
    if code != 200:
        print("仓库不可用：", code, info.get("message"))
        return 4
    default = info.get("default_branch") or "main"

    # 已存在则复用
    rel = None
    code, existing = req("/repos/%s/releases/tags/%s" % (repo, tag), token)
    if code == 200:
        rel = existing
        print("复用已有 release：%s（id=%s）" % (tag, rel["id"]))
        code, rel = req("/repos/%s/releases/%s" % (repo, rel["id"]), token, "PATCH",
                        json.dumps({"name": TITLE, "body": BODY, "draft": False,
                                    "prerelease": False}).encode())
        if code != 200:
            print("更新 release 失败：", code, rel.get("message"))
            return 6
    else:
        body = {"tag_name": tag, "target_commitish": default, "name": TITLE,
                "body": BODY, "draft": False, "prerelease": False}
        code, rel = req("/repos/%s/releases" % repo, token, "POST",
                        json.dumps(body).encode())
        if code not in (200, 201):
            print("创建 release 失败：", code, rel.get("message"))
            return 5
        print("release 已建：%s（id=%s）" % (tag, rel["id"]))

    # 删除同名旧资产，避免重复
    code, assets = req("/repos/%s/releases/%s/assets" % (repo, rel["id"]), token)
    if code == 200 and isinstance(assets, list):
        for a in assets:
            req("/repos/%s/releases/assets/%s" % (repo, a["id"]), token, "DELETE")
            print("  删除旧资产：%s" % a["name"])

    for rel_path in ASSETS:
        path = os.path.join(root, rel_path)
        if not os.path.isfile(path):
            print("缺少文件：", rel_path)
            return 7
        name = os.path.basename(path)
        with open(path, "rb") as f:
            data = f.read()
        url = "%s/repos/%s/releases/%s/assets?name=%s" % (UPLOAD, repo, rel["id"], name)
        code, up = req(url, token, "POST", raw=data, ctype="application/octet-stream")
        if code not in (200, 201):
            print("上传失败 %s：%s %s" % (name, code, up.get("message")))
            return 8
        print("  已上传 %-20s %d 字节  %s" % (name, len(data), up.get("browser_download_url", "")))

    code, final = req("/repos/%s/releases/latest" % repo, token)
    if code == 200:
        print("最新 release：%s / %s" % (final.get("tag_name"), final.get("name")))
        print("地址：%s" % final.get("html_url"))
    return 0


if __name__ == "__main__":
    sys.exit(main())