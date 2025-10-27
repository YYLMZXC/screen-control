using System;
using System.Windows.Forms;

namespace ScreenControl
{
    public partial class SettingsForm : Form
    {
        // 公共属性，供父窗体访问设置值
        public bool EnableHotkeys { get; private set; }

        public SettingsForm(bool enableHotkeys)
        {
            InitializeComponent();
            
            // 初始化设置值
            EnableHotkeys = enableHotkeys;
            
            // 加载设置到界面
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // 设置复选框状态
            chkEnableHotkeys.Checked = EnableHotkeys;
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
                
                return true;
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
        private CheckBox chkEnableHotkeys;
        private Button btnOK;
        private Button btnCancel;

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
            this.chkEnableHotkeys = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkEnableHotkeys);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(280, 80);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "屏幕控制设置";
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
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(120, 140);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(210, 140);
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
            this.ClientSize = new System.Drawing.Size(310, 190);
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
            this.ResumeLayout(false);
        }
    }
}