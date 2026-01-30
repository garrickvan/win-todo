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

using System.Runtime.InteropServices;
using WinTodo.Common;

namespace WinTodo.Services
{
  /// <summary>
  /// 系统托盘管理器，负责创建和管理系统托盘图标及菜单
  /// </summary>
  /// <param name="app">应用实例</param>
  /// <param name="window">主窗口实例</param>
  public partial class SystemTrayManager : IDisposable
  {
    private readonly App _app;
    private readonly Window _window;
    private NotifyIconData _notifyIconData;
    private bool _isDisposed;
    private WindowMessageHook? _messageHook;

    /// <summary>
    /// 构造函数，初始化系统托盘
    /// </summary>
    /// <param name="app">应用实例</param>
    /// <param name="window">主窗口实例</param>
    public SystemTrayManager(App app, Window window)
    {
      _app = app;
      _window = window;
      Initialize();
    }

    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    private void Initialize()
    {
      try
      {
        // 获取窗口句柄
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        if (hWnd == IntPtr.Zero)
        {
          LogHelper.LogError(new Exception("获取窗口句柄失败"), "初始化系统托盘失败");
          return;
        }

        // 初始化NotifyIconData结构体
        _notifyIconData = new NotifyIconData
        {
          cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
          hWnd = hWnd,
          uID = 1,
          uFlags = NotifyIconFlags.NIF_ICON | NotifyIconFlags.NIF_MESSAGE | NotifyIconFlags.NIF_TIP,
          uCallbackMessage = WM_USER + 1,
          szTip = "WinTodo桌面工具",
          dwState = 0,
          dwStateMask = 0
        };

        // 加载图标资源
        _notifyIconData.hIcon = LoadIconFromResource();
        if (_notifyIconData.hIcon == IntPtr.Zero)
        {
          LogHelper.LogError(new Exception("加载图标资源失败"), "初始化系统托盘失败");
          return;
        }

        // 添加系统托盘图标
        bool result = Shell_NotifyIcon(NotifyIconMessage.NIM_ADD, ref _notifyIconData);
        if (!result)
        {
          LogHelper.LogError(new Exception("添加系统托盘图标失败"), "初始化系统托盘失败");
          return;
        }

        // 设置窗口消息钩子，用于处理托盘消息（放在最后，确保图标已成功添加）
        _messageHook = new WindowMessageHook(hWnd);
        _messageHook.MessageReceived += OnWindowMessageReceived;
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "初始化系统托盘失败");
        // 确保在发生异常时释放已分配的资源
        if (_messageHook != null)
        {
          _messageHook.Dispose();
          _messageHook = null;
        }
        // 尝试移除可能已添加的托盘图标
        try
        {
          Shell_NotifyIcon(NotifyIconMessage.NIM_DELETE, ref _notifyIconData);
        }
        catch (Exception removeEx)
        {
          LogHelper.LogError(removeEx, "移除系统托盘图标失败");
        }
      }
    }

