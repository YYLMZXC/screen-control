using System;

namespace ScreenControl
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnScreensaver = new System.Windows.Forms.Button();
            this.btnDpmsSleep = new System.Windows.Forms.Button();
            this.btnDisableMonitor = new System.Windows.Forms.Button();
            this.btnBrightness = new System.Windows.Forms.Button();

            this.SuspendLayout();
            // 
            // btnScreensaver
            // 
            this.btnScreensaver.Location = new System.Drawing.Point(95, 100);
            this.btnScreensaver.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnScreensaver.Name = "btnScreensaver";
            this.btnScreensaver.Size = new System.Drawing.Size(195, 55);
            this.btnScreensaver.TabIndex = 1;
            this.btnScreensaver.Text = "启动系统屏保(&1)"; // Alt+1 快捷键
            this.btnScreensaver.UseVisualStyleBackColor = true;
            this.btnScreensaver.Click += new System.EventHandler(this.btnScreensaver_Click);
            // 
            // btnDpmsSleep
            // 
            this.btnDpmsSleep.Location = new System.Drawing.Point(310, 100);
            this.btnDpmsSleep.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnDpmsSleep.Name = "btnDpmsSleep";
            this.btnDpmsSleep.Size = new System.Drawing.Size(195, 55);
            this.btnDpmsSleep.TabIndex = 2;
            this.btnDpmsSleep.Text = "DPMS 休眠(&2)"; // Alt+2 快捷键
            this.btnDpmsSleep.UseVisualStyleBackColor = true;
            this.btnDpmsSleep.Click += new System.EventHandler(this.btnDpmsSleep_Click);
            // 
            // btnDisableMonitor
            // 
            this.btnDisableMonitor.Location = new System.Drawing.Point(95, 180);
            this.btnDisableMonitor.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnDisableMonitor.Name = "btnDisableMonitor";
            this.btnDisableMonitor.Size = new System.Drawing.Size(195, 55);
            this.btnDisableMonitor.TabIndex = 4;
            this.btnDisableMonitor.Text = "禁用显示器(&3)"; // Alt+3 快捷键
            this.btnDisableMonitor.UseVisualStyleBackColor = true;
            this.btnDisableMonitor.Click += new System.EventHandler(this.btnDisableMonitor_Click);
            // 
            // btnBrightness
            // 
            this.btnBrightness.Location = new System.Drawing.Point(310, 180);
            this.btnBrightness.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnBrightness.Name = "btnBrightness";
            this.btnBrightness.Size = new System.Drawing.Size(195, 55);
            this.btnBrightness.TabIndex = 5;
            this.btnBrightness.Text = "亮度调节(&4)"; // Alt+4 快捷键
            this.btnBrightness.UseVisualStyleBackColor = true;
            this.btnBrightness.Click += new System.EventHandler(this.btnBrightness_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 360);
            this.btnHelp = new System.Windows.Forms.Button();
            // 
            // btnHelp
            // 
            this.btnHelp.Location = new System.Drawing.Point(530, 10);
            this.btnHelp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(50, 30);
            this.btnHelp.TabIndex = 3;
            this.btnHelp.Text = "?";
            this.btnHelp.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHelp.ForeColor = System.Drawing.Color.RoyalBlue;
            this.btnHelp.Cursor = System.Windows.Forms.Cursors.Help;
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            
            
            this.Controls.Add(this.btnBrightness);
            this.Controls.Add(this.btnDisableMonitor);
            this.Controls.Add(this.btnDpmsSleep);
            this.Controls.Add(this.btnScreensaver);
            this.Controls.Add(this.btnHelp);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "MainForm";
            this.Text = "屏幕控制";            this.KeyPreview = true;
            this.Load += new System.EventHandler(this.MainForm_Load_1);            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnScreensaver;
        private System.Windows.Forms.Button btnDpmsSleep;
        private System.Windows.Forms.Button btnDisableMonitor;
        private System.Windows.Forms.Button btnBrightness;
        private System.Windows.Forms.Button btnHelp;


    }
}

