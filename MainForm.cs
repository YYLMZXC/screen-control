using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // 立即关闭屏幕
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // 锁屏
        [DllImport("user32.dll")]
        private static extern void LockWorkStation();

        private const uint WM_SYSCOMMAND = 0x0112;
        private const uint SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;

        private void TurnOffScreen()
        {
            try
            {
                // 发送消息关闭屏幕
                SendMessage(this.Handle, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭屏幕失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LockAndTurnOffScreen()
        {
            try
            {
                // 先锁屏
                LockWorkStation();

                // 等待一段时间，确保锁屏完成
                System.Threading.Thread.Sleep(1000);

                // 然后关闭屏幕
                TurnOffScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"锁屏并关闭屏幕失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTurnOffScreen_Click(object sender, EventArgs e)
        {
            // 点击按钮后先锁屏再关闭屏幕
            LockAndTurnOffScreen();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 初始化按钮
            Button btnTurnOffScreen = new Button
            {
                Location = new System.Drawing.Point(100, 50),
                Size = new System.Drawing.Size(200, 50),
                Text = "锁屏并关闭屏幕",
                UseVisualStyleBackColor = true
            };
            btnTurnOffScreen.Click += btnTurnOffScreen_Click;
            this.Controls.Add(btnTurnOffScreen);
        }
    }
}