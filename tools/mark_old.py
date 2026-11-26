#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把除最新版以外的 release 标题加上「[旧版本]」前缀，保留历史版本可下载。

用法：python tools/mark_old.py <token> <保留的 tag>
"""
import json
import sys
import urllib.error
import urllib.request

API = "https://api.github.com"
REPO = "jzszlxx/flatcat"


def req(path, token, method="GET", body=None):
    r = urllib.request.Request(API + path, method=method)
    r.add_header("Accept", "application/vnd.github+json")
    r.add_header("Authorization", "Bearer " + token)
    r.add_header("X-GitHub-Api-Version", "2022-11-28")
    r.add_header("User-Agent", "FlatCat-mark-old")
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        r.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(r, data=data, timeout=60) as resp:
            text = resp.read().decode("utf-8")
            return resp.status, (json.loads(text) if text.strip() else {})
    except urllib.error.HTTPError as e:
        return e.code, {"message": e.read().decode("utf-8", "replace")[:200]}


def main():
    if len(sys.argv) < 3:
        print("用法：python tools/mark_old.py <token> <保留的 tag>")
        return 2
    token, keep = sys.argv[1].strip(), sys.argv[2].strip()
    code, rels = req("/repos/%s/releases?per_page=50" % REPO, token)
    if code != 200 or not isinstance(rels, list):
        print("读取 release 失败：", code, rels.get("message"))
        return 3
    for r in rels:
        tag, name = r.get("tag_name", ""), (r.get("name") or "")
        if tag == keep:
            print("跳过最新版：%s" % tag)
            continue
        if name.startswith("[旧版本]"):
            print("已是旧版：%s" % tag)
            continue
        code, out = req("/repos/%s/releases/%s" % (REPO, r["id"]), token, "PATCH",
                        {"name": "[旧版本] " + name})
        if code == 200:
            print("已标旧版：%s → [旧版本] %s" % (tag, name))
        else:
            print("标记失败 %s：%s %s" % (tag, code, out.get("message")))
            return 4
    return 0


if __name__ == "__main__":
    sys.exit(main())