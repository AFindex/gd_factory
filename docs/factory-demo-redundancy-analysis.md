# FactoryDemo / MobileFactoryDemo 冗余分析与复用重构大纲

## 文件清单

| 文件 | 行数 | 职责 |
|------|------|------|
| `FactoryDemo.cs` | 2389 | 静态工厂沙盒主控制器 |
| `FactoryDemo.MapLoading.cs` | 72 | 世界地图加载与应力测试段 |
| `FactoryDemo.Persistence.cs` | 261 | 蓝图/地图/进度存取档 |
| `MobileFactoryDemo.cs` | ~5000+ | 移动工厂主控制器（世界+内部双场景） |
| `MobileFactoryDemo.MapLoading.cs` | 52 | 世界地图+内部地图加载 |
| `MobileFactoryDemo.Persistence.cs` | 452 | 蓝图/地图/进度存取档（世界+内部） |
| `MobileFactoryDemo.Combat.cs` | 179 | 战斗场景配置 |
| `FactoryDemoRuntimeSupport.cs` | 215 | 场景脚手架/原始组件（两者共用） |
| `FactoryDemoInteractionBridge.cs` | 183 | 库存交互桥接（两者共用） |

---

## 一级冗余：完全相同的代码块（可直接抽取为共享基类/工具）

### 1. 场景脚手架构建 (`BuildSceneGraph`)
两个文件的 `BuildSceneGraph()` 结构几乎完全一致：
- 调用 `FactoryDemoSceneScaffold.Build()` 传入 `rootSpecs[]`
- 从 scaffold 提取各根节点
- 创建 `FactoryTransportRenderManager`（FD）或跳过（MFD）
- 创建预览视觉
- 创建 HUD 并绑定大量事件
- 绑定 `PlayerHud` 事件

**复用方案**：抽取 `DemoSceneInitializer` 静态类，接收 `rootSpecs` + HUD 工厂 + 可选传输渲染器，返回 `ScaffoldInitResult`。

### 2. 玩家生成 (`SpawnPlayerController`)
两者 `SpawnPlayerController()` 几乎逐行相同：
- `new FactoryPlayerController()` → AddChild → GlobalPosition
- `EnsureStarterLoadout` / `SelectHotbarIndex(0)`
- `SetFollowTarget` / `FollowTargetEnabled = true`
- `_playerPlacementState.SetSelectedSlot(...)`

**复用方案**：抽取 `FactoryPlayerSpawner.SpawnPlayerInWorld()` 静态方法。

### 3. 玩家出生点搜索 (`FindPlayerSpawnPosition`)
两者均以螺旋搜索从 preferred cell 向外找空位，代码结构完全相同（半径循环→Y 循环→X 循环→IsInBounds + TryGetStructure 检查）。

**复用方案**：抽取 `FactoryPlayerSpawner.FindSpawnPosition(GridManager, Vector2I preferred, int maxRadius)`。

### 4. 玩家移动范围 (`GetPlayerMovementBounds`)
两者完全相同：取 grid 的 WorldMin/Max 各加 1.0f 边距。

**复用方案**：移至 `FactoryPlayerSpawner` 或 `GridManager` 扩展方法。

### 5. 预览底层基础组件
以下方法在两者中完全相同且均为 static：
- `UpdatePreviewPowerRange(BuildPrototypeKind?, IFactorySite, MeshInstance3D, Color)`
- `TryGetPowerPreviewInfo(BuildPrototypeKind?, out int)`
- `ApplyPowerLinkColor(MeshInstance3D, Color)`
- `GetPowerAnchor(FactoryStructure)` — 同一 switch 表

**复用方案**：这些已经是 static helper，可直接移入 `FactoryPowerPreviewSupport` 或专用共享工具类。当前它们分散在两个 partial class 中。

### 6. SmoothMetric
```csharp
private static double SmoothMetric(double current, double sample, double weight)
```
两者完全相同。

**复用方案**：移至 `FactoryMath` 或 `FactoryMetrics` 工具类。

### 7. BuildCellRect
```csharp
private static Rect2I BuildCellRect(Vector2I a, Vector2I b, int padding = 0)
```
FD 有，MFD 没有但直接用了内联逻辑。完全相同的辅助方法。

