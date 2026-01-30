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

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinTodo.Services;

namespace WinTodo.Views
{
  /// <summary>
  /// 任务项控件，显示单个待办事项
  /// </summary>
  public sealed partial class TodoItem : UserControl
  {
    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event EventHandler<(int TaskId, bool Completed)>? StatusChanged;

    /// <summary>
    /// 标题编辑事件
    /// </summary>
    public event EventHandler<(int TaskId, string NewTitle)>? TitleEdited;

    /// <summary>
    /// 删除事件
    /// </summary>
    public event EventHandler<int>? DeleteSignal;

    /// <summary>
    /// 恢复事件
    /// </summary>
    public event EventHandler<int>? RestoreSignal;

    /// <summary>
    /// 永久删除事件
    /// </summary>
    public event EventHandler<int>? PermanentDeleteSignal;

    /// <summary>
    /// 优先级变更事件
    /// </summary>
    public event EventHandler<(int TaskId, int Priority)>? PriorityChanged;

    private TaskItem _task;

    /// <summary>
    /// 任务项构造函数
    /// </summary>
    /// <param name="task">任务数据</param>
    public TodoItem(TaskItem task)
    {
      this.InitializeComponent();
      _task = task;
      InitializeTaskItem();
      AddRightClickMenu();
    }

    /// <summary>
    /// 初始化任务项
    /// </summary>
    private void InitializeTaskItem()
    {
      // 设置状态图标
      if (_task.Completed)
      {
        StatusIcon.Text = "✅";
      }
      else
      {
        // 根据优先级设置不同图标：0=一般(⚪)，1=重要(🔵)，2=紧急(🔴)
        switch (_task.Priority)
        {
          case 2:
            StatusIcon.Text = "🔴";
            break;
          case 1:
            StatusIcon.Text = "🔵";
            break;
          default: // 0或其他值
            StatusIcon.Text = "⚪";
            break;
        }
      }

      // 设置标题
      string title = string.IsNullOrEmpty(_task.Title) ? "双击编辑任务内容" : _task.Title;
      TitleLabel.Text = title;
      TitleEdit.Text = _task.Title;

      // 设置样式
      UpdateTitleStyle();
    }

    /// <summary>
    /// 更新标题样式
    /// </summary>
    private void UpdateTitleStyle()
    {
      if (_task.Completed)
      {
        // 添加删除线
        TitleLabel.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        // 在WinUI 3中，TextDecorations通过XAML设置，这里我们可以通过修改字体样式来实现
        var font = new FontFamily("Microsoft YaHei UI");
        var fontWeight = FontWeights.Normal;
        var fontSize = 14.0;
        TitleLabel.FontFamily = font;
        TitleLabel.FontWeight = fontWeight;
        TitleLabel.FontSize = fontSize;
      }
      else if (string.IsNullOrEmpty(_task.Title))
      {
        TitleLabel.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
      }
      else
      {
        TitleLabel.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
      }
    }

