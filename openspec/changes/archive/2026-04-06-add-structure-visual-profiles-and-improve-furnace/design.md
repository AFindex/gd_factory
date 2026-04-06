## Context

当前 `FactoryStructure` 体系把建筑逻辑、几何搭建和运行态表现放在同一个继承点上：每个结构子类通过 `BuildVisuals()` 直接创建 `MeshInstance3D`，再在 `UpdateVisuals()` 里手工更新少量材质状态。这个方式对早期原型足够直接，但当建筑需要更复杂的分层表现、动画、特效锚点，或者未来需要直接挂接 `PackedScene`、导入模型和 `AnimationPlayer` 资源时，结构脚本会很快变成“既管模拟，又管美术资产，又管动画状态”的耦合中心。

项目已经在移动物品上落地过 `FactoryTransportVisualProfile` 这类“逻辑定义 + 视觉回退链”的模式，因此建筑表现也适合采用类似的配置化设计。这个变化是横跨结构基类、具体建筑实现、未来资源加载路径和 demo 表现的交叉改造，值得先通过设计文档把边界定稳。

## Goals / Non-Goals

**Goals:**
- 为建筑引入独立于模拟逻辑的视觉 profile 抽象，让结构可以声明“如何表现”，而不必把所有视觉代码都写死在结构类里。
- 支持三种来源的建筑表现：纯代码生成、外部 `PackedScene`/模型资源、以及包含动画节点的资产化表现。
- 定义统一的建筑视觉状态快照，让“运行中、空闲、供电不足、输出阻塞、受攻击”等状态能驱动表现更新，而不要求每个结构脚本直接摸节点树。
- 保持对现有纯代码建筑的兼容，让未迁移建筑仍可继续工作。
- 以熔炉为第一个迁移示例，使用代码定义方式做出更像熔炉的炉膛、余烬、烟囱和热态动画。

**Non-Goals:**
- 本次不要求把所有现有建筑一次性迁移到新视觉 profile。
- 本次不引入完整材质系统、粒子美术管线或外部 DCC 导出的最终成品资源规范。
- 本次不改变建筑的放置、配方、供电或物流模拟规则。
- 本次不要求所有建筑都必须具备动画，仅要求新架构可以承载动画。

## Decisions

### Introduce a structure visual-profile contract parallel to structure simulation classes

新增一套与 `FactoryTransportVisualProfile` 类似但面向建筑的配置对象，例如 `FactoryStructureVisualProfile` / `FactoryStructureVisualDefinition`。结构类仍然保留“是什么、怎么模拟”的职责，但把“要生成什么视觉树、允许哪些回退、如何绑定动画锚点”交给 visual profile 描述。

这样一来，`SmelterStructure` 之类的类只需要声明自己的 visual profile，并在必要时提供少量 profile 参数，而不是直接在结构类里堆积所有可视节点细节。

Alternative considered: 继续沿用每个结构子类覆写 `BuildVisuals()` / `UpdateVisuals()` 的方式，并在需要时手工特判加载 scene。这个方案被放弃，因为它会让每个结构都自己发明一套资源加载和状态绑定协议，后续模型/动画接入会重复劳动。

### Add a presentation root/controller layer owned by `FactoryStructure`

`FactoryStructure` 仍然是世界中的权威节点，但其视觉子树应被收敛到统一的 presentation root，例如 `StructureVisualRoot`，并由一个轻量控制器对象负责：
- 构建视觉实例
- 缓存常用锚点、材质或动画播放器引用
- 接收结构运行态快照并更新显示

结构基类负责生命周期管理，具体 visual profile 只需要返回“如何构建”和“如何响应状态”。这避免了未来 scene 资产、程序化节点和通用战斗 overlay 彼此缠绕。

Alternative considered: 让每个 profile 直接返回一个裸 `Node3D`，由结构类自己记住并随意更新。这个方案太松散，不利于统一 fallback、状态同步和资源释放。

### Represent runtime presentation with a small immutable visual-state snapshot

新增统一的视觉状态快照，例如包含：
- `IsActive`
- `IsProcessing`
- `ProcessRatio`
- `PowerStatus`
- `PowerSatisfaction`
- `HasBufferedOutput`
- `IsHovered`
- `IsSelected`
- `IsUnderAttack`

`FactoryStructure.UpdateVisuals()` 不再优先做每个建筑的手工节点改色，而是生成当前状态快照并交给 presentation controller。对 `FactoryRecipeMachineStructure` 这样的子类，可以扩展快照字段而不泄漏具体节点结构。

