## Context

当前运输物体的可视化路径建立在 `FlowTransportStructure` 的每物品一节点模型上：物品进入传送结构时会调用 `CreateTransitVisual()`，由 `FactoryTransportVisualFactory.CreateVisual(...)` 直接创建 `Node3D`，随后在 `UpdateVisuals()` 里逐个插值并回写节点位置。这条路径实现简单，也保留了 item profile 的模型、贴图和 billboard 回退链，但它把“运输状态”“渲染节点生命周期”“每帧 transform 更新”绑定得太紧，导致物流密度升高后 CPU 场景树开销和 draw call 数同时上涨。

项目里已经有两个可复用基础：一是 `FactoryDemo.TryGetVisibleWorldCellRect(...)` 能从当前相机投影出世界可见 cell 范围；二是 `FactoryItemCatalog`/`FactoryTransportVisualProfile` 已经能为每种 item kind 解出稳定的视觉配置。这意味着这次优化不需要改变物流模拟本身，而是应把运输结构改为输出轻量渲染快照，再让独立渲染聚合层基于 profile 和可见区统一完成分桶、裁剪和实例提交。

## Goals / Non-Goals

**Goals:**
- 把运输物体渲染从“每个 item 一个场景节点”改为“快照驱动的聚合渲染”，显著降低高吞吐物流场景的节点和 draw call 成本。
- 利用现有可见 world cell rect 能力，只为摄像机附近和视口相关区域提交运输物体实例。
- 为 item visual profile 增加可参与批渲染和分级退化的稳定描述，让近处保持可读性、远处降低渲染成本。
- 让静态工厂 demo 暴露新的运输渲染统计和压力验证，确保后续优化有稳定回归基线。
- 保持运输模拟确定性，确保渲染优化不改变 item 顺序、位置、节拍与传递行为。

**Non-Goals:**
- 不重写 `SimulationController` 的物流规则，也不改变 belt、splitter、merger 的吞吐或路由语义。
- 不尝试引入 GPU compute 驱动的全新物流模拟，本次仍以 CPU 维护运输状态。
- 不覆盖所有世界物体渲染；本次聚焦运输中的物流物体，而不是建筑本体、敌人、HUD 或地表装饰。
- 不要求第一版就做真实遮挡剔除、LOD 资源流送或跨场景通用渲染框架。

## Decisions

### 用“运输渲染快照 + 聚合渲染器”替代“运输结构直接持有可视节点”

`FlowTransportStructure` 不再把 `TransitItemState.Visual` 作为真实 `Node3D` 持有，而是改为保留最小渲染数据，例如 item kind、source/target、lane key、当前位置、上一帧位置和所属结构/世界坐标。每一帧或每个 simulation tick 后，运输结构向新的渲染层提交 `FactoryTransportRenderSnapshot`，由聚合器决定是否可见、落入哪个批次，以及应该使用何种表现层级。

建议新增一个集中式运行时组件，例如 `FactoryTransportRenderManager`，由 `FactoryDemo` 持有并在 `_Process` 或现有视觉刷新阶段驱动。它负责：
- 接收当前 frame 的运输快照；
- 根据 item kind/profile 解析批次 key；
- 维护批量实例节点，例如 `MultiMeshInstance3D` 或少量共享 `MeshInstance3D` 容器；
- 回收本帧未被提交的实例槽位。

这样能把场景树中的物流可视对象数量从“运输 item 数量级”压到“视觉桶数量级”。备选方案是继续让每个结构自己维护一个局部 `MultiMeshInstance3D`。这能减少改单个类的难度，但会把同类 item 分散到大量结构级批次里，无法真正做跨结构合批，也更难统一裁剪策略，因此不采用。

### 以“可见 cell rect + 安全边距”作为第一版裁剪边界

当前 `FactoryDemo.TryGetVisibleWorldCellRect(...)` 已能把视口投影到工厂平面并得到 cell 范围。第一版运输渲染裁剪应直接复用这个结果，并增加一圈可配置 padding 作为安全边距，避免物体在视口边缘出现明显 pop-in。渲染聚合器只收集位于可见 rect 或 padding rect 内的运输快照；其余 item 继续模拟，但不提交任何实例。

这样做的优点是实现简单、与当前俯视相机模型一致，而且能天然和 grid-based 结构组织对齐。备选方案是按 item 世界坐标逐个做视锥测试，这会让每帧数学判断和状态分发更细，但对当前规则化的格子工厂收益有限。另一个备选方案是真实遮挡剔除，不过当前世界主要是低矮建筑和开放视野，复杂度远高于收益，因此不在本次范围内。

### 运输 visual profile 扩展为“渲染描述符”，支持实例桶和距离退化

`FactoryTransportVisualProfile` 当前主要描述 tint、贴图、模型工厂和 fallback 开关，但不适合直接作为合批 key。设计上应新增一层稳定的渲染描述符，例如：
- 表现模式：placeholder box、textured box、billboard quad、custom model；
- 近景主表现；
- 中景/远景退化表现；
- 可共享 mesh/material 的批次标识；
- 是否允许进入实例化批渲染；
- 远景时的高度、缩放和颜色参数。

`FactoryTransportVisualFactory` 则从“创建 Node3D”转向“解析 descriptor 和共享资源”。近景仍尽量保留当前 profile 的模型或贴图表达；中景优先退化到共享 box/quad；远景可进一步退化到更轻量的 billboard 或色块占位。这样既保留 item 种类可辨识度，也让大部分远距离物体合并到更少的批次。

