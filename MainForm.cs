using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer monitorTimer;
        private System.Windows.Forms.Timer uptimeTimer;
        private DateTime screenOffTime;
        private DateTime startTime;
        private bool isScreenOff = false;
        private const string LogFilePath = "bugs/screencontrol.log";
        private const string Version = "1.0.0";
        private const string GiteeUrl = "https://gitee.com/yylmzxc/screen-control";
        private const string GithubUrl = "https://github.com/YYLMZXC/screen-control";
        private Label statusLabel; // 用于显示状态信息的标签

        public MainForm()
        {
            InitializeComponent();
            InitializeMonitorTimer();
            InitializeUptimeTimer();
            InitializeStatusLabel();
            startTime = DateTime.Now;
            LogOperation($"应用程序启动，启动时间：{startTime:yyyy-MM-dd HH:mm:ss}");
            UpdateStatus("应用程序已启动，就绪");
        }

        private void InitializeUptimeTimer()
        {
            uptimeTimer = new System.Windows.Forms.Timer();
            uptimeTimer.Interval = 1000; // 每秒更新一次界面显示
            uptimeTimer.Tick += UptimeTimer_Tick;
            uptimeTimer.Start();
        }

        private void InitializeMonitorTimer()
        {
            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 1000; // 1秒检查一次
            monitorTimer.Tick += MonitorTimer_Tick;
        }

        private void InitializeStatusLabel()
        {
            statusLabel = new Label();
            statusLabel.Text = "就绪";  
            statusLabel.Width = this.ClientSize.Width - 20;
            statusLabel.Left = 10;
            statusLabel.Top = this.ClientSize.Height - 30;
            statusLabel.AutoSize = false;
            statusLabel.Height = 20;
            statusLabel.BorderStyle = BorderStyle.FixedSingle;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Controls.Add(statusLabel);
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

        // 立即关闭屏幕
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 锁屏
        [DllImport("user32.dll")]
        private static extern void LockWorkStation();

        // 获取系统电源状态
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // 防止系统睡眠
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        // 电源管理常量
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private const uint WM_SYSCOMMAND = 0x0112;
        private const uint SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;
        private const int MONITOR_STANDBY = 1;

        // 检查系统是否被唤醒（屏幕是否开启）
        private bool IsSystemAwake()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            
            // 获取当前系统时间和最后一次输入时间的差值
            uint idleTime = (uint)Environment.TickCount - lastInputInfo.dwTime;
            
            // 如果空闲时间小于2秒，认为系统刚刚被唤醒
            return idleTime < 2000;
        }

        private void TurnOffScreen()
        {
            try
            {
                // 记录屏幕关闭时间
                screenOffTime = DateTime.Now;
                isScreenOff = true;
                
                // 发送消息关闭屏幕
                SendMessage(this.Handle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
                
                // 设置线程执行状态，防止系统睡眠但允许显示关闭
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
                
                // 启动监控计时器
                monitorTimer.Start();
                
                string message = $"屏幕已关闭，时间：{screenOffTime:yyyy-MM-dd HH:mm:ss}";
                LogOperation(message);
                UpdateStatus(message);
            }
            catch (Exception ex)
            {
                string errorMsg = $"关闭屏幕失败：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
            }
        }

        private void LockAndTurnOffScreen()
        {
            try
            {
                // 先锁屏
                LockWorkStation();
                LogOperation("系统已锁屏");

                // 等待一段时间，确保锁屏完成
                System.Threading.Thread.Sleep(1000);

                // 然后关闭屏幕
                TurnOffScreen();
            }
            catch (Exception ex)
            {
                string errorMsg = $"锁屏并关闭屏幕失败：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
            }
        }

        private void LogOperation(string operation)
        {
            try
            {
                // 确保日志目录存在
                string logDirectory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operation}";
                
                // 使用StreamWriter确保数据立即写入磁盘，防止系统崩溃时丢失数据
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine(logEntry);
                    writer.Flush(); // 强制将缓冲区内容写入磁盘
                }
            }
            catch (Exception ex)
            {
                // 日志记录失败不应影响主要功能
                Console.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        private void btnTurnOffScreen_Click(object sender, EventArgs e)
        {
            // 点击按钮后只关闭屏幕
            TurnOffScreen();
        }

        private void btnLockAndTurnOffScreen_Click(object sender, EventArgs e)
        {
            // 点击按钮后先锁屏再关闭屏幕
            LockAndTurnOffScreen();
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (isScreenOff && IsSystemAwake())
            {
                // 屏幕已被唤醒，计算关闭时长
                DateTime now = DateTime.Now;
                TimeSpan duration = now - screenOffTime;
                
                monitorTimer.Stop();
                isScreenOff = false;
                
                // 恢复正常电源状态
                SetThreadExecutionState(ES_CONTINUOUS);
                
                string message = $"屏幕已唤醒，关闭时长：{duration.TotalMinutes:F2}分钟（{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒）";
                LogOperation(message);
                UpdateStatus(message);
            }
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            try
            {
                // 从嵌入式资源加载背景图片
                using (Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("ScreenControl.res.screencontrol.png"))
                {
                    if (stream != null)
                    {
                        this.BackgroundImage = System.Drawing.Image.FromStream(stream);
                        this.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                    else
                    {
                        UpdateStatus("无法加载嵌入式背景图片资源");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载背景图片失败：{ex.Message}");
            }
        }

        private int logCounter = 0; // 用于控制日志记录频率

        private void UptimeTimer_Tick(object sender, EventArgs e)
        {
            // 计算运行时间
            TimeSpan uptime = DateTime.Now - startTime;
            string uptimeMessage = $"稳定运行时间：{uptime.Days}天{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            
            // 每秒更新界面显示
            UpdateStatus(uptimeMessage);
            
            // 每分钟记录一次日志（避免日志文件过大）
            logCounter++;
            if (logCounter >= 60)
            {
                string detailedMessage = $"应用程序运行时间：{uptime.TotalHours:F2}小时（{uptime.Days}天{uptime.Hours}小时{uptime.Minutes}分钟{uptime.Seconds}秒）";
                LogOperation(detailedMessage);
                logCounter = 0;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            monitorTimer.Stop();
            uptimeTimer.Stop();
            
            // 恢复正常电源状态
            if (isScreenOff)
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
            
            // 记录总运行时间
            TimeSpan totalUptime = DateTime.Now - startTime;
            string shutdownMessage = $"应用程序关闭，总运行时间：{totalUptime.TotalHours:F2}小时（{totalUptime.Days}天{totalUptime.Hours}小时{totalUptime.Minutes}分钟{totalUptime.Seconds}秒）";
            LogOperation(shutdownMessage);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 处理数字键1（关闭屏幕）
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                TurnOffScreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // 处理数字键2（锁屏并关闭屏幕）
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                LockAndTurnOffScreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // 处理H键（帮助）
            else if (e.KeyCode == Keys.H && e.Alt)
            {
                ShowHelp();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void ShowHelp()
        {
            // 创建帮助菜单
            ContextMenuStrip helpMenu = new ContextMenuStrip();
            
            // 添加菜单项
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("关于");
            aboutMenuItem.Click += AboutMenuItem_Click;
            
            // 将菜单项添加到菜单
            helpMenu.Items.Add(aboutMenuItem);
            
            // 显示菜单在按钮旁边
            helpMenu.Show(btnHelp, new System.Drawing.Point(0, btnHelp.Height));
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            // 创建关于对话框
            Form aboutForm = new Form();
            aboutForm.Text = "关于屏幕控制";
            aboutForm.Size = new System.Drawing.Size(400, 300);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false;
            aboutForm.MinimizeBox = false;
            
            // 创建标签显示版本信息和项目地址
            Label versionLabel = new Label();
            versionLabel.Location = new System.Drawing.Point(20, 20);
            versionLabel.Size = new System.Drawing.Size(350, 30);
            versionLabel.Text = $"版本号: {Version}";
            versionLabel.Font = new System.Drawing.Font(versionLabel.Font, System.Drawing.FontStyle.Bold);
            
            Label giteeLabel = new Label();
            giteeLabel.Location = new System.Drawing.Point(20, 60);
            giteeLabel.Size = new System.Drawing.Size(350, 30);
            giteeLabel.Text = $"Gitee 地址: {GiteeUrl}";
            giteeLabel.ForeColor = System.Drawing.Color.Blue;
            giteeLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            giteeLabel.Click += (s, ev) => System.Diagnostics.Process.Start(GiteeUrl);
            
            Label githubLabel = new Label();
            githubLabel.Location = new System.Drawing.Point(20, 100);
            githubLabel.Size = new System.Drawing.Size(350, 30);
            githubLabel.Text = $"GitHub 地址: {GithubUrl}";
            githubLabel.ForeColor = System.Drawing.Color.Blue;
            githubLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            githubLabel.Click += (s, ev) => System.Diagnostics.Process.Start(GithubUrl);
            
            Label descriptionLabel = new Label();
            descriptionLabel.Location = new System.Drawing.Point(20, 140);
            descriptionLabel.Size = new System.Drawing.Size(350, 80);
            descriptionLabel.Text = "屏幕控制是一款简单实用的工具，支持快速关闭屏幕和锁屏并关闭屏幕功能。\n\n快捷键：\n1 - 关闭屏幕\n2 - 锁屏并关闭屏幕\nAlt+H - 帮助菜单";
            descriptionLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            descriptionLabel.AutoSize = false;
            
            // 添加关闭按钮
            Button closeButton = new Button();
            closeButton.Location = new System.Drawing.Point(150, 220);
            closeButton.Size = new System.Drawing.Size(100, 30);
            closeButton.Text = "关闭";
            closeButton.Click += (s, ev) => aboutForm.Close();
            
            // 将控件添加到表单
            aboutForm.Controls.Add(versionLabel);
            aboutForm.Controls.Add(giteeLabel);
            aboutForm.Controls.Add(githubLabel);
            aboutForm.Controls.Add(descriptionLabel);
            aboutForm.Controls.Add(closeButton);
            
            // 显示对话框
            aboutForm.ShowDialog(this);
        }
    }
}