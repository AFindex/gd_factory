## Context

当前静态 `FactoryDemo`、聚焦 `MobileFactoryDemo` 和大型移动工厂场景已经具备真实工厂系统的主要组件，例如采矿机、熔炉、组装机、弹药组装器、发电机、炮塔、边界端口、采矿输入端口和回收/仓储链路，但大量 authored 用例与 smoke 验证仍然依赖 `BuildPrototypeKind.Producer` 直接产物来驱动流程。结果是：

- demo 地图里的矿区、接收端、防线与内部工厂并没有被组织成稳定的真实闭环；
- 移动工厂内部预制布局虽然拓扑丰富，但不少案例仍以测试建筑快速喂料，而不是展示真实输入、加工和输出；
- 资源目录目前只有 `Coal`、`IronOre`、`CopperOre`，无法支撑用户要求的“更多矿物 + 更完整的配方链 + 电力维护 + 防御补给”用例分层；
- HUD 里仍保留“测试建筑/建造测试”心智，导致 sandbox 更像调试台而不是完整玩法样板。

这次变更是一次跨内容目录、场景编排、移动工厂预制和 smoke 覆盖的交叉修改，值得先把设计定清楚，再进入实现。

## Goals / Non-Goals

**Goals:**

- 用真实工厂建筑替换静态 demo、移动工厂 demo 和大型移动工厂场景中的主流水线 `Producer` 用例。
- 扩展矿物与配方目录，至少形成“采矿 -> 冶炼/精炼 -> 中间件 -> 防御或维护产物 -> 接收/回收”的可观察闭环。
- 让静态世界按主题分区提供 authored sandbox cases，覆盖采矿、配方加工、电力维护、防御补给、接收站周转和地图建筑互动。
- 让移动工厂场景围绕“富矿地图 + 多接收站 + 多角色工厂内部布局”构建内外联动案例，重点验证端口交换和内部加工闭环。
- 将 smoke / sandbox 验证切换为依赖真实链路成功指标，例如矿物采集、配方完成、供电恢复、弹药送达、防线存活和接收站吞吐。

**Non-Goals:**

- 不在这次变更中重做移动工厂生命周期、部署 UI 或蓝图系统本身。
- 不引入全新的自动化 AI、任务系统或完全程序生成地图。
- 不强制删除 `Producer` 类型本身的底层代码；本次重点是让它退出 authored sandbox 主路径与规范要求。
- 不追求一次把所有未来矿物都补齐；本次只补足支撑 demo 闭环所需的一组扩展矿物与其配方链。

## Decisions

### Decision 1: 用“主题闭环分区”重组静态 FactoryDemo，而不是继续叠加零散回归线

静态 `FactoryDemo` 将按主题拆成若干 authored district，每个 district 都必须用真实建筑展示一种完整或半完整循环，例如：

- 采矿与接收分区：富矿区、钻机、初级分拣/接收站。
- 冶炼与中间件分区：多矿支路汇入熔炉、组装机和仓储。
- 电力维护分区：燃料发电、跨区电线杆、补电与断供恢复案例。
- 防御补给分区：真实弹药产线为炮塔供弹，展示稳态防守与补给失败两类结果。
- 地图建筑交互分区：大型仓储、回收站、桥、装载/卸载器等形成站点式吞吐节点。

这样做比继续在 `FactoryDemo.cs` 里追加孤立回归线更适合表达用户要求的“按方面补充用例，并形成完整循环”。备选方案是保留现有分散布局，只替换局部 `Producer`；该方案改动较小，但无法把闭环关系变成长期可维护的场景结构，所以不采用。

### Decision 2: 扩展资源与配方目录时，先服务 demo 闭环，再追求品类数量

新增矿物与配方会先围绕 demo 需要的功能闭环来挑选，而不是纯粹增加资源名录。设计上采用一组“能支撑不同主题用例”的矿物家族：

- 建材/防御向矿物，例如 `StoneOre`，用于墙体、仓储或结构维护物资。
- 弹药/化工向矿物，例如 `SulfurOre`，用于防御补给升级链。
- 电力/电子向矿物，例如 `QuartzOre`，用于更高阶电气或维护组件。

实现时仍沿用当前内容层分工：

- `FactoryTypes.cs` 增加 `FactoryItemKind`
- `FactoryResources.cs` 增加 `FactoryResourceKind`
- `FactoryItemVisuals.cs` 配齐名称、颜色、可视化 profile
- `FactoryStructureRecipes.cs` 把新物料接入 `MiningDrillRecipes`、`SmelterRecipes`、`AssemblerRecipes` 或新增的 machine catalog

备选方案是只重排现有煤/铁/铜三类资源。这样能更快完成替换，但无法满足“增加更多矿物配置”和多主题闭环的需求，因此不采用。

### Decision 3: 移动工厂场景采用“世界富矿 + 接收站 + 内部加工模板”三级结构

