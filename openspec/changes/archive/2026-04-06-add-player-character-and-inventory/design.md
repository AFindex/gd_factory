## Context

当前项目已经有较成熟的工厂原型骨架：静态 sandbox 由 [FactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryDemo.cs) 驱动，移动工厂体验由 [MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs) 驱动；两边都复用了 [FactoryCameraRig.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryCameraRig.cs) 的固定俯视相机，以及 [FactoryStructureDetailWindow.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetailWindow.cs) / [FactoryStructureDetails.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetails.cs) 的独立详情窗口与槽位展示模型。现有库存底层已经支持栈叠、合并和同容器内拖拽，物品定义也通过 [FactoryItemVisuals.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryItemVisuals.cs) 的 `FactoryItemCatalog` 统一管理图标、颜色和堆叠上限。

缺口在于：现有交互仍以“场景 HUD + 相机模式”为中心，而不是“玩家角色 + 玩家持有物品”为中心。结果是建筑选择、容器交互、独立人物 UI、以及建筑本身作为可携带物件这几类玩法没有共同抽象，也让 sandbox 与 mobile demo 的交互入口逐渐分叉。这个 change 的目标是引入一个可复用的主角层，把已有的库存、面板、建造和镜头能力收束到统一的玩家交互模型上。

## Goals / Non-Goals

**Goals:**
- 在 static sandbox 与 mobile factory demo 中都生成一个主角实体，并让它成为默认交互焦点。
- 让主角拥有背包、底部热键栏、独立的人物/物品/属性面板，并且这些窗口能与现有建筑详情窗口共存。
- 让玩家背包和现有结构库存使用统一的槽位/堆叠/拖拽规则，而不是另起一套数据结构。
- 让可建造建筑拥有“物品化”定义，可以放进玩家背包、显示图标、进入热键栏、并通过左键放置到世界。
- 在不破坏移动工厂现有部署与内部编辑能力的前提下，让 mobile demo 的默认 WASD 控制入口切换为主角，而不是直接控制工厂或相机。

**Non-Goals:**
- 不在这次变更中引入完整角色成长、装备数值、任务系统、采集动画或近战/射击战斗。
- 不把主角扩展成带导航网格、跳跃、重力台阶和复杂碰撞反馈的完整动作角色；第一版只需要平面移动和胶囊占位。
- 不重写所有 HUD 成为全新的 UI 框架；会尽量复用现有 detail window、workspace 与 inventory slot 组件。
- 不在第一版处理世界掉落物拾取、建筑蓝图消耗配方、多人同步或网络复制。

## Decisions

### 用 `CharacterBody3D + CapsuleShape3D` 建立轻量主角实体，并让两个 demo 共享一套玩家控制器

主角会采用 `CharacterBody3D` 作为运行时实体，附带胶囊碰撞体和简单的胶囊/圆柱视觉占位，而不是只用 `Node3D` 手写 transform。这样做的原因是：
- Godot 已经为 `CharacterBody3D` 提供了稳定的平面位移语义，后续如果要加碰撞、交互范围和简单地形兼容，会比纯 `Node3D` 更自然。
- 用户已经明确要求“先用胶囊体占位”，这和 `CharacterBody3D + CapsuleShape3D` 的最小实现正好吻合。
- sandbox 和 mobile demo 都是 3D 俯视场景，共享一个 `FactoryPlayerController` 比在两个场景里各写一套输入逻辑更容易收敛行为。

备选方案是继续沿用当前 demo 的“无实体输入焦点”，让 HUD 维护一个虚拟玩家状态。这个方案实现更快，但会让相机跟随、交互距离、角色站位和后续世界拾取都继续缺少落点，因此不采用。

### 玩家库存直接复用现有栈叠库存模型，并在其上抽象跨容器转移

当前项目已经有 `FactoryInventorySlotState`、堆叠合并和 `TryMoveItem` 等库存能力，结构详情面板也已经围绕 `FactoryInventorySectionModel` 构建。设计上不再为玩家背包创建另一套“UI 专用”库存对象，而是把玩家背包、热键栏和建筑库存都统一建模为可暴露槽位网格的库存端点。

具体会新增一个类似 `IFactoryInventoryEndpoint` / `FactoryInventoryHandle` 的抽象，用于描述：
- 槽位布局与当前物品快照；
- 是否允许拖入 / 拖出；
- 从某个槽位提取、合并、拆分、放入的命令接口；
- 所属对象是谁（玩家、仓储、炮塔、发电机等）。

跨容器拖拽将通过一个集中式 transfer service 执行，而不是把“从玩家拖到建筑”拆成两个局部移动。这样可以复用既有堆叠规则，同时把失败条件收敛成统一结果，例如目标满栈、目标不兼容、来源不可移动。

