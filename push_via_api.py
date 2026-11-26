#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""通过 GitHub REST / Git Data API 把 FlatCat 工程写进已有仓库。

适用于本机 github.com 被阻断、只有 api.github.com 可通的情况：
不需要 git push，逐个文件 POST blob → 组装 tree → 建 commit → 更新 ref。

用法：
    set GITHUB_TOKEN=ghp_xxx && python push_via_api.py
    python push_via_api.py ghp_xxx [owner/repo]

token 只在内存中使用，不落盘。完成后请在 GitHub 设置里撤销该 token。
"""
import base64
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request

API = "https://api.github.com"
DEFAULT_REPO = "jzszlxx/-"
AUTHOR_NAME = "认知觉醒能量站"
AUTHOR_EMAIL = "congnixuejue@users.noreply.github.com"
DESCRIPTION = ("会跟着你敲键盘抬爪的 2D 桌面宠物：透明置顶、点击穿透、全局键鼠响应、"
               "截图（全屏/矩形/套索，可隐藏小猫）、电台与本地音乐、番茄钟/喝水/倒计时/多组闹钟、"
               "六页设置窗口。C# + WinForms，单文件 exe，MIT。© 2026 认知觉醒能量站")
COMMIT_MSG = """FlatCat 1.4：截图 + 电台/本地音乐

- 截图：全屏 / 矩形选区 / 不规则套索三种；可先隐藏小猫，不把自己截进图里
- 截图存 PNG（文件名带时间戳，不覆盖旧图），同时复制到剪贴板
- 电台：5 个预置频道（http mp3 流），可新增自定义电台并持久化，音量 25/50/75/100%
- 本地音乐：多选文件或整个文件夹（递归收录，最多 500 首），上一首/下一首/停止
- 设置窗口扩到六页（新增「截图」「媒体」），托盘菜单加截图/电台/音乐入口
- 音频走系统自带 WPF MediaPlayer（独立 STA 线程），仍是零第三方依赖
- 修复：老配置缺 v1.4 字段时补默认值（v14init 一次性迁移）
- 版权：认知觉醒能量站（微信公众号同名），MIT License"""


def req(path, token, method="GET", body=None, raw=False):
    url = path if path.startswith("http") else API + path
    r = urllib.request.Request(url, method=method)
    r.add_header("Accept", "application/vnd.github+json")
    r.add_header("Authorization", "Bearer " + token)
    r.add_header("X-GitHub-Api-Version", "2022-11-28")
    r.add_header("User-Agent", "FlatCat-publisher")
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        r.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(r, data=data, timeout=60) as resp:
            text = resp.read().decode("utf-8")
            return resp.status, (json.loads(text) if text.strip() else {})
    except urllib.error.HTTPError as e:
        text = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(text)
        except Exception:
            return e.code, {"message": text[:300]}


def tracked_files(root):
    out = subprocess.run(["git", "ls-files", "-z"], cwd=root, capture_output=True)
    names = [n for n in out.stdout.decode("utf-8").split("\x00") if n]
    return names


def main():
    token = sys.argv[1].strip() if len(sys.argv) >= 2 and sys.argv[1].strip() else \
        os.environ.get("GITHUB_TOKEN", "").strip()
    repo = sys.argv[2].strip() if len(sys.argv) >= 3 and sys.argv[2].strip() else DEFAULT_REPO
    if not token:
        print("缺少 token：先设 GITHUB_TOKEN 或作为第一个参数传入")
        return 2

    code, me = req("/user", token)
    if code != 200:
        print("token 无效或权限不足：", code, me.get("message"))
        return 3
    print("已认证：%s" % me["login"])

    code, info = req("/repos/" + repo, token)
    if code != 200:
        print("仓库不可用：", code, info.get("message"))
        return 4
    default = info.get("default_branch") or "main"
    print("目标仓库：%s（默认分支 %s，当前 size=%s）" % (info["full_name"], default, info.get("size")))

    code_ref, _ = req("/repos/%s/git/ref/heads/%s" % (repo, default), token)
    if code_ref == 404:
        # 空仓库不能直接 POST blob（409 Git Repository is empty），先用 Contents API 落一次提交
        seed = base64.b64encode(("# FlatCat" + chr(10) + chr(10) + "正在初始化仓库…" + chr(10)).encode("utf-8")).decode()
        code, seed_res = req("/repos/%s/contents/README.md" % repo, token, "PUT",
                             {"message": "init repository", "content": seed})
        if code not in (200, 201):
            print("初始化空仓库失败：", code, seed_res.get("message"))
            return 5
        print("空仓库已初始化（占位 README）")

    root = os.path.dirname(os.path.abspath(__file__))
    files = tracked_files(root)
    print("待提交文件：%d 个" % len(files))

    tree = []
    for i, rel in enumerate(files, 1):
        with open(os.path.join(root, rel), "rb") as f:
            data = f.read()
        payload = {"content": base64.b64encode(data).decode(), "encoding": "base64"}
        code, blob = req("/repos/%s/git/blobs" % repo, token, "POST", payload)
        if code not in (200, 201):
            print("上传失败 %s：%s %s" % (rel, code, blob.get("message")))
            return 5
        tree.append({"path": rel, "mode": "100644", "type": "blob", "sha": blob["sha"]})
        if i % 5 == 0 or i == len(files):
            print("  已上传 %d/%d" % (i, len(files)))

    code, new_tree = req("/repos/%s/git/trees" % repo, token, "POST", {"tree": tree})
    if code not in (200, 201):
        print("建 tree 失败：", code, new_tree.get("message"))
        return 6
    print("tree 已建：%s" % new_tree["sha"])

    parents = []
    code, ref = req("/repos/%s/git/ref/heads/%s" % (repo, default), token)
    if code == 200:
        parents = [ref["object"]["sha"]]
        print("父提交：%s" % parents[0][:10])
    else:
        print("空仓库，创建首个提交")

    commit_body = {
        "message": COMMIT_MSG,
        "tree": new_tree["sha"],
        "parents": parents,
        "author": {"name": AUTHOR_NAME, "email": AUTHOR_EMAIL},
        "committer": {"name": AUTHOR_NAME, "email": AUTHOR_EMAIL},
    }
    code, commit = req("/repos/%s/git/commits" % repo, token, "POST", commit_body)
    if code not in (200, 201):
        print("建 commit 失败：", code, commit.get("message"))
        return 7
    print("commit：%s" % commit["sha"])

    if parents:
        code, upd = req("/repos/%s/git/refs/heads/%s" % (repo, default), token, "PATCH",
                        {"sha": commit["sha"], "force": False})
    else:
        code, upd = req("/repos/%s/git/refs" % repo, token, "POST",
                        {"ref": "refs/heads/" + default, "sha": commit["sha"]})
    if code not in (200, 201):
        print("更新分支失败：", code, upd.get("message"))
        return 8
    print("分支 %s 已指向新提交" % default)

    # 顺手把仓库描述与主页换成项目信息（不可写权限时忽略失败）
    patch = {"description": DESCRIPTION, "homepage": ""}
    code, _ = req("/repos/" + repo, token, "PATCH", patch)
    print("仓库描述更新：", "成功" if code == 200 else "跳过（%s）" % code)

    code, info = req("/repos/" + repo, token)
    if code == 200:
        print("仓库地址：%s" % info["html_url"])
        print("大小：%s KB  推送时间：%s" % (info.get("size"), info.get("pushed_at")))
    return 0


if __name__ == "__main__":
    sys.exit(main())