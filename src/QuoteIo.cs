// FlatCat - 金句导入：解析 txt / csv / json，去重、限长、带上限保护
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FlatCat
{
    /// <summary>
    /// 用户把自己的内容库导进来：每行一句（可用 | 或 Tab 分隔作者与出处），
    /// 或者一个 JSON 数组（对象或纯字符串）。
    /// </summary>
    internal static class QuoteIo
    {
        /// <summary>单条正文最长保留字符数，超长的导入时跳过。</summary>
        public const int MaxTextLength = 200;

        /// <summary>JSON 两种常见字段名的合体契约，反序列化后取非空者。</summary>
        [DataContract]
        internal sealed class Dto
        {
            [DataMember(Name = "text")] public string Text;
            [DataMember(Name = "t")] public string T;
            [DataMember(Name = "author")] public string Author;
            [DataMember(Name = "a")] public string A;
            [DataMember(Name = "source")] public string Source;
            [DataMember(Name = "s")] public string S;

            public string RealText { get { return string.IsNullOrEmpty(Text) ? T : Text; } }
            public string RealAuthor { get { return string.IsNullOrEmpty(Author) ? A : Author; } }
            public string RealSource { get { return string.IsNullOrEmpty(Source) ? S : Source; } }
        }

        /// <summary>去重用的归一化：去掉空白与常见标点，便于识别"同一句换了标点"。</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) continue;
                if (c == '，' || c == '。' || c == '、' || c == '；' || c == '：' || c == '！' || c == '？'
                    || c == ',' || c == '.' || c == ';' || c == ':' || c == '!' || c == '?'
                    || c == '“' || c == '”' || c == '‘' || c == '’' || c == '"' || c == '\''
                    || c == '（' || c == '）' || c == '(' || c == ')') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>按字节读文本：先严格 UTF-8，失败再按 GBK（中文 Windows 常见）。</summary>
        public static string ReadText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string text = null;
            try
            {
                UTF8Encoding strict = new UTF8Encoding(false, true);
                text = strict.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                text = null;
            }
            if (text == null)
            {
                try { text = Encoding.GetEncoding(936).GetString(bytes); }
                catch { text = Encoding.Default.GetString(bytes); }
            }
            return text.TrimStart('\uFEFF');
        }

        /// <summary>把整段文本解析成条目（json 走 JSON 分支，其余按行）。</summary>
        public static List<CustomQuote> Parse(string content)
        {
            if (string.IsNullOrEmpty(content)) return new List<CustomQuote>();
            string head = content.TrimStart();
            if (head.StartsWith("[") || head.StartsWith("{"))
            {
                // 看着像 JSON 就只按 JSON 解析：解析不出来宁可返回空，
                // 不能回退成按行读，否则整段 JSON 源码会被当成一句金句存进去
                return ParseJson(content);
            }
            return ParseLines(content);
        }

        /// <summary>txt / csv：每行一条，支持「句子|作者|出处」或 Tab 分隔；# 开头当注释。</summary>
        public static List<CustomQuote> ParseLines(string content)
        {
            List<CustomQuote> list = new List<CustomQuote>();
            string[] lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().TrimStart('\uFEFF');
                if (line.Length == 0) continue;
                if (line[0] == '#' || line.StartsWith("//")) continue;

                string text = line, author = "", source = "";
                char sep = line.IndexOf('|') >= 0 ? '|' : (line.IndexOf('\t') >= 0 ? '\t' : '\0');
                if (sep != '\0')
                {
                    string[] parts = line.Split(sep);
                    text = parts[0].Trim();
                    if (parts.Length > 1) author = parts[1].Trim();
                    if (parts.Length > 2) source = parts[2].Trim();
                }
                if (text.Length == 0) continue;
                list.Add(new CustomQuote(text, author, source));
            }
            return list;
        }

        /// <summary>json：对象数组（text/t/author/a/source/s）或纯字符串数组。</summary>
        public static List<CustomQuote> ParseJson(string content)
        {
            List<CustomQuote> list = new List<CustomQuote>();
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Dto>));
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    List<Dto> dtos = ser.ReadObject(ms) as List<Dto>;
                    if (dtos != null)
                    {
                        for (int i = 0; i < dtos.Count; i++)
                        {
                            Dto d = dtos[i];
                            if (d == null) continue;
                            string t = d.RealText;
                            if (string.IsNullOrEmpty(t)) continue;
                            list.Add(new CustomQuote(t.Trim(), d.RealAuthor, d.RealSource));
                        }
                    }
                }
                if (list.Count > 0) return list;
            }
            catch { }

            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<string>));
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    List<string> strs = ser.ReadObject(ms) as List<string>;
                    if (strs != null)
                    {
                        for (int i = 0; i < strs.Count; i++)
                        {
                            if (string.IsNullOrEmpty(strs[i])) continue;
                            list.Add(new CustomQuote(strs[i].Trim(), "", ""));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// 把文件导入到 target：跳过重复（与已有自定义、与内置条目都算重复）、跳过空与超长。
        /// 返回新增条数；skipped 返回被跳过的条数；error 非空表示整个文件读/解析失败。
        /// </summary>
        public static int Import(string path, List<CustomQuote> target, out int skipped, out string error)
        {
            skipped = 0;
            error = null;
            if (target == null) return 0;
            string content;
            try
            {
                content = ReadText(path);
            }
            catch (Exception ex)
            {
                error = "读取失败：" + ex.Message;
                return 0;
            }

            List<CustomQuote> parsed = Parse(content);
            if (parsed.Count == 0)
            {
                error = "没解析出任何句子（txt 每行一句，或 json 数组）";
                return 0;
            }

            return Add(parsed, target, out skipped);
        }

        /// <summary>
        /// 把已解析的条目并进 target：与已有自定义、与内置条目都去重，跳过空与超长，
        /// 到上限后剩下的全记为跳过。返回实际新增条数。
        /// </summary>
        public static int Add(List<CustomQuote> parsed, List<CustomQuote> target, out int skipped)
        {
            skipped = 0;
            if (parsed == null || target == null) return 0;

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < target.Count; i++)
                if (target[i] != null && target[i].Text != null) seen.Add(Normalize(target[i].Text));
            for (int i = 0; i < QuoteBank.All.Length; i++)
                seen.Add(Normalize(QuoteBank.All[i].Text));

            int added = 0;
            for (int i = 0; i < parsed.Count; i++)
            {
                CustomQuote q = parsed[i];
                if (q == null || string.IsNullOrEmpty(q.Text)) { skipped++; continue; }
                string key = Normalize(q.Text);
                if (key.Length == 0) { skipped++; continue; }
                if (q.Text.Length > MaxTextLength) { skipped++; continue; }
                if (seen.Contains(key)) { skipped++; continue; }
                if (target.Count >= QuoteBank.CustomLimit) { skipped++; continue; }
                seen.Add(key);
                target.Add(q);
                added++;
            }
            return added;
        }
    }
}