备选方案是继续只支持“容器内部移动”，玩家与建筑之间通过按钮式搬运或 `Shift+Click` 快捷转移。这个方案不满足用户明确提出的双向拖拽体验，因此不采用。

### 建筑物品化采用“结构套件 item kind”方案，而不是单独的建造券系统

当前 `FactoryItemCatalog` 已经承担显示名称、图标、颜色和堆叠上限的统一入口，因此建筑进入背包时，最稳妥的做法是把可建造建筑扩展为新的 `FactoryItemKind` 定义集合，并在 catalog 中增加 `PlaceablePrototypeKind` 之类的放置元数据映射。每个“建筑套件”物品会显式对应一个 `BuildPrototypeKind`，同时配置自己的图标、名称、堆叠上限和提示文案。

这样做的好处是：
- 热键栏和背包不需要区分“资源物品”和“建筑物品”的渲染路径；
- 左键放置时可以直接从选中物品解析目标 `BuildPrototypeKind`；
- 图标和堆叠规则继续走现有 catalog 驱动路径，不需要为建筑单独维护一套 UI 素材系统。

备选方案是保留纯 `BuildPrototypeKind` 选择，并在玩家背包里只存“建筑许可数量”。这个方案会让背包 UI 与建造 UI 分离，既不直观，也不利于未来掉落、搬运和拆除返还，因此不采用。

### 热键栏是玩家背包的投影，不另存一份独立库存

底部快捷栏会被实现为玩家背包前 N 个槽位的投影视图，或由玩家显式绑定到背包槽位的引用，而不是额外维护一个复制库存。这样能保证：
- 从背包调整堆叠后，热键栏立即反映最新数量；
- 从快捷栏放置建筑后，真实消耗发生在背包源槽位，不会产生同步问题；
- 未来如果支持快捷键轮盘、拖拽重排或多页热键栏，底层仍然只有一份物品事实来源。

备选方案是给热键栏一套独立 9 格库存，并在背包与热键栏之间做同步。这个方案会引入双份状态、复制/撤销边界复杂度和额外 bug 面，不值得。

### 相机继续复用 `FactoryCameraRig`，但增加“跟随目标”和“上下文锁定”模式

已有 `FactoryCameraRig` 已支持平滑对焦、缩放和基于屏幕位置的纠偏，对新需求来说最有价值的是在此基础上增加：
- `FollowTarget`: 当前跟随的 `Node3D` 或世界坐标提供者；
- `FollowEnabled`: 开启时相机以主角为默认焦点，关闭时保留手动观察；
- `InputLockContext`: 当玩家正在拖拽 UI 或面板获得焦点时，屏蔽不该触发的世界输入。

static sandbox 中，相机会默认持续跟随主角；mobile demo 中，默认也是跟随主角，但进入显式的 observer / factory command context 后可以切换为现有的自由观察语义。这样能最大化复用现有相机实现，同时避免为了“跟随”而再造一个几乎重复的 camera controller。

备选方案是为玩家再做一套独立跟随相机，只在 mobile demo 继续使用 `FactoryCameraRig`。这个方案会让两个 demo 的镜头行为继续分叉，不利于后续统一。

### 保留现有结构详情窗口模式，并在其旁边引入玩家面板宿主

`FactoryStructureDetailWindow` 已经具备可拖拽、可展示槽位和配方的独立窗口能力，因此设计上不会推翻它，而是复用其窗口风格和 inventory grid 组件，再新增一层 `FactoryPlayerPanelHost` 或一组并列窗口来承载：
- 背包面板；
- 物品信息面板；
- 人物属性面板；
- 可能的快捷栏扩展/角色装备区。

其中背包面板与结构详情窗口会共享同一套槽位渲染与拖拽手势；物品信息面板显示当前选中或悬停物品的说明；属性面板显示角色基础属性与状态。用户可以同时打开多个面板，它们互不挤占结构详情窗口。

备选方案是把人物信息全部塞回现有 workspace HUD 中。这个方案违背“都是独立面板”的需求，也会让背包/属性重新回到大面板耦合状态，因此不采用。

### 世界交互优先级统一收束为“UI > 拖拽 > 角色交互 > 建筑放置”

为了避免新增玩家层后输入再度打架，统一规定输入优先级：
1. 若鼠标命中 HUD、独立面板或槽位拖拽，则只处理 UI；
2. 若存在进行中的跨容器拖拽，则只处理拖拽释放/取消；
3. 若玩家对准可交互建筑并未持有可放置建筑物品，则左键优先打开/聚焦结构详情；
4. 若当前选中的热键槽位携带可放置建筑物品，则左键执行网格预览与放置；
5. 右键或 `Esc` 用于退出当前建筑放置或拖拽态，回到普通交互。

