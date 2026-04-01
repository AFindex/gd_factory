## Why

当前移动工厂 demo 已经验证了 deploy / recall / redeploy 的机制，但世界侧仍然更像“在锚点间瞬移的概念样机”，没有把移动工厂表现成真正可移动、可转向、可部署的载具单位。与此同时，移动工厂和相机共享 WASD，导致玩家在操作移动工厂时缺少清晰的输入归属，也让“观察世界”和“指挥工厂”两种意图混在了一起。

## What Changes

- 为移动工厂补上更明确的世界态交互：区分移动态与部署态，并在移动态提供真实的移动与转向表现。
- 新增移动工厂命令模式与观察模式的切换交互，并提供明确的 HUD 按钮/提示来说明当前输入归属。
- 将移动工厂 demo 中的 WASD 默认改为控制移动工厂本体，只有进入观察模式时才恢复为控制世界相机。
- 将部署交互改为 RTS 风格的“放置建筑”流程：玩家进入部署指令后在地面预览落点与朝向，若目标合法则移动工厂自动前往、对齐朝向并完成部署。
- 保留当前内部编辑分屏能力，但让其与新的命令模式/观察模式并存，避免与世界侧控制冲突。
- 更新 demo 文案、提示和测试，使玩家能够理解“驾驶/部署/观察/编辑”之间的切换关系。

## Capabilities

### New Capabilities
- `mobile-factory-command-modes`: 定义移动工厂 demo 中的命令模式、部署模式、观察模式及其输入路由、HUD 状态和模式切换反馈。

### Modified Capabilities
- `mobile-factory-demo`: 将 demo 从固定锚点部署验证升级为可展示移动、转向、自动进场部署与观察切换的完整交互体验。
- `mobile-factory-lifecycle`: 扩展生命周期要求，使移动工厂在非部署状态下具备可见的世界移动/转向过程，并支持合法部署时的自动对位与朝向对齐。
- `factory-camera-and-input`: 调整移动工厂 demo 内的相机输入约束，使相机平移不再默认占用 WASD，而是仅在观察模式激活时接管移动输入。

## Impact

- 受影响代码主要集中在 [MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs)、[MobileFactoryInstance.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryInstance.cs)、[MobileFactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.cs) 与 [FactoryCameraRig.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryCameraRig.cs)。
- 需要补充或重写移动工厂 demo 的输入映射、状态机、部署预览、HUD 按钮和自动部署流程。
- 需要更新移动工厂相关 smoke test 与文档说明，确保新交互在 headless 校验之外也有清晰的行为契约。
