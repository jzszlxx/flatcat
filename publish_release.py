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
DEFAULT_TAG = "v1.5.0"
TITLE = "FlatCat 1.5 · 动画内核 + 设置窗口换壳"
ASSETS = ["out/FlatCat.exe", "out/FlatCat-1.5.zip"]

BODY = """## FlatCat 1.5

一只会跟着你敲键盘抬爪的 2D 桌面宠物。透明置顶、点击穿透、不抢焦点，双击就能跑，不装运行时。

### 这一版新增

- **动画内核换成关键帧**：`src/Anim.cs` 里 16 段动作片段（呼吸、睡着、摸头、提醒、专注、舔爪、歪头、打字、惊醒、开心、落地、打哈欠、整点抖动、被拖走），4 种缓动曲线带轻微过冲；基础循环层 + 一次性动作层叠加，动作播完自动回落，不再是一堆互相打架的阈值判断
- **有表情了**：参数从 12 个扩到 23 个，新增笑眼、瞪眼、眉毛、微笑、张嘴、吐舌、腮红、耳朵转向、头部左右转、压扁回弹、身体前倾。摸头是笑眼 + 腮红 + 头蹭，提醒是瞪眼 + 张嘴 + 耳朵前竖，打哈欠会吐舌，拖起来瞪眼 + 尾巴耷拉，放下有压扁回弹
- **设置窗口换壳**：左导航 + 右内容 + 实时预览三栏；顶部搜索框按页名或页内控件文字过滤（输入「铃」能同时留下铃声和呼噜声）；底部状态栏；托盘菜单从 21 项平铺收到 9 项一级 + 二级分组
- **全局热键**：`Ctrl+Alt+S/R/L/P`（全屏截图 / 矩形 / 套索 / 播放暂停）。被占用时整组自动降到 `Ctrl+Shift+Alt`，不会只抢到一半导致按下去行为不一致
- **便携模式**：exe 旁放一个 `flatcat.portable` 空文件，配置改存 `exe\data\settings.json`，不碰用户漫游目录
- **静默时段**：默认关、22 点到 8 点，支持跨午夜。到点只静音，气泡和「最近提醒」日志照旧写，不吞提醒

### 1.4 已有

- **截图**：全屏 / 矩形选区 / 不规则套索三种。可以勾选「截图时隐藏小猫」，截之前先把自己藏起来，图里就没有它
- 截图存 PNG，文件名带时间戳不覆盖旧图，同时复制进剪贴板；默认放在 `图片\\FlatCat 截图`
- **电台**：5 个预置频道可直接播（AsiaFM 亚洲热歌台、1766 线上电台、Anison 动漫音乐台、台中古典音乐台、古典音乐台 FM 97.7），可新增自定义电台并持久化，音量 25/50/75/100%
- **本地音乐**：多选文件或整个文件夹（递归收录，最多 500 首），上一首 / 下一首 / 停止，一首放完自动接下一首
- 设置窗口扩到六页：外观 / 行为 / 提醒 / 系统 / 截图 / 媒体

### 修复

- `Settings.Save()` 没打 `v14init` / `v15init` 标记，导致老配置每次加载都把新增字段当「老配置」再覆盖一遍（1.4 的音量也受此影响）：保存前先打标记
- 热键注册只记成功项、失败时没有可见提示：改为整组降级到 `Ctrl+Shift+Alt`，状态栏显示当前实际组合
- 设置页标签宽度按字符估算偏小，「配置文件」「全局热键」末字被裁：中文按 17px + 12px 余量重算

### 已知限制

- 内置播放器只认 `http://` 流；`https://` 流在部分机器上会让 MediaPlayer 内部抛错，这类地址请用系统播放器
- 截图在锁屏、纯远程桌面等非交互会话里会是黑图，属系统限制
- 电台流来自第三方，可能失效；可在「媒体」页删除或换源
- 全局热键可能和常驻软件冲突：`Ctrl+Alt` 被占时整组退到 `Ctrl+Shift+Alt`；两套都被占则只注册到哪几个算几个，可在系统页整体关掉
- 便携模式靠 exe 同目录的 `flatcat.portable` 判定，放了 / 删了都要**重启程序**才切换配置目录
- 静默时段只静音，不拦气泡和「最近提醒」记录

### 下载

- `FlatCat.exe`（约 240 KB，免安装，双击即用）
- `FlatCat-1.5.zip`（含 exe、源码、构建脚本、取证脚本）

MIT License · © 2026 认知觉醒能量站"""


def req(url, token, method="GET", body=None, ctype="application/json", raw=None):
    if not url.startswith("http"):
        url = API + url
    data = raw
    if data is None and body is not None:
        data = body if isinstance(body, (bytes, bytearray)) else json.dumps(body).encode()
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