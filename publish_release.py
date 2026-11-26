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
DEFAULT_TAG = "v1.7.0"
TITLE = "FlatCat 1.7.0 · 金句库可以自己加了，字体字号也能调"
ASSETS = ["out/FlatCat.exe", "out/FlatCat-1.7.0.zip"]

BODY = """## FlatCat 1.7.0

一只会跟着你敲键盘抬爪的 2D 桌面宠物。透明置顶、点击穿透、不抢焦点，双击就能跑，不装运行时。

### 这一版：金句库归你，字体字号也归你

- **诗与哲思还是内置的**：哲思 84 条、诗句 106 条、原创金句 80 条，共 270 条。诗句与哲思都是公有领域经典原文（唐宋诗词、诗经、楚辞、论语、道德经、庄子、菜根谭、幽梦影、围炉夜话等，作者逝世均逾 70 年），金句为 FlatCat 原创，随项目以 MIT 发布，**不联网、不请求任何接口**
- **可以自己加**：金句页的输入框粘一句就行，多行就是多句，可用 `|` 分隔作者和出处（`山有木兮木有枝，心悦君兮君不知|佚名|越人歌`）
- **也可以整库导进来**：`导入文件…` 支持
  - `txt` / `csv`：每行一句，可用 `|` 或 Tab 分隔作者、出处，`#` 开头是注释
  - `json`：字符串数组，或带 `text` / `author` / `source` 字段的对象数组
  - 编码自动识别（先按 UTF-8 严格解码，失败回落 GBK），中文不会乱码
  - 可一次选多个文件；与内置条目、与已导入条目双向去重；空行、超 200 字的行自动跳过；上限 5000 条
- **我的库可管**：列表里能逐条看到，「删除选中」或「清空全部」（有二次确认），顶部实时显示池内条数与「已添加 M 条，上限 5000 条」
- **字体可选、字号可调**：字号 12–20px；字体下拉列出本机真实装了的 13 款中文字体（微软雅黑 / 宋体 / 楷体 / 仿宋 / 等线 / 华文中宋 / 幼圆 / 隶书 / 华文楷体 / 华文宋体 / 黑体 / Segoe UI / Consolas）；配了不存在的字体名会自动回退微软雅黑
- **大字不压耳朵**：折行字数跟着字号走（12px 每行 21 字、15px 17 字、20px 13 字），气泡下沿固定贴着猫头顶，字号变大是向上长，不会往下压耳朵
- **示例是真的**：设置页那块示例不是一行文字，而是直接调渲染函数画出来的气泡，看到什么样、猫头上就是什么样

### 顺手修掉的两处

- 整段 JSON 解析失败时旧逻辑会回退成「按行读」，把 JSON 源码本身存成一句金句。现在看到 `[` 或 `{` 开头就只按 JSON 解析，解析不出来明确报错，不再产生脏数据
- 18px 两行时文字顶部发糊：GDI+ 的文字盒高度不足会裁掉字的上下缘，已按字号加高文字盒

### 头顶显示怎么用

「金句」页最上面三选一：不显示 / 时间 / 金句。选金句后，猫头顶按「更换间隔」（30 分钟到 24 小时）轮换，随时可以点「立即换一条」。分类勾选决定抽句范围，勾上「我的」才会把你的句子也抽进来（添加或导入成功后会自动勾上）。

### 已知限制

- 混合类型的 JSON 数组（对象和字符串混在一个数组里）不支持，会明确报「没解析出句子」，不会硬塞
- 金句最多两行，超出部分截断加「…」；单条正文上限 200 字
- 设置页示例区没有猫头，所以气泡尾巴会被裁掉一点（那是给猫头留的）
- 字体列表只列本机装了的；想让某款字体出现，先装到系统里
- 截图在锁屏、纯远程桌面等非交互会话里会是黑图，属系统限制；内置播放器只认 `http://` 流

### 自检

`FlatCat.exe --selftest out/selftest.txt` 共 49 项，本版全部 PASS，其中 9 条是这一版新增：v17 字段往返、自定义池进出与分类隔离、去重与上限、txt/json 导入、坏 JSON 拒收、字号折行、字体回退、金句页控件回读。

### 下载

- `FlatCat.exe`（约 283 KB，免安装，双击即用）
- `FlatCat-1.7.0.zip`（含 exe、源码、构建脚本、取证脚本、金句生成脚本）

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