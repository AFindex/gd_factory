## Context

移动工厂输入接口已经通过分步重构形成了一套比较清晰的工作方法：先压回纯静态基线，再逐段恢复世界侧接货、缓存停留、桥接、舱内缓存和与解包舱的交接，同时坚持单一视觉宿主、显式锚点和 readiness gating。这套方法已经在 [docs/移动工厂输入接口动画分步重构规划.md](/D:/Godot/projs/net-factory/docs/%E7%A7%BB%E5%8A%A8%E5%B7%A5%E5%8E%82%E8%BE%93%E5%85%A5%E6%8E%A5%E5%8F%A3%E5%8A%A8%E7%94%BB%E5%88%86%E6%AD%A5%E9%87%8D%E6%9E%84%E8%A7%84%E5%88%92.md) 中沉淀下来，也和 `godot-heavy-cargo-handoff-debug`、`godot-staged-animation-rebuild` 两个 skill 的建议一致。

输出接口虽然已经具备基础逻辑链路，但当前实现仍有几个和输入侧不对称的地方：

- `MobileFactoryOutputPortStructure` 主要依赖基类默认的重载显示宿主选择，没有像输入口那样显式收紧每个 host 的可见时机。
- 输出口当前更偏向“封包舱交货后直接推进到世界释放”，缺少像输入口那样可以单独验收的阶段停留与镜像锚点合同。
- `CargoPacker` 与输出口之间虽然有逻辑交接，但对“谁在显示这件大包、世界路线何时真正接管”还缺少一份面向动画重建的分阶段合同。

本次设计不打算再对输出侧追加一层新的特判动画，而是把输出口明确纳入和输入口一致的 staged rebuild 方法：先定义阶段、宿主、锚点和切换边界，再分步恢复每一段表现。

## Goals / Non-Goals

**Goals:**

- 为输出接口定义与输入接口对称的阶段合同：`PackerDock -> InnerBuffer -> BridgeOut -> OuterBuffer -> WorldRelease`。
- 保证一件完整世界大包在输出链路上始终只有一个可见宿主，避免封包舱、输出口和世界路线重复显示。
- 让输出接口的桥接、等待和释放时机绑定到真实逻辑边界，而不是依赖额外的猜测式显隐。
- 复用输入接口已经验证过的 phased rebuild 方法、锚点镜像思路和 smoke 验收方式。
- 为 focused demo 和 smoke 提供可重复验证的输出口动画观察路径。

**Non-Goals:**

- 不重做 `CargoPacker` 的配方、加工时长或清单规则。
- 不引入多件并行桥接、多车道输出或新的世界物流抽象。
- 不重写普通 belt / route 系统，只在输出重载交接边界上补齐 staged animation 合同。
- 不要求首轮就做完所有机械细节动画；先保证阶段边界、宿主和路径正确。

## Decisions

### 1. 输出口按输入口同样的“先基线、后分段恢复”顺序重建

输出口不直接追求一次性做出完整华丽动画，而是采用和输入口相同的 staged rebuild 顺序：

1. 静态基线，只保留功能逻辑与静态结构。
2. `CargoPacker -> InnerBuffer` 单段交接。
3. `InnerBuffer` 明确等待。
4. `InnerBuffer -> BridgeOut -> OuterBuffer` 连续桥接。
5. `OuterBuffer` 明确等待世界接货。
6. `OuterBuffer -> WorldRelease` 可见释放。

这样做的原因是，输出口当前缺少像输入口那样已经验过的阶段隔离。如果直接在现有逻辑上叠更复杂的出舱动画，很容易重新引入“旧宿主还没退，新宿主已经起”的问题。

备选方案是只对当前输出口桥接做局部补间和停顿修饰。这个方案无法把问题拆成可验收的小步，也不符合两个 handoff/rebuild skill 强调的“先重建阶段边界，再加 polish”的方法，因此放弃。

### 2. 每个输出阶段都明确 visual owner 与 logical owner

输出链路中的每个阶段都需要同时写清楚两个所有权：

- `CargoPacker` 处理位和 dispatch 位阶段，由 `CargoPacker` 持有逻辑与视觉。
- `InnerBuffer`、`BridgeOut`、`OuterBuffer` 阶段，由 `MobileFactoryOutputPortStructure` 持有逻辑与视觉。
- `WorldRelease` 完成后，由世界 route / target structure 接管逻辑与视觉。

只有在明确接管事件发生时才切 owner：

- `CargoPacker` 真正把 bundle 交给输出口时，封包舱让出视觉。
- 输出口真正调用世界侧接货成功时，世界 route 才出现该 bundle。

这样做的原因是，输出侧最常见的问题不是速度不对，而是“逻辑已经放手但旧画面还在”或“世界侧还没接住就先冒出来”。把 visual owner 和 logical owner 同步成一份合同，才能保证后续动画是可信的。

备选方案是保留当前多点显示逻辑，再用更多 `if` 分支压重复可见节点。这个方案会继续积累时序特判，不利于长期维护，因此放弃。

### 3. 输出口锚点合同直接镜像输入口，但方向与交接对象相反

