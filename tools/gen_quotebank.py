# -*- coding: utf-8 -*-
"""把 out/quotes_src.json 生成为 src/QuoteBank.cs，并做数据体检。

体检项：空串、超长、重复、作者/出处缺失、分类计数。
校验：与 out/quote_pool.json（chinese-poetry 原文切句，MIT）比对，命中则用库里的原文与出处。

v1.7：池子里增加了「自定义」分类（CatCustom），抽句改为 BuildPool + Pick(pool) 两步，
      这样用户自己添加/导入的条目能和内置条目一起参与，不用每次现场转换。
"""
import json
import os
import sys

MAX_LEN = {"poem": 24, "thought": 26, "line": 28}
CAT = {"thought": "CatThought", "poem": "CatPoem", "line": "CatLine"}


def norm(s):
    return s.replace("，", ",").replace("。", ".").replace("、", ",").replace("：", ":").replace("\u3000", "").strip()


def main():
    src = json.load(open(os.path.join("out", "quotes_src.json"), encoding="utf-8"))
    pool = []
    ppath = os.path.join("out", "quote_pool.json")
    if os.path.exists(ppath):
        pool = json.load(open(ppath, encoding="utf-8"))
    index = {}
    for q in pool:
        index[norm(q["text"])] = q

    rows, seen, problems = [], set(), []
    for it in src:
        t = (it.get("t") or "").strip()
        a = (it.get("a") or "").strip()
        s = (it.get("s") or "").strip()
        k = it.get("k")
        if k not in CAT:
            problems.append("未知分类: %r" % it)
            continue
        if not t:
            problems.append("空句: %r" % it)
            continue
        if len(t) > MAX_LEN[k]:
            problems.append("超长(%d>%d): %s" % (len(t), MAX_LEN[k], t))
        n = norm(t)
        if n in seen:
            problems.append("重复: %s" % t)
            continue
        seen.add(n)
        hit = index.get(n)
        if hit:
            if not a and hit.get("author"):
                a = hit["author"]
            if not s and hit.get("source"):
                s = hit["source"]
        rows.append({"t": t, "a": a or "佚名", "s": s, "k": k})

    counts = {}
    for r in rows:
        counts[r["k"]] = counts.get(r["k"], 0) + 1

    print("条目总数 %d" % len(rows))
    for k in ("poem", "thought", "line"):
        print("  %-8s %d 条" % (k, counts.get(k, 0)))
    print("命中 chinese-poetry 原库校验 %d 条" % sum(
        1 for r in rows if norm(r["t"]) in index))
    if problems:
        print("体检问题 %d 条：" % len(problems))
        for p in problems[:20]:
            print("   -", p)
    else:
        print("体检：无问题")

    out = []
    out.append("// FlatCat - 金句库：诗句 / 哲思 / 原创金句，全部内置，不联网")
    out.append("// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。")
    out.append("//")
    out.append("// 数据来源：")
    out.append("//   诗句、哲思为公有领域经典原文（唐宋诗词、诗经、楚辞、论语、道德经、庄子、")
    out.append("//   菜根谭、幽梦影、围炉夜话等，作者逝世均逾 70 年）。")
    out.append("//   其中诗经 / 论语 / 楚辞 / 幽梦影 / 纳兰性德 等条目已与 chinese-poetry/chinese-poetry")
    out.append("//   （MIT License）收录的原文逐条比对过，出处以此为准。")
    out.append("//   金句为 FlatCat 原创短句，随项目一同以 MIT 发布。")
    out.append("//")
    out.append("// 未采用 hitokoto-osc/sentences-bundle：该项目为 AGPL-3.0，与本项目 MIT 不兼容。")
    out.append("//")
    out.append("// 本文件由 tools/gen_quotebank.py 生成；用户自定义条目运行时另行拼进池子，不写在这里。")
    out.append("using System;")
    out.append("using System.Collections.Generic;")
    out.append("")
    out.append("namespace FlatCat")
    out.append("{")
    out.append("    /// <summary>一条金句：正文 + 作者 + 出处 + 分类位。</summary>")
    out.append("    internal sealed class Quote")
    out.append("    {")
    out.append("        public readonly string Text;")
    out.append("        public readonly string Author;")
    out.append("        public readonly string Source;")
    out.append("        public readonly int Cat;")
    out.append("")
    out.append("        public Quote(string text, string author, string source, int cat)")
    out.append("        {")
    out.append("            Text = text;")
    out.append("            Author = author;")
    out.append("            Source = source;")
    out.append("            Cat = cat;")
    out.append("        }")
    out.append("")
    out.append("        /// <summary>气泡第二行的小字署名；原创金句不署名。</summary>")
    out.append("        public string Credit")
    out.append("        {")
    out.append("            get")
    out.append("            {")
    out.append("                if (Cat == QuoteBank.CatLine) return \"\";")
    out.append("                if (string.IsNullOrEmpty(Author) || Author == \"佚名\") return Source == null ? \"\" : Source;")
    out.append("                if (string.IsNullOrEmpty(Source)) return Author;")
    out.append("                return Author + \" · \" + Source;")
    out.append("            }")
    out.append("        }")
    out.append("    }")
    out.append("")
    out.append("    internal static class QuoteBank")
    out.append("    {")
    out.append("        public const int CatThought = 1;   // 哲思")
    out.append("        public const int CatPoem = 2;      // 诗句")
    out.append("        public const int CatLine = 4;      // 原创金句")
    out.append("        public const int CatCustom = 8;    // 用户自己添加 / 导入的内容库")
    out.append("        public const int CatAll = CatThought | CatPoem | CatLine | CatCustom;")
    out.append("        /// <summary>内置条目的分类范围（不含自定义）。</summary>")
    out.append("        public const int CatBuiltIn = CatThought | CatPoem | CatLine;")
    out.append("        /// <summary>自定义条目上限，防止配置文件被塞爆。</summary>")
    out.append("        public const int CustomLimit = 5000;")
    out.append("")
    out.append("        public static readonly Quote[] All = new Quote[]")
    out.append("        {")
    for r in rows:
        out.append('            new Quote(%s, %s, %s, %s),' % (
            cs(r["t"]), cs(r["a"]), cs(r["s"]), CAT[r["k"]]))
    out.append("        };")
    out.append("")
    out.append("        /// <summary>按分类位组装抽句池：内置条目 + 用户自定义条目。</summary>")
    out.append("        public static List<Quote> BuildPool(int mask, List<CustomQuote> custom)")
    out.append("        {")
    out.append("            if ((mask & CatAll) == 0) mask = CatAll;")
    out.append("            List<Quote> pool = new List<Quote>();")
    out.append("            for (int i = 0; i < All.Length; i++)")
    out.append("                if ((All[i].Cat & mask) != 0) pool.Add(All[i]);")
    out.append("            if (custom != null && (mask & CatCustom) != 0)")
    out.append("            {")
    out.append("                for (int i = 0; i < custom.Count && pool.Count < 20000; i++)")
    out.append("                {")
    out.append("                    CustomQuote c = custom[i];")
    out.append("                    if (c == null || string.IsNullOrEmpty(c.Text)) continue;")
    out.append("                    pool.Add(new Quote(c.Text, c.Author, c.Source, CatCustom));")
    out.append("                }")
    out.append("            }")
    out.append("            return pool;")
    out.append("        }")
    out.append("")
    out.append("        public static int CountOf(int mask, List<CustomQuote> custom)")
    out.append("        {")
    out.append("            if ((mask & CatAll) == 0) mask = CatAll;")
    out.append("            int n = 0;")
    out.append("            for (int i = 0; i < All.Length; i++) if ((All[i].Cat & mask) != 0) n++;")
    out.append("            if (custom != null && (mask & CatCustom) != 0)")
    out.append("            {")
    out.append("                for (int i = 0; i < custom.Count; i++)")
    out.append("                    if (custom[i] != null && !string.IsNullOrEmpty(custom[i].Text)) n++;")
    out.append("            }")
    out.append("            return n;")
    out.append("        }")
    out.append("")
    out.append("        /// <summary>从池子里随机取一条；尽量避开上一条，避免连着重复。</summary>")
    out.append("        public static Quote Pick(List<Quote> pool, string lastText, Random r)")
    out.append("        {")
    out.append("            if (pool == null || pool.Count == 0) return null;")
    out.append("            if (r == null) r = new Random();")
    out.append("            int pick = r.Next(pool.Count);")
    out.append("            for (int guard = 0; guard < 6 && pool.Count > 1 && pool[pick].Text == lastText; guard++)")
    out.append("                pick = r.Next(pool.Count);")
    out.append("            return pool[pick];")
    out.append("        }")
    out.append("")
    out.append("        /// <summary>分类名，供设置页与自检显示。</summary>")
    out.append("        public static string CatName(int cat)")
    out.append("        {")
    out.append("            if (cat == CatThought) return \"哲思\";")
    out.append("            if (cat == CatPoem) return \"诗句\";")
    out.append("            if (cat == CatLine) return \"金句\";")
    out.append("            if (cat == CatCustom) return \"自定义\";")
    out.append("            return \"未知\";")
    out.append("        }")
    out.append("    }")
    out.append("}")

    path = os.path.join("src", "QuoteBank.cs")
    open(path, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
    print("已生成 %s（%d 行，%d 条）" % (path, len(out), len(rows)))
    return 0


def cs(s):
    """C# 字符串字面量：转义反斜杠、引号、换行。"""
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n") + '"'


if __name__ == "__main__":
    sys.exit(main())