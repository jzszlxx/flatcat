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
               "关键帧动画与表情（笑眼/瞪眼/张嘴/吐舌/腮红/歪头/压扁回弹）、"
               "截图（全屏/矩形/套索）、电台与本地音乐、番茄钟/喝水/倒计时/多组闹钟、"
               "左导航设置窗口（搜索 + 实时预览）、全局热键、便携模式、静默时段、"
               "头顶时间或金句（270 条内置 + 可导入自己的金句库，字体字号可调）。"
               "C# + WinForms，单文件 exe，MIT。© 2026 认知觉醒能量站")
COMMIT_MSG = """FlatCat 1.7.0：金句库可自己添加与导入，字体字号可调

- 金句设置独立成「金句」页：头顶显示三选一、抽句范围四类（含「我的」）、
  更换间隔 30–1440 分钟、字体、字号 12–20px、自绘实时示例
- 我的金句库：添加单句（每行一句，可用 | 分隔作者与出处）、导入
  txt/csv/json（可多选）、删除选中、清空全部，上限 5000 条
- 导入解析 src/QuoteIo.cs：UTF-8 严格解码失败回落 GBK；与内置条目、
  已导入条目双向去重；空行与 # 注释忽略；超 200 字跳过
- 渲染参数化：DrawQuote 接受字体与字号，折行字数与行高按字号算，
  气泡下沿固定贴猫头顶、字号变大向上长，不压耳朵；字体缺失回退微软雅黑
- 修复：整段 JSON 解析失败曾回退成按行读，会把 JSON 源码存成一句金句；
  现在看到 [ 或 { 开头就只按 JSON 解析，失败明确报错
- 修复：GDI+ 文字盒高度不足会裁掉字的上下缘（18px 两行首版发糊）
- 体验：添加或导入成功后自动勾上「我的」，不再出现导入了却不参与抽句
- 自检新增 9 条：v17 字段往返、自定义池进出与分类隔离、去重与上限、
  txt/json 导入、坏 JSON 拒收、字号折行、字体回退、金句页控件回读

此前版本要点：
- 1.6：头顶时间与金句二选一，内置 270 条（哲思 84/诗句 106/原创 80），
  全部公有领域原文或原创，不联网
- 1.5.1：修好设置窗口导航被清空的缺陷（搜索占位符被当成过滤词）
- 1.5：关键帧动画内核、23 个表情参数、左导航设置窗口与实时预览、
  全局热键、便携模式、静默时段
- 1.4：截图（全屏/矩形/套索）、电台、本地音乐、提醒与闹钟
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