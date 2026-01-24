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

namespace WinTodo.Data
{

    public static class PathHelper
    {
        private const string DataDirectory = "Data";
        private const string AppFolderName = "WinTodo";


        public static string GetLocalAppDataDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath, AppFolderName, DataDirectory);
        }

        /// <summary>
        /// 获取指定文件名的数据文件路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>数据文件路径</returns>
        public static string GetDataFilePath(string fileName)
        {
            return Path.Combine(GetLocalAppDataDirectory(), fileName);
        }

        /// <summary>
        /// 确保本地应用数据目录存在，如果不存在则创建
        /// </summary>
        /// <returns>本地应用数据目录路径</returns>
        public static string EnsureLocalAppDataDirectoryExists()
        {
            string dataDir = GetLocalAppDataDirectory();
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            return dataDir;
        }
    }
}

