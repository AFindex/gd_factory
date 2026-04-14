## Why

移动工厂输入接口已经通过分步重构逐渐建立了清晰的阶段边界、单一视觉宿主和连续交接路径，但输出接口仍然更多停留在“封包舱给货后直接往世界侧放”的状态，缺少与输入侧同等级的阶段感和可读性。现在补齐输出接口的镜像动画合同，能把同一套重载交接原则同时应用在进舱和出舱链路上，避免后续继续围绕重复显示、提前出货、宿主切换跳变去打零碎补丁。

## What Changes

- 为移动工厂输出接口定义一套与输入接口对称的 staged animation 合同，明确 `CargoPacker -> 内缓存 -> 桥接 -> 外缓存 -> 世界释放` 的阶段顺序。
- 要求输出接口在每个阶段都维持单一视觉宿主，不允许同一件大包同时在封包舱、桥位、外缓存或世界带侧重复显示。
- 要求输出接口的桥接、缓存停留、世界释放节拍与真实逻辑接管边界对齐，避免“世界侧还没接货就先出现成品包”或“接口已经放手但旧宿主还残留”的表现。
- 要求输出接口的锚点、等待位和朝向规则与输入接口采用镜像思路，优先复用已经在输入口验证过的 phased rebuild 方法，而不是再叠一套独立特判。
- 补充 focused mobile demo 与 smoke 验证，使演示场景能够稳定观察输出链路的阶段推进、等待世界接货和单宿主行为。

## Capabilities

### New Capabilities
- `mobile-factory-output-port-animation`: 定义移动工厂输出接口从封包舱交接到世界侧释放的分阶段动画合同、单宿主规则和验收行为。

### Modified Capabilities
- `mobile-factory-boundary-attachments`: 输出类边界接口的 requirement 将补充镜像输入口的 staged handoff、缓存等待和世界释放可读性。
- `mobile-factory-demo`: focused mobile demo 的 requirement 将补充可观察的输出接口阶段动画与等待世界接货表现。

## Impact

- 主要会影响 `scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs` 中的输出接口阶段机、锚点解析和表现宿主切换。
- 需要同步核对 `scripts/factory/structures/CargoConversionStructures.cs` 中 `CargoPacker` 的交接/释放时序，确保输出口动画与封包舱放手边界一致。
- smoke 与 demo 相关验证会跟着扩展，以覆盖输出接口的阶段推进、单一可见宿主和世界释放等待场景。
