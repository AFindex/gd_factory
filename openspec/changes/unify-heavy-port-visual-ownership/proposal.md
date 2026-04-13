## Why

当前移动工厂的重载输入/输出链路虽然已经有世界侧缓存、桥接和舱内缓存的逻辑分层，但运行时表现仍由世界侧接口 root、舱内接口结构和缓存/桥接锚点分别决定是否显示同一件大包，导致重复显示、时序错位和“belt 还没扣货但接口已经出现一份货物”的观感问题。现在必须把这层收敛成单一 visual owner 合同，否则继续微调阶段阈值、滑移速度或缓存停顿只会不断叠加补丁，无法把体验拉回可读状态。

## What Changes

- 将移动工厂重载输入/输出的大包表现重构为单一 visual owner 模型：同一件世界大包在任意时刻只能由一套运行时显示节点负责呈现。
- 移除世界侧接口 root 与舱内接口 root 对同一件重载货物的并行显示职责，改为由重载接口结构统一驱动整条交接路径的货物本体。
- 将重载输入链和输出链的显示路径重定义为单轨 staged path，覆盖世界带接驳点、世界侧缓存位、桥前就位、穿壳桥位、舱内缓存位和转换舱交接位。
- 调整 belt -> 重载接口、重载接口 -> 解包舱 / 封包舱之间的 ownership 切换时机，确保世界带扣货、接口持有、桥接显示和转换舱接管在逻辑与视觉上同步。
- 保留现有重载缓存和握手规则，但把“谁负责画货物”从多层独立判断改成显式的单点决策。
- 更新 focused demo、smoke 和相关状态提示，验证不会再出现同一件货物在世界侧、桥位和舱内侧同时闪现或错位重复显示。

## Capabilities

### New Capabilities
- `heavy-port-visual-ownership`: 定义移动工厂重载大包在世界带、边界接口、桥接路径和转换舱之间的单一 visual owner 与 staged path 合同。

### Modified Capabilities
- `mobile-factory-boundary-attachments`: 重载输入/输出 attachment 的 requirement 将从“显示世界侧缓存与桥接状态”升级为“以单一 owner 驱动完整重载交接显示”。
- `factory-structure-visual-profiles`: 重载接口与转换舱的视觉 requirement 将改为消费统一的大包显示所有权，而不是各自独立判断是否显示货物本体。
- `mobile-factory-demo`: focused demo 的 requirement 将改为能清晰展示单一大包从世界带进入接口、穿壳、进入解包/封包交接位的连续过程，并避免重复显示。

## Impact

- 主要影响 [MobileFactoryBoundaryAttachmentStructure.cs](/D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs)、[MobileFactoryInstance.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryInstance.cs)、[FlowTransportStructure.cs](/D:/Godot/projs/net-factory/scripts/factory/structures/FlowTransportStructure.cs) 以及重载转换相关结构脚本。
- 需要同步调整 world attachment visual root 与舱内结构 visual root 的职责边界，避免多处同时生成或保留同一件大包的显示节点。
- focused demo / smoke / inspection 提示会跟着更新，但不会引入一套新的世界物流模拟，也不会重写普通 belt 拓扑。
