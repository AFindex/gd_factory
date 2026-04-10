## Why

当前 diagnostics 插件已经能在 Godot 编辑器内附加 `dotnet-trace` 并产出 `trace.speedscope.json`，但第一轮定位仍然需要跳到浏览器或外部查看器里看 flamegraph。这打断了编辑器内的诊断闭环，也让“刚采完 trace 就快速判断 C# 热点”这件事仍然偏笨重，因此现在适合把查看能力直接补进现有插件工作流。

## What Changes

- 在 `addons/dotnet_diagnostics_profiler` 里新增独立的 Trace Viewer 页面或 tab，用于在 Godot 编辑器内浏览 `.NET` trace 的 flamegraph / icicle 视图。
- 让 Viewer 能直接加载 `artifacts/dotnet-diagnostics/` 下最新一份 `trace.speedscope.json`，并支持手动打开任意 `trace.speedscope.json` 文件重新解析。
- 增加 speedscope sampled profile 解析与聚合层，把 `shared.frames`、`profiles`、`samples`、`weights` 转换成可浏览的聚合调用树，而不是逐 sample 直接渲染。
- 以自绘或等价高效方案渲染 flamegraph，并提供 hover 高亮、tooltip、节点详情、放大子树、返回上级/重置根视图、多 profile 切换、滚动与窗口缩放适配。
- 在加载、解析、异常文件、可疑结果和空样本场景下提供明确反馈与基础统计，保持与现有“附加 trace”流程和状态文件约定一致。

## Capabilities

### New Capabilities
- `editor-dotnet-trace-viewer`: 在 Godot 编辑器内加载并浏览 `dotnet-trace --format Speedscope` 生成的 sampled trace，支持最新 trace 刷新、手动打开、profile 切换、聚合 flamegraph 浏览和错误反馈。

### Modified Capabilities
- None.

## Impact

- 受影响代码主要集中在 [addons/dotnet_diagnostics_profiler/plugin.gd](D:\Godot\projs\net-factory\addons\dotnet_diagnostics_profiler\plugin.gd)、新增的 viewer/parser 自绘脚本，以及 `tools/profiling` 和 `artifacts/dotnet-diagnostics/plugin-status` 的目录约定复用。
- 需要新增一份 OpenSpec capability，定义编辑器内 trace 加载、解析、渲染、交互、错误处理和 diagnostics 集成的行为契约。
- 不引入浏览器依赖或外部查看器作为第一轮定位前提，但会增加 Godot 编辑器插件侧的 UI、自绘和 JSON 解析复杂度。
