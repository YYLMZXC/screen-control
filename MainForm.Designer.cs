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
            this.btnTurnOffScreen = new System.Windows.Forms.Button();
            this.btnLockAndTurnOffScreen = new System.Windows.Forms.Button();
            this.chkDelayWakeUp = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnTurnOffScreen
            // 
            this.btnTurnOffScreen.Location = new System.Drawing.Point(100, 150);
            this.btnTurnOffScreen.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnTurnOffScreen.Name = "btnTurnOffScreen";
            this.btnTurnOffScreen.Size = new System.Drawing.Size(150, 50);
            this.btnTurnOffScreen.TabIndex = 1;
            this.btnTurnOffScreen.Text = "关闭屏幕(&1)"; // Alt+1 快捷键
            this.btnTurnOffScreen.UseVisualStyleBackColor = true;
            this.btnTurnOffScreen.Click += new System.EventHandler(this.btnTurnOffScreen_Click);
            // 
            // btnLockAndTurnOffScreen
            // 
            this.btnLockAndTurnOffScreen.Location = new System.Drawing.Point(350, 150);
            this.btnLockAndTurnOffScreen.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnLockAndTurnOffScreen.Name = "btnLockAndTurnOffScreen";
            this.btnLockAndTurnOffScreen.Size = new System.Drawing.Size(150, 50);
            this.btnLockAndTurnOffScreen.TabIndex = 2;
            this.btnLockAndTurnOffScreen.Text = "锁屏并关闭屏幕(&2)"; // Alt+2 快捷键
            this.btnLockAndTurnOffScreen.UseVisualStyleBackColor = true;
            this.btnLockAndTurnOffScreen.Click += new System.EventHandler(this.btnLockAndTurnOffScreen_Click);
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
            
            // 
            // chkDelayWakeUp
            // 
            this.chkDelayWakeUp.AutoSize = true;
            this.chkDelayWakeUp.Location = new System.Drawing.Point(200, 280);
            this.chkDelayWakeUp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.chkDelayWakeUp.Name = "chkDelayWakeUp";
            this.chkDelayWakeUp.Size = new System.Drawing.Size(216, 16);
            this.chkDelayWakeUp.TabIndex = 4;
            this.chkDelayWakeUp.Text = "启用延迟唤醒检测（防止鼠标误唤醒）";
            this.chkDelayWakeUp.UseVisualStyleBackColor = true;
            this.chkDelayWakeUp.Checked = true;
            
            // 
            // chkGlobalHotkeys
            // 
            this.chkGlobalHotkeys = new System.Windows.Forms.CheckBox();
            this.chkGlobalHotkeys.AutoSize = true;
            this.chkGlobalHotkeys.Location = new System.Drawing.Point(200, 310);
            this.chkGlobalHotkeys.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.chkGlobalHotkeys.Name = "chkGlobalHotkeys";
            this.chkGlobalHotkeys.Size = new System.Drawing.Size(240, 16);
            this.chkGlobalHotkeys.TabIndex = 5;
            this.chkGlobalHotkeys.Text = "启用全局快捷键监听（最小化时也可使用）";
            this.chkGlobalHotkeys.UseVisualStyleBackColor = true;
            this.chkGlobalHotkeys.Checked = true;
            this.chkGlobalHotkeys.CheckedChanged += new System.EventHandler(this.chkGlobalHotkeys_CheckedChanged);

            this.Controls.Add(this.btnTurnOffScreen);
            this.Controls.Add(this.btnLockAndTurnOffScreen);
            this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.chkDelayWakeUp);
            this.Controls.Add(this.chkGlobalHotkeys);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "MainForm";
            this.Text = "屏幕控制";            this.KeyPreview = true;
            this.Load += new System.EventHandler(this.MainForm_Load_1);            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnTurnOffScreen;
        private System.Windows.Forms.Button btnLockAndTurnOffScreen;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.CheckBox chkDelayWakeUp;
        private System.Windows.Forms.CheckBox chkGlobalHotkeys;
    }
}

