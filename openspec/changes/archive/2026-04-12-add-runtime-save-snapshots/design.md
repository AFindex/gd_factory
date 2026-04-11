## Context

当前持久化能力分成两类：一类是 [FactoryMapPersistence.cs](D:/Godot/projs/net-factory/scripts/factory/maps/FactoryMapPersistence.cs) 导出的 `nfmap` 地图文档，它保留当前地图边界、矿点、建筑布局、配方和少量 seed item；另一类是 [FactoryBlueprintPersistence.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryBlueprintPersistence.cs) 保存的蓝图。两者都不会保留 `SimulationController` 中维护的动态运行时状态，例如在途物流物品、敌人、刷怪节奏、玩家背包与位置，以及结构子类里各自维护的库存、血量和生产进度。

这个缺口现在已经足够明显，因为仓库里的 demo 不再只是静态搭景：玩家角色、背包、战斗、移动工厂世界/内部双站点、边界附件物流和配方生产都已经形成连续会话。要做到“真正的保存进度”，不能直接把当前场景树整体序列化；更合适的方式是复用现有 map 重建能力，另外叠加一个只记录语义化运行时差异的快照层。

## Goals / Non-Goals

**Goals:**
- 为静态工厂 demo 和聚焦移动工厂 demo 增加可保存、可读取的运行时快照存档。
- 保留当前会话里的地图布局、结构生命值、结构内部库存/配方/进度、传送带和附件在途物品、玩家状态、活动敌人与刷怪进度。
- 通过现有 map loader 重建站点，再以显式 hydration 流程恢复动态状态，避免依赖场景树噪音。
- 把运行时进度存档与现有 `nfmap` 地图导出、蓝图持久化分开，避免污染工程内源数据。

**Non-Goals:**
- 不保存选中结构、悬停格子、打开面板等纯 UI 临时状态。
- 不在第一版支持自动云同步、多用户/联机同步或跨设备冲突解决。
- 不要求保留纯瞬时特效，例如攻击 tracer、hover 特效或正在飞行的投射物；这些可以在恢复后的下一次仿真中重新生成。

## Decisions

### 使用独立的 `nfsave` 运行时快照文档，而不是扩展现有 `nfmap`

`nfmap` 的职责已经在 `factory-map-data-format` 里被定义为“最小语义地图格式”，目的是重建布局，而不是记录会话内每个动态对象的瞬时状态。如果把 belt progress、玩家背包、敌人波次等内容继续塞进 `nfmap`，会模糊 authored map 和 savegame 的边界，也会让当前 map validator 承担并不属于它的职责。

因此第一版会新增独立的保存文档与 DTO，例如顶层 `FactorySaveSnapshotDocument`，并放到 `user://factory-runtime/saves/` 一类的运行时目录中。文档会显式声明 schema version、slot metadata、保存时间和站点列表；每个站点既包含一份捕获出的 map document，也包含该站点的动态 runtime state。

备选方案是直接在现有 `nfmap` 里新增可选节。这个方案会破坏“地图文档只保存重建所需最小语义”的原则，因此不采用。

### 采用“站点重建 + 运行时 hydration”的两阶段恢复流程

恢复不会尝试还原之前的 Godot 节点树，而是分成两个阶段：
- 第一步：从快照里内嵌的 world/interior map document 调用现有 map loading 入口，重建保存时的建筑布局与地图边界。
- 第二步：对重建出来的 player、structure、transport、enemy、combat director 等参与者重新注入动态状态，然后重建 topology、刷新 HUD 和继续 simulation。

这样可以最大化复用现有 [FactoryMapPersistence.cs](D:/Godot/projs/net-factory/scripts/factory/maps/FactoryMapPersistence.cs) 与 map runtime loading 约定，也能避免直接序列化 `Node3D`/`MeshInstance3D` 之类噪音字段。为了匹配重建后的实体，快照会使用稳定的语义键，例如 `siteId + rootCell + kind + facing` 来定位结构，敌人和玩家则用各自的角色记录定位。

备选方案是直接把整棵场景树或 `PackedScene` 运行时实例序列化回磁盘。这个方案实现快，但对版本演进、文件差异和校验都很脆弱，因此不采用。

### 为结构、玩家、敌人与战斗系统增加显式快照参与接口

当前动态状态分散在很多地方：`FactoryPlayerController` 保留玩家背包和快捷栏状态，`FlowTransportStructure` 保留 `_items` 里的在途物品和进度，`FactoryEnemyActor` 保留生命值、路径推进和攻击冷却，`FactoryCombatDirector` 保留各 lane 的刷怪索引和下一次生成倒计时，结构子类还各自保留库存、配方和生产进度。统一保存它们最稳妥的方式，是增加一组显式的快照参与接口，而不是在中央 serializer 里对所有具体类型做条件分支。

