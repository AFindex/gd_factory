## Why

当前项目已经能把运行中的地图布局导出为 `nfmap`，也能持久化蓝图，但这些文件不会保留传送带在途物品、建筑内部库存和生产进度、敌人、玩家位置与背包等真实游玩状态。随着物流、战斗、玩家角色和移动工厂流程都已经具备持续演进的运行时状态，现在适合补上完整快照存档，让玩家能够真正中断并恢复一局进度。

## What Changes

- 增加独立于地图导出和蓝图持久化的运行时进度快照格式与持久化服务，用于保存和读取可恢复的游戏存档。
- 为静态工厂 demo 和聚焦移动工厂 demo 捕获并恢复当前会话中的地图布局与动态状态，包括建筑生命值、内部库存/配方/生产进度、传送带与边界附件上的在途物品及位置、玩家位置与背包/快捷栏状态、活动敌人以及敌人刷怪节奏。
- 提供共享的保存/读取入口与运行时存档槽位元数据，并在 HUD 中暴露明确的保存结果、读取结果和校验失败反馈。
- 通过现有地图重建流程先恢复世界/内部地图，再把动态快照重新注入到新建的运行时实体上，避免依赖场景树级别的原样序列化。

## Capabilities

### New Capabilities
- `factory-runtime-save-snapshots`: 保存并恢复当前工厂会话的完整运行时快照，包括活动站点、动态物流、玩家和敌对单位状态。

### Modified Capabilities
- None.

## Impact

- 受影响代码会集中在 [FactoryDemo.Persistence.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryDemo.Persistence.cs)、[MobileFactoryDemo.Persistence.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.Persistence.cs)、[FactoryPersistencePaths.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryPersistencePaths.cs)、[FactoryMapPersistence.cs](D:/Godot/projs/net-factory/scripts/factory/maps/FactoryMapPersistence.cs)、[SimulationController.cs](D:/Godot/projs/net-factory/scripts/factory/SimulationController.cs)、[FactoryPlayerController.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryPlayerController.cs)、[FactoryEnemyActor.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryEnemyActor.cs)、以及各类结构运行时状态所在的 `structures/` 目录。
- 需要新增一份 OpenSpec capability，用来约束快照文档、快照恢复顺序、存档槽位与失败回滚行为。
- 不会替换已有 `nfmap` 最小语义地图格式，但会新增一套运行时存档文档、DTO、校验和 HUD 交互路径。
