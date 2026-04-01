## Why

当前 `factory_demo.tscn` 仍然偏向“最小可运行样例”，开场线路数量有限、HUD 占屏偏大，难以稳定暴露复杂拓扑、长链路传送和持续阻塞时的回归问题。现在正适合把它升级成更高压的静态测试场景，并补上运行期 profiler 可视化与分流器阻塞修复，让后续物流迭代有更可靠的观察基线。

## What Changes

- 扩展 `factory_demo.tscn` 的默认开场布局，加入更多复杂拓扑、长距离传送带、交叉回路、分流回汇和高吞吐压力段，让场景在启动后就能持续施压物流系统。
- 重做 `FactoryHud` 的布局密度与信息层级，使 HUD 主要占据屏幕约五分之一的可视区域，同时保留建造、预览、吞吐和帮助信息。
- 在 HUD 中加入轻量 profiler 统计，至少持续显示帧率、帧时间，并暴露物流模拟/场景更新中的主要热点指标，方便观察性能退化。
- 修复分流器的阻塞传播 bug：当一个出口被堵住时，只要另一个出口仍可接收，分流器仍应继续把物流送往可用出口，而不是整体停摆。
- 为静态工厂 demo 补充能覆盖复杂默认拓扑、HUD profiler 信息和分流器阻塞回退行为的 smoke/regression 验证点。

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `factory-production-demo`: 扩展静态工厂 demo 的默认测试布局、HUD 反馈和物流分流行为，使其能够作为更高压力的运行与回归观察场景。

## Impact

- 受影响代码主要集中在 `scenes/factory_demo.tscn`、`scripts/factory/FactoryDemo.cs`、`scripts/factory/FactoryHud.cs`、`scripts/factory/SimulationController.cs` 与 `scripts/factory/structures/SplitterStructure.cs`。
- 需要为默认 demo 的场景 authoring、运行时 HUD、性能统计采样和物流分发逻辑建立更明确的行为约束。
- 预计会影响默认静态 demo 的 smoke test 断言与文档说明，但不会引入新的外部依赖或 breaking API。
