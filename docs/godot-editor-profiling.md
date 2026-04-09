# Godot 编辑器内 C# Profiling 方案

## 这个插件解决什么问题

你的实际工作流是：

1. 在 Godot 编辑器里改代码
2. 直接从编辑器点运行测试游戏
3. 希望对“跑起来的游戏”做 C# profiling

这和“分析编辑器自己卡不卡”是两回事。现在仓库里新增的编辑器插件，做的是：

- 从 Godot 编辑器菜单里触发
- 自动寻找“当前项目正在运行的 Godot 游戏进程”
- 用 `dotnet-trace` 按 PID 附加到那个游戏进程
- 把 trace 输出到 `artifacts/dotnet-diagnostics/`

它不会去分析 `--editor` 进程本身。

## 先启用插件

插件路径：

- `res://addons/dotnet_diagnostics_profiler/plugin.cfg`

在 Godot 编辑器里打开：

- `Project -> Project Settings -> Plugins`

把 `Dotnet Diagnostics Profiler` 打开。

## 使用方式

启用插件后，编辑器底部会出现一个 `Diagnostics` 面板，工具菜单里还会有：

- `Diagnostics: Open Process Panel`
- `Diagnostics: Open Profiling Docs`

推荐使用顺序：

1. 先在编辑器里点击运行游戏
2. 游戏窗口起来后，回到编辑器
3. 打开 `Diagnostics` 面板并点 `刷新进程`
4. 在列表里选中你要附加的进程
5. 选择采样时长，再点 `附加 Trace`

产物会写到：

- `artifacts/dotnet-diagnostics/`

## 面板里能看到什么

列表会展示当前项目相关的所有 Godot 进程，包括：

- `PID`
- `创建时间`
- `已运行多久`
- `类型`
  可能是 `编辑器`、`游戏`、`Headless`
- `编辑器启动`
  如果命令行里带 `--editor-pid`，这里会标 `是`
- `命令行`

这样你就可以明确区分：

- Godot 编辑器本身
- 编辑器启动的游戏子进程
- 旧的 headless smoke 进程

再由你自己决定附加到谁。

## 相关脚本

- `tools/profiling/Attach-RunningGodotTrace.ps1`
- `tools/profiling/Invoke-DotNetTraceSession.ps1`
- `tools/profiling/Invoke-DotNetGcDump.ps1`

其中：

- `Attach-RunningGodotTrace.ps1`
  面向“编辑器里已经跑起来的游戏进程”
- `Invoke-DotNetTraceSession.ps1`
  面向“直接由脚本启动 Godot 再附加采样”
- `Invoke-DotNetGcDump.ps1`
  面向“已有 PID，抓一份托管内存快照”

## 为什么不用 CodeTrack

CodeTrack 已经从仓库里移除。主要原因是：

- 旧
- GUI 手工步骤重
- 自动附加体验不稳定
- 不如官方 .NET 诊断工具适合当前这个项目

现在主推的方案是：

- `dotnet-trace` 查 CPU 热点
- `dotnet-gcdump` 查托管内存

## 一个现实限制

如果你的游戏进程启动后特别快就退出了，手工点选仍然可能来不及。这种情况更适合直接用 `Invoke-DotNetTraceSession.ps1` 脚本化启动并采样。
