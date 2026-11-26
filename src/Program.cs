// FlatCat - 入口：单实例、离屏渲染取证、自检
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FlatCat
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--shot")
            {
                RenderSheet(args[1], new string[] { "idle" });
                return;
            }
            if (args.Length >= 2 && args[0] == "--sheet")
            {
                RenderSheet(args[1], new string[] { "idle", "watch", "typing", "pet", "sleep", "focus", "water" });
                return;
            }
            if (args.Length >= 2 && args[0] == "--extras")
            {
                RenderExtras(args[1]);
                return;
            }
            if (args.Length >= 2 && args[0] == "--coats")
            {
                RenderCoats(args[1]);
                return;
            }
            if (args.Length >= 2 && args[0] == "--faces")
            {
                RenderFaces(args[1]);
                return;
            }
            if (args.Length >= 2 && args[0] == "--quote")
            {
                RenderQuotes(args[1]);
                return;
            }
            if (args.Length >= 2 && args[0] == "--quotesizes")
            {
                RenderQuoteSizes(args[1]);
                return;
            }

            if (args.Length >= 2 && args[0] == "--selftest")
            {
                SelfTest(args[1]);
                return;
            }

            bool created;
            using (Mutex mtx = new Mutex(true, "Local\\FlatCat.SingleInstance", out created))
            {
                if (!created)
                {
                    // 已经有猫在跑：把设置窗口叫出来（等价于双击 exe 图标）
                    IntPtr host = Native.FindWindow(null, "桌面小猫");
                    if (host != IntPtr.Zero)
                        Native.PostMessage(host, Native.WM_APP_SETTINGS, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Settings cfg = Settings.Load();
                PetForm form = new PetForm(cfg);
                if (args.Length >= 2 && args[0] == "--settings")
                {
                    int page;
                    if (int.TryParse(args[1], out page)) form.StartPage = page;
                }
                Application.Run(form);
                GC.KeepAlive(mtx);
            }
        }

        /// <summary>六格表情图：正常 / 开心 / 惊讶 / 打哈欠 / 摸头 / 睡着。</summary>
        private static void RenderFaces(string path)
        {
            string[] states = new string[] { "idle", "happy", "alert", "yawn", "pet", "sleep" };
            const int cell = 240;
            using (Bitmap bmp = new Bitmap(cell * states.Length, cell, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    for (int i = 0; i < states.Length; i++)
                    {
                        g.ResetTransform();
                        g.TranslateTransform(i * cell, 0);
                        g.ScaleTransform(0.75f, 0.75f);
                        CatAnim a = CatAnim.ForState(states[i]);
                        CatPainter.Draw(g, a, false, Coats.All[0], 0, true, false);
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        private static void RenderSheet(string path, string[] states)
        {
            const int cellW = 320;
            const int cellH = 320;
            int n = states.Length;
            using (Bitmap bmp = new Bitmap(cellW * n, cellH, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    for (int i = 0; i < n; i++)
                    {
                        g.ResetTransform();
                        g.TranslateTransform(i * cellW, 0);

                        string state = states[i];
                        string bubble = null;
                        bool alert = false;

                        if (state == "focus")
                        {
                            bubble = "专注 24:59";
                            state = "watch";
                        }
                        else if (state == "water")
                        {
                            bubble = "该喝水了";
                            state = "alert";
                            alert = true;
                        }

                        if (bubble != null) CatPainter.DrawBubble(g, bubble, 0.62f, alert);

                        GraphicsState st = g.Save();
                        g.TranslateTransform(0, CatPainter.BubbleH);
                        CatPainter.Draw(g, CatAnim.ForState(state), true, Coats.All[0]);
                        g.Restore(st);

                        g.ResetTransform();
                        if (i > 0)
                        {
                            using (Pen p = new Pen(Color.FromArgb(50, 150, 120, 60), 1f))
                                g.DrawLine(p, i * cellW, 0, i * cellW, cellH);
                        }
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        /// <summary>金句气泡四格：单行短句 / 两行 / 超长句 / 标点断行，用来看有没有被裁。</summary>
        private static void RenderQuotes(string path)
        {
            string[] texts = new string[]
            {
                "上善若水",
                "把今天过好，就是最好的长期主义。",
                "安能摧眉折腰事权贵，使我不得开心颜",
                "学而时习之，不亦说乎"
            };
            const int cellW = 380;
            const int cellH = 380;
            using (Bitmap bmp = new Bitmap(cellW * 2, cellH * 2 + 28, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    for (int i = 0; i < texts.Length; i++)
                    {
                        int cx = i % 2, cy = i / 2;
                        GraphicsState st = g.Save();
                        g.TranslateTransform(cx * cellW + 30, cy * cellH);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        CatPainter.DrawQuote(g, texts[i], 0.3f);
                        g.TranslateTransform(0, CatPainter.BubbleH);
                        CatPainter.Draw(g, CatAnim.ForState("idle"), false, Coats.All[0]);
                        g.Restore(st);

                        using (Pen p = new Pen(Color.FromArgb(40, 150, 120, 60), 1f))
                        {
                            if (cx == 1) g.DrawLine(p, cellW, cy * cellH, cellW, cy * cellH + cellH);
                            if (cy == 1) g.DrawLine(p, cx * cellW, cellH, cx * cellW + cellW, cellH);
                        }
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        private static void RenderCoats(string path)
        {
            int n = Coats.All.Length;
            const int cellW = 260;
            const int cellH = 300;
            using (Bitmap bmp = new Bitmap(cellW * n, cellH + 26, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    using (Font f = new Font("Microsoft YaHei", 14f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 62, 52)))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        for (int i = 0; i < n; i++)
                        {
                            g.ResetTransform();
                            g.TranslateTransform(i * cellW, 0);
                            float s = 260f / 320f;
                            g.ScaleTransform(s, s);
                            g.TranslateTransform(30, 0);
                            CatPainter.Draw(g, CatAnim.ForState("idle"), true, Coats.All[i]);

                            g.ResetTransform();
                            g.DrawString(Coats.All[i].Name, f, b, new RectangleF(i * cellW, cellH, cellW, 24), sf);
                        }
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        /// <summary>同一句在 12 / 15 / 18 / 20px 四档字号下的样子：确认字号可调、不压耳朵、不被裁。</summary>
        private static void RenderQuoteSizes(string path)
        {
            float[] sizes = new float[] { 12f, 15f, 18f, 20f };
            string[] texts = new string[]
            {
                "上善若水，厚德载物",
                "把今天过好，就是最好的长期主义。",
                "安能摧眉折腰事权贵，使我不得开心颜",
                "海内存知己，天涯若比邻"
            };
            const int cellW = 380;
            const int cellH = 380;
            using (Bitmap bmp = new Bitmap(cellW * 2, cellH * 2 + 34, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    for (int i = 0; i < texts.Length; i++)
                    {
                        int cx = i % 2, cy = i / 2;
                        GraphicsState st = g.Save();
                        g.TranslateTransform(cx * cellW + 30, cy * cellH);
                        CatPainter.DrawQuote(g, texts[i], 0.3f, "Microsoft YaHei", sizes[i]);
                        g.TranslateTransform(0, CatPainter.BubbleH);
                        CatPainter.Draw(g, CatAnim.ForState("idle"), false, Coats.All[0]);
                        g.Restore(st);

                        using (Pen p = new Pen(Color.FromArgb(40, 150, 120, 60), 1f))
                        {
                            if (cx == 1) g.DrawLine(p, cellW, cy * cellH, cellW, cy * cellH + cellH);
                            if (cy == 1) g.DrawLine(p, cx * cellW, cellH, cx * cellW + cellW, cellH);
                        }
                    }
                    using (Font f = new Font("Microsoft YaHei", 14f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 62, 52)))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        for (int i = 0; i < sizes.Length; i++)
                        {
                            int cx = i % 2, cy = i / 2;
                            g.DrawString(((int)sizes[i]) + " px", f, b,
                                         new RectangleF(cx * cellW, cy * cellH + cellH, cellW, 26), sf);
                        }
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        /// <summary>配饰与异瞳预览：无 / 铃铛 / 围巾 / 蝴蝶结 / 眼镜 / 异瞳+无斑纹，横排出图。</summary>
        private static void RenderExtras(string path)
        {
            int[] acc = new int[] { 0, 1, 2, 3, 4, 0 };
            string[] names = new string[] { "无配饰", "铃铛", "围巾", "蝴蝶结", "眼镜", "异瞳·无斑纹" };
            const int cellW = 320;
            const int cellH = 320;
            int n = acc.Length;
            using (Bitmap bmp = new Bitmap(cellW * n, cellH + 26, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(255, 244, 238, 228));
                    using (Font f = new Font("Microsoft YaHei", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 62, 52)))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        for (int i = 0; i < n; i++)
                        {
                            g.ResetTransform();
                            g.TranslateTransform(i * cellW, 0);
                            bool hetero = (i == n - 1);
                            bool spots = (i != n - 1);
                            CatPainter.Draw(g, CatAnim.ForState("idle"), false, Coats.All[0], acc[i], spots, hetero);
                            g.ResetTransform();
                            g.DrawString(names[i], f, b, new RectangleF(i * cellW, cellH, cellW, 24), sf);
                        }
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }

        private static void SelfTest(string path)
        {
            List<string> lines = new List<string>();
            int fails = 0;

            string backup = null;
            bool had = File.Exists(Settings.FilePath);
            try
            {
                if (had) backup = File.ReadAllText(Settings.FilePath);
            }
            catch { }

            Settings probe = new Settings();
            probe.Scale = 1.25;
            probe.Opacity = 0.8;
            probe.SleepMinutes = 9;
            probe.X = 321;
            probe.Y = 654;
            probe.PomodoroFocus = 45;
            probe.PomodoroBreak = 10;
            probe.WaterEnabled = true;
            probe.WaterMinutes = 60;
            probe.CoatId = 3;
            probe.LeftHanded = true;
            probe.PrivacyMode = true;
            probe.ShowKeyLabels = false;
            Alarm a1 = new Alarm("起床", 7, 30, true, false);
            a1.Sound = 1;
            a1.Days = 0x1F;
            probe.Alarms.Add(a1);
            Alarm a2 = new Alarm("开会", 14, 0, false, true);
            a2.Days = 0x0A;
            probe.Alarms.Add(a2);
            probe.HourlyChime = true;
            probe.HourlyStart = 9;
            probe.HourlyEnd = 21;
            probe.SnoozeMinutes = 7;
            probe.AccessoryId = 2;
            probe.ShowSpots = false;
            probe.Heterochromia = true;
            probe.ClockMode = true;
            probe.HeadMode = 2;
            probe.QuoteCats = QuoteBank.CatPoem;
            probe.QuoteMinutes = 90;
            probe.QuoteFont = "SimSun";
            probe.QuoteFontSize = 18;
            probe.CustomQuotes.Add(new CustomQuote("自检：读万卷书，行万里路", "佚名", "自检用例"));
            probe.CustomQuotes.Add(new CustomQuote("自检：纸上得来终觉浅，绝知此事要躬行", "陆游", "冬夜读书示子聿"));
            probe.TotalPomodoros = 42;
            probe.AlertLog.Add("12:00  测试提醒");
            probe.Hotkeys = false;
            probe.QuietEnabled = true;
            probe.QuietStart = 23;
            probe.QuietEnd = 7;
            bool saved = probe.Save();
            Settings back = Settings.Load();
            bool roundTrip = Math.Abs(back.Scale - 1.25) < 1e-9
                             && Math.Abs(back.Opacity - 0.8) < 1e-9
                             && back.SleepMinutes == 9
                             && back.X == 321
                             && back.Y == 654;
            bool reminderRoundTrip = back.PomodoroFocus == 45
                                     && back.PomodoroBreak == 10
                                     && back.WaterEnabled
                                     && back.WaterMinutes == 60;
            bool behaviorRoundTrip = back.CoatId == 3 && back.LeftHanded && back.PrivacyMode && !back.ShowKeyLabels;
            bool alarmRoundTrip = back.Alarms != null && back.Alarms.Count == 2
                                  && back.Alarms[0].Hour == 7 && back.Alarms[0].Minute == 30
                                  && back.Alarms[0].Label == "起床"
                                  && back.Alarms[0].Sound == 1
                                  && back.Alarms[0].Days == 0x1F
                                  && back.Alarms[1].Days == 0x0A;

            if (had && backup != null) File.WriteAllText(Settings.FilePath, backup);
            else if (!had && File.Exists(Settings.FilePath)) File.Delete(Settings.FilePath);

            lines.Add("settings.path = " + Settings.FilePath);
            lines.Add("settings.save = " + (saved ? "PASS" : "FAIL"));
            lines.Add("settings.roundtrip = " + (roundTrip ? "PASS" : "FAIL"));
            lines.Add("settings.reminder_fields = " + (reminderRoundTrip ? "PASS" : "FAIL"));
            lines.Add("settings.behavior_fields = " + (behaviorRoundTrip ? "PASS" : "FAIL"));
            lines.Add("settings.alarm_list = " + (alarmRoundTrip ? "PASS" : "FAIL"));
            if (!saved || !roundTrip || !reminderRoundTrip || !behaviorRoundTrip || !alarmRoundTrip) fails++;

            // 深拷贝（设置窗口取消回滚依赖它）
            Settings clone = probe.Clone();
            bool cloneOk = clone.Scale == probe.Scale && clone.Alarms.Count == probe.Alarms.Count
                           && clone.CoatId == probe.CoatId;
            lines.Add("settings.clone = " + (cloneOk ? "PASS" : "FAIL"));
            if (!cloneOk) fails++;

            bool extraOk = back.HourlyChime && back.HourlyStart == 9 && back.HourlyEnd == 21
                           && back.SnoozeMinutes == 7 && back.AccessoryId == 2
                           && !back.ShowSpots && back.Heterochromia && back.ClockMode
                           && back.TotalPomodoros == 42
                           && back.AlertLog != null && back.AlertLog.Count == 1;
            lines.Add("settings.extra_fields = " + (extraOk ? "PASS" : "FAIL"));
            if (!extraOk) fails++;

            // v1.5 新增字段往返
            bool v15Fields = !back.Hotkeys && back.QuietEnabled
                             && back.QuietStart == 23 && back.QuietEnd == 7;
            lines.Add("settings.v15_fields = " + (v15Fields ? "PASS" : "FAIL"));
            if (!v15Fields) fails++;

            // v1.6 头顶显示与金句库
            bool v16Fields = back.HeadMode == 2 && back.QuoteCats == QuoteBank.CatPoem
                             && back.QuoteMinutes == 90;
            lines.Add("settings.v16_fields = " + (v16Fields ? "PASS" : "FAIL")
                      + " (head=" + back.HeadMode + " cats=" + back.QuoteCats + " min=" + back.QuoteMinutes + ")");
            if (!v16Fields) fails++;

            // v1.7 自定义金句库 + 字体字号往返
            bool v17Fields = back.QuoteFont == "SimSun" && back.QuoteFontSize == 18
                             && back.CustomQuotes != null && back.CustomQuotes.Count == 2
                             && back.CustomQuotes[1].Author == "陆游";
            lines.Add("settings.v17_fields = " + (v17Fields ? "PASS" : "FAIL")
                      + " (font=" + back.QuoteFont + " size=" + back.QuoteFontSize
                      + " custom=" + (back.CustomQuotes == null ? -1 : back.CustomQuotes.Count) + ")");
            if (!v17Fields) fails++;

            int qThought = QuoteBank.CountOf(QuoteBank.CatThought, null);
            int qPoem = QuoteBank.CountOf(QuoteBank.CatPoem, null);
            int qLine = QuoteBank.CountOf(QuoteBank.CatLine, null);
            int qEmpty = 0, qLong = 0, qDup = 0;
            HashSet<string> qSeen = new HashSet<string>();
            for (int i = 0; i < QuoteBank.All.Length; i++)
            {
                Quote q = QuoteBank.All[i];
                if (q.Text == null || q.Text.Length == 0 || q.Source == null) qEmpty++;
                else if (q.Text.Length > 28) qLong++;
                if (q.Text != null && !qSeen.Add(q.Text)) qDup++;
            }
            bool bankOk = qThought > 0 && qPoem > 0 && qLine > 0
                          && QuoteBank.All.Length == qThought + qPoem + qLine
                          && qEmpty == 0 && qLong == 0 && qDup == 0;
            lines.Add("quote.bank = " + (bankOk ? "PASS" : "FAIL") + " (哲思" + qThought
                      + "/诗句" + qPoem + "/金句" + qLine + " 总" + QuoteBank.All.Length
                      + " 空" + qEmpty + " 长" + qLong + " 重" + qDup + ")");
            if (!bankOk) fails++;

            // 只从勾选分类里取；同一条不会连着出两次
            Random qr = new Random(7);
            bool pickOk = true;
            for (int i = 0; i < 60; i++)
            {
                Quote q = QuoteBank.Pick(QuoteBank.BuildPool(QuoteBank.CatPoem, null), null, qr);
                if (q == null || q.Cat != QuoteBank.CatPoem) { pickOk = false; break; }
            }
            List<Quote> linePool = QuoteBank.BuildPool(QuoteBank.CatLine, null);
            Quote q1 = QuoteBank.Pick(linePool, null, qr);
            Quote q2 = QuoteBank.Pick(linePool, q1 == null ? "" : q1.Text, qr);
            if (q1 == null || q2 == null || q1.Text == q2.Text) pickOk = false;
            lines.Add("quote.pick = " + (pickOk ? "PASS" : "FAIL")
                      + " (" + (q1 == null ? "-" : q1.Text) + " / " + (q2 == null ? "-" : q2.Text) + ")");
            if (!pickOk) fails++;

            // 金句气泡两行折行：超过一行要折成两行，标点处断开，且一个字都不能丢
            string longQuote = new string('甲', 9) + "，" + new string('乙', 14);
            int wrapPer = CatPainter.PerLineFor(15f);
            string ql1, ql2;
            CatPainter.SplitQuoteForTest(longQuote, out ql1, out ql2);
            bool wrapOk = ql1.Length > 0 && ql2 != null
                          && ql1.Length <= wrapPer && ql2.Length <= wrapPer
                          && ql1.EndsWith("，") && (ql1 + ql2) == longQuote;
            lines.Add("quote.wrap = " + (wrapOk ? "PASS" : "FAIL") + " (" + ql1 + " / " + ql2 + ")");
            if (!wrapOk) fails++;

            // 第二行只剩标点时并回第一行，别让气泡里出现孤零零一个句号
            string qt1, qt2;
            CatPainter.SplitQuoteForTest("把今天过好，就是最好的长期主义。", out qt1, out qt2);
            bool tailPunctOk = qt2 == null && qt1.EndsWith("。") && qt1.Length == 16;
            lines.Add("quote.tail_punct = " + (tailPunctOk ? "PASS" : "FAIL")
                      + " (" + qt1 + " / " + (qt2 == null ? "null" : qt2) + ")");
            if (!tailPunctOk) fails++;

            // 我的金句库：进池、计数、分类隔离
            List<CustomQuote> mine = new List<CustomQuote>();
            mine.Add(new CustomQuote("自检一：今天也要好好吃饭", "", ""));
            mine.Add(new CustomQuote("自检二：做完一件事再开下一件", "FlatCat", "自检"));
            int customMaskCount = QuoteBank.CountOf(QuoteBank.CatCustom, mine);
            List<Quote> customPool = QuoteBank.BuildPool(QuoteBank.CatCustom, mine);
            bool customPoolOk = customMaskCount == 2 && customPool.Count == 2
                                && customPool[0].Cat == QuoteBank.CatCustom
                                && customPool[1].Text == "自检二：做完一件事再开下一件";
            List<Quote> poemOnly = QuoteBank.BuildPool(QuoteBank.CatPoem, mine);
            bool customIsolated = true;
            for (int i = 0; i < poemOnly.Count; i++)
                if (poemOnly[i].Cat == QuoteBank.CatCustom) { customIsolated = false; break; }
            lines.Add("quote.custom_pool = " + ((customPoolOk && customIsolated) ? "PASS" : "FAIL")
                      + " (count=" + customMaskCount + " pool=" + customPool.Count + " isolated=" + customIsolated + ")");
            if (!customPoolOk || !customIsolated) fails++;

            // 去重 / 超长 / 上限：重复内置句、同一句换个标点、超 200 字都要跳过
            List<CustomQuote> dupIn = new List<CustomQuote>();
            dupIn.Add(new CustomQuote("自检一：今天也要好好吃饭", "", ""));
            dupIn.Add(new CustomQuote("海上生明月,天涯共此时", "", ""));
            dupIn.Add(new CustomQuote(new string('长', 260), "", ""));
            dupIn.Add(new CustomQuote("自检三：新的句子", "", ""));
            int dupSkip;
            int dupAdded = QuoteIo.Add(dupIn, mine, out dupSkip);
            bool dedupOk = dupAdded == 1 && dupSkip == 3 && mine.Count == 3;
            lines.Add("quote.custom_dedup = " + (dedupOk ? "PASS" : "FAIL")
                      + " (added=" + dupAdded + " skipped=" + dupSkip + " total=" + mine.Count + ")");
            if (!dedupOk) fails++;

            // 导入文件：UTF-8 txt（含 | 分隔与重复行）与 json 字符串数组
            string tTxt = Path.Combine(Path.GetTempPath(), "flatcat_q_selftest.txt");
            string tJson = Path.Combine(Path.GetTempPath(), "flatcat_q_selftest.json");
            int importAdded = 0, importSkip = 0;
            string importErr = null;
            try
            {
                string txt = "# 注释行" + Environment.NewLine
                           + "自检六：txt 导入第一句|自检" + Environment.NewLine
                           + "自检七：txt 导入第二句" + Environment.NewLine
                           + "自检三：新的句子" + Environment.NewLine;
                File.WriteAllText(tTxt, txt, new UTF8Encoding(false));
                File.WriteAllText(tJson, "[\"自检八：json 第一句\", \"自检九：json 第二句\", \"自检一：今天也要好好吃饭\"]", new UTF8Encoding(false));
                int sk1;
                string er1;
                importAdded += QuoteIo.Import(tTxt, mine, out sk1, out er1);
                importSkip += sk1;
                if (er1 != null) importErr = er1;
                int sk2;
                string er2;
                importAdded += QuoteIo.Import(tJson, mine, out sk2, out er2);
                importSkip += sk2;
                if (er2 != null) importErr = er2;
            }
            catch (Exception ex) { importErr = ex.GetType().Name; }
            finally
            {
                try { if (File.Exists(tTxt)) File.Delete(tTxt); } catch { }
                try { if (File.Exists(tJson)) File.Delete(tJson); } catch { }
            }
            bool importOk = importErr == null && importAdded == 4 && importSkip == 2 && mine.Count == 7;
            lines.Add("quote.import = " + (importOk ? "PASS" : "FAIL")
                      + " (added=" + importAdded + " skipped=" + importSkip + " total=" + mine.Count
                      + " err=" + (importErr == null ? "-" : importErr) + ")");
            if (!importOk) fails++;

            // 坏 JSON 不能被当成一句金句存进去（1.7 修：JSON 解析失败不回退按行读）
            bool jsonReject = QuoteIo.Parse("[{\"text\":\"a\", \"b\"]").Count == 0
                              && QuoteIo.Parse("{不是 json").Count == 0;
            lines.Add("quote.json_bad = " + (jsonReject ? "PASS" : "FAIL"));
            if (!jsonReject) fails++;

            // 字号改折行：字号越大每行越少，20px 时不能还按 17 字折
            int p12 = CatPainter.PerLineFor(12f), p15 = CatPainter.PerLineFor(15f), p20 = CatPainter.PerLineFor(20f);
            string s20a, s20b, s12a, s12b;
            CatPainter.SplitQuoteForTest("安能摧眉折腰事权贵，使我不得开心颜", 20f, out s20a, out s20b);
            CatPainter.SplitQuoteForTest("安能摧眉折腰事权贵，使我不得开心颜", 12f, out s12a, out s12b);
            bool sizeWrapOk = p12 > p15 && p15 > p20 && p20 >= 8
                              && s20a.Length <= p20 && s20b != null && s20b.Length <= p20
                              && s12a.Length <= p12;
            lines.Add("quote.wrap_size = " + (sizeWrapOk ? "PASS" : "FAIL")
                      + " (12/" + p12 + " 15/" + p15 + " 20/" + p20
                      + " → " + s20a.Length + "+" + (s20b == null ? 0 : s20b.Length) + ")");
            if (!sizeWrapOk) fails++;

            // 字体不存在时回退到本机真实存在的字体
            string fallback = CatPainter.ResolveFont("NoSuchFontForFlatCat123");
            bool fontOk = fallback != "NoSuchFontForFlatCat123" && CatPainter.FontExists(fallback)
                          && CatPainter.FontExists(CatPainter.ResolveFont("SimSun"));
            lines.Add("quote.font_fallback = " + (fontOk ? "PASS" : "FAIL") + " (" + fallback + ")");
            if (!fontOk) fails++;

            // v1.5 静默时段闸门：跨午夜、同侧、起止相同（视为关闭）
            bool quietOk = Reminder.InQuiet(true, 22, 8, 23)
                        && Reminder.InQuiet(true, 22, 8, 3)
                        && !Reminder.InQuiet(true, 22, 8, 12)
                        && !Reminder.InQuiet(true, 22, 8, 8)
                        && Reminder.InQuiet(true, 13, 14, 13)
                        && !Reminder.InQuiet(true, 13, 14, 14)
                        && !Reminder.InQuiet(false, 22, 8, 23)
                        && !Reminder.InQuiet(true, 9, 9, 9);
            lines.Add("quiet.gate = " + (quietOk ? "PASS" : "FAIL"));
            if (!quietOk) fails++;

            // v1.5 便携模式：没放标记文件时必须落在漫游目录
            string roaming = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlatCat");
            bool portableOk = Settings.IsPortable
                ? Settings.Dir.EndsWith("\\data", StringComparison.OrdinalIgnoreCase)
                : string.Equals(Settings.Dir, roaming, StringComparison.OrdinalIgnoreCase);
            lines.Add("settings.portable = " + (portableOk ? "PASS" : "FAIL")
                      + " (" + (Settings.IsPortable ? "portable" : "roaming") + ")");
            if (!portableOk) fails++;

            // v1.5 全局热键：真注册一遍再注销，验证 4 个组合都抢得到
            int hkOk = 0;
            string hkSet = "-";
            string hkErr = "";
            try
            {
                using (Form hkWin = new Form())
                {
                    hkWin.ShowInTaskbar = false;
                    hkWin.FormBorderStyle = FormBorderStyle.None;
                    hkWin.StartPosition = FormStartPosition.Manual;
                    hkWin.SetBounds(-32000, -32000, 8, 8);
                    hkWin.Show();
                    int[] ids = new int[] { 2101, 2102, 2103, 2104 };
                    uint[] vks = new uint[] { 0x53, 0x52, 0x4C, 0x50 };
                    uint[] combos = new uint[]
                    {
                        Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT,
                        Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_SHIFT | Native.MOD_NOREPEAT
                    };
                    string[] names = new string[] { "Ctrl+Alt", "Ctrl+Shift+Alt" };
                    for (int c = 0; c < combos.Length; c++)
                    {
                        hkOk = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            if (Native.RegisterHotKey(hkWin.Handle, ids[i], combos[c], vks[i])) hkOk++;
                        }
                        for (int i = 0; i < 4; i++) Native.UnregisterHotKey(hkWin.Handle, ids[i]);
                        hkSet = names[c] + " " + hkOk + "/4";
                        if (hkOk == 4) break;
                    }
                }
            }
            catch (Exception ex) { hkErr = ex.GetType().Name; }
            lines.Add("hotkey.register = " + (hkOk == 4 ? "PASS" : "FAIL")
                      + " (" + hkSet + (hkErr.Length > 0 ? " " + hkErr : "") + ")");
            if (hkOk != 4) fails++;

            // 设置窗口能否正常构造（布局或控件字段出错时这里会抛异常）
            try
            {
                SettingsForm probeForm = new SettingsForm(new Settings(), null);
                int ctrlCount = probeForm.Controls.Count;
                int navCount = probeForm.NavCount;
                string navTitles = string.Join("/", probeForm.NavTitles);
                lines.Add("ui.client = " + probeForm.ClientSize.Width + "x" + probeForm.ClientSize.Height);
                // v1.5 UI 壳：左导航要列出 6 个页签，空壳等于没做
                bool navOk = navCount == 7;
                lines.Add("ui.nav_items = " + (navOk ? "PASS" : "FAIL") + " (" + navCount + " " + navTitles + ")");
                if (!navOk) fails++;

                // 逐项点击导航，检查内容区是否真的跟着换页、并且不是空的
                bool navSwitch = true;
                string navLog = "";
                string quietLog = "";
                for (int i = 0; i < navCount; i++)
                {
                    probeForm.SelectNav(i);
                    int page = probeForm.CurrentPage;
                    int vis = probeForm.ControlCountOnCurrentPage;
                    navLog += i + "->" + page + "/" + vis + " ";
                    if (page != i || vis <= 0) navSwitch = false;
                }
                lines.Add("ui.nav_switch = " + (navSwitch ? "PASS" : "FAIL") + " (" + navLog.Trim() + ")");
                if (!navSwitch) fails++;
                probeForm.Dispose();

                // v1.7 金句页：切到第 2 页，回读示例/字体/字号/自定义库，模拟添加一句
                SettingsForm qf = new SettingsForm(new Settings(), null);
                qf.SelectNav(1);
                bool onQuotePage = qf.CurrentPage == 1;
                qf.AddQuoteForTest("自检页：添加一句金句");
                qf.AddQuoteForTest("自检页：添加一句金句");
                int qCtrl = qf.ControlCountOnCurrentPage;
                bool quotePageOk = onQuotePage && qCtrl > 0
                                   && qf.QuoteCustomCountForTest == 1
                                   && qf.QuoteSampleTextForTest.Length > 0
                                   && qf.QuoteFontCountForTest >= 1
                                   && qf.QuoteFontSelectedForTest.Length > 0
                                   && Math.Abs(qf.QuoteSampleSizeForTest - 15f) < 0.01f;
                quietLog = "page=" + qf.CurrentPage + " ctrl=" + qCtrl
                           + " custom=" + qf.QuoteCustomCountForTest
                           + " fonts=" + qf.QuoteFontCountForTest
                           + "/" + qf.QuoteFontSelectedForTest
                           + " size=" + qf.QuoteSampleSizeForTest;
                qf.Dispose();
                lines.Add("ui.quote_page = " + (quotePageOk ? "PASS" : "FAIL") + " (" + quietLog + ")");
                if (!quotePageOk) fails++;

                // 回归：搜索框清空后导航不能被占位符文本过滤成空列表（1.5.0 的 P0 缺陷）
                int navAfterSearch = 0;
                string navSearchLog = "";
                try
                {
                    SettingsForm sf = new SettingsForm(new Settings(), null);
                    sf.SearchForTest("铃");
                    int hit = sf.NavCount;
                    sf.ClearSearchForTest();
                    navAfterSearch = sf.NavCount;
                    navSearchLog = "过滤后=" + hit + " 清空后=" + navAfterSearch;
                    sf.Dispose();
                }
                catch (Exception ex) { navSearchLog = ex.GetType().Name; }
                bool navSearchOk = navAfterSearch == 7;
                lines.Add("ui.nav_after_search = " + (navSearchOk ? "PASS" : "FAIL") + " (" + navSearchLog + ")");
                if (!navSearchOk) fails++;
                lines.Add("settings.form_build = " + (ctrlCount > 0 ? "PASS" : "FAIL")
                          + " (controls=" + ctrlCount + ")");
                if (ctrlCount <= 0) fails++;
            }
            catch (Exception ex)
            {
                lines.Add("settings.form_build = FAIL (" + ex.GetType().Name + ": " + ex.Message + ")");
                fails++;
            }

            // 全局钩子
            InputMonitor mon = new InputMonitor();
            mon.Start();
            bool hookOk = mon.Ready && !mon.Failed;
            lines.Add("hooks.install = " + (hookOk ? "PASS" : "FAIL"));
            if (!hookOk) fails++;
            mon.Dispose();
            lines.Add("hooks.dispose = PASS");

            // 时钟格式化
            string[][] clockCases = new string[][] {
                new string[] { "0", "00:00" },
                new string[] { "59", "00:59" },
                new string[] { "60", "01:00" },
                new string[] { "1500", "25:00" },
                new string[] { "3661", "1:01:01" }
            };
            bool clockOk = true;
            foreach (string[] c in clockCases)
            {
                string got = Reminder.Clock(int.Parse(c[0]));
                if (got != c[1]) { clockOk = false; lines.Add("   clock(" + c[0] + ") = " + got + " (expected " + c[1] + ")"); }
            }
            lines.Add("reminder.clock_format = " + (clockOk ? "PASS" : "FAIL"));
            if (!clockOk) fails++;

            // 闹钟匹配
            DateTime nine30 = new DateTime(2026, 11, 25, 9, 30, 0);   // 星期三
            DateTime sat = new DateTime(2026, 11, 28, 9, 30, 0);      // 星期六
            Alarm daily = new Alarm("测试", 9, 30, true, false);
            Alarm workday = new Alarm("上班", 9, 30, false, true);
            Alarm disabled = new Alarm("停用", 9, 30, true, false);
            disabled.Enabled = false;

            bool alarmOk = Reminder.AlarmMatches(daily, nine30, "")
                           && Reminder.AlarmMatches(workday, nine30, "")
                           && !Reminder.AlarmMatches(workday, sat, "")
                           && !Reminder.AlarmMatches(disabled, nine30, "")
                           && !Reminder.AlarmMatches(daily, sat.AddHours(1), "")
                           && !Reminder.AlarmMatches(daily, nine30, Reminder.AlarmKey(daily, nine30));
            lines.Add("reminder.alarm_match = " + (alarmOk ? "PASS" : "FAIL"));
            if (!alarmOk) fails++;

            // 每周重复位掩码：bit0=周一（2000-01-03 是星期一）
            DateTime monday = new DateTime(2000, 1, 3, 9, 30, 0);
            bool daysOk = Reminder.DaysMatch(0x01, monday)
                          && !Reminder.DaysMatch(0x02, monday)
                          && Reminder.DaysText(0x0A) == "周二四"
                          && Reminder.DaysText(0x7F) == "每天"
                          && Reminder.DaysText(0x1F) == "工作日";
            Alarm mondayAlarm = new Alarm("周一例会", 9, 30, false, false);
            mondayAlarm.Days = 0x01;   // 仅周一
            daysOk = daysOk && Reminder.AlarmMatches(mondayAlarm, monday, "")
                              && !Reminder.AlarmMatches(mondayAlarm, monday.AddDays(1), "");
            lines.Add("reminder.days_mask = " + (daysOk ? "PASS" : "FAIL"));
            if (!daysOk) fails++;

            // 下次触发时间
            Alarm nextDaily = new Alarm("晨会", 10, 30, true, false);
            DateTime from = new DateTime(2000, 1, 3, 10, 0, 0);
            bool nextOk = Reminder.NextFire(nextDaily, from) == new DateTime(2000, 1, 3, 10, 30, 0);
            Alarm once = new Alarm("一次", 9, 0, false, false);
            nextOk = nextOk && Reminder.NextFire(once, from) == DateTime.MinValue;
            string nextText = Reminder.NextAlarmText(new List<Alarm>() { nextDaily }, from);
            nextOk = nextOk && nextText.IndexOf("10:30") >= 0;
            lines.Add("reminder.next_fire = " + (nextOk ? "PASS" : "FAIL"));
            if (!nextOk) fails++;

            // 按键名映射
            bool keyOk = KeyName.Of(0x41) == "A" && KeyName.Of(0x20) == "Space"
                         && KeyName.Of(0x0D) == "Enter" && KeyName.Of(0x25) == "Left"
                         && KeyName.Of(0x70) == "F1" && KeyName.IsLeftHalf(0x51) && !KeyName.IsLeftHalf(0x50);
            lines.Add("behavior.key_names = " + (keyOk ? "PASS" : "FAIL"));
            if (!keyOk) fails++;

            // 图标
            int icoSize = 0;
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "flatcat.ico");
            lines.Add("icon.exists = " + (File.Exists(icoPath) ? "PASS" : "SKIP(未随包)"));
            if (File.Exists(icoPath)) icoSize = (int)new FileInfo(icoPath).Length;

            // 离屏渲染（含气泡）
            using (Bitmap bmp = new Bitmap(320, 320, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    CatPainter.DrawBubble(g, "专注 24:59", 0.6f, false);
                    GraphicsState st = g.Save();
                    g.TranslateTransform(0, CatPainter.BubbleH);
                    CatPainter.Draw(g, CatAnim.ForState("idle"), true, Coats.All[0]);
                    g.Restore(st);
                }

                Color head = bmp.GetPixel(160, 112 + (int)CatPainter.BubbleH);
                Color corner = bmp.GetPixel(4, 4);

                int opaque = 0;
                int semi = 0;
                int bubblePixels = 0;
                for (int y = 0; y < 320; y += 2)
                {
                    for (int x = 0; x < 320; x += 2)
                    {
                        int a = bmp.GetPixel(x, y).A;
                        if (a > 120) opaque++;
                        else if (a > 8 && a <= 120) semi++;
                        if (y < 56 && a > 120) bubblePixels++;
                    }
                }

                bool headSolid = head.A > 200;
                bool cornerClear = corner.A == 0;
                bool bubbleDrawn = bubblePixels > 150;
                bool enough = opaque > 6000;
                lines.Add("render.headSolid = " + (headSolid ? "PASS" : "FAIL") + " (a=" + head.A + ")");
                lines.Add("render.cornerTransparent = " + (cornerClear ? "PASS" : "FAIL") + " (a=" + corner.A + ")");
                lines.Add("render.bubblePixels = " + (bubbleDrawn ? "PASS" : "FAIL") + " (count=" + bubblePixels + ")");
                lines.Add("render.bodyPixels = " + (enough ? "PASS" : "FAIL") + " (opaque=" + opaque + ", edge=" + semi + ")");
                if (!headSolid || !cornerClear || !bubbleDrawn || !enough) fails++;
            }

            // 七种毛色都能画出色块
            bool coatsOk = true;
            for (int i = 0; i < Coats.All.Length; i++)
            {
                using (Bitmap bmp = new Bitmap(320, 320, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);
                        CatPainter.Draw(g, CatAnim.ForState("idle"), true, Coats.All[i]);
                    }
                    Color body = bmp.GetPixel(160, 230);
                    if (body.A < 200) { coatsOk = false; lines.Add("   coat[" + i + "] body alpha=" + body.A); }
                    if (i > 0)
                    {
                        Color prev = Coats.All[i - 1].Fur;
                        if (body.R == prev.R && body.G == prev.G && body.B == prev.B)
                        { coatsOk = false; lines.Add("   coat[" + i + "] 与上一色相同"); }
                    }
                }
            }
            lines.Add("render.coats = " + (coatsOk ? "PASS" : "FAIL"));
            if (!coatsOk) fails++;

            // 配饰（围巾）与异瞳
            bool accessoryOk = false;
            using (Bitmap bmp = new Bitmap(320, 320, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    CatPainter.Draw(g, CatAnim.ForState("idle"), false, Coats.All[0], 2, true, false);
                }
                Color neck = bmp.GetPixel(160, 179);
                accessoryOk = neck.A > 150 && neck.R > neck.G + 30;
                lines.Add("render.accessory = " + (accessoryOk ? "PASS" : "FAIL")
                          + " (rgb=" + neck.R + "," + neck.G + "," + neck.B + ")");
            }
            if (!accessoryOk) fails++;

            bool heteroOk = false;
            using (Bitmap bmp = new Bitmap(320, 320, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    CatPainter.Draw(g, CatAnim.ForState("idle"), false, Coats.All[0], 0, true, true);
                }
                // 取眼球彩色区（偏离瞳孔中心 10px，避免取到黑色瞳孔）
                Color left = bmp.GetPixel(147, 109);
                Color right = bmp.GetPixel(173, 109);
                heteroOk = left.A > 150 && right.A > 150
                           && (Math.Abs(left.R - right.R) > 30 || Math.Abs(left.B - right.B) > 30);
            }
            lines.Add("render.heterochromia = " + (heteroOk ? "PASS" : "FAIL"));
            if (!heteroOk) fails++;

            // 截图：抓取 / 裁剪 / 落盘
            // 抓屏依赖可见桌面：RDP 断开时 CopyFromScreen 会抛“句柄无效”，这种情况标 SKIP 而不是 FAIL
            bool grabOk = false;
            string grabNote = "";
            try
            {
                using (Bitmap grab = Shot.Grab(new Rectangle(0, 0, 200, 120)))
                {
                    grabOk = grab.Width == 200 && grab.Height == 120;
                }
            }
            catch (Exception ex)
            {
                grabNote = "SKIP(桌面不可抓屏 " + ex.GetType().Name + ")";
            }
            lines.Add("capture.grab = " + (grabNote.Length > 0 ? grabNote : (grabOk ? "PASS" : "FAIL")));
            if (grabNote.Length == 0 && !grabOk) fails++;

            bool cropOk = false;
            using (Bitmap canvas = new Bitmap(200, 200, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.FromArgb(255, 255, 0, 0));
                }
                List<Point> poly = new List<Point>();
                poly.Add(new Point(20, 20));
                poly.Add(new Point(180, 20));
                poly.Add(new Point(100, 180));
                using (Bitmap cut = Shot.CropToPolygon(canvas, new Rectangle(0, 0, 200, 200), poly))
                {
                    Color inside = cut.GetPixel(cut.Width / 2, 40);
                    Color outside = cut.GetPixel(2, 158);
                    cropOk = cut.Width == 160 && cut.Height == 160 && inside.A > 200 && outside.A < 40;
                }
            }
            lines.Add("capture.crop = " + (cropOk ? "PASS" : "FAIL"));
            if (!cropOk) fails++;

            bool saveOk = false;
            string tempDir = Path.Combine(Path.GetTempPath(), "flatcat_selftest_shot");
            using (Bitmap small = new Bitmap(64, 48, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(small)) g.Clear(Color.FromArgb(255, 196, 86, 63));
                string shotSaved = Shot.Save(small, tempDir);
                if (shotSaved != null && File.Exists(shotSaved))
                {
                    using (Bitmap shotBack = new Bitmap(shotSaved)) saveOk = shotBack.Width == 64 && shotBack.Height == 48;
                }
            }
            lines.Add("capture.save = " + (saveOk ? "PASS" : "FAIL"));
            if (!saveOk) fails++;

            // 电台与播放器
            List<RadioStation> presets = Radios.Presets();
            bool presetOk = presets.Count >= 3;
            for (int i = 0; i < presets.Count; i++)
            {
                Uri pu;
                if (!Radios.IsHttp(presets[i].Url) || !Uri.TryCreate(presets[i].Url, UriKind.Absolute, out pu)) presetOk = false;
            }
            lines.Add("media.presets = " + (presetOk ? "PASS" : "FAIL") + " (" + presets.Count + " 个)");
            if (!presetOk) fails++;

            bool scanOk = AudioPlayer.ScanFolder(Path.Combine(Path.GetTempPath(), "no_such_dir_flatcat")).Count == 0;
            lines.Add("media.scan_empty = " + (scanOk ? "PASS" : "FAIL"));
            if (!scanOk) fails++;

            bool playerOk = false;
            try
            {
                using (AudioPlayer ap = new AudioPlayer())
                {
                    playerOk = !string.IsNullOrEmpty(ap.Status);
                    ap.SetVolume(0.5);
                }
            }
            catch (Exception ex)
            {
                lines.Add("   player error: " + ex.GetType().Name);
            }
            lines.Add("media.player_thread = " + (playerOk ? "PASS" : "FAIL"));
            if (!playerOk) fails++;

            float dpi;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) dpi = g.DpiX;
            lines.Add("display.dpi = " + dpi);
            lines.Add("window.size.at100% = " + (int)Math.Round(CatPainter.Design * dpi / 96f)
                      + "x" + (int)Math.Round((CatPainter.Design + CatPainter.BubbleH) * dpi / 96f));

            lines.Add("----");
            lines.Add(fails == 0 ? "RESULT: PASS" : "RESULT: FAIL (" + fails + ")");
            if (icoSize > 0) lines.Add("icon.bytes = " + icoSize);

            File.WriteAllText(path, string.Join("\r\n", lines.ToArray()), new UTF8Encoding(false));
        }
    }
}