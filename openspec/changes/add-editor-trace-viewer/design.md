## Context

现有 `addons/dotnet_diagnostics_profiler/plugin.gd` 已经在 Godot 编辑器内提供进程列表、附加 trace 入口，以及基于 `artifacts/dotnet-diagnostics/plugin-status/*.json` 的轮询反馈。trace 完成后，状态文件里会记录 `session_dir`、`trace_path`、`speedscope_path`、`process_id` 和命令行，因此“跳到最新 trace 结果”所需的定位信息已经存在。

当前缺口不在采样入口，而在查看环节。仓库里已经持续生成 `trace.speedscope.json`，且实际文件包含 `shared.frames` 与多个 profile；其中常见 profile 类型是 sampled，也可能混有 evented profile。第一版要在 Godot 编辑器内完成 sampled profile 的加载、聚合和 flamegraph 浏览，同时继续明确区分“编辑器本身”和“编辑器启动的游戏进程”，避免让 Viewer 看起来像是在重新发起采样。

## Goals / Non-Goals

**Goals:**
- 在现有 diagnostics 插件里增加独立的 Trace Viewer 页面或 tab，而不是把结果只塞进弹窗文本区。
- 复用 `artifacts/dotnet-diagnostics` 与 `plugin-status` 的既有约定，支持“刷新最新 trace”与“手动打开文件”两条加载路径。
- 解析 speedscope JSON 的基础结构，并把 sampled profile 的 `samples` + `weights` 聚合成带累计权重的调用树。
- 使用适合大 trace 的自绘渲染方式提供 flamegraph / icicle 浏览、hover、tooltip、节点详情、子树放大、返回与重置。
- 在大文件、空样本、无效 JSON、结构不支持和可疑小文件场景下提供明确反馈与统计信息。

**Non-Goals:**
- 不在第一版支持 evented profile 的完整可视化，也不要求复刻 speedscope.app 的全部交互模式。
- 不支持多文件对比、实时流式读取正在写入中的 trace、导出图片或完整的 time-order 视图。
- 不修改 `dotnet-trace` 采样脚本的采样模型，也不把插件改造成通用性能分析器市场插件。

## Decisions

### 将 diagnostics 插件拆成“采样入口 + Viewer 页面”双区，而不是继续扩写单一状态文本面板

现有插件窗口已经有清晰的进程选择和附加 trace 工作流，但右侧状态区只适合展示文本结果，不适合承载可滚动 flamegraph、详情面板和 profile 切换。第一版将保留“附加 Trace”所在的诊断页，同时新增一个独立 Trace Viewer 页面或 tab，二者共享同一个插件窗口与状态上下文。

这样做能保留已有使用习惯，也能避免把“重新采样”和“查看现有 trace”混成一个面板。相比直接改成全新独立窗口，tab/page 模式更容易复用现有插件生命周期和状态轮询逻辑。

备选方案是 trace 完成后直接打开外部浏览器或嵌入 WebView；这与“在 Godot 编辑器内完成第一轮定位”的目标相冲突，而且会重新引入外部依赖，因此不采用。

### 加载路径优先复用 `plugin-status` 与 `artifacts/dotnet-diagnostics`，并将“最新 trace”定义为最新完成状态指向的有效 speedscope 文件

Viewer 的“刷新最新 trace”不会扫描任意目录猜测结果，而是优先读取 `artifacts/dotnet-diagnostics/plugin-status/*.json` 中最近完成且带 `speedscope_path` 的状态文件，再回落到 `artifacts/dotnet-diagnostics/**/trace.speedscope.json` 的最新文件时间排序。这样可以最大化复用现有 diagnostics 工作流，并保留“这是哪次 attach 产生的结果”的上下文。

手动打开则允许用户选择任意 `trace.speedscope.json` 重载，但仍会在 UI 中显示当前文件路径、profile 名称与类型，避免用户误以为自己正在看最新采样结果。

备选方案是让插件维护一份新的“latest trace cache”文件。这个方案会额外引入状态一致性问题，而且已有状态文件已经足够表达最近一次有效 trace，因此不采用。

### 为 sampled profile 建立中间聚合模型，而不是直接把原始 sample 映射为 UI 节点

Speedscope sampled profile 的原始数据重点是 `shared.frames`、`profiles[*].samples` 和 `profiles[*].weights`。第一版会在解析后构建统一中间模型：
- `TraceDocument`: 文件级元信息、共享 frame 表、profile 列表、警告/错误。
- `TraceProfileSummary`: profile 名称、类型、样本数、最大栈深度、是否受支持。
- `TraceTreeNode`: `frame_name`、完整名称、累计权重、直接子节点权重、父子关系、路径信息。

聚合时按每条 sample 的 frame index 栈路径，从根向叶累加权重；没有 `weights` 时使用权重 `1` 作为回退。这样可以稳定计算节点累计权重、相对占比、子节点摘要和当前根视图下的 flamegraph 宽度，同时避免“一条 sample 一个控件”导致的性能退化。

备选方案是直接把 sample 展平成矩形列表或生成成千上万的 `Control` 子节点。前者无法表达聚合热点，后者在大 trace 下会明显拖慢编辑器，因此都不采用。

### flamegraph 区使用自绘 `Control` + 命中测试缓存，而不是节点化 UI 树

