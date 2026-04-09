## Why

当前物流物体一多，传送带上的每个物品都会以独立 `Node3D` 形式参与更新与绘制，导致静态工厂场景在高吞吐下明显掉帧。现在需要把运输物体渲染从“逐物体节点绘制”升级为“可见区裁剪 + 分桶合批 + 实例化绘制”的整体方案，避免工厂规模一上来就被渲染成本拖垮。

## What Changes

- 为运输中的物流物体新增统一的高性能渲染管线，按视觉类型和材质分桶，优先使用实例化批渲染替代逐物体 `Node3D` 绘制。
- 为传送带、分流器、合并器等运输结构增加屏幕可见区驱动的渲染裁剪策略，只提交摄像机附近和可观察范围内的运输物体实例。
- 为运输物体引入近中远分级显示策略，保证近处保留可读性，远处降低几何和更新成本，必要时退化为更轻量的 billboard 或占位表现。
- 让运输模拟继续保持确定性，渲染层只消费运输状态快照，不再让每个运输物品都依赖独立场景节点生命周期。
- 为默认静态工厂 demo 补充高密度物流压力观察与回归验证，确保优化后在大量运输物体场景下仍保持物品可辨识和吞吐可观察。

## Capabilities

### New Capabilities
- `factory-transport-render-performance`: 定义运输物体在高吞吐场景下的裁剪、分桶、实例化与退化渲染规则。

### Modified Capabilities
- `factory-item-visual-profiles`: 扩展运输视觉配置，使 item profile 能参与实例分桶、近远距离退化和轻量回退渲染。
- `factory-production-demo`: 扩展静态工厂 demo 的性能观察与压力验证要求，确保高物流密度下仍可观察运输表现与性能状态。

## Impact

- 受影响代码预计集中在 `scripts/factory/structures/FlowTransportStructure.cs`、`scripts/factory/FactoryItemVisuals.cs`、`scripts/factory/FactoryDemo.cs`、`scripts/factory/FactoryCameraRig.cs` 以及相关 smoke/regression 测试。
- 需要新增运输渲染聚合层、实例批次管理和可见区统计数据，并调整现有运输结构把“渲染节点创建”改为“提交渲染快照”。
- 不引入新的外部依赖，但会改变运输物体的运行时渲染组织方式与性能观测方式。
