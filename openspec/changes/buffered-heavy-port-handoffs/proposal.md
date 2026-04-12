## Why

当前移动工厂已经区分了世界大包和舱内小包的视觉尺度，但边界输入/输出接口仍然更像普通 transport 节点：大包只是“经过接口”，没有形成世界侧缓存、舱内侧缓存、连续桥接和与解包/封包舱的显式交接节拍。这会让世界带到舱内处理位之间缺少工业装卸感，也会继续诱发重复显示、瞬移感和处理过程不连续的问题。

现在正适合把这一层补成正式规则，因为世界大包 / 舱内小包、重载接口、解包/封包舱和舱内布局已经初步成型。如果不在这时把“重载装卸机”语义立住，后续再叠视觉和玩法时会越来越依赖临时补丁，体验和代码都会变得更脆。

## What Changes

- 把输入/输出接口从普通边界端口升级为“重载装卸机”，明确支持世界侧缓存 `1` 个大包、舱内侧缓存 `1` 个大包，以及桥接中的单件重载转运。
- 为输入方向定义连续交接链路：世界传送带无缝接入世界侧托盘，经桥接进入舱内托盘，再与解包舱做握手，只有在解包舱空闲并接货时才继续推进。
- 为输出方向定义镜像链路：封包舱完成大包后先放入舱内侧输出缓存，再经桥接移动到世界侧输出缓存，最后无缝释放到世界传送带。
- 要求接口转运的大包在整条链上保持同一世界尺度，不在接口、桥接或舱内缓存窗口里做额外缩放。
- 要求解包/封包舱在处理世界大包时保留完整大包模型，展示夹持、定位、处理和退场节拍；解包完成后再通过淡出或拆壳退场，随后才吐出舱内小包。
- 为编辑器、demo 和运行时提示增加“重载缓存位 / 桥接位 / 处理位”的状态表达，让玩家能读懂接口正在等货、转运、等待处理或等待外部接货。

## Capabilities

### New Capabilities
- `buffered-heavy-port-handoffs`: 定义重载输入/输出接口的双缓存、桥接转运、与解包/封包舱握手以及连续交接表现。

### Modified Capabilities
- `mobile-factory-boundary-attachments`: 边界接口需要从普通过舱端口升级为显式的世界侧缓存、桥接位和舱内侧缓存节点。
- `factory-structure-visual-profiles`: 输入/输出接口、解包舱和封包舱需要增加重载装卸机风格的连续表现、缓存位和处理中大包展示。
- `factory-multi-cell-structures`: 重载接口与转换舱的占位和交接边需要支持双缓存与重载处理布局，不再只是单段 through-lane。
- `mobile-factory-demo`: demo 需要展示世界传送带到接口、接口到解包舱、封包舱到接口再回到世界带的完整无缝链路。
- `mobile-factory-interior-editing`: 编辑器预览、详情和提示需要说明接口缓存状态、桥接状态以及解包/封包目标交接关系。

## Impact

- 受影响的实现主要会落在 [scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs](/D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs)、[scripts/factory/structures/CargoConversionStructures.cs](/D:/Godot/projs/net-factory/scripts/factory/structures/CargoConversionStructures.cs)、[scripts/factory/structures/MobileFactoryPortBridge.cs](/D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryPortBridge.cs)、[scripts/factory/FactoryStructureFactory.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryStructureFactory.cs) 以及相关 demo / smoke / map 配置。
- 这次变更会新增一套明确的重载接口状态机和缓存槽位语义，但不要求重写底层 belt 拓扑或站点抽象。
- 视觉上会进一步强化“世界大包装卸层”和“舱内小包带式物流层”的分层，相关运行时提示、demo 展示和验证也需要同步更新。
