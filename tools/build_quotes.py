# -*- coding: utf-8 -*-
"""从 MIT 许可的 chinese-poetry 仓库取原文，切句筛出适合头顶气泡的短句。

输出：
    out/qdata/*.json        原始数据
    out/quote_pool.json     候选池（text / author / source）
    out/quote_pool.txt      带编号的可读清单，供人工挑选
"""
import base64
import json
import os
import re
import urllib.parse
import urllib.request

TOKEN = os.environ["TOKEN"]
BASE = "https://api.github.com"
OUT = os.path.join("out", "qdata")

SOURCES = [
    ("全唐诗", "poet.tang.0.json", "tang"),
    ("宋词", "ci.song.0.json", "song"),
    ("诗经", "shijing.json", "shijing"),
    ("楚辞", "chuci.json", "chuci"),
    ("纳兰性德", "纳兰性德诗集.json", "nalan"),
    ("论语", "lunyu.json", "lunyu"),
    ("幽梦影", "youmengying.json", "youmeng"),
]


def api_get(path):
    url = BASE + urllib.parse.quote("/repos/chinese-poetry/chinese-poetry/contents/" + path, safe="/")
    req = urllib.request.Request(url)
    req.add_header("Authorization", "Bearer " + TOKEN)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("User-Agent", "flatcat-quotes")
    with urllib.request.urlopen(req, timeout=120) as resp:
        return json.loads(resp.read().decode("utf-8"))


def fetch(dirname, filename):
    os.makedirs(OUT, exist_ok=True)
    local = os.path.join(OUT, filename)
    if os.path.exists(local) and os.path.getsize(local) > 1000:
        return open(local, "rb").read()
    meta = api_get(dirname + "/" + filename)
    raw = base64.b64decode(meta["content"])
    open(local, "wb").write(raw)
    return raw


def collect(kind, raw):
    """统一抽出 (text, author, source) 三元组。"""
    data = json.loads(raw.decode("utf-8-sig"))
    rows = []
    if kind in ("tang", "song"):
        for it in data:
            author = it.get("author", "") or ""
            title = it.get("title") or it.get("rhythmic") or ""
            for p in it.get("paragraphs", []) or []:
                rows.append((p, author, title))
    elif kind == "shijing":
        for it in data:
            chapter = it.get("chapter", "")
            title = it.get("title", "")
            for p in it.get("content", []) or []:
                rows.append((p, "", (chapter + "·" + title).strip("·")))
    elif kind == "chuci":
        for it in data:
            sec = it.get("section", "")
            title = it.get("title", "")
            for p in it.get("content", []) or []:
                rows.append((p, "屈原等", (sec + "·" + title).strip("·")))
    elif kind == "nalan":
        for it in data:
            title = it.get("title", "")
            for p in it.get("para", []) or []:
                rows.append((p, "纳兰性德", title))
    elif kind == "lunyu":
        for it in data:
            chapter = it.get("chapter", "")
            for p in it.get("paragraphs", []) or []:
                rows.append((p, "孔子及弟子", chapter))
    elif kind == "youmeng":
        for it in data:
            content = it.get("content") or it.get("paragraph") or ""
            if isinstance(content, list):
                content = "".join(content)
            rows.append((content, "张潮", "幽梦影"))
    return rows


SPLIT = re.compile(r"[。！？；\n]")


def sentences(text):
    for part in SPLIT.split(text):
        part = part.strip(" \t，、,:：")
        part = part.replace("　", "")
        if not (8 <= len(part) <= 24):
            continue
        # 优先成对句：中含逗号，视觉上像一句完整的话
        if "，" not in part and "、" not in part:
            continue
        if part.count("，") > 1:
            continue
        yield part


def main():
    pool = []
    seen = set()
    stats = {}
    for dirname, filename, kind in SOURCES:
        raw = fetch(dirname, filename)
        n = 0
        for text, author, source in collect(kind, raw):
            for s in sentences(text):
                if s in seen:
                    continue
                seen.add(s)
                pool.append({"text": s, "author": author, "source": source, "kind": kind})
                n += 1
                if n >= 400:
                    break
            if n >= 400:
                break
        stats[kind] = n
        print("%-10s %4d 条候选" % (dirname, n))

    json.dump(pool, open("out/quote_pool.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    lines = []
    for i, q in enumerate(pool):
        src = q["source"]
        who = q["author"]
        tag = ("%s·%s" % (who, src)) if who else src
        lines.append("%3d  %-26s [%s]" % (i, q["text"], tag))
    open("out/quote_pool.txt", "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print("合计 %d 条 -> out/quote_pool.json / out/quote_pool.txt" % len(pool))


if __name__ == "__main__":
    main()