输出口的关键锚点不再“凭当前实现大致够用”来推导，而是定义成一套镜像输入口的固定阶段点：

- `PackerHandoffAnchor`: 封包舱把 bundle 放给输出口的起点
- `InnerBufferPayloadAnchor`: 输出口舱内缓存位
- `InteriorBridgeExit`: 输出口离舱桥位起点
- `WorldOuterBufferPayloadAnchor`: 世界侧缓存位
- `WorldReleaseEdge`: 世界路线正式接管的释放边

这几个点的语义要固定，后续插值、朝向和 smoke 判断都以它们为准，而不是以“当前父节点下大概差不多的位置”去猜。

这样做的原因是，输入口重构已经证明：真正难调的不是速度，而是锚点角色模糊。输出口如果不先把这些点的角色写死，很容易再次出现桥尾断裂、释放点前后不一致和世界接货节拍错位。

备选方案是延续基类默认路径，只在输出口局部加偏移量。这个方案短期省事，但难以支撑后续分步验收，因此放弃。

### 4. 等待世界接货使用 readiness gating，固定停顿只作为分步验收工具

输出口最终的推进条件以 readiness gating 为准：

- `InnerBuffer -> BridgeOut` 需要桥接链路空闲，且输出口当前没有 world-side 滞留。
- `OuterBuffer -> WorldRelease` 需要世界 route 真正可以接受该 bundle。

可以保留很短的 settle / hold 时间作为视觉落位或单步验收工具，但这些时间不能代替真正的 ready 条件。尤其 `OuterBuffer` 阶段的结束，必须由世界接货能力驱动，而不是单纯时间到了就放。

这样做的原因是，输入口第二步和第四步的经验已经很明确：固定停留适合验证“这个宿主站不站得住”，但最终交接条件还是要由下游准备好来决定。输出口同样需要这个区分。

备选方案是用固定停顿驱动全部阶段。这个方案在 world route 堵塞、断连或多次连续发送时都会变得不可信，因此放弃。

### 5. demo 与 smoke 按“单一可见宿主 + 阶段顺序”验收，而不是只看最终有无出货

这次 change 的回归验证不能只验证“最后大包有没有到世界侧”，而是要明确覆盖：

- 是否看到了 `CargoPacker -> InnerBuffer` 的交接
- 是否看到了 `OuterBuffer` 的等待世界接货
- 同一时刻是否最多只有一个可见 bundle 本体
- 世界 route 的接货时机是否与 `WorldReleaseEdge` 一致

这样做的原因是，输出侧很多问题在“最终能出货”的前提下依然存在。如果 smoke 只检查吞吐成功，就很容易让重复显示或释放错位溜过去。

备选方案是继续沿用现有偏吞吐的 smoke 断言。这个方案无法覆盖本 change 最关心的动画合同，因此放弃。

## Risks / Trade-offs

- [输出口当前逻辑已经跑通，重建动画时容易误伤现有出货功能] → Mitigation: 采用输入口同样的分步重建顺序，每一步只放开一个阶段，并保留功能逻辑不冻结。
- [封包舱与输出口切 owner 的边界如果不干净，仍可能出现双宿主一帧重叠] → Mitigation: 以 `TryAcceptPackedBundle` 成功和世界 route 真正接货成功作为唯一切换点，不允许纯靠动画进度猜测。
- [镜像输入口并不等于逐行复制输入口实现，硬复制可能带来错误朝向或错误父坐标系] → Mitigation: 只镜像阶段合同和验收顺序，不强制复用错误的具体偏移，输出口仍以自己的锚点语义为准。
- [focused demo 节奏不足时，输出阶段可能不容易被人眼观察] → Mitigation: 在 demo / smoke 中保留“等待世界接货”的可观察场景，并输出阶段与宿主信息用于调试。

## Migration Plan

1. 先把输出口当前阶段、宿主和关键锚点补成一份明确合同，确认 `CargoPacker`、输出口和世界 route 的切换边界。
2. 收紧输出口 host 可见策略，必要时先退回静态基线，只保留功能逻辑。
3. 恢复 `CargoPacker -> InnerBuffer` 单段表现，并验证封包舱放手后输出口才出现 bundle。
4. 恢复 `InnerBuffer -> BridgeOut -> OuterBuffer`，并验证桥接时没有双宿主。
5. 恢复 `OuterBuffer -> WorldRelease`，并验证世界 route 只在可见释放边接管。
6. 更新 focused demo 与 smoke，让输出口阶段动画成为可重复观察的回归场景。

回滚策略：

- 如果某一步引发重复显示或逻辑错位，回退到上一个已验证阶段，而不是在坏链路上继续叠补丁。
- 如果世界侧释放段一时不稳定，可先保留 `OuterBuffer` 等待画面，延后开放世界 release 动画，但不能牺牲单宿主合同。

## Open Questions

- 当前没有阻塞实现的开放问题。默认假设是：输出口可以在首轮复用现有 `CanReleaseToWorld` 判定，只调整阶段暴露方式、锚点语义和视觉所有权切换。
