using Microsoft.UI.Xaml.Navigation;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using WinTodo.Data;
using WinTodo.UI;

namespace WinTodo
{
  /// <summary>
  /// Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  public partial class App : Application
  {
    public Window? window;
    private SystemTrayManager? _systemTrayManager;
    private bool _isPositionLocked;
    private bool _isTopmost;
    private ConfigManager _configManager;
    private bool _isWindowInitialized = false;

    /// <summary>
    /// 获取或设置窗口是否置顶
    /// </summary>
    public bool IsTopmost => _isTopmost;

    /// <summary>
    /// Windows API 导入 - 设置窗口样式（64位）
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrA", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>
    /// Windows API 导入 - 获取窗口样式（64位）
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrA", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    /// <summary>
    /// Windows API 导入 - 设置窗口位置
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// Windows API 导入 - 获取桌面窗口
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "FindWindowA", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// <summary>
    /// Windows API 导入 - 设置父窗口
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>
    /// Windows API 导入 - 获取桌面窗口
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr GetDesktopWindow();

    /// <summary>
    /// Windows API 导入 - 设置窗口显示状态
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Windows API 导入 - 调用原始窗口过程
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcA", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// 窗口样式常量
    /// </summary>
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_SYSMENU = 0x00080000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWLP_WNDPROC = -4;

    /// <summary>
    /// 设置窗口位置的常量
    /// </summary>
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_DRAWFRAME = 0x0020;
    private const uint SWP_NOOWNERZORDER = 0x0200;

    /// <summary>
    /// 显示窗口的常量
    /// </summary>
    private const int SW_SHOW = 5;
    private const int SW_SHOWNA = 8;
    private const int SW_FORCEMINIMIZE = 11;

    /// <summary>
    /// 鼠标消息常量
    /// </summary>
    private const uint WM_NCHITTEST = 0x0084;
    private const uint HTCAPTION = 2;
    private const uint HTCLIENT = 1;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_NCLBUTTONDOWN = 0x00A1;

    /// <summary>
    /// Windows API 导入 - 释放鼠标捕获
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseCapture();

    /// <summary>
    /// Windows API 导入 - 发送消息
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Windows API 导入 - 获取窗口位置和大小
    /// </summary>
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller), SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// 结构体，用于GetWindowRect函数
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    /// <summary>
    /// 窗口过程委托
    /// </summary>
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProc? _wndProc;
    private IntPtr _oldWndProc;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
      this.InitializeComponent();
      _configManager = new ConfigManager();

