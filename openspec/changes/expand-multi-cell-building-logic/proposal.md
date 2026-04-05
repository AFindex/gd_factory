## Why

The current factory sandbox already tracks occupied cells and mobile-factory deployment footprints, but most placeable structures still behave like `1x1` anchors with single-cell assumptions in placement, previews, and interaction rules. That blocks richer Factorio-style layouts, makes mobile factories less expressive than the static sandbox, and prevents us from adding larger structures such as wide turrets or footprint-heavy support buildings without one-off hacks.

We need a shared multi-cell building model now so future structures can reuse the same occupancy, preview, combat, and logistics foundations across the world grid and mobile-factory interiors without splashing unrelated gameplay systems.

## What Changes

- Extend the core building and reservation model so a structure can declare a rotated multi-cell footprint instead of always occupying a single anchor cell.
- Update world-grid and mobile-factory interior placement validation, previews, and removal flows to operate on the full occupied-cell set for each structure.
- Introduce a dedicated multi-cell structures capability with example prototypes that exercise the new foundation, including a large turret that occupies multiple cells and fires independent projectiles.
- Expand existing tower-defense and mobile-factory editing behaviors so the new structures can be placed, previewed, simulated, and inspected in both static and mobile-factory contexts.
- Keep the change isolated to shared building, preview, and combat placement surfaces so existing production, power, and logistics behaviors continue to work for unchanged `1x1` structures.

## Capabilities

### New Capabilities
- `factory-multi-cell-structures`: Shared footprint definitions, rotated occupied-cell resolution, and example large structures for both world and mobile-factory construction flows.

### Modified Capabilities
- `factory-grid-building`: Placement, preview, occupancy, and removal rules must validate every required cell of a structure footprint instead of assuming a single cell.
- `factory-tower-defense`: Combat buildings must support larger defensive footprints and a large turret example that spawns independent projectile attacks.
- `mobile-factory-interior-editing`: Interior editing must preview, place, rotate, and remove multi-cell structures using the same build workflow as the world grid.

## Impact

- Affected code will likely center on `scripts/factory/GridManager.cs`, `scripts/factory/FactoryDemo.cs`, `scripts/factory/MobileFactoryDemo.cs`, `scripts/factory/FactoryPreviewVisuals.cs`, `scripts/factory/FactoryStructureFactory.cs`, `scripts/factory/FactoryTypes.cs`, and `scripts/factory/structures/*.cs`.
- Existing authored demo layouts, smoke coverage, and structure definitions will need targeted updates so the new footprint model is exercised without regressing legacy `1x1` behavior.
- No external dependencies are expected, but the change will add new spec deltas and example content for sandbox combat and mobile-factory construction.
