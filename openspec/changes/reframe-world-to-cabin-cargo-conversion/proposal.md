## Why

当前项目虽然已经区分了世界标准和舱内标准，但“世界货物比舱内货物流更大”这件事还没有被真正落实成稳定的玩法与表现契约。结果就是解包/封包/缓冲模块看起来像普通小设备，舱内料轨上的物品也仍然像世界货物缩小版，既削弱了转换建筑存在的必要性，也让“内部不是缩小版世界工厂”这个方向重新变得含糊。

## What Changes

- 明确建立“世界大件货物”与“舱内标准载具/供料单元”之间的尺度断层，要求世界侧货物在视觉和功能上都不能直接进入舱内料轨。
- 明确规定世界大件货物即使出现在内部工厂的边界交接区、转换舱或缓冲位中，也不再缩放为“舱内适配尺寸”。
- 重定义解包机与封包机的功能与空间身份：它们是处理单个世界大件的一进一出转换舱，而不是多口小型物流节点。
- 要求解包/封包模块在工作时能在模型上展示正在处理的世界货物或舱内载具，让玩家直接看见“转换正在发生什么”。
- 重新定义缓冲模块的职责与表现，使其服务于大件转换节拍整理，而不是和解包/封包共享过多“加工设备”语言。
- 调整舱内模块、接口和货物表现，使舱内工业模块的体量接近世界大件货物，而舱内料轨只承载转换后的内部小载具。
- 更新 demo、编辑预览与验证，确保世界大件 -> 解包 -> 舱内载具 -> 封包 -> 世界大件这一链条在规则和视觉上都成立。

## Capabilities

### New Capabilities
- `world-to-cabin-cargo-conversion`: 定义世界大件货物与舱内标准载具之间的一进一出转换契约、处理节拍与可见载荷表现。

### Modified Capabilities
- `factory-item-visual-profiles`: 货物表现需要明确区分世界大件与舱内小载具两种尺度，不再允许仅靠缩放伪装转换。
- `factory-structure-visual-profiles`: 解包机、封包机、缓冲模块和舱内生产模块的体量与轮廓需要围绕“大件转换舱 + 小载具物流”重构。
- `mobile-factory-boundary-attachments`: 边界接口需要与大件输入/输出语义一致，突出世界大件只能在接口与转换舱之间交接。
- `mobile-factory-interior-editing`: 内部预览、标签和提示需要向玩家说明哪些对象处理世界大件，哪些对象只承载内部小载具。
- `mobile-factory-demo`: focused / dual-standards 案例需要能清楚演示世界大件经转换舱进入舱内后被拆成小载具流动的过程。

## Impact

- 受影响代码主要包括 `scripts/factory/FactoryItemVisuals.cs`、`scripts/factory/structures/CargoConversionStructures.cs`、`scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs`、舱内模块结构类、`scripts/factory/MobileFactoryDemo.cs` 和相关 smoke / validation。
- 会改变解包/封包/缓冲模块的接口语义、可见载荷表现、舱内模块尺度关系，以及世界与舱内货物的视觉契约。
- 不要求改写底层 belt 拓扑或 cargo form 基础结构，但会要求转换建筑、边界接口和 demo 配置体现新的单件转换节拍与尺寸逻辑。
