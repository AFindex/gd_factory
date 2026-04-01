## Context

当前移动工厂的外部交互边界仍然是一次性实现。`MobileFactoryInstance` 在构造时直接创建固定的 `_outputBridge`，并从 `Profile.OutputBridgeCell` 与 `Profile.PortOffsetsEast` 推导唯一输出端口；`MobileFactoryPortBridge` 在未绑定世界时会进入 `_transitRecycleTotal++` 的回收分支，因此移动态下端口不会真正堵住，而是持续吞掉物品。这和现在的玩法目标相反，因为玩家需要看到“未部署/未接通的输出就是堵塞边界”，而不是一个隐形垃圾桶。

同时，当前内部编辑只支持把结构放在完全位于工厂内部的单格位置。端口并不是一个可建造的模块，没有“有些格子必须在厂内、有些格子必须伸到厂外”的空间约束，也无法扩展到输入端口或未来其他与外部世界交互的 attachment。部署表现也只是在世界格上出现一个独立标记，缺少从工厂本体伸出、接到世界网格的连续感。

这次设计需要同时解决玩法正确性、可视化连续性和后续扩展性。用户要求的核心不是“再加一个输入桥”这么简单，而是把移动工厂边界定义成一套可放置、可扩展、可旋转、可部署激活的 attachment 系统。这里可以借鉴《异星工厂》近岸抽水器的设计思路：建筑虽然主体位于一侧，但其可建造性依赖边界另一侧的格子条件。根据 Factorio 官方 Friday Facts #336 的 redesign 说明，offshore pump 放在陆地上，但会额外检查邻接的 `2x3` 水域区域是否满足条件。我们不需要照搬它的具体尺寸，但可以沿用它“一个跨边界模块由内外两部分格子共同定义”的原则。

## Goals / Non-Goals

**Goals:**
- 修复移动工厂未部署时输出端口吞物的问题，让未激活输出边界表现为真实阻塞。
- 为移动工厂增加从外部世界向内部导入物品的输入端口能力，并与现有输出端口共享统一系统。
- 将端口升级为可建造、可扩展的边界 attachment 模块，支持旋转、占格校验、部署激活和状态展示。
- 让边界 attachment 的占格同时覆盖工厂内部和外部连接区域，形成明确的“跨边界”放置规则。
- 在部署时生成从 attachment 本体延伸到世界格的连接件视觉，建立连续的连接感。
- 将移动工厂与外部世界的交互统一到一套 attachment runtime/definition 边界中，方便后续增加非 I/O 的外部交互建筑。

**Non-Goals:**
- 不在这次设计中引入流体系统、能量网络或完整 docking 网络。
- 不尝试一次支持任意复杂的非矩形 hull 切割；第一版只要求 attachment 挂在预定义的外缘挂点或周界边上。
- 不重做整个世界物流规则；静态世界结构仍沿用当前 `FactoryStructure` / `IFactorySite` 模型。
- 不要求第一版就做出完整美术资源体系；可先使用程序化 mesh 完成连续连接表现。

## Decisions

### 将固定输出桥重构为通用的边界 attachment 系统

新增一层显式的数据模型，例如：
- `BoundaryAttachmentDefinition`: 定义 attachment 类型、允许的交互方向、旋转规则、占格 stencil、可视化参数和部署时需要暴露的世界连接点。
- `BoundaryAttachmentInstance`: 绑定到某个移动工厂的具体 attachment，记录朝向、挂点、激活状态、连接状态和运行时节点。
- `BoundaryAttachmentKind`: 至少包含 `OutputPort` 与 `InputPort`，后续可扩展到其他对外交互建筑。

`MobileFactoryInstance` 不再 hardcode 一个 `_outputBridge`，而是维护一组 attachment 实例，并在部署/回收时统一激活或解除绑定。当前输出桥可以保留其输送实现，但应退化为 attachment runtime 的一种具体 transport endpoint，而不是移动工厂本体的固定字段。

这样做的原因是用户已经明确提出未来还会有其他与外部世界交互的建筑。如果这次只是把 `_outputBridge` 复制一份 `_inputBridge`，很快又会遇到第三、第四类边界结构的重复问题。

备选方案是继续保留 `MobileFactoryPortBridge` 作为唯一特殊结构，再额外补一个输入桥。这个方案实现快，但会把“可建造、可扩展、跨边界占格”的需求继续挤压到 hardcode 分支里，因此放弃。

### 用“内侧格 + 边界格 + 外侧格”的 stencil 定义 attachment 形状

每个边界 attachment 使用可旋转 stencil 定义自身几何约束，而不是只保存一个内部 cell 和一个世界 port cell。推荐至少拆成三类坐标：
- `InteriorCells`: attachment 在工厂内部占据或保留的格子。
- `BoundaryCells`: attachment 穿过 hull 边缘的喉部/安装位，用于校验它是否确实贴在边界上。
- `ExteriorCells`: attachment 激活后要在世界侧连接、占用或验证的格子。

