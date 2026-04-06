## Why

当前工厂游戏的基础 UI 已经覆盖启动器、sandbox HUD、mobile factory HUD、玩家背包/热键栏、蓝图面板、结构详情窗和 `UiShowcase`，但这些界面仍保留较多柔和渐变、发光色和彩色强调，且多个面板与按钮使用了明显圆角，导致整体观感更偏“科幻控制台”，不够贴近工厂游戏所需的硬朗、清晰和模块化信息密度。随着工作区菜单、独立详情窗和玩家物品面板越来越多，我们需要一套统一的 one bit 风格视觉基线，在不触碰现有业务逻辑的前提下提升一致性、层级感和可读性。

## What Changes

- 新增一套共享的基础 UI 视觉规范，围绕 one bit 风格建立黑白高对比、细边框、块面分区、反相高亮和近乎直角的控件样式。
- 重构基础 UI 的通用外观实现，使启动器、运行时 HUD、工作区导航、蓝图面板、结构详情窗、玩家背包/热键栏和 UI showcase 共享同一套视觉 token 或样式辅助函数，而不是各自维护不同的圆角和配色。
- 将目前偏柔和的圆角面板、胶囊按钮、彩色渐变强调和氛围装饰改为更克制的 one bit 表达，例如纯色填充、像素/网格感分隔线、黑白反差状态和更硬朗的悬停/选中态。
- 保留现有输入映射、工作区切换、蓝图流程、库存拖拽、结构详情交互、玩家热键栏行为和场景状态同步逻辑，仅调整视觉表现与布局细节。
- 为视觉改造补充可验证的验收口径，确保运行时交互仍可达、文本仍可读、关键状态仍可区分，并且不会因为主题切换影响现有业务流程。

## Capabilities

### New Capabilities
- `factory-ui-one-bit-theme`: 定义工厂游戏基础 UI 的共享 one bit 风格，包括黑白主视觉、方正边框、低圆角/无圆角控件、清晰的选中与悬停态，以及在 launcher、HUD、详情窗和展示场景中的一致应用。

### Modified Capabilities
None.

## Impact

- 受影响代码将集中在 [DemoLauncher.cs](/D:/Godot/projs/net-factory/scripts/DemoLauncher.cs)、[DemoNavigation.cs](/D:/Godot/projs/net-factory/scripts/DemoNavigation.cs)、[UiShowcase.cs](/D:/Godot/projs/net-factory/scripts/UiShowcase.cs)、[FactoryWorkspaceChrome.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryWorkspaceChrome.cs)、[FactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryHud.cs)、[MobileFactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.cs)、[MobileFactoryHud.Workspaces.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.Workspaces.cs)、[FactoryBlueprintPanel.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryBlueprintPanel.cs)、[FactoryStructureDetailWindow.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetailWindow.cs) 和 [FactoryPlayerHud.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryPlayerHud.cs)。
- 这次改动更偏视觉系统整理，可能会新增共享样式 helper、颜色/边框常量或主题构建入口，以减少当前散落在各个控件类中的 `StyleBoxFlat` 重复定义。
- 需要回归验证 demo launcher、static factory demo、mobile factory demo、large scenario、玩家背包窗口和 `UiShowcase`，确认控件状态、拖拽热区、面板布局和工作区切换都维持原有功能。