备选方案是强制所有 item 都只显示单一颜色方块。这个方案性能最好，但会直接破坏既有的 `factory-item-visual-profiles` 可读性目标，因此不采用。另一个备选是让带 `ModelFactory` 的物品继续维持独立节点。这样会让高价值 item 维持质量，但会把性能热点重新引回场景树，因此更适合只作为极少数“不支持实例化的特殊表现”的保底例外，而不是默认路径。

### 近中远分级基于摄像机距离和 profile 回退链，而不是单一全局 LOD

渲染聚合器会依据运输物体到相机的距离或所在 cell 到视口中心的区段，分配为 near、mid、far 三档：
- near：尽量使用 profile 的主表现，保证玩家靠近传送带时仍能看出矿石、板材、弹药等差异；
- mid：退化到可共享的 textured box 或 billboard；
- far：优先使用最轻量的 billboard / 占位实例，并降低不必要的阴影或材质复杂度。

这比只做“可见 / 不可见”二元裁剪更符合工厂游戏的观察习惯，因为远处大规模物流仍然需要“看得出有物流在动”，而不是完全消失。备选方案是只在屏幕外裁剪、不做分级。这样实现更简单，但在超长传送带都位于屏幕内时，仍会保留过多高成本表现，无法解决用户反馈中的核心压力场景。

### 首次实现优先覆盖 `FlowTransportStructure` 系列，保留其他独立持物路径后续接入

高密度瓶颈主要来自 belt / splitter / merger / bridge / loader 等继承 `FlowTransportStructure` 的连续运输结构，因此第一阶段只把这条主路径切到聚合渲染器。像 `InserterStructure` 的手持物、结构详情面板或独立世界道具可继续保留现有 `Node3D` 方式，等主瓶颈解决后再评估是否统一。

这样可以用最小改动获得最大收益，也避免一次性重写所有 item 视觉路径。备选方案是全项目所有 item 表现同时重构。那会让变更范围扩散到 UI、手持动画、检查窗口和战斗掉落，不利于快速落地和回归。

### 用 demo telemetry 和 smoke case 锁定性能优化的行为契约

这次不是单纯“内部重构”，所以需要让 demo 和 smoke 明确验证新的渲染契约。`FactoryHud` 或等价 telemetry 区块应显示至少以下信息：
- 当前活动运输物体总数；
- 当前可见运输物体数；
- 当前实例批次数或桶数；
- 当前运输渲染模式是否启用聚合路径。

同时新增高密度运输 smoke / regression case，验证：
- 大量运输物体存在时，渲染 telemetry 仍然可读；
- 可见运输物体数小于总活动运输物体数，证明裁剪生效；
- mixed profile item 在聚合路径下仍有可辨识回退，不会全部丢失显示。

备选方案是仅凭人工观察帧率变化判断优化是否成立。这会让后续回归很难发现“性能回退但功能没坏”或“功能还在但裁剪失效”的问题，因此不采用。

## Risks / Trade-offs

- [运输快照与模拟状态解耦后，若同步时序处理不当，可能出现一帧错位或残影] -> Mitigation: 保留 `previous/current` 进度并让聚合器继续用现有 `tickAlpha` 插值，而不是退回离散跳变。
- [`MultiMeshInstance3D` 更适合共享 mesh/material，复杂自定义模型未必都能完美实例化] -> Mitigation: 在 descriptor 上显式区分“可实例化”和“特殊单体保底”，先覆盖绝大多数常见物流 item。
- [距离退化如果过于激进，会损害物品辨识度] -> Mitigation: 把 profile 的 tint 和核心轮廓保留下来，并通过 smoke case 验证矿石、板材、弹药等至少维持类别差异。
- [可见 rect 裁剪依赖相机投影，边缘 padding 设置不当会造成 pop-in] -> Mitigation: 使用带余量的 padded rect，并在设计中把 padding 作为可调参数，而不是写死常量。
- [新渲染路径可能让调试单个运输 item 的直观性下降] -> Mitigation: 保留开发态开关或 telemetry，能查看总活动数、可见数和批次数，必要时允许回退到 legacy 单体渲染进行诊断。

## Migration Plan

1. 为 `FactoryTransportVisualProfile` 和 `FactoryTransportVisualFactory` 引入可复用的渲染描述符与共享资源解析路径。
2. 在 `FlowTransportStructure` 中移除每 item 创建/销毁 `Node3D` 的责任，改为维护最小运输状态并输出渲染快照。
3. 新增 `FactoryTransportRenderManager`，完成快照收集、可见 rect 裁剪、分桶和实例批次提交。
4. 在 `FactoryDemo` 中接入聚合渲染器和 telemetry，把相机可见 rect 结果传给运输渲染层。
5. 扩展 smoke / regression case，覆盖高密度物流、裁剪生效和 profile 回退可读性。

回滚策略：若聚合渲染路径在实现期造成明显视觉回归，可保留新的 descriptor 和 telemetry 结构，但临时让 `FlowTransportStructure` 继续走 legacy `Node3D` 创建路径；这样可以逐步接入分桶/裁剪，而不用一次性强切全部视觉表现。

## Open Questions

- 第一版的远景退化是否需要按 item kind 维持专属贴图，还是统一退化为带 tint 的共享 billboard 即可？
- `FactoryDemo` 之外是否已经存在需要同一运输渲染聚合器的第二个场景；如果短期没有，可以先把 manager 设计为工厂 demo runtime 组件而不是全局 autoload。
- telemetry 是否需要记录帧耗时的运输渲染细分统计，还是先显示可见数和批次数即可；这取决于实现期的实际诊断需求。