**复用方案**：移至 `FactoryGridUtility`。

### 8. 热键映射 (`TryMapHotbarKey`)
`Key.Key1~Key0 → 0~9` 的 switch 映射完全相同。

**复用方案**：移至 `FactoryInputUtility`。

### 9. `IsPointerOverUi` / `IsInventoryUiInteractionActive` / `IsWorldPointerInputBlocked`
FD 有三个方法，MFD 用 `FactoryBaselineInteractionRules.AllowsWorldPointerInput()` 封装。FD 也应统一使用 `FactoryBaselineInteractionRules`。

### 10. 供电连接预览框架
FD 的 `RenderPowerLinkSet` / `DrawDashedPowerLink` / `EnsurePowerLinkDashCapacity` / `SetPowerLinkDashCount` 四个方法与 MFD 的 `RenderInteriorPowerLinkSet` / `DrawInteriorDashedPowerLink` 四个方法模式完全相同，仅根节点和 dash 列表不同。

**复用方案**：`FactoryPowerPreviewSupport.RenderPowerLinkSet` 已是共享静态方法，接收委托参数。两个 demo 中的薄包装方法可以统一为一个泛型包装模板。

### 11. ShouldShowSelectionRange / IsPowerPreviewActive
FD 和 MFD 的 `ShouldShowSelectionRange` / `ShouldShowInteriorSelectionRange` 和对应的 `IsPowerPreviewActive` / `IsInteriorPowerPreviewActive` 逻辑结构完全一致（检查 Build 模式下的 PowerPole/Generator，或 Interact 模式下的 IFactoryPowerNode）。

---

## 二级冗余：概念相同但上下文不同的重复（可通过参数化统一）

### 12. 蓝图工作流（Capture + Apply + Cancel）
FD 有一套完整的蓝图生命周期（世界网格上），MFD 有两套（世界浅层 + 内部深层），代码模式高度相似：

| FD | MFD 世界 | MFD 内部 |
|---|---|---|
| `StartBlueprintCapture` | - | `StartInteriorBlueprintCapture` |
| `BeginBlueprintSelection` | - | `BeginInteriorBlueprintSelection` |
| `CompleteBlueprintSelection` | - | `CompleteInteriorBlueprintSelection` |
| `EnterBlueprintApplyMode` | `EnterWorldBlueprintApplyMode` | `EnterInteriorBlueprintApplyMode` |
| `ConfirmBlueprintApply` | `ConfirmWorldBlueprintApply` | `ConfirmInteriorBlueprintApply` |
| `CancelBlueprintWorkflow` | `CancelBlueprintWorkflow` | `CancelInteriorBlueprintWorkflow` |
| `RotateBlueprintApplyPreview` | `RotateWorldBlueprintPreview` | `RotateInteriorBlueprintPreview` |
| `HandleBlueprintSaveRequested` | - | `HandleInteriorBlueprintSaveRequested` |
| `HandleBlueprintSelected` | 共用 | 共用 `_activeBlueprintSiteKind` 路由 |
| `HandleBlueprintDeleteRequested` | - | `HandleInteriorBlueprintDeleteRequested` |
| `BuildBlueprintPanelState` | `BuildWorldBlueprintPanelState` | `BuildInteriorBlueprintPanelState` |

**复用方案**：创建一个 `BlueprintWorkflowContext` 结构体封装 site adapter / rotation ref / plan ref / mode ref / preview root / ghost root / meshes / ghosts，然后用一个 `BlueprintWorkflowRunner` 类统一执行所有 blueprint 操作。

### 13. 建造拖拽放置系统
FD 的 `HandleBuildDragPlacement` / `TryPlaceCurrentBuildTarget` / `ResetBuildPlacementStroke` / `ResolveWorldPlacementFacing` / `ReorientBeltAt` 与 MFD 内部的 `HandleInteriorBuildDragPlacement` / `TryPlaceCurrentInteriorTarget` / 等完全对应。

**复用方案**：抽取 `BuildDragController` 类，接收 `IFactorySite` + 放置/验证/消耗委托。

