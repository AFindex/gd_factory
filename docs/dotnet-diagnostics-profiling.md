# Godot C# 诊断方案

## 为什么换掉 CodeTrack

这个项目是 Godot 4.6.1 + .NET 8。对这类项目，`CodeTrack 1.0.3.3` 有两个现实问题：

- 它是较老的 GUI 工具，脚本化能力弱，自动附加和自动录制不顺。
- 它不是 Godot 官方或 .NET 官方现在主推的链路，维护感也比较弱。

更适合当前项目的方案，是使用官方 .NET 诊断工具：

- `dotnet-trace`：抓 CPU 热点和调用栈
- `dotnet-gcdump`：抓内存快照

## 官方依据

- Godot 官方文档明确写了：内置 Profiler 目前不支持 C# 脚本；对 C# 建议使用 Rider + dotTrace 一类外部工具。
- .NET 官方文档说明 `dotnet-trace collect` 支持按进程采样，这条链路适合拿来附加 Godot 运行中的 .NET 宿主。
- .NET 官方文档也说明了 `dotnet-gcdump` 的标准用法。

参考：

- [Godot Profiler 文档](https://docs.godotengine.org/zh-cn/4.x/tutorials/scripting/debug/the_profiler.html)
- [dotnet-trace 文档](https://learn.microsoft.com/zh-cn/dotnet/core/diagnostics/dotnet-trace)
- [dotnet-gcdump 文档](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-gcdump)

## 仓库内已经接好的脚本

- `tools/profiling/Invoke-DotNetTraceSession.ps1`
- `tools/profiling/Attach-RunningGodotTrace.ps1`
- `tools/profiling/Invoke-DotNetGcDump.ps1`

同时，仓库已经加入本地工具清单：

- `.config/dotnet-tools.json`

这样工具版本会跟仓库走，不需要你自己记全局安装状态。

如果你主要是“从 Godot 编辑器里点运行，然后再附加采样”，请优先看 [godot-editor-profiling.md](D:/Godot/projs/net-factory/docs/godot-editor-profiling.md)。

## 最推荐的用法

### 1. 抓 CPU 热点

```powershell
.\tools\profiling\Invoke-DotNetTraceSession.ps1 -Preset factory-demo
```

这个脚本会：

- 自动做 `Debug` 构建
- 自动 `dotnet tool restore`
- 先启动 Godot
- 再按 PID 附加 `dotnet-trace`
- 把结果写到 `artifacts/dotnet-diagnostics/...`

默认 profile 是：

```text
dotnet-common,dotnet-sampled-thread-time
```

这比旧的 `cpu-sampling` 命名更符合 .NET 9 当前官方文档。

### 2. 抓内存快照

先拿到 Godot 进程 PID，然后运行：

```powershell
.\tools\profiling\Invoke-DotNetGcDump.ps1 -ProcessId 12345
```

适合排查：

- 某类对象越跑越多
- 怀疑有缓存/集合没释放
- 某场景切换后内存没降下来

## 结果怎么分析

`dotnet-trace` 的输出默认会保留 `.nettrace`，如果你选了 `Speedscope` 或 `Chromium`，还会额外生成对应格式的文件。

常见分析方式：

- `.nettrace`：用 Visual Studio 或 PerfView 打开
- `speedscope`：用火焰图工具查看热点

如果你只是想先看一个文本版热点列表，也可以对 `.nettrace` 跑：

```powershell
dotnet dotnet-trace report .\trace.nettrace topN
```

## 当前验证结果

在这台机器上，我已经实际验证通过：

- `dotnet-trace` 先启动 Godot，再按 PID 附加，能够稳定生成 `.nettrace`
- `Speedscope` 转换也能正常产出

我也试过 `dotnet-counters 9.0.661903` 去附加当前 Godot 4.6.1 Mono 宿主，但它在这里会直接抛 `NullReferenceException`。所以当前仓库主推方案先收敛为：

1. `dotnet-trace` 负责 CPU 热点
2. `dotnet-gcdump` 负责内存快照
3. Godot 内置 Profiler 继续看帧、渲染、物理等引擎级信息

这套比 CodeTrack 更现代，也更容易做成稳定的一键脚本。
