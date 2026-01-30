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
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using WinTodo.Services;

namespace WinTodo.Views
{
  /// <summary>
  /// 任务应用主页面
  /// </summary>
  public sealed partial class MainPage : Page
  {
    private DataManager _dataManager = new();
    private ConfigManager _configManager = new();
    private string _currentCategory = "工作";
    private bool _isStayOnTop;
    private bool _isPositionLocked;
    private bool _isDragging;
    private PointInt32 _dragStartPoint;
    private PointInt32 _windowStartPosition;
    private MenuFlyout? _contextMenu; // 右键菜单

    // 自定义Point结构体，用于替代System.Drawing.Point
    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
      public int X;
      public int Y;
    }

    // Windows API 导入
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out Point lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // Windows API 常量
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;

    // RECT 结构体定义
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    /// <summary>
    /// 分类定义
    /// </summary>
    private readonly List<(string Name, string Icon)> _categories = new List<(string Name, string Icon)>
        {
            ("工作", "📋"),
            ("生活", "🏠"),
            ("学习", "📚"),
            ("回收站", "🗑️")
        };

    /// <summary>
    /// 主页面构造函数
    /// </summary>
    public MainPage()
    {
      this.InitializeComponent();
      InitializeApp();
    }

    /// <summary>
    /// 初始化应用
    /// </summary>
    private void InitializeApp()
    {
      // 初始化数据管理器
      _dataManager = new();

      // 初始化配置管理器
      _configManager = new();

      // 加载配置
      LoadConfig();

      // 初始化固定按钮图标
      LockButton.Content = _isPositionLocked ? "🔒" : "🔓";

      // 初始化右键菜单
      InitializeContextMenu();

      // 创建分类标签
      CreateCategoryTabs();

      // 加载任务
      LoadTasks();
    }

    /// <summary>
    /// 初始化右键菜单
    /// </summary>
    private void InitializeContextMenu()
    {
      // 创建右键菜单
      _contextMenu = new();

      // 创建添加任务菜单项
      MenuFlyoutItem addTaskItem = new();
      addTaskItem.Text = "添加任务";
      addTaskItem.Click += AddTaskMenuItem_Click;

      // 将菜单项添加到菜单
      _contextMenu.Items.Add(addTaskItem);
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
      _isStayOnTop = _configManager.Get("is_stay_on_top", false);
      _isPositionLocked = _configManager.Get("is_position_locked", false);
    }