    /// <summary>
    /// 添加右键菜单
    /// </summary>
    private void AddRightClickMenu()
    {
      // 创建上下文菜单
      MenuFlyout menuFlyout = new();

      if (_task.IsDeleted)
      {
        // 回收站中的任务菜单
        MenuFlyoutItem restoreItem = new MenuFlyoutItem { Text = "恢复任务" };
        restoreItem.Click += (sender, e) => RestoreSignal?.Invoke(this, _task.Id);
        menuFlyout.Items.Add(restoreItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem permanentDeleteItem = new MenuFlyoutItem { Text = "永久删除" };
        permanentDeleteItem.Click += (sender, e) => PermanentDeleteSignal?.Invoke(this, _task.Id);
        menuFlyout.Items.Add(permanentDeleteItem);
      }
      else if (string.IsNullOrEmpty(_task.Title))
      {
        // 空任务菜单，只显示删除选项
        MenuFlyoutItem permanentDeleteItem = new() { Text = "永久删除" };
        permanentDeleteItem.Click += (sender, e) => PermanentDeleteSignal?.Invoke(this, _task.Id);
        menuFlyout.Items.Add(permanentDeleteItem);
      }
      else
      {
        // 普通任务菜单
        if (_task.Completed)
        {
          MenuFlyoutItem markUncompletedItem = new() { Text = "标记为未完成" };
          markUncompletedItem.Click += (sender, e) => StatusChanged?.Invoke(this, (_task.Id, false));
          menuFlyout.Items.Add(markUncompletedItem);
        }
        else
        {
          MenuFlyoutItem markCompletedItem = new()
          {
            Text = "标记为已完成"
          };
          markCompletedItem.Click += (sender, e) => StatusChanged?.Invoke(this, (_task.Id, true));
          menuFlyout.Items.Add(markCompletedItem);

          // 紧急度设置选项
          menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // 恢复任务菜单项 标记为紧急
          MenuFlyoutItem markUrgentItem = new() { Text = "标记为紧急" };
          markUrgentItem.Click += (sender, e) => PriorityChanged?.Invoke(this, (_task.Id, 2));
          menuFlyout.Items.Add(markUrgentItem);

          // 标记为重要
          MenuFlyoutItem markImportantItem = new() { Text = "标记为重要" };
          markImportantItem.Click += (sender, e) => PriorityChanged?.Invoke(this, (_task.Id, 1));
          menuFlyout.Items.Add(markImportantItem);

          // 标记为一般
          MenuFlyoutItem markNormalItem = new() { Text = "标记为一般" };
          markNormalItem.Click += (sender, e) => PriorityChanged?.Invoke(this, (_task.Id, 0));
          menuFlyout.Items.Add(markNormalItem);
        }

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem deleteItem = new() { Text = "删除到回收站" };
        deleteItem.Click += (sender, e) => DeleteSignal?.Invoke(this, _task.Id);
        menuFlyout.Items.Add(deleteItem);
        }

        // 为控件添加上下文菜单
        this.ContextFlyout = menuFlyout;
      }

    /// <summary>
    /// 标题双击事件，进入编辑模式
    /// </summary>
    private void TitleLabel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
      // 已完成或已删除的任务不允许编辑
      if (_task.Completed || _task.IsDeleted)
      {
        return;
      }

      StartEdit();
    }

    /// <summary>
    /// 开始编辑
    /// </summary>
    private void StartEdit()
    {
      TitleLabel.Visibility = Visibility.Collapsed;
      TitleEdit.Visibility = Visibility.Visible;

      // 延迟设置焦点，确保输入框已经可见
      Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
      {
        TitleEdit.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        TitleEdit.SelectAll();
      });
    }

    /// <summary>
    /// 编辑框失焦事件，保存编辑内容
    /// </summary>
    private void TitleEdit_LostFocus(object sender, RoutedEventArgs e)
    {
      OnEditFinished();
    }

    /// <summary>
    /// 编辑完成，保存内容
    /// </summary>
    private void OnEditFinished()
    {
      string newTitle = TitleEdit.Text.Trim();
      TitleEdited?.Invoke(this, (_task.Id, newTitle));

      // 更新标签显示
      _task.Title = newTitle;
      TitleLabel.Text = string.IsNullOrEmpty(newTitle) ? "双击编辑任务内容" : newTitle;
      TitleEdit.Text = newTitle;

      // 退出编辑模式
      TitleEdit.Visibility = Visibility.Collapsed;
      TitleLabel.Visibility = Visibility.Visible;

      // 更新样式
      UpdateTitleStyle();

      // 显示编辑成功的反馈
      ShowEditFeedback();
    }

    /// <summary>
    /// 显示编辑成功的反馈
    /// </summary>
    private void ShowEditFeedback()
    {
      // 保存原始背景
      var originalBackground = this.Background;

      // 设置淡绿色背景反馈
      this.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen);

      // 500ms后恢复原始背景
      var timer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500)
      };
      timer.Tick += (sender, e) =>
      {
        timer.Stop();
        this.Background = originalBackground;
      };
      timer.Start();
    }

    /// <summary>
    /// 编辑框文本变化事件
    /// </summary>
    private void TitleEdit_TextChanged(object sender, TextChangedEventArgs e)
    {
      // 实时更新任务标题
      _task.Title = TitleEdit.Text;
    }

    /// <summary>
    /// 编辑框键盘事件，处理回车键保存
    /// </summary>
    private void TitleEdit_KeyDown(object sender, KeyRoutedEventArgs e)
    {
      // 按下回车键时保存编辑内容
      if (e.Key == Windows.System.VirtualKey.Enter)
      {
        OnEditFinished();
      }
    }

    /// <summary>
    /// 公共方法：开始编辑任务
    /// </summary>
    public void BeginEdit()
    {
      StartEdit();
    }

    /// <summary>
    /// 鼠标进入事件处理程序，实现悬停效果
    /// </summary>
    private void ItemGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
      // 使用预定义的接近颜色或使用ColorHelper
      ItemGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
    }

    /// <summary>
    /// 鼠标离开事件处理程序，恢复原始状态
    /// </summary>
    private void ItemGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
      ItemGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
  }
}

