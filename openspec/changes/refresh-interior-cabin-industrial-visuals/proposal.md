## Why

`define-dual-industrial-standards` 已经把世界标准和舱内标准在规则、目录和货物工况上拆开了，但移动工厂内部的大部分建筑、接口和物流件仍然沿用世界侧的外形语言，只是换了名称或轻量装饰。现在需要把“内部不是外部缩小版”真正落到模型和表现层，否则玩家进入舱内时仍会把它理解成微缩世界工厂，前面建立的工业标准差异也难以被直观看懂。

## What Changes

- 为移动工厂舱内建立一套独立的视觉语言，明确哪些结构属于供料槽道、模块舱、检修面、缓存柜、炮井和壳体接口。
- 调整内部专用建筑的模型表现，让解包机、封包机、缓冲槽、内部精炼/制造模块、电力节点、仓储和炮塔硬点都呈现为舰载/车载模块，而不是世界建筑缩小版。
- 调整舱内货物与物流载体的表现，让 `InteriorFeed` 及其衍生载体表现为料盒、卡匣、供料盒、弹仓、抽屉仓等舱内工况，不再复用世界散装/封装货物轮廓。
- 更新边界接口、预览、编辑器提示和演示场景中的舱内表现，使输入/输出接口、内部预览和货物流转在视觉上清楚区分“壳体接口”和“舱内物流层”。
- 扩展可视化验证与 smoke 覆盖，确保后续不会再把世界侧外形误带回舱内结构或舱内货物表现中。

## Capabilities

### New Capabilities
- `interior-cabin-visual-language`: 定义移动工厂舱内工业对象的统一视觉语汇、结构分层和类别约束。

### Modified Capabilities
- `factory-item-visual-profiles`: 舱内货物工况需要解析为舱内专用载体与轮廓，而不是继续借用世界货物表现。
- `factory-structure-visual-profiles`: 舱内建筑的模型和视觉层次需要体现嵌入式模块、检修面和槽道语义。
- `mobile-factory-interior-editing`: 内部编辑预览、标签和面板需要和新的舱内模型语言保持一致，帮助玩家读懂维护空间。
- `mobile-factory-boundary-attachments`: 壳体边界接口需要在视觉上表现为标准转换接口和舱体硬连接，而不是世界侧端口的缩比复用。

## Impact

- 受影响代码集中在 `scripts/factory/structures/`, `scripts/factory/FactoryItemVisuals.cs`, `scripts/factory/MobileFactoryDemo.cs`, `scripts/factory/MobileFactoryHud*.cs`, `scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs` 以及相关 smoke / validation 支撑。
- 会影响移动工厂内部的建筑模型、货物可视化、边界接口表现、编辑器预览与提示文案。
- 不改变底层物流拓扑、放置规则和 cargo-form 基础数据结构，但会要求演示地图和验证逻辑对新的舱内专用表现具备回归保护。