### 14. 删除拖拽框选系统
FD 的 `HandleDeletePrimaryPress/Release` / `GetDeleteRect` / `CountStructuresInDeleteRect` / `DeleteStructuresInRect` 与 MFD 的 `HandleEditorDeletePrimaryPress/Release` / 对应的 `CountInteriorStructuresInDeleteRect` 完全对应。

MFD 另外还有世界的删除系统（`HandleWorldDeletePrimaryPress/Release`）。

**复用方案**：抽取 `DeleteDragController`，接收 site + 删除委托。

### 15. 蓝图预览池管理
FD 的 `EnsureBlueprintPreviewCapacity` / `EnsureBlueprintGhostPreview` / `SupportsGhostBlueprintPreview` 与 MFD 的 `EnsureWorldBlueprintPreviewCapacity` / `EnsureInteriorBlueprintPreviewCapacity` 及其 ghost 版本完全对应。

**复用方案**：统一为一个 `BlueprintPreviewPool` 泛型类，管理 mesh + ghost 的容量确保逻辑。

### 16. 蓝图 Site Adapter 创建
FD 创建 1 个，MFD 创建 2 个（世界 + 内部），创建模式完全相同：`new FactoryBlueprintSiteAdapter(siteKind, siteId, ...)`，带 validate / place / remove 委托。

**复用方案**：`FactoryBlueprintSiteAdapterFactory.CreateForGrid/ForMobileFactory`。

### 17. 蓝图预览渲染
FD 的 `UpdateBlueprintPreview` 与 MFD 的 `UpdateWorldBlueprintPreview` / `UpdateInteriorBlueprintPreview` 有相同的循环结构：遍历 Entries，设置 mesh 位置/旋转/颜色，管理 ghost preview。

**复用方案**：抽取 `BlueprintPreviewRenderer.Update(site, plan, root, ghostRoot, meshes, ghosts)`。

### 18. 资源浮层重建
FD 的 `RebuildResourceOverlayVisuals` 和 MFD 的 `RebuildMobileResourceOverlayVisuals` 本质上是同样的 `FactoryMapVisualSupport.RebuildResourceOverlay()` 调用，仅参数不同（前缀名、几何参数）。

**复用方案**：已经是薄包装，可进一步抽取为 `FactoryMapVisualSupport.RebuildResourceOverlayWithDefaults(grid, root, prefix)`。

### 19. HUD 事件绑定
`BuildSceneGraph` 中 `_hud` 的事件订阅列表非常长（FD 约 20+ 个订阅，MFD 约 40+ 个订阅）。大量事件名称和委托签名重叠。

**复用方案**：`HudEventBinder` 类分阶段绑定通用事件（库存操作、蓝图操作、存档操作），只留特定事件由各自 demo 绑定。

### 20. UpdateHud 模式
FD 和 MFD 的 `UpdateHud()` 结构相似：
1. 处理蓝图 workspace 自动切换
2. 构建 `FactoryBaselineHudProjectionBuilder.Create()`
3. 处理资源/建筑 detail 信息
4. `FactoryBaselineHudApplicator.Apply(...)`
5. 设置状态统计（FPS, Sink, Combat, Transport）
6. 设置蓝图面板

但 MFD 还需要额外设置编辑模式、世界选择、交付统计、port 状态等。

---

## 三级冗余：MFD 内部的世界↔内部双重维护

MFD 自身在 "世界侧" 和 "内部侧" 之间存在大量对称副本：

### 21. 交互模式三态切换
世界侧：`EnterWorldInteractionMode` / `EnterWorldDeleteMode` / `CancelPlayerWorldPlacement`
内部侧：`EnterInteriorInteractionMode` / `EnterInteriorDeleteMode` / `EnterInteriorBuildMode`

模式切换逻辑相同，但状态变量不同。

### 22. 悬停格更新
世界侧：`UpdateHoveredWorldCell()` — 射线投影 → cell → 检查 structure/deposit → 处理蓝图/placement/delete
内部侧：`UpdateHoveredInteriorCell()` — 编辑器射线投影 → cell → 检查 structure → 处理蓝图/placement/delete

