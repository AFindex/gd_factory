## Why

当前项目已经建立了“世界工业标准”和“舱内工业标准”的区分，但世界大包与舱内小包之间仍然只是同一物品换 `CargoForm` 的 1:1 切换，既没有真正的装箱清单语义，也没有把“大包不能进入舱内传送带”落成稳定规则。现在需要把这一层补实，否则解包/封包会继续像换皮节点，世界货物表现也会继续在 2D/3D、尺寸和运输语义上互相打架。

## What Changes

- 建立“世界大包 `WorldBundle` / 舱内小包 `CabinCarrier`”的正式转换契约，让解包和封包围绕装箱清单、数量比例和货包模板工作，而不是简单保留同一种 `ItemKind` 直通。
- 明确规定世界大包不能适配舱内普通传送带，只能停留在输入接口、解包舱、重载缓冲位、封包舱和输出接口等重载节点之间。
- 让世界侧货物统一改为“纯 3D 盒体 + 贴图”的重载表现，并关闭 shadow casting；舱内小包继续保持轻量的 2D/billboard 风格，强化两层物流标准的视觉断层。
- 重做解包/封包舱段的模型语言和运行表现，使其能根据目标世界大包的实际规格调整占位与舱体体量，并在工作中像机械臂/装卸机构一样展示正在处理的大包。
- 把解包逻辑从“1 个世界物体产出 1 个舱内物体”升级为“1 个世界货包按 manifest 拆成多份舱内小包”，同时为封包定义固定模板、类别模板和受控混装模板的边界。
- 更新 focused mobile demo、编辑预览、验证和说明文案，让玩家能直接读懂“大包交接层”和“小包带式物流层”是两套不同的运输语义。

## Capabilities

### New Capabilities
- `world-bundle-cabin-carrier-conversion`: 定义世界大包、舱内小包、装箱清单、解包/封包模板和重载交接节点之间的规则契约。

### Modified Capabilities
- `factory-item-visual-profiles`: 世界货物需要改成无阴影的 3D 贴图盒体，舱内小包继续保持 2D 轻量表现，并能按货包模板分流表现。
- `factory-structure-visual-profiles`: 解包/封包舱段和重载缓冲位需要根据世界大包规格调整占位、体量和处理动画，不再只是静态壳体。
- `factory-multi-cell-structures`: 解包/封包舱段需要支持按世界大包规格变化的内部占位与 footprint 规则。
- `mobile-factory-boundary-attachments`: 边界接口需要从普通端口语义升级为世界大包重载交接节点，禁止把大包直接表现为进入舱内料轨。
- `mobile-factory-interior-editing`: 编辑器、预览和提示需要区分重载大包节点与舱内小包物流层，并说明解包/封包目标模板和适配范围。
- `mobile-factory-demo`: mobile demo 需要演示世界大包通过重载交接进入解包舱、拆成多个舱内小包流动，再重新封包出舱的完整链路。

## Impact

- 受影响代码会集中在 `scripts/factory/FactoryItemVisuals.cs`、`scripts/factory/FactoryIndustrialStandards.cs`、`scripts/factory/FactoryStructureFactory.cs`、`scripts/factory/structures/CargoConversionStructures.cs`、`scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs`、`scripts/factory/MobileFactoryDemo.cs` 以及相关 smoke / map / authored content。
- 会新增或重构世界货包模板、manifest 配置、转换比例、封包模板约束，以及世界大包与舱内小包的双层表现规则。
- 会改变 focused mobile interior authored layout、边界挂点语义和 conversion chamber 占位方式，但不要求重写底层 belt 拓扑或 transport 核心。