第一版会把通用状态拆成几层：
- `FactoryStructure` 级通用状态：生命值、是否摧毁、通用 inventory helpers、语义键。
- `FlowTransportStructure` 级物流状态：在途 item 列表、来源/目标 cell、lane key、progress。
- 结构子类特有状态：当前 recipe、内部 buffer、生产进度、炮塔弹药等，通过结构级接口补充。
- 角色/战斗状态：玩家位置与背包、敌人血量与路径进度、combat director 的 lane timers/spawn index。

备选方案是用反射扫描字段或把整个对象 dump 成字典。这个方案对版本兼容和测试都不友好，因此不采用。

### 快照需要保留 item identity，并在恢复后继续分配不冲突的新 item id

当前 `SimulationController.CreateItem()` 使用 `_nextItemId` 递增分配新 item。若快照恢复时不保留 item id，那么结构库存、在途物流和玩家背包中的同一件物品会在加载后被重新编号，容易让 UI 签名、调试信息和后续增量逻辑失去连续性。

因此快照文档会记录 item 的 `id`、`sourceKind` 和 `itemKind`。恢复流程需要允许按指定 id 构造物品，或者在加载后把 `_nextItemId` 推进到快照中最大 item id 之后，确保后续新生成的 item 不会与旧记录冲突。

备选方案是完全忽略历史 item id，只恢复堆叠结果。这样实现更简单，但会让 belt/背包/结构内部状态的精确恢复和后续调试变差，因此不采用。

### Save/load 入口挂到现有 persistence HUD，但与地图导出按钮分区

仓库里已经有专门的 persistence HUD 区块，用于“导出运行时副本”和“保存到当前地图源”。运行时快照的用户心智与它相近，但语义完全不同：地图导出是写布局文档，savegame 是存档槽位。因此第一版会复用现有 persistence HUD 的承载位置，但新增独立的“进度保存/读取”分区、命名输入和槽位状态文案，而不是复用现有 map save 按钮。

这样做可以让现有测试/演示入口仍然可用，也不会误把真实游玩进度写回 `res://data/...`。第一版只支持运行时目录下的 save slots，不提供“保存到当前工程源”的对应动作。

备选方案是把 save/load 完全做成新的主菜单或单独窗口。那个方向后续可以再做，但对当前需求来说改动面更大，因此不采用。

## Risks / Trade-offs

- [结构运行时字段分散在很多子类里，容易漏掉某些生产或战斗状态] -> Mitigation: 先定义统一参与接口与默认 helper，再按结构类别列出需要覆盖的状态清单，缺失时在加载日志里给出明确 warning。
- [用结构语义键匹配快照时，如果加载后的布局与快照不一致，可能导致 hydration 目标缺失] -> Mitigation: 在清空当前会话前先完整校验 map document 和 runtime record，并在 mismatch 时终止加载。
- [大量在途物品和库存会让 JSON 文档变大、加载时产生卡顿] -> Mitigation: 保持文档只记录语义字段，不保存渲染数据，并把恢复分成 validate/build/hydrate 三步以便显示明确状态。
- [敌人只恢复实例而不恢复刷怪计时，会导致读档后波次错位] -> Mitigation: 把 `FactoryCombatDirector` 的 lane spawn index 和 countdown 也纳入快照，而不只保存当前场上的敌人。
- [不保存投射物会让极个别瞬时战斗画面在读档后略有差异] -> Mitigation: 明确列为第一版非目标，并保证敌人、结构生命值和刷怪节奏保持一致，使战局在下一 tick 后继续合理推进。

## Migration Plan

1. 新增 save snapshot 文档模型、路径约定、slot metadata 和读写/校验入口，与现有 map export 路径并存。
2. 为 world demo 和 mobile demo 组装“站点 map capture + runtime capture”流程，并让加载流程能够先重建站点再恢复动态状态。
3. 为 `FactoryStructure`、`FlowTransportStructure`、`FactoryPlayerController`、`FactoryEnemyActor` 和 `FactoryCombatDirector` 增加显式 capture/restore 逻辑。
4. 在 HUD 中增加命名存档与读取动作，并对失败、成功和不支持版本提供用户可见反馈。
5. 通过 smoke/manual 回归验证 belt 物品、库存、玩家位置、敌人状态和移动工厂双站点读档结果。

回滚策略：如果运行时快照的 hydration 在短期内不稳定，可以隐藏 save/load UI，并保留现有 map export 与 blueprint persistence；新增的 save slot 目录和文档不会影响已有 `nfmap` 或 `res://data` 资产。

## Open Questions

- 第一版是否需要同时保存“最近一次使用的存档槽位”并支持一键快速覆盖，还是只先支持显式命名保存？
- 移动工厂 demo 中是否还需要额外保存移动工厂本体在世界中的部署/展开状态，如果当前内部站点快照不足以完全重建？
- 对损坏但可部分解析的存档，是只给出拒绝加载，还是允许未来做“尽力恢复并标记告警”的调试入口？当前设计优先拒绝部分恢复。