结构几乎相同，约 60% 重复。

### 23. 预览更新
世界侧：`UpdateWorldPreview()` — 放置预览、删除预览、选择预览、部署预览、采矿预览
内部侧：`UpdateInteriorPreview()` — 放置预览、删除预览、蓝图框选预览、边界附着预览

结构相同但细节不同（世界有采矿/部署，内部有边界附着）。

### 24. 建筑验证
世界侧：`TryValidateWorldPlacement` / `TryValidatePlayerWorldPlacement` — 调用 `_grid.CanPlaceStructure()`
内部侧：`TryValidateInteriorPlacement` — 调用 `_mobileFactory.CanPlaceInteriorStructure()`

验证逻辑可通过 `IFactorySite` 统一。

### 25. 建筑放置
世界侧：`PlaceStructure` / `PlaceWorldStructure`
内部侧：`_mobileFactory.PlaceInteriorStructure`

### 26. 建筑移除
世界侧：`RemoveStructure` / `RemoveWorldStructure`
内部侧：`RemoveInteriorStructure` / `RemoveAllInteriorStructuresInRect`

### 27. 朝向渲染
世界侧：`ResolveWorldPlacementFacing` / `ResolvePlayerWorldPlacementFacing` — Belt 拖拽朝向 + 自动连接
内部侧：`ResolveInteriorPlacementFacing` / `ReorientInteriorBeltAt` — 完全相同的算法

---

## 复用重构总路线图

### 阶段 A：底层工具抽取（低风险，高收益）

| # | 新文件/类 | 合并来源 | 影响 |
|---|----------|---------|------|
| A1 | `FactoryPlayerSpawner` (static) | FD + MFD 的 `SpawnPlayerController`, `FindPlayerSpawnPosition`, `GetPlayerMovementBounds` | 消除 ~100 行重复 |
| A2 | `FactoryMetrics` (static) | FD + MFD 的 `SmoothMetric` | 消除 2 份副本 |
| A3 | `FactoryGridUtility.BuildCellRect` (static) | FD 的 `BuildCellRect` + MFD 内联逻辑 | 统一网格工具 |
| A4 | `FactoryInputUtility.TryMapHotbarKey` (static) | FD + MFD 的 `TryMapHotbarKey` | 消除 2 份副本 |
| A5 | 统一 `FactoryBaselineInteractionRules` | FD 改用 `AllowsWorldPointerInput` 替代 `IsWorldPointerInputBlocked` | 消除 3 个方法 |
| A6 | `FactoryPowerPreviewHelper` | FD + MFD 的 `UpdatePreviewPowerRange`, `TryGetPowerPreviewInfo`, `ApplyPowerLinkColor`, `GetPowerAnchor`, `GetPreviewPowerAnchor` | 消除 ~50 行重复 |

### 阶段 B：交互控制器抽取（中等风险，中收益）

| # | 新文件/类 | 合并来源 | 影响 |
|---|----------|---------|------|
| B1 | `BuildDragController` | FD `HandleBuildDragPlacement` + MFD `HandleInteriorBuildDragPlacement` + `HandlePlayerWorldBuildDragPlacement` | 消除 ~150 行重复 |
| B2 | `DeleteDragController` | FD `HandleDeletePrimaryPress/Release` + MFD 两套删除系统 | 消除 ~120 行重复 |
| B3 | `InteractionModeController` | 三态切换（Interact/Build/Delete）+ 状态变量管理 | 统一交互模式生命周期 |
| B4 | `SelectionController` | FD 的 `HandlePrimaryClick/SecondaryClick` + MFD 的 `HandleEditorPrimaryClick/SecondaryClick` + `HandleWorldPrimaryClick/SecondaryClick` | 消除点击处理重复 |

### 阶段 C：蓝图工作流统一（中等风险，高收益）

