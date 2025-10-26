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

        public MainForm()
        {
            InitializeComponent();
            InitializeMonitorTimer();
            LogOperation("应用程序启动");
        }

        private void InitializeMonitorTimer()
        {
            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 1000; // 1秒检查一次
            monitorTimer.Tick += MonitorTimer_Tick;
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
                
                // 启动监控计时器
                monitorTimer.Start();
                
                string message = $"屏幕已关闭，时间：{screenOffTime:yyyy-MM-dd HH:mm:ss}";
                LogOperation(message);
                MessageBox.Show(message, "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string errorMsg = $"关闭屏幕失败：{ex.Message}";
                MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogOperation(errorMsg);
            }
        }

        private void LogOperation(string operation)
        {
            try
            {
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
                
                string message = $"屏幕已唤醒，关闭时长：{duration.TotalMinutes:F2}分钟（{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒）";
                LogOperation(message);
                MessageBox.Show(message, "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            // 加载时的初始化工作已在构造函数中完成
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            monitorTimer.Stop();
            LogOperation("应用程序关闭");
        }
    }
}