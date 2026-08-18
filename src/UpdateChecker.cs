using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScreenControl
{
    /// <summary>
    /// 更新检查器类，用于检测应用程序的新版本
    /// </summary>
    public class UpdateChecker
    {
        private const string GiteeReleasesUrl = "https://gitee.com/yylmzxc/screen-control/releases";
        private const string GiteeApiUrl = "https://gitee.com/api/v5/repos/yylmzxc/screen-control/releases/latest";
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 更新信息类
        /// </summary>
        public class UpdateInfo
        {
            public string LatestVersion { get; set; } = string.Empty;
            public bool HasUpdate { get; set; } = false;
            public string ReleaseUrl { get; set; } = GiteeReleasesUrl;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public UpdateChecker()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// 检查是否有新版本
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <returns>更新信息</returns>
        public async Task<UpdateInfo> CheckForUpdatesAsync(string currentVersion)
        {
            var updateInfo = new UpdateInfo();
            
            try
            {
                // 首先尝试使用API获取最新版本
                try
                {
                    var jsonResponse = await _httpClient.GetStringAsync(GiteeApiUrl);
                    updateInfo.LatestVersion = ExtractVersionFromApiResponse(jsonResponse);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // 处理403 Forbidden错误
                        throw new Exception("检查更新失败: 403 Forbidden (Rate Limit Exceeded)，IP访问频率限制，请稍后再试");
                    }
                    else
                    {
                        // 处理其他HTTP错误
                        throw new Exception($"检查更新失败: 网络错误 ({ex.StatusCode})，请检查网络连接后重试");
                    }
                }
                catch (TaskCanceledException)
                {
                    // 处理请求超时
                    throw new Exception("检查更新失败: 请求超时，请检查网络连接后重试");
                }
                catch
                {
                    // API失败时，尝试从HTML页面解析
                    try
                    {
                        var htmlResponse = await _httpClient.GetStringAsync(GiteeReleasesUrl);
                        updateInfo.LatestVersion = ExtractVersionFromHtml(htmlResponse);
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            // 处理403 Forbidden错误
                            throw new Exception("检查更新失败: 403 Forbidden (Rate Limit Exceeded)，IP访问频率限制，请稍后再试");
                        }
                        else
                        {
                            // 处理其他HTTP错误
                            throw new Exception($"检查更新失败: 网络错误 ({ex.StatusCode})，请检查网络连接后重试");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // 处理请求超时
                        throw new Exception("检查更新失败: 请求超时，请检查网络连接后重试");
                    }
                    catch
                    {
                        // 处理其他未预期的异常
                        throw new Exception("检查更新失败: 网络连接异常，请检查网络连接后重试");
                    }
                }

                // 如果获取到了版本号，进行版本比较
                if (!string.IsNullOrEmpty(updateInfo.LatestVersion))
                {
                    updateInfo.HasUpdate = CompareVersions(updateInfo.LatestVersion, currentVersion);
                }
            }
            catch (Exception ex)
            {
                // 如果出现异常，记录但不抛出，返回默认信息
                throw new Exception("检查更新失败: " + ex.Message);
            }

            return updateInfo;
        }

        /// <summary>
        /// 从API响应中提取版本号
        /// </summary>
        /// <param name="jsonResponse">API响应内容</param>
        /// <returns>版本号</returns>
        private string ExtractVersionFromApiResponse(string jsonResponse)
        {
            try
            {
                // 使用简单的字符串查找方法提取版本号
                int tagNameIndex = jsonResponse.IndexOf("\"tag_name\":");
                if (tagNameIndex >= 0)
                {
                    // 查找tag_name值的开始位置
                    int startIndex = jsonResponse.IndexOf('"', tagNameIndex + 11) + 1;
                    // 查找tag_name值的结束位置
                    int endIndex = jsonResponse.IndexOf('"', startIndex);
                    
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        string tagName = jsonResponse.Substring(startIndex, endIndex - startIndex);
                        // 如果tag_name以v开头，去掉v
                        if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            return tagName.Substring(1);
                        }
                        return tagName;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 从HTML页面中提取版本号
        /// </summary>
        /// <param name="html">HTML内容</param>
        /// <returns>版本号</returns>
        private string ExtractVersionFromHtml(string html)
        {
            try
            {
                // 使用正则表达式从HTML中提取版本号
                // 匹配类似 v1.3.1 或 1.3.1 的版本格式
                var match = Regex.Match(html, @"v?([0-9]+\.[0-9]+\.[0-9]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        /// <param name="latestVersion">最新版本号</param>
        /// <param name="currentVersion">当前版本号</param>
        /// <returns>如果最新版本比当前版本新，返回true</returns>
        private bool CompareVersions(string latestVersion, string currentVersion)
        {
            try
            {
                // 分割版本号为数字部分
                var latestParts = latestVersion.Split('.');
                var currentParts = currentVersion.Split('.');

                // 比较每个部分
                int maxLength = Math.Max(latestParts.Length, currentParts.Length);
                for (int i = 0; i < maxLength; i++)
                {
                    int latest = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;
                    int current = i < currentParts.Length ? int.Parse(currentParts[i]) : 0;

                    if (latest > current)
                    {
                        return true;
                    }
                    else if (latest < current)
                    {
                        return false;
                    }
                }

                // 所有部分都相等
                return false;
            }
            catch
            {
                // 如果版本号格式不正确，返回false
                return false;
            }
        }
    }
}