Alternative considered: profile 直接反向查询结构实例上的各种字段。这个方案更省代码，但会把 profile 与具体继承层级强耦合，难以复用，也不利于未来 scene 资产型表现使用更稳定的输入协议。

### Use a deterministic fallback chain for authored assets

建筑视觉 profile 需要支持“偏好外部资产，但不要因缺资源而失去表现”。建议回退顺序为：
1. 可实例化且验证通过的 authored scene / model hierarchy
2. authored scene 中缺失动画时的静态 scene 表现
3. profile 提供的程序化 code builder
4. 最低保真通用占位体

这与现有 item visual profile 的思路一致，能保证未来资源导入可以渐进接入，而不会阻塞功能开发或 smoke 验证。

Alternative considered: 如果外部资源缺失则直接报错并中止构建。这个方案不适合当前项目阶段，因为资源制作和玩法迭代还会并行推进。

### Keep legacy `BuildVisuals()` compatibility through an adapter path

为了降低迁移成本，结构基类可以保留旧的 `BuildVisuals()` 入口，但把它收敛为默认 procedural adapter：如果某结构没有显式 visual profile，就自动使用 legacy builder 生成表现。这样改造不会强迫一次性重写所有结构。

新迁移的结构可以覆盖新的 `CreateVisualProfile()` 或类似入口，而旧结构不动也能继续工作。

Alternative considered: 一刀切移除 `BuildVisuals()`。这个方案会让改动面过大，也会把提案范围从“建立新通道”变成“全量迁移”。

### Make the smelter the reference procedural profile

熔炉应成为第一批参考实现，但仍然用代码构建，而不是立刻依赖外部模型。目标是证明新系统不只是“为了未来资产接入”，它也能让程序化建筑表现更有层次。具体表现可以包括：
- 更像炉体的底座、耐火砖主体、进料口、炉门、排烟烟囱和顶部冠盖分层
- 基于处理状态的炉膛发光、炉门脉冲和烟囱热端亮灭
- 基于处理节奏的轻微高度脉动、排烟节奏或热气柱动画
- 在断电或停机时平滑退回冷却状态，而不是瞬间切色

Alternative considered: 直接先做一个外部熔炉模型来验证。这个方案不是不能做，但会把本次 change 对资源依赖拉高，也无法证明“纯代码定义依然是一等公民”。

## Risks / Trade-offs

- [视觉 profile 抽象过早复杂化结构基类] -> Mitigation: 第一版只支持当前确实需要的字段和回退模式，不把它扩成通用资源图系统。
- [scene 资产型建筑与程序化建筑的状态绑定接口不一致] -> Mitigation: 统一使用视觉状态快照和命名锚点/动画通道约定，而不是每个表现自由发挥。
- [兼容旧 `BuildVisuals()` 会让基类短期内存在双轨逻辑] -> Mitigation: 明确 legacy adapter 只是过渡层，并优先把新增或重点建筑迁移到新 profile。
- [熔炉增强表现如果做得太夸张会干扰地图可读性] -> Mitigation: 控制动画幅度，优先做热态、发光和节奏感，而不是大范围粒子或遮挡性效果。
- [未来外部模型资产的坐标轴、缩放和材质约定可能反复调整] -> Mitigation: 把资源装配收口到 profile/controller 层，避免把这些兼容细节散落到结构模拟代码中。

## Migration Plan

1. 在结构基类里加入新的 visual profile / presentation controller 管线，并提供 legacy `BuildVisuals()` 适配入口。
2. 为建筑定义新增可选的 authored scene / animation 引用和程序化 builder 回退能力。
3. 将熔炉迁移到新 profile 管线，并用程序化 profile 完成第一版炉体与动画优化。
4. 更新 demo / smoke 验证，确认熔炉在运行、停机、供电不足等状态下能正确响应，同时未迁移建筑仍然正常显示。
5. 后续新建筑或高优先级旧建筑可以逐步迁移到新管线；如果需要回滚，可让具体结构退回 legacy adapter 而不影响模拟逻辑。

## Open Questions

- 第一版是否需要把“受攻击闪烁”也并入结构视觉状态快照，还是先继续沿用基类战斗 overlay 即可？
- 外部 `PackedScene` 资产是否需要约定标准命名锚点（如 `Glow`, `Chimney`, `AnimationPlayer`），还是通过 profile 明确传入节点路径更稳妥？
- 熔炉的热态表现是否需要最小化地加入 Godot 粒子/热浪 plane，还是先用材质发光和几何脉动完成第一版可读性验证？