| # | 新文件/类 | 合并来源 | 影响 |
|---|----------|---------|------|
| C1 | `BlueprintWorkflowContext` (struct) | 封装 site adapter, rotation, plan, mode, meshes, ghosts 等 | 数据结构统一 |
| C2 | `BlueprintWorkflowRunner` | FD 全量 + MFD 世界+内部三套 blueprint 操作 | 消除 ~300 行重复 |
| C3 | `BlueprintPreviewRenderer` | `UpdateBlueprintPreview` × 3 | 消除 ~200 行重复 |
| C4 | `BlueprintPreviewPool` | `EnsureBlueprintPreviewCapacity` × 3 + ghost × 3 | 消除 ~150 行重复 |

### 阶段 D：存档系统统一（低风险，中收益）

| # | 新文件/类 | 合并来源 | 影响 |
|---|----------|---------|------|
| D1 | `RuntimeSaveController` | FD 和 MFD 的 `SaveRuntimeSnapshot` / `LoadRuntimeSnapshot` 公共流程 | 约 60% 重复，保留各自站点差异 |
| D2 | 统一 `TearDownRuntimeSession` | FD 和 MFD 的结构拆除逻辑 | 前 8 行完全相同 |

### 阶段 E：HUD 绑定模式统一（低风险，低收益）

| # | 改进 | 说明 |
|---|------|------|
| E1 | `HudEventBinder` | 按阶段批量绑定事件，减少 `BuildSceneGraph` 中的样板代码 |

---

## 量化估算

| 类别 | 当前总行数（两套） | 可消除重复 | 重复率 |
|------|-------------------|-----------|--------|
| 蓝图系统 | ~600 行 | ~300 行 | 50% |
| 建造/删除拖拽 | ~400 行 | ~220 行 | 55% |
| 预览系统 | ~500 行 | ~200 行 | 40% |
| 玩家生成 | ~120 行 | ~80 行 | 67% |
| 供电链接预览 | ~150 行 | ~60 行 | 40% |
| 存档持久化 | ~400 行 | ~150 行 | 38% |
| 工具方法 | ~100 行 | ~80 行 | 80% |
| HUD 事件绑定 | ~150 行 | ~50 行 | 33% |
| **合计** | **~2420 行** | **~1140 行** | **~47%** |

---

## 实施优先级建议

1. **P0（立即）**：阶段 A — 纯工具抽取，零风险，立刻减少 ~400 行
2. **P1（下一步）**：阶段 C — 蓝图系统统一，改善最显著的重复区域
3. **P2（后续）**：阶段 B — 交互控制器，需更多设计工作但收益大
4. **P3（可选）**：阶段 D + E — 存档和 HUD 优化

## 架构原则

- **IFactorySite 接口**（已存在）是统一世界/内部操作的关键抽象，所有放置/验证/删除操作应通过它进行
- **委托注入**：交互控制器通过委托接收 site-specific 行为，避免硬编码对 FD/MFD 的依赖
- **partial class 保持**：FD/MFD 的 partial class 拆分策略保留，仅将通用逻辑提升到共享层

---

## 附：项目整体代码健康度评估

> 当前项目总代码量 ~57,618 行（含 ~10,000 行冒烟测试），对于仍在玩法探索阶段的独游项目，**偏不健康**。以下是三个主要问题。

### 问题一：过度架构

**现象**：

- 大量的 C# 接口抽象：`IFactorySite`、`IFactoryPowerNode`、`IFactoryInspectable`、`IFactoryCombatSystem`、`IFactoryInventoryEndpointProvider`、`IFactoryStructureDetailProvider` 等
- 多层委托链：`FactoryBaselineHudProjectionBuilder` → `FactoryBaselineHudApplicator` → `FactoryBaselineInteractionRules`，数据在三四个静态类之间传递
- 每个 demo 用 partial class 拆分成 3~5 个文件（`MapLoading`、`Persistence`、`Combat`），但每个拆分只服务一个具体场景
- `BuildPrototypeDefinition` 字典在两个 demo 中各定义一份，约 30+ 条目手工重复维护

**根因**：这些模式借鉴了团队协作项目的最佳实践（SOLID、依赖注入、关注点分离），但团队项目引入抽象是为了**多人并行开发 + 长期维护**。独游只有一个人，接口和委托链带来的"可替换性"几乎不会被使用，反而增加了每次修改时需要跳转的文件数量。

