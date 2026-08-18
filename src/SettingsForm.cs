using System;
using System.Collections.Generic;
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

        public int DpmsKey { get; private set; }
        public MainForm.KeyModifier DpmsModifier { get; private set; }

        public int BrightnessKey { get; private set; }
        public MainForm.KeyModifier BrightnessModifier { get; private set; }

        public int HelpKey { get; private set; }
        public MainForm.KeyModifier HelpModifier { get; private set; }

        // 单个快捷键条目
        private class HotkeyEntry
        {
            public string Name = "";                             // 功能名（用于冲突提示）
            public TextBox TextBox = null!;                      // 捕获输入框
            public int Key;                                      // 按键
            public MainForm.KeyModifier Modifier;                // 修饰键
        }

        private readonly List<HotkeyEntry> entries = new List<HotkeyEntry>();

        public SettingsForm(bool enableHotkeys, int closeScreenDelay,
            int turnOffScreenKey, MainForm.KeyModifier turnOffScreenModifier,
            int dpmsKey, MainForm.KeyModifier dpmsModifier,
            int brightnessKey, MainForm.KeyModifier brightnessModifier,
            int helpKey, MainForm.KeyModifier helpModifier)
        {
            InitializeComponent();

            EnableHotkeys = enableHotkeys;
            CloseScreenDelay = closeScreenDelay;

            // 构建热键条目（顺序与界面一致）
            entries.Add(new HotkeyEntry { Name = "启动系统屏保", TextBox = txtTurnOffScreenKey, Key = turnOffScreenKey, Modifier = turnOffScreenModifier });
            entries.Add(new HotkeyEntry { Name = "DPMS 休眠", TextBox = txtDpmsKey, Key = dpmsKey, Modifier = dpmsModifier });
            entries.Add(new HotkeyEntry { Name = "亮度调节", TextBox = txtBrightnessKey, Key = brightnessKey, Modifier = brightnessModifier });
            entries.Add(new HotkeyEntry { Name = "帮助菜单", TextBox = txtHelpKey, Key = helpKey, Modifier = helpModifier });

            foreach (HotkeyEntry entry in entries)
            {
                entry.TextBox.Text = FormatHotkey(entry.Modifier, entry.Key);
                entry.TextBox.KeyDown += TxtHotkey_KeyDown;
            }

            // 加载设置到界面
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // 设置复选框状态
            chkEnableHotkeys.Checked = EnableHotkeys;

            // 设置延迟时间
            numCloseScreenDelay.Value = CloseScreenDelay;

            // 根据热键启用状态更新控件可用性
            UpdateHotkeyControlsEnabled();
        }

        private void UpdateHotkeyControlsEnabled()
        {
            bool isEnabled = chkEnableHotkeys.Checked;
            foreach (HotkeyEntry entry in entries)
            {
                entry.TextBox.Enabled = isEnabled;
            }
        }

        // 组合键显示文本，如 "Ctrl+Alt+1"
        private static string FormatHotkey(MainForm.KeyModifier modifier, int key)
        {
            string text = "";
            if (modifier.HasFlag(MainForm.KeyModifier.Control)) text += "Ctrl+";
            if (modifier.HasFlag(MainForm.KeyModifier.Alt)) text += "Alt+";
            if (modifier.HasFlag(MainForm.KeyModifier.Shift)) text += "Shift+";
            if (modifier.HasFlag(MainForm.KeyModifier.Win)) text += "Win+";
            return text + FormatKey(key);
        }

        // 按键名显示：主键盘数字键显示为 1-9（枚举名是 D1-D9），小键盘数字键显示为 小键盘0-9
        private static string FormatKey(int key)
        {
            if (key >= (int)Keys.D0 && key <= (int)Keys.D9)
                return ((char)('0' + key - (int)Keys.D0)).ToString();
            if (key >= (int)Keys.NumPad0 && key <= (int)Keys.NumPad9)
                return "小键盘" + (key - (int)Keys.NumPad0);
            return ((Keys)key).ToString();
        }

        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            // 阻止默认按键行为
            e.SuppressKeyPress = true;
            e.Handled = true;

            Keys key = e.KeyCode;

            // 忽略纯修饰键
            if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu ||
                key == Keys.LWin || key == Keys.RWin || key == Keys.None)
            {
                return;
            }

            // 解析修饰键
            MainForm.KeyModifier modifier = MainForm.KeyModifier.None;
            if (e.Alt) modifier |= MainForm.KeyModifier.Alt;
            if (e.Control) modifier |= MainForm.KeyModifier.Control;
            if (e.Shift) modifier |= MainForm.KeyModifier.Shift;
            if (e.KeyData.HasFlag(Keys.LWin) || e.KeyData.HasFlag(Keys.RWin))
                modifier |= MainForm.KeyModifier.Win;

            // 必须至少带一个修饰键（F1-F12 功能键除外），避免裸键误触发
            if (modifier == MainForm.KeyModifier.None &&
                !(key >= Keys.F1 && key <= Keys.F24))
            {
                return;
            }

            if (sender is TextBox textBox)
            {
                HotkeyEntry entry = entries.Find(x => x.TextBox == textBox);
                if (entry != null)
                {
                    entry.Key = (int)key;
                    entry.Modifier = modifier;
                    textBox.Text = FormatHotkey(modifier, (int)key);
                }
            }
        }

        // 检测四个快捷键之间是否存在冲突
        private bool HasConflict(out string conflictInfo)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    if (entries[i].Key == entries[j].Key &&
                        entries[i].Modifier == entries[j].Modifier)
                    {
                        conflictInfo = $"「{entries[i].Name}」与「{entries[j].Name}」使用了相同的快捷键 {FormatHotkey(entries[i].Modifier, entries[i].Key)}";
                        return true;
                    }
                }
            }
            conflictInfo = "";
            return false;
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
            // 保存基本设置
            EnableHotkeys = chkEnableHotkeys.Checked;
            CloseScreenDelay = (int)numCloseScreenDelay.Value;

            // 快捷键冲突检测
            if (EnableHotkeys && HasConflict(out string conflictInfo))
            {
                MessageBox.Show("快捷键冲突：" + conflictInfo + "，请重新设置。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 保存快捷键设置
            TurnOffScreenKey = entries[0].Key;
            TurnOffScreenModifier = entries[0].Modifier;
            DpmsKey = entries[1].Key;
            DpmsModifier = entries[1].Modifier;
            BrightnessKey = entries[2].Key;
            BrightnessModifier = entries[2].Modifier;
            HelpKey = entries[3].Key;
            HelpModifier = entries[3].Modifier;
            return true;
        }
    }

    // 自动生成的部分类，包含UI初始化代码
    public partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        private GroupBox groupBoxGeneral;
        private CheckBox chkEnableHotkeys;
        private NumericUpDown numCloseScreenDelay;
        private Label labelCloseScreenDelay;
        private Button btnOK;
        private Button btnCancel;
        private Label labelHotkeyHint;

        private GroupBox groupBoxHotkeys;
        private Label labelTurnOffScreenKey;
        private TextBox txtTurnOffScreenKey;
        private Label labelDpmsKey;
        private TextBox txtDpmsKey;
        private Label labelBrightnessKey;
        private TextBox txtBrightnessKey;
        private Label labelHelpKey;
        private TextBox txtHelpKey;

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
            this.groupBoxGeneral = new System.Windows.Forms.GroupBox();
            this.chkEnableHotkeys = new System.Windows.Forms.CheckBox();
            this.numCloseScreenDelay = new System.Windows.Forms.NumericUpDown();
            this.labelCloseScreenDelay = new System.Windows.Forms.Label();
            this.groupBoxHotkeys = new System.Windows.Forms.GroupBox();
            this.labelHelpKey = new System.Windows.Forms.Label();
            this.txtHelpKey = new System.Windows.Forms.TextBox();
            this.labelBrightnessKey = new System.Windows.Forms.Label();
            this.txtBrightnessKey = new System.Windows.Forms.TextBox();
            this.labelDpmsKey = new System.Windows.Forms.Label();
            this.txtDpmsKey = new System.Windows.Forms.TextBox();
            this.labelTurnOffScreenKey = new System.Windows.Forms.Label();
            this.txtTurnOffScreenKey = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelHotkeyHint = new System.Windows.Forms.Label();
            this.groupBoxGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCloseScreenDelay)).BeginInit();
            this.groupBoxHotkeys.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxGeneral
            // 
            this.groupBoxGeneral.Controls.Add(this.chkEnableHotkeys);
            this.groupBoxGeneral.Controls.Add(this.numCloseScreenDelay);
            this.groupBoxGeneral.Controls.Add(this.labelCloseScreenDelay);
            this.groupBoxGeneral.Location = new System.Drawing.Point(12, 12);
            this.groupBoxGeneral.Name = "groupBoxGeneral";
            this.groupBoxGeneral.Size = new System.Drawing.Size(456, 105);
            this.groupBoxGeneral.TabIndex = 0;
            this.groupBoxGeneral.TabStop = false;
            this.groupBoxGeneral.Text = "常规设置";
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
            // labelCloseScreenDelay
            // 
            this.labelCloseScreenDelay.AutoSize = true;
            this.labelCloseScreenDelay.Location = new System.Drawing.Point(27, 68);
            this.labelCloseScreenDelay.Name = "labelCloseScreenDelay";
            this.labelCloseScreenDelay.Size = new System.Drawing.Size(123, 15);
            this.labelCloseScreenDelay.TabIndex = 2;
            this.labelCloseScreenDelay.Text = "延迟关闭屏幕：(秒)";
            // 
            // numCloseScreenDelay
            // 
            this.numCloseScreenDelay.Location = new System.Drawing.Point(156, 66);
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
            // groupBoxHotkeys
            // 
            this.groupBoxHotkeys.Controls.Add(this.labelHelpKey);
            this.groupBoxHotkeys.Controls.Add(this.txtHelpKey);
            this.groupBoxHotkeys.Controls.Add(this.labelBrightnessKey);
            this.groupBoxHotkeys.Controls.Add(this.txtBrightnessKey);
            this.groupBoxHotkeys.Controls.Add(this.labelDpmsKey);
            this.groupBoxHotkeys.Controls.Add(this.txtDpmsKey);
            this.groupBoxHotkeys.Controls.Add(this.labelTurnOffScreenKey);
            this.groupBoxHotkeys.Controls.Add(this.txtTurnOffScreenKey);
            this.groupBoxHotkeys.Location = new System.Drawing.Point(12, 127);
            this.groupBoxHotkeys.Name = "groupBoxHotkeys";
            this.groupBoxHotkeys.Size = new System.Drawing.Size(456, 185);
            this.groupBoxHotkeys.TabIndex = 5;
            this.groupBoxHotkeys.TabStop = false;
            this.groupBoxHotkeys.Text = "全局快捷键（点击输入框后直接按组合键设置）";
            // 
            // labelTurnOffScreenKey
            // 
            this.labelTurnOffScreenKey.AutoSize = true;
            this.labelTurnOffScreenKey.Location = new System.Drawing.Point(27, 35);
            this.labelTurnOffScreenKey.Name = "labelTurnOffScreenKey";
            this.labelTurnOffScreenKey.Size = new System.Drawing.Size(105, 15);
            this.labelTurnOffScreenKey.TabIndex = 0;
            this.labelTurnOffScreenKey.Text = "启动系统屏保：";
            // 
            // txtTurnOffScreenKey
            // 
            this.txtTurnOffScreenKey.Location = new System.Drawing.Point(150, 32);
            this.txtTurnOffScreenKey.Name = "txtTurnOffScreenKey";
            this.txtTurnOffScreenKey.Size = new System.Drawing.Size(240, 21);
            this.txtTurnOffScreenKey.TabIndex = 1;
            this.txtTurnOffScreenKey.Text = "Alt+1";
            // 
            // labelDpmsKey
            // 
            this.labelDpmsKey.AutoSize = true;
            this.labelDpmsKey.Location = new System.Drawing.Point(27, 70);
            this.labelDpmsKey.Name = "labelDpmsKey";
            this.labelDpmsKey.Size = new System.Drawing.Size(81, 15);
            this.labelDpmsKey.TabIndex = 2;
            this.labelDpmsKey.Text = "DPMS 休眠：";
            // 
            // txtDpmsKey
            // 
            this.txtDpmsKey.Location = new System.Drawing.Point(150, 67);
            this.txtDpmsKey.Name = "txtDpmsKey";
            this.txtDpmsKey.Size = new System.Drawing.Size(240, 21);
            this.txtDpmsKey.TabIndex = 3;
            this.txtDpmsKey.Text = "Alt+2";
            // 
            // labelBrightnessKey
            // 
            this.labelBrightnessKey.AutoSize = true;
            this.labelBrightnessKey.Location = new System.Drawing.Point(27, 105);
            this.labelBrightnessKey.Name = "labelBrightnessKey";
            this.labelBrightnessKey.Size = new System.Drawing.Size(81, 15);
            this.labelBrightnessKey.TabIndex = 4;
            this.labelBrightnessKey.Text = "亮度调节：";
            // 
            // txtBrightnessKey
            // 
            this.txtBrightnessKey.Location = new System.Drawing.Point(150, 102);
            this.txtBrightnessKey.Name = "txtBrightnessKey";
            this.txtBrightnessKey.Size = new System.Drawing.Size(240, 21);
            this.txtBrightnessKey.TabIndex = 5;
            this.txtBrightnessKey.Text = "Alt+3";
            // 
            // labelHelpKey
            // 
            this.labelHelpKey.AutoSize = true;
            this.labelHelpKey.Location = new System.Drawing.Point(27, 140);
            this.labelHelpKey.Name = "labelHelpKey";
            this.labelHelpKey.Size = new System.Drawing.Size(81, 15);
            this.labelHelpKey.TabIndex = 6;
            this.labelHelpKey.Text = "帮助菜单：";
            // 
            // txtHelpKey
            // 
            this.txtHelpKey.Location = new System.Drawing.Point(150, 137);
            this.txtHelpKey.Name = "txtHelpKey";
            this.txtHelpKey.Size = new System.Drawing.Size(240, 21);
            this.txtHelpKey.TabIndex = 7;
            this.txtHelpKey.Text = "Alt+H";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(170, 355);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(260, 355);
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
            this.labelHotkeyHint.Location = new System.Drawing.Point(30, 322);
            this.labelHotkeyHint.Name = "labelHotkeyHint";
            this.labelHotkeyHint.Size = new System.Drawing.Size(352, 15);
            this.labelHotkeyHint.TabIndex = 3;
            this.labelHotkeyHint.Text = "提示：点击快捷键输入框后直接按您想要设置的组合键即可";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 400);
            this.Controls.Add(this.labelHotkeyHint);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupBoxHotkeys);
            this.Controls.Add(this.groupBoxGeneral);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "屏幕控制设置";
            this.groupBoxGeneral.ResumeLayout(false);
            this.groupBoxGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCloseScreenDelay)).EndInit();
            this.groupBoxHotkeys.ResumeLayout(false);
            this.groupBoxHotkeys.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
