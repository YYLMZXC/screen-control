using System;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class SettingsForm : Form
    {
        // 公共属性，供父窗体访问设置值
        public bool EnableHotkeys { get; private set; }
        public int CloseScreenDelay { get; private set; } // 延迟关闭屏幕的时间（秒）
        public int TurnOffScreenKey { get; private set; }
        public MainForm.KeyModifier TurnOffScreenModifier { get; private set; }
        public int LockScreenKey { get; private set; }
        public MainForm.KeyModifier LockScreenModifier { get; private set; }

        public SettingsForm(bool enableHotkeys, int closeScreenDelay, 
            int turnOffScreenKey, MainForm.KeyModifier turnOffScreenModifier, 
            int lockScreenKey, MainForm.KeyModifier lockScreenModifier)
        {            
            InitializeComponent();
            
            // 初始化设置值
            EnableHotkeys = enableHotkeys;
            CloseScreenDelay = closeScreenDelay;
            TurnOffScreenKey = turnOffScreenKey;
            TurnOffScreenModifier = turnOffScreenModifier;
            LockScreenKey = lockScreenKey;
            LockScreenModifier = lockScreenModifier;
            
            // 加载设置到界面
            LoadSettingsToUI();
            
            // 为快捷键输入框添加事件处理
            txtTurnOffScreenKey.KeyDown += TxtHotkey_KeyDown;
            txtLockScreenKey.KeyDown += TxtHotkey_KeyDown;
        }

        private void LoadSettingsToUI()
        {            
            // 设置复选框状态
            chkEnableHotkeys.Checked = EnableHotkeys;
            
            // 设置延迟时间
            numCloseScreenDelay.Value = CloseScreenDelay;
            
            // 设置关闭屏幕快捷键
            cboTurnOffScreenModifier.SelectedIndex = (int)TurnOffScreenModifier;
            txtTurnOffScreenKey.Text = ((Keys)TurnOffScreenKey).ToString();
            
            // 设置锁屏并关闭屏幕快捷键
            cboLockScreenModifier.SelectedIndex = (int)LockScreenModifier;
            txtLockScreenKey.Text = ((Keys)LockScreenKey).ToString();
            
            // 根据热键启用状态更新控件可用性
            UpdateHotkeyControlsEnabled();
        }
        
        private void UpdateHotkeyControlsEnabled()
        {
            bool isEnabled = chkEnableHotkeys.Checked;
            cboTurnOffScreenModifier.Enabled = isEnabled;
            txtTurnOffScreenKey.Enabled = isEnabled;
            cboLockScreenModifier.Enabled = isEnabled;
            txtLockScreenKey.Enabled = isEnabled;
        }
        
        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            // 阻止默认按键行为
            e.SuppressKeyPress = true;
            
            // 捕获按下的键和修饰键
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                // 记录按键信息
                Keys key = e.KeyCode;
                MainForm.KeyModifier modifier = MainForm.KeyModifier.None;
                
                if (e.Alt)
                    modifier |= MainForm.KeyModifier.Alt;
                if (e.Control)
                    modifier |= MainForm.KeyModifier.Control;
                if (e.Shift)
                    modifier |= MainForm.KeyModifier.Shift;
                if (e.KeyData.HasFlag(Keys.LWin) || e.KeyData.HasFlag(Keys.RWin))
                    modifier |= MainForm.KeyModifier.Win;
                
                // 更新对应的修饰键组合框
                if (textBox == txtTurnOffScreenKey)
                {
                    cboTurnOffScreenModifier.SelectedIndex = (int)modifier;
                    TurnOffScreenModifier = modifier;
                    TurnOffScreenKey = (int)key;
                }
                else if (textBox == txtLockScreenKey)
                {
                    cboLockScreenModifier.SelectedIndex = (int)modifier;
                    LockScreenModifier = modifier;
                    LockScreenKey = (int)key;
                }
                
                // 更新文本框显示
                textBox.Text = key.ToString();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // 验证并保存设置
            if (ValidateAndSaveSettings())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidateAndSaveSettings()
        {            
            try
            {
                // 保存设置
                EnableHotkeys = chkEnableHotkeys.Checked;
                CloseScreenDelay = (int)numCloseScreenDelay.Value;
                
                // 保存快捷键设置
                TurnOffScreenModifier = (MainForm.KeyModifier)cboTurnOffScreenModifier.SelectedIndex;
                LockScreenModifier = (MainForm.KeyModifier)cboLockScreenModifier.SelectedIndex;
                
                // 验证快捷键是否相同
                if (TurnOffScreenKey == LockScreenKey && TurnOffScreenModifier == LockScreenModifier)
                {
                    MessageBox.Show("关闭屏幕和锁屏的快捷键设置不能相同！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                return true;
            }            
            catch (Exception ex)
            {                
                MessageBox.Show("保存设置时出错：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }        }
    }
    
    // 自动生成的部分类，包含UI初始化代码
    public partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private GroupBox groupBox1;
        private CheckBox chkEnableHotkeys;
        private NumericUpDown numCloseScreenDelay;
        private Label labelCloseScreenDelay;
        private Label labelTurnOffScreenKey;
        private Label labelLockScreenKey;
        private ComboBox cboTurnOffScreenModifier;
        private ComboBox cboLockScreenModifier;
        private TextBox txtTurnOffScreenKey;
        private TextBox txtLockScreenKey;
        private Button btnOK;
        private Button btnCancel;
        private Label labelHotkeyHint;
        
        private void chkEnableHotkeys_CheckedChanged(object sender, EventArgs e)
        {
            UpdateHotkeyControlsEnabled();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {            
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelCloseScreenDelay = new System.Windows.Forms.Label();
            this.numCloseScreenDelay = new System.Windows.Forms.NumericUpDown();
            this.chkEnableHotkeys = new System.Windows.Forms.CheckBox();
            this.labelTurnOffScreenKey = new System.Windows.Forms.Label();
            this.labelLockScreenKey = new System.Windows.Forms.Label();
            this.cboTurnOffScreenModifier = new System.Windows.Forms.ComboBox();
            this.cboLockScreenModifier = new System.Windows.Forms.ComboBox();
            this.txtTurnOffScreenKey = new System.Windows.Forms.TextBox();
            this.txtLockScreenKey = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelHotkeyHint = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCloseScreenDelay)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtLockScreenKey);
            this.groupBox1.Controls.Add(this.txtTurnOffScreenKey);
            this.groupBox1.Controls.Add(this.cboLockScreenModifier);
            this.groupBox1.Controls.Add(this.cboTurnOffScreenModifier);
            this.groupBox1.Controls.Add(this.labelLockScreenKey);
            this.groupBox1.Controls.Add(this.labelTurnOffScreenKey);
            this.groupBox1.Controls.Add(this.labelCloseScreenDelay);
            this.groupBox1.Controls.Add(this.numCloseScreenDelay);
            this.groupBox1.Controls.Add(this.chkEnableHotkeys);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(450, 180);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "屏幕控制设置";
            // 
            // labelCloseScreenDelay
            // 
            this.labelCloseScreenDelay.AutoSize = true;
            this.labelCloseScreenDelay.Location = new System.Drawing.Point(27, 60);
            this.labelCloseScreenDelay.Name = "labelCloseScreenDelay";
            this.labelCloseScreenDelay.Size = new System.Drawing.Size(123, 15);
            this.labelCloseScreenDelay.TabIndex = 2;
            this.labelCloseScreenDelay.Text = "延迟关闭屏幕：(秒)";
            // 
            // numCloseScreenDelay
            // 
            this.numCloseScreenDelay.Location = new System.Drawing.Point(156, 58);
            this.numCloseScreenDelay.Maximum = new decimal(new int[] {
            60, 0, 0, 0});
            this.numCloseScreenDelay.Minimum = new decimal(new int[] {
            0, 0, 0, 0});
            this.numCloseScreenDelay.Name = "numCloseScreenDelay";
            this.numCloseScreenDelay.Size = new System.Drawing.Size(130, 21);
            this.numCloseScreenDelay.TabIndex = 1;
            this.numCloseScreenDelay.Value = new decimal(new int[] {
            5, 0, 0, 0});
            // 
            // chkEnableHotkeys
            // 
            this.chkEnableHotkeys.AutoSize = true;
            this.chkEnableHotkeys.Location = new System.Drawing.Point(30, 30);
            this.chkEnableHotkeys.Name = "chkEnableHotkeys";
            this.chkEnableHotkeys.Size = new System.Drawing.Size(240, 19);
            this.chkEnableHotkeys.TabIndex = 0;
            this.chkEnableHotkeys.Text = "启用全局快捷键监听（最小化时也可使用）";
            this.chkEnableHotkeys.UseVisualStyleBackColor = true;
            this.chkEnableHotkeys.CheckedChanged += new System.EventHandler(this.chkEnableHotkeys_CheckedChanged);
            // 
            // labelTurnOffScreenKey
            // 
            this.labelTurnOffScreenKey.AutoSize = true;
            this.labelTurnOffScreenKey.Location = new System.Drawing.Point(27, 95);
            this.labelTurnOffScreenKey.Name = "labelTurnOffScreenKey";
            this.labelTurnOffScreenKey.Size = new System.Drawing.Size(105, 15);
            this.labelTurnOffScreenKey.TabIndex = 3;
            this.labelTurnOffScreenKey.Text = "关闭屏幕快捷键：";
            // 
            // labelLockScreenKey
            // 
            this.labelLockScreenKey.AutoSize = true;
            this.labelLockScreenKey.Location = new System.Drawing.Point(27, 130);
            this.labelLockScreenKey.Name = "labelLockScreenKey";
            this.labelLockScreenKey.Size = new System.Drawing.Size(120, 15);
            this.labelLockScreenKey.TabIndex = 4;
            this.labelLockScreenKey.Text = "锁屏并关闭屏幕：";
            // 
            // cboTurnOffScreenModifier
            // 
            this.cboTurnOffScreenModifier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTurnOffScreenModifier.FormattingEnabled = true;
            this.cboTurnOffScreenModifier.Items.AddRange(new object[] {
            "无",
            "Alt",
            "Ctrl",
            "Ctrl+Alt",
            "Shift",
            "Alt+Shift",
            "Ctrl+Shift",
            "Ctrl+Alt+Shift",
            "Win"});
            this.cboTurnOffScreenModifier.Location = new System.Drawing.Point(156, 92);
            this.cboTurnOffScreenModifier.Name = "cboTurnOffScreenModifier";
            this.cboTurnOffScreenModifier.Size = new System.Drawing.Size(120, 23);
            this.cboTurnOffScreenModifier.TabIndex = 5;
            // 
            // cboLockScreenModifier
            // 
            this.cboLockScreenModifier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLockScreenModifier.FormattingEnabled = true;
            this.cboLockScreenModifier.Items.AddRange(new object[] {
            "无",
            "Alt",
            "Ctrl",
            "Ctrl+Alt",
            "Shift",
            "Alt+Shift",
            "Ctrl+Shift",
            "Ctrl+Alt+Shift",
            "Win"});
            this.cboLockScreenModifier.Location = new System.Drawing.Point(156, 127);
            this.cboLockScreenModifier.Name = "cboLockScreenModifier";
            this.cboLockScreenModifier.Size = new System.Drawing.Size(120, 23);
            this.cboLockScreenModifier.TabIndex = 6;
            // 
            // txtTurnOffScreenKey
            // 
            this.txtTurnOffScreenKey.Location = new System.Drawing.Point(282, 92);
            this.txtTurnOffScreenKey.Name = "txtTurnOffScreenKey";
            this.txtTurnOffScreenKey.Size = new System.Drawing.Size(100, 21);
            this.txtTurnOffScreenKey.TabIndex = 7;
            this.txtTurnOffScreenKey.Text = "D1";
            // 
            // txtLockScreenKey
            // 
            this.txtLockScreenKey.Location = new System.Drawing.Point(282, 127);
            this.txtLockScreenKey.Name = "txtLockScreenKey";
            this.txtLockScreenKey.Size = new System.Drawing.Size(100, 21);
            this.txtLockScreenKey.TabIndex = 8;
            this.txtLockScreenKey.Text = "D2";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(180, 240);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(270, 240);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelHotkeyHint
            // 
            this.labelHotkeyHint.AutoSize = true;
            this.labelHotkeyHint.ForeColor = System.Drawing.Color.Gray;
            this.labelHotkeyHint.Location = new System.Drawing.Point(30, 210);
            this.labelHotkeyHint.Name = "labelHotkeyHint";
            this.labelHotkeyHint.Size = new System.Drawing.Size(352, 15);
            this.labelHotkeyHint.TabIndex = 3;
            this.labelHotkeyHint.Text = "提示：点击快捷键输入框后直接按您想要设置的组合键即可";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 290);
            this.Controls.Add(this.labelHotkeyHint);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "屏幕控制设置";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCloseScreenDelay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}