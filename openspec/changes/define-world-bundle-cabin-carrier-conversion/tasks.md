## 1. Cargo Template Model

- [x] 1.1 为世界大包引入 bundle template / manifest / size tier 数据定义，并让世界货包运行时能够解析到对应模板。
- [x] 1.2 重构解包逻辑，使 1 个世界大包能够按 manifest 节拍产出多个舱内小包，而不是只做 `CargoForm` 的 1:1 变换。
- [x] 1.3 重构封包逻辑，使封包机围绕目标模板累计舱内小包，并只在清单满足时产出 1 个世界大包。
- [x] 1.4 实现固定模板、类别模板和受控混装模板的边界校验，并禁止任意自由混装。

## 2. Cargo Presentation

- [x] 2.1 调整 `FactoryItemVisuals` 与运输描述符，让世界货包统一使用无阴影的 3D 贴图盒体表现，并按 bundle size tier 改变盒体尺寸。
- [x] 2.2 保持舱内小包继续使用 2D/billboard 风格表现，并确保同一资源在世界大包与舱内小包之间保留一致的识别线索。
- [x] 2.3 明确大包与小包在边界交接、处理腔、缓冲位、舱内带路由中的 presentation context 和 fallback 规则。

## 3. Conversion Chambers And Heavy Handoff

- [ ] 3.1 为解包舱、封包舱建立按 size tier 切换的 footprint / 占位档位，并同步 placement、preview、map/runtime loading 和 blueprint 行为。
- [x] 3.2 重做解包舱、封包舱和重载缓冲位的模型表现，使其更像重载装卸舱段而不是普通小型加工机。
- [x] 3.3 为解包/封包舱段加入机械臂、夹持器、滑台或等效机构的处理动画，并在锚点上展示正在处理的世界大包模型。
- [x] 3.4 调整输入/输出边界附件，使世界大包只在重载交接节点间转位，不进入普通舱内 belt / splitter / merger / bridge。

## 4. Editor And Demo Content

- [ ] 4.1 更新 mobile factory interior editor 与 world miniature，让重载大包节点、小包物流层、当前 bundle template 和 size tier 都能被玩家读懂。
- [x] 4.2 重构 focused mobile interior authored layout 与相关挂点，使 demo 能展示“世界大包 -> 解包 -> 多个舱内小包 -> 封包 -> 世界大包”的完整链路。
- [x] 4.3 调整 demo 文案、提示和说明，让玩家理解大包不会进入舱内普通传送带、封包也受模板限制。

## 5. Validation

- [x] 5.1 为 bundle template、manifest 解包、模板封包、混装限制和 heavy handoff 规则补充或更新 smoke / validation 覆盖。
- [ ] 5.2 为世界 3D 大包 / 舱内 2D 小包的表现分离、无阴影世界货包和按 size tier 切换的 conversion chamber footprint 补充回归检查。
- [x] 5.3 更新 focused mobile demo 的 smoke 断言，验证 world bundle 不会直接出现在舱内 belt，且解包/封包链路可以按模板完成转换。
