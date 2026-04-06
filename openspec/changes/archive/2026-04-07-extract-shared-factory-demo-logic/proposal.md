## Why

`FactoryDemo` and `MobileFactoryDemo` both sit on top of the same factory-domain runtime, but they now duplicate large amounts of controller code for scene bootstrapping, HUD wiring, player inventory/detail routing, preview helpers, blueprint workflow plumbing, and smoke-oriented utility behavior. That duplication makes every new factory feature more expensive to ship twice, increases drift between the two demos, and raises the risk that a fix lands in one surface while the other silently regresses.

## What Changes

- Introduce a shared factory-demo runtime/composition layer that owns the common bootstrap flow used by both demos: environment setup, root-node creation, simulation/camera/player scaffolding, shared preview utility creation, and common teardown-safe helper methods.
- Extract the repeated controller logic for structure detail routing, player inventory transfer/selection, blueprint panel state coordination, and shared preview/material helper routines into reusable collaborators or base hooks instead of keeping near-identical copies in both demo scene scripts.
- Keep `FactoryDemo` focused on static world-grid sandbox authoring and keep `MobileFactoryDemo` focused on mobile-factory command/deploy/interior editing, with each demo supplying only scenario-specific state, HUD mapping, validation hooks, and authored content.
- Define explicit extension seams for site-specific placement validation, preview rendering, workspace selection behavior, and smoke/test orchestration so future factory features can be integrated once and adapted per demo instead of copied.
- Preserve current user-visible behavior and capability coverage for both demos while reducing script size, drift, and coupling.

## Capabilities

### New Capabilities
- `factory-demo-runtime-composition`: Shared composition rules for factory demo controllers so static and mobile demo scenes can reuse the same bootstrap, interaction-bridge, and preview helper contracts while keeping their scenario-specific behavior.

### Modified Capabilities

## Impact

- Affected code will center on `scripts/factory/FactoryDemo.cs` and `scripts/factory/MobileFactoryDemo.cs`, plus new shared support types under `scripts/factory/` for demo composition, HUD/detail/inventory bridging, preview overlay helpers, and scene bootstrap utilities.
- Related systems include `FactoryHud`, `MobileFactoryHud`, `FactoryPlayerHud`, `FactoryPlayerController`, `FactoryBlueprintSiteAdapter`, `FactoryPreviewVisuals`, `GridManager`, and `SimulationController` because both demos currently wire those systems directly.
- The main risk is architectural churn during extraction, so the change should preserve existing smoke coverage and keep demo-specific authored scenarios outside the shared layer.