**信号**：FD 和 MFD 之间 47% 重复率就是过度抽象的反证——抽象层铺得很开，但具体场景逻辑还是得各自硬写一遍，抽象并没有减少实际代码量。

### 问题二：核心玩法的代码密度偏低，"壳"代码占比过高

**粗略分层估算**：

| 层级 | 说明 | 估算行数 | 占比 |
|------|------|---------|------|
| 核心模拟 | `SimulationController`、`FactoryStructure` 基类/子类、传输/电力/战斗模拟 | ~10,000 | 17% |
| 建筑定义 | 30+ 建筑类型各自的逻辑（采矿、熔炼、组装、炮塔等） | ~8,000 | 14% |
| UI/HUD | `FactoryHud`、`MobileFactoryHud`、`FactoryPlayerHud`、工作区面板 | ~8,000 | 14% |
| Demo 控制器 | `FactoryDemo.cs` + `MobileFactoryDemo.cs` 及其 partial class | ~10,000 | 17% |
| 蓝图/持久化 | 蓝图捕获/应用、地图导入导出、运行时存档 | ~5,000 | 9% |
| 预览/视觉 | 悬停预览、蓝图预览、供电连线、端口提示、资源浮层 | ~5,000 | 9% |
| 冒烟测试 | 各种 Smoke 控制器、验证脚本 | ~10,000 | 17% |
| 工具/配置 | `FactoryConstants`、`FactoryDirection`、`FactoryPresentation` 等 | ~1,600 | 3% |

核心模拟 + 建筑定义合计约 **31%**，而 demo 控制器 + UI + 持久化 + 预览这些"壳"层合计约 **49%**，冒烟测试又占 17%。

一个健康的工厂类沙盒游戏项目，核心模拟代码应该占到 50%~60%，"壳"代码是增删改查外围逻辑，不应超过模拟逻辑本身。

### 问题三：冒烟测试过度膨胀

10,000 行冒烟测试对当前阶段来说异常高。冒烟测试应该是**粗粒度的场景验证脚本**（"工厂部署后炮塔是否能开火"），而不是细粒度的单元测试。当前量级暗示两种可能：
- 测试脚本中重复了 demo 控制器的初始化/配置逻辑
- 游戏缺乏稳定的核心循环，不得不靠大量测试来保证每个小功能不被破坏

正常的游戏项目在原型阶段，测试应是辅助性的（~5%~10% 代码量），而非与核心模拟持平。

### 建议

以下建议按优先级排列，不做大重构，只在后续开发中渐进式执行：

1. **新功能先写 procedural，别先抽接口**：新建筑、新系统先在单个文件里用具体类型写完，等玩法验证稳定、出现第三处使用时再考虑抽象。`IFactorySite` 已经够用了，不要再新增接口。

2. **Demo 控制器减重**：本文档的阶段 A（工具抽取）可以立刻执行，零风险减少 ~400 行。阶段 C（蓝图统一）在后续需要修改蓝图逻辑时顺手做。

3. **合并重复的定义字典**：`_definitions` 字典在两个 demo 中各自维护一份，改为从一个共享的 `FactoryPrototypeCatalog` 读取。

4. **冒烟测试瘦身**：review 冒烟测试代码，删除那些与 demo 初始化高度重复的部分，改为共享一个 `SmokeTestHarness` 基类。

5. **关注代码量增长率而非绝对值**：目标是把代码量增长曲线从线性压到对数——前 1 万行实现核心玩法，每新增一个建筑类型只需 100~200 行（而非现在的 ~500 行带全套 HUD 粘连）。

### 健康的代码量基准参考

| 游戏规模 | 典型代码量 | 核心模拟占比 |
|---------|-----------|-------------|
| 小型独游（Stacklands 级别） | 8k~15k | 60~70% |
| 中型独游（Factorio 早期原型） | 20k~40k | 50~60% |
| 大型独游（Factorio 正式版） | 200k+ | 35~45% |
| **本项目当前** | **57k** | **~31%** |

当前代码量处于"中型独游的体量、大型独游的架构复杂度、小型独游的核心玩法深度"这个不匹配区。