移动工厂相关场景将不再把重点放在单个内部拓扑是否复杂，而是把重点放到内外循环：

- 世界层：布置大量矿物簇、接收站、回收/转运站、敌压路线和多个部署锚点。
- 边界层：通过 `InputPort`、`OutputPort`、`MiningInputPort` 形成世界到内部的物流与采矿入口。
- 内部层：每台移动工厂使用真实采矿输入、缓存、熔炼、组装、弹药或维修产线模板。

聚焦 mobile demo 只保留少量高辨识度闭环，强调玩家能看懂一次完整“采入 -> 内部加工 -> 对外输出/补给”。大型 test scenario 则扩大为多工厂并行，每台工厂承担不同角色，例如矿石粗加工、弹药补给、维护物资转运、前线回收等。

备选方案是继续沿用目前的内部拓扑案例，只在世界层多放矿。这样仍然会让内部工厂停留在测试建筑驱动的短路状态，无法满足用户对 sandbox 用例的重点要求。

### Decision 4: 保留 workspace 结构，但把“测试建筑”入口转为“场景验证/案例切换”

HUD / workspace 不需要推倒重来，但要调整语义：

- 静态 demo 与移动工厂场景的 build-test workspace 保留为 sandbox 操作入口；
- 其中原本直接暴露 `Producer` 的内容，替换为案例说明、场景重置、验证开关、关键链路状态与诊断信息；
- 常规建造 palette 只保留真实会出现在 sandbox 闭环里的建筑。

这样既保留现有调试效率，也避免继续鼓励用测试建筑搭主流水线。备选方案是完全删除 build-test workspace；这会伤害调试效率，且不是用户需求核心，因此不采用。

### Decision 5: smoke 验证从“能出货”提升为“多闭环都能持续运行”

当前 smoke 已能检查供电产线、分流、桥接、蓝图、战斗等，但其中仍有若干 producer-based 验证段。新的 smoke 设计会把成功标准拆成更贴近玩法闭环的断言：

- 至少一条真实采矿 -> 加工 -> 接收链完成交付；
- 至少一条真实弹药 -> 炮塔补给链在运行并能应对早期压力；
- 至少一条电力维护/恢复链验证断供后的恢复；
- 至少一个移动工厂通过真实端口输入输出形成内外循环；
- 不再接受仅通过 `Producer` 或手工注入占位货物达成的主路径成功。

备选方案是保留原 smoke 结构，仅替换建筑类型名。这样会遗漏大量真实系统耦合问题，因此不采用。

## Risks / Trade-offs

- [场景复杂度上升导致 `FactoryDemo.cs` 与 `MobileFactoryDemo.cs` 继续膨胀] -> 把 authored case builders 按主题拆分为清晰的 helper 区段，必要时抽到 scenario library / layout helpers。
- [新增矿物过多会让配方和平衡失控] -> 只引入支撑 demo 闭环所需的最小矿物集合，并在 proposal/specs 中把用途绑定到具体案例。
- [移除 `Producer` 主链后，现有 smoke 可能短期内大量失败] -> 先为每个旧 producer lane 找到替代真实链路，再逐段替换 smoke 断言，避免一次性大爆炸。
- [移动工厂内外循环更真实后，长时间运行更容易堵塞] -> 每个 authored case 必须设计回收站、仓储泄压或循环消费端，避免把永久堵塞当作稳态。
- [workspace 入口仍保留“测试”概念可能让用户误解] -> 将命名和说明文案改为“sandbox 验证/案例工具”，并让默认主入口落在真实建筑与案例观察上。

## Migration Plan

1. 先扩展矿物、资源和配方目录，并补齐 transport visual / map deposit 定义。
2. 重构静态 `FactoryDemo` 的 authored district，删掉主路径上的 producer-based lanes，并同步更新 smoke。
3. 更新 `MobileFactoryScenarioLibrary` 与 `MobileFactoryDemo` 的内部模板、世界矿区、接收站和端口场景。
4. 调整 HUD / workspace 文案与入口，移除主路径上的测试建筑暴露。
5. 运行静态 demo、聚焦 mobile demo 和大型 mobile scenario 的 smoke / 手动验证，确保三类场景都能形成真实闭环。

回滚策略保持简单：按场景分批替换，单个 district / preset 替换失败时可回退对应 helper 或 preset，而不必整体回退整个 change。

## Open Questions

- 扩展矿物的首批具体名单是否固定为 `StoneOre`、`SulfurOre`、`QuartzOre`，还是在实现阶段根据现有美术/命名资源微调。
- “接收站”是继续基于 `Sink + Storage + Loader/Unloader` 组合表达，还是需要抽象出更明确的站点预制概念；本次设计倾向先用现有建筑组合落地。
- `Producer` 是否只从 authored case 中移除，还是也要从默认玩家工具栏与 build palette 中隐藏；当前设计建议至少先移出默认 demo 主入口。
