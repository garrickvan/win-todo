//  Copyright (c) 2026 WinTodo
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System.Text.Json;
using System.Text.Json.Serialization;
using WinTodo.Common;

namespace WinTodo.Services
{
  /// <summary>
  /// 配置数据类，包含应用程序的所有配置项
  /// </summary>
  internal class ConfigData
  {
    public bool IsStayOnTop { get; set; }
    public bool IsPositionLocked { get; set; }
    public bool IsShowingInTaskbar { get; set; }
    public WindowPosition WindowPosition { get; set; } = new WindowPosition { X = 100, Y = 100 };
  }

  /// <summary>
  /// 窗口位置类，包含窗口的X和Y坐标
  /// </summary>
  internal class WindowPosition
  {
    public int X { get; set; }
    public int Y { get; set; }
  }

  /// <summary>
  /// JSON序列化上下文，用于编译时生成序列化代码，避免反射禁用问题
  /// </summary>
  [JsonSerializable(typeof(ConfigData))]
  [JsonSerializable(typeof(WindowPosition))]
  internal partial class ConfigContext : JsonSerializerContext
  { }

  /// <summary>
  /// 配置管理器，负责处理应用程序的配置持久化
  /// </summary>
  public class ConfigManager
  {
    private const string ConfigFileName = "config.json";
    private readonly string _configFilePath;
    private ConfigData _configData = new();

    /// <summary>
    /// JSON序列化选项，缓存以提高性能
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        TypeInfoResolver = ConfigContext.Default
    };

    /// <summary>
    /// 初始化配置管理器
    /// </summary>
    public ConfigManager()
    {
      try
      {
        // 使用PathHelper确保数据目录存在并获取配置文件路径
        PathHelper.EnsureLocalAppDataDirectoryExists();
        _configFilePath = PathHelper.GetDataFilePath(ConfigFileName);
        LoadConfig();
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "初始化配置目录失败");
        // 如果无法创建目录，仍然继续执行，确保应用能够启动
        _configFilePath = PathHelper.GetDataFilePath(ConfigFileName);
        // 恢复已有配置，只记录错误
      }
    }

    /// <summary>
        /// 从文件加载配置
        /// </summary>
        private void LoadConfig()
        {
          if (File.Exists(_configFilePath))
          {
            try
            {
              var json = File.ReadAllText(_configFilePath);
              _configData = JsonSerializer.Deserialize(json, ConfigContext.Default.ConfigData) ?? _configData;
              // 确保WindowPosition不为null
              _configData.WindowPosition ??= new() { X = 100, Y = 100 };
            }
            catch (Exception ex)
            {
              LogHelper.LogError(ex, "反序列化配置失败");
              // 恢复现有配置，只记录错误
            }
          }
          else
          {
            // 如果文件不存在，使用默认配置
            _configData = new()
            {
              IsStayOnTop = false,
              IsPositionLocked = true,
              IsShowingInTaskbar = true,
              WindowPosition = new() { X = 100, Y = 100 }
            };
            // 保存默认配置到文件
            SaveConfig();
          }
        }

    /// <summary>
        /// 保存配置到文件
        /// </summary>
        private void SaveConfig()
        {
          try
          {
            var json = JsonSerializer.Serialize(_configData, ConfigContext.Default.ConfigData);
            File.WriteAllText(_configFilePath, json);
          }
          catch (Exception ex)
          {
            LogHelper.LogError(ex, "保存配置失败");
          }
        }

    /// <summary>
    /// 获取配置值
    /// </summary>
    /// <typeparam name="T">配置项类型</typeparam>
    /// <param name="key">配置项名称</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    public T Get<T>(string key, T defaultValue)
    {
      return key switch
      {
        "is_stay_on_top" => (T)(object)_configData.IsStayOnTop,
        "is_position_locked" => (T)(object)_configData.IsPositionLocked,
        "is_showing_in_taskbar" => (T)(object)_configData.IsShowingInTaskbar,
        _ => defaultValue,
      };
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <param name="key">配置项名称</param>
    /// <param name="value">配置值</param>
    public void Set(string key, bool value)
    {
      switch (key)
      {
        case "is_stay_on_top":
          _configData.IsStayOnTop = value;
          break;
        case "is_position_locked":
          _configData.IsPositionLocked = value;
          break;
        case "is_showing_in_taskbar":
          _configData.IsShowingInTaskbar = value;
          break;
      }
      SaveConfig();
    }

    /// <summary>
        /// 获取窗口位置
        /// </summary>
        /// <returns>窗口位置字典，包含X和Y坐标</returns>
        public Dictionary<string, int> GetWindowPosition()
        {
          return new()
                {
                    { "x", _configData.WindowPosition.X },
                    { "y", _configData.WindowPosition.Y }
                };
        }

    /// <summary>
    /// 更新窗口位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public void UpdateWindowPosition(int x, int y)
    {
      _configData.WindowPosition.X = x;
      _configData.WindowPosition.Y = y;
      SaveConfig();
    }
  }
}

