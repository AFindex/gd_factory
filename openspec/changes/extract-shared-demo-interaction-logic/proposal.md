## Why

`factory_demo.tscn` and the mobile-factory demos now share the same baseline factory interaction loop, but that baseline is still split across two large controllers with duplicated HUD, selection, input, blueprint, and preview coordination code. The static demo has effectively become the common interaction subset of the mobile demo, so continuing to evolve both surfaces separately makes every interaction change slower, riskier, and harder to keep consistent.

## What Changes

- Extract the shared low-level interaction flow currently embodied by `FactoryDemo` into a reusable interaction layer that both the static demo and the mobile-factory demos can consume.
- Move common interaction responsibilities out of `FactoryDemo.cs` and `MobileFactoryDemo.cs`, including workspace-facing HUD state projection, structure selection/detail routing, build-delete-blueprint mode switching, player inventory placement arming, and shared preview/input gating helpers.
- Keep `factory_demo.tscn` focused on the static sandbox rules and keep the mobile-factory demos focused on deploy, world-anchor, and interior-editor-specific behavior by layering those demo-exclusive rules on top of the shared interaction foundation.
- Clean up interaction-related code in both demo controllers so each scene keeps only scenario-specific orchestration, validation, and authored gameplay branches.

## Capabilities

### New Capabilities

### Modified Capabilities

- `factory-demo-runtime-composition`: expand the shared demo composition contract so both demos reuse one baseline interaction foundation for HUD projection, selection/detail routing, build and blueprint workflows, placement intent, and preview/input coordination while preserving demo-specific gameplay extensions

## Impact

- Primary code impact will be in [FactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/FactoryDemo.cs), [MobileFactoryDemo.cs](/D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs), and the existing shared support types under [scripts/factory](/D:/Godot/projs/net-factory/scripts/factory).
- Likely shared extraction targets include `FactoryDemoInteractionBridge`, `FactoryDemoRuntimeSupport`, HUD adapters, blueprint workflow coordination, and preview/input helper code that is still duplicated or controller-owned.
- Main risk is interaction regression across both demos during refactor, so the shared layer needs a clear boundary: common factory interaction behavior is shared once, while mobile-factory-specific deploy/editor logic remains local.
