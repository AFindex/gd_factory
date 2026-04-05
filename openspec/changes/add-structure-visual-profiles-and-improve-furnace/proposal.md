## Why

当前工厂建筑的外观和动画几乎都直接写死在 `FactoryStructure` 子类里，这让原型迭代很快，但也把“建筑逻辑”和“建筑表现”绑得过紧。随着项目准备逐步引入更完整的 3D 模型和动画资产，我们需要先把建筑表现抽象成可切换的视觉定义层，这样未来既能继续保留代码生成的占位/程序化表现，也能无缝接入外部模型、场景和动画资源。

熔炉是最合适的首个落地对象，因为它已经具备明确的生产语义和视觉预期，但当前表现还比较像静态方块机器。把熔炉作为示例一起优化，可以验证新架构是否真的能同时支持“代码定义”和“更有炉体感的动画表现”。

## What Changes

- Introduce a structure visual-profile system that separates building simulation behavior from world presentation data and runtime presentation control.
- Allow a structure visual profile to resolve either a code-built procedural visual, an authored `PackedScene`/3D model hierarchy, or a scene with animation assets, with deterministic fallback when higher-fidelity assets are unavailable.
- Add shared visual lifecycle hooks for idle, working, underpowered, blocked, and destroyed-adjacent presentation states so recipe-capable machines can drive animations without hardcoding every effect inside their simulation class.
- Keep support for fully code-defined buildings so existing structures can continue shipping without requiring immediate asset migration.
- Use the smelter/furnace as the first migrated example by refactoring it onto the new visual-profile pipeline and upgrading its code-defined visuals to read more clearly as a furnace with stronger heat, firebox, and exhaust animation cues.

## Capabilities

### New Capabilities
- `factory-structure-visual-profiles`: Configurable building visual definitions that support procedural code visuals, authored 3D scene/model assets, and animation-aware runtime state updates.

### Modified Capabilities

## Impact

- Affected code will include structure base classes under `scripts/factory/structures/`, build prototype registration/presentation plumbing, and whichever factory scene/demo paths instantiate structure visuals.
- The change introduces a new contract between simulation state and visual state, but it is intended to preserve existing gameplay rules, recipes, power behavior, and placement logic.
- Future imported 3D model and animation assets will have a stable integration point instead of requiring per-structure ad hoc code paths.
