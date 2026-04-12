## Why

当前普通工厂 demo 和移动工厂 demo 虽然已经具备真实生产链与编辑能力，但调试新物流、配方、电力或 UI 交互时，仍然需要先搭完整供料与供电链，导致回归验证和玩法迭代成本偏高。与此同时，移动工厂 demo 的 HUD 在上一次重构后仍保留了一些冗余层级，编辑操作面板和总览顶部 chrome 之间的职责还不够收敛，影响调试效率。

## What Changes

- 为世界模式与内部编辑模式分别补充一组调试用建筑/舱段，支持无成本持续产出多种测试物品，便于快速验证运输、加工、缓存、装卸、炮塔补给和蓝图流程。
- 新增一个无成本、永久运行的测试发电机，用于稳定给调试布局供电，不再要求玩家先铺设燃料链才能验证下游系统。
- 让普通工厂 demo 和移动工厂 demo 都能在各自建造分类中访问这些调试建筑，且命名、分类和用途对齐当前工业标准。
- 精简移动工厂 demo 的编辑操作面板，只保留建造分类与建造入口，不再在该面板顶部重复显示总览性信息。
- 将移动工厂顶部工作区面板并入“移动工厂总览”面板，使顶部区域成为总览 panel 的 tab 页签区，减少 HUD 上层重复 chrome。
- 为移动工厂总览 panel 右侧增加显式折叠按钮，支持向左侧滑隐藏与向右侧滑显示，便于在观察世界和调试布局之间快速切换。

## Capabilities

### New Capabilities
- `factory-debug-buildings`: 定义世界与舱内调试建筑/舱段，包括无成本物品生成器与永久测试发电机的行为、分类、可放置站点和调试用途。

### Modified Capabilities
- `factory-production-demo`: 静态工厂 demo 的建造分类需要暴露调试建筑，并允许玩家快速搭建零成本调试链路。
- `mobile-factory-demo`: 聚焦移动工厂 demo 的总览 HUD、顶部 tab 集成、总览侧滑隐藏以及世界模式下的调试建筑入口。
- `mobile-factory-interior-editing`: 内部编辑模式需要在建造分类中加入调试舱段，并把编辑操作面板收敛为以建造分类为核心的最小面板。

## Impact

- 主要影响 [FactoryIndustrialStandards.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryIndustrialStandards.cs)、[FactoryTypes.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryTypes.cs)、相关结构实现与建造目录/显示文案。
- 主要影响 [FactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryDemo.cs)、[FactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryHud.cs)、[MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs)、[MobileFactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.cs) 与 [MobileFactoryHud.Workspaces.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.Workspaces.cs)。
- 需要补充相应 spec deltas 与 smoke / regression 覆盖，确保调试建筑不会破坏现有真实生产链默认体验，同时验证移动工厂 HUD 的新折叠与 tab 集成行为。