    /// <summary>
    /// 从资源加载图标，提供多种加载方案以提高成功率
    /// </summary>
    /// <returns>图标句柄</returns>
    private IntPtr LoadIconFromResource()
    {
      try
      {
        // 方案1: 尝试使用ExtractIcon从当前可执行文件提取图标（优先级调整，因为这是最可靠的）
        IntPtr hIcon = ExtractIconFromExecutable();
        if (hIcon != IntPtr.Zero)
        {
          return hIcon;
        }

        // 方案2: 尝试使用LoadIcon加载Windows标准图标
        hIcon = LoadStandardWindowsIcon();
        if (hIcon != IntPtr.Zero)
        {
          return hIcon;
        }

        // 方案3: 尝试使用LoadIcon加载默认应用图标
        hIcon = LoadDefaultIcon();
        if (hIcon != IntPtr.Zero)
        {
          return hIcon;
        }

        // 方案4: 尝试使用GetStockObject获取默认图标
        hIcon = GetStockObject(StockObjectType.OBM_APPLICATION);
        if (hIcon != IntPtr.Zero)
        {
          return hIcon;
        }

        LogHelper.LogError(new Exception("所有图标加载方案均失败"), "加载图标资源失败");
        return IntPtr.Zero;
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "加载图标资源失败");
        return IntPtr.Zero;
      }
    }

    /// <summary>
    /// 使用LoadIcon加载默认图标
    /// </summary>
    /// <returns>图标句柄</returns>
    private IntPtr LoadDefaultIcon()
    {
      try
      {
        // 尝试使用GetModuleHandle获取当前模块句柄
        IntPtr hModule = GetModuleHandle(null);
        if (hModule != IntPtr.Zero)
        {
          // 尝试加载默认图标，使用数值常量
          IntPtr hIcon = LoadIcon(hModule, (IntPtr)IDI_APPLICATION);
          if (hIcon != IntPtr.Zero)
          {
            return hIcon;
          }
        }

        // 如果加载失败，尝试使用Windows默认图标
        IntPtr systemIcon = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
        if (systemIcon != IntPtr.Zero)
        {
          return systemIcon;
        }

        return IntPtr.Zero;
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "使用LoadIcon加载默认图标失败");
        return IntPtr.Zero;
      }
    }

    /// <summary>
    /// 从当前可执行文件提取图标
    /// </summary>
    /// <returns>图标句柄</returns>
    private IntPtr ExtractIconFromExecutable()
    {
      try
      {
        // 尝试获取当前可执行文件路径的多种方式
        string exePath = string.Empty;

        // 方式1: 使用Process.GetCurrentProcess().MainModule
        var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
        if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
        {
          exePath = mainModule.FileName;
        }
        // 方式2: 使用Assembly.GetExecutingAssembly().Location
        else if (string.IsNullOrEmpty(exePath))
        {
          exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
        // 方式3: 使用Assembly.GetEntryAssembly().Location
        else if (string.IsNullOrEmpty(exePath))
        {
          var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
          if (entryAssembly != null)
          {
            exePath = entryAssembly.Location;
          }
        }

        if (!string.IsNullOrEmpty(exePath))
        {
          IntPtr hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
          if (hIcon != IntPtr.Zero && hIcon != (IntPtr)1)
          {
            return hIcon;
          }
        }
        return IntPtr.Zero;
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "从可执行文件提取图标失败");
        return IntPtr.Zero;
      }
    }

    /// <summary>
    /// 加载Windows标准图标
    /// </summary>
    /// <returns>图标句柄</returns>
    private IntPtr LoadStandardWindowsIcon()
    {
      try
      {
        // 尝试加载Windows标准图标，如应用程序图标、信息图标等
        // 使用数值常量替代字符串常量，提高兼容性
        const int IDI_APPLICATION = 32512;
        const int IDI_INFORMATION = 32516;
        const int IDI_WARNING = 32513;
        const int IDI_ERROR = 32514;
        const int IDI_QUESTION = 32515;

        // 尝试加载不同的标准图标
        int[] standardIcons = { IDI_APPLICATION, IDI_INFORMATION, IDI_WARNING, IDI_QUESTION, IDI_ERROR };

        foreach (int iconId in standardIcons)
        {
          IntPtr hIcon = LoadIcon(IntPtr.Zero, (IntPtr)iconId);
          if (hIcon != IntPtr.Zero)
          {
            return hIcon;
          }
        }

        return IntPtr.Zero;
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "加载Windows标准图标失败");
        return IntPtr.Zero;
      }
    }

    /// <summary>
    /// Win32 API - 加载图标
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    /// <summary>
    /// Win32 API - 获取系统默认对象
    /// </summary>
    [DllImport("gdi32.dll")]
    private static extern IntPtr GetStockObject(StockObjectType fnObject);

    /// <summary>
    /// 系统对象类型枚举
    /// </summary>
    private enum StockObjectType : int
    {
      OBM_APPLICATION = 32752
    }

    /// <summary>
    /// Win32 API - 从可执行文件提取图标
    /// </summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    /// <summary>
    /// 默认图标常量
    /// </summary>
    private const int IDI_APPLICATION = 32512;

    /// <summary>
    /// 处理窗口消息
    /// </summary>
    /// <param name="message">消息类型</param>
    /// <param name="wParam">消息参数W</param>
    /// <param name="lParam">消息参数L</param>
    private void OnWindowMessageReceived(uint message, IntPtr wParam, IntPtr lParam)
    {
      if (message == _notifyIconData.uCallbackMessage)
      {
        HandleTrayMessage(wParam, lParam);
      }
    }

    /// <summary>
    /// 处理托盘消息
    /// </summary>
    /// <param name="wParam">消息参数W</param>
    /// <param name="lParam">消息参数L</param>
    private void HandleTrayMessage(IntPtr wParam, IntPtr lParam)
    {
      // 检查是否是当前托盘图标
      if ((uint)wParam != _notifyIconData.uID)
      {
        return;
      }

      // 处理不同的鼠标事件
      switch ((WindowMessage)lParam)
      {
        case WindowMessage.WM_RBUTTONUP:
          // 右键点击显示菜单
          ShowContextMenu();
          break;
        case WindowMessage.WM_LBUTTONUP:
          // 左键点击可以添加其他逻辑
          break;
      }
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu()
    {
      try
      {
        // 获取当前鼠标位置
        var mousePoint = new Point();
        GetCursorPos(out mousePoint);

        // 设置鼠标位置为菜单显示位置
        SetForegroundWindow(_notifyIconData.hWnd);

        // 创建菜单
        IntPtr hMenu = CreatePopupMenu();

        // 添加菜单项
        // 置顶菜单项，根据当前状态显示不同的文本和选中状态
        string topmostText = _app.IsTopmost ? "取消置顶" : "置顶";
        MenuFlags topmostFlags = _app.IsTopmost ? MenuFlags.MF_STRING | MenuFlags.MF_CHECKED : MenuFlags.MF_STRING;
        AppendMenu(hMenu, topmostFlags, 1001, topmostText);
        AppendMenu(hMenu, MenuFlags.MF_STRING, 1002, "打开数据目录");
        // 添加开机启动菜单项，根据当前状态显示不同的文本和选中状态
        bool isStartupEnabled = IsStartupEnabled();
        string startupText = isStartupEnabled ? "取消开机启动" : "开机启动";
        MenuFlags startupFlags = isStartupEnabled ? MenuFlags.MF_STRING | MenuFlags.MF_CHECKED : MenuFlags.MF_STRING;
        AppendMenu(hMenu, startupFlags, 1005, startupText);

        // 任务栏菜单项，根据当前状态显示不同的文本
        string taskbarText = _app.IsShowingInTaskbar ? "隐藏任务栏" : "显示任务栏";
        AppendMenu(hMenu, MenuFlags.MF_STRING, 1004, taskbarText);

        AppendMenu(hMenu, MenuFlags.MF_SEPARATOR, 0, string.Empty);
        AppendMenu(hMenu, MenuFlags.MF_STRING, 1003, "退出");

        // 显示菜单
        int command = TrackPopupMenu(
            hMenu,
            MenuFlags.TPM_RETURNCMD | MenuFlags.TPM_RIGHTBUTTON,
            mousePoint.X,
            mousePoint.Y,
            0,
            _notifyIconData.hWnd,
            IntPtr.Zero);

        // 处理菜单命令
        HandleMenuCommand(command);

        // 销毁菜单
        DestroyMenu(hMenu);
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "显示托盘菜单失败");
      }
    }

    /// <summary>
    /// 处理菜单命令
    /// </summary>
    /// <param name="command">命令ID</param>
    private void HandleMenuCommand(int command)
    {
      switch (command)
      {
        case 1001:
          // 切换置顶状态
          _app.ToggleTopmost();
          break;
        case 1002:
          // 打开数据目录
          OpenDataDirectory();
          break;
        case 1005:
          // 切换开机启动状态
          ToggleStartup();
          break;
        case 1004:
          // 切换任务栏显示状态
          _app.ToggleTaskbarVisibility();
          break;
        case 1003:
          // 退出应用
          _app.Exit();
          break;
      }
    }

    /// <summary>
    /// 打开数据目录
    /// </summary>
    private static void OpenDataDirectory()
    {
      try
      {
        // 使用PathHelper获取并确保本地应用数据目录存在
        string dataDirectory = PathHelper.EnsureLocalAppDataDirectoryExists();

        // 打开文件资源管理器
        System.Diagnostics.Process.Start("explorer.exe", dataDirectory);
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "打开数据目录失败");
      }
    }

    /// <summary>
    /// 检查是否启用了开机启动
    /// </summary>
    /// <returns>是否启用了开机启动</returns>
    private static bool IsStartupEnabled()
    {
      try
      {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
        if (key != null)
        {
          // 检查是否存在名为启动项
          var value = key.GetValue(APPLICATION_NAME) as string;
          return !string.IsNullOrEmpty(value);
        }
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "检查开机启动状态失败");
      }
      return false;
    }

    /// <summary>
    /// 切换开机启动状态
    /// </summary>
    private static void ToggleStartup()
    {
      try
      {
        bool isEnabled = IsStartupEnabled();
        if (isEnabled)
        {
          // 禁用开机启动
          using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
          key?.DeleteValue(APPLICATION_NAME, false);
        }
        else
        {
          // 启用开机启动
          // 获取当前进程的主模块路径，这会返回实际的可执行文件路径
          string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ??
                          System.Reflection.Assembly.GetExecutingAssembly().Location;
          using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
          key?.SetValue(APPLICATION_NAME, exePath);
        }
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "切换开机启动状态失败");
      }
    }

    /// <summary>
    /// 更新系统托盘图标
    /// </summary>
    public void UpdateTrayIcon()
    {
      try
      {
        Shell_NotifyIcon(NotifyIconMessage.NIM_MODIFY, ref _notifyIconData);
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "更新系统托盘图标失败");
      }
    }

    /// <summary>
    /// 删除系统托盘图标
    /// </summary>
    private void RemoveTrayIcon()
    {
      try
      {
        Shell_NotifyIcon(NotifyIconMessage.NIM_DELETE, ref _notifyIconData);
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "移除系统托盘图标失败");
      }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
      if (!_isDisposed)
      {
        if (disposing)
        {
          // 释放托管资源
          _messageHook?.Dispose();
        }

        // 释放非托管资源
        RemoveTrayIcon();

        _isDisposed = true;
      }
    }

    #region 常量定义

    /// <summary>
    /// 应用程序名称，用于注册表启动项
    /// </summary>
    private const string APPLICATION_NAME = "WinTodo_Desktop_Tool";

    #endregion

    #region Win32 API 定义

    private const uint WM_USER = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, ref NotifyIconData pnid);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr hMenu, MenuFlags uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int TrackPopupMenu(IntPtr hMenu, MenuFlags uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    // 获取当前模块句柄
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [Flags]
    private enum LoadLibraryFlags : uint
    {
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
      public int X;
      public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
      public uint cbSize;
      public IntPtr hWnd;
      public uint uID;
      public NotifyIconFlags uFlags;
      public uint uCallbackMessage;
      public IntPtr hIcon;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string szTip;
      public uint dwState;
      public uint dwStateMask;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string szInfo;
      public uint uVersion;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string szInfoTitle;
      public uint dwInfoFlags;
      public Guid guidItem;
      public IntPtr hBalloonIcon;
    }

    [Flags]
    private enum NotifyIconMessage
    {
      NIM_ADD = 0x00000000,
      NIM_MODIFY = 0x00000001,
      NIM_DELETE = 0x00000002,
      NIM_SETFOCUS = 0x00000003,
      NIM_SETVERSION = 0x00000004
    }

    [Flags]
    private enum NotifyIconFlags
    {
      NIF_MESSAGE = 0x00000001,
      NIF_ICON = 0x00000002,
      NIF_TIP = 0x00000004,
      NIF_STATE = 0x00000008,
      NIF_INFO = 0x00000010,
      NIF_GUID = 0x00000020,
      NIF_REALTIME = 0x00000040,
      NIF_SHOWTIP = 0x00000080
    }

    [Flags]
    private enum MenuFlags
    {
      MF_STRING = 0x00000000,
      MF_SEPARATOR = 0x00000800,
      MF_CHECKED = 0x00000008,
      MF_UNCHECKED = 0x00000001,
      TPM_RETURNCMD = 0x00000100,
      TPM_RIGHTBUTTON = 0x00000002
    }

    private enum WindowMessage
    {
      WM_RBUTTONUP = 0x0205,
      WM_LBUTTONUP = 0x0202,
      WM_CONTEXTMENU = 0x007B
    }

    #endregion

    /// <summary>
    /// 窗口消息钩子，用于拦截窗口消息
    /// </summary>
    private partial class WindowMessageHook : IDisposable
    {
      private readonly IntPtr _hWnd;
      private readonly WndProc _wndProc;
      private IntPtr _oldWndProc;
      private bool _isDisposed;

      /// <summary>
      /// 消息接收事件
      /// </summary>
      public event Action<uint, IntPtr, IntPtr>? MessageReceived;

      /// <summary>
      /// 构造函数
      /// </summary>
      /// <param name="hWnd">窗口句柄</param>
      public WindowMessageHook(IntPtr hWnd)
      {
        _hWnd = hWnd;
        _wndProc = new WndProc(WindowProc);
        _oldWndProc = IntPtr.Zero;

        try
        {
          _oldWndProc = SetWindowLongPtr(_hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
        }
        catch (Exception ex)
        {
          LogHelper.LogError(ex, "设置窗口过程失败");
        }
      }

      /// <summary>
      /// 窗口过程函数
      /// </summary>
      private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
      {
        try
        {
          // 触发消息事件
          MessageReceived?.Invoke(msg, wParam, lParam);
        }
        catch (Exception ex)
        {
          LogHelper.LogError(ex, "处理窗口消息失败");
        }

        // 调用原窗口过程
        try
        {
          if (_oldWndProc != IntPtr.Zero)
          {
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
          }
        }
        catch (Exception ex)
        {
          LogHelper.LogError(ex, "调用原窗口过程失败");
        }

        // 如果原窗口过程为空，调用DefWindowProc
        return DefWindowProc(hWnd, msg, wParam, lParam);
      }

      /// <summary>
      /// 释放资源
      /// </summary>
      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      /// <summary>
      /// 释放资源
      /// </summary>
      /// <param name="disposing">是否释放托管资源</param>
      protected virtual void Dispose(bool disposing)
      {
        if (!_isDisposed)
        {
          // 恢复原窗口过程
          if (_oldWndProc != IntPtr.Zero)
          {
            try
            {
              SetWindowLongPtr(_hWnd, GWLP_WNDPROC, _oldWndProc);
            }
            catch (Exception ex)
            {
              LogHelper.LogError(ex, "恢复原窗口过程失败");
            }
            finally
            {
              _oldWndProc = IntPtr.Zero;
            }
          }

          _isDisposed = true;
        }
      }

      #region Win32 API

      private const int GWLP_WNDPROC = -4;

      private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

      // 根据系统位数选择合适的函数
      [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
      private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

      [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
      private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

      [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
      private static extern IntPtr CallWindowProc64(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

      [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
      private static extern IntPtr CallWindowProc32(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

      /// <summary>
      /// 调用默认窗口过程
      /// </summary>
      [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
      private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

      /// <summary>
      /// 设置窗口过程，根据系统位数自动选择合适的函数
      /// </summary>
      private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
      {
        if (IntPtr.Size == 8) // 64位系统
        {
          return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        else // 32位系统
        {
          return SetWindowLong32(hWnd, nIndex, dwNewLong);
        }
      }

      /// <summary>
      /// 调用窗口过程，根据系统位数自动选择合适的函数
      /// </summary>
      private static IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
      {
        if (IntPtr.Size == 8) // 64位系统
        {
          return CallWindowProc64(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
        }
        else // 32位系统
        {
          return CallWindowProc32(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
        }
      }

      #endregion
    }
  }
}