编辑器中的合法性检查以 attachment 的局部挂点和朝向为输入，先验证 `InteriorCells`/`BoundaryCells` 是否满足内部边界要求；部署预览则进一步把 `ExteriorCells` 旋转并投射到世界网格上，验证对应的世界单元是否可保留。

这种设计借鉴的是 Factorio offshore pump 那种“主体在一边、建造条件跨到另一边”的边界建筑原则。对我们的玩法来说，最重要的是玩家能够清楚看到某个 attachment 不是纯粹的内部设备，而是一个横跨工厂内外边界的接口。

备选方案是继续只在部署时根据一个内部 cell 额外算出单个 port cell。这个方案无法表达更复杂的挂点形状，也无法在编辑器里解释“为什么这里能放、那里不能放”，因此不采用。

### 未激活输出端口必须阻塞，不能再隐式回收

输出 attachment 在未部署、已回收或未连接到合法世界侧线路时，必须表现为不可向外分发物品。具体上可以采用以下规则：
- attachment 不再在 `TryDispatchItem` 失败时吞掉物品。
- 已经进入 attachment 内部 transit 的物品要停留在 attachment 的缓冲/运输状态，直到目标可用或结构被显式清空。
- 上游 belt/merger/producer 按现有输送规则自然背压，形成玩家可见的堵塞。

输入 attachment 则采用对称规则：未激活时不向内部发送物品，也不假装从世界侧取得输入。

这样可以保证系统语义统一：“跨边界交互只有在 attachment 激活且完成世界绑定时才成立”。用户想修的 bug 本质上就是当前 runtime 违反了这条契约。

备选方案是保留旧的内部回收，但只在某些 demo profile 上关闭。这个方案会让玩法规则依赖配置，而不是依赖 attachment 状态，后续很难维护。

### 将 attachment 的内部放置与世界部署拆成两个阶段

边界 attachment 的生命周期应拆成两个层面：
- `Placed`: 玩家已经在内部编辑器中放置了 attachment，内部占格与朝向固定存在。
- `Activated`: 当前部署朝向与世界位置允许该 attachment 投影到世界，并成功绑定了外侧连接点。

这意味着 attachment 的内部布局在移动态和部署态之间持续存在，但只有部署成功后才会产生世界侧 reservation、连接节点和跨站点物品交换。回收时只解除 activation，不销毁 attachment 放置本身。

这与现有移动工厂“内部布局跨生命周期持久化”的规则一致，也能避免在每次 deploy/recall 时重建或丢失玩家配置的边界模块。

备选方案是在部署时临时生成端口、回收时直接删除。这会让端口不再是“可建造的工厂部件”，不符合本次目标。

### 用 attachment 激活器统一管理部署校验、绑定和视觉连接

为避免 `MobileFactoryInstance` 同时知道每一种 attachment 的物流逻辑和可视化细节，引入一层激活器，例如 `BoundaryAttachmentActivator` 或等价服务，负责：
- 汇总当前移动工厂所有已放置 attachment 的旋转后 `ExteriorCells`。
- 在部署预览阶段生成世界侧校验数据和预览颜色。
- 在正式部署时申请/释放对应的 world reservations。
- 为每个激活 attachment 生成 connector visual 和世界侧 endpoint visual。
- 把 attachment runtime 绑定到 `GridManager`/`IFactorySite`。

`MobileFactoryInstance` 只负责生命周期切换和传递 anchor/facing，不直接 hardcode “世界端口根节点应该长什么样”。这会让部署视觉也跟着 attachment 数量和类型扩展。

备选方案是在 `MobileFactoryInstance` 里继续维护一个 `_worldPortRoot`，然后再为输入端口加第二个 root。这个方案无法扩展到可建造 attachment 列表，会再次走向 hardcode。

### 部署预览与编辑预览共用同一套 attachment 几何求解

attachment 的 shape、旋转和内外侧单元不能在“内部编辑器”和“世界部署预览”里各算一套，否则很容易出现编辑器能放、部署时却无效的错位 bug。应当把几何求解统一为一套纯数据 API，例如：
- `GetInteriorOccupiedCells(anchor, facing)`
- `GetBoundaryCells(anchor, facing)`
- `GetExteriorWorldCells(factoryAnchor, factoryFacing, attachmentAnchor, attachmentFacing)`
- `GetConnectorSegments(...)`

编辑器使用前两组数据做内部预览，部署模式使用完整结果做世界投影预览。这样 attachment 的可见形状和真实部署形状才能一致。

备选方案是 editor 只显示一个图标、world preview 再单独算世界端口。这个方案最省事，但正是当前“逻辑和表现脱节”的来源之一。

### 连接视觉从 attachment 本体生成，而不是从世界格孤立生成