渲染层会使用单个自定义 `Control` 的 `_draw()` 或等价自绘方案，按层级绘制当前根视图下的矩形块。每次布局完成后缓存一份“可见矩形 -> TraceTreeNode”命中表，用于 hover、tooltip、点击选中和放大子树。滚动能力通过把自绘控件放入 `ScrollContainer`，并基于最大栈深度动态设置自定义最小高度。

这种方案可以在窗口缩放时重新计算布局宽度，避免内容全部挤在左上角，也能只绘制当前可见树的矩形而不是创建大量节点。长函数名在块内只画截断文本，完整名交给 tooltip 和详情区展示。

备选方案是用 `GraphEdit`、`Tree` 或海量 `PanelContainer` 拼出 icicle 图。它们更快出原型，但对成百上千节点的性能和布局可控性较差，因此不采用。

### 详情区与图形区固定分区，并以“选中节点常驻详情”作为主信息通道

Viewer 主体将分成三块：
- 顶部工具栏：刷新最新 trace、打开文件、profile 切换、当前文件路径、加载状态。
- 中间主区域：左侧或上方为可滚动 flamegraph，自绘展示；右侧或下方为详情面板。
- 底部或详情顶部统计区：frame 数、sample 数、profile 数、最大栈深度、警告。

点击块后，详情区持续显示完整函数名、累计权重、相对占比、父节点、直接子节点摘要与当前缩放路径。Zoom in 会把当前选中节点设为新的可视根，Back/Reset 分别返回上一级或原始根视图。这样即便 tooltip 瞬时消失，用户仍能稳定阅读当前热点。

备选方案是只依赖 hover tooltip 展示详情。这个方案在 Godot 编辑器内可读性差，也不满足“详情区必须始终显示当前选中节点”的需求，因此不采用。

### 错误、可疑结果和不支持 profile 采用显式状态模型，而不是静默降级

加载流程会进入显式状态：`idle`、`loading`、`parsing`、`ready`、`warning`、`error`。当文件为空、JSON 非法、缺少 `shared.frames`、profile 结构不符合 sampled 预期、没有可用样本，或只包含 evented profile 时，Viewer 必须以错误或警告文案说明原因，而不是显示空白面板。

“可疑结果”第一版用启发式规则提示，例如：文件体积极小、frame 数极少、sample 数为零或极低、状态文件已标记 `completed_with_warning`。这些启发式只用于提示用户结果可能无效，不阻止继续浏览。

备选方案是遇到不支持 profile 时自动忽略并渲染空图。这个方案会让用户误以为插件无响应，因此不采用。

## Risks / Trade-offs

- [Godot GDScript 里解析大 JSON 可能造成短暂停顿] -> Mitigation: 明确显示 `正在加载/正在解析/正在构建视图`，并把聚合与布局步骤拆成可感知阶段。
- [自绘 flamegraph 的命中测试和文本裁剪逻辑容易出现边界 bug] -> Mitigation: 为最小宽度块、超长名称、缩放后返回路径和 hover/selection 优先级补充针对性测试样例。
- [多 profile 文件里混有 evented profile 可能让用户误解“切换 profile”为什么有的不可用] -> Mitigation: profile 下拉明确显示类型，并对不支持的项标记为不支持或显示只读错误态。
- [最新 trace 解析如果只依赖目录时间，可能选错文件] -> Mitigation: 先读 `plugin-status` 的最近完成记录，仅在缺失时再回退到目录扫描。
- [大窗口和小窗口都要兼容时，布局容易退化成左上角小块] -> Mitigation: 使用 `SplitContainer` / `ScrollContainer` 和明确的尺寸策略，确保图形区与详情区始终各自可伸缩。

## Migration Plan

1. 将现有 diagnostics 插件 UI 重组为可承载“进程/附加 trace”和“Trace Viewer”的多页面结构，并保留当前附加 trace 能力。
2. 新增 trace 定位与加载服务，复用 `plugin-status` 和 `artifacts/dotnet-diagnostics` 目录约定实现“刷新最新 trace”和“打开文件”。
3. 新增 speedscope sampled profile 解析与聚合模块，输出 Viewer 可消费的中间树模型和统计信息。
4. 新增自绘 flamegraph 控件、详情区与 profile 切换交互，并处理 hover、tooltip、选中、放大、返回、重置和滚动。
5. 为有效 trace、多 profile、可疑小文件、空样本和非法 JSON 增加插件级验证或场景化手工回归步骤。

回滚策略：如果 Viewer 的自绘或解析在短期内不稳定，可以保留 diagnostics 插件现有附加 trace 能力，并临时隐藏 Viewer 入口；状态文件与 trace 产物目录不会因此受损。

## Open Questions

- 第一版 profile 切换是否展示所有 profile 并对 evented 显示“不支持”，还是只列出 sampled profile？当前更倾向于全部展示并显式标注类型。
- “可疑结果”提示阈值是采用固定值，还是结合状态文件中的 `speedscope_size` / `trace_size` 动态判断？当前倾向于先用固定启发式，再辅以状态文件警告态。
- Viewer 页面最终放在同一个弹窗里的 tab，还是升级为 editor main screen 插件页面？当前设计优先沿用现有插件窗口，降低改动面。
