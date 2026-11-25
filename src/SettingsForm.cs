// FlatCat - 设置窗口：外观 / 行为 / 提醒（番茄钟、喝水、倒计时、闹钟、整点）/ 系统
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FlatCat
{
    internal sealed class SettingsForm : Form
    {
        private readonly Settings _live;
        private readonly Settings _work;
        private readonly Action<Settings> _onApply;

        // 外观
        private ComboBox _coatBox;
        private Panel _coatPreview;
        private ComboBox _accessoryBox;
        private TrackBar _scaleBar;
        private Label _scaleLabel;
        private TrackBar _opacityBar;
        private Label _opacityLabel;
        private CheckBox _showKeyboard;
        private CheckBox _showSpots;
        private CheckBox _hetero;
        private CheckBox _clockMode;

        // 行为
        private NumericUpDown _sleepMinutes;
        private CheckBox _idleActions;
        private CheckBox _leftHanded;
        private CheckBox _privacy;
        private CheckBox _showKeyLabels;
        private CheckBox _lockThrough;
        private CheckBox _purr;

        // 提醒
        private NumericUpDown _focusMinutes;
        private NumericUpDown _breakMinutes;
        private CheckBox _autoContinue;
        private Label _todayLabel;
        private CheckBox _waterOn;
        private NumericUpDown _waterMinutes;
        private ListBox _alarmList;
        private DateTimePicker _alarmTime;
        private TextBox _alarmLabel;
        private CheckBox[] _dayBoxes = new CheckBox[7];
        private ComboBox _alarmSound;
        private CheckBox _alarmEnabled;
        private Button _addButton;
        private Label _nextLabel;
        private int _editingIndex = -1;
        private NumericUpDown _countdownMinutes;
        private NumericUpDown _snoozeMinutes;
        private CheckBox _hourly;
        private NumericUpDown _hourlyStart;
        private NumericUpDown _hourlyEnd;

        // 系统
        private CheckBox _autostart;
        private Label _statLabel;
        private ListBox _historyList;

        public event EventHandler StartCountdownRequested;
        public event EventHandler StartFocusRequested;
        public event EventHandler TestAlertRequested;

        public SettingsForm(Settings live, Action<Settings> onApply)
        {
            _live = live;
            _work = live.Clone();
            _onApply = onApply;

            Text = "桌面小猫 · 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(652, 548);
            TopMost = true;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.None;

            BuildTabs();
            BuildButtons();
            LoadValues();
        }

        private TabControl _tabs;

        private void BuildTabs()
        {
            _tabs = new TabControl();
            _tabs.SetBounds(12, 12, 628, 486);
            _tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _tabs.TabPages.Add(BuildAppearancePage());
            _tabs.TabPages.Add(BuildBehaviorPage());
            _tabs.TabPages.Add(BuildReminderPage());
            _tabs.TabPages.Add(BuildSystemPage());

            Controls.Add(_tabs);
        }

        // ---------------------------------------------------------------- 外观
        private TabPage BuildAppearancePage()
        {
            TabPage page = NewPage("外观");

            page.Controls.Add(NewLabel("毛色", 20, 26));
            _coatBox = new ComboBox();
            _coatBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _coatBox.SetBounds(112, 22, 190, 24);
            _coatBox.Items.AddRange(Coats.Names());
            _coatBox.SelectedIndexChanged += delegate { UpdateCoatPreview(); };
            page.Controls.Add(_coatBox);

            _coatPreview = new Panel();
            _coatPreview.SetBounds(320, 18, 150, 34);
            _coatPreview.BorderStyle = BorderStyle.FixedSingle;
            _coatPreview.BackgroundImageLayout = ImageLayout.Center;
            page.Controls.Add(_coatPreview);

            page.Controls.Add(NewLabel("配饰", 20, 72));
            _accessoryBox = new ComboBox();
            _accessoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _accessoryBox.SetBounds(112, 68, 190, 24);
            _accessoryBox.Items.AddRange(new object[] { "无配饰", "小铃铛", "围巾", "蝴蝶结", "眼镜" });
            page.Controls.Add(_accessoryBox);

            page.Controls.Add(NewLabel("大小", 20, 112));
            _scaleBar = new TrackBar();
            _scaleBar.SetBounds(112, 102, 290, 45);
            _scaleBar.Minimum = 50;
            _scaleBar.Maximum = 160;
            _scaleBar.TickFrequency = 10;
            _scaleBar.SmallChange = 5;
            _scaleBar.LargeChange = 10;
            _scaleBar.ValueChanged += delegate { _scaleLabel.Text = _scaleBar.Value + "%"; };
            page.Controls.Add(_scaleBar);
            _scaleLabel = NewLabel("100%", 410, 112);
            _scaleLabel.Font = new Font(Font, FontStyle.Bold);
            page.Controls.Add(_scaleLabel);

            page.Controls.Add(NewLabel("不透明度", 20, 166));
            _opacityBar = new TrackBar();
            _opacityBar.SetBounds(112, 156, 290, 45);
            _opacityBar.Minimum = 40;
            _opacityBar.Maximum = 100;
            _opacityBar.TickFrequency = 10;
            _opacityBar.ValueChanged += delegate { _opacityLabel.Text = _opacityBar.Value + "%"; };
            page.Controls.Add(_opacityBar);
            _opacityLabel = NewLabel("100%", 410, 166);
            _opacityLabel.Font = new Font(Font, FontStyle.Bold);
            page.Controls.Add(_opacityLabel);

            _showKeyboard = NewCheck("猫脚下显示小键盘", 20, 210, 240);
            page.Controls.Add(_showKeyboard);

            _showSpots = NewCheck("显示斑纹", 20, 240, 140);
            page.Controls.Add(_showSpots);

            _hetero = NewCheck("异瞳（一只眼换色）", 170, 240, 200);
            page.Controls.Add(_hetero);

            _clockMode = NewCheck("头顶常显当前时间", 20, 270, 240);
            page.Controls.Add(_clockMode);

            Label tip = NewLabel("", 20, 304);
            tip.SetBounds(20, 304, 570, 60);
            tip.ForeColor = Color.DimGray;
            tip.Text = "毛色、配饰、斑纹、异瞳、时间显示点「确定 / 应用」后立即生效。\r\n" +
                        "大小与不透明度同样即时套用；窗口会重新定位，避免跑到屏幕外。";
            page.Controls.Add(tip);

            return page;
        }

        // ---------------------------------------------------------------- 行为
        private TabPage BuildBehaviorPage()
        {
            TabPage page = NewPage("行为");

            page.Controls.Add(NewLabel("闲置多久算睡着了", 20, 30));
            _sleepMinutes = new NumericUpDown();
            _sleepMinutes.SetBounds(200, 26, 80, 24);
            _sleepMinutes.Minimum = 0;
            _sleepMinutes.Maximum = 120;
            page.Controls.Add(_sleepMinutes);
            page.Controls.Add(NewLabel("分钟（0 = 不睡）", 288, 30));

            _idleActions = NewCheck("闲置时随机做小动作（舔爪、歪头）", 20, 72, 420);
            page.Controls.Add(_idleActions);

            _leftHanded = NewCheck("左撇子模式（抬爪左右对调）", 20, 108, 420);
            page.Controls.Add(_leftHanded);

            _privacy = NewCheck("隐私模式（只显示左右手，不显示按了哪个键）", 20, 144, 460);
            page.Controls.Add(_privacy);

            _showKeyLabels = NewCheck("显示按键名（打字时头顶气泡显示键名）", 20, 180, 460);
            page.Controls.Add(_showKeyLabels);

            _lockThrough = NewCheck("锁定点击穿透（锁定后猫身也不再接收点击）", 20, 216, 460);
            page.Controls.Add(_lockThrough);

            _purr = NewCheck("摸头时播放呼噜声", 20, 252, 460);
            page.Controls.Add(_purr);

            Label tip = NewLabel("", 20, 300);
            tip.SetBounds(20, 296, 570, 40);
            tip.ForeColor = Color.DimGray;
            tip.Text = "隐私模式只是不显示键名：程序始终只取虚拟键码，不记录字符、不写盘、不联网。";
            page.Controls.Add(tip);

            return page;
        }

        // ---------------------------------------------------------------- 提醒
        private TabPage BuildReminderPage()
        {
            TabPage page = NewPage("提醒");

            // 番茄钟
            GroupBox pomo = new GroupBox();
            pomo.Text = "番茄钟";
            pomo.SetBounds(14, 12, 290, 146);
            page.Controls.Add(pomo);

            pomo.Controls.Add(NewLabel("专注", 12, 28));
            _focusMinutes = new NumericUpDown();
            _focusMinutes.SetBounds(60, 24, 56, 24);
            _focusMinutes.Minimum = 1;
            _focusMinutes.Maximum = 180;
            pomo.Controls.Add(_focusMinutes);
            pomo.Controls.Add(NewLabel("分钟", 120, 28));

            pomo.Controls.Add(NewLabel("休息", 156, 28));
            _breakMinutes = new NumericUpDown();
            _breakMinutes.SetBounds(196, 24, 52, 24);
            _breakMinutes.Minimum = 1;
            _breakMinutes.Maximum = 60;
            pomo.Controls.Add(_breakMinutes);
            pomo.Controls.Add(NewLabel("分", 250, 28));

            _autoContinue = NewCheck("专注结束自动开始休息", 12, 58, 262);
            pomo.Controls.Add(_autoContinue);

            _todayLabel = NewLabel("今日完成 0 个番茄", 12, 88);
            _todayLabel.SetBounds(12, 86, 262, 20);
            pomo.Controls.Add(_todayLabel);

            Button focusNow = new Button();
            focusNow.Text = "立即开始专注";
            focusNow.SetBounds(12, 110, 110, 26);
            focusNow.Click += delegate
            {
                Collect();
                if (StartFocusRequested != null) StartFocusRequested(this, EventArgs.Empty);
            };
            pomo.Controls.Add(focusNow);

            // 喝水
            GroupBox water = new GroupBox();
            water.Text = "喝水";
            water.SetBounds(316, 12, 292, 88);
            page.Controls.Add(water);

            _waterOn = NewCheck("开启喝水提醒", 12, 24, 240);
            water.Controls.Add(_waterOn);

            water.Controls.Add(NewLabel("每隔", 12, 56));
            _waterMinutes = new NumericUpDown();
            _waterMinutes.SetBounds(56, 52, 60, 24);
            _waterMinutes.Minimum = 5;
            _waterMinutes.Maximum = 240;
            water.Controls.Add(_waterMinutes);
            water.Controls.Add(NewLabel("分钟提醒一次", 124, 56));

            // 倒计时
            GroupBox countdown = new GroupBox();
            countdown.Text = "倒计时";
            countdown.SetBounds(316, 108, 292, 50);
            page.Controls.Add(countdown);

            _countdownMinutes = new NumericUpDown();
            _countdownMinutes.SetBounds(12, 16, 56, 24);
            _countdownMinutes.Minimum = 1;
            _countdownMinutes.Maximum = 600;
            _countdownMinutes.Value = 10;
            countdown.Controls.Add(_countdownMinutes);
            countdown.Controls.Add(NewLabel("分钟后提醒", 74, 20));

            Button startCd = new Button();
            startCd.Text = "开始";
            startCd.SetBounds(190, 16, 56, 24);
            startCd.Click += delegate
            {
                Collect();
                if (StartCountdownRequested != null) StartCountdownRequested(this, EventArgs.Empty);
            };
            countdown.Controls.Add(startCd);

            // 闹钟
            GroupBox alarmBox = new GroupBox();
            alarmBox.Text = "闹钟（可多个，双击列表可编辑）";
            alarmBox.SetBounds(14, 164, 594, 202);
            page.Controls.Add(alarmBox);

            _alarmList = new ListBox();
            _alarmList.SetBounds(12, 22, 230, 130);
            _alarmList.IntegralHeight = false;
            _alarmList.DoubleClick += delegate { BeginEditAlarm(); };
            alarmBox.Controls.Add(_alarmList);

            Button edit = new Button();
            edit.Text = "编辑";
            edit.SetBounds(12, 158, 52, 26);
            edit.Click += delegate { BeginEditAlarm(); };
            alarmBox.Controls.Add(edit);

            Button remove = new Button();
            remove.Text = "删除";
            remove.SetBounds(70, 158, 52, 26);
            remove.Click += delegate
            {
                if (_alarmList.SelectedIndex < 0) return;
                _work.Alarms.RemoveAt(_alarmList.SelectedIndex);
                _editingIndex = -1;
                _addButton.Text = "添加闹钟";
                RefreshAlarms();
            };
            alarmBox.Controls.Add(remove);

            Button toggle = new Button();
            toggle.Text = "启用 / 停用";
            toggle.SetBounds(128, 158, 84, 26);
            toggle.Click += delegate
            {
                if (_alarmList.SelectedIndex < 0) return;
                Alarm a = _work.Alarms[_alarmList.SelectedIndex];
                a.Enabled = !a.Enabled;
                RefreshAlarms();
            };
            alarmBox.Controls.Add(toggle);

            alarmBox.Controls.Add(NewLabel("时间", 252, 26));
            _alarmTime = new DateTimePicker();
            _alarmTime.Format = DateTimePickerFormat.Custom;
            _alarmTime.CustomFormat = "HH:mm";
            _alarmTime.ShowUpDown = true;
            _alarmTime.SetBounds(296, 22, 78, 24);
            _alarmTime.Value = DateTime.Today.AddHours(9);
            alarmBox.Controls.Add(_alarmTime);

            alarmBox.Controls.Add(NewLabel("标签", 384, 26));
            _alarmLabel = new TextBox();
            _alarmLabel.SetBounds(428, 22, 148, 24);
            _alarmLabel.Text = "提醒";
            alarmBox.Controls.Add(_alarmLabel);

            alarmBox.Controls.Add(NewLabel("重复", 252, 60));
            string[] dayNames = new string[] { "一", "二", "三", "四", "五", "六", "日" };
            for (int i = 0; i < 7; i++)
            {
                CheckBox cb = new CheckBox();
                cb.Text = dayNames[i];
                cb.SetBounds(296 + i * 38, 58, 36, 22);
                cb.Checked = true;
                _dayBoxes[i] = cb;
                alarmBox.Controls.Add(cb);
            }

            alarmBox.Controls.Add(NewLabel("铃声", 252, 94));
            _alarmSound = new ComboBox();
            _alarmSound.DropDownStyle = ComboBoxStyle.DropDownList;
            _alarmSound.SetBounds(296, 90, 120, 24);
            _alarmSound.Items.AddRange(new object[] { "系统提示音", "猫叫", "静音" });
            alarmBox.Controls.Add(_alarmSound);

            _alarmEnabled = NewCheck("启用", 428, 90, 70);
            _alarmEnabled.Checked = true;
            alarmBox.Controls.Add(_alarmEnabled);

            _addButton = new Button();
            _addButton.Text = "添加闹钟";
            _addButton.SetBounds(296, 124, 86, 26);
            _addButton.Click += delegate { CommitAlarm(); };
            alarmBox.Controls.Add(_addButton);

            Button clear = new Button();
            clear.Text = "清空";
            clear.SetBounds(390, 124, 60, 26);
            clear.Click += delegate
            {
                _work.Alarms.Clear();
                _editingIndex = -1;
                _addButton.Text = "添加闹钟";
                RefreshAlarms();
            };
            alarmBox.Controls.Add(clear);

            _nextLabel = NewLabel("", 252, 154);
            _nextLabel.SetBounds(252, 154, 330, 36);
            _nextLabel.ForeColor = Color.DimGray;
            alarmBox.Controls.Add(_nextLabel);

            // 增强：贪睡 / 整点报时 / 测试
            GroupBox extra = new GroupBox();
            extra.Text = "增强";
            extra.SetBounds(14, 372, 594, 78);
            page.Controls.Add(extra);

            CheckBox snooze = NewCheck("贪睡", 12, 20, 60);
            snooze.Checked = true;
            extra.Controls.Add(snooze);

            _snoozeMinutes = new NumericUpDown();
            _snoozeMinutes.SetBounds(74, 18, 50, 24);
            _snoozeMinutes.Minimum = 0;
            _snoozeMinutes.Maximum = 60;
            extra.Controls.Add(_snoozeMinutes);
            Label snoozeTip = NewLabel("分钟后补一次（0 = 不补）", 132, 22);
            snoozeTip.SetBounds(132, 22, 200, 20);
            extra.Controls.Add(snoozeTip);

            _hourly = NewCheck("整点报时", 12, 48, 80);
            extra.Controls.Add(_hourly);

            _hourlyStart = new NumericUpDown();
            _hourlyStart.SetBounds(96, 46, 46, 24);
            _hourlyStart.Minimum = 0;
            _hourlyStart.Maximum = 23;
            extra.Controls.Add(_hourlyStart);

            Label toLabel = NewLabel("至", 148, 50);
            toLabel.SetBounds(148, 50, 16, 20);
            extra.Controls.Add(toLabel);

            _hourlyEnd = new NumericUpDown();
            _hourlyEnd.SetBounds(166, 46, 46, 24);
            _hourlyEnd.Minimum = 0;
            _hourlyEnd.Maximum = 24;
            extra.Controls.Add(_hourlyEnd);

            Label hourTip = NewLabel("点之间，每小时一次", 218, 50);
            hourTip.SetBounds(218, 50, 150, 20);
            extra.Controls.Add(hourTip);

            Button test = new Button();
            test.Text = "预览提醒";
            test.SetBounds(392, 22, 88, 28);
            test.Click += delegate
            {
                Collect();
                if (TestAlertRequested != null) TestAlertRequested(this, EventArgs.Empty);
            };
            extra.Controls.Add(test);

            Label testTip = NewLabel("先看效果，不写配置", 392, 52);
            testTip.SetBounds(392, 52, 180, 20);
            testTip.ForeColor = Color.DimGray;
            extra.Controls.Add(testTip);

            return page;
        }

        // ---------------------------------------------------------------- 系统
        private TabPage BuildSystemPage()
        {
            TabPage page = NewPage("系统");

            _autostart = NewCheck("开机自动启动", 20, 26, 260);
            page.Controls.Add(_autostart);

            page.Controls.Add(NewLabel("配置文件", 20, 70));
            TextBox pathBox = new TextBox();
            pathBox.SetBounds(112, 66, 450, 24);
            pathBox.ReadOnly = true;
            pathBox.Text = Settings.FilePath;
            page.Controls.Add(pathBox);

            Button openDir = new Button();
            openDir.Text = "打开所在文件夹";
            openDir.SetBounds(112, 100, 132, 28);
            openDir.Click += delegate
            {
                try { Process.Start("explorer.exe", "/select,\"" + Settings.FilePath + "\""); }
                catch { }
            };
            page.Controls.Add(openDir);

            _statLabel = NewLabel("", 20, 144);
            _statLabel.SetBounds(20, 142, 560, 20);
            page.Controls.Add(_statLabel);

            Label ver = NewLabel("FlatCat 1.3 · .NET Framework 4.8 · 单文件约 150 KB", 20, 176);
            ver.SetBounds(20, 174, 560, 20);
            page.Controls.Add(ver);

            page.Controls.Add(NewLabel("最近提醒", 20, 204));
            _historyList = new ListBox();
            _historyList.SetBounds(20, 226, 570, 152);
            _historyList.IntegralHeight = false;
            page.Controls.Add(_historyList);

            Label note = NewLabel("", 20, 390);
            note.SetBounds(20, 388, 570, 64);
            note.ForeColor = Color.DimGray;
            note.Text = "程序只读取全局按键的虚拟键码用于决定抬哪只爪，不记录字符内容、不写日志、不联网。\r\n" +
                        "透明窗口带 WS_EX_NOACTIVATE，点猫不会抢走前台窗口的焦点。\r\n" +
                        "双击猫或双击托盘图标都能直接打开这个设置窗口。";
            page.Controls.Add(note);

            return page;
        }

        private void BuildButtons()
        {
            Button ok = new Button();
            ok.Text = "确定";
            ok.SetBounds(436, 508, 84, 30);
            ok.Click += delegate { Collect(); Apply(); Close(); };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.SetBounds(528, 508, 84, 30);
            cancel.Click += delegate { Close(); };
            Controls.Add(cancel);

            Button apply = new Button();
            apply.Text = "应用";
            apply.SetBounds(336, 508, 84, 30);
            apply.Click += delegate { Collect(); Apply(); };
            Controls.Add(apply);
        }

        private static TabPage NewPage(string title)
        {
            TabPage page = new TabPage(title);
            page.BackColor = SystemColors.Window;
            page.Padding = new Padding(0);
            return page;
        }

        private static Label NewLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = false;
            // 按内容估算宽度：固定 120 会把右边的输入框盖住（Label 会画自家背景色）
            int w = 0;
            for (int i = 0; i < text.Length; i++)
                w += (text[i] < 128) ? 8 : 15;
            l.SetBounds(x, y, Math.Max(20, w + 8), 20);
            return l;
        }

        private static CheckBox NewCheck(string text, int x, int y, int w)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.SetBounds(x, y, w, 22);
            return c;
        }

        private void LoadValues()
        {
            _coatBox.SelectedIndex = Math.Max(0, Math.Min(Coats.All.Length - 1, _work.CoatId));
            UpdateCoatPreview();

            _accessoryBox.SelectedIndex = Math.Max(0, Math.Min(4, _work.AccessoryId));

            _scaleBar.Value = (int)Math.Round(_work.Scale * 100);
            _scaleBar.Value = Math.Max(_scaleBar.Minimum, Math.Min(_scaleBar.Maximum, _scaleBar.Value));
            _scaleLabel.Text = _scaleBar.Value + "%";

            _opacityBar.Value = (int)Math.Round(_work.Opacity * 100);
            _opacityBar.Value = Math.Max(_opacityBar.Minimum, Math.Min(_opacityBar.Maximum, _opacityBar.Value));
            _opacityLabel.Text = _opacityBar.Value + "%";

            _showKeyboard.Checked = _work.ShowKeyboard;
            _showSpots.Checked = _work.ShowSpots;
            _hetero.Checked = _work.Heterochromia;
            _clockMode.Checked = _work.ClockMode;

            _sleepMinutes.Value = Math.Max(_sleepMinutes.Minimum, Math.Min(_sleepMinutes.Maximum, _work.SleepMinutes));
            _idleActions.Checked = _work.IdleActions;
            _leftHanded.Checked = _work.LeftHanded;
            _privacy.Checked = _work.PrivacyMode;
            _showKeyLabels.Checked = _work.ShowKeyLabels;
            _lockThrough.Checked = _work.ClickThroughLocked;
            _purr.Checked = _work.Purr;

            _focusMinutes.Value = Math.Max(_focusMinutes.Minimum, Math.Min(_focusMinutes.Maximum, _work.PomodoroFocus));
            _breakMinutes.Value = Math.Max(_breakMinutes.Minimum, Math.Min(_breakMinutes.Maximum, _work.PomodoroBreak));
            _autoContinue.Checked = _work.PomodoroAutoContinue;
            _todayLabel.Text = "今日完成 " + _work.TodayPomodoros + " 个番茄";
            _statLabel.Text = "今日 " + _work.TodayPomodoros + " 个番茄 · 累计 " + _work.TotalPomodoros + " 个";

            _waterOn.Checked = _work.WaterEnabled;
            _waterMinutes.Value = Math.Max(_waterMinutes.Minimum, Math.Min(_waterMinutes.Maximum, _work.WaterMinutes));

            _snoozeMinutes.Value = Math.Max(_snoozeMinutes.Minimum, Math.Min(_snoozeMinutes.Maximum, _work.SnoozeMinutes));
            _hourly.Checked = _work.HourlyChime;
            _hourlyStart.Value = Math.Max(_hourlyStart.Minimum, Math.Min(_hourlyStart.Maximum, _work.HourlyStart));
            _hourlyEnd.Value = Math.Max(_hourlyEnd.Minimum, Math.Min(_hourlyEnd.Maximum, _work.HourlyEnd));

            _alarmSound.SelectedIndex = Math.Max(0, Math.Min(2, _work.AlarmSound));
            _autostart.Checked = _work.Autostart;

            _historyList.BeginUpdate();
            _historyList.Items.Clear();
            if (_work.AlertLog != null)
            {
                for (int i = _work.AlertLog.Count - 1; i >= 0; i--) _historyList.Items.Add(_work.AlertLog[i]);
            }
            if (_historyList.Items.Count == 0) _historyList.Items.Add("（今天还没有提醒）");
            _historyList.EndUpdate();

            RefreshAlarms();
        }

        private void UpdateCoatPreview()
        {
            int idx = _coatBox.SelectedIndex;
            if (idx < 0) idx = 0;
            Coat c = Coats.ById(idx);
            Bitmap bmp = new Bitmap(28, 28);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                using (SolidBrush b = new SolidBrush(c.Fur)) g.FillEllipse(b, 2, 4, 24, 22);
                using (SolidBrush b = new SolidBrush(c.Eye)) g.FillEllipse(b, 8, 12, 5, 5);
                using (SolidBrush b = new SolidBrush(c.Eye)) g.FillEllipse(b, 15, 12, 5, 5);
                using (SolidBrush b = new SolidBrush(c.Pink))
                {
                    Point[] ear = new Point[] { new Point(4, 8), new Point(9, 1), new Point(13, 7) };
                    g.FillPolygon(b, ear);
                }
            }
            if (_coatPreview.BackgroundImage != null) _coatPreview.BackgroundImage.Dispose();
            _coatPreview.BackgroundImage = bmp;
        }

        private int DaysMask()
        {
            int mask = 0;
            for (int i = 0; i < 7; i++)
                if (_dayBoxes[i] != null && _dayBoxes[i].Checked) mask |= (1 << i);
            return mask;
        }

        private void BeginEditAlarm()
        {
            if (_alarmList.SelectedIndex < 0 || _alarmList.SelectedIndex >= _work.Alarms.Count) return;
            Alarm a = _work.Alarms[_alarmList.SelectedIndex];

            _alarmTime.Value = DateTime.Today.AddHours(Math.Min(23, Math.Max(0, a.Hour)))
                                             .AddMinutes(Math.Min(59, Math.Max(0, a.Minute)));
            _alarmLabel.Text = a.Label;

            int days = a.Days;
            if (days == 0)
            {
                if (a.Daily) days = 0x7F;
                else if (a.Weekdays) days = 0x1F;
            }
            for (int i = 0; i < 7; i++) _dayBoxes[i].Checked = (days & (1 << i)) != 0;

            _alarmSound.SelectedIndex = Math.Max(0, Math.Min(2, a.Sound));
            _alarmEnabled.Checked = a.Enabled;
            _editingIndex = _alarmList.SelectedIndex;
            _addButton.Text = "保存修改";
        }

        private void CommitAlarm()
        {
            int days = DaysMask();
            Alarm a;
            if (_editingIndex >= 0 && _editingIndex < _work.Alarms.Count)
            {
                a = _work.Alarms[_editingIndex];
            }
            else
            {
                a = new Alarm();
            }

            a.Label = string.IsNullOrEmpty(_alarmLabel.Text.Trim()) ? "提醒" : _alarmLabel.Text.Trim();
            a.Hour = _alarmTime.Value.Hour;
            a.Minute = _alarmTime.Value.Minute;
            a.Days = days;
            a.Daily = (days == 0x7F);
            a.Weekdays = (days == 0x1F);
            a.Sound = Math.Max(0, _alarmSound.SelectedIndex);
            a.Enabled = _alarmEnabled.Checked;

            if (_editingIndex < 0) _work.Alarms.Add(a);
            _editingIndex = -1;
            _addButton.Text = "添加闹钟";
            RefreshAlarms();
        }

        private void RefreshAlarms()
        {
            _alarmList.BeginUpdate();
            _alarmList.Items.Clear();
            for (int i = 0; i < _work.Alarms.Count; i++)
                _alarmList.Items.Add(_work.Alarms[i].ToString());
            _alarmList.EndUpdate();
            _nextLabel.Text = "下次：" + Reminder.NextAlarmText(_work.Alarms, DateTime.Now);
        }

        private void Collect()
        {
            _work.CoatId = Math.Max(0, _coatBox.SelectedIndex);
            _work.AccessoryId = Math.Max(0, _accessoryBox.SelectedIndex);
            _work.ShowSpots = _showSpots.Checked;
            _work.Heterochromia = _hetero.Checked;
            _work.ClockMode = _clockMode.Checked;
            _work.Scale = _scaleBar.Value / 100.0;
            _work.Opacity = _opacityBar.Value / 100.0;
            _work.ShowKeyboard = _showKeyboard.Checked;

            _work.SleepMinutes = (int)_sleepMinutes.Value;
            _work.IdleActions = _idleActions.Checked;
            _work.LeftHanded = _leftHanded.Checked;
            _work.PrivacyMode = _privacy.Checked;
            _work.ShowKeyLabels = _showKeyLabels.Checked;
            _work.ClickThroughLocked = _lockThrough.Checked;
            _work.Purr = _purr.Checked;

            _work.PomodoroFocus = (int)_focusMinutes.Value;
            _work.PomodoroBreak = (int)_breakMinutes.Value;
            _work.PomodoroAutoContinue = _autoContinue.Checked;
            _work.WaterEnabled = _waterOn.Checked;
            _work.WaterMinutes = (int)_waterMinutes.Value;
            _work.SnoozeMinutes = (int)_snoozeMinutes.Value;
            _work.HourlyChime = _hourly.Checked;
            _work.HourlyStart = (int)_hourlyStart.Value;
            _work.HourlyEnd = (int)_hourlyEnd.Value;
            _work.AlarmSound = Math.Max(0, _alarmSound.SelectedIndex);

            _work.Autostart = _autostart.Checked;
        }

        private void Apply()
        {
            _live.CopyFrom(_work);
            _live.Save();
            if (_onApply != null) _onApply(_live);
        }

        /// <summary>供 --settings N 直达某一页：0 外观 / 1 行为 / 2 提醒 / 3 系统。</summary>
        public void SelectPage(int index)
        {
            if (index >= 0 && index < _tabs.TabPages.Count) _tabs.SelectedIndex = index;
        }

        public int CountdownMinutes
        {
            get { return (int)_countdownMinutes.Value; }
        }

        /// <summary>当前在设置里选中的铃声音号，用于「预览提醒」。</summary>
        public int SelectedSound
        {
            get { return Math.Max(0, _alarmSound.SelectedIndex); }
        }
    }
}