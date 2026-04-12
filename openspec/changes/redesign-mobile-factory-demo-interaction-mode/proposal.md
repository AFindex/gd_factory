## Why

当前 `mobile_factory_demo` 的 HUD 与 `factory_demo.tscn` 已经形成了两套不同的使用心智：静态工厂 demo 更接近“顶部工作区 + 单主面板 + 独立详情窗口”，而移动工厂 demo 则长期叠加世界信息、分屏编辑区和模式按钮，导致玩家很难快速理解当前正在控制什么、该去哪里编辑、以及 HUD 的重点信息在哪里。现在移动工厂已经具备较完整的生命周期、部署预览和舱内编辑能力，继续沿用当前分散式 HUD 会放大操作负担，也不利于后续迭代。

## What Changes

- 将移动工厂 demo 的主 HUD 交互与视觉组织重构为基本参考 `factory_demo.tscn` 的模式：保留顶部工作区导航，主信息区回归统一主面板表达，而不是默认同时展开多个世界/编辑面板。
- 明确区分世界操作态与编辑态，进入编辑模式时打开独立的“编辑操作面板”，集中承载内部建造、删除、旋转、蓝图和结构交互入口。
- 重新整理移动工厂 demo 的模式切换语义，让玩家控制、工厂控制、部署预览、观察模式、编辑模式之间的职责边界更清晰，并通过 HUD 文案持续反馈当前输入归属。
- 保留独立结构详情窗口与蓝图、存档等工作区能力，但调整它们与编辑模式的关系，避免编辑操作和场景状态说明混杂在同一块 HUD 中。
- 更新 smoke test / 文案契约，确保新交互模式下的 HUD 层级、模式切换和编辑面板行为可验证。

## Capabilities

### New Capabilities
- `mobile-factory-editor-operation-panel`: 定义移动工厂进入编辑模式时的独立操作面板布局、入口、关闭方式与基础交互职责。

### Modified Capabilities
- `mobile-factory-demo`: 聚焦 demo 的 HUD 组织方式与默认交互动线，要求其整体风格和工作区体验基本对齐静态工厂 demo。
- `mobile-factory-command-modes`: 调整移动工厂 demo 的控制模式集合与切换规则，把编辑模式纳入显式操作上下文，并明确各模式的输入归属与 HUD 反馈。
- `mobile-factory-interior-editing`: 修改内部编辑的打开方式、悬停焦点和工具承载形式，使编辑模式通过独立操作面板工作，而不是继续依赖当前常驻式分屏侧栏承载全部操作。

## Impact

- 主要影响 [MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs)、[MobileFactoryHud.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.cs)、[MobileFactoryHud.Workspaces.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryHud.Workspaces.cs) 与移动工厂相关 smoke tests。
- 需要对移动工厂 HUD 的布局层次、工作区内容分配、编辑态输入路由和模式状态文案进行重构。
- 需要新增一份针对“编辑操作面板”的规格，并为移动工厂 demo / 命令模式 / 内部编辑三份已有规格补充行为变化。
