## Why

当前工厂物流要求传送带必须端对端连接，导致很多常见的 T 字并线布局只能依赖专用合流器，占格更大、绕线更多，也让已有地图和蓝图难以表达更紧凑的合流方案。与此同时，现有合流器仍然只有双入口，在允许皮带中段并入后会显得能力偏弱，因此需要同步升级为三入口合流器，让物流拓扑更自然且更接近玩家预期。

## What Changes

- 允许一段传送带从侧向或后向指向另一段传送带的中段，并把物品直接并入目标传送带，形成不需要端点直连的 T 字合流。
- 为传送带补充中段合流的拓扑识别、运行时路由和可视反馈，使被并入的目标段能继续向前输送而不是把中段输入视为断线。
- 将原有合流器从双入口单出口改为三入口单出口，保留单格建筑定位但支持来自后方、左侧和右侧的同时汇入。
- 调整建造校验、预览提示、地图连通性分析和 smoke/regression 覆盖，确保新物流拓扑在世界地图和移动工厂内部都被视为合法且可回归。

## Capabilities

### New Capabilities
- `factory-logistics-routing`: 定义传送带中段合流与三入口合流器的物流拓扑、接收规则和运行时转发契约。

### Modified Capabilities
- `factory-grid-building`: 更新建造校验与预览规则，使传送带可合法指向另一段传送带的中段，并让合流器预览表达三入口拓扑。
- `factory-map-headless-validation`: 更新地图连通性分析和聚焦诊断，使 headless 校验能够正确识别中段合流传送带和三入口合流器，不再把它们误报为断开。

## Impact

- 受影响代码主要包括 `scripts/factory/structures/BeltStructure.cs`、`scripts/factory/structures/MergerStructure.cs`、`scripts/factory/structures/FlowTransportStructure.cs` 以及相关拓扑重建逻辑。
- 建造和预览入口需要更新，包括静态工厂与移动工厂的放置校验、端口提示、文案和可能的 authored 样例布局。
- 地图校验与聚焦分析需要识别新的输入/输出关系，相关 smoke/regression 也需要覆盖中段合流和三入口合流器的持续输送行为。
