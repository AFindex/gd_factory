# AGENTS.md

## 项目概述

本项目是一个基于 **Godot 4.6 Mono (C#)** 的 2.5D 工厂自动化模拟沙盒游戏，灵感来源于《异星工厂》(Factorio)。主要玩法包括：

- 静态工厂沙盒（采矿、精炼、传送带、组装机、电力网络、炮塔防御）
- 移动工厂系统（可部署/回收的移动工厂，支持内部编辑、边界挂载、世界网格移动）
- 蓝图捕获与应用
- 多场景启动器（`scenes/demo_launcher.tscn`）

## 技术栈

| 层 | 技术 |
|---|---|
| 引擎 | Godot 4.6.1-stable (Mono) |
| 语言 | C# / .NET 8.0 |
| 物理 | Jolt Physics 3D |
| 渲染 | Forward Plus, Direct3D 12 |
| 构建 | Godot.NET.Sdk/4.6.1 |
| 版本控制 | Git（`.gitattributes` 强制 LF 换行） |
| 项目管理 | OpenSpec 规范驱动开发 |

## 常用命令

```powershell
# 打开编辑器
godot_mono --path D:\Godot\projs\net-factory --editor

# 编译 C# 解决方案
godot_mono --path D:\Godot\projs\net-factory --headless --build-solutions --quit

# 运行默认场景（启动器）
godot_mono --path D:\Godot\projs\net-factory

# 运行指定场景
godot_mono --path D:\Godot\projs\net-factory --scene res://scenes/demo_launcher.tscn

# 静态工厂冒烟测试
godot_console.exe --headless --path 'D:\Godot\projs\net-factory' -- --factory-smoke-test

# 地图验证
godot_console.exe --headless --path 'D:\Godot\projs\net-factory' -- --factory-map-validate

# 移动工厂冒烟测试
godot_console.exe --headless --path 'D:\Godot\projs\net-factory' -- --mobile-factory-smoke-test
```

## 项目结构

```
net-factory/
├── project.godot                    # Godot 项目配置
├── net_factory.sln / .csproj       # .NET 解决方案
├── scenes/                          # Godot 场景文件 (.tscn)
├── scripts/                         # 所有 C# 源码
│   └── factory/                     # 核心工厂系统 (60+ 文件)
│       ├── maps/                    # 地图子系统
│       ├── structures/              # 30+ 建筑类型
│       └── smoke/                   # 冒烟测试控制器
├── data/factory/                    # 游戏数据
│   ├── blueprints/                  # 蓝图 JSON
│   └── maps/                        # 自定义 .nfmap 地图
├── docs/                            # 文档（中文）
├── openspec/                        # OpenSpec 规范驱动开发
│   ├── specs/                       # 能力规范
│   └── changes/                     # 变更提案
├── addons/                          # 编辑器插件
└── tools/profiling/                 # 性能分析脚本
```

## 代码架构与约定

### 命名规范
- 文件、类、结构体、公共方法/属性、常量、枚举：`PascalCase`
- 私有字段：`_camelCase`（下划线前缀）
- 静态工具类：以 `Factory` 前缀命名

### 核心架构模式

1. **场景即控制器**：每个 `.tscn` 是薄包装，引用单个 C# 脚本，脚本在 `_Ready()` 中通过程序化构建场景图。

2. **接口抽象**：核心抽象使用 C# 接口：
   - `IFactorySite` — 网格系统抽象（世界用 `GridManager`，内部用 `MobileFactorySite`）
   - `IFactoryPowerNode`、`IFactoryInspectable`、`IFactoryCombatSystem` 等

3. **partial class 拆分**：大型场景控制器通过 .NET partial class 跨文件拆分：
   - `FactoryDemo.MapLoading.cs`、`FactoryDemo.Persistence.cs`
   - `MobileFactoryDemo.Combat.cs`、`MobileFactoryDemo.Persistence.cs`
   - `MobileFactoryHud.Workspaces.cs`

4. **抽象基类 + 工厂模式**：`FactoryStructure`（968行）是所有建筑类型的抽象基类，由 `FactoryStructureFactory` 统一创建实例。

5. **视觉配置分离**：`FactoryStructureVisualProfile` / `FactoryStructureVisualController` 将建筑视觉与逻辑解耦，支持世界/舱内两种视觉风格。

6. **仿真驱动**：`SimulationController` 运行确定性 tick 循环（每步0.05s，每物理帧上限2步），管理建筑、电力网络、战斗和物流物品路由。

7. **事件/委托驱动 HUD**：主控制器通过委托连接 UI 和游戏逻辑，HUD 按工作区面板（建造、蓝图、遥测、战斗、测试、存档）组织。

8. **自定义地图格式 (.nfmap)**：通过 `FactoryMapRuntimeLoader` 加载，`FactoryMapValidationService` 验证，`FactoryMapCatalog` 编目。

9. **双工业标准**：游戏支持 `World` 和 `Interior` 两种视觉/行为上下文，通过 `FactoryIndustrialStandards` 和 `FactoryStructureLogisticsContract` 统一契约。

10. **OpenSpec 规范驱动开发**：
    - `openspec/specs/` — 当前能力规范
    - `openspec/changes/` — 活跃变更提案
    - `openspec/changes/archive/` — 已完成提案
    - 每个变更引用其修改的能力规范

### 编辑规范
- **不要添加不必要的注释**：只在必要时添加注释。
- **遵循现有模式**：修改代码前先阅读周边代码，保持风格一致。
- **优先使用 Edit 工具修改现有文件**，不要轻易创建新文件。
- **代码标识符使用英文**，用户界面字符串可以使用中文。

## 语言要求

**⚠️ 重要：所有思考和输出必须使用中文。**
