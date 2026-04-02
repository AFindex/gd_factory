## Why

当前移动工厂已经能在移动态和部署态之间切换，但它与外部世界的交互边界仍然过于临时：移动态下输出端口会继续“吃掉”物品而不是真实堵塞，端口只支持单一外输方向，也没有把“端口本身是一个需要占格、可扩展、可部署的结构”纳入玩法和表现。随着后续还会加入更多与外部世界交互的建筑，现在需要把移动工厂边界系统从一次性的桥接逻辑升级为可扩展的 attachment/port 体系。

## What Changes

- 修复移动工厂在移动态或未连接世界时输出端口持续回收物品的问题，使未激活的输出边界表现为真实阻塞，进而正确影响内部传送带与缓存。
- 为移动工厂新增来自外部世界的输入端口能力，使部署后的工厂既能向外输出，也能从世界侧接收物料。
- 将移动工厂端口重构为可建造、可扩展的边界模块，而不是写死在 profile 里的单个输出桥。
- 为边界模块引入明确的格子形状要求：端口/attachment 必须同时占据工厂内部格与外部世界格，形成跨边界的固定部署接口。
- 在部署时从端口主体延伸出连接到世界格子的可视化连接件，让玩家能看到端口是如何“伸出去”并接入世界线路，而不是只在地面上出现一个孤立标记。
- 将移动工厂与外部世界的交互统一收口到一套可扩展的边界 attachment 架构中，为后续非 I/O 端口的外部交互建筑预留相同的放置、校验、激活和表现机制。

## Capabilities

### New Capabilities
- `mobile-factory-boundary-attachments`: 定义移动工厂跨内外边界的可建造 attachment/port 模块，包括输入、输出、占格规则、部署激活和世界连接表现。

### Modified Capabilities
- `mobile-factory-lifecycle`: 调整部署边界行为，使未激活输出端口阻塞而不是吞物，并让部署校验/激活覆盖可旋转的 attachment 占格与外部连接单元。
- `mobile-factory-interior-editing`: 扩展内部编辑器，使玩家能查看、放置和扩展带有内外格约束的边界 attachment，并看到其当前外部连接状态。
- `mobile-factory-demo`: 更新 demo，使其展示双向世界交互、真实端口连接件，以及部署后通过 attachment 与世界网格建立连续物流边界的表现。

## Impact

- 主要影响 `scripts/factory/` 下的移动工厂生命周期、内部站点、部署预览、网格预留和跨边界物流桥接逻辑，尤其是 `MobileFactoryInstance.cs`、`MobileFactorySite.cs`、`GridManager.cs`、`SimulationController.cs` 与 `structures/MobileFactoryPortBridge.cs`。
- 需要引入新的边界模块定义、可视化连接节点和编辑器表现，并调整 demo 场景中移动工厂与外部传送带的 authored content。
- 会改变当前“未部署时输出端口自动内部回收”的演示语义，相关 HUD、提示文案和测试基线需要一起更新。