      // 注册全局异常处理事件
      RegisterGlobalExceptionHandlers();
    }

    /// <summary>
    /// 注册全局异常处理事件
    /// </summary>
    private void RegisterGlobalExceptionHandlers()
    {
      // 处理UI线程未捕获异常
      this.UnhandledException += App_UnhandledException;

      // 处理非UI线程未捕获异常
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

      // 处理异步任务中未观察到的异常
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
      // 设置环境变量，确保Windows App Runtime能正常工作
      Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);

      window = new Window();

      if (window.Content is not Frame rootFrame)
      {
        rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;
        window.Content = rootFrame;
      }

      // 设置窗口大小
      var appWindow = window.AppWindow;
      appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 960, Height = 1080 });

      // 导航到主页面
      _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);

      // 激活窗口
      window.Activate();

      // 初始化系统托盘（在窗口激活后）
      _systemTrayManager = new SystemTrayManager(this, window);

      // 设置窗口为桌面部件模式（在窗口激活后）
      SetupDesktopWidgetMode();

      // 加载并设置窗口位置
      LoadAndSetWindowPosition();

      // 加载并设置窗口置顶状态
      LoadAndSetTopmostState();

      // 加载并设置任务栏显示状态
      LoadAndSetTaskbarVisibilityState();

      // 设置窗口过程钩子，处理窗口拖动事件
      SetWindowProcHook();

      // 添加窗口位置变化监听
      SetupWindowPositionMonitoring();
    }

    /// <summary>
    /// 设置桌面部件模式
    /// </summary>
    private void SetupDesktopWidgetMode()
    {
      try
      {
        // 获取窗口句柄
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window!);

        if (hWnd != IntPtr.Zero)
        {
          // 移除窗口边框和标题栏
          IntPtr stylePtr = GetWindowLongPtr(hWnd, GWL_STYLE);
          int style = (int)stylePtr;
          style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
          SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr(style));

          // 设置扩展样式：无激活、工具窗口、不在任务栏显示
          IntPtr exStylePtr = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
          int exStyle = (int)exStylePtr;
          //   exStyle |= WS_EX_NOACTIVATE;
          // exStyle |= WS_EX_TOOLWINDOW;
          exStyle &= ~WS_EX_APPWINDOW;
          SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

          // 额外确保窗口不在任务栏显示：移除WS_EX_APPWINDOW并添加WS_EX_TOOLWINDOW
          // WS_EX_TOOLWINDOW样式的窗口不会在任务栏显示
          // WS_EX_APPWINDOW样式的窗口会在任务栏显示，所以需要移除

          // 查找Program Manager窗口（桌面的实际窗口）
          IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");

          // 获取桌面窗口句柄
          IntPtr desktopHwnd = GetDesktopWindow();

          // 仅在调试模式下输出信息
          System.Diagnostics.Debug.WriteLine($"桌面窗口句柄: {desktopHwnd}, Program Manager句柄: {progmanHwnd}");

          // 先确保窗口可见
          ShowWindow(hWnd, SW_SHOW);

          // 将窗口设置为Program Manager窗口的子窗口，这样它会显示在桌面背景之上，桌面图标之下
          // 当用户点击返回桌面时，部件会和桌面一起显示，不会被隐藏
          if (progmanHwnd != IntPtr.Zero)
          {
            SetParent(hWnd, progmanHwnd);
          }
          else
          {
            // 如果Program Manager窗口找不到，回退到桌面窗口
            SetParent(hWnd, desktopHwnd);
          }

          // 确保窗口始终可见，并且位于所有窗口的底部，但在桌面背景之上
          SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0,
              SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);

          // 再次显示窗口，确保设置完窗口后窗口仍然可见
          ShowWindow(hWnd, SW_SHOW);
          window?.Activate();

          // 关闭标题栏
          var appWindow = window?.AppWindow;
          if (appWindow != null)
          {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = false;
          }
        }
        else
        {
          System.Diagnostics.Debug.WriteLine("获取窗口句柄失败");
        }
      }
      catch (Exception ex)
      {
        // 捕获并记录异常，避免应用崩溃
        LogHelper.LogError(ex, "设置桌面部件模式失败");
        // 确保窗口仍然可见
        window?.Activate();
      }
    }

    /// <summary>
    /// 加载并设置窗口置顶状态
    /// </summary>
    private void LoadAndSetTopmostState()
    {
      // 从配置中加载置顶状态
      _isTopmost = _configManager.Get<bool>("is_stay_on_top", false);

      // 根据加载的状态设置窗口置顶
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
      if (_isTopmost)
      {
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);
      }
    }

    /// <summary>
    /// 加载并设置任务栏显示状态
    /// </summary>
    private void LoadAndSetTaskbarVisibilityState()
    {
      // 从配置中加载任务栏显示状态
      bool isShowingInTaskbar = _configManager.Get<bool>("is_showing_in_taskbar", false);

      // 根据加载的状态设置窗口是否在任务栏显示
      if (isShowingInTaskbar)
      {
        ShowInTaskbar();
      }
      else
      {
        HideFromTaskbar();
      }
    }

    /// <summary>
    /// 确保窗口始终处于桌面层级（底层）
    /// </summary>
    public void ToggleStayOnBottom()
    {
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

      // 先显示窗口，确保可见
      ShowWindow(hWnd, SW_SHOW);

      // 确保窗口始终处于桌面层级，位于所有窗口的底部，但在桌面背景之上
      SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0,
          SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);

      // 再次显示窗口，确保设置后窗口仍然可见
      ShowWindow(hWnd, SW_SHOW);
      window?.Activate();

      // 更新置顶状态并保存到配置
      _isTopmost = false;
      _configManager.Set("is_stay_on_top", _isTopmost);
    }

    /// <summary>
    /// 切换窗口置顶状态
    /// </summary>
    public void ToggleTopmost()
    {
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

      // 切换置顶状态
      _isTopmost = !_isTopmost;

      // 设置窗口置顶或取消置顶
      IntPtr hWndInsertAfter = _isTopmost ? HWND_TOPMOST : HWND_BOTTOM;
      SetWindowPos(hWnd, hWndInsertAfter, 0, 0, 0, 0,
          SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);

      // 再次显示窗口，确保设置后窗口仍然可见
      ShowWindow(hWnd, SW_SHOW);
      window?.Activate();

      // 保存置顶状态到配置文件
      _configManager.Set("is_stay_on_top", _isTopmost);
    }

    /// <summary>
    /// 切换窗口位置锁定状态
    /// </summary>
    public void TogglePositionLock()
    {
      _isPositionLocked = !_isPositionLocked;
    }

    /// <summary>
    /// 获取窗口位置是否锁定
    /// </summary>
    public bool IsPositionLocked => _isPositionLocked;

    /// <summary>
    /// 设置窗口过程钩子，处理窗口拖动事件
    /// </summary>
    private void SetWindowProcHook()
    {
      if (window == null) return;

      // 获取窗口句柄
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

      System.Diagnostics.Debug.WriteLine($"[调试] 设置窗口过程钩子，窗口句柄: {hWnd}");

      // 创建窗口过程委托
      _wndProc = new WndProc(WindowProc);

      // 获取函数指针
      IntPtr procPtr = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_wndProc);
      System.Diagnostics.Debug.WriteLine($"[调试] 窗口过程函数指针: {procPtr}");

      // 设置窗口过程钩子
      _oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, procPtr);
      System.Diagnostics.Debug.WriteLine($"[调试] 原窗口过程: {_oldWndProc}");
    }

    /// <summary>
    /// 加载并设置窗口位置
    /// </summary>
    private void LoadAndSetWindowPosition()
    {
      if (window == null) return;

      try
      {
        // 从配置中获取窗口位置
        var position = _configManager.GetWindowPosition();
        int x = position["x"];
        int y = position["y"];

        System.Diagnostics.Debug.WriteLine($"[调试] 加载窗口位置: X={x}, Y={y}");

        // 获取窗口句柄
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (hWnd != IntPtr.Zero)
        {
          // 设置窗口位置
          SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0,
              SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOZORDER | SWP_NOOWNERZORDER);

          System.Diagnostics.Debug.WriteLine($"[调试] 窗口位置设置完成");
        }
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "加载窗口位置失败");
      }
      finally
      {
        _isWindowInitialized = true;
      }
    }

    /// <summary>
    /// 设置窗口位置监控
    /// </summary>
    private void SetupWindowPositionMonitoring()
    {
      if (window == null) return;

      // 在窗口关闭时保存窗口位置
      window.Closed += OnWindowClosed;
    }

    /// <summary>
    /// 窗口关闭时保存窗口位置
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
      SaveWindowPosition();
    }

    /// <summary>
    /// 保存当前窗口位置
    /// </summary>
    private void SaveWindowPosition()
    {
      if (window == null || !_isWindowInitialized) return;

      try
      {
        // 获取窗口句柄
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (hWnd != IntPtr.Zero)
        {
          // 获取窗口位置
          if (GetWindowRect(hWnd, out RECT rect))
          {
            int x = rect.Left;
            int y = rect.Top;

            System.Diagnostics.Debug.WriteLine($"[调试] 保存窗口位置: X={x}, Y={y}");

            // 保存到配置
            _configManager.UpdateWindowPosition(x, y);
          }
        }
      }
      catch (Exception ex)
      {
        LogHelper.LogError(ex, "保存窗口位置失败");
      }
    }

    /// <summary>
    /// 窗口消息常量 - 窗口移动或调整大小结束
    /// </summary>
    private const uint WM_EXITSIZEMOVE = 0x0232;

    /// <summary>
    /// 自定义窗口过程，处理窗口消息
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    /// <param name="msg">消息类型</param>
    /// <param name="wParam">消息参数W</param>
    /// <param name="lParam">消息参数L</param>
    /// <returns>处理结果</returns>
    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
      // 添加调试日志，记录所有消息
      System.Diagnostics.Debug.WriteLine($"[调试] 收到窗口消息: {msg}, wParam: {wParam}, lParam: {lParam}");

      // 处理WM_LBUTTONDOWN消息，用于窗口拖动
      if (msg == WM_LBUTTONDOWN)
      {
        System.Diagnostics.Debug.WriteLine($"[调试] 处理WM_LBUTTONDOWN消息，锁定状态: {_isPositionLocked}");

        if (!_isPositionLocked)
        {
          System.Diagnostics.Debug.WriteLine("[调试] 窗口未锁定，执行拖动操作");

          // 释放鼠标捕获
          bool releaseResult = ReleaseCapture();
          System.Diagnostics.Debug.WriteLine($"[调试] ReleaseCapture结果: {releaseResult}");

          // 发送WM_NCLBUTTONDOWN消息，模拟在标题栏上按下鼠标左键
          IntPtr sendResult = SendMessage(hWnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
          System.Diagnostics.Debug.WriteLine($"[调试] SendMessage结果: {sendResult}");

          return IntPtr.Zero;
        }
        else
        {
          System.Diagnostics.Debug.WriteLine("[调试] 窗口已锁定，忽略拖动操作");
        }
      }
      // 处理窗口移动或调整大小结束的消息，保存窗口位置
      else if (msg == WM_EXITSIZEMOVE)
      {
        System.Diagnostics.Debug.WriteLine("[调试] 处理WM_EXITSIZEMOVE消息，保存窗口位置");
        SaveWindowPosition();
      }

      // 对于其他消息，调用原始窗口过程处理
      IntPtr result = CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
      System.Diagnostics.Debug.WriteLine($"[调试] 原始窗口过程返回: {result}");
      return result;
    }

    /// <summary>
    /// 获取窗口是否在任务栏显示
    /// </summary>
    public bool IsShowingInTaskbar
    {
      get
      {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        if (hWnd == IntPtr.Zero) return false;

        // 获取当前扩展样式
        IntPtr exStylePtr = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
        int exStyle = (int)exStylePtr;

        // 检查是否有WS_EX_APPWINDOW样式且没有WS_EX_TOOLWINDOW样式
        return (exStyle & WS_EX_APPWINDOW) != 0 && (exStyle & WS_EX_TOOLWINDOW) == 0;
      }
    }

    /// <summary>
    /// 显示任务栏
    /// </summary>
    public void ShowInTaskbar()
    {
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
      if (hWnd == IntPtr.Zero) return;

      // 获取当前扩展样式
      IntPtr exStylePtr = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
      int exStyle = (int)exStylePtr;

      // 添加WS_EX_APPWINDOW样式（在任务栏显示）
      exStyle |= WS_EX_APPWINDOW;
      // 移除WS_EX_TOOLWINDOW样式（工具窗口不会在任务栏显示）
      exStyle &= ~WS_EX_TOOLWINDOW;

      // 设置新的扩展样式
      SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

      // 确保窗口可见
      ShowWindow(hWnd, SW_SHOW);
      window?.Activate();
    }

    /// <summary>
    /// 隐藏任务栏
    /// </summary>
    public void HideFromTaskbar()
    {
      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
      if (hWnd == IntPtr.Zero) return;

      // 获取当前扩展样式
      IntPtr exStylePtr = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
      int exStyle = (int)exStylePtr;

      // 移除WS_EX_APPWINDOW样式（不在任务栏显示）
      exStyle &= ~WS_EX_APPWINDOW;
      // 添加WS_EX_TOOLWINDOW样式（工具窗口不会在任务栏显示）
      exStyle |= WS_EX_TOOLWINDOW;

      // 设置新的扩展样式
      SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(exStyle));

      // 确保窗口可见
      ShowWindow(hWnd, SW_SHOW);
      window?.Activate();
    }

    /// <summary>
    /// 切换任务栏显示状态
    /// </summary>
    public void ToggleTaskbarVisibility()
    {
      if (IsShowingInTaskbar)
      {
        HideFromTaskbar();
      }
      else
      {
        ShowInTaskbar();
      }

      // 保存任务栏显示状态到配置文件
      _configManager.Set("is_showing_in_taskbar", IsShowingInTaskbar);
    }

    /// <summary>
    /// 退出应用
    /// </summary>
    public new void Exit()
    {
      base.Exit();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
      // 记录导航失败的真实异常信息
      string message = $"Failed to load Page {e.SourcePageType.FullName}";
      if (e.Exception != null)
      {
        LogHelper.LogError(e.Exception, $"导航失败: {message}");
      }
      else
      {
        LogHelper.LogError(new Exception(message), "导航失败");
      }
    }

    /// <summary>
    /// 处理UI线程未捕获异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">异常事件参数</param>
    private void App_UnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
      LogHelper.LogError(e.Exception, "UI线程未捕获异常");
      e.Handled = true; // 标记为已处理，防止应用崩溃
    }

    /// <summary>
    /// 处理非UI线程未捕获异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">异常事件参数</param>
    private void CurrentDomain_UnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception exception)
      {
        LogHelper.LogError(exception, "非UI线程未捕获异常");
      }
      else
      {
        LogHelper.LogError(new Exception($"非UI线程未捕获异常: {e.ExceptionObject}"), "非UI线程未捕获异常");
      }
    }

    /// <summary>
    /// 处理异步任务中未观察到的异常
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">异常事件参数</param>
    private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
      LogHelper.LogError(e.Exception, "异步任务未观察到的异常");
      e.SetObserved(); // 标记为已观察，防止应用崩溃
    }
  }
}

