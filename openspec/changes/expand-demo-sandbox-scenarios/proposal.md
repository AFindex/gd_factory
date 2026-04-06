## Why

当前 demo 与移动工厂场景里仍有大量依赖 `Producer` 这类兼容测试建筑的流水线，导致矿物、配方、电力、防御和接收站之间的真实闭环没有被完整演示，也让 sandbox 用例更像回归占位线而不是可扩展的玩法样板。现在需要把这些 demo 用例升级为围绕真实采矿、加工、供电、补给与防御构成的完整循环，既便于继续开发，也让后续 smoke / sandbox 验证覆盖真实系统。

## What Changes

- 移除静态工厂 demo、移动工厂 demo 和大型移动工厂场景中依赖 `Producer`/测试建筑的主流水线与回归样板，改为使用真实的采矿机、熔炉、组装机、弹药组装器、发电机、接收/输出链路和地图建筑。
- 扩展 demo 用例所需的矿物与配方覆盖范围，让地图存在更密集、更丰富的矿区，并让主世界与移动工厂内部都能围绕这些矿物形成多阶段生产链。
- 补齐按“采矿、配方加工、电力维护、防御补给、接收站周转、地图建筑互动”划分的 authored sandbox case studies，让静态世界和移动工厂内外都能展示完整循环。
- 将移动工厂重点调整为“富矿地图 + 多接收站 + 多工厂内部加工”的 sandbox 结构，强调世界矿物采集、边界端口交换、内部工厂加工、再向外部网络输出或补给的往返闭环。
- 更新 smoke / sandbox 验证目标，使其验证真实生产链、防御补给链和移动工厂端口循环，而不再依赖测试建筑直接产物。

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `factory-production-demo`: 默认静态 sandbox 必须改为由真实采矿、配方加工、电力、防御和接收站链路组成的完整 authored 用例，并移除测试建筑主流水线。
- `factory-manufacturing-chain`: 配方目录需要扩展到支撑更多矿物家族与多阶段 demo 循环，确保地图与工厂内部的生产链不再依赖兼容占位产物。
- `factory-resource-extraction`: 资源开采能力需要覆盖更丰富的矿物配置、富矿区布局，以及移动工厂在矿区与接收站之间的采集输入预期。
- `factory-power-grid`: demo 中的供电链不仅要让机器启动，还要展示维护、补电、断供恢复和跨区供电的 authored 用例。
- `factory-tower-defense`: 防御案例需要改为由真实弹药生产与物流补给支撑，并允许出现补给成功与补给失败两种可观察结果。
- `mobile-factory-demo`: 聚焦移动工厂 demo 的世界地图、接收站、边界端口和内部加工链，要求围绕真实矿物流转构建 sandbox 用例而不是测试建筑直出。
- `mobile-factory-test-scenario`: 大型移动工厂场景需要布置大量矿物、多个接收站、多个角色不同的移动工厂，并让其内外循环都使用真实工厂链路。
- `mobile-factory-interior-editing`: 预制内部案例需要从测试建筑拓扑切换为真实工厂结构拓扑，确保玩家打开任意内部工厂时看到的是可运行的 sandbox 生产/补给布局。

## Impact

- 主要影响 `scripts/factory/FactoryDemo.cs`、`scripts/factory/MobileFactoryDemo.cs`、`scripts/factory/MobileFactoryScenarioLibrary.cs`、`scripts/factory/FactoryStructureRecipes.cs`、`scripts/factory/FactoryResources.cs`、`scripts/factory/FactoryItemVisuals.cs` 与相关 HUD / workspace 组织代码。
- 需要重新审视 `BuildPrototypeKind.Producer` 在静态与移动场景、测试 workspace、预制布局和 smoke 验证中的职责，避免 demo 主路径继续依赖 legacy 占位建筑。
- 会修改多个现有 OpenSpec capability 的 requirement / scenario，以把“真实闭环 sandbox 用例”定义为规范行为，而不是当前的实现细节。
