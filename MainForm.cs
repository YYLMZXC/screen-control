using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;

namespace ScreenControl
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer monitorTimer;
        private System.Windows.Forms.Timer uptimeTimer;
        private DateTime screenOffTime;
        private DateTime startTime;
        private DateTime lastMouseMoveTime;
        private bool isScreenOff = false;
        private const string LogFilePath = "bugs/screencontrol.log";
        private const string SettingsFilePath = "settings.json";
        private const string Version = "1.5.1";
        private const string GiteeUrl = "https://gitee.com/yylmzxc/screen-control";
        private const string GithubUrl = "https://github.com/YYLMZXC/screen-control";
        private Label statusLabel; // 用于显示状态信息的标签
        private Label uptimeLabel; // 用于显示运行时间的标签
        private NotifyIcon notifyIcon; // 托盘图标
        private bool enableHotkeys = true; // 快捷键启用状态标志
        private ContextMenuStrip trayMenu; // 托盘右键菜单

        public MainForm()
        {
            InitializeComponent();
            InitializeMonitorTimer();
            InitializeUptimeTimer();
            InitializeStatusLabel();
            InitializeTrayIcon(); // 初始化托盘图标
            
            // 加载设置
            LoadSettings();
            
            // 添加复选框状态变化事件
            if (chkDelayWakeUp != null)
            {
                chkDelayWakeUp.CheckedChanged += ChkDelayWakeUp_CheckedChanged;
            }
            
            startTime = DateTime.Now;
            LogOperation($"应用程序启动，启动时间：{startTime:yyyy-MM-dd HH:mm:ss}");
            UpdateStatus("应用程序已启动，就绪");
            
            // 程序启动时自动检查更新（使用AutoUpdateManager类在后台线程中进行）
            AutoUpdateManager updateManager = new AutoUpdateManager(Version, UpdateStatus, LogOperation, this);
            updateManager.StartAutoCheck();
            
            // 注册全局热键（始终注册，但会根据enableHotkeys状态决定是否响应）
            RegisterGlobalHotkeys();
        }
        
        // 初始化托盘图标和菜单
        private void InitializeTrayIcon()
        {
            // 创建托盘图标
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "屏幕控制 v" + Version;
            
            // 尝试多种方式加载图标
            try
            {
                // 首先尝试使用文件系统中的图标文件
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screencontrol.ico");
                if (File.Exists(iconPath))
                {
                    notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    LogOperation("从文件系统加载托盘图标成功");
                }
                else
                {
                    // 尝试使用res文件夹中的图标
                    iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "screencontrol.ico");
                    if (File.Exists(iconPath))
                    {
                        notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                        LogOperation("从res文件夹加载托盘图标成功");
                    }
                    else
                    {
                        // 最后尝试从嵌入式资源加载
                        try
                        {
                            using (Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("ScreenControl.res.screencontrol.ico"))
                            {
                                if (stream != null)
                                {
                                    notifyIcon.Icon = new System.Drawing.Icon(stream);
                                    LogOperation("从嵌入式资源加载托盘图标成功");
                                }
                                else
                                {
                                    LogOperation("无法找到嵌入式图标资源");
                                    // 使用默认图标
                                    notifyIcon.Icon = SystemIcons.Application;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogOperation($"从嵌入式资源加载托盘图标失败：{ex.Message}");
                            // 使用默认图标
                            notifyIcon.Icon = SystemIcons.Application;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogOperation($"加载托盘图标失败：{ex.Message}");
                // 确保至少有一个默认图标
                notifyIcon.Icon = SystemIcons.Application;
            }
            
            // 创建托盘右键菜单
            trayMenu = new ContextMenuStrip();
            
            // 添加显示主窗口菜单项
            ToolStripMenuItem showMainFormItem = new ToolStripMenuItem("显示主窗口");
            showMainFormItem.Click += ShowMainFormItem_Click;
            trayMenu.Items.Add(showMainFormItem);
            
            // 添加关闭屏幕菜单项
            ToolStripMenuItem turnOffScreenItem = new ToolStripMenuItem("关闭屏幕");
            turnOffScreenItem.Click += (s, e) => TurnOffScreen();
            trayMenu.Items.Add(turnOffScreenItem);
            
            // 添加锁屏并关闭屏幕菜单项
            ToolStripMenuItem lockAndTurnOffScreenItem = new ToolStripMenuItem("锁屏并关闭屏幕");
            lockAndTurnOffScreenItem.Click += (s, e) => LockAndTurnOffScreen();
            trayMenu.Items.Add(lockAndTurnOffScreenItem);
            
            // 添加分隔线
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // 添加退出程序菜单项
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += ExitItem_Click;
            trayMenu.Items.Add(exitItem);
            
            // 设置托盘图标菜单
            notifyIcon.ContextMenuStrip = trayMenu;
            
            // 添加双击事件，用于恢复主窗口
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            // 设置当主窗口关闭时不自动清理托盘图标
            notifyIcon.Visible = false; // 初始时隐藏，当最小化时显示
        }
        
        // 显示主窗口菜单项点击事件
        private void ShowMainFormItem_Click(object sender, EventArgs e)
        {
            ShowMainForm();
        }
        
        // 退出程序菜单项点击事件
        private void ExitItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        // 托盘图标双击事件
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {            
            ShowMainForm();
        }
        
        // 显示主窗口
        private void ShowMainForm()
        {            
            try
            {                
                // 清除手动隐藏标志
                isWindowManuallyHidden = false;
                
                // 强制设置窗口状态为正常，确保不是最小化
                this.WindowState = FormWindowState.Normal;
                
                // 先确保任务栏图标可见，然后显示窗口
                this.ShowInTaskbar = true;
                this.Visible = true;
                
                // 强制激活窗口
                this.Activate();
                
                // 使用API设置为前台窗口
                SetForegroundWindow(this.Handle);
                
                // 确保窗口在最前面
                this.BringToFront();
                
                // 短暂设置为TopMost然后清除，确保窗口在最前面
                this.TopMost = true;
                this.Refresh();
                Application.DoEvents();
                this.TopMost = false;
                
                // 确保窗口获得焦点
                this.Focus();
                
                LogOperation("窗口已显示并前置");
            }
            catch (Exception ex)
            {                
                LogOperation($"恢复窗口显示时出错: {ex.Message}");
            }
        }
        
        // 切换窗口显示状态（用于菜单点击等）
        private void ToggleWindowVisibility()
        {            
            if (this.Visible && this.WindowState != FormWindowState.Minimized)
            {                
                // 如果窗口可见且未最小化，隐藏它但保持任务栏图标
                this.Visible = false;
                isWindowManuallyHidden = true;
                LogOperation("窗口已隐藏（通过菜单点击）");
            }
            else
            {                
                // 如果窗口不可见或最小化，显示它
                ShowMainForm();
            }
        }
        
        // 复选框状态变化事件处理
        private void ChkDelayWakeUp_CheckedChanged(object sender, EventArgs e)
        {
            SaveSettings();
            // 更新状态标签以显示复选框的当前状态
            string statusText = chkDelayWakeUp.Checked ? "延迟唤醒检测已启用" : "延迟唤醒检测已禁用";
            UpdateStatus(statusText);
            LogOperation(statusText);
        }
        
        // 保存设置
        private void SaveSettings()
        {
            try
            {
                // 创建设置对象
                var settings = new
                {
                    DelayWakeUpEnabled = chkDelayWakeUp?.Checked ?? true,
                    EnableHotkeys = enableHotkeys
                };
                
                // 序列化并保存到文件
                string settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
                File.WriteAllText(SettingsFilePath, settingsJson);
            }
            catch (Exception ex)
            {
                LogOperation($"保存设置失败：{ex.Message}");
            }
        }
        
        // 加载设置
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath) && chkDelayWakeUp != null)
                {
                    string settingsJson = File.ReadAllText(SettingsFilePath);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settingsJson);
                    
                    if (settings != null && settings.DelayWakeUpEnabled != null)
                    {
                        chkDelayWakeUp.Checked = settings.DelayWakeUpEnabled;
                    }
                    
                    // 加载快捷键启用状态，默认为启用
                    if (settings != null && settings.EnableHotkeys != null)
                    {
                        enableHotkeys = settings.EnableHotkeys;
                    }
                }
            }
            catch (Exception ex)
            {
                LogOperation($"加载设置失败：{ex.Message}");
                // 加载失败时使用默认值
                if (chkDelayWakeUp != null)
                {
                    chkDelayWakeUp.Checked = true;
                }
            }
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
            // 状态标签 - 用于显示程序状态信息
            statusLabel = new Label();
            statusLabel.Text = "就绪";  
            statusLabel.Width = this.ClientSize.Width - 20;
            statusLabel.Left = 10;
            statusLabel.Top = this.ClientSize.Height - 55;
            statusLabel.AutoSize = false;
            statusLabel.Height = 20;
            statusLabel.BorderStyle = BorderStyle.FixedSingle;
            statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Controls.Add(statusLabel);
            
            // 运行时间标签 - 专门用于显示运行时间
            uptimeLabel = new Label();
            uptimeLabel.Text = "运行时间: 00:00:00";
            uptimeLabel.Width = this.ClientSize.Width - 20;
            uptimeLabel.Left = 10;
            uptimeLabel.Top = this.ClientSize.Height - 30;
            uptimeLabel.AutoSize = false;
            uptimeLabel.Height = 20;
            uptimeLabel.BorderStyle = BorderStyle.FixedSingle;
            uptimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Controls.Add(uptimeLabel);
        }

        private void UpdateStatus(string message)
        {
            // 确保在UI线程中更新状态显示
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateStatus), message);
            }
            else
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = message;
                    statusLabel.Refresh();
                }
            }
        }

        // 立即关闭屏幕
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 锁屏
        [DllImport("user32.dll")]
        private static extern void LockWorkStation();

        // 设置窗口前台
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

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
            
            // 如果空闲时间小于2秒，认为系统有输入（鼠标/键盘）
            if (idleTime < 2000)
            {
                // 检查是否启用延迟唤醒检测
                if (chkDelayWakeUp != null && chkDelayWakeUp.Checked)
                {
                    // 如果是第一次检测到鼠标移动，记录时间
                    if (lastMouseMoveTime == DateTime.MinValue)
                    {
                        lastMouseMoveTime = DateTime.Now;
                        return false; // 不立即唤醒
                    }
                    
                    // 计算距离第一次检测到鼠标移动的时间
                    TimeSpan delayTime = DateTime.Now - lastMouseMoveTime;
                    
                    // 如果超过3秒才真正唤醒
                    if (delayTime.TotalMilliseconds >= 3000)
                    {
                        lastMouseMoveTime = DateTime.MinValue; // 重置计时
                        return true; // 唤醒屏幕
                    }
                    return false; // 延迟期间不唤醒
                }
                return true; // 未启用延迟，立即唤醒
            }
            
            // 没有输入活动，重置鼠标移动时间
            if (idleTime >= 2000)
            {
                lastMouseMoveTime = DateTime.MinValue;
            }
            
            return false;
        }

        private void TurnOffScreen()
        {
            try
            {
                // 记录屏幕关闭时间和状态
                screenOffTime = DateTime.Now;
                
                // 在UI线程中获取Handle，避免线程间操作异常
                IntPtr mainHandle = IntPtr.Zero;
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        mainHandle = this.Handle;
                    }));
                }
                else
                {
                    mainHandle = this.Handle;
                }
                
                // 使用单独的方法来执行可能会卡死的操作，并设置超时
                bool completed = ExecuteWithTimeout(() =>
                {
                    // 重置鼠标移动时间
                    lastMouseMoveTime = DateTime.MinValue;
                    
                    // 无论屏幕当前状态如何，都尝试关闭屏幕
                    // 使用SendMessage超时保护版本
                    SendMessageTimeout(mainHandle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
                    
                    // 设置线程执行状态，防止系统睡眠但允许显示关闭
                    SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
                }, 2000); // 2秒超时
                
                if (completed)
                {
                    // 记录屏幕关闭状态
                    isScreenOff = true;
                    
                    // 启动监控计时器
                    if (!monitorTimer.Enabled)
                    {
                        monitorTimer.Start();
                    }
                    
                    // 确保运行时间计时器继续运行
                    if (!uptimeTimer.Enabled)
                    {
                        uptimeTimer.Start();
                    }
                    
                    string message = $"屏幕关闭命令已发送，时间：{screenOffTime:yyyy-MM-dd HH:mm:ss}";
                    LogOperation(message);
                    UpdateStatus(message);
                }
                else
                {
                    // 重置状态标志，避免卡死状态
                    isScreenOff = false;
                    
                    string message = "屏幕关闭操作超时，可能与其他程序冲突";
                    LogOperation(message);
                    UpdateStatus(message);
                }
            }
            catch (Exception ex)
            {
                // 重置状态标志，避免卡死状态
                isScreenOff = false;
                
                string errorMsg = $"关闭屏幕时发生错误：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
            }
        }
        
        // 带有超时保护的SendMessage方法
        private void SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, int timeoutMs = 1000)
        {
            try
            {
                // 创建一个委托来执行SendMessage
                Action sendAction = () => SendMessage(hWnd, Msg, wParam, lParam);
                
                // 使用Task.Run和Task.Wait设置超时
                Task.Run(sendAction).Wait(timeoutMs);
            }
            catch (Exception ex)
            {
                LogOperation($"SendMessage操作超时或失败：{ex.Message}");
            }
        }
        
        // 通用的超时执行方法
        private bool ExecuteWithTimeout(Action action, int timeoutMs)
        {
            try
            {
                Task task = Task.Run(action);
                return task.Wait(timeoutMs);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void LockAndTurnOffScreen()
        {
            try
            {
                // 用于跟踪锁屏是否成功的标志
                bool[] lockSuccess = new bool[] { true }; // 使用数组引用传递
                
                // 使用超时保护执行锁屏操作
                bool completed = ExecuteWithTimeout(() =>
                {
                    try
                    {
                        // 先锁屏
                        LockWorkStation();
                        LogOperation("系统已锁屏");
                    }
                    catch (Exception ex)
                    {
                        LogOperation($"锁屏操作失败：{ex.Message}");
                        lockSuccess[0] = false;
                    }
                }, 3000); // 3秒超时
                
                if (completed && lockSuccess[0])
                {
                    // 使用单独的Task执行短暂延迟，避免UI线程阻塞
                    Task.Run(async () =>
                    {
                        // 短暂延迟后关闭屏幕
                        await Task.Delay(1000);
                        
                        // 调用关闭屏幕方法
                        this.Invoke((Action)(() =>
                        {
                            TurnOffScreen();
                        }));
                    });
                }
                else
                {
                    string message = "锁屏操作超时或失败，可能与其他程序冲突";
                    LogOperation(message);
                    UpdateStatus(message);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"锁屏并关闭屏幕时发生错误：{ex.Message}";
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
            // 快速检查，如果没有处于屏幕关闭状态，可以跳过详细检查
            if (!isScreenOff)
            {
                return;
            }
            
            try
            {
                // 使用超时保护的方式检查系统唤醒状态
                bool[] isSystemAwake = new bool[] { false }; // 使用数组引用传递
                bool checkCompleted = ExecuteWithTimeout(() =>
                {
                    isSystemAwake[0] = IsSystemAwake();
                }, 1000); // 1秒超时
                
                // 只有当检查成功完成且系统确实唤醒时才进行状态切换
                if (checkCompleted && isSystemAwake[0])
                {
                    // 使用超时保护执行状态重置操作
                    ExecuteWithTimeout(() =>
                    {
                        // 屏幕已被唤醒，计算关闭时长
                        DateTime now = DateTime.Now;
                        TimeSpan duration = now - screenOffTime;
                        
                        // 重置屏幕状态标志
                        isScreenOff = false;
                        
                        // 停止监控计时器
                        if (monitorTimer.Enabled)
                        {
                            monitorTimer.Stop();
                        }
                        
                        // 恢复正常电源状态
                        SetThreadExecutionState(ES_CONTINUOUS);
                        
                        // 确保运行时间计时器继续运行
                        if (!uptimeTimer.Enabled)
                        {
                            uptimeTimer.Start();
                        }
                        
                        string message = $"屏幕已唤醒，关闭时长：{duration.TotalMinutes:F2}分钟（{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒）";
                        UpdateStatus(message);
                        LogOperation(message);
                    }, 1500); // 1.5秒超时
                }
            }
            catch (Exception ex)
            {
                // 重置状态标志，避免卡死状态
                isScreenOff = false;
                
                string errorMsg = $"监控屏幕状态时出错：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
                
                // 在错误情况下，尝试停止监控计时器以防止持续出错
                try
                {
                    if (monitorTimer.Enabled)
                    {
                        monitorTimer.Stop();
                    }
                    // 重置线程执行状态
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
                catch { /* 忽略重置状态时可能出现的错误 */ }
            }
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            try
            {
                // 加载设置后更新快捷键开关状态
                if (chkGlobalHotkeys != null)
                {
                    chkGlobalHotkeys.Checked = enableHotkeys;
                }
                
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
            string uptimeMessage = $"运行时间：{uptime.Days}天{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            
            // 专门更新运行时间标签
            if (uptimeLabel != null)
            {
                uptimeLabel.Text = uptimeMessage;
            }
            
            // 每分钟记录一次日志（避免日志文件过大）
            logCounter++;
            if (logCounter >= 60)
            {
                string detailedMessage = $"应用程序运行时间：{uptime.TotalHours:F2}小时（{uptime.Days}天{uptime.Hours}小时{uptime.Minutes}分钟{uptime.Seconds}秒）";
                LogOperation(detailedMessage);
                logCounter = 0;
            }
        }

        // 处理窗口大小改变事件，实现最小化到托盘
        protected override void OnResize(EventArgs e)
        {            
            base.OnResize(e);            
            
            // 当窗口最小化时，隐藏窗口并显示托盘图标
            if (this.WindowState == FormWindowState.Minimized)
            {                
                // 设置最小化标志，不是手动隐藏
                isWindowManuallyHidden = false;
                
                // 隐藏窗口但保持任务栏图标可见（用于点击切换）
                this.Visible = false;
                // 确保任务栏图标保持可见
                this.ShowInTaskbar = true;
                
                // 确保托盘图标可见
                notifyIcon.Visible = true;
                LogOperation("程序已最小化到托盘");
                
                try
                {                    
                    // 显示提示气泡
                    notifyIcon.ShowBalloonTip(3000, "屏幕控制", "程序已最小化到托盘，双击托盘图标或点击任务栏图标恢复窗口", ToolTipIcon.Info);
                }
                catch (Exception ex)
                {                    
                    LogOperation($"显示托盘提示气泡失败：{ex.Message}");
                }
            }
        }
        
        // 添加一个标志来跟踪窗口是否被主动隐藏（非最小化状态）
        private bool isWindowManuallyHidden = false;
        
        // 窗口消息处理方法在全局热键实现中已包含，此处删除重复定义
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {            
            base.OnFormClosing(e);            
            monitorTimer.Stop();
            
            // 清理托盘图标
            if (notifyIcon != null)
            {                
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            uptimeTimer.Stop();
            
            // 注销全局热键
            UnregisterGlobalHotkeys();
            
            // 清理托盘图标
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            
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

        // 全局热键常量定义
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_TURNOFFSCREEN = 1;
        private const int HOTKEY_ID_LOCKSCREEN = 2;
        private const int HOTKEY_ID_HELP = 3;
        
        // 注册/注销全局热键的API声明
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        // 注册全局热键
        private void RegisterGlobalHotkeys()
        {
            try
            {
                // 注册数字键1（无修饰键）用于关闭屏幕
                RegisterHotKey(this.Handle, HOTKEY_ID_TURNOFFSCREEN, 0, (int)Keys.D1);
                RegisterHotKey(this.Handle, HOTKEY_ID_TURNOFFSCREEN, 0, (int)Keys.NumPad1);
                
                // 注册数字键2（无修饰键）用于锁屏并关闭屏幕
                RegisterHotKey(this.Handle, HOTKEY_ID_LOCKSCREEN, 0, (int)Keys.D2);
                RegisterHotKey(this.Handle, HOTKEY_ID_LOCKSCREEN, 0, (int)Keys.NumPad2);
                
                // 注册Alt+H用于打开帮助
                RegisterHotKey(this.Handle, HOTKEY_ID_HELP, (int)KeyModifier.Alt, (int)Keys.H);
                
                LogOperation("全局热键已注册");
            }
            catch (Exception ex)
            {
                LogOperation($"注册全局热键失败: {ex.Message}");
            }
        }
        
        // 注销全局热键
        private void UnregisterGlobalHotkeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_TURNOFFSCREEN);
                UnregisterHotKey(this.Handle, HOTKEY_ID_LOCKSCREEN);
                UnregisterHotKey(this.Handle, HOTKEY_ID_HELP);
                
                LogOperation("全局热键已注销");
            }
            catch (Exception ex)
            {
                LogOperation($"注销全局热键失败: {ex.Message}");
            }
        }
        
        // 热键修饰符枚举
        [Flags]
        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }
        
        // 窗口消息处理，用于捕获全局热键消息
        protected override void WndProc(ref Message m)
        {            
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_RESTORE = 0xF120;
            const int SC_MINIMIZE = 0xF020;
            const int SC_CLOSE = 0xF060;
            const int WM_ACTIVATEAPP = 0x001C;
            const int WM_ACTIVATE = 0x0006;
            
            // 处理全局热键消息
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                
                // 检查是否启用快捷键
                if (enableHotkeys)
                {
                    switch (id)
                    {
                        case HOTKEY_ID_TURNOFFSCREEN:
                            TurnOffScreen();
                            break;
                        case HOTKEY_ID_LOCKSCREEN:
                            LockAndTurnOffScreen();
                            break;
                        case HOTKEY_ID_HELP:
                            ShowHelp();
                            break;
                    }
                }
            }
            // 处理系统命令消息（包括任务栏点击）
            else if (m.Msg == WM_SYSCOMMAND)
            {                
                // 从消息的低16位提取命令
                int wparam = m.WParam.ToInt32();
                int cmd = wparam & 0xFFF0; // 清除低位的标志位
                
                // 处理最小化命令 - 只在窗口正常状态下处理
                if (cmd == SC_MINIMIZE && this.WindowState == FormWindowState.Normal)
                {                    
                    // 让系统正常处理最小化，我们会在OnResize中捕获
                    base.WndProc(ref m);
                    return;
                }
                // 处理恢复窗口命令（来自任务栏点击）
                else if (cmd == SC_RESTORE)
                {                    
                    // 当窗口不可见（无论是手动隐藏还是最小化隐藏）时，显示窗口
                    if (!this.Visible)
                    {                         
                        // 直接显示主窗口，不切换状态
                        ShowMainForm();
                        return; // 阻止默认处理
                    }
                }
            }
            
            // 处理应用程序激活消息 - 这通常来自任务栏点击
            else if (m.Msg == WM_ACTIVATEAPP)
            {                
                // 当通过任务栏点击激活应用程序，并且窗口当前是隐藏状态
                if (m.WParam.ToInt32() == 1 && !this.Visible)
                {                     
                    // 直接显示主窗口，不切换状态
                    ShowMainForm();
                }
            }
            else if (m.Msg == WM_ACTIVATE)
            {                
                // 当窗口接收到激活消息，确保窗口是可见的
                int wparam = m.WParam.ToInt32();
                // 如果是激活消息（wparam != 0）并且窗口当前被隐藏
                if (wparam != 0 && !this.Visible)
                {                     
                    ShowMainForm();
                }
            }
            
            base.WndProc(ref m);
        }
        
        // 窗口焦点时的快捷键处理（保留原始功能）
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {            
            // 检查是否启用快捷键
            if (!enableHotkeys)
                return;
                
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
            // 处理Alt+H快捷键（打开帮助）
            else if (e.Alt && e.KeyCode == Keys.H)
            {
                ShowHelp();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        
        private void chkGlobalHotkeys_CheckedChanged(object sender, EventArgs e)
        {
            // 更新快捷键启用状态
            enableHotkeys = chkGlobalHotkeys.Checked;
            
            // 记录日志
            string status = enableHotkeys ? "启用" : "禁用";
            string message = $"全局快捷键监听已{status}";
            LogOperation(message);
            UpdateStatus(message);
            
            // 保存设置
            SaveSettings();
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void ShowHelp()
        {            // 创建帮助菜单
            ContextMenuStrip helpMenu = new ContextMenuStrip();
            
            // 添加关于菜单项（使用&标记设置快捷键为A）
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("关于(&A)");
            aboutMenuItem.Click += AboutMenuItem_Click;
            
            // 添加检查更新菜单项（使用&标记设置快捷键为U）
            ToolStripMenuItem checkUpdateMenuItem = new ToolStripMenuItem("检查更新(&U)");
            checkUpdateMenuItem.Click += CheckUpdateMenuItem_Click;
            
            // 将菜单项添加到菜单
            helpMenu.Items.Add(aboutMenuItem);
            helpMenu.Items.Add(checkUpdateMenuItem);
            
            // 显示菜单在按钮旁边
            helpMenu.Show(btnHelp, new System.Drawing.Point(0, btnHelp.Height));
        }
        
        // 检查更新菜单项点击事件处理
        private async void CheckUpdateMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateStatus("正在检查更新...");
                LogOperation("开始检查更新");
                
                // 创建更新检查器实例
                UpdateChecker updateChecker = new UpdateChecker();
                
                // 在后台线程中检查更新，避免阻塞UI
                UpdateChecker.UpdateInfo updateInfo = await Task.Run(() => updateChecker.CheckForUpdatesAsync(Version));
                
                if (string.IsNullOrEmpty(updateInfo.LatestVersion))
                {
                    UpdateStatus("无法获取最新版本信息");
                    LogOperation("检查更新：无法获取最新版本信息");
                    MessageBox.Show("无法获取最新版本信息，请稍后再试。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (updateInfo.HasUpdate)
                {
                    UpdateStatus($"发现新版本: {updateInfo.LatestVersion}");
                    LogOperation($"检查更新：发现新版本 {updateInfo.LatestVersion}");
                    
                    // 使用统一的更新对话框
                    UpdateDownloader.ShowUpdateDialog(updateInfo, Version, UpdateStatus, LogOperation, this);

                }
                else
                {
                    UpdateStatus("您使用的是最新版本");
                    LogOperation("检查更新：当前已是最新版本");
                    MessageBox.Show($"您当前使用的版本 {Version} 已是最新版本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("检查更新失败：" + ex.Message);
                LogOperation("检查更新失败：" + ex.Message);
                MessageBox.Show("检查更新失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            giteeLabel.Click += (s, ev) => {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(GiteeUrl);
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
            };
            
            Label githubLabel = new Label();
            githubLabel.Location = new System.Drawing.Point(20, 100);
            githubLabel.Size = new System.Drawing.Size(350, 30);
            githubLabel.Text = $"GitHub 地址: {GithubUrl}";
            githubLabel.ForeColor = System.Drawing.Color.Blue;
            githubLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            githubLabel.Click += (s, ev) => {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(GithubUrl);
                psi.UseShellExecute = true;
                System.Diagnostics.Process.Start(psi);
            };
            
            Label descriptionLabel = new Label();
            descriptionLabel.Location = new System.Drawing.Point(20, 140);
            descriptionLabel.Size = new System.Drawing.Size(350, 80);
            descriptionLabel.Text = "屏幕控制是一款简单实用的工具，支持快速关闭屏幕和锁屏并关闭屏幕功能。\n\n快捷键：alt+1,alt+2,alt+h,alt+a\n1 - 关闭屏幕\n2 - 锁屏并关闭屏幕\nAlt+H - 帮助菜单";
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