using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenControl
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 注册全局异常处理器，记录未处理异常到日志
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => OnUnhandledException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => OnUnhandledException(e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// 未处理异常：记录异常信息到日志文件。
        /// </summary>
        private static void OnUnhandledException(Exception? ex)
        {
            try
            {
                string message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 未处理异常：{ex?.Message}\n{ex?.StackTrace}";
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter("bugs/screencontrol.log", true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
            catch
            {
                // 日志写入失败忽略
            }
        }
    }
}
