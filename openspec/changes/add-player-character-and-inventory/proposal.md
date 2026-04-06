## Why

当前工厂 sandbox 和移动工厂 demo 都以“相机/模式”作为交互起点，缺少一个统一的玩家载体，导致背包、个人物品栏、建筑收纳与放置、以及与仓储/建筑插槽联动这些核心生存建造交互没有落脚点。现在引入主角概念，可以把两个体验统一到同一套可扩展的人物控制与物品交互基座上，为后续拾取、建造、角色成长和世界操作建立稳定入口。

## What Changes

- 新增一个跨静态 sandbox 与移动工厂 demo 共享的主角能力：主角以胶囊体占位进入场景，支持 `WASD` 移动、相机跟随，以及作为默认交互焦点。
- 为主角新增背包系统与底部热键栏，提供类似 MC 的快捷物品栏，并增加可独立开关的背包、物品、个人属性等面板。
- 允许主角背包与仓储、生产建筑和其他带插槽容器之间进行拖拽转移、堆叠合并与双向移动，统一容器交互规则。
- 将可建造建筑拓展为可收纳进背包的物品形态，补齐对应图标/显示信息，并允许玩家从热键栏或背包中选择后通过左键放置到世界。
- 调整 sandbox 与移动工厂 demo 的输入与 HUD，使“控制主角”“打开人物面板”“查看建筑详情”“从物品栏放置建筑”可以并存，而不是互相覆盖。
- 保留现有建筑详情和移动工厂能力，但把它们接入主角主导的交互流程。

## Capabilities

### New Capabilities
- `factory-player-character`: 定义主角实体、玩家背包与热键栏、独立人物面板、跨容器拖拽规则，以及建筑物品化后的持有与放置体验。

### Modified Capabilities
- `factory-camera-and-input`: 将主角控制与相机跟随纳入输入契约，并明确 UI 悬停/面板交互时如何屏蔽世界放置与移动输入。
- `factory-grid-building`: 将建筑放置的起点从纯 HUD 选型扩展为玩家背包/热键栏中的建筑物品，并约束左键放置行为与交互模式切换。
- `factory-storage-and-inserters`: 扩展库存交互要求，使玩家背包能够与仓储和建筑插槽进行统一的拖拽、转移与堆叠联动。
- `factory-structure-detail-panels`: 扩展独立面板体系，使结构详情、玩家背包、物品信息与人物属性都能作为独立窗口协同显示。
- `mobile-factory-command-modes`: 调整移动工厂 demo 的默认输入入口，使主角成为基础控制对象，而工厂命令/观察/部署成为显式切换出来的上下文。
- `mobile-factory-demo`: 将移动工厂 demo 的默认交互入口调整为主角角色视角，并保留移动工厂相关演示能力。

## Impact

- 受影响代码会集中在 [FactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryDemo.cs)、[MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs)、[FactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryHud.cs)、[MobileFactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.cs)、[FactoryCameraRig.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryCameraRig.cs)、[FactoryPlacement.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryPlacement.cs) 与库存/结构明细相关脚本。
- 需要新增主角运行时数据、玩家库存 UI、建筑物品定义与图标映射，并调整现有结构库存与详情面板的数据绑定方式。
- 需要补充 sandbox 与 mobile demo 的交互验证，覆盖主角移动、面板开关、跨容器拖拽、建筑放置与镜头跟随等关键流程。
