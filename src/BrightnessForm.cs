using System;
using System.ComponentModel;
using System.Drawing;
using System.Management;
using System.Windows.Forms;

namespace ScreenControl
{
    /// <summary>
    /// 亮度调节对话框。通过 WMI (WmiMonitorBrightness / WmiMonitorBrightnessMethods) 读取和设置屏幕亮度。
    /// 仅支持支持 DDC/CI 或 ACPI 亮度调节的显示器（主要是笔记本电脑内置屏）。
    /// </summary>
    public partial class BrightnessForm : Form
    {
        private TrackBar _trackBarBrightness = null!;
        private Label _labelValue = null!;
        private Label _labelHint = null!;
        private Button _btnOK = null!;
        private Button _btnCancel = null!;

        private byte _originalBrightness = 255;
        private bool _applyOnChange = true;

        public BrightnessForm()
        {
            InitializeComponent();

            // 设计器打开窗体时跳过 WMI 查询（OOP 设计器进程中执行
            // WMI 操作可能导致设计器服务崩溃，表现为 IUIService 缺失）
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            LoadCurrentBrightness();
        }

        private void InitializeComponent()
        {
            this._trackBarBrightness = new System.Windows.Forms.TrackBar();
            this._labelValue = new System.Windows.Forms.Label();
            this._labelHint = new System.Windows.Forms.Label();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._trackBarBrightness)).BeginInit();
            this.SuspendLayout();
            // 
            // _trackBarBrightness
            // 
            this._trackBarBrightness.Location = new System.Drawing.Point(30, 45);
            this._trackBarBrightness.Maximum = 100;
            this._trackBarBrightness.Name = "_trackBarBrightness";
            this._trackBarBrightness.Size = new System.Drawing.Size(330, 45);
            this._trackBarBrightness.TabIndex = 0;
            this._trackBarBrightness.TickFrequency = 10;
            this._trackBarBrightness.ValueChanged += new System.EventHandler(this.TrackBarBrightness_ValueChanged);
            // 
            // _labelValue
            // 
            this._labelValue.AutoSize = true;
            this._labelValue.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this._labelValue.Location = new System.Drawing.Point(160, 15);
            this._labelValue.Name = "_labelValue";
            this._labelValue.Size = new System.Drawing.Size(0, 21);
            this._labelValue.TabIndex = 1;
            this._labelValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _labelHint
            // 
            this._labelHint.AutoSize = true;
            this._labelHint.ForeColor = System.Drawing.Color.Gray;
            this._labelHint.Location = new System.Drawing.Point(30, 95);
            this._labelHint.Name = "_labelHint";
            this._labelHint.Size = new System.Drawing.Size(0, 17);
            this._labelHint.TabIndex = 2;
            this._labelHint.Text = "提示：仅支持支持 ACPI/DDC 亮度控制的显示器（通常是笔记本内置屏）。";
            // 
            // _btnOK
            // 
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.Location = new System.Drawing.Point(170, 140);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(90, 32);
            this._btnOK.TabIndex = 3;
            this._btnOK.Text = "确定";
            this._btnOK.UseVisualStyleBackColor = true;
            this._btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(270, 140);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(90, 32);
            this._btnCancel.TabIndex = 4;
            this._btnCancel.Text = "取消";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // BrightnessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(390, 195);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._labelHint);
            this.Controls.Add(this._labelValue);
            this.Controls.Add(this._trackBarBrightness);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BrightnessForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "亮度调节";
            ((System.ComponentModel.ISupportInitialize)(this._trackBarBrightness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadCurrentBrightness()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT * FROM WmiMonitorBrightness"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        byte current = Convert.ToByte(obj["CurrentBrightness"]);
                        _originalBrightness = current;
                        _trackBarBrightness.Value = current;
                        _labelValue.Text = current + "%";
                        return;
                    }
                }
                _labelHint.Text = "当前显示器不支持 ACPI 亮度调节（多为台式机外接屏）。";
                _applyOnChange = false;
            }
            catch (Exception ex)
            {
                _labelHint.Text = "读取亮度失败：" + ex.Message;
                _applyOnChange = false;
            }
        }

        private void TrackBarBrightness_ValueChanged(object sender, EventArgs e)
        {
            _labelValue.Text = _trackBarBrightness.Value + "%";
            if (_applyOnChange)
            {
                ApplyBrightness((byte)_trackBarBrightness.Value);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (_applyOnChange)
            {
                ApplyBrightness((byte)_trackBarBrightness.Value);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // 取消时恢复原始亮度
            if (_applyOnChange && _originalBrightness != 255)
            {
                ApplyBrightness(_originalBrightness);
            }
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ApplyBrightness(byte brightness)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj.InvokeMethod("WmiSetBrightness", new object[] { 1, brightness });
                    }
                }
            }
            catch (Exception ex)
            {
                _applyOnChange = false;
                _labelHint.Text = "设置亮度失败：" + ex.Message;
            }
        }
    }
}
