// FlatCat - 矢量猫与气泡绘制（毛色 / 斑纹 / 异瞳 / 配饰）
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlatCat
{
    internal static class CatPainter
    {
        public const float Design = 320f;

        /// <summary>
        /// 气泡额外占用的顶部高度。猫头顶上方本来就有约 62px 留白，
        /// 气泡直接画在留白里，所以这里保持 0，窗口高度仍等于 Design。
        /// </summary>
        public const float BubbleH = 0f;

        public const float HeadCx = 160f;
        public const float HeadCy = 112f;
        public const float HeadR = 62f;

        private static readonly Color KbBase = Color.FromArgb(46, 42, 38);
        private static readonly Color KbKey = Color.FromArgb(216, 205, 182);
        private static readonly Color KbAccent = Color.FromArgb(206, 126, 58);
        private static readonly Color Ink = Color.FromArgb(38, 34, 32);

        /// <summary>兼容旧调用：无配饰、显示斑纹、不异瞳。</summary>
        public static void Draw(Graphics g, CatAnim a, bool showKeyboard, Coat coat)
        {
            Draw(g, a, showKeyboard, coat, 0, true, false);
        }

        public static void Draw(Graphics g, CatAnim a, bool showKeyboard, Coat coat,
                                int accessory, bool showSpots, bool heterochromia)
        {
            if (coat == null) coat = Coats.All[0];

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float dip = a.BodyDip * 4f;

            DrawShadow(g, dip, showKeyboard);
            DrawTail(g, a, coat, showSpots);
            DrawBody(g, a, dip, coat, showSpots);
            if (showKeyboard) DrawKeyboard(g);
            DrawFrontLegs(g, a, dip, coat);
            DrawHead(g, a, dip, coat, heterochromia, accessory);
            DrawNeckAccessory(g, accessory, dip);
        }

        /// <summary>猫头顶的对话气泡：显示倒计时、按键名或提醒文案。坐标在设计空间，y=0 起。</summary>
        public static void DrawBubble(Graphics g, string text, float t, bool alert)
        {
            DrawBubble(g, text, t, alert, 19f);
        }

        public static void DrawBubble(Graphics g, string text, float t, bool alert, float fontSize)
        {
            if (text == null || text.Length == 0) return;

            using (Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                SizeF measured = g.MeasureString(text, font, 1000, sf);
                float w = measured.Width + 28f;
                float minW = fontSize > 16f ? 108f : 72f;
                if (w < minW) w = minW;
                if (w > 268f) w = 268f;
                float h = fontSize > 16f ? 44f : 38f;

                float bob = (float)Math.Sin(t * 2.6) * 2f;
                float x = 160f - w / 2f;
                float y = (fontSize > 16f ? 4f : 12f) + bob;

                Color fill = alert ? Color.FromArgb(250, 232, 216) : Color.FromArgb(250, 255, 255, 251);
                Color edge = alert ? Color.FromArgb(206, 124, 78) : Color.FromArgb(210, 200, 184);
                Color inkColor = alert ? Color.FromArgb(146, 68, 28) : Color.FromArgb(58, 53, 46);

                using (GraphicsPath body = RoundRect(new RectangleF(x, y, w, h), 14f))
                {
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, body);
                    using (Pen p = new Pen(edge, 1.6f)) g.DrawPath(p, body);
                }

                PointF[] tail = new PointF[]
                {
                    new PointF(151f, y + h - 1f),
                    new PointF(169f, y + h - 1f),
                    new PointF(160f, y + h + 9f)
                };
                using (GraphicsPath tp = new GraphicsPath())
                {
                    tp.AddPolygon(tail);
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, tp);
                }
                using (Pen p = new Pen(edge, 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, tail[0], tail[2]);
                    g.DrawLine(p, tail[2], tail[1]);
                }

                using (SolidBrush b = new SolidBrush(inkColor))
                    g.DrawString(text, font, b, new RectangleF(x, y, w, h), sf);
            }
        }

        /// <summary>金句气泡：正文最多折两行，字体与字号由设置决定，宽度 268 固定。</summary>
        public static void DrawQuote(Graphics g, string text, float t)
        {
            DrawQuote(g, text, t, "Microsoft YaHei", 15f);
        }

        /// <summary>字号能塞进一行多少字：按 268 宽度与中文字宽估算，留一点余量。</summary>
        public static int PerLineFor(float fontSize)
        {
            if (fontSize < 10f) fontSize = 10f;
            if (fontSize > 20f) fontSize = 20f;
            int n = (int)(268f / (fontSize * 1.02f));
            if (n < 8) n = 8;
            if (n > 22) n = 22;
            return n;
        }

        /// <summary>两行时单行的高度：字号越大行距越紧，封顶 20.5 保证两行不越过猫头顶。</summary>
        private static float LineH(float fontSize)
        {
            float lh = fontSize + 3f;
            if (lh > 20.5f) lh = 20.5f;
            return lh;
        }

        /// <summary>
        /// 把用户写的字体名解析成本机字体族的规范名（如 Microsoft YaHei → 微软雅黑）。
        /// GDI+ 自己认英文名和本地化名，这里借用它，解析不了返回 null。
        /// </summary>
        public static string CanonicalFont(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                using (FontFamily f = new FontFamily(name))
                {
                    if (f.Name != null && f.Name.Length > 0) return f.Name;
                }
            }
            catch { }
            return null;
        }

        /// <summary>本机是否存在该字体族。</summary>
        public static bool FontExists(string name)
        {
            return CanonicalFont(name) != null;
        }

        /// <summary>字体缺失时回退微软雅黑，避免 new Font 静默换成别的字形。</summary>
        public static string ResolveFont(string name)
        {
            string c = CanonicalFont(name);
            if (!string.IsNullOrEmpty(c)) return c;
            c = CanonicalFont("Microsoft YaHei");
            if (!string.IsNullOrEmpty(c)) return c;
            c = CanonicalFont("宋体");
            return string.IsNullOrEmpty(c) ? "宋体" : c;
        }

        /// <summary>候选字体里本机实际装了的那些（统一成规范名、去重），供设置页下拉用。</summary>
        public static string[] PickFonts(string[] wanted)
        {
            if (wanted == null) return new string[0];
            List<string> hit = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < wanted.Length; i++)
            {
                string c = CanonicalFont(wanted[i]);
                if (c == null) continue;
                if (!seen.Add(c.ToLowerInvariant())) continue;
                hit.Add(c);
            }
            return hit.ToArray();
        }

        /// <summary>金句气泡（可指定字体与字号）。</summary>
        public static void DrawQuote(Graphics g, string text, float t, string fontName, float fontSize)
        {
            if (text == null || text.Length == 0) return;
            if (fontSize < 10f) fontSize = 10f;
            if (fontSize > 20f) fontSize = 20f;

            string line1, line2;
            SplitQuote(text, PerLineFor(fontSize), out line1, out line2);

            using (Font font = new Font(ResolveFont(fontName), fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                float w = 268f;
                float lh = LineH(fontSize);
                float h = line2 == null ? lh + 14f : lh * 2f + 8f;
                float bob = (float)Math.Sin(t * 2.6) * 2f;
                float x = 160f - w / 2f;
                // 气泡下沿始终贴着猫头顶（设计坐标 y=50），字号小时整体下移
                float y = 50f - h + bob;

                Color fill = Color.FromArgb(250, 255, 255, 251);
                Color edge = Color.FromArgb(210, 200, 184);
                Color ink = Color.FromArgb(58, 53, 46);

                using (GraphicsPath body = RoundRect(new RectangleF(x, y, w, h), 14f))
                {
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, body);
                    using (Pen p = new Pen(edge, 1.6f)) g.DrawPath(p, body);
                }

                PointF[] tail = new PointF[]
                {
                    new PointF(151f, y + h - 1f),
                    new PointF(169f, y + h - 1f),
                    new PointF(160f, y + h + 9f)
                };
                using (GraphicsPath tp = new GraphicsPath())
                {
                    tp.AddPolygon(tail);
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, tp);
                }
                using (Pen p = new Pen(edge, 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, tail[0], tail[2]);
                    g.DrawLine(p, tail[2], tail[1]);
                }

                // 文字盒比行距高几像素：GDI+ 在矩形不够高时会裁掉字的上下缘（字号越大越明显）
                float boxH = fontSize + 6f;
                float blockH = line2 == null ? boxH : lh + boxH;
                float top = y + (h - blockH) / 2f;
                using (SolidBrush b = new SolidBrush(ink))
                {
                    g.DrawString(line1, font, b, new RectangleF(x, top, w, boxH), sf);
                    if (line2 != null) g.DrawString(line2, font, b, new RectangleF(x, top + lh, w, boxH), sf);
                }
            }
        }

        /// <summary>供自检调用的折行入口（默认 15px）。</summary>
        public static void SplitQuoteForTest(string text, out string first, out string second)
        {
            SplitQuote(text, PerLineFor(15f), out first, out second);
        }

        /// <summary>供自检调用的折行入口（指定字号）。</summary>
        public static void SplitQuoteForTest(string text, float fontSize, out string first, out string second)
        {
            SplitQuote(text, PerLineFor(fontSize), out first, out second);
        }

        /// <summary>金句折行：优先在中间标点处断开，最长两行、每行 perLine 字。</summary>
        private static void SplitQuote(string text, int perLine, out string first, out string second)
        {
            first = text;
            second = null;
            if (text.Length <= perLine) return;

            int cut = -1;
            int from = perLine / 2;
            if (from < 4) from = 4;
            for (int i = from; i <= perLine && i < text.Length; i++)
            {
                char c = text[i - 1];
                if (c == '，' || c == '、' || c == '；' || c == '。' || c == '：') cut = i;
            }
            if (cut < 0) cut = perLine;

            first = text.Substring(0, cut);
            second = text.Substring(cut);
            if (second.Length > perLine) second = second.Substring(0, perLine - 1) + "…";

            // 第二行只剩标点时别单独占一行（会变成一个孤零零的句号），并回第一行
            if (second != null && second.Trim('，', '。', '、', '；', '：', '！', '？', ' ').Length == 0)
            {
                first = first + second;
                second = null;
            }
        }

        private static void DrawShadow(Graphics g, float dip, bool keyboard)
        {
            float y = keyboard ? 300f : 272f;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(88, y + dip * 0.4f, 144, 16);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(26, 70, 56, 42)))
                    g.FillPath(b, p);
            }
        }

        private static void DrawTail(Graphics g, CatAnim a, Coat coat, bool spots)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(206, 244);
            g.RotateTransform(a.TailAngle);
            using (Pen pen = new Pen(coat.Fur, 17f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddBezier(0, 0, 14, -8, 40, -20, 54, -56);
                    g.DrawPath(pen, p);
                }
            }
            if (spots)
            {
                using (Pen pen = new Pen(coat.FurDark, 6f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 26, -14, 33, -24);
                    g.DrawLine(pen, 42, -30, 48, -42);
                }
            }
            g.Restore(st);
        }

        private static void DrawBody(Graphics g, CatAnim a, float dip, Coat coat, bool spots)
        {
            GraphicsState st = g.Save();
            if (a.Squash > 0.01f || Math.Abs(a.BodyLean) > 0.01f)
            {
                // 落地压扁：横向变宽、纵向变矮；前倾时整体横向偏移
                float sx = 1f + 0.10f * a.Squash;
                float sy = 1f - 0.14f * a.Squash;
                g.TranslateTransform(160f + a.BodyLean * 7f, 206f + dip);
                g.ScaleTransform(sx, sy);
                g.TranslateTransform(-160f, -206f);
            }
            else
            {
                g.TranslateTransform(0, dip);
            }

            FillEllipse(g, coat.Fur, 102, 220, 60, 54);
            FillEllipse(g, coat.Fur, 158, 220, 60, 54);

            using (GraphicsPath body = new GraphicsPath())
            {
                body.AddBezier(160, 142, 120, 142, 98, 170, 98, 208);
                body.AddBezier(98, 208, 98, 250, 118, 266, 160, 266);
                body.AddBezier(160, 266, 202, 266, 222, 250, 222, 208);
                body.AddBezier(222, 208, 222, 170, 200, 142, 160, 142);
                body.CloseFigure();
                using (SolidBrush b = new SolidBrush(coat.Fur)) g.FillPath(b, body);

                if (spots)
                {
                    g.SetClip(body);
                    using (Pen pen = new Pen(coat.FurDark, 7f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawBezier(pen, 106, 190, 120, 198, 126, 212, 124, 228);
                        g.DrawBezier(pen, 214, 190, 200, 198, 194, 212, 196, 228);
                    }
                    g.ResetClip();
                }
            }

            using (GraphicsPath bib = new GraphicsPath())
            {
                bib.AddEllipse(138, 148, 44, 42);
                using (SolidBrush b = new SolidBrush(coat.Light))
                    g.FillPath(b, bib);
            }
            g.Restore(st);
        }

        private static void DrawKeyboard(Graphics g)
        {
            RectangleF kb = new RectangleF(52, 262, 216, 40);
            using (GraphicsPath p = RoundRect(kb, 8f))
            using (SolidBrush b = new SolidBrush(KbBase))
                g.FillPath(b, p);

            float x0 = 60f, y0 = 268f;
            int cols = 12, rows = 2;
            float kw = 13.5f, kh = 7.5f, gap = 2.4f;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    bool accent = (r == 0 && c == 0) || (r == 1 && c == cols - 1);
                    using (SolidBrush b = new SolidBrush(accent ? KbAccent : KbKey))
                        g.FillRectangle(b, x0 + c * (kw + gap), y0 + r * (kh + gap), kw, kh);
                }
            }
            using (SolidBrush b = new SolidBrush(KbKey))
                g.FillRectangle(b, x0 + 3 * (kw + gap), y0 + 2 * (kh + gap), kw * 5, kh);
        }

        private static void DrawFrontLegs(Graphics g, CatAnim a, float dip, Coat coat)
        {
            DrawFrontLeg(g, 118f, 192f + dip, a.LeftPaw, coat);
            DrawFrontLeg(g, 174f, 192f + dip, a.RightPaw, coat);
        }

        private static void DrawFrontLeg(Graphics g, float x, float y, float lift, Coat coat)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            g.TranslateTransform(0, -16f * lift);
            g.RotateTransform(-8f * lift);

            using (GraphicsPath p = RoundRect(new RectangleF(0, 0, 28, 58), 13f))
            using (SolidBrush b = new SolidBrush(coat.Fur))
                g.FillPath(b, p);

            using (GraphicsPath paw = new GraphicsPath())
            {
                paw.AddEllipse(-3, 44, 34, 20);
                using (SolidBrush b = new SolidBrush(coat.Pad)) g.FillPath(b, paw);
            }
            using (SolidBrush b = new SolidBrush(Color.FromArgb(115, 255, 252, 246)))
            {
                g.FillEllipse(b, 5f, 50f, 5f, 5f);
                g.FillEllipse(b, 12.5f, 49f, 5f, 5f);
                g.FillEllipse(b, 20f, 50f, 5f, 5f);
            }
            using (Pen pen = new Pen(Color.FromArgb(60, coat.FurDark), 1.5f))
            {
                g.DrawLine(pen, 7, 58, 7, 62);
                g.DrawLine(pen, 14, 59, 14, 63);
                g.DrawLine(pen, 21, 58, 21, 62);
            }
            g.Restore(st);
        }

        private static void DrawHead(Graphics g, CatAnim a, float dip, Coat coat, bool hetero, int accessory)
        {
            GraphicsState st = g.Save();
            // HeadYaw：转头时头和脖子错开一点，避免整颗头平移显得假
            g.TranslateTransform(HeadCx + a.HeadYaw * 1.3f, HeadCy + dip * 3.2f + a.Squash * 6f);
            g.RotateTransform(a.HeadTilt);
            if (a.Squash > 0.01f) g.ScaleTransform(1f + 0.06f * a.Squash, 1f - 0.09f * a.Squash);

            DrawEar(g, -1f, a.EarTwitch, a.EarAngle, coat);
            DrawEar(g, 1f, a.EarTwitch, a.EarAngle, coat);

            using (SolidBrush b = new SolidBrush(coat.Fur))
                g.FillEllipse(b, -56, -50, 112, 102);

            using (Pen pen = new Pen(coat.FurDark, 5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, -10, -45, -10, -28);
                g.DrawLine(pen, 0, -48, 0, -29);
                g.DrawLine(pen, 10, -45, 10, -28);
            }

            using (SolidBrush b = new SolidBrush(coat.Light))
                g.FillEllipse(b, -21, 10, 42, 30);

            Color odd = OddEyeColor(coat.Eye);
            DrawEye(g, a, -1, coat, coat.Eye);
            DrawEye(g, a, 1, coat, hetero ? odd : coat.Eye);

            DrawFace(g, a, coat);

            using (Pen pen = new Pen(Color.FromArgb(80, Ink), 1.5f))
            {
                pen.StartCap = LineCap.Round;
                g.DrawLine(pen, -23, 18, -40, 14);
                g.DrawLine(pen, -23, 23, -41, 24);
                g.DrawLine(pen, 23, 18, 40, 14);
                g.DrawLine(pen, 23, 23, 41, 24);
            }

            if (accessory == 4) DrawGlasses(g);
            g.Restore(st);
        }

        /// <summary>鼻子、嘴、舌头、腮红、眉毛。</summary>
        private static void DrawFace(Graphics g, CatAnim a, Coat coat)
        {
            // 腮红
            if (a.Cheek > 0.02f)
            {
                int alpha = (int)(78f * a.Cheek);
                if (alpha > 90) alpha = 90;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, coat.Cheek)))
                {
                    g.FillEllipse(b, -41f, 6f, 21f, 12f);
                    g.FillEllipse(b, 20f, 6f, 21f, 12f);
                }
            }

            // 鼻子
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] { new PointF(-6.5f, 16f), new PointF(6.5f, 16f), new PointF(0f, 24f) });
                using (SolidBrush b = new SolidBrush(coat.Nose)) g.FillPath(b, p);
            }

            float smile = a.MouthSmile;
            float open = a.MouthOpen;
            using (Pen pen = new Pen(Ink, 1.7f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, 0, 24, 0, 26.5f);
            }

            if (open < 0.1f)
            {
                // 闭嘴：嘴角随 smile 上扬
                float lift = 2.6f * smile;
                using (Pen pen = new Pen(Ink, 1.7f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, -9f, 24.5f - lift, 9f, 7f + lift, 0, 180);
                    g.DrawArc(pen, 0f, 24.5f - lift, 9f, 7f + lift, 0, 180);
                }
            }
            else
            {
                // 张嘴：椭圆口，舌头顶在下唇
                float h = 3f + 13f * open;
                float w = 9f + 5f * open;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(190, 64, 42, 38)))
                    g.FillEllipse(b, -w, 23f, w * 2f, h);
                if (a.Tongue > 0.04f)
                {
                    float tw = 3f + 4.5f * a.Tongue;
                    float th = 2f + 4f * a.Tongue;
                    using (SolidBrush b = new SolidBrush(coat.Pink))
                        g.FillEllipse(b, -tw, 23f + h - th * 0.9f, tw * 2f, th);
                }
            }

            // 眉毛
            if (Math.Abs(a.Brow) > 0.04f)
            {
                float lift = 3.4f * a.Brow;
                using (Pen pen = new Pen(Color.FromArgb(170, Ink.R, Ink.G, Ink.B), 2.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, -34f, -17f - lift, -17f, -20.5f - lift * 0.6f);
                    g.DrawLine(pen, 17f, -20.5f - lift * 0.6f, 34f, -17f - lift);
                }
            }
        }

        /// <summary>异瞳色：原色偏蓝就换成金色，否则换成蓝色，保证两只眼明显不同。</summary>
        private static Color OddEyeColor(Color baseColor)
        {
            if (baseColor.B > baseColor.G + 12) return Color.FromArgb(201, 162, 39);
            return Color.FromArgb(90, 160, 208);
        }

        private static void DrawGlasses(Graphics g)
        {
            using (Pen frame = new Pen(Color.FromArgb(58, 50, 44), 2.6f))
            using (Pen leg = new Pen(Color.FromArgb(58, 50, 44), 2.0f))
            {
                g.DrawEllipse(frame, -24f - 15f, -3f - 15f, 30f, 30f);
                g.DrawEllipse(frame, 24f - 15f, -3f - 15f, 30f, 30f);
                g.DrawLine(leg, -9f, -3f, 9f, -3f);
                g.DrawLine(leg, -39f, -4f, -52f, -9f);
                g.DrawLine(leg, 39f, -4f, 52f, -9f);
            }
        }

        /// <summary>脖子上的配饰：1 铃铛 / 2 围巾 / 3 蝴蝶结（4 眼镜在头部内绘制）。</summary>
        private static void DrawNeckAccessory(Graphics g, int accessory, float dip)
        {
            if (accessory == 1)
            {
                float cy = 170f + dip * 2f;
                using (SolidBrush ring = new SolidBrush(Color.FromArgb(196, 152, 62)))
                    g.FillEllipse(ring, 150f, cy - 14f, 20f, 9f);
                using (SolidBrush bell = new SolidBrush(Color.FromArgb(232, 181, 74)))
                    g.FillEllipse(bell, 149f, cy - 6f, 22f, 22f);
                using (Pen edge = new Pen(Color.FromArgb(178, 132, 48), 1.4f))
                    g.DrawEllipse(edge, 149f, cy - 6f, 22f, 22f);
                using (SolidBrush hl = new SolidBrush(Color.FromArgb(248, 232, 178)))
                    g.FillEllipse(hl, 154f, cy - 1f, 7f, 6f);
                using (Pen slit = new Pen(Color.FromArgb(150, 110, 40), 1.6f))
                    g.DrawLine(slit, 155f, cy + 11f, 165f, cy + 11f);
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(150, 110, 40)))
                    g.FillEllipse(dot, 158f, cy + 13f, 4f, 4f);
            }
            else if (accessory == 2)
            {
                using (GraphicsPath scarf = new GraphicsPath())
                {
                    scarf.AddBezier(124f, 156f, 138f, 176f, 182f, 176f, 196f, 156f);
                    scarf.AddLine(196f, 156f, 196f, 170f);
                    scarf.AddBezier(196f, 170f, 182f, 190f, 138f, 190f, 124f, 170f);
                    scarf.CloseFigure();
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(196, 86, 63))) g.FillPath(b, scarf);
                }
                using (GraphicsPath tail = new GraphicsPath())
                {
                    tail.AddPolygon(new PointF[]
                    {
                        new PointF(186f, 176f), new PointF(206f, 182f),
                        new PointF(200f, 214f), new PointF(182f, 206f)
                    });
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(176, 72, 52))) g.FillPath(b, tail);
                }
                using (Pen stitch = new Pen(Color.FromArgb(232, 156, 132), 1.6f))
                {
                    g.DrawBezier(stitch, 130f, 162f, 144f, 178f, 176f, 178f, 190f, 162f);
                }
            }
            else if (accessory == 3)
            {
                float cy = 168f + dip * 2f;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(226, 122, 142)))
                {
                    GraphicsPath left = new GraphicsPath();
                    left.AddEllipse(134f, cy - 9f, 24f, 18f);
                    g.FillPath(b, left);
                    GraphicsPath right = new GraphicsPath();
                    right.AddEllipse(162f, cy - 9f, 24f, 18f);
                    g.FillPath(b, right);
                }
                using (SolidBrush b = new SolidBrush(Color.FromArgb(198, 92, 112)))
                {
                    g.FillEllipse(b, 153f, cy - 6f, 14f, 12f);
                }
                using (Pen line = new Pen(Color.FromArgb(178, 78, 98), 1.2f))
                {
                    g.DrawEllipse(line, 134f, cy - 9f, 24f, 18f);
                    g.DrawEllipse(line, 162f, cy - 9f, 24f, 18f);
                }
            }
        }

        private static void DrawEar(Graphics g, float side, float twitch, float earAngle, Coat coat)
        {
            GraphicsState st = g.Save();
            g.ScaleTransform(side, 1f);
            // earAngle 正值向后压（折耳），负值向前竖；twitch 是摸头时的小幅抖动
            g.RotateTransform(-twitch * 6f + earAngle * 1.35f);

            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] { new PointF(9, -36), new PointF(30, -80), new PointF(48, -32) });
                using (SolidBrush b = new SolidBrush(coat.Fur)) g.FillPath(b, p);
            }
            using (GraphicsPath inner = new GraphicsPath())
            {
                inner.AddPolygon(new PointF[] { new PointF(19, -41), new PointF(30, -67), new PointF(41, -39) });
                using (SolidBrush b = new SolidBrush(coat.Pink)) g.FillPath(b, inner);
            }
            g.Restore(st);
        }

        private static void DrawEye(Graphics g, CatAnim a, int side, Coat coat, Color eyeColor)
        {
            float cx = 24f * side;
            float cy = -3f;

            // 笑眼：整只眼画成上弯弧，不再画眼球
            if (a.EyeHappy > 0.55f)
            {
                using (Pen pen = new Pen(Ink, 3.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, cx - 11f, cy - 1f, 22f, 16f, 200, 140);
                }
                return;
            }

            float open = a.EyeOpen;
            if (open < 0.03f) open = 0.03f;
            if (open > 1f) open = 1f;

            float wide = a.EyeWide;
            if (wide < 0f) wide = 0f;
            if (wide > 1f) wide = 1f;

            float rx = 14f + 2.6f * wide;
            float ry = 16f * open * (1f + 0.15f * wide);

            using (SolidBrush b = new SolidBrush(eyeColor))
                g.FillEllipse(b, cx - rx, cy - ry, rx * 2f, ry * 2f);

            float px = cx + a.PupilX * 4.5f;
            float py = cy + a.PupilY * 4.5f;
            float prx = 4.6f * (1f - 0.32f * wide);
            float pry = 9f * open * (1f - 0.28f * wide);
            if (pry < 1.4f) pry = 1.4f;
            using (SolidBrush b = new SolidBrush(Ink))
                g.FillEllipse(b, px - prx, py - pry, prx * 2f, pry * 2f);

            using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 255, 255, 250)))
                g.FillEllipse(b, px - 3.6f, py - pry * 0.72f, 3.4f, 3.4f);

            if (wide > 0.3f)
            {
                using (Pen pen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.5f))
                    g.DrawEllipse(pen, cx - rx + 1.2f, cy - ry + 1.2f, rx * 2f - 2.4f, ry * 2f - 2.4f);
            }

            if (open < 0.22f)
            {
                using (Pen pen = new Pen(Ink, 2.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, cx - 10f, cy - 7f, 20f, 13f, 200, 140);
                }
            }
        }

        private static void FillEllipse(Graphics g, Color c, float x, float y, float w, float h)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.FillEllipse(b, x, y, w, h);
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}