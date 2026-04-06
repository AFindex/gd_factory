## Why

当前工厂建造体验在几个高频操作上出现了不一致：多格建筑在蓝图应用预览时会发生错位，组装机与弹药组装机的三进三出端口没有稳定分布在机体两侧，输入输出端口提示在非传送带预览时也会持续干扰视图，而成功摆放后立刻退出连续建造也让批量搭建效率偏低。随着多格建筑、蓝图复用和物流规划已经成为核心玩法，这些交互落差会直接降低布局判断的可靠性与连贯性。

## What Changes

- 修正蓝图应用预览中多格建筑的可视位置，使预览锚点、占地范围与最终放置结果保持一致，包括旋转后的多格结构。
- 调整组装机与弹药组装机的 `3x2` 多端口布局，让三个输入端口稳定分布在一侧、三个输出端口稳定分布在另一侧，便于传送带从两边组织物流。
- 收敛端口提示显示逻辑，只在传送带放置预览模式下显示附近多端口建筑的输入/输出标记，避免其他建筑预览时出现无关方块标记。
- 优化世界工厂的建造交互：成功放置后保持当前建筑仍然处于已选中状态，可继续连续摆放；按住左键在地图上拖动时，对经过且可放置的格子自动连续放置。

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `factory-blueprint-workflow`: 蓝图应用预览需要对多格建筑使用与实际落地一致的解析中心与朝向，避免预览错位。
- `factory-grid-building`: 世界建造模式需要支持连续摆放、左键拖拽自动摆放，以及仅在传送带预览时显示端口提示。
- `factory-manufacturing-chain`: 组装机与弹药组装机的三输入三输出端口分布需要调整为两侧对称布局，保证物流连接契约稳定。
- `factory-multi-cell-structures`: 多格结构的预览与蓝图可视化需要遵循同一套 footprint 中心和旋转规则，保证预览与最终占地一致。

## Impact

- Affected specs: `factory-blueprint-workflow`, `factory-grid-building`, `factory-manufacturing-chain`, `factory-multi-cell-structures`
- Affected code likely includes `scripts/factory/FactoryDemo.cs`, `scripts/factory/FactoryBlueprints.cs`, `scripts/factory/FactoryPlacement.cs`, `scripts/factory/FactoryLogisticsPreview.cs`, `scripts/factory/FactoryStructureFactory.cs`, `scripts/factory/structures/AssemblerStructure.cs`, and `scripts/factory/structures/AmmoAssemblerStructure.cs`
- No external dependencies or API surface changes are expected; the impact is concentrated in placement preview behavior, logistics hint visibility, and manufacturing structure footprint/port contracts
