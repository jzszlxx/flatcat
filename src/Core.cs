// FlatCat - 核心：原生互操作、配置、全局钩子、毛色、按键名、计时工具
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace FlatCat
{
    #region 原生互操作
    internal static class Native
    {
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int GWL_EXSTYLE = -20;

        public const int ULW_ALPHA = 2;
        public const byte AC_SRC_OVER = 0;
        public const byte AC_SRC_ALPHA = 1;

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_QUIT = 0x0012;

        /// <summary>第二实例唤起已有实例打开设置窗口。</summary>
        public const int WM_APP_SETTINGS = 0x8000 + 1;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx; public int cy; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int biSize; public int biWidth; public int biHeight;
            public short biPlanes; public short biBitCount; public int biCompression;
            public int biSizeImage; public int biXPelsPerMeter; public int biYPelsPerMeter;
            public int biClrUsed; public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public int bmiColors; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
            public uint time; public int x; public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")] public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
        [DllImport("user32.dll")] public static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] public static extern IntPtr DispatchMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] public static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    }
    #endregion

    #region 闹钟
    [DataContract]
    public class Alarm
    {
        [DataMember(Name = "label")] public string Label = "提醒";
        [DataMember(Name = "hour")] public int Hour = 9;
        [DataMember(Name = "minute")] public int Minute = 0;
        [DataMember(Name = "daily")] public bool Daily = true;
        [DataMember(Name = "weekdays")] public bool Weekdays = false;
        [DataMember(Name = "enabled")] public bool Enabled = true;
        /// <summary>每周重复位掩码：bit0=周一 … bit6=周日；0 表示沿用 Daily/Weekdays 旧语义。</summary>
        [DataMember(Name = "days")] public int Days = 0;
        /// <summary>铃声：0=系统提示音，1=猫叫，2=静音。</summary>
        [DataMember(Name = "sound")] public int Sound = 0;

        public Alarm() { }

        public Alarm(string label, int hour, int minute, bool daily, bool weekdays)
        {
            Label = label;
            Hour = hour;
            Minute = minute;
            Daily = daily;
            Weekdays = weekdays;
        }

        public string TimeText
        {
            get { return string.Format("{0:00}:{1:00}", Hour, Minute); }
        }

        public string RepeatText
        {
            get
            {
                if (Days != 0) return Reminder.DaysText(Days);
                if (Weekdays) return "工作日";
                if (Daily) return "每天";
                return "仅一次";
            }
        }

        public override string ToString()
        {
            return TimeText + "  " + Label + "  (" + RepeatText + ")" + (Enabled ? "" : "  [停用]");
        }
    }
    #endregion

    #region 设置
    [DataContract]
    public class Settings
    {
        [DataMember(Name = "scale")] public double Scale = 1.0;
        [DataMember(Name = "opacity")] public double Opacity = 1.0;
        [DataMember(Name = "sleepMinutes")] public int SleepMinutes = 5;
        [DataMember(Name = "autostart")] public bool Autostart = false;
        [DataMember(Name = "clickThroughLocked")] public bool ClickThroughLocked = false;
        [DataMember(Name = "showKeyboard")] public bool ShowKeyboard = true;
        [DataMember(Name = "purr")] public bool Purr = true;
        [DataMember(Name = "x")] public int X = -1;
        [DataMember(Name = "y")] public int Y = -1;

        // 外观与行为
        [DataMember(Name = "coat")] public int CoatId = 0;
        [DataMember(Name = "showKeyLabels")] public bool ShowKeyLabels = true;
        [DataMember(Name = "privacyMode")] public bool PrivacyMode = false;
        [DataMember(Name = "leftHanded")] public bool LeftHanded = false;
        [DataMember(Name = "idleActions")] public bool IdleActions = true;

        // 番茄钟与喝水提醒
        [DataMember(Name = "pomodoroFocus")] public int PomodoroFocus = 25;
        [DataMember(Name = "pomodoroBreak")] public int PomodoroBreak = 5;
        [DataMember(Name = "pomodoroAutoContinue")] public bool PomodoroAutoContinue = true;
        [DataMember(Name = "waterEnabled")] public bool WaterEnabled = false;
        [DataMember(Name = "waterMinutes")] public int WaterMinutes = 45;

        // 闹钟与统计
        [DataMember(Name = "alarms")] public List<Alarm> Alarms = new List<Alarm>();
        [DataMember(Name = "todayPomodoros")] public int TodayPomodoros = 0;
        [DataMember(Name = "todayDate")] public string TodayDate = "";

        // v1.3 增强
        [DataMember(Name = "hourlyChime")] public bool HourlyChime = false;
        [DataMember(Name = "hourlyStart")] public int HourlyStart = 8;
        [DataMember(Name = "hourlyEnd")] public int HourlyEnd = 22;
        [DataMember(Name = "snoozeMinutes")] public int SnoozeMinutes = 5;
        [DataMember(Name = "alarmSound")] public int AlarmSound = 0;
        [DataMember(Name = "accessory")] public int AccessoryId = 0;
        [DataMember(Name = "showSpots")] public bool ShowSpots = true;
        [DataMember(Name = "heterochromia")] public bool Heterochromia = false;
        [DataMember(Name = "clockMode")] public bool ClockMode = false;
        [DataMember(Name = "totalPomodoros")] public int TotalPomodoros = 0;
        [DataMember(Name = "alertLog")] public List<string> AlertLog = new List<string>();

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlatCat"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(Dir, "settings.json"); }
        }

        public static Settings Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new Settings();
                using (FileStream fs = File.OpenRead(FilePath))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Settings));
                    Settings s = ser.ReadObject(fs) as Settings;
                    if (s == null) return new Settings();
                    if (s.Alarms == null) s.Alarms = new List<Alarm>();
                    if (s.AlertLog == null) s.AlertLog = new List<string>();
                    return s;
                }
            }
            catch
            {
                return new Settings();
            }
        }

        public bool Save()
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                using (FileStream fs = File.Create(FilePath))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Settings));
                    ser.WriteObject(fs, this);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>深拷贝，给设置窗口做“取消回滚”用。</summary>
        public Settings Clone()
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Settings));
                using (MemoryStream ms = new MemoryStream())
                {
                    ser.WriteObject(ms, this);
                    ms.Position = 0;
                    Settings copy = ser.ReadObject(ms) as Settings;
                    return copy ?? this;
                }
            }
            catch
            {
                return this;
            }
        }

        public void CopyFrom(Settings other)
        {
            if (other == null) return;
            Scale = other.Scale; Opacity = other.Opacity; SleepMinutes = other.SleepMinutes;
            Autostart = other.Autostart; ClickThroughLocked = other.ClickThroughLocked;
            ShowKeyboard = other.ShowKeyboard; Purr = other.Purr;
            X = other.X; Y = other.Y;
            CoatId = other.CoatId; ShowKeyLabels = other.ShowKeyLabels; PrivacyMode = other.PrivacyMode;
            LeftHanded = other.LeftHanded; IdleActions = other.IdleActions;
            PomodoroFocus = other.PomodoroFocus; PomodoroBreak = other.PomodoroBreak;
            PomodoroAutoContinue = other.PomodoroAutoContinue;
            WaterEnabled = other.WaterEnabled; WaterMinutes = other.WaterMinutes;
            Alarms = other.Alarms;
            TodayPomodoros = other.TodayPomodoros; TodayDate = other.TodayDate;
            HourlyChime = other.HourlyChime; HourlyStart = other.HourlyStart; HourlyEnd = other.HourlyEnd;
            SnoozeMinutes = other.SnoozeMinutes; AlarmSound = other.AlarmSound;
            AccessoryId = other.AccessoryId; ShowSpots = other.ShowSpots;
            Heterochromia = other.Heterochromia; ClockMode = other.ClockMode;
            TotalPomodoros = other.TotalPomodoros;
            AlertLog = new List<string>(other.AlertLog == null ? new string[0] : other.AlertLog.ToArray());
        }
    }
    #endregion

    #region 毛色
    internal sealed class Coat
    {
        public readonly string Name;
        public readonly Color Fur;
        public readonly Color FurDark;
        public readonly Color Light;
        public readonly Color Pink;
        public readonly Color Eye;

        public Coat(string name, int fur, int dark, int light, int pink, int eye)
        {
            Name = name;
            // 注意：Color.FromArgb(int) 的单参重载把实参当 ARGB，直接传 0x9C8D7A 会得到 alpha=0（全透明）
            Fur = FromRgb(fur);
            FurDark = FromRgb(dark);
            Light = FromRgb(light);
            Pink = FromRgb(pink);
            Eye = FromRgb(eye);
        }

        private static Color FromRgb(int rgb)
        {
            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
    }

    internal static class Coats
    {
        public static readonly Coat[] All = new Coat[]
        {
            new Coat("灰棕虎斑", 0x9C8D7A, 0x786957, 0xF3EEE5, 0xD9A6A0, 0x7BB93F),
            new Coat("橘虎斑",   0xE9973F, 0xC97A2A, 0xFBE8C9, 0xE8A79E, 0x6FA83C),
            new Coat("奶牛",     0x3E3A38, 0x2A2624, 0xF6F2EA, 0xD9A6A0, 0x7BB93F),
            new Coat("白猫",     0xF2EDE4, 0xD6CCBC, 0xFFFFFF, 0xEFB9B2, 0x6AA9D8),
            new Coat("蓝灰",     0x93A7B4, 0x6F8492, 0xE9F0F4, 0xD9A6A0, 0xC9A227),
            new Coat("三花",     0xF0E7D9, 0xC77C2E, 0xFFFFFF, 0xE0A9A2, 0x7BB93F),
            new Coat("暹罗",     0xEADFCE, 0x7E6650, 0xFDFAF4, 0xD9A6A0, 0x5AA0D0)
        };

        public static Coat ById(int id)
        {
            if (id < 0 || id >= All.Length) return All[0];
            return All[id];
        }

        public static string[] Names()
        {
            string[] names = new string[All.Length];
            for (int i = 0; i < All.Length; i++) names[i] = All[i].Name;
            return names;
        }
    }
    #endregion

    #region 按键名
    internal static class KeyName
    {
        /// <summary>把虚拟键码转成气泡里显示的短名。只做展示，不记录任何输入内容。</summary>
        public static string Of(int vk)
        {
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x60 && vk <= 0x69) return "Num" + (vk - 0x60);
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);

            switch (vk)
            {
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x13: return "Pause";
                case 0x14: return "Caps";
                case 0x1B: return "Esc";
                case 0x20: return "Space";
                case 0x21: return "PgUp";
                case 0x22: return "PgDn";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "Left";
                case 0x26: return "Up";
                case 0x27: return "Right";
                case 0x28: return "Down";
                case 0x2C: return "PrtSc";
                case 0x2D: return "Ins";
                case 0x2E: return "Del";
                case 0x5B: return "Win";
                case 0x5C: return "Win";
                case 0x5D: return "Menu";
                case 0x90: return "NumLock";
                case 0x91: return "ScrLock";
                case 0xA0: return "LShift";
                case 0xA1: return "RShift";
                case 0xA2: return "LCtrl";
                case 0xA3: return "RCtrl";
                case 0xA4: return "LAlt";
                case 0xA5: return "RAlt";
                case 0xBA: return ";";
                case 0xBB: return "+";
                case 0xBC: return ",";
                case 0xBD: return "-";
                case 0xBE: return ".";
                case 0xBF: return "/";
                case 0xC0: return "`";
                case 0xDB: return "[";
                case 0xDC: return "\\";
                case 0xDD: return "]";
                case 0xDE: return "'";
                default: return "Key" + vk;
            }
        }

        /// <summary>这个键属于键盘左半区还是右半区，用于决定抬哪只爪。</summary>
        public static bool IsLeftHalf(int vk)
        {
            switch (vk)
            {
                case 0x51: case 0x57: case 0x45: case 0x52: case 0x54:
                case 0x41: case 0x53: case 0x44: case 0x46: case 0x47:
                case 0x5A: case 0x58: case 0x43: case 0x56: case 0x42:
                case 0x31: case 0x32: case 0x33: case 0x34: case 0x35:
                case 0xC0: case 0x09: case 0x14: case 0x10: case 0x11:
                case 0x5B: case 0x12: case 0x08:
                    return true;
                default:
                    return false;
            }
        }
    }
    #endregion

    #region 计时工具
    internal static class Reminder
    {
        /// <summary>把剩余秒数格式化成 24:59 / 1:05:00 这样的时钟串。</summary>
        public static string Clock(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            if (h > 0) return string.Format("{0}:{1:00}:{2:00}", h, m, s);
            return string.Format("{0:00}:{1:00}", m, s);
        }

        /// <summary>闹钟是否该在这一分钟触发。weekdays 只在周一至周五。</summary>
        public static bool AlarmMatches(Alarm a, DateTime now, string lastFiredKey)
        {
            if (a == null || !a.Enabled) return false;
            if (a.Hour != now.Hour || a.Minute != now.Minute) return false;
            if (a.Days != 0)
            {
                if (!DaysMatch(a.Days, now)) return false;
            }
            else if (a.Weekdays && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday))
            {
                return false;
            }
            string key = now.ToString("yyyyMMddHHmm") + "|" + a.Hour + ":" + a.Minute + "|" + a.Label;
            return key != lastFiredKey;
        }

        public static string AlarmKey(Alarm a, DateTime now)
        {
            return now.ToString("yyyyMMddHHmm") + "|" + a.Hour + ":" + a.Minute + "|" + a.Label;
        }

        public static string TodayKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        /// <summary>位掩码转「周一三五」这样的中文描述。</summary>
        public static string DaysText(int days)
        {
            if (days == 0) return "仅一次";
            if (days == 0x7F) return "每天";
            if (days == 0x1F) return "工作日";
            if (days == 0x60) return "周末";
            string[] names = new string[] { "一", "二", "三", "四", "五", "六", "日" };
            StringBuilder sb = new StringBuilder("周");
            for (int i = 0; i < 7; i++)
                if ((days & (1 << i)) != 0) sb.Append(names[i]);
            return sb.ToString();
        }

        /// <summary>位掩码是否命中今天（bit0=周一，Sunday 映射到 bit6）。</summary>
        public static bool DaysMatch(int days, DateTime now)
        {
            if (days == 0) return true;
            int bit = ((int)now.DayOfWeek + 6) % 7;
            return (days & (1 << bit)) != 0;
        }

        /// <summary>从现在起下一次触发时间；不再重复或已停用时返回 DateTime.MinValue。</summary>
        public static DateTime NextFire(Alarm a, DateTime from)
        {
            if (a == null || !a.Enabled) return DateTime.MinValue;
            bool once = (a.Days == 0 && !a.Daily && !a.Weekdays);
            bool[] allowed = new bool[7];
            if (a.Days != 0)
            {
                for (int i = 0; i < 7; i++) allowed[i] = (a.Days & (1 << i)) != 0;
            }
            else
            {
                for (int i = 0; i < 7; i++) allowed[i] = true;
                if (a.Weekdays) { allowed[5] = false; allowed[6] = false; }
            }
            int days = once ? 1 : 8;
            for (int d = 0; d < days; d++)
            {
                DateTime day = from.Date.AddDays(d);
                int bit = ((int)day.DayOfWeek + 6) % 7;
                if (!allowed[bit]) continue;
                DateTime t = day.AddHours(a.Hour).AddMinutes(a.Minute);
                if (t > from) return t;
            }
            return DateTime.MinValue;
        }

        /// <summary>所有启用闹钟里最近的一次，用于设置窗口与菜单预览。</summary>
        public static string NextAlarmText(List<Alarm> alarms, DateTime from)
        {
            DateTime best = DateTime.MinValue;
            string label = "";
            if (alarms != null)
            {
                for (int i = 0; i < alarms.Count; i++)
                {
                    DateTime t = NextFire(alarms[i], from);
                    if (t != DateTime.MinValue && (best == DateTime.MinValue || t < best))
                    {
                        best = t;
                        label = alarms[i].TimeText + " " + alarms[i].Label;
                    }
                }
            }
            if (best == DateTime.MinValue) return "暂无闹钟";
            double minutes = (best - from).TotalMinutes;
            string when;
            if (minutes < 60) when = ((int)Math.Ceiling(minutes)) + " 分钟后";
            else if (minutes < 60 * 24 && best.Date == from.Date) when = best.ToString("HH:mm") + " 今天";
            else when = best.ToString("MM-dd HH:mm");
            return label + " · " + when;
        }
    }
    #endregion

    #region 全局输入（低级钩子）
    internal sealed class InputMonitor : IDisposable
    {
        private Thread _thread;
        private uint _tid;
        private IntPtr _kbHook = IntPtr.Zero;
        private IntPtr _msHook = IntPtr.Zero;
        private Native.HookProc _kbProc;
        private Native.HookProc _msProc;
        private volatile bool _ready;
        private volatile bool _failed;

        private volatile int _lastVk;
        private volatile int _lastDown;
        private volatile int _wheel;

        public int LastVk { get { return _lastVk; } }
        public bool LastIsDown { get { return _lastDown == 1; } }
        public bool Ready { get { return _ready; } }
        public bool Failed { get { return _failed; } }

        private int _keySeq;
        public int KeySeq { get { return Thread.VolatileRead(ref _keySeq); } }

        public int TakeWheel()
        {
            int w = _wheel;
            if (w != 0) _wheel = 0;
            return w;
        }

        public void Start()
        {
            _kbProc = KbProc;
            _msProc = MsProc;
            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Name = "FlatCat.Hooks";
            _thread.Start();
            for (int i = 0; i < 120 && !_ready && !_failed; i++) Thread.Sleep(10);
            if (!_ready) _failed = true;
        }

        private void Run()
        {
            _tid = Native.GetCurrentThreadId();

            // 先建立消息队列，否则 PostThreadMessage(WM_QUIT) 会被丢弃
            Native.MSG warm;
            Native.PeekMessage(out warm, IntPtr.Zero, 0, 0, 0);

            IntPtr hMod = Native.GetModuleHandle(null);
            _kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _kbProc, hMod, 0);
            _msHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _msProc, hMod, 0);
            if (_kbHook == IntPtr.Zero)
            {
                _failed = true;
                return;
            }
            _ready = true;

            Native.MSG msg;
            while (Native.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
            {
                Native.TranslateMessage(ref msg);
                Native.DispatchMessage(ref msg);
            }

            if (_kbHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_kbHook);
            if (_msHook != IntPtr.Zero) Native.UnhookWindowsHookEx(_msHook);
            _kbHook = IntPtr.Zero;
            _msHook = IntPtr.Zero;
            _ready = false;
        }

        // 回调内只做赋值，绝不做耗时操作：超时会被系统静默摘钩
        private IntPtr KbProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int m = (int)wParam;
                if (m == Native.WM_KEYDOWN || m == Native.WM_SYSKEYDOWN || m == Native.WM_KEYUP || m == Native.WM_SYSKEYUP)
                {
                    Native.KBDLLHOOKSTRUCT k = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
                    _lastVk = (int)k.vkCode;
                    _lastDown = (m == Native.WM_KEYDOWN || m == Native.WM_SYSKEYDOWN) ? 1 : 0;
                    Interlocked.Increment(ref _keySeq);
                }
            }
            return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr MsProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (int)wParam == Native.WM_MOUSEWHEEL)
            {
                Native.MSLLHOOKSTRUCT m = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT));
                _wheel += (short)((m.mouseData >> 16) & 0xFFFF);
            }
            return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_tid != 0) Native.PostThreadMessage(_tid, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            if (_thread != null) _thread.Join(800);
        }
    }
    #endregion

    #region 动画参数
    internal sealed class CatAnim
    {
        public float Breathe;
        public float EyeOpen = 1f;
        public float PupilX;
        public float PupilY;
        public float HeadTilt;
        public float LeftPaw;
        public float RightPaw;
        public float TailAngle;
        public float EarTwitch;
        public float Sleep;
        public float BodyDip;
        public float PurrWobble;

        public static CatAnim ForState(string state)
        {
            CatAnim a = new CatAnim();
            switch (state)
            {
                case "typing":
                    a.LeftPaw = 1f; a.PupilY = 0.7f; a.EyeOpen = 0.85f; a.HeadTilt = 2f;
                    break;
                case "sleep":
                    a.EyeOpen = 0f; a.Sleep = 1f; a.BodyDip = 1f; a.HeadTilt = 8f; a.Breathe = 0.15f;
                    break;
                case "pet":
                    a.EyeOpen = 0.12f; a.HeadTilt = -5f; a.EarTwitch = 1f; a.PurrWobble = 1f;
                    break;
                case "alert":
                    a.EyeOpen = 1f; a.HeadTilt = -3f; a.EarTwitch = 1f;
                    break;
                case "lick":
                    a.LeftPaw = 0.85f; a.HeadTilt = -9f; a.EyeOpen = 0.5f;
                    break;
                case "watch":
                    a.PupilX = 0.9f; a.HeadTilt = 4f;
                    break;
                default:
                    a.Breathe = 0.6f; a.TailAngle = 6f;
                    break;
            }
            return a;
        }
    }
    #endregion
}