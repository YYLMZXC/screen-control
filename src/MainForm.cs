using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

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
        private const string Version = "1.8.0";
        private const string GiteeUrl = "https://gitee.com/yylmzxc/screen-control";
        private const string GithubUrl = "https://github.com/YYLMZXC/screen-control";

        private Label statusLabel; // 用于显示状态信息的标签
        private Label uptimeLabel; // 用于显示运行时间的标签
        private NotifyIcon notifyIcon; // 托盘图标
        private bool enableHotkeys = true; // 快捷键启用状态标志
        private int closeScreenDelay = 2; // 延迟关闭屏幕的秒数，默认x秒

        // 自适应布局基准（与设计器中的初始 ClientSize 一致）
        private const int LayoutBaseWidth = 600;
        private const int LayoutBaseHeight = 360;
        private const float LayoutMinScale = 0.5f;   // 窗口缩小时最小缩放比
        private const float LayoutMaxScale = 1.6f;   // 窗口放大时最大缩放比
        private float baseButtonFontSize;  // 功能按钮原始字体大小（用于按比例缩放）
        private float baseHelpFontSize;    // 帮助按钮原始字体大小

        // 布局性能诊断：字体缓存（复用避免 GDI 句柄泄漏导致卡顿）
        private Font cachedFuncButtonFont;
        private Font cachedHelpFont;
        private long fontCreateCount;      // 字体重建次数（诊断用）
        private DateTime lastLayoutLogTime;  // 布局日志节流
        private DateTime lastResizeLogTime;  // 尺寸变化日志节流
        
        // 启动系统屏保快捷键设置
        private int turnOffScreenKey = (int)Keys.D1;
        private KeyModifier turnOffScreenModifier = KeyModifier.Alt;

        // DPMS 休眠快捷键设置
        private int dpmsKey = (int)Keys.D2;
        private KeyModifier dpmsModifier = KeyModifier.Alt;

        // 亮度调节快捷键设置
        private int brightnessKey = (int)Keys.D3;
        private KeyModifier brightnessModifier = KeyModifier.Alt;

        // 帮助菜单快捷键设置
        private int helpKey = (int)Keys.H;
        private KeyModifier helpModifier = KeyModifier.Alt;

        private ContextMenuStrip trayMenu; // 托盘右键菜单

        public MainForm()
        {
            InitializeComponent();

            // 设计器打开窗体时跳过运行时初始化：
            // OOP 设计器（DesignToolsServer 进程）会实例化构造函数，若执行
            // 写日志/读设置/托盘图标/网络检查/全局热键等操作，会导致设计器
            // 服务崩溃，表现为 IUIService 等服务缺失。
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            // 开启双缓冲，减少窗口拉伸/缩放时的闪烁
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            InitializeMonitorTimer();
            InitializeUptimeTimer();
            InitializeStatusLabel();
            InitializeTrayIcon(); // 初始化托盘图标
            
            // 加载设置
            LoadSettings();
                        
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
            
            // 直接从嵌入式资源加载图标，不再从文件系统加载
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
            
            // 添加启动系统屏保菜单项
            ToolStripMenuItem screensaverItem = new ToolStripMenuItem("启动系统屏保");
            screensaverItem.Click += (s, e) => TurnOffScreen();
            trayMenu.Items.Add(screensaverItem);
            
            // 添加DPMS休眠菜单项
            ToolStripMenuItem dpmsItem = new ToolStripMenuItem("DPMS 休眠");
            dpmsItem.Click += (s, e) => DpmsSleep();
            trayMenu.Items.Add(dpmsItem);
            
            // 添加亮度调节菜单项
            ToolStripMenuItem brightnessItem = new ToolStripMenuItem("亮度调节");
            brightnessItem.Click += (s, e) => ShowBrightnessDialog();
            trayMenu.Items.Add(brightnessItem);
            
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
              
        // 保存设置
        private void SaveSettings()
        {
            try
            {
                // 创建设置对象，包含所有可配置参数
                var settings = new
                {
                    EnableHotkeys = enableHotkeys,
                    CloseScreenDelay = closeScreenDelay,
                    TurnOffScreenKey = turnOffScreenKey,
                    TurnOffScreenModifier = (int)turnOffScreenModifier,
                    DpmsKey = dpmsKey,
                    DpmsModifier = (int)dpmsModifier,
                    BrightnessKey = brightnessKey,
                    BrightnessModifier = (int)brightnessModifier,
                    HelpKey = helpKey,
                    HelpModifier = (int)helpModifier
                };
                
                // 序列化并保存到文件
                string settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
                File.WriteAllText(SettingsFilePath, settingsJson);
                
                LogOperation($"设置已保存：快捷键={enableHotkeys}，延迟关闭屏幕={closeScreenDelay}秒");
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
                if (File.Exists(SettingsFilePath))
                {
                    string settingsJson = File.ReadAllText(SettingsFilePath);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settingsJson);
                    
                    if (settings != null)
                    {
                        // 加载快捷键启用状态
                        if (settings.EnableHotkeys != null)
                        {
                            enableHotkeys = settings.EnableHotkeys;
                        }
                        
                        // 加载延迟关闭屏幕的时间
                        if (settings.CloseScreenDelay != null)
                        {
                            closeScreenDelay = settings.CloseScreenDelay;
                        }
                        
                        // 加载启动系统屏保快捷键
                        if (settings.TurnOffScreenKey != null)
                        {
                            turnOffScreenKey = settings.TurnOffScreenKey;
                        }
                        
                        if (settings.TurnOffScreenModifier != null)
                        {
                            turnOffScreenModifier = (KeyModifier)settings.TurnOffScreenModifier;
                        }
                        
                        // 加载DPMS休眠快捷键
                        if (settings.DpmsKey != null)
                        {
                            dpmsKey = settings.DpmsKey;
                        }
                        
                        if (settings.DpmsModifier != null)
                        {
                            dpmsModifier = (KeyModifier)settings.DpmsModifier;
                        }
                        
                        // 加载亮度调节快捷键
                        if (settings.BrightnessKey != null)
                        {
                            brightnessKey = settings.BrightnessKey;
                        }
                        
                        if (settings.BrightnessModifier != null)
                        {
                            brightnessModifier = (KeyModifier)settings.BrightnessModifier;
                        }
                        
                        // 加载帮助菜单快捷键
                        if (settings.HelpKey != null)
                        {
                            helpKey = settings.HelpKey;
                        }
                        
                        if (settings.HelpModifier != null)
                        {
                            helpModifier = (KeyModifier)settings.HelpModifier;
                        }
                    }
                    
                    LogOperation($"设置已加载：快捷键={enableHotkeys}，延迟关闭屏幕={closeScreenDelay}秒");
                }
            }
            catch (Exception ex)
            {
                LogOperation($"加载设置失败：{ex.Message}");

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
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

      

        // 设置窗口前台
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // 获取桌面窗口句柄
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        

        

      
              
        // 检查系统是否被唤醒（屏幕是否开启）
        private bool IsSystemAwake()
        {            
            // 移除唤醒检测机制，只保留记录功能
            // 始终返回false，让系统本身处理唤醒
            return false;
        }

        private void TurnOffScreen()
        {
            try
            {
                if (closeScreenDelay > 0)
                {
                    // 延迟关闭屏幕
                    UpdateStatus($"将在{closeScreenDelay}秒后关闭屏幕...");
                    LogOperation($"开始延迟{closeScreenDelay}秒关闭屏幕");
                    
                    // 使用定时器实现延迟
                    System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer();
                    delayTimer.Interval = closeScreenDelay * 1000; // 转换为毫秒
                    delayTimer.Tick += (s, e) =>
                    {
                        delayTimer.Stop();
                        delayTimer.Dispose();
                        
                        // 执行实际的屏幕关闭操作
                        PerformScreenTurnOff();
                    };
                    delayTimer.Start();
                }
                else
                {
                    // 立即关闭屏幕
                    PerformScreenTurnOff();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"关闭屏幕失败：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
            }
        }
        
        // SystemParametersInfo API 声明
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        // SystemParametersInfo 常量
        private const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
        private const uint SPIF_SENDCHANGE = 0x0002;

        // 执行实际的屏幕关闭操作
        private void PerformScreenTurnOff()
        {
            try
            {
                // 记录屏幕关闭时间
                screenOffTime = DateTime.Now;
                lastMouseMoveTime = DateTime.MinValue;
                isScreenOff = true;
                
              
                
                // 获取系统目录路径
                string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string system32Dir = Path.Combine(windowsDir, "System32");
                string scrnsavePath = Path.Combine(system32Dir, "scrnsave.scr");
                
                bool screenSaverStarted = false;
                
                // 检查scrnsave.scr是否存在
                if (File.Exists(scrnsavePath))
                {
                    try
                    {
                        // 启动屏幕保护程序，/s 参数表示立即启动
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                        psi.FileName = scrnsavePath;
                        psi.Arguments = "/s";
                        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        System.Diagnostics.Process.Start(psi);
                        LogOperation($"已成功启动屏幕保护程序: {scrnsavePath}");
                        screenSaverStarted = true;
                    }
                    catch (Exception ex)
                    {
                        LogOperation($"启动屏幕保护程序失败: {ex.Message}");
                    }
                }
                else
                {
                    LogOperation($"未找到屏幕保护程序: {scrnsavePath}");
                }
              
                
                // 启动监控计时器
                monitorTimer.Start();
                
                string message = $"屏幕已关闭{(screenSaverStarted ? "（使用屏幕保护程序）" : "（使用系统API）")}，时间：{screenOffTime:yyyy-MM-dd HH:mm:ss}，后台程序保持运行";
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

        private readonly object logWriteLock = new object(); // 日志写盘互斥，保证并发写入不交错

        private void LogOperation(string operation)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operation}";

                // 异步写盘：磁盘慢（机械盘/杀毒扫描）时避免阻塞 UI 线程导致卡顿
                Task.Run(() => WriteLogEntry(logEntry));
            }
            catch
            {
                // 日志记录失败不应影响主要功能
            }
        }

        private void WriteLogEntry(string logEntry)
        {
            try
            {
                lock (logWriteLock)
                {
                    // 确保日志目录存在
                    string logDirectory = Path.GetDirectoryName(LogFilePath);
                    if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine(logEntry);
                        writer.Flush(); // 强制将缓冲区内容写入磁盘，防止系统崩溃时丢失数据
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        // ---------- DPMS 休眠 ----------

        // 广播消息相关常量
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITORPOWER_OFF = 2; // 1=低功耗, 2=关闭

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// DPMS 休眠：向所有顶层窗口广播 SC_MONITORPOWER 消息，让显示器进入关闭状态（DPMS Power Off）。
        /// </summary>
        private void DpmsSleep()
        {
            try
            {
                // HWND_BROADCAST = 0xFFFF，广播给所有窗口
                bool posted = PostMessage(
                    (IntPtr)0xFFFF, WM_SYSCOMMAND,
                    (IntPtr)SC_MONITORPOWER, (IntPtr)MONITORPOWER_OFF);

                if (posted)
                {
                    string message = $"已发送 DPMS 休眠指令，时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    LogOperation(message);
                    UpdateStatus(message);
                }
                else
                {
                    string errorMsg = "DPMS 休眠指令发送失败";
                    UpdateStatus(errorMsg);
                    LogOperation(errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"DPMS 休眠失败：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
            }
        }

        // ---------- 亮度调节 ----------

        private void ShowBrightnessDialog()
        {
            try
            {
                using (BrightnessForm brightnessForm = new BrightnessForm())
                {
                    brightnessForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"亮度调节失败：{ex.Message}";
                UpdateStatus(errorMsg);
                LogOperation(errorMsg);
                MessageBox.Show(errorMsg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnScreensaver_Click(object sender, EventArgs e)
        {
            // 启动系统屏保
            TurnOffScreen();
        }

        private void btnDpmsSleep_Click(object sender, EventArgs e)
        {
            DpmsSleep();
        }

        private void btnBrightness_Click(object sender, EventArgs e)
        {
            ShowBrightnessDialog();
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
                
            
                logCounter = 0;
                
                string message = $"屏幕已唤醒，关闭时长：{duration.TotalMinutes:F2}分钟（{duration.Hours}小时{duration.Minutes}分钟{duration.Seconds}秒）";
                LogOperation(message);
                UpdateStatus(message);
            }
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            try
            {
                // AutoScale 已应用，记录按钮当前字体大小作为缩放基准
                baseButtonFontSize = btnScreensaver.Font.Size;
                baseHelpFontSize = btnHelp.Font.Size;

                
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

            // 窗口首次显示时应用自适应布局
            LayoutControls();
        }

        private int logCounter = 0; // 用于控制日志记录频率

        private void UptimeTimer_Tick(object sender, EventArgs e)
        {
            // 计算运行时间 - 完全基于程序启动时间，不受任何其他操作影响
            TimeSpan uptime = DateTime.Now - startTime;
            string uptimeMessage = $"运行时间：{uptime.Days}天{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";
            
            // 专门更新运行时间标签
            if (uptimeLabel != null)
            {
                uptimeLabel.Text = uptimeMessage;
            }
            
            // 每30秒记录一次日志（避免日志文件过大）
            logCounter++;
            if (logCounter >= 30)
            {
                string detailedMessage = $"应用程序运行时间：{uptime.TotalHours:F2}小时（{uptime.Days}天{uptime.Hours}小时{uptime.Minutes}分钟{uptime.Seconds}秒）";
                LogOperation(detailedMessage);
                logCounter = 0;
            }
        }

        // 根据窗口大小自适应布局：功能按钮等比缩放并居中，帮助按钮贴右上角，状态栏贴底部拉伸
        private void LayoutControls()
        {
            // 最小化时 ClientSize 为 0，直接跳过
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            // InitializeComponent 期间设置 ClientSize 会提前触发 OnResize，
            // 此时部分按钮可能尚未创建，统一判空保护
            if (btnScreensaver == null || btnDpmsSleep == null ||
                btnBrightness == null || btnHelp == null)
                return;

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                SuspendLayout(); // 抑制布局风暴，减少闪烁
                try
                {
                    float scaleX = (float)ClientSize.Width / LayoutBaseWidth;
                    float scaleY = (float)ClientSize.Height / LayoutBaseHeight;
                    float scale = Math.Min(scaleX, scaleY);
                    scale = Math.Max(LayoutMinScale, Math.Min(LayoutMaxScale, scale));

                    // 缩放居中偏移
                    int offsetX = (int)((ClientSize.Width - LayoutBaseWidth * scale) / 2f);
                    int offsetY = (int)((ClientSize.Height - LayoutBaseHeight * scale) / 2f);

                    // 三个功能按钮按设计位置缩放并居中（字体复用缓存，避免 GDI 泄漏）
                    LayoutButton(btnScreensaver, 95, 100, 195, 55, scale, offsetX, offsetY, baseButtonFontSize, ref cachedFuncButtonFont);
                    LayoutButton(btnDpmsSleep, 310, 100, 195, 55, scale, offsetX, offsetY, baseButtonFontSize, ref cachedFuncButtonFont);
                    LayoutButton(btnBrightness, 95, 180, 195, 55, scale, offsetX, offsetY, baseButtonFontSize, ref cachedFuncButtonFont);

                    // 帮助按钮贴右上角
                    btnHelp.Left = ClientSize.Width - (int)(70 * scale); // 距右边缘 20*scale
                    btnHelp.Top = (int)(10 * scale);
                    btnHelp.Width = (int)(50 * scale);
                    btnHelp.Height = (int)(30 * scale);
                    float helpBaseSize = baseHelpFontSize > 0 ? baseHelpFontSize : btnHelp.Font.Size;
                    UpdateButtonFont(btnHelp, helpBaseSize, scale, ref cachedHelpFont);

                    // 状态与运行时间标签贴底部、宽度随窗口拉伸
                    if (statusLabel != null)
                    {
                        statusLabel.Left = 10;
                        statusLabel.Top = ClientSize.Height - 55;
                        statusLabel.Width = ClientSize.Width - 20;
                    }
                    if (uptimeLabel != null)
                    {
                        uptimeLabel.Left = 10;
                        uptimeLabel.Top = ClientSize.Height - 30;
                        uptimeLabel.Width = ClientSize.Width - 20;
                    }
                }
                finally
                {
                    ResumeLayout(true); // 一次性重绘，减少闪烁
                }
            }
            finally
            {
                sw.Stop();

                // 诊断日志：布局超过 5ms 说明有性能问题，立即记录；
                // 正常布局节流到 10 秒记录一次，避免高频 resize 刷爆日志
                long elapsedMs = sw.ElapsedMilliseconds;
                bool tooSlow = elapsedMs >= 5;
                bool throttleOk = (DateTime.Now - lastLayoutLogTime).TotalSeconds >= 10;
                if (tooSlow || throttleOk)
                {
                    lastLayoutLogTime = DateTime.Now;
                    LogOperation($"布局诊断：ClientSize={ClientSize.Width}x{ClientSize.Height}，" +
                        $"缩放比={Math.Max(LayoutMinScale, Math.Min(LayoutMaxScale, Math.Min((float)ClientSize.Width / LayoutBaseWidth, (float)ClientSize.Height / LayoutBaseHeight))):F2}，" +
                        $"耗时={elapsedMs}ms，字体重建={fontCreateCount}次" +
                        (tooSlow ? "【布局过慢，可能造成卡顿】" : ""));
                }
            }
        }

        // 按设计坐标与缩放比例重设按钮位置、大小和字体
        private void LayoutButton(Button btn, int designX, int designY, int designW, int designH,
            float scale, int offsetX, int offsetY, float baseFontSize, ref Font fontCache)
        {
            btn.Left = offsetX + (int)(designX * scale);
            btn.Top = offsetY + (int)(designY * scale);
            btn.Width = (int)(designW * scale);
            btn.Height = (int)(designH * scale);
            // 基准未记录时（InitializeComponent 期间 OnResize 会提前触发）回退到按钮当前字体
            float baseSize = baseFontSize > 0 ? baseFontSize : btn.Font.Size;
            UpdateButtonFont(btn, baseSize, scale, ref fontCache);
        }

        // 复用字体对象：缩放比未变化时直接沿用，避免每次 resize 都 new Font 造成 GDI 句柄泄漏和卡顿
        private void UpdateButtonFont(Button btn, float baseSize, float scale, ref Font cache)
        {
            float newSize = baseSize * scale;
            if (cache != null && Math.Abs(cache.Size - newSize) < 0.01f)
                return; // 字号未变化，复用缓存

            FontFamily family = btn.Font.FontFamily; // 替换前保存，避免 Dispose 后访问已释放对象
            Font newFont = new Font(family, newSize);
            fontCreateCount++;

            if (cache != null)
            {
                cache.Dispose();
            }
            else if (!btn.Font.IsSystemFont)
            {
                // 首次替换时释放设计器创建的非系统字体（如 btnHelp 的字体），避免泄漏
                btn.Font.Dispose();
            }

            cache = newFont;
            btn.Font = newFont;
        }

        // 处理窗口大小改变事件，实现最小化到托盘
        protected override void OnResize(EventArgs e)
        {            
            base.OnResize(e);            
            
            // 窗口大小变化时自适应布局（最小化时内部有保护）
            LayoutControls();

            // 尺寸变化日志（节流 2 秒，避免拖拽窗口时高频刷日志）
            if (this.WindowState != FormWindowState.Minimized &&
                (DateTime.Now - lastResizeLogTime).TotalMilliseconds >= 2000)
            {
                lastResizeLogTime = DateTime.Now;
                LogOperation($"窗口尺寸变化：ClientSize={ClientSize.Width}x{ClientSize.Height}，WindowState={this.WindowState}");
            }

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
            
          
            
            // 记录总运行时间
            TimeSpan totalUptime = DateTime.Now - startTime;
            string shutdownMessage = $"应用程序关闭，总运行时间：{totalUptime.TotalHours:F2}小时（{totalUptime.Days}天{totalUptime.Hours}小时{totalUptime.Minutes}分钟{totalUptime.Seconds}秒）";
            LogOperation(shutdownMessage);
        }

        // 全局热键常量定义
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_TURNOFFSCREEN = 1; // 启动系统屏保（可配置）
        private const int HOTKEY_ID_DPMS = 2;          // DPMS 休眠
        private const int HOTKEY_ID_HELP = 3;          // 帮助菜单
        private const int HOTKEY_ID_BRIGHTNESS = 4;    // 亮度调节
        private const int HOTKEY_ID_NUMPAD_OFFSET = 100; // 小键盘热键 id 偏移（需与主键盘区分）
        
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
                // 注销现有热键，避免冲突
                UnregisterGlobalHotkeys();
                
                // 注册四个可自定义功能的快捷键
                RegisterHotkeyWithNumpad(HOTKEY_ID_TURNOFFSCREEN, turnOffScreenKey, turnOffScreenModifier);
                RegisterHotkeyWithNumpad(HOTKEY_ID_DPMS, dpmsKey, dpmsModifier);
                RegisterHotkeyWithNumpad(HOTKEY_ID_BRIGHTNESS, brightnessKey, brightnessModifier);
                RegisterHotkeyWithNumpad(HOTKEY_ID_HELP, helpKey, helpModifier);
                
                LogOperation("全局热键已注册");
            }
            catch (Exception ex)
            {
                LogOperation($"注册全局热键失败: {ex.Message}");
            }
        }
        
        // 注册单个热键；若按键是主键盘数字键，同时注册小键盘对应键
        private void RegisterHotkeyWithNumpad(int id, int key, KeyModifier modifier)
        {
            // 校验：无修饰键且不是功能键时跳过，避免裸键在任意程序误触发
            if (key <= 0 ||
                (modifier == KeyModifier.None && !(key >= (int)Keys.F1 && key <= (int)Keys.F24)))
            {
                LogOperation($"快捷键 {FormatKeyName(key)} 缺少修饰键，已跳过注册");
                return;
            }

            if (!RegisterHotKey(this.Handle, id, (int)modifier, key))
            {
                LogOperation($"注册热键失败: {FormatKeyName(key)}（可能已被其他程序占用）");
            }

            if (key >= (int)Keys.D0 && key <= (int)Keys.D9)
            {
                int numPadKey = key - (int)Keys.D0 + (int)Keys.NumPad0;
                RegisterHotKey(this.Handle, id + HOTKEY_ID_NUMPAD_OFFSET, (int)modifier, numPadKey);
            }
        }

        // 按键名显示，主键盘数字键显示为 1-9 而非 D1-D9
        private static string FormatKeyName(int key)
        {
            if (key >= (int)Keys.D0 && key <= (int)Keys.D9)
                return ((char)('0' + key - (int)Keys.D0)).ToString();
            if (key >= (int)Keys.NumPad0 && key <= (int)Keys.NumPad9)
                return "小键盘" + (key - (int)Keys.NumPad0);
            return ((Keys)key).ToString();
        }
        
        // 注销全局热键
        private void UnregisterGlobalHotkeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_TURNOFFSCREEN);
                UnregisterHotKey(this.Handle, HOTKEY_ID_DPMS);
                UnregisterHotKey(this.Handle, HOTKEY_ID_HELP);
                UnregisterHotKey(this.Handle, HOTKEY_ID_BRIGHTNESS);
                // 注销小键盘对应的热键
                UnregisterHotKey(this.Handle, HOTKEY_ID_TURNOFFSCREEN + HOTKEY_ID_NUMPAD_OFFSET);
                UnregisterHotKey(this.Handle, HOTKEY_ID_DPMS + HOTKEY_ID_NUMPAD_OFFSET);
                UnregisterHotKey(this.Handle, HOTKEY_ID_HELP + HOTKEY_ID_NUMPAD_OFFSET);
                UnregisterHotKey(this.Handle, HOTKEY_ID_BRIGHTNESS + HOTKEY_ID_NUMPAD_OFFSET);
                
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
                // 小键盘热键的 id 带偏移，映射回功能 id
                int id = m.WParam.ToInt32();
                if (id >= HOTKEY_ID_NUMPAD_OFFSET)
                {
                    id -= HOTKEY_ID_NUMPAD_OFFSET;
                }
                
                // 检查是否启用快捷键
                if (enableHotkeys)
                {
                    switch (id)
                    {
                        case HOTKEY_ID_TURNOFFSCREEN:
                            TurnOffScreen();
                            break;
                        case HOTKEY_ID_DPMS:
                            DpmsSleep();
                            break;
                        case HOTKEY_ID_BRIGHTNESS:
                            ShowBrightnessDialog();
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
        
        // 窗口焦点时的快捷键处理（保留原始功能，按键与修饰键跟随全局设置）
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {            
            // 检查是否启用快捷键
            if (!enableHotkeys)
                return;
                
            int code = (int)e.KeyCode;

            // 处理启动系统屏保
            if (IsHotkeyMatch(code, e, turnOffScreenKey, turnOffScreenModifier))
            {
                TurnOffScreen();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // 处理DPMS休眠
            else if (IsHotkeyMatch(code, e, dpmsKey, dpmsModifier))
            {
                DpmsSleep();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // 处理亮度调节
            else if (IsHotkeyMatch(code, e, brightnessKey, brightnessModifier))
            {
                ShowBrightnessDialog();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // 处理帮助菜单
            else if (IsHotkeyMatch(code, e, helpKey, helpModifier))
            {
                ShowHelp();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // 判断按键是否与配置的热键匹配（含修饰键与小键盘等价键）
        private static bool IsHotkeyMatch(int code, KeyEventArgs e, int mainKey, KeyModifier modifier)
        {
            bool keyMatch = code == mainKey || IsNumpadEquivalent(code, mainKey);
            if (!keyMatch) return false;

            return e.Alt == ((modifier & KeyModifier.Alt) != 0) &&
                   e.Control == ((modifier & KeyModifier.Control) != 0) &&
                   e.Shift == ((modifier & KeyModifier.Shift) != 0) &&
                   e.KeyData.HasFlag(Keys.LWin) == ((modifier & KeyModifier.Win) != 0) &&
                   e.KeyData.HasFlag(Keys.RWin) == ((modifier & KeyModifier.Win) != 0);
        }

        // 判断是否为对应的小键盘数字键
        private static bool IsNumpadEquivalent(int code, int mainKey)
        {
            return mainKey >= (int)Keys.D0 && mainKey <= (int)Keys.D9 &&
                   code == mainKey - (int)Keys.D0 + (int)Keys.NumPad0;
        }
        


        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm(
                enableHotkeys, closeScreenDelay, 
                turnOffScreenKey, turnOffScreenModifier,
                dpmsKey, dpmsModifier,
                brightnessKey, brightnessModifier,
                helpKey, helpModifier))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // 保存新的设置值
                    enableHotkeys = settingsForm.EnableHotkeys;
                    closeScreenDelay = settingsForm.CloseScreenDelay;
                    turnOffScreenKey = settingsForm.TurnOffScreenKey;
                    turnOffScreenModifier = settingsForm.TurnOffScreenModifier;
                    dpmsKey = settingsForm.DpmsKey;
                    dpmsModifier = settingsForm.DpmsModifier;
                    brightnessKey = settingsForm.BrightnessKey;
                    brightnessModifier = settingsForm.BrightnessModifier;
                    helpKey = settingsForm.HelpKey;
                    helpModifier = settingsForm.HelpModifier;
                    
                    // 重新注册热键
                    if (enableHotkeys)
                    {
                        RegisterGlobalHotkeys();
                    }
                    else
                    {
                        UnregisterGlobalHotkeys();
                    }
                    
                    SaveSettings();
                    UpdateStatus("设置已更新");
                    LogOperation($"用户通过界面更新设置：快捷键={enableHotkeys}，延迟关闭屏幕={closeScreenDelay}秒");
                }
            }
        }

        private void ShowHelp()
        {            // 创建帮助菜单
            ContextMenuStrip helpMenu = new ContextMenuStrip();
            
            // 添加设置菜单项（使用&标记设置快捷键为S）
            ToolStripMenuItem settingsMenuItem = new ToolStripMenuItem("设置(&S)");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            
            // 添加分隔线
            helpMenu.Items.Add(new ToolStripSeparator());
            
            // 添加关于菜单项（使用&标记设置快捷键为A）
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("关于(&A)");
            aboutMenuItem.Click += AboutMenuItem_Click;
            
            // 添加检查更新菜单项（使用&标记设置快捷键为U）
            ToolStripMenuItem checkUpdateMenuItem = new ToolStripMenuItem("检查更新(&U)");
            checkUpdateMenuItem.Click += CheckUpdateMenuItem_Click;
            
            // 将菜单项添加到菜单
            helpMenu.Items.Add(settingsMenuItem);
            helpMenu.Items.Add(aboutMenuItem);
            helpMenu.Items.Add(checkUpdateMenuItem);
            
            // 显示菜单在按钮旁边
            helpMenu.Show(btnHelp, new System.Drawing.Point(0, btnHelp.Height));
        }
        
        // 设置菜单项点击事件处理
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            // 调用设置按钮的点击事件处理方法
            btnSettings_Click(sender, e);
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
                    UpdateStatus($"您使用的是最新版本 {updateInfo.LatestVersion}");
                    LogOperation($"检查更新：当前已是最新版本 {updateInfo.LatestVersion}");
                    MessageBox.Show($"最新版本为 {updateInfo.LatestVersion}，您当前使用的版本 {Version} 已是最新版本！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            descriptionLabel.Text = "屏幕控制是一款简单实用的工具，支持三种屏幕控制方式：\n" +
                "1 - 启动系统屏保\n" +
                "2 - DPMS 休眠（显示器进入省电状态）\n" +
                "3 - 亮度调节（支持 ACPI/DDC 的显示器）\n\n" +
                "所有快捷键均可在「设置」中自定义\n" +
                "Alt+A - 关于对话框";
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