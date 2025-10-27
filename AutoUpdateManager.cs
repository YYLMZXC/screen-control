using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenControl
{
    /// <summary>
    /// 自动更新管理器类，负责程序启动时自动检查更新
    /// </summary>
    public class AutoUpdateManager
    {
        private readonly string _currentVersion;
        private readonly Action<string> _statusUpdater;
        private readonly Action<string> _logWriter;
        private readonly Form _parentForm;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="currentVersion">当前程序版本</param>
        /// <param name="statusUpdater">更新状态的委托方法</param>
        /// <param name="logWriter">记录日志的委托方法</param>
        /// <param name="parentForm">父窗体引用，用于UI操作</param>
        public AutoUpdateManager(string currentVersion, Action<string> statusUpdater, Action<string> logWriter, Form parentForm)
        {
            _currentVersion = currentVersion;
            _statusUpdater = statusUpdater;
            _logWriter = logWriter;
            _parentForm = parentForm;
        }
        
        /// <summary>
        /// 启动自动检查更新
        /// </summary>
        public void StartAutoCheck()
        {
            // 在后台线程中启动自动检查，避免阻塞UI
            _ = PerformAutoCheckAsync();
        }
        
        /// <summary>
        /// 执行自动检查更新的异步方法
        /// </summary>
        private async Task PerformAutoCheckAsync()
        {
            try
            {
                // 稍微延迟一点，让程序完全启动后再检查
                await Task.Delay(3000);
                
                // 创建更新检查器实例
                UpdateChecker updateChecker = new UpdateChecker();
                
                // 检查更新
                UpdateChecker.UpdateInfo updateInfo = await updateChecker.CheckForUpdatesAsync(_currentVersion);
                
                // 只有在发现新版本时才显示提示
                if (updateInfo.HasUpdate && !string.IsNullOrEmpty(updateInfo.LatestVersion))
                {
                    _parentForm.Invoke((MethodInvoker)delegate
                    {
                        // 更新状态
                        _statusUpdater?.Invoke($"发现新版本: {updateInfo.LatestVersion}");
                        
                        // 记录日志
                        _logWriter?.Invoke($"自动检查更新：发现新版本 {updateInfo.LatestVersion}");
                        
                        // 询问用户是否查看更新
                        DialogResult result = MessageBox.Show(
                            $"发现新版本 {updateInfo.LatestVersion}！\n您当前使用的版本是 {_currentVersion}。\n\n是否查看详情？", 
                            "发现新版本", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Information);
                        
                        if (result == DialogResult.Yes)
                        {
                            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(updateInfo.ReleaseUrl);
                            psi.UseShellExecute = true;
                            System.Diagnostics.Process.Start(psi);
                            _statusUpdater?.Invoke("已打开发布页面");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // 自动检查更新失败时不显示错误，只记录日志
                _logWriter?.Invoke("自动检查更新失败（静默）：" + ex.Message);
            }
        }
    }
}