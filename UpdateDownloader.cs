using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenControl
{
    /// <summary>
    /// 更新下载器类，负责下载新版本文件
    /// </summary>
    public class UpdateDownloader
    {
        /// <summary>
        /// 显示更新对话框的静态方法，供AutoUpdateManager和MainForm共用
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="statusUpdater">状态更新委托</param>
        /// <param name="logWriter">日志记录委托</param>
        /// <param name="parentForm">父窗体</param>
        public static void ShowUpdateDialog(UpdateChecker.UpdateInfo updateInfo, string currentVersion, Action<string> statusUpdater, Action<string> logWriter, Form parentForm)
        {
            // 创建自定义对话框
            Form customDialog = new Form();
            customDialog.Text = "发现新版本";
            customDialog.Size = new System.Drawing.Size(350, 200);
            customDialog.StartPosition = FormStartPosition.CenterParent;
            customDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            customDialog.MaximizeBox = false;
            customDialog.MinimizeBox = false;
            
            // 创建消息标签
            Label messageLabel = new Label();
            messageLabel.Location = new System.Drawing.Point(20, 20);
            messageLabel.Size = new System.Drawing.Size(300, 60);
            messageLabel.Text = $"发现新版本 {updateInfo.LatestVersion}！\n您当前使用的版本是 {currentVersion}。\n\n请选择操作方式：";
            messageLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            
            // 创建下载按钮
            Button downloadButton = new Button();
            downloadButton.Location = new System.Drawing.Point(20, 90);
            downloadButton.Size = new System.Drawing.Size(100, 30);
            downloadButton.Text = "直接下载";
            
            // 创建浏览器按钮
            Button browserButton = new Button();
            browserButton.Location = new System.Drawing.Point(130, 90);
            browserButton.Size = new System.Drawing.Size(100, 30);
            browserButton.Text = "浏览器查看";
            
            // 创建取消按钮
            Button cancelButton = new Button();
            cancelButton.Location = new System.Drawing.Point(240, 90);
            cancelButton.Size = new System.Drawing.Size(75, 30);
            cancelButton.Text = "取消";
            
            // 添加控件到对话框
            customDialog.Controls.Add(messageLabel);
            customDialog.Controls.Add(downloadButton);
            customDialog.Controls.Add(browserButton);
            customDialog.Controls.Add(cancelButton);
            
            // 设置默认按钮
            customDialog.AcceptButton = downloadButton;
            customDialog.CancelButton = cancelButton;
            
            // 设置按钮事件
            bool dialogResult = false;
            downloadButton.Click += (s, args) => { dialogResult = true; customDialog.Close(); };
            browserButton.Click += (s, args) => { 
                ProcessStartInfo psi = new ProcessStartInfo(updateInfo.ReleaseUrl);
                psi.UseShellExecute = true;
                Process.Start(psi);
                statusUpdater?.Invoke("已打开发布页面");
                customDialog.Close(); 
            };
            cancelButton.Click += (s, args) => customDialog.Close();
            
            // 显示对话框
            if (parentForm != null)
            {
                customDialog.ShowDialog(parentForm);
            }
            else
            {
                customDialog.ShowDialog();
            }
            
            // 如果用户选择直接下载
            if (dialogResult)
            {
                // 构建下载URL（假设发布文件名为ScreenControl.zip）
                string downloadBaseUrl = "https://gitee.com/yylmzxc/screen-control/releases/download/" + updateInfo.LatestVersion;
                string fileName = "ScreenControl.zip"; // 这里可以根据实际发布文件命名约定修改
                
                // 创建下载器并开始下载
                UpdateDownloader downloader = new UpdateDownloader(downloadBaseUrl, updateInfo.LatestVersion, statusUpdater, logWriter, parentForm);
                downloader.ShowDownloadDialog(fileName);
            }
        }

        private readonly string _downloadBaseUrl;
        private readonly string _version;
        private readonly Action<string> _statusUpdater;
        private readonly Action<string> _logWriter;
        private readonly Form _parentForm;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="downloadBaseUrl">下载基础URL</param>
        /// <param name="version">版本号</param>
        /// <param name="statusUpdater">状态更新委托</param>
        /// <param name="logWriter">日志记录委托</param>
        /// <param name="parentForm">父窗体引用</param>
        public UpdateDownloader(string downloadBaseUrl, string version, Action<string> statusUpdater, Action<string> logWriter, Form parentForm)
        {
            _downloadBaseUrl = downloadBaseUrl;
            _version = version;
            _statusUpdater = statusUpdater;
            _logWriter = logWriter;
            _parentForm = parentForm;
        }

        /// <summary>
        /// 显示下载对话框并开始下载
        /// </summary>
        /// <param name="fileName">文件名</param>
        public void ShowDownloadDialog(string fileName)
        {
            DownloadProgressForm progressForm = new DownloadProgressForm();
            progressForm.Text = $"正在下载 {_version} 版本...";
            
            // 开始下载任务
            Task.Run(async () => {
                try
                {
                    // 创建版本文件夹（在当前程序目录下）
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string versionFolder = Path.Combine(appDirectory, "UpdateDownloader", _version);
                    Directory.CreateDirectory(versionFolder);
                    
                    // 下载文件路径
                    string filePath = Path.Combine(versionFolder, fileName);
                    
                    // 更新状态
                    _parentForm.Invoke((MethodInvoker)delegate {
                        _statusUpdater?.Invoke($"正在下载 {fileName}...");
                    });
                    
                    // 记录日志
                    _logWriter?.Invoke($"开始下载文件: {filePath}");
                    
                    // 执行下载
                    await DownloadFileAsync(_downloadBaseUrl + "/" + fileName, filePath, progressForm.UpdateProgress, progressForm.UpdateStatus);
                    
                    // 下载完成
                    _parentForm.Invoke((MethodInvoker)delegate {
                        _statusUpdater?.Invoke($"下载完成: {fileName}");
                        progressForm.Close();
                        
                        // 显示下载完成提示
                                  // 显示下载完成提示
                        DialogResult result = MessageBox.Show(
                            $"文件 {fileName} 下载完成！\n保存位置: {Path.GetDirectoryName(filePath)}\n\n是否打开文件位置？\n是否立即解压并安装更新？", 
                            "下载完成", 
                            MessageBoxButtons.YesNoCancel, 
                            MessageBoxIcon.Information, 
                            MessageBoxDefaultButton.Button1);
                        
                        if (result == DialogResult.Yes)
                        {
                            // 打开文件位置
                            Process.Start("explorer.exe", "/select," + filePath);
                        }
                        else if (result == DialogResult.No)
                        {
                            // 尝试解压并安装更新
                            try
                            {
                                _statusUpdater?.Invoke("正在解压更新文件...");
                                string extractPath = Path.Combine(Path.GetDirectoryName(filePath), "UpdateTemp");
                                if (Directory.Exists(extractPath))
                                {
                                    Directory.Delete(extractPath, true);
                                }
                                Directory.CreateDirectory(extractPath);
                                
                                // 这里可以添加解压逻辑，使用System.IO.Compression或其他解压库
                                // 由于没有引入解压库，这里只显示提示
                                _statusUpdater?.Invoke("更新准备完成，请手动解压并安装。");
                                MessageBox.Show("更新文件已下载，请手动解压并安装。", "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                _statusUpdater?.Invoke($"更新准备失败: {ex.Message}");
                                MessageBox.Show($"更新准备失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    });
                    _logWriter?.Invoke($"文件下载完成: {filePath}");
                }
                catch (Exception ex)
                {
                    _parentForm.Invoke((MethodInvoker)delegate {
                        _statusUpdater?.Invoke($"下载失败: {ex.Message}");
                        progressForm.Close();
                        MessageBox.Show("下载失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    
                    _logWriter?.Invoke($"文件下载失败: {ex.Message}");
                }
            });
            
            // 显示进度对话框
            progressForm.ShowDialog(_parentForm);
        }

        /// <summary>
        /// 异步下载文件
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <param name="statusCallback">状态回调函数</param>
        private async Task DownloadFileAsync(string url, string filePath, Action<int> progressCallback, Action<string> statusCallback)
        {
            using (HttpClient client = new HttpClient())
            {
                // 设置超时时间为30秒
                client.Timeout = TimeSpan.FromSeconds(30);
                
                try
                {
                    // 获取文件大小
                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        // 检查是否为403 Forbidden错误
                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            throw new Exception("下载失败: 403 Forbidden (Rate Limit Exceeded)，IP访问频率限制，请稍后再试");
                        }
                        // 检查其他HTTP错误状态码
                        else if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"下载失败: 网络错误 ({response.StatusCode})，请检查网络连接后重试");
                        }
                        
                        response.EnsureSuccessStatusCode();
                        
                        long totalBytes = response.Content.Headers.ContentLength ?? 0;
                        
                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;
                            
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;
                                
                                // 更新进度
                                if (totalBytes > 0)
                                {
                                    int progress = (int)((totalRead * 100) / totalBytes);
                                    _parentForm.Invoke((MethodInvoker)delegate {
                                        progressCallback?.Invoke(progress);
                                        statusCallback?.Invoke($"已下载 {FormatFileSize(totalRead)} / {FormatFileSize(totalBytes)}");
                                    });
                                }
                            }
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    if ((int)ex.StatusCode == 403)
                    {
                        throw new Exception("下载失败: 403 Forbidden (Rate Limit Exceeded)，IP访问频率限制，请稍后再试");
                    }
                    else
                    {
                        throw new Exception($"下载失败: 网络连接错误 - {ex.Message}，请检查网络连接后重试");
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("下载失败: 请求超时，请检查网络连接后重试");
                }
                catch (IOException ex)
                {
                    throw new Exception($"下载失败: 文件写入错误 - {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return $"{bytes} {sizes[order]}";
        }
    }

    /// <summary>
    /// 下载进度对话框
    /// </summary>
    public class DownloadProgressForm : Form
    {
        private ProgressBar _progressBar;
        private Label _statusLabel;

        public DownloadProgressForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "下载中...";
            this.Size = new System.Drawing.Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // 创建进度条
            _progressBar = new ProgressBar();
            _progressBar.Location = new System.Drawing.Point(20, 40);
            _progressBar.Size = new System.Drawing.Size(340, 20);
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            
            // 创建状态标签
            _statusLabel = new Label();
            _statusLabel.Location = new System.Drawing.Point(20, 70);
            _statusLabel.Size = new System.Drawing.Size(340, 20);
            _statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _statusLabel.Text = "准备下载...";
            
            // 添加控件
            this.Controls.Add(_progressBar);
            this.Controls.Add(_statusLabel);
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="progress">进度值（0-100）</param>
        public void UpdateProgress(int progress)
        {
            _progressBar.Value = progress;
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        /// <param name="status">状态文本</param>
        public void UpdateStatus(string status)
        {
            _statusLabel.Text = status;
        }
    }
}