    /// <summary>
    /// 创建分类标签
    /// </summary>
    private void CreateCategoryTabs()
    {
      foreach (var (name, icon) in _categories)
      {
        Button btn = new Button
        {
          Content = $"{icon} {name}",
          FontSize = 13,
          FontWeight = Microsoft.UI.Text.FontWeights.Bold,
          Style = (Style)Application.Current.Resources["CategoryButtonStyle"]
        };

        // 设置初始样式
        UpdateCategoryButtonStyle(btn, name == _currentCategory);

        // 添加点击事件
        btn.Click += (sender, e) => OnCategoryClicked(name);

        // 添加到标签栏
        CategoryTabs.Children.Add(btn);
      }

      // 更新标签上的未完成任务数量
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 更新分类标签上的未完成任务数量
    /// </summary>
    private void UpdateCategoryTabsCounts()
    {
      for (int i = 0; i < CategoryTabs.Children.Count; i++)
      {
        if (CategoryTabs.Children[i] is Button btn)
        {
          string categoryName = _categories[i].Name;
          string icon = _categories[i].Icon;

          // 只对工作、生活、学习标签显示未完成数量
          if (categoryName != "回收站")
          {
            int pendingCount = _dataManager.GetPendingTasksByGroup(categoryName);
            btn.Content = $"{icon} {categoryName} ({pendingCount})";
          }
          else
          {
            btn.Content = $"{icon} {categoryName}";
          }
        }
      }
    }

    /// <summary>
    /// 更新分类按钮样式
    /// </summary>
    /// <param name="btn">按钮</param>
    /// <param name="isSelected">是否选中</param>
    private void UpdateCategoryButtonStyle(Button btn, bool isSelected)
    {
      if (isSelected)
      {
        // 使用预定义样式
        btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 227, 242, 253)); // #E3F2FD
        btn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 91, 155, 213)); // #5B9BD5
      }
      else
      {
        btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        btn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 58, 64)); // #343A40
      }
    }

    /// <summary>
    /// 分类点击事件
    /// </summary>
    /// <param name="categoryName">分类名称</param>
    private void OnCategoryClicked(string categoryName)
    {
      _currentCategory = categoryName;

      // 更新所有分类按钮样式
      for (int i = 0; i < CategoryTabs.Children.Count; i++)
      {
        if (CategoryTabs.Children[i] is Button btn)
        {
          string btnCategoryName = _categories[i].Name;
          UpdateCategoryButtonStyle(btn, btnCategoryName == categoryName);
        }
      }

      // 刷新任务列表
      RefreshTasks();
    }

    /// <summary>
    /// 加载任务
    /// </summary>
    private void LoadTasks()
    {
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 刷新任务列表
    /// </summary>
    private void RefreshTasks()
    {
      // 清空任务容器
      TasksContainer.Children.Clear();

      // 获取当前分类的任务
      List<TaskItem> tasks;
      if (_currentCategory == "回收站")
      {
        tasks = _dataManager.GetRecycleBinTasks().ToList();
      }
      else
      {
        tasks = _dataManager.GetTasksByGroup(_currentCategory).ToList();
      }

      // 排序：未完成任务在前，已完成任务在后；未完成任务按紧急度降序（紧急>重要>一般），已完成任务按创建时间排序
      var sortedTasks = tasks.OrderBy(t => t.Completed)
                           .ThenByDescending(t => !t.Completed ? t.Priority : 0)
                           .ThenByDescending(t => t.CreatedAt)
                           .ToList();

      // 添加任务项
      foreach (var task in sortedTasks)
      {
        _ = AddTaskItem(task);
      }

      // 如果没有任务，显示提示
      if (tasks.Count == 0)
      {
        ShowEmptyState();
      }
    }

    /// <summary>
    /// 添加任务项
    /// </summary>
    /// <param name="task">任务数据</param>
    /// <returns>添加的 TodoItem 对象</returns>
    private TodoItem AddTaskItem(TaskItem task)
    {
      TodoItem todoItem = new(task);
      todoItem.StatusChanged += OnTaskStatusChanged;
      todoItem.TitleEdited += OnTaskTitleEdited;
      todoItem.DeleteSignal += OnTaskDelete;
      todoItem.RestoreSignal += OnTaskRestore;
      todoItem.PermanentDeleteSignal += OnTaskPermanentDelete;
      todoItem.PriorityChanged += OnTaskPriorityChanged;
      TasksContainer.Children.Add(todoItem);
      return todoItem;
    }

    /// <summary>
    /// 显示空状态
    /// </summary>
    private void ShowEmptyState()
    {
      StackPanel emptyPanel = new()
      {
        Margin = new(0, 40, 0, 40),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Orientation = Orientation.Vertical,
        Spacing = 12
      };

      // 空状态图标
      TextBlock emptyIcon = new()
      {
        Text = "📝",
        FontSize = 48,
        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
        HorizontalAlignment = HorizontalAlignment.Center
      };

      // 空状态标题
      TextBlock emptyTitle = new()
      {
        Text = "没有任务",
        FontSize = 16,
        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
        HorizontalAlignment = HorizontalAlignment.Center
      };

      // 空状态描述
      TextBlock emptyDesc = new()
      {
        Text = "当前分组中没有任务，点击上方\"添加任务\"按钮创建一个新任务",
        FontSize = 12,
        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
        HorizontalAlignment = HorizontalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.WrapWholeWords,
        MaxWidth = 300
      };

      emptyPanel.Children.Add(emptyIcon);
      emptyPanel.Children.Add(emptyTitle);
      emptyPanel.Children.Add(emptyDesc);

      TasksContainer.Children.Add(emptyPanel);
    }

    /// <summary>
    /// 添加任务按钮点击事件
    /// </summary>
    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
      // 使用当前选中的分类作为新任务分组
      string group = _currentCategory;
      if (group == "回收站")
      {
        group = "工作"; // 从回收站添加任务时，默认添加到工作分组
      }

      // 添加空标题新任务
      _dataManager.AddTask("", group, "");

      // 刷新任务列表
      RefreshTasks();

      // 更新统计信息
      UpdateBottomStats();
      UpdateCategoryTabsCounts();

      // 不再自动聚焦到最新添加的任务输入框
      // 让用户手动双击任务项进行编辑
    }

    /// <summary>
    /// 添加任务按钮鼠标悬停事件
    /// </summary>
    private void AddTaskButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
      // 使用预定义样式
      AddTaskButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
    }

    /// <summary>
    /// 添加任务按钮鼠标离开事件
    /// </summary>
    private void AddTaskButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
      // 使用预定义样式
      AddTaskButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    /// <summary>
    /// 任务状态变化事件处理
    /// </summary>
    private void OnTaskStatusChanged(object? sender, (int TaskId, bool Completed) e)
    {
      _dataManager.UpdateTaskStatus(e.TaskId, e.Completed);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 任务标题编辑事件处理
    /// </summary>
    private void OnTaskTitleEdited(object? sender, (int TaskId, string NewTitle) e)
    {
      _dataManager.UpdateTaskTitle(e.TaskId, e.NewTitle);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 任务删除事件处理
    /// </summary>
    private void OnTaskDelete(object? sender, int taskId)
    {
      _dataManager.DeleteTask(taskId);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 任务恢复事件处理
    /// </summary>
    private void OnTaskRestore(object? sender, int taskId)
    {
      _dataManager.RestoreTask(taskId);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 任务永久删除事件处理
    /// </summary>
    private void OnTaskPermanentDelete(object? sender, int taskId)
    {
      _dataManager.PermanentDeleteTask(taskId);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 任务优先级变更事件处理
    /// </summary>
    private void OnTaskPriorityChanged(object? sender, (int TaskId, int Priority) e)
    {
      _dataManager.UpdateTaskPriority(e.TaskId, e.Priority);
      RefreshTasks();
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }

    /// <summary>
    /// 更新底部统计信息
    /// </summary>
    private void UpdateBottomStats()
    {
      var stats = _dataManager.GetTaskCount();
      StatsLabel.Text = $"总任务: {stats["total"]} 已完成: {stats["completed"]} 进行中: {stats["pending"]}";
      UpdateLabel.Text = $"最后更新: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// 固定/解锁按钮点击事件处理
    /// </summary>
    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
      // 切换固定状态
      _isPositionLocked = !_isPositionLocked;

      // 更新按钮图标
      LockButton.Content = _isPositionLocked ? "🔒" : "🔓";

      // 保存配置
      _configManager.Set("is_position_locked", _isPositionLocked);

      // 获取应用实例并更新锁定状态
      var app = Application.Current as App;
      if (app != null)
      {
        app.TogglePositionLock();
      }
    }

    /// <summary>
    /// 鼠标按下事件处理，开始拖动
    /// </summary>
    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
      if (_isPositionLocked)
        return;

      // 获取当前窗口句柄
      var window = (Application.Current as App)?.window;
      if (window == null)
        return;

      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

      // 记录初始鼠标位置
      GetCursorPos(out Point cursorPos);
      _dragStartPoint = new(cursorPos.X, cursorPos.Y);

      // 记录初始窗口位置
      if (GetWindowRect(hWnd, out RECT windowRect))
      {
        _windowStartPosition = new(windowRect.Left, windowRect.Top);
        _isDragging = true;
      }
    }

    /// <summary>
    /// 鼠标移动事件处理，更新窗口位置
    /// </summary>
    private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
      if (!_isDragging || _isPositionLocked)
        return;

      // 获取当前窗口句柄
      var window = (Application.Current as App)?.window;
      if (window == null)
        return;

      var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

      // 获取当前鼠标位置
      GetCursorPos(out Point cursorPos);

      // 计算位置偏移
      int offsetX = cursorPos.X - _dragStartPoint.X;
      int offsetY = cursorPos.Y - _dragStartPoint.Y;

      // 计算新的窗口位置
      int newX = _windowStartPosition.X + offsetX;
      int newY = _windowStartPosition.Y + offsetY;

      // 更新窗口位置
      SetWindowPos(hWnd, HWND_NOTOPMOST, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
    }

    /// <summary>
    /// 鼠标释放事件处理，结束拖动
    /// </summary>
    private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
      _isDragging = false;
    }

    /// <summary>
    /// 任务列表区域右键点击事件处理
    /// </summary>
    private void TasksScrollViewer_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
      // 显示右键菜单
      if (_contextMenu != null)
      {
        _contextMenu.ShowAt(TasksScrollViewer, e.GetPosition(TasksScrollViewer));
      }
    }

    /// <summary>
    /// 右键菜单添加任务事件处理
    /// </summary>
    private void AddTaskMenuItem_Click(object sender, RoutedEventArgs e)
    {
      // 使用当前选中的分类作为新任务分组
      string group = _currentCategory;
      if (group == "回收站")
      {
        group = "工作"; // 从回收站添加任务时，默认添加到工作分组
      }

      // 添加空标题新任务
      _dataManager.AddTask("", group, "");

      // 刷新任务列表
      RefreshTasks();

      // 更新统计信息
      UpdateBottomStats();
      UpdateCategoryTabsCounts();
    }
  }
}
