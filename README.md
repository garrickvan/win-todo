# WinTodo

WinTodo是一个简洁、高效的Windows待办事项应用程序，帮助您管理日常任务和提高工作效率。

## 功能特性

- ✅ 简洁美观的用户界面
- ✅ 支持任务的添加、编辑、删除和完成标记
- ✅ 任务分类和优先级管理
- ✅ 系统托盘集成，方便快速访问
- ✅ 数据本地存储，保护隐私
- ✅ 支持快捷键操作
- ✅ 可调整窗口大小和位置

## 应用截图

![WinTodo截图](Assets/Screenshot.png)

## 系统要求

- Windows 10 版本 17763.0 或更高版本，或 Windows 11
- .NET 6.0 或更高版本
- Visual Studio 2022 版本 17.3 或更高版本
- 已安装以下 Visual Studio 工作负载：
  - 通用 Windows 平台开发
  - .NET 桌面开发
  - Windows App SDK C# 模板

## 安装方法

1. 克隆或下载项目源代码
2. 使用Visual Studio 2022打开解决方案文件 `WinTodo.slnx`
3. 恢复 NuGet 包：右键点击解决方案 -> 恢复 NuGet 包
4. 选择目标平台（x86、x64 或 ARM64）
5. 编译项目：按 F7 或选择 "生成" -> "生成解决方案"
6. 运行项目：按 F5 或选择 "调试" -> "开始调试"

## 构建命令行

如果您喜欢使用命令行构建项目，可以使用以下命令：

```powershell
# 恢复 NuGet 包
dotnet restore

# 构建项目（默认配置为 Debug，平台为 x64）
dotnet build

# 运行项目
dotnet run

# 发布项目（示例：发布为 x64 平台的 Release 版本）
dotnet publish -c Release -p:Platform=x64
```

## 使用说明

### 添加任务

1. 点击"添加任务"按钮或使用快捷键Ctrl+N
2. 在输入框中输入任务内容
3. 设置任务的优先级和分类（可选）
4. 点击"保存"按钮或按Enter键保存任务

### 编辑任务

1. 双击要编辑的任务
2. 修改任务内容、优先级或分类
3. 点击"保存"按钮或按Enter键保存修改

### 删除任务

1. 选中要删除的任务
2. 点击"删除"按钮或使用快捷键Delete
3. 在确认对话框中点击"确定"删除任务

### 标记任务完成

1. 点击任务左侧的复选框
2. 完成的任务将显示为已勾选状态

### 系统托盘功能

- 点击系统托盘图标可以显示/隐藏主窗口
- 右键点击系统托盘图标可以打开快捷菜单

## 快捷键

- Ctrl+N: 添加新任务
- Delete: 删除选中任务
- Ctrl+S: 保存当前修改
- Ctrl+Q: 退出应用程序

## 项目结构

```
WinTodo/
├── Assets/           # 资源文件
├── Data/             # 数据存储
├── image/            # 图片资源
├── Properties/       # 项目属性
├── UI/               # 用户界面组件
├── Views/            # 视图文件
├── App.xaml          # 应用程序入口XAML
├── App.xaml.cs       # 应用程序入口代码
├── WinTodo.csproj    # 项目文件
└── WinTodo.slnx      # 解决方案文件
```

## 开发环境

- Visual Studio 2022
- .NET Framework 4.8
- WPF (Windows Presentation Foundation)

## 贡献指南

欢迎大家贡献代码！如果您想为项目做出贡献，请遵循以下步骤：

1. Fork项目仓库
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启一个Pull Request

## 许可证

本项目采用MIT许可证 - 查看[LICENSE](LICENSE)文件了解详情

## 联系方式

如有问题或建议，请通过以下方式联系：

- GitHub Issues: [提交问题](https://github.com/yourusername/WinTodo/issues)

## 更新日志

### v1.0.0 (2026-01-23)

- 初始版本发布
- 实现基本的任务管理功能
- 添加系统托盘集成
- 支持任务优先级和分类

## 致谢

感谢所有为项目做出贡献的开发者和测试人员！
