// MIT License
//
// Copyright (c) 2026 WinTodo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;

namespace WinTodo.Common
{

    public static class LogHelper
    {
        private const string LogFileName = "runtime.log";
        private static readonly string _logFilePath;


        static LogHelper()
        {
            // 使用PathHelper确保数据目录存在并获取日志文件路径
            string dataDirectory = PathHelper.EnsureLocalAppDataDirectoryExists();
            _logFilePath = Path.Combine(dataDirectory, LogFileName);
        }


        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="additionalMessage">附加消息</param>
        public static void LogError(Exception exception, string additionalMessage = "")
        {
            try
            {
                // 安全获取异常信息，避免在访问异常属性时抛出新异常
                string exceptionType = "";
                string exceptionMessage = "";
                string exceptionStackTrace = "";

                if (exception != null)
                {
                    try
                    {
                        exceptionType = exception.GetType().FullName ?? "未知类型";
                    }
                    catch { exceptionType = "获取类型失败"; }

                    try
                    {
                        exceptionMessage = exception.Message ?? "无异常消息";
                    }
                    catch { exceptionMessage = "获取消息失败"; }

                    try
                    {
                        exceptionStackTrace = exception.StackTrace ?? "无堆栈跟踪";
                    }
                    catch { exceptionStackTrace = "获取堆栈跟踪失败"; }
                }

                // 构建日志内容
                string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: " +
                                   $"{additionalMessage}";

                // 只有当附加消息不为空时才添加换行符，否则直接添加异常信息
                if (!string.IsNullOrWhiteSpace(additionalMessage))
                {
                    logContent += Environment.NewLine;
                }
                else
                {
                    logContent += Environment.NewLine;
                }

                logContent += $"Exception Type: {exceptionType}{Environment.NewLine}" +
                              $"Message: {exceptionMessage}{Environment.NewLine}" +
                              $"Stack Trace: {exceptionStackTrace}{Environment.NewLine}" +
                              $"{new string('-', 80)}{Environment.NewLine}";

                // 写入日志文件
                File.AppendAllText(_logFilePath, logContent);
            }
            catch (Exception ex)
            {
                // 防止日志记录自身出错导致应用崩溃
                Console.WriteLine($"Failed to write log: {ex.Message}");
                // 尝试使用更简单的方式记录错误
                try
                {
                    string simpleLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: 日志记录失败 - {ex.Message}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, simpleLog);
                }
                catch { /* 忽略所有日志记录错误 */ }
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="context">日志上下文（可选）</param>
        public static void LogInfo(string message, string context = "")
        {
            try
            {
                string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] INFO: {message}";
                if (!string.IsNullOrWhiteSpace(context))
                {
                    logContent += $" [{context}]";
                }
                logContent += Environment.NewLine;
                File.AppendAllText(_logFilePath, logContent);
            }
            catch (Exception ex)
            {
                // 防止日志记录自身出错导致应用崩溃
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">警告消息</param>
        /// <param name="context">日志上下文（可选）</param>
        public static void LogWarning(string message, string context = "")
        {
            try
            {
                string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] WARNING: {message}";
                if (!string.IsNullOrWhiteSpace(context))
                {
                    logContent += $" [{context}]";
                }
                logContent += Environment.NewLine;
                File.AppendAllText(_logFilePath, logContent);
            }
            catch (Exception ex)
            {
                // 防止日志记录自身出错导致应用崩溃
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }
  }
}

