using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer monitorTimer;
        private DateTime screenOffTime;
        private bool isScreenOff = false;
        private const string LogFilePath = "bugs/screencontrol.log";
        private Label statusLabel; // 用于显示状态信息的标签

        public MainForm()
        {
            InitializeComponent();
            InitializeMonitorTimer();
            InitializeStatusLabel();
            LogOperation("应用程序启动");
            UpdateStatus("应用程序已启动，就绪");
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
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            monitorTimer.Stop();
            
            // 恢复正常电源状态
            if (isScreenOff)
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
            
            LogOperation("应用程序关闭");
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
        }
    }
}