这样既能保持现有“点击建筑查看详情”的默认体验，又能让“从热键栏选建筑后左键放置”的行为清晰可预测。

备选方案是始终让左键优先放置、交互改到额外按键。这个方案会让主角站在建筑旁边时很难直觉地查看详情，也会损伤当前 demo 的结构检查流程，因此不采用。

### 移动工厂 demo 改为“主角默认模式 + 工厂上下文模式”的双层控制

当前 `MobileFactoryControlMode` 默认从工厂命令开始，但这次需求要求主角成为最基础交互起点。因此 mobile demo 会改成双层模型：
- 顶层是 `PlayerControlMode`，默认由主角接收 WASD、镜头跟随主角；
- 当玩家显式进入移动工厂命令上下文时，才切换到现有工厂命令/部署/观察流；
- 离开工厂上下文后，输入与镜头回到主角。

这意味着移动工厂现有 `FactoryCommand / DeployPreview / Observer` 模式不需要被删除，而是从“demo 默认状态”降级为“玩家主动进入的专门上下文”。这样能同时满足用户的新要求和现有移动工厂演示价值。

备选方案是完全砍掉移动工厂专属控制模式，让一切都通过主角近距离交互驱动。这个方案会让现有 mobile demo 的核心展示点退化太多，因此不采用。

## Risks / Trade-offs

- [玩家库存与结构库存共用底层后，接口边界会比现在更抽象] → Mitigation: 保持 `FactoryInventory` 为事实存储，只新增薄适配层，不把现有结构库存直接重写成复杂 ECS。
- [把建筑扩展为 item kind 会让 `FactoryItemKind` 枚举快速变大] → Mitigation: 用 catalog 映射和分类字段收拢逻辑，UI 与放置代码只依赖“是否可放置/对应哪个 prototype”，而不是硬编码每个枚举分支。
- [主角默认模式可能和现有 mobile demo smoke 假设冲突] → Mitigation: 在 tasks 中明确更新 smoke 覆盖与 demo 入口文案，避免测试仍默认按旧控制模型断言。
- [多窗口拖拽 + 世界放置容易产生输入冲突] → Mitigation: 统一输入优先级，并在拖拽开始后临时锁定世界点击直到释放或取消。
- [相机跟随主角后，大地图下可能影响玩家对全局生产线的观察] → Mitigation: 保留 observer / workspace 入口和跟随开关，让相机既能跟随也能显式切回观察模式。
- [建筑物品化后，原有纯 HUD 建造面板的定位会变得模糊] → Mitigation: 第一版允许 HUD build palette 继续存在，但将其重定位为调试/授予建筑物品的工具，而非唯一建造入口。

## Migration Plan

1. 先引入主角场景/控制器、玩家状态模型和最小相机跟随能力，让 sandbox 与 mobile demo 都能加载主角占位。
2. 抽象玩家背包与快捷栏数据模型，并把现有库存 UI 组件提炼成可复用的跨容器槽位视图。
3. 扩展 `FactoryItemCatalog` 与相关枚举/定义，让建筑套件成为正式物品并补齐图标映射。
4. 将 static sandbox 的建造入口切换到“可放置热键栏物品”，并保留现有 build workspace 作为辅助授予/测试入口。
5. 扩展结构详情与玩家面板宿主，打通玩家背包 <-> 仓储 / 建筑库存的拖拽转移。
6. 最后改造 mobile demo 的顶层输入路由，让主角默认接管控制，再把移动工厂控制模式收束为显式上下文。
7. 更新 smoke / demo notes / HUD 提示，验证主角移动、相机跟随、背包窗口、跨容器拖拽和建筑放置主路径。

回滚策略：如果跨容器拖拽或 mobile demo 输入重构带来较大回归，可先保留主角移动与跟随镜头，以及玩家背包只读展示，再暂时回退“玩家直接放置建筑”和“mobile demo 顶层输入切换”，分阶段恢复完整功能。

## Open Questions

- 主角与建筑交互是否需要距离限制，还是第一版允许全图点击已知结构直接打开窗口？当前建议 sandbox 放宽距离限制，后续再补“靠近才可交互”。
- 建筑拆除后是否应直接返还为建筑套件物品到玩家背包，还是先只支持调试方式授予？当前建议把“拆除返还”留作后续增量，先聚焦放置与容器联动。
- mobile demo 中进入工厂命令上下文的方式，应该是靠近交互按钮、专属 HUD 按钮，还是二者都支持？当前倾向先提供 HUD 入口，避免和近距离交互门槛耦合。
