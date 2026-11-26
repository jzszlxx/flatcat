// FlatCat - 设置窗口：外观 / 行为 / 提醒（番茄钟、喝水、倒计时、闹钟、整点）/ 系统
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // 截图
        private CheckBox _captureHideCat;
        private CheckBox _captureCopy;
        private TextBox _captureDir;

        // 媒体
        private ListBox _radioList;
        private TextBox _radioName;
        private TextBox _radioUrl;
        private TrackBar _volumeBar;
        private Label _volumeLabel;
        private ListBox _musicList;
        private List<string> _musicFiles = new List<string>();
        private Label _audioStatus;

        // 系统
        private CheckBox _autostart;
        private Label _statLabel;
        private ListBox _historyList;

        // v1.5 系统页：热键、静默时段、配置位置
        private CheckBox _hotkeys;
        private CheckBox _quiet;
        private NumericUpDown _quietStart;
        private NumericUpDown _quietEnd;
        private Label _portableLabel;

        public event EventHandler StartCountdownRequested;
        public event EventHandler ShotFullRequested;
        public event EventHandler ShotRectRequested;
        public event EventHandler ShotLassoRequested;
        public event Action<RadioStation> RadioPlayRequested;
        public event EventHandler RadioStopRequested;
        public event EventHandler MusicPickRequested;
        public event Action<List<string>, int> MusicPlayRequested;
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
            ClientSize = new Size(1180, 640);
            TopMost = true;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(247, 242, 234);

            BuildTabs();
            BuildShell();
            BuildButtons();
            LoadValues();
        }

        private TabControl _tabs;

        // v1.5 外壳：左导航 + 搜索 + 实时预览 + 状态栏
        private TextBox _search;
        private ListBox _nav;
        private Panel _preview;
        private Label _status;
        private readonly List<int> _navMap = new List<int>();
        private System.Windows.Forms.Timer _previewTimer;

        private void BuildTabs()
        {
            _tabs = new TabControl();
            // 隐藏标签头：页面切换交给左侧导航
            _tabs.Appearance = TabAppearance.FlatButtons;
            _tabs.ItemSize = new Size(1, 1);
            _tabs.SizeMode = TabSizeMode.Fixed;

            _tabs.TabPages.Add(BuildAppearancePage());
            _tabs.TabPages.Add(BuildBehaviorPage());
            _tabs.TabPages.Add(BuildReminderPage());
            _tabs.TabPages.Add(BuildSystemPage());
            _tabs.TabPages.Add(BuildCapturePage());
            _tabs.TabPages.Add(BuildMediaPage());
            // 布局与加入控件交给 BuildShell
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
            openDir.SetBounds(112, 98, 132, 26);
            openDir.Click += delegate
            {
                try { Process.Start("explorer.exe", "/select,\"" + Settings.FilePath + "\""); }
                catch { }
            };
            page.Controls.Add(openDir);

            // v1.5 全局热键
            page.Controls.Add(NewLabel("全局热键", 20, 138));
            _hotkeys = NewCheck("启用全局热键（Ctrl+Alt+S / R / L / P）", 20, 160, 460);
            page.Controls.Add(_hotkeys);

            Label hkTip = NewLabel("", 20, 186);
            hkTip.SetBounds(20, 184, 620, 34);
            hkTip.ForeColor = Color.DimGray;
            hkTip.Text = "S 全屏 · R 矩形 · L 套索 · P 播放/暂停音乐；Ctrl+Alt 被占用时整组自动换 Ctrl+Shift+Alt。\r\n" +
                         "程序只读全局按键的虚拟键码决定抬哪只爪，不记录字符、不写日志、不联网。";
            page.Controls.Add(hkTip);

            // v1.5 静默时段
            page.Controls.Add(NewLabel("静默时段", 20, 226));
            _quiet = NewCheck("启用（只静音，气泡与日志照旧）", 20, 248, 300);
            page.Controls.Add(_quiet);

            _quietStart = new NumericUpDown();
            _quietStart.SetBounds(330, 246, 56, 24);
            _quietStart.Minimum = 0;
            _quietStart.Maximum = 23;
            page.Controls.Add(_quietStart);

            Label to = NewLabel("至", 392, 250);
            page.Controls.Add(to);

            _quietEnd = new NumericUpDown();
            _quietEnd.SetBounds(414, 246, 56, 24);
            _quietEnd.Minimum = 0;
            _quietEnd.Maximum = 23;
            page.Controls.Add(_quietEnd);

            Label dot = NewLabel("点", 476, 250);
            page.Controls.Add(dot);

            // 便携模式状态
            page.Controls.Add(NewLabel("配置位置", 20, 286));
            _portableLabel = NewLabel("", 112, 286);
            _portableLabel.SetBounds(112, 284, 470, 20);
            page.Controls.Add(_portableLabel);

            Button openCfg = new Button();
            openCfg.Text = "打开配置目录";
            openCfg.SetBounds(112, 310, 132, 26);
            openCfg.Click += delegate
            {
                try { Process.Start("explorer.exe", "\"" + Settings.Dir + "\""); }
                catch { }
            };
            page.Controls.Add(openCfg);

            _statLabel = NewLabel("", 20, 348);
            _statLabel.SetBounds(20, 346, 560, 20);
            page.Controls.Add(_statLabel);

            Label ver = NewLabel("FlatCat 1.5 · .NET Framework 4.8 · 单文件约 240 KB", 20, 374);
            ver.SetBounds(20, 372, 560, 20);
            page.Controls.Add(ver);

            page.Controls.Add(NewLabel("最近提醒", 20, 402));
            _historyList = new ListBox();
            _historyList.SetBounds(20, 424, 570, 116);
            _historyList.IntegralHeight = false;
            page.Controls.Add(_historyList);

            return page;
        }

        // ---------------------------------------------------------------- 截图
        private TabPage BuildCapturePage()
        {
            TabPage page = NewPage("截图");

            _captureHideCat = NewCheck("截图时先藏起小猫（不把自己画进图里）", 20, 26, 500);
            page.Controls.Add(_captureHideCat);

            _captureCopy = NewCheck("截图后同时复制到剪贴板", 20, 60, 400);
            page.Controls.Add(_captureCopy);

            page.Controls.Add(NewLabel("保存位置", 20, 100));
            _captureDir = new TextBox();
            _captureDir.SetBounds(112, 96, 380, 24);
            page.Controls.Add(_captureDir);

            Button browse = new Button();
            browse.Text = "选择…";
            browse.SetBounds(500, 95, 88, 26);
            browse.Click += delegate
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "选择截图保存目录";
                    if (Directory.Exists(_captureDir.Text)) dlg.SelectedPath = _captureDir.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) _captureDir.Text = dlg.SelectedPath;
                }
            };
            page.Controls.Add(browse);

            Button resetDir = new Button();
            resetDir.Text = "恢复默认";
            resetDir.SetBounds(112, 130, 88, 26);
            resetDir.Click += delegate { _captureDir.Text = Shot.DefaultDir; };
            page.Controls.Add(resetDir);

            page.Controls.Add(NewLabel("马上试一下", 20, 176));

            Button full = new Button();
            full.Text = "全屏";
            full.SetBounds(112, 172, 96, 30);
            full.Click += delegate { if (ShotFullRequested != null) ShotFullRequested(this, EventArgs.Empty); };
            page.Controls.Add(full);

            Button rect = new Button();
            rect.Text = "矩形选区";
            rect.SetBounds(216, 172, 96, 30);
            rect.Click += delegate { if (ShotRectRequested != null) ShotRectRequested(this, EventArgs.Empty); };
            page.Controls.Add(rect);

            Button lasso = new Button();
            lasso.Text = "不规则";
            lasso.SetBounds(320, 172, 96, 30);
            lasso.Click += delegate { if (ShotLassoRequested != null) ShotLassoRequested(this, EventArgs.Empty); };
            page.Controls.Add(lasso);

            Label tip = NewLabel("", 20, 222);
            tip.SetBounds(20, 218, 600, 70);
            tip.ForeColor = Color.DimGray;
            tip.Text = "矩形：拖出方框松手；不规则：按住左键沿轮廓描一圈再松手；按 Esc 取消。\r\n" +
                       "存成 PNG，文件名带时间戳，不会覆盖旧图；默认放在「图片\\FlatCat 截图」。";
            page.Controls.Add(tip);

            return page;
        }

        // ---------------------------------------------------------------- 媒体
        private TabPage BuildMediaPage()
        {
            TabPage page = NewPage("媒体");

            page.Controls.Add(NewLabel("电台（预置 + 自定义）", 20, 18));

            _radioList = new ListBox();
            _radioList.SetBounds(20, 42, 380, 178);
            _radioList.IntegralHeight = false;
            page.Controls.Add(_radioList);

            Button playRadio = new Button();
            playRadio.Text = "播放选中";
            playRadio.SetBounds(20, 228, 100, 28);
            playRadio.Click += delegate
            {
                RadioStation st = _radioList.SelectedItem as RadioStation;
                if (st != null && RadioPlayRequested != null) RadioPlayRequested(st);
            };
            page.Controls.Add(playRadio);

            Button stopRadio = new Button();
            stopRadio.Text = "停止";
            stopRadio.SetBounds(128, 228, 80, 28);
            stopRadio.Click += delegate { if (RadioStopRequested != null) RadioStopRequested(this, EventArgs.Empty); };
            page.Controls.Add(stopRadio);

            page.Controls.Add(NewLabel("名称", 416, 42));
            _radioName = new TextBox();
            _radioName.SetBounds(416, 62, 200, 24);
            page.Controls.Add(_radioName);

            page.Controls.Add(NewLabel("流地址", 416, 94));
            _radioUrl = new TextBox();
            _radioUrl.SetBounds(416, 114, 200, 24);
            page.Controls.Add(_radioUrl);

            Button addRadio = new Button();
            addRadio.Text = "添加电台";
            addRadio.SetBounds(416, 146, 96, 28);
            addRadio.Click += delegate { AddRadio(); };
            page.Controls.Add(addRadio);

            Button delRadio = new Button();
            delRadio.Text = "删除选中";
            delRadio.SetBounds(520, 146, 96, 28);
            delRadio.Click += delegate { DeleteRadio(); };
            page.Controls.Add(delRadio);

            page.Controls.Add(NewLabel("音量", 416, 184));
            _volumeBar = new TrackBar();
            _volumeBar.SetBounds(416, 204, 180, 34);
            _volumeBar.Minimum = 0;
            _volumeBar.Maximum = 100;
            _volumeBar.TickFrequency = 25;
            _volumeBar.ValueChanged += delegate { _volumeLabel.Text = _volumeBar.Value + "%"; };
            page.Controls.Add(_volumeBar);
            _volumeLabel = NewLabel("70%", 600, 190);
            page.Controls.Add(_volumeLabel);

            page.Controls.Add(NewLabel("本地音乐", 20, 272));

            _musicList = new ListBox();
            _musicList.SetBounds(20, 296, 380, 132);
            _musicList.IntegralHeight = false;
            page.Controls.Add(_musicList);

            Button pickFiles = new Button();
            pickFiles.Text = "选文件夹…";
            pickFiles.SetBounds(416, 296, 96, 28);
            pickFiles.Click += delegate { if (MusicPickRequested != null) MusicPickRequested(this, EventArgs.Empty); };
            page.Controls.Add(pickFiles);

            Button playMusic = new Button();
            playMusic.Text = "播放选中";
            playMusic.SetBounds(520, 296, 96, 28);
            playMusic.Click += delegate
            {
                if (_musicFiles == null || _musicList.SelectedIndex < 0) return;
                if (MusicPlayRequested != null) MusicPlayRequested(new List<string>(_musicFiles), _musicList.SelectedIndex);
            };
            page.Controls.Add(playMusic);

            Label musicTip = NewLabel("", 416, 336);
            musicTip.SetBounds(416, 334, 210, 60);
            musicTip.ForeColor = Color.DimGray;
            musicTip.Text = "支持 mp3 / wav / wma /\r\nm4a / aac / flac，最多\r\n收录 500 首。";
            page.Controls.Add(musicTip);

            _audioStatus = NewLabel("", 20, 440);
            _audioStatus.SetBounds(20, 438, 600, 20);
            _audioStatus.ForeColor = Color.DimGray;
            page.Controls.Add(_audioStatus);

            return page;
        }

        private void AddRadio()
        {
            string name = _radioName.Text.Trim();
            string url = _radioUrl.Text.Trim();
            if (url.Length == 0)
            {
                MessageBox.Show(this, "流地址不能为空。", "添加电台", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed))
            {
                MessageBox.Show(this, "地址格式不对，应以 http:// 开头。\r\n注意：本程序内置播放器对 https 流可能打不开，建议用 http 流。",
                    "添加电台", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (name.Length == 0) name = "自定义电台";
            _work.CustomRadios.Add(new RadioStation(name, url));
            _radioName.Text = "";
            _radioUrl.Text = "";
            RefreshRadios();
        }

        private void DeleteRadio()
        {
            RadioStation st = _radioList.SelectedItem as RadioStation;
            if (st == null || _work.CustomRadios == null) return;
            for (int i = 0; i < _work.CustomRadios.Count; i++)
            {
                if (_work.CustomRadios[i].Url == st.Url)
                {
                    _work.CustomRadios.RemoveAt(i);
                    break;
                }
            }
            RefreshRadios();
        }

        private void RefreshRadios()
        {
            _radioList.BeginUpdate();
            _radioList.Items.Clear();
            List<RadioStation> presets = Radios.Presets();
            for (int i = 0; i < presets.Count; i++)
                _radioList.Items.Add(presets[i]);
            if (_work.CustomRadios != null)
            {
                for (int i = 0; i < _work.CustomRadios.Count; i++)
                    _radioList.Items.Add(_work.CustomRadios[i]);
            }
            _radioList.EndUpdate();
        }

        private void RefreshMusicList()
        {
            _musicFiles = AudioPlayer.ScanFolder(_work.MusicFolder);
            _musicList.BeginUpdate();
            _musicList.Items.Clear();
            if (_musicFiles.Count == 0) _musicList.Items.Add("（还没选音乐文件夹）");
            else
            {
                for (int i = 0; i < _musicFiles.Count; i++)
                    _musicList.Items.Add(Path.GetFileName(_musicFiles[i]));
            }
            _musicList.EndUpdate();
        }

        /// <summary>由主窗口回调，刷新播放状态行。</summary>
        public void SetAudioStatus(string text)
        {
            if (_audioStatus != null) _audioStatus.Text = text;
        }

        // ---------------------------------------------------------------- 外壳
        private void BuildShell()
        {
            _search = new TextBox();
            _search.SetBounds(12, 12, 166, 26);
            _search.Text = "搜索设置…";
            _search.ForeColor = Color.Gray;
            _search.GotFocus += delegate
            {
                if (_search.ForeColor == Color.Gray)
                {
                    _search.Text = "";
                    _search.ForeColor = Color.FromArgb(38, 34, 32);
                }
            };
            _search.LostFocus += delegate
            {
                if (_search.Text.Trim().Length == 0)
                {
                    _search.Text = "搜索设置…";
                    _search.ForeColor = Color.Gray;
                }
            };
            _search.TextChanged += delegate { RebuildNav(); };
            Controls.Add(_search);

            _nav = new ListBox();
            _nav.SetBounds(0, 46, 190, 556);
            _nav.BorderStyle = BorderStyle.None;
            _nav.DrawMode = DrawMode.OwnerDrawFixed;
            _nav.ItemHeight = 42;
            _nav.BackColor = Color.FromArgb(242, 235, 224);
            _nav.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f);
            _nav.DrawItem += DrawNavItem;
            _nav.SelectedIndexChanged += delegate { SyncPageFromNav(); };
            Controls.Add(_nav);

            _tabs.SetBounds(190, 12, 700, 580);
            _tabs.SelectedIndexChanged += delegate { SyncNavFromPage(); };
            Controls.Add(_tabs);

            _preview = new Panel();
            _preview.SetBounds(902, 12, 262, 262);
            _preview.BackColor = Color.FromArgb(242, 235, 224);
            _preview.Paint += delegate(object s, PaintEventArgs e) { PaintPreview(e.Graphics, _preview.ClientRectangle); };
            Controls.Add(_preview);

            Label pl = new Label();
            pl.Text = "实时预览";
            pl.SetBounds(902, 280, 120, 20);
            pl.ForeColor = Color.FromArgb(124, 111, 96);
            Controls.Add(pl);

            Label tip = new Label();
            tip.SetBounds(902, 302, 262, 140);
            tip.ForeColor = Color.FromArgb(124, 111, 96);
            tip.Text = "左侧切页，上方搜索框可直接过滤设置项。\r\n\r\n改毛色、配饰、大小、斑纹、异瞳时，\r\n这里会立刻跟着变。";
            Controls.Add(tip);

            _status = new Label();
            _status.SetBounds(12, 606, 860, 22);
            _status.ForeColor = Color.FromArgb(124, 111, 96);
            Controls.Add(_status);

            _previewTimer = new System.Windows.Forms.Timer();
            _previewTimer.Interval = 220;
            _previewTimer.Tick += delegate { if (_preview != null && !_preview.IsDisposed) _preview.Invalidate(); };
            _previewTimer.Start();

            FormClosed += delegate { if (_previewTimer != null) _previewTimer.Stop(); };

            RebuildNav();
        }

        private void RebuildNav()
        {
            string kw = _search.ForeColor == Color.Gray ? "" : _search.Text.Trim();
            int keep = _tabs.SelectedIndex;

            _navMap.Clear();
            _nav.BeginUpdate();
            _nav.Items.Clear();
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                string title = _tabs.TabPages[i].Text;
                if (kw.Length > 0
                    && title.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0
                    && !PageContains(i, kw)) continue;
                _navMap.Add(i);
                _nav.Items.Add(title);
            }
            _nav.EndUpdate();
            if (_navMap.Count == 0) return;

            int idx = _navMap.IndexOf(keep);
            _nav.SelectedIndex = idx >= 0 ? idx : 0;
        }

        /// <summary>搜索兜底：页内控件的文字含关键词也算命中。</summary>
        private bool PageContains(int pageIndex, string kw)
        {
            return ContainsText(_tabs.TabPages[pageIndex].Controls, kw);
        }

        private static bool ContainsText(Control.ControlCollection controls, string kw)
        {
            foreach (Control c in controls)
            {
                string t = c.Text;
                if (!string.IsNullOrEmpty(t) && t.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (c.Controls.Count > 0 && ContainsText(c.Controls, kw)) return true;
            }
            return false;
        }

        private void DrawNavItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(255, 252, 246) : Color.FromArgb(242, 235, 224)))
                e.Graphics.FillRectangle(b, e.Bounds);
            if (sel)
            {
                using (SolidBrush accent = new SolidBrush(Color.FromArgb(196, 86, 63)))
                    e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 7, 3, e.Bounds.Height - 14);
            }
            TextRenderer.DrawText(e.Graphics, (string)_nav.Items[e.Index], _nav.Font,
                new Rectangle(e.Bounds.X + 18, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height),
                Color.FromArgb(38, 34, 32),
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void SyncPageFromNav()
        {
            if (_nav.SelectedIndex < 0 || _nav.SelectedIndex >= _navMap.Count) return;
            int page = _navMap[_nav.SelectedIndex];
            if (_tabs.SelectedIndex != page) _tabs.SelectedIndex = page;
        }

        private void SyncNavFromPage()
        {
            int idx = _navMap.IndexOf(_tabs.SelectedIndex);
            if (idx >= 0 && _nav.SelectedIndex != idx) _nav.SelectedIndex = idx;
        }

        private void PaintPreview(Graphics g, Rectangle r)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(247, 242, 234)))
                g.FillRectangle(bg, r);
            using (Pen border = new Pen(Color.FromArgb(214, 202, 184)))
                g.DrawRectangle(border, 0, 0, r.Width - 1, r.Height - 1);

            int coat = _coatBox == null ? 0 : Math.Max(0, _coatBox.SelectedIndex);
            int acc = _accessoryBox == null ? 0 : Math.Max(0, _accessoryBox.SelectedIndex);
            bool spots = _showSpots == null || _showSpots.Checked;
            bool hetero = _hetero != null && _hetero.Checked;

            float scale = (r.Width - 8f) / 320f;
            System.Drawing.Drawing2D.GraphicsState st = g.Save();
            g.TranslateTransform(4f, (r.Height - 320f * scale) / 2f);
            g.ScaleTransform(scale, scale);

            CatAnim a = CatAnim.ForState("idle");
            a.Breathe = 0.55f;
            a.TailAngle = 5f;
            CatPainter.Draw(g, a, false, Coats.ById(coat), acc, spots, hetero);
            g.Restore(st);
        }

        /// <summary>左导航当前列出的页数（搜索过滤后），供自检读取。</summary>
        public int NavCount { get { return _nav == null ? 0 : _nav.Items.Count; } }

        public string[] NavTitles
        {
            get
            {
                if (_nav == null) return new string[0];
                string[] arr = new string[_nav.Items.Count];
                for (int i = 0; i < _nav.Items.Count; i++) arr[i] = _nav.Items[i].ToString();
                return arr;
            }
        }

        /// <summary>底部状态栏文案。</summary>
        public void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
        }

        private void BuildButtons()
        {
            Button ok = new Button();
            ok.Text = "确定";
            ok.SetBounds(992, 600, 84, 30);
            ok.Click += delegate { Collect(); Apply(); Close(); };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.SetBounds(1084, 600, 84, 30);
            cancel.Click += delegate { Close(); };
            Controls.Add(cancel);

            Button apply = new Button();
            apply.Text = "应用";
            apply.SetBounds(900, 600, 84, 30);
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
                w += (text[i] < 128) ? 8 : 17;
            l.SetBounds(x, y, Math.Max(20, w + 12), 20);
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

            _hotkeys.Checked = _work.Hotkeys;
            _quiet.Checked = _work.QuietEnabled;
            _quietStart.Value = Math.Max(0, Math.Min(23, _work.QuietStart));
            _quietEnd.Value = Math.Max(0, Math.Min(23, _work.QuietEnd));
            _portableLabel.Text = Settings.IsPortable
                ? "便携模式：exe 旁 data\\settings.json"
                : "漫游模式：%APPDATA%\\FlatCat\\settings.json";

            _captureHideCat.Checked = _work.CaptureHideCat;
            _captureCopy.Checked = _work.CaptureCopy;
            _captureDir.Text = string.IsNullOrEmpty(_work.CaptureDir) ? Shot.DefaultDir : _work.CaptureDir;

            _volumeBar.Value = Math.Max(0, Math.Min(100, _work.Volume));
            _volumeLabel.Text = _volumeBar.Value + "%";
            RefreshRadios();
            RefreshMusicList();

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
            _work.Hotkeys = _hotkeys.Checked;
            _work.QuietEnabled = _quiet.Checked;
            _work.QuietStart = (int)_quietStart.Value;
            _work.QuietEnd = (int)_quietEnd.Value;

            _work.CaptureHideCat = _captureHideCat.Checked;
            _work.CaptureCopy = _captureCopy.Checked;
            _work.CaptureDir = _captureDir.Text.Trim();
            _work.Volume = _volumeBar.Value;
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