部署完成后，每个激活 attachment 至少应表现出三段视觉层级：
- 工厂边缘的 attachment 主体。
- 从主体伸出的 connector/stem/arm。
- 落在世界侧目标格上的 mouth/socket/endpoint。

第一版可以使用程序化 mesh 组合实现，不要求复杂动画，但要求 connector 的起点明确属于 attachment，而不是在世界格上凭空出现一个方块。若 attachment 因朝向变化或扩展尺寸不同，connector 也必须同步变化。

这项设计决定的价值主要在“联通感”。用户明确提出部署时要“从这些端口的模型对应的伸出一个模型连接到世界的格子”，所以视觉不能继续依赖一个脱离 attachment 来源的 `_worldPortRoot`。

备选方案是仅保留地面标记或发光框。这个方案实现简单，但无法满足需求。

### 为未来非 I/O attachment 预留共同接口，但首版只实现 item channel

虽然这次真正落地的 attachment 主要是输入/输出端口，但 runtime 接口应留出通道类型与激活回调，例如：
- `InteractionChannelType`：`ItemInput`、`ItemOutput`，未来可扩展 `FluidInput`、`PowerTap`、`DockingLatch`。
- `Activate(worldSite, projection)`
- `Deactivate()`
- `GetEditorOverlayState()`
- `BuildConnectorVisual(...)`

首版实现只需要 item flow，但是 attachment 系统本身不应把“物品物流桥”写死为唯一用途。这样后续新增其他与外部世界交互的建筑时，可以复用同样的挂点、占格、部署校验和连接视觉链路。

备选方案是先不做抽象，等未来有第二种 attachment 再说。考虑到这次已经要大改端口数据结构，顺手把边界抽象定下来成本更低，也更符合用户的前瞻性要求。

## Risks / Trade-offs

- [边界 attachment 系统会让当前较轻量的 mobile factory runtime 明显复杂化] → Mitigation: 保持 definition/instance/activator 三层最小职责，避免把输入、输出和可视化各自再拆出独立管理器。
- [输出阻塞改正确后，现有 demo 内部布局可能因为没有回收支路而长期堵住] → Mitigation: 更新 demo authored layout，给出符合新规则的缓冲或消费路径，并把“堵住是正确行为”纳入测试预期。
- [跨边界 stencil 与旋转投影容易出现一格错位] → Mitigation: 统一几何求解 API，并用四向 smoke case 覆盖 interior/exterior cell 投影。
- [attachment 可建造后，玩家可能尝试把它放在不合理的位置] → Mitigation: 第一版只开放预定义的 perimeter 挂点或外缘合法区域，并在 editor 里清晰着色解释失败原因。
- [视觉连接件若完全程序化，可能显得过渡性较强] → Mitigation: 先保证空间关系正确和联通感，再把 connector visual 封装到可替换节点，后续可替换为正式美术资源。
- [为未来预留接口可能被认为“过度设计”] → Mitigation: 只预留边界激活和 channel 类型，不提前实现无需求的系统。

## Migration Plan

1. 引入 attachment definition/instance 数据结构，并把当前固定输出桥迁移为一个默认预置的 `OutputPort` attachment。
2. 改造内部编辑器的占格与预览逻辑，使其能够放置 attachment，并显示 interior/boundary/exterior 形状约束。
3. 重构 `MobileFactoryInstance` 的部署流程，接入 attachment activator，用 attachment 投影结果替代当前单一 `PortOffsetsEast` 校验。
4. 修改 `MobileFactoryPortBridge` 或其替代 runtime，使未激活输出阻塞、未激活输入不导入，去掉 transit recycle 语义。
5. 实现输入 attachment 的世界绑定与 demo authored content，让部署后的移动工厂同时展示外输入与外输出链路。
6. 用 attachment 生成 connector/stem/world endpoint visual，替换当前孤立的世界侧端口标记。
7. 更新 demo 布局、HUD 文案和 smoke tests，覆盖放置、部署、回收、阻塞、双向连接和视觉同步。

回滚策略：如果 attachment 系统集成中出现大回归，可先保留“固定预置 attachment 列表”而不开放自由扩展放置，同时继续沿用新的激活/阻塞语义。这样能够保住核心 bug 修复与双向交互，不必完全退回旧的单桥 hardcode 方案。

## Open Questions

- 第一版 attachment 放置是否只允许挂在预定义 mount points，还是允许沿整条工厂边缘自由拖放？当前建议先做 mount points，以降低形状校验复杂度。
- 输入 attachment 的世界侧交互是否完全复用现有 belt push/pull 规则，还是需要一个显式的 loader/unloader 样式端点？当前建议先做 belt-compatible endpoint，再视体验决定是否加专用端点。
- 一个 attachment 的 `ExteriorCells` 是否允许超过一个世界格并形成更长的“伸出段”？当前建议首版支持单 endpoint + 可视连接件，多格 exterior shape 主要用于占格校验而不是多端输出。
