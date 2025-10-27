using System;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class SettingsForm : Form
    {
        // 公共属性，供父窗体访问设置值
        public int IdleTimeThreshold { get; private set; }
        public int ScreenOffInitialDelay { get; private set; }
        public int DelayWakeUpDuration { get; private set; }
        public bool EnableHotkeys { get; private set; }
        public bool DelayWakeUpEnabled { get; private set; }

        public SettingsForm(int idleTimeThreshold, int screenOffInitialDelay, int delayWakeUpDuration, bool enableHotkeys, bool delayWakeUpEnabled)
        {
            InitializeComponent();
            
            // 初始化设置值
            IdleTimeThreshold = idleTimeThreshold;
            ScreenOffInitialDelay = screenOffInitialDelay;
            DelayWakeUpDuration = delayWakeUpDuration;
            EnableHotkeys = enableHotkeys;
            DelayWakeUpEnabled = delayWakeUpEnabled;
            
            // 加载设置到界面
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // 设置数值输入控件的值
            txtIdleTimeThreshold.Text = (IdleTimeThreshold / 1000).ToString(); // 显示为秒
            txtScreenOffInitialDelay.Text = (ScreenOffInitialDelay / 1000).ToString(); // 显示为秒
            txtDelayWakeUpDuration.Text = (DelayWakeUpDuration / 1000).ToString(); // 显示为秒
            
            // 设置复选框状态
            chkEnableHotkeys.Checked = EnableHotkeys;
            chkDelayWakeUp.Checked = DelayWakeUpEnabled;
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
                // 验证并保存空闲时间阈值（秒转毫秒）
                int idleTimeSeconds = int.Parse(txtIdleTimeThreshold.Text);
                if (idleTimeSeconds < 1 || idleTimeSeconds > 300)
                {
                    MessageBox.Show("空闲时间阈值必须在1-300秒之间", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                IdleTimeThreshold = idleTimeSeconds * 1000;
                
                // 验证并保存屏幕关闭初始延迟（秒转毫秒）
                int initialDelaySeconds = int.Parse(txtScreenOffInitialDelay.Text);
                if (initialDelaySeconds < 0 || initialDelaySeconds > 60)
                {
                    MessageBox.Show("屏幕关闭初始延迟必须在0-60秒之间", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                ScreenOffInitialDelay = initialDelaySeconds * 1000;
                
                // 验证并保存延迟唤醒持续时间（秒转毫秒）
                int wakeUpDelaySeconds = int.Parse(txtDelayWakeUpDuration.Text);
                if (wakeUpDelaySeconds < 0 || wakeUpDelaySeconds > 30)
                {
                    MessageBox.Show("延迟唤醒持续时间必须在0-30秒之间", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                DelayWakeUpDuration = wakeUpDelaySeconds * 1000;
                
                // 保存其他设置
                EnableHotkeys = chkEnableHotkeys.Checked;
                DelayWakeUpEnabled = chkDelayWakeUp.Checked;
                
                return true;
            }
            catch (FormatException)
            {
                MessageBox.Show("请输入有效的数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存设置时出错：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
    
    // 自动生成的部分类，包含UI初始化代码
    public partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private GroupBox groupBox1;
        private Label label1;
        private TextBox txtIdleTimeThreshold;
        private Label label2;
        private TextBox txtScreenOffInitialDelay;
        private Label label3;
        private TextBox txtDelayWakeUpDuration;
        private CheckBox chkDelayWakeUp;
        private CheckBox chkEnableHotkeys;
        private Button btnOK;
        private Button btnCancel;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;

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
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.chkDelayWakeUp = new System.Windows.Forms.CheckBox();
            this.chkEnableHotkeys = new System.Windows.Forms.CheckBox();
            this.txtDelayWakeUpDuration = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtScreenOffInitialDelay = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtIdleTimeThreshold = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.chkDelayWakeUp);
            this.groupBox1.Controls.Add(this.chkEnableHotkeys);
            this.groupBox1.Controls.Add(this.txtDelayWakeUpDuration);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtScreenOffInitialDelay);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtIdleTimeThreshold);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(370, 230);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "屏幕控制设置";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(110, 170);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 15);
            this.label7.TabIndex = 11;
            this.label7.Text = "秒";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(110, 120);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 15);
            this.label6.TabIndex = 10;
            this.label6.Text = "秒";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(110, 70);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(29, 15);
            this.label5.TabIndex = 9;
            this.label5.Text = "秒";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 190);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(229, 15);
            this.label4.TabIndex = 8;
            this.label4.Text = "说明：初始延迟期间将完全忽略唤醒检测";
            // 
            // chkDelayWakeUp
            // 
            this.chkDelayWakeUp.AutoSize = true;
            this.chkDelayWakeUp.Location = new System.Drawing.Point(200, 130);
            this.chkDelayWakeUp.Name = "chkDelayWakeUp";
            this.chkDelayWakeUp.Size = new System.Drawing.Size(119, 19);
            this.chkDelayWakeUp.TabIndex = 7;
            this.chkDelayWakeUp.Text = "启用延迟唤醒检测";
            this.chkDelayWakeUp.UseVisualStyleBackColor = true;
            // 
            // chkEnableHotkeys
            // 
            this.chkEnableHotkeys.AutoSize = true;
            this.chkEnableHotkeys.Location = new System.Drawing.Point(200, 70);
            this.chkEnableHotkeys.Name = "chkEnableHotkeys";
            this.chkEnableHotkeys.Size = new System.Drawing.Size(99, 19);
            this.chkEnableHotkeys.TabIndex = 6;
            this.chkEnableHotkeys.Text = "启用快捷键";
            this.chkEnableHotkeys.UseVisualStyleBackColor = true;
            // 
            // txtDelayWakeUpDuration
            // 
            this.txtDelayWakeUpDuration.Location = new System.Drawing.Point(140, 165);
            this.txtDelayWakeUpDuration.Name = "txtDelayWakeUpDuration";
            this.txtDelayWakeUpDuration.Size = new System.Drawing.Size(50, 25);
            this.txtDelayWakeUpDuration.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 170);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(128, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "延迟唤醒持续时间：";
            // 
            // txtScreenOffInitialDelay
            // 
            this.txtScreenOffInitialDelay.Location = new System.Drawing.Point(140, 115);
            this.txtScreenOffInitialDelay.Name = "txtScreenOffInitialDelay";
            this.txtScreenOffInitialDelay.Size = new System.Drawing.Size(50, 25);
            this.txtScreenOffInitialDelay.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "屏幕关闭初始延迟：";
            // 
            // txtIdleTimeThreshold
            // 
            this.txtIdleTimeThreshold.Location = new System.Drawing.Point(140, 65);
            this.txtIdleTimeThreshold.Name = "txtIdleTimeThreshold";
            this.txtIdleTimeThreshold.Size = new System.Drawing.Size(50, 25);
            this.txtIdleTimeThreshold.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "输入活动检测阈值：";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(220, 250);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(300, 250);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(395, 290);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "屏幕控制设置";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}