## Context

当前移动工厂的重载输入/输出链路已经具备世界侧缓存、桥接路径、舱内缓存和解包/封包转换的基础逻辑，但运行时的大包显示职责仍然分散在多个地方：

- 世界侧 attachment root 会根据自己的缓存状态决定是否显示大包
- 舱内侧 attachment / structure visual root 也会根据自己的阶段单独显示大包
- 解包舱 / 封包舱在处理位又会尝试展示同一件大包

这会把“同一件货物正在沿一条连续路径移动”拆成多个局部判断，结果就是：

- 同一件大包在世界侧、桥上、舱内侧或处理位重复闪现
- 世界传送带还没真正扣货，接口却已经先出现一件货
- 一个阶段结束后，下一阶段会重新生成一个看起来像同一件货但其实不是同一显示实例的模型
- 玩家无法稳定读懂货物究竟是在等待、在过桥，还是已经被解包/封包舱接管

这次 change 的目标不是继续微调阶段时长、速度和淡入淡出阈值，而是重构重载大包的显示归属：同一件世界大包在任意时刻只能有一个 active visual owner，所有 world-side、bridge-side、cabin-side 与 converter-side 表现都围绕这个合同协作。

约束如下：

- 不重写普通世界传送带或舱内轻型料轨的底层拓扑规则
- 不把完整世界大包重新适配到舱内普通料轨上运输
- 不引入一个全新的世界物流系统，只修正重载接口这条边界链路
- 保留既有重载缓存、解包/封包处理与 focused demo 的大方向，只把视觉 ownership 和时序收拢为统一模型

## Goals / Non-Goals

**Goals:**
- 为每一件参与重载交接的世界大包建立单一 visual owner 合同，杜绝重复显示与错位重生。
- 将重载交接表现重构为一条统一 staged path，覆盖世界带接驳、世界侧缓存、桥前对位、穿壳桥位、舱内缓存和转换舱交接。
- 让世界带扣货、接口接货、桥接移动、转换舱接管、世界带放货这些逻辑动作与视觉所有权切换同步。
- 明确 world attachment root、cabin attachment root、converter chamber 三类节点的职责边界：静态几何、锚点、cargo presenter 接管点。
- 保持世界大包在整个重载链路内都使用 world scale，不再出现阶段切换时额外缩放或换模。
- 为 focused demo 和回归验证提供可观察的“单件大包连续进/出工厂”标准。

**Non-Goals:**
- 不把普通轻型传送带改造成可承载 world-scale cargo。
- 不在这次 change 内做多件并行桥接、编组装卸或多 cargo 同桥队列。
- 不重做所有移动工厂外壳或摄像机系统，只处理与重载交接可读性直接相关的结构和表现职责。
- 不重新定义解包/封包配方平衡，只规范世界大包与舱内小包之间的显示与交接关系。

## Decisions

### 1. 为重载货物引入共享的 presentation handle，而不是让每个 root 各画一份

每一件参与重载链路的 world cargo 都必须绑定一个共享的 presentation handle。这个 handle 代表“当前场景中这件大包的唯一可见本体”，它在不同阶段被交接给不同 host，但不会被世界侧 root、桥接 root、舱内 root、converter chamber 同时各自实例化一份。

推荐实现语义：

- logical cargo token 继续描述货物身份、内容与交接进度
- presentation handle 只负责该 logical cargo 的唯一可见世界模型
- handle 在不同阶段重绑到不同的锚点或 host，但同一时刻只有一个 owner

选择这个方案的原因：

- 它能直接根除“同一件货被多个系统重复渲染”的结构性问题
- 它允许我们把动画、插值和淡出集中到一个 presenter 上，而不是多个 root 之间猜测彼此状态
- 它天然支持“同一件货从世界带走到桥上，再走进解包舱”的连续观感

备选方案是保留现在的多 root 结构，只靠更多的显示条件去避免重叠。这种做法已经证明会不断积累特判，仍然难以覆盖首帧闪现、延迟扣货和阶段错位，所以放弃。

### 2. Boundary attachment 负责 staged path 和静态几何，但不再独立决定 cargo body 显示

重载输入/输出 attachment 仍然是跨边界交接的逻辑枢纽，因为它同时知道：

- 世界侧 belt / route 的状态
- 外缓存、桥接、内缓存的阶段
- 与 unpacker / packer 的握手时机

因此 attachment 应当成为重载 staged path 的主要编排者，定义统一的锚点顺序：

- world approach / belt handoff point
- outer buffer
- bridge align
- bridge crossing
- inner buffer
- converter handoff point

但 attachment world root 和 cabin root 只负责：

- 静态壳体开口、导轨、托盘、桥架、观察窗等几何
- staged path 锚点
- 阶段灯光、状态文字、空闲/忙碌反馈

它们不再基于“我这边缓存有货”就自己生成一件大包模型。真正的大包本体只来自共享 presentation handle。

备选方案是把唯一 presenter 放到 world root 或 cabin root 某一侧，再让另一侧通过复制或镜像来补可见性。这会重新引入多重职责边界，不利于 converter takeover，因此放弃。

### 3. Visual ownership 切换必须与 logical custody 边界对齐

这次修复的关键不是“动画更顺滑”本身，而是 ownership 边界必须和真正的逻辑接管边界一致。

输入方向：

- 世界带拥有货物，直到 input port 真正接受该货物进入 outer buffer handoff
- 接受发生的那一刻，belt 扣货，同时 presentation handle 的 owner 从世界 route 切换到 heavy handoff chain
- handle 之后沿 outer buffer -> bridge -> inner buffer 连续移动
- 当 unpacker 通过明确握手接受货物时，owner 从 handoff chain 切换到 unpacker chamber

输出方向：

- packer chamber 在打包完成前持续拥有大包显示
- 只有在 output port 真正接受成品大包时，presentation handle 才切换到 heavy handoff chain
- world route 只有在能够接货且完成 release 接受时才接管 owner，同时 belt / route 上出现该货物

这个决定可以避免两类常见错误：

- 视觉先动了，逻辑还没扣货
- 逻辑已经换 owner 了，但旧地方还残留一帧甚至多帧显示

备选方案是保留现有逻辑交接，只做更复杂的 delayed hide / delayed spawn timing。这样仍然无法保证所有路径的一致性，因此放弃。

### 4. Converter chamber 在处理期间成为完整世界大包的唯一 owner

解包舱和封包舱不再把大包处理简化为“接口淡掉，大包就算消失”。一旦 converter 通过握手接受了完整 world cargo：

- unpacker / packer chamber 成为新的 visual owner
- 其 chamber visual 负责持有并展示这件完整大包
- unpacker 在 unpack-complete 节点之后才允许这件大包退场并开始舱内小包的发射
- packer 在 pack-complete / release-ready 节点之后才把完整大包交给 output handoff

这样做的意义是把转换层真正塑造成“世界标准 <-> 舱内标准”的交汇点，而不是让 port 看起来像一个会魔法消失/出现货物的洞口。

备选方案是在 handoff chain 中途就 dissolve 大包，converter 只展示抽象工作灯。这个方案会继续削弱 converter 的可读性，因此放弃。

### 5. 静态结构与 cargo presenter 解耦，但仍保留局部状态反馈

虽然 cargo body 只有一个 presenter，但 port 和 converter 仍需要表达自身状态。因此视觉层应拆成两类：

- static/profile geometry：托盘、桥架、开口、壳体、夹具、腔室、信号灯
- shared cargo presentation：当前唯一的大包本体

结构 visual profile 继续负责静态几何与局部状态，例如：

- 桥架伸缩、对位灯、缓存位空满提示
- unpacker / packer 夹具动作、腔室灯光、处理节拍动画

但“是否画大包本体”改由 shared ownership state 驱动。这样既保留结构个性，也不会让各自 profile 再次长出重复 cargo 判定逻辑。

备选方案是让 visual profile 直接读更多 buffer / phase 变量，并各自算出要不要画货。这种方式与这次 change 的核心目标冲突，因此放弃。

### 6. 以 attachment-local authority 为主，而不是全局 cargo scene manager

单一 presenter 并不意味着要上一个全局的“大包显示总管”。这次设计更适合把 authority 放在 active heavy handoff controller 本地：

- 一个 active heavy input/output chain 只需要协调自己这条边界链路上的 presenter
- converter 在握手时暂时接管该 presenter
- 当处理完成或释放回 chain 时，再把 presenter 交回 handoff controller 或直接交给 world route

这样做比全局 manager 更容易落地，也更贴合现有 `MobileFactoryBoundaryAttachmentStructure`、`MobileFactoryInstance` 与 converter structure 的责任划分。

备选方案是做全局 cargo scene registry。虽然理论上更统一，但会扩大本次 change 的实现半径，也会让普通世界物流和重载边界耦合过深，因此先不采用。

## Risks / Trade-offs

- [共享 presenter 与既有多 root 代码并存时容易残留旧显示分支] → Mitigation: 在 spec 和实现中明确禁止 world root / cabin root / converter root 自行生成 full-size cargo body，统一改为消费 shared ownership state。
- [ownership 切换边界如果定义不清，会出现“无 owner 空窗”或“双 owner 重叠”] → Mitigation: 以 belt accept/release、converter accept/release 这些明确逻辑事件作为唯一切换点，不允许基于动画进度猜测切换。
- [attachment-local authority 可能让跨模块调试更依赖接口实现] → Mitigation: 保留结构化状态快照与 debug 文本，让 current owner、current host、current phase 可见。
- [converter 持有 presenter 后，壳体遮挡或观察角度问题会更明显] → Mitigation: 在 visual profile delta 中同步要求 chamber 处理阶段保持可读，不允许靠 attachment 额外复制一份 cargo 来“补看见”。
- [focused demo 可能仍然因为旧地图、旧路径或旧状态提示而看起来不连贯] → Mitigation: 将 demo 明确列为本 change 的修改能力之一，要求更新观测路径和验证场景。

## Migration Plan

1. 定义重载大包的 shared ownership contract，包括 logical cargo token、presentation handle、current owner / host / anchor path 的语义。
2. 收拢 heavy attachment 的 world-side 与 cabin-side 显示职责，让它们只保留 staged path 锚点与静态几何。
3. 把输入/输出 belt accept/release 与 handoff chain 的 ownership 切换绑定到同一批逻辑边界。
4. 为 unpacker / packer 引入明确的 presenter takeover / release 时机，保证 chamber 处理成为唯一可见阶段。
5. 更新 demo、状态提示和回归验证，专门覆盖“无 duplicate flash、无 late consume、无 disconnected respawn”。

回滚策略：

- 如果首轮实现无法一次补完全部动画细节，可以先交付正确 ownership 与正确时序，再对路径插值和细节动画做后续 polish。
- 但不能回滚到“多 root 同时画货物，再靠条件屏蔽”的老模型，因为那会直接恢复当前的核心缺陷。

## Open Questions

- world route 在接管 output cargo 的那一帧，是否需要额外的 belt-side easing 片段，还是只要 ownership 与 release 时机正确即可？
- focused demo 是否需要额外放慢或高亮重载链路，以便更容易验证 single-owner contract？
- world miniature / 远景视图是否只复用抽象状态，而不完整复刻同一 presenter 路径？
