// FlatCat - 宠物窗口：分层窗口、渲染循环、交互、计时与闹钟、托盘
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FlatCat
{
    internal sealed class PetForm : Form
    {
        private const int FrameMs = 33;
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "FlatCat";

        private readonly Settings _cfg;
        private readonly InputMonitor _input;
        private System.Windows.Forms.Timer _timer;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private SettingsForm _settingsForm;
        private System.Media.SoundPlayer _purr;
        private System.Media.SoundPlayer _meow;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Random _rand = new Random();
        private double _lastTick;

        // 分层窗口表面
        private IntPtr _screenDc = IntPtr.Zero;
        private IntPtr _memDc = IntPtr.Zero;
        private IntPtr _dib = IntPtr.Zero;
        private IntPtr _oldDib = IntPtr.Zero;
        private IntPtr _bits = IntPtr.Zero;
        private int _dibW, _dibH;
        private byte[] _zero;
        private byte[] _buf;
        private byte[] _mask;

        private float _dpi = 1f;
        private float _scaleTotal = 1f;
        private int _opacity = 255;

        private readonly CatAnim _anim = new CatAnim();
        private string _state = "idle";
        private int _lastSeq;
        private DateTime _lastActivity = DateTime.Now;
        private DateTime _pawRelease = DateTime.MinValue;
        private float _blinkAt = -10f;
        private float _blinkNext = 3f;
        private bool _petting;
        private bool _dragging;
        private int _dragStartX, _dragStartY, _winStartX, _winStartY;
        private bool _through;
        private bool _hidden;
        private bool _exiting;
        private bool _cursorOn;
        private int _wheelFlash;

        // 番茄钟、喝水、倒计时、闹钟
        private int _pomoMode;                      // 0 停 / 1 专注 / 2 休息
        private DateTime _pomoEnd = DateTime.MinValue;
        private DateTime _waterNext = DateTime.MinValue;
        private DateTime _alertUntil = DateTime.MinValue;
        private string _alertText = "";
        private DateTime _countdownEnd = DateTime.MinValue;
        private string _alarmLastKey = "";
        private float _bubbleT;

        // v1.3：贪睡补提醒与整点报时
        private DateTime _snoozeEnd = DateTime.MinValue;
        private string _snoozeText = "";
        private int _lastChimeHour = -1;

        // 按键标签与闲置小动作
        private string _keyLabel = "";
        private DateTime _keyLabelUntil = DateTime.MinValue;
        private float _idleActionAt = -100f;
        private int _idleActionKind;

        private int _winX;
        private int _winY;

        public PetForm(Settings cfg)
        {
            _cfg = cfg;

            Text = "桌面小猫";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) _dpi = g.DpiX / 96f;
            ApplyScale();

            _input = new InputMonitor();
            _input.Start();

            LoadPurr();
            EnsureTodayCounter();
            BuildMenu();
            BuildTray();

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            _winX = _cfg.X >= 0 ? _cfg.X : wa.Right - Width - 48;
            _winY = _cfg.Y >= 0 ? _cfg.Y : wa.Bottom - Height - 16;
            ClampToWorkArea(wa);

            if (_cfg.WaterEnabled) _waterNext = DateTime.Now.AddMinutes(_cfg.WaterMinutes);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = FrameMs;
            _timer.Tick += OnTick;

            CreateHandle();
            SetThrough(true);
            RenderFrame();
            _timer.Start();
        }

        private void EnsureTodayCounter()
        {
            string today = Reminder.TodayKey();
            if (_cfg.TodayDate != today)
            {
                _cfg.TodayDate = today;
                _cfg.TodayPomodoros = 0;
                _cfg.Save();
            }
        }

        private void ClampToWorkArea(Rectangle wa)
        {
            if (_winX < wa.Left) _winX = wa.Left;
            if (_winX > wa.Right - Width) _winX = wa.Right - Width;
            if (_winY < wa.Top) _winY = wa.Top;
            if (_winY > wa.Bottom - Height) _winY = wa.Bottom - Height;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        #region 表面与渲染
        private void ApplyScale()
        {
            _scaleTotal = (float)(_cfg.Scale * _dpi);
            int w = (int)Math.Round(CatPainter.Design * _scaleTotal);
            int h = (int)Math.Round((CatPainter.Design + CatPainter.BubbleH) * _scaleTotal);
            if (w < 120) w = 120;
            if (h < 120) h = 120;
            Width = w;
            Height = h;
            _opacity = (int)Math.Round(Math.Max(0.25, Math.Min(1.0, _cfg.Opacity)) * 255);
            ClampToWorkArea(Screen.PrimaryScreen.WorkingArea);
        }

        private void EnsureSurface(int w, int h)
        {
            if (_dib != IntPtr.Zero && _dibW == w && _dibH == h) return;
            ReleaseSurface();

            _screenDc = Native.GetDC(IntPtr.Zero);
            _memDc = Native.CreateCompatibleDC(_screenDc);

            Native.BITMAPINFO bmi = new Native.BITMAPINFO();
            bmi.bmiHeader.biSize = Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = w;
            bmi.bmiHeader.biHeight = -h;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            IntPtr bits;
            _dib = Native.CreateDIBSection(_memDc, ref bmi, 0, out bits, IntPtr.Zero, 0);
            _bits = bits;
            _oldDib = Native.SelectObject(_memDc, _dib);

            _dibW = w;
            _dibH = h;
            _zero = new byte[w * h * 4];
            _buf = new byte[w * h * 4];
            _mask = new byte[w * h];
        }

        private void ReleaseSurface()
        {
            if (_memDc != IntPtr.Zero && _oldDib != IntPtr.Zero)
            {
                Native.SelectObject(_memDc, _oldDib);
                _oldDib = IntPtr.Zero;
            }
            if (_dib != IntPtr.Zero) { Native.DeleteObject(_dib); _dib = IntPtr.Zero; }
            if (_memDc != IntPtr.Zero) { Native.DeleteDC(_memDc); _memDc = IntPtr.Zero; }
            if (_screenDc != IntPtr.Zero) { Native.ReleaseDC(IntPtr.Zero, _screenDc); _screenDc = IntPtr.Zero; }
            _bits = IntPtr.Zero;
        }

        private void RenderFrame()
        {
            if (!IsHandleCreated) return;
            int w = Width, h = Height;
            if (w <= 0 || h <= 0) return;
            EnsureSurface(w, h);

            Marshal.Copy(_zero, 0, _bits, _zero.Length);

            using (Graphics g = Graphics.FromHdc(_memDc))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.ScaleTransform(_scaleTotal, _scaleTotal);

                string bubble = CurrentBubble();
                if (bubble != null)
                {
                    bool alert = DateTime.Now < _alertUntil;
                    float fontSize = (bubble.Length > 8) ? 16f : 19f;
                    CatPainter.DrawBubble(g, bubble, _bubbleT, alert, fontSize);
                }

                GraphicsState st = g.Save();
                g.TranslateTransform(0, CatPainter.BubbleH);
                CatPainter.Draw(g, _anim, _cfg.ShowKeyboard, Coats.ById(_cfg.CoatId),
                                _cfg.AccessoryId, _cfg.ShowSpots, _cfg.Heterochromia);
                g.Restore(st);
            }

            Marshal.Copy(_bits, _buf, 0, _buf.Length);
            int n = w * h;
            for (int i = 0; i < n; i++) _mask[i] = _buf[i * 4 + 3];

            Native.POINT dst = new Native.POINT();
            dst.X = _winX;
            dst.Y = _winY;
            Native.POINT src = new Native.POINT();
            Native.SIZE size = new Native.SIZE();
            size.cx = w;
            size.cy = h;
            Native.BLENDFUNCTION bf = new Native.BLENDFUNCTION();
            bf.BlendOp = Native.AC_SRC_OVER;
            bf.BlendFlags = 0;
            bf.SourceConstantAlpha = (byte)_opacity;
            bf.AlphaFormat = Native.AC_SRC_ALPHA;

            Native.UpdateLayeredWindow(Handle, IntPtr.Zero, ref dst, ref size, _memDc, ref src, 0, ref bf, Native.ULW_ALPHA);
        }

        private bool HitCat(int screenX, int screenY)
        {
            int x = screenX - _winX;
            int y = screenY - _winY;
            if (x < 0 || y < 0 || x >= _dibW || y >= _dibH) return false;
            if (_mask == null) return false;
            return _mask[y * _dibW + x] > 120;
        }

        private bool HitHead(int screenX, int screenY)
        {
            float x = (screenX - _winX) / _scaleTotal;
            float y = (screenY - _winY) / _scaleTotal - CatPainter.BubbleH;
            float dx = x - CatPainter.HeadCx;
            float dy = y - (CatPainter.HeadCy + 4f);
            return Math.Sqrt(dx * dx + dy * dy) <= CatPainter.HeadR * 0.78f;
        }
        #endregion

        #region 计时、闹钟与提醒
        private string CurrentBubble()
        {
            DateTime now = DateTime.Now;

            if (now < _alertUntil) return _alertText;

            if (_countdownEnd != DateTime.MinValue)
            {
                int left = (int)Math.Ceiling((_countdownEnd - now).TotalSeconds);
                if (left <= 0) return null;
                return "倒计时 " + Reminder.Clock(left);
            }

            if (now < _keyLabelUntil && _keyLabel.Length > 0) return _keyLabel;

            if (_pomoMode == 1) return "专注 " + Reminder.Clock((int)Math.Ceiling((_pomoEnd - now).TotalSeconds));
            if (_pomoMode == 2) return "休息 " + Reminder.Clock((int)Math.Ceiling((_pomoEnd - now).TotalSeconds));

            if (_cfg.ClockMode) return now.ToString("HH:mm");

            return null;
        }

        private void StartPomodoro(int mode)
        {
            int minutes = mode == 1 ? _cfg.PomodoroFocus : _cfg.PomodoroBreak;
            if (minutes < 1) minutes = 1;
            _pomoMode = mode;
            _pomoEnd = DateTime.Now.AddMinutes(minutes);
            _alertUntil = DateTime.MinValue;
            _lastActivity = DateTime.Now;
            RenderFrame();
        }

        private void CountPomodoro()
        {
            EnsureTodayCounter();
            _cfg.TodayPomodoros++;
            _cfg.TotalPomodoros++;
            _cfg.Save();
        }

        private void StartCountdown(int minutes)
        {
            if (minutes < 1) minutes = 1;
            _countdownEnd = DateTime.Now.AddMinutes(minutes);
            _alertUntil = DateTime.MinValue;
            _lastActivity = DateTime.Now;
            RenderFrame();
        }

        private void UpdateReminders()
        {
            DateTime now = DateTime.Now;

            // 番茄钟
            if (_pomoMode != 0 && now >= _pomoEnd)
            {
                if (_pomoMode == 1)
                {
                    CountPomodoro();
                    if (_cfg.PomodoroAutoContinue)
                    {
                        StartPomodoro(2);
                        Alert("专注结束 · 休息一下");
                    }
                    else
                    {
                        _pomoMode = 0;
                        Alert("专注结束 · 今日第 " + _cfg.TodayPomodoros + " 个");
                    }
                }
                else
                {
                    _pomoMode = 0;
                    Alert("休息结束 · 继续专注");
                }
            }

            // 喝水
            if (_cfg.WaterEnabled)
            {
                if (_waterNext == DateTime.MinValue) _waterNext = now.AddMinutes(_cfg.WaterMinutes);
                else if (now >= _waterNext)
                {
                    _waterNext = now.AddMinutes(_cfg.WaterMinutes);
                    Alert("该喝水了");
                }
            }
            else
            {
                _waterNext = DateTime.MinValue;
            }

            // 倒计时
            if (_countdownEnd != DateTime.MinValue && now >= _countdownEnd)
            {
                _countdownEnd = DateTime.MinValue;
                Alert("倒计时结束");
            }

            // 贪睡补提醒
            if (_snoozeEnd != DateTime.MinValue && now >= _snoozeEnd)
            {
                _snoozeEnd = DateTime.MinValue;
                Alert(_snoozeText, _cfg.AlarmSound);
            }

            // 整点报时
            if (_cfg.HourlyChime && now.Minute == 0 && now.Hour != _lastChimeHour
                && now.Hour >= _cfg.HourlyStart && now.Hour <= _cfg.HourlyEnd)
            {
                _lastChimeHour = now.Hour;
                Alert(now.ToString("HH:00") + " 整点报时", 1);
            }

            // 闹钟
            for (int i = 0; i < _cfg.Alarms.Count; i++)
            {
                Alarm a = _cfg.Alarms[i];
                if (Reminder.AlarmMatches(a, now, _alarmLastKey))
                {
                    _alarmLastKey = Reminder.AlarmKey(a, now);
                    Alert(a.TimeText + " " + a.Label, a.Sound);
                    if (_cfg.SnoozeMinutes > 0)
                    {
                        _snoozeEnd = now.AddMinutes(_cfg.SnoozeMinutes);
                        _snoozeText = a.TimeText + " " + a.Label + "（补提醒）";
                    }
                    break;
                }
            }
        }

        private void Alert(string text)
        {
            Alert(text, _cfg.AlarmSound);
        }

        private void Alert(string text, int sound)
        {
            _alertText = text;
            _alertUntil = DateTime.Now.AddSeconds(9);
            _lastActivity = DateTime.Now;
            PlayAlert(sound);
            LogAlert(text);
        }

        /// <summary>0 = 系统提示音，1 = 猫叫，2 = 静音。</summary>
        private void PlayAlert(int sound)
        {
            if (sound == 2) return;
            if (sound == 1 && _meow != null)
            {
                try { _meow.Play(); return; } catch { }
            }
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
        }

        private void LogAlert(string text)
        {
            try
            {
                if (_cfg.AlertLog == null) _cfg.AlertLog = new List<string>();
                _cfg.AlertLog.Add(DateTime.Now.ToString("HH:mm") + "  " + text);
                while (_cfg.AlertLog.Count > 20) _cfg.AlertLog.RemoveAt(0);
                _cfg.Save();
            }
            catch { }
        }
        #endregion

        #region 主循环
        private void OnTick(object sender, EventArgs e)
        {
            double now = _clock.Elapsed.TotalSeconds;
            double dt = now - _lastTick;
            if (dt <= 0.0001) dt = FrameMs / 1000.0;
            if (dt > 0.2) dt = 0.2;
            _lastTick = now;

            ReadInput();
            UpdateReminders();
            UpdateState();
            StepAnimation((float)dt);
            RenderFrame();
            UpdateClickThrough();
        }

        private void ReadInput()
        {
            int wheel = _input.TakeWheel();
            if (wheel != 0)
            {
                _lastActivity = DateTime.Now;
                _wheelFlash = 8;
                if (_state == "sleep") _state = "idle";
            }

            if (!_input.Ready) return;
            int seq = _input.KeySeq;
            if (seq == _lastSeq) return;
            _lastSeq = seq;

            _lastActivity = DateTime.Now;
            if (_state == "sleep") _state = "idle";

            if (!_input.LastIsDown) return;
            int vk = _input.LastVk;
            if (vk == 0) return;
            TriggerPaw(vk);
        }

        private void TriggerPaw(int vk)
        {
            bool isLeft = KeyName.IsLeftHalf(vk);
            if (_cfg.LeftHanded) isLeft = !isLeft;
            bool space = vk == 0x20;

            if (space)
            {
                _anim.LeftPaw = 1f;
                _anim.RightPaw = 1f;
            }
            else if (isLeft)
            {
                _anim.LeftPaw = 1f;
            }
            else
            {
                _anim.RightPaw = 1f;
            }

            if (_cfg.ShowKeyLabels)
            {
                if (_cfg.PrivacyMode)
                    _keyLabel = space ? "双手" : (isLeft ? "左手" : "右手");
                else
                    _keyLabel = KeyName.Of(vk);
                _keyLabelUntil = DateTime.Now.AddMilliseconds(700);
            }

            _pawRelease = DateTime.Now.AddMilliseconds(170);
            if (!_petting) _state = "typing";
        }

        private void UpdateState()
        {
            Native.POINT p;
            Native.GetCursorPos(out p);

            double idleMin = (DateTime.Now - _lastActivity).TotalMinutes;
            if (_cfg.SleepMinutes > 0 && idleMin >= _cfg.SleepMinutes && !_petting && !_dragging)
            {
                _state = "sleep";
            }
            else if (_state == "sleep" && idleMin < _cfg.SleepMinutes)
            {
                _state = "idle";
            }

            if (_state == "typing" && DateTime.Now >= _pawRelease) _state = "idle";
            if (_petting) { _state = "pet"; return; }

            if (DateTime.Now < _alertUntil) { _state = "alert"; return; }

            float headY = CatPainter.HeadCy + CatPainter.BubbleH;

            if (_state == "idle" || _state == "watch")
            {
                float dx = (p.X - _winX) / _scaleTotal - CatPainter.HeadCx;
                float dy = (p.Y - _winY) / _scaleTotal - headY;
                bool near = Math.Sqrt(dx * dx + dy * dy) < 300f;
                _state = near ? "watch" : "idle";
            }

            // 闲置小动作
            if (_cfg.IdleActions && _state == "idle" && (_bubbleT - _idleActionAt) > (9f + (float)_rand.NextDouble() * 12f))
            {
                _idleActionAt = _bubbleT;
                _idleActionKind = _rand.Next(2);
            }
        }

        private static float Lerp(float from, float to, float k)
        {
            return from + (to - from) * k;
        }

        private void StepAnimation(float dt)
        {
            float fast = 1f - (float)Math.Exp(-dt * 14f);
            float slow = 1f - (float)Math.Exp(-dt * 3.2f);
            float t = (float)_clock.Elapsed.TotalSeconds;
            _bubbleT = t;

            bool sleeping = _state == "sleep";
            bool alerting = DateTime.Now < _alertUntil;
            float sinceIdleAction = t - _idleActionAt;
            bool licking = _cfg.IdleActions && _state == "idle" && sinceIdleAction < 1.3f && _idleActionKind == 0;
            bool tilting = _cfg.IdleActions && _state == "idle" && sinceIdleAction < 1.6f && _idleActionKind == 1;

            float breatheSpeed = sleeping ? 0.85f : (alerting ? 2.2f : 1.7f);
            _anim.Breathe = (float)((Math.Sin(t * breatheSpeed * Math.PI) + 1.0) * 0.5);

            _blinkNext -= dt;
            if (_blinkNext <= 0f)
            {
                _blinkNext = 2.6f + (float)_rand.NextDouble() * 4.5f;
                _blinkAt = t;
            }
            bool blinking = (t - _blinkAt) < 0.11f;

            float eyeTarget = 1f;
            if (sleeping) eyeTarget = 0.04f;
            else if (_petting) eyeTarget = 0.1f;
            else if (blinking && !alerting) eyeTarget = 0.08f;
            else if (licking) eyeTarget = 0.45f;
            _anim.EyeOpen = Lerp(_anim.EyeOpen, eyeTarget, blinking ? fast * 3f : fast);

            Native.POINT p;
            Native.GetCursorPos(out p);
            float hx = _winX + CatPainter.HeadCx * _scaleTotal;
            float hy = _winY + (CatPainter.HeadCy + CatPainter.BubbleH) * _scaleTotal;
            float tx = (p.X - hx) / (260f * _scaleTotal);
            float ty = (p.Y - hy) / (260f * _scaleTotal);
            if (tx > 1f) tx = 1f; if (tx < -1f) tx = -1f;
            if (ty > 1f) ty = 1f; if (ty < -1f) ty = -1f;

            if (sleeping || _petting || licking) { tx = 0f; ty = 0f; }
            _anim.PupilX = Lerp(_anim.PupilX, tx, slow);
            _anim.PupilY = Lerp(_anim.PupilY, ty, slow);

            float tiltTarget = 0f;
            if (sleeping) tiltTarget = 8f;
            else if (_petting) tiltTarget = -4f + (float)Math.Sin(t * 6.0) * 1.6f;
            else if (alerting) tiltTarget = -3f;
            else if (licking) tiltTarget = -11f;
            else if (tilting) tiltTarget = 9f;
            else tiltTarget = _anim.PupilX * 4.5f;
            _anim.HeadTilt = Lerp(_anim.HeadTilt, tiltTarget, slow);

            if (DateTime.Now >= _pawRelease)
            {
                _anim.LeftPaw = Lerp(_anim.LeftPaw, licking ? 0.8f : 0f, fast);
                _anim.RightPaw = Lerp(_anim.RightPaw, 0f, fast);
            }

            float tailTarget = (float)Math.Sin(t * (sleeping ? 0.6f : (alerting ? 3.0f : 1.1f))) * (sleeping ? 3f : (alerting ? 12f : 9f));
            if (_state == "typing") tailTarget += 6f;
            _anim.TailAngle = Lerp(_anim.TailAngle, tailTarget, slow);

            float earTarget = _petting ? 1f : (alerting ? 0.85f : (tilting ? 0.6f : 0f));
            _anim.EarTwitch = Lerp(_anim.EarTwitch, earTarget, slow);

            float dipTarget = sleeping ? 1f : 0f;
            _anim.BodyDip = Lerp(_anim.BodyDip, dipTarget, slow);

            _anim.PurrWobble = _petting ? (float)(Math.Sin(t * 22.0) * 0.5 + 0.5) : 0f;

            if (_wheelFlash > 0) _wheelFlash--;
        }

        private void UpdateClickThrough()
        {
            Native.POINT p;
            Native.GetCursorPos(out p);
            bool onCat = HitCat(p.X, p.Y);
            _cursorOn = onCat;

            bool want;
            if (_dragging || _petting) want = false;
            else if (_cfg.ClickThroughLocked) want = true;
            else want = !onCat;

            SetThrough(want);
        }

        private void SetThrough(bool through)
        {
            if (_through == through) return;
            if (!IsHandleCreated) return;
            int ex = Native.GetWindowLong(Handle, Native.GWL_EXSTYLE);
            int want = through ? (ex | Native.WS_EX_TRANSPARENT) : (ex & ~Native.WS_EX_TRANSPARENT);
            Native.SetWindowLong(Handle, Native.GWL_EXSTYLE, want);
            _through = through;
        }
        #endregion

        #region 交互
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_APP_SETTINGS)
            {
                BeginInvoke((MethodInvoker)delegate { OpenSettings(); });
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            // 双击猫身 = 直接打开设置（比翻右键菜单更好找）
            if (_petting) { _petting = false; StopPurr(); }
            if (_dragging) { _dragging = false; Capture = false; }
            _state = "idle";
            OpenSettings();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right)
            {
                _menu.Show(Cursor.Position);
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            Native.POINT p;
            Native.GetCursorPos(out p);

            if (HitHead(p.X, p.Y))
            {
                _petting = true;
                _state = "pet";
                StartPurr();
                _lastActivity = DateTime.Now;
            }
            else if (HitCat(p.X, p.Y))
            {
                _dragging = true;
                _dragStartX = p.X;
                _dragStartY = p.Y;
                _winStartX = _winX;
                _winStartY = _winY;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            Native.POINT p;
            Native.GetCursorPos(out p);
            _winX = _winStartX + (p.X - _dragStartX);
            _winY = _winStartY + (p.Y - _dragStartY);
            RenderFrame();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            if (_petting)
            {
                _petting = false;
                StopPurr();
                _state = "idle";
            }
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                _cfg.X = _winX;
                _cfg.Y = _winY;
                _cfg.Save();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting)
            {
                e.Cancel = true;
                ToggleVisible();
                return;
            }
            base.OnFormClosing(e);
        }
        #endregion

        #region 设置窗口
        /// <summary>启动参数 --settings N 指定要直达的设置页；-1 表示不自动打开。</summary>
        public int StartPage = -1;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (StartPage >= 0) OpenSettingsAt(StartPage);
        }

        public void OpenSettingsAt(int page)
        {
            OpenSettings();
            if (_settingsForm != null) _settingsForm.SelectPage(page);
        }

        private void OpenSettings()
        {
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Activate();
                return;
            }

            _settingsForm = new SettingsForm(_cfg, ApplySettings);
            _settingsForm.StartCountdownRequested += delegate
            {
                StartCountdown(_settingsForm.CountdownMinutes);
            };
            _settingsForm.StartFocusRequested += delegate
            {
                StartPomodoro(1);
            };
            _settingsForm.TestAlertRequested += delegate
            {
                Alert("这是一条提醒（预览）", _settingsForm.SelectedSound);
            };
            _settingsForm.FormClosed += delegate { _settingsForm = null; };
            _settingsForm.Show();
        }

        private void ApplySettings(Settings s)
        {
            ApplyScale();
            SetAutostart(s.Autostart);
            RenderFrame();
        }
        #endregion

        #region 菜单与托盘
        private void BuildMenu()
        {
            _menu = new ContextMenuStrip();
            _menu.ShowImageMargin = false;
            _menu.Font = SystemFonts.MenuFont;

            ToolStripMenuItem settings = new ToolStripMenuItem("设置…");
            settings.Font = new Font(_menu.Font, FontStyle.Bold);
            settings.Click += delegate { OpenSettings(); };
            _menu.Items.Add(settings);

            ToolStripMenuItem toggle = new ToolStripMenuItem("显示 / 隐藏");
            toggle.Click += delegate { ToggleVisible(); };
            _menu.Items.Add(toggle);
            _menu.Items.Add(new ToolStripSeparator());

            _menu.Items.Add(BuildScaleMenu());
            _menu.Items.Add(BuildOpacityMenu());
            _menu.Items.Add(BuildSleepMenu());
            _menu.Items.Add(BuildCoatMenu());
            _menu.Items.Add(BuildPomodoroMenu());
            _menu.Items.Add(BuildWaterMenu());
            _menu.Items.Add(BuildReminderMenu());

            ToolStripMenuItem kb = new ToolStripMenuItem("显示小键盘");
            kb.Checked = _cfg.ShowKeyboard;
            kb.Click += delegate
            {
                _cfg.ShowKeyboard = !_cfg.ShowKeyboard;
                kb.Checked = _cfg.ShowKeyboard;
                _cfg.Save();
                RenderFrame();
            };
            _menu.Items.Add(kb);

            ToolStripMenuItem lockThrough = new ToolStripMenuItem("锁定点击穿透");
            lockThrough.Checked = _cfg.ClickThroughLocked;
            lockThrough.Click += delegate
            {
                _cfg.ClickThroughLocked = !_cfg.ClickThroughLocked;
                lockThrough.Checked = _cfg.ClickThroughLocked;
                _cfg.Save();
            };
            _menu.Items.Add(lockThrough);

            ToolStripMenuItem purr = new ToolStripMenuItem("呼噜声");
            purr.Checked = _cfg.Purr;
            purr.Click += delegate
            {
                _cfg.Purr = !_cfg.Purr;
                purr.Checked = _cfg.Purr;
                if (!_cfg.Purr) StopPurr();
                _cfg.Save();
            };
            _menu.Items.Add(purr);

            ToolStripMenuItem auto = new ToolStripMenuItem("开机自启");
            auto.Checked = IsAutostartEnabled();
            auto.Click += delegate
            {
                bool on = !IsAutostartEnabled();
                SetAutostart(on);
                auto.Checked = on;
                _cfg.Autostart = on;
                _cfg.Save();
            };
            _menu.Items.Add(auto);

            _menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem about = new ToolStripMenuItem("关于");
            about.Click += delegate { ShowAbout(); };
            _menu.Items.Add(about);

            ToolStripMenuItem quit = new ToolStripMenuItem("退出");
            quit.Click += delegate { ExitApp(); };
            _menu.Items.Add(quit);

            ContextMenuStrip = _menu;
        }

        private ToolStripMenuItem BuildCoatMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("毛色");
            string[] names = Coats.Names();
            for (int i = 0; i < names.Length; i++)
            {
                int id = i;
                ToolStripMenuItem item = new ToolStripMenuItem(names[i]);
                item.Checked = _cfg.CoatId == id;
                item.Click += delegate
                {
                    _cfg.CoatId = id;
                    _cfg.Save();
                    MarkGroup(root, item);
                    RenderFrame();
                };
                root.DropDownItems.Add(item);
            }
            return root;
        }

        private ToolStripMenuItem BuildPomodoroMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("番茄钟");

            ToolStripMenuItem status = new ToolStripMenuItem("未计时");
            status.Enabled = false;
            root.DropDownItems.Add(status);
            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem startFocus = new ToolStripMenuItem("开始专注");
            startFocus.Click += delegate { StartPomodoro(1); };
            root.DropDownItems.Add(startFocus);

            ToolStripMenuItem startBreak = new ToolStripMenuItem("开始休息");
            startBreak.Click += delegate { StartPomodoro(2); };
            root.DropDownItems.Add(startBreak);

            ToolStripMenuItem stop = new ToolStripMenuItem("停止计时");
            stop.Click += delegate { _pomoMode = 0; _alertUntil = DateTime.MinValue; };
            root.DropDownItems.Add(stop);

            root.DropDownOpening += delegate
            {
                DateTime now = DateTime.Now;
                if (_pomoMode == 1)
                    status.Text = "专注中 · " + Reminder.Clock((int)Math.Ceiling((_pomoEnd - now).TotalSeconds));
                else if (_pomoMode == 2)
                    status.Text = "休息中 · " + Reminder.Clock((int)Math.Ceiling((_pomoEnd - now).TotalSeconds));
                else
                    status.Text = "今日完成 " + _cfg.TodayPomodoros + " 个";
                stop.Enabled = _pomoMode != 0;
            };

            return root;
        }

        private ToolStripMenuItem BuildWaterMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("喝水提醒");

            ToolStripMenuItem on = new ToolStripMenuItem("开启提醒");
            on.Checked = _cfg.WaterEnabled;
            on.Click += delegate
            {
                _cfg.WaterEnabled = !_cfg.WaterEnabled;
                on.Checked = _cfg.WaterEnabled;
                _waterNext = _cfg.WaterEnabled ? DateTime.Now.AddMinutes(_cfg.WaterMinutes) : DateTime.MinValue;
                _cfg.Save();
            };
            root.DropDownItems.Add(on);

            root.DropDownOpening += delegate
            {
                if (_cfg.WaterEnabled && _waterNext != DateTime.MinValue)
                {
                    int left = (int)Math.Ceiling((_waterNext - DateTime.Now).TotalSeconds);
                    if (left < 0) left = 0;
                    on.Text = "开启提醒（下次 " + Reminder.Clock(left) + "）";
                }
                else
                {
                    on.Text = "开启提醒";
                }
            };

            return root;
        }

        private ToolStripMenuItem BuildReminderMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("提醒 / 闹钟");

            ToolStripMenuItem next = new ToolStripMenuItem("下次：暂无闹钟");
            next.Enabled = false;
            root.DropDownItems.Add(next);
            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem open = new ToolStripMenuItem("打开设置…");
            open.Click += delegate { OpenSettings(); };
            root.DropDownItems.Add(open);

            ToolStripMenuItem test = new ToolStripMenuItem("预览提醒效果");
            test.Click += delegate { Alert("这是一条提醒", _cfg.AlarmSound); };
            root.DropDownItems.Add(test);

            ToolStripMenuItem hourly = new ToolStripMenuItem("整点报时");
            hourly.Checked = _cfg.HourlyChime;
            hourly.Click += delegate
            {
                _cfg.HourlyChime = !_cfg.HourlyChime;
                hourly.Checked = _cfg.HourlyChime;
                _cfg.Save();
            };
            root.DropDownItems.Add(hourly);

            ToolStripMenuItem snooze = new ToolStripMenuItem("稍后提醒（贪睡）");
            snooze.Click += delegate
            {
                int m = _cfg.SnoozeMinutes > 0 ? _cfg.SnoozeMinutes : 5;
                _snoozeEnd = DateTime.Now.AddMinutes(m);
                _snoozeText = "稍后提醒";
                Alert("好的，" + m + " 分钟后再提醒你");
            };
            root.DropDownItems.Add(snooze);

            root.DropDownOpening += delegate
            {
                next.Text = "下次：" + Reminder.NextAlarmText(_cfg.Alarms, DateTime.Now);
            };
            return root;
        }

        private ToolStripMenuItem BuildScaleMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("大小");
            double[] values = new double[] { 0.7, 1.0, 1.3 };
            string[] labels = new string[] { "小 (70%)", "中 (100%)", "大 (130%)" };
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                ToolStripMenuItem item = new ToolStripMenuItem(labels[i]);
                item.Checked = Math.Abs(_cfg.Scale - v) < 0.001;
                item.Click += delegate
                {
                    _cfg.Scale = v;
                    ApplyScale();
                    _cfg.Save();
                    MarkGroup(root, item);
                    RenderFrame();
                };
                root.DropDownItems.Add(item);
            }
            return root;
        }

        private ToolStripMenuItem BuildOpacityMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("不透明度");
            double[] values = new double[] { 1.0, 0.85, 0.7 };
            string[] labels = new string[] { "100%", "85%", "70%" };
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                ToolStripMenuItem item = new ToolStripMenuItem(labels[i]);
                item.Checked = Math.Abs(_cfg.Opacity - v) < 0.001;
                item.Click += delegate
                {
                    _cfg.Opacity = v;
                    ApplyScale();
                    _cfg.Save();
                    MarkGroup(root, item);
                    RenderFrame();
                };
                root.DropDownItems.Add(item);
            }
            return root;
        }

        private ToolStripMenuItem BuildSleepMenu()
        {
            ToolStripMenuItem root = new ToolStripMenuItem("闲置睡眠");
            int[] values = new int[] { 5, 15, 0 };
            string[] labels = new string[] { "5 分钟", "15 分钟", "不睡" };
            for (int i = 0; i < values.Length; i++)
            {
                int v = values[i];
                ToolStripMenuItem item = new ToolStripMenuItem(labels[i]);
                item.Checked = _cfg.SleepMinutes == v;
                item.Click += delegate
                {
                    _cfg.SleepMinutes = v;
                    _cfg.Save();
                    MarkGroup(root, item);
                };
                root.DropDownItems.Add(item);
            }
            return root;
        }

        private static void MarkGroup(ToolStripMenuItem root, ToolStripMenuItem chosen)
        {
            foreach (ToolStripItem it in root.DropDownItems)
            {
                ToolStripMenuItem mi = it as ToolStripMenuItem;
                if (mi != null) mi.Checked = (mi == chosen);
            }
        }

        private void BuildTray()
        {
            _tray = new NotifyIcon();
            Icon ico = null;
            try { ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
            if (ico != null) _tray.Icon = ico;
            else _tray.Icon = SystemIcons.Application;
            _tray.Text = "桌面小猫";
            _tray.Visible = true;
            _tray.ContextMenuStrip = _menu;
            _tray.DoubleClick += delegate { OpenSettings(); };
        }

        private void ToggleVisible()
        {
            if (_hidden)
            {
                _hidden = false;
                Show();
                SetThrough(true);
                RenderFrame();
            }
            else
            {
                _hidden = true;
                Hide();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "桌面小猫 FlatCat 1.3\n\n" +
                "透明窗口 + 全局键鼠响应 + 矢量猫。\n\n" +
                "你在任何程序里打字，它会抬爪；\n" +
                "把鼠标移到它头上按住，它会呼噜；\n" +
                "按住身体可以把它拖到任何地方。\n\n" +
                "双击猫身或双击托盘图标 = 打开设置：\n" +
                "毛色 / 配饰 / 斑纹 / 异瞳 / 大小，\n" +
                "闹钟（可编辑、按星期重复、贪睡），\n" +
                "番茄钟、喝水、倒计时、整点报时。

" +
                "© 2026 认知觉醒能量站 · 微信公众号同名
" +
                "MIT License · 源码见 GitHub",
                "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApp()
        {
            _exiting = true;
            try { _timer.Stop(); } catch { }
            StopPurr();
            try { _input.Dispose(); } catch { }
            if (_settingsForm != null && !_settingsForm.IsDisposed) { try { _settingsForm.Close(); } catch { } }
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
            _cfg.X = _winX;
            _cfg.Y = _winY;
            _cfg.Save();
            ReleaseSurface();
            Close();
        }
        #endregion

        #region 声音与自启
        private void LoadPurr()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream s = asm.GetManifestResourceStream("purr.wav");
                if (s != null) _purr = new System.Media.SoundPlayer(s);
                Stream m = asm.GetManifestResourceStream("catmeow.wav");
                if (m != null) _meow = new System.Media.SoundPlayer(m);
            }
            catch { _purr = null; }
        }

        private void StartPurr()
        {
            if (!_cfg.Purr || _purr == null) return;
            try { _purr.PlayLooping(); } catch { }
        }

        private void StopPurr()
        {
            if (_purr == null) return;
            try { _purr.Stop(); } catch { }
        }

        private static bool IsAutostartEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (k == null) return false;
                    object v = k.GetValue(RunValue);
                    return v != null && string.Equals(v.ToString(), Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        private static void SetAutostart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (on) k.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue(RunValue, false);
                }
            }
            catch { }
        }
        #endregion
    }
}