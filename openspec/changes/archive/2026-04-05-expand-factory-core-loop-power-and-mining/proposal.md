## Why

The current sandbox proves that belts, buffers, inserters, combat pressure, and blueprint workflows all function, but its economy is still placeholder-driven: `ProducerStructure` creates finished items from nothing, recipe switching only changes output labels, and there is no power or resource extraction layer constraining the layout. We need a real input -> production -> output loop now so future combat, mobile factory, and blueprint work can build on an actual factory simulation instead of an infinite-source prototype.

## What Changes

- Replace the placeholder producer-first loop in the static sandbox with a resource-driven chain that starts from mineable deposits, passes through recipe-based manufacturing, and ends at meaningful outputs or sinks.
- Introduce true manufacturing structures and shared recipe definitions with declared inputs, outputs, cycle time, and power demand so assemblers behave like real factory machines instead of timed item spawners.
- Add a Factorio-inspired power network with generators, poles or coverage relays, supply-versus-demand accounting, and deterministic underpowered behavior for mining and manufacturing structures.
- Add a mining layer with authored ore patches, extractors that must be placed on valid deposits, and logistics handoff from extraction into downstream belts, storage, and inserters.
- Expand the static sandbox map to roughly three times its current playable footprint and author startup scenario slices that demonstrate powered mining, intermediate manufacturing, and final output delivery from first launch.
- Extend smoke coverage and sandbox use cases so the larger map continuously verifies extraction, power availability, manufacturing throughput, and delivery outcomes.
- Refresh new structure and resource presentation with a readable industrial visual language inspired by Factorio's clarity and silhouettes, without copying external assets directly.

## Capabilities

### New Capabilities
- `factory-resource-extraction`: Resource deposits, mining structures, deposit placement rules, and extraction handoff into the logistics network.
- `factory-power-grid`: Power generation, transmission coverage, consumer demand, and deterministic powered versus unpowered machine behavior.
- `factory-manufacturing-chain`: Recipe-driven manufacturing machines, intermediate inputs, and shared production rules for real input/output processing.

### Modified Capabilities
- `factory-production-demo`: The default static sandbox layout expands to a larger map and starts with authored end-to-end powered mining and manufacturing use cases instead of only placeholder logistics chains.

## Impact

- Affected systems include `FactoryDemo`, `FactoryHud`, `SimulationController`, `FactoryTypes`, `FactoryStructureFactory`, `FactoryStructureRecipes`, and the structure hierarchy under `scripts/factory/structures/`.
- The change will introduce new world-authored resource content, new structure kinds for extraction and power, and shared data models for recipe inputs, outputs, and machine power state.
- Smoke tests, authored starter layouts, and structure detail surfaces will need updates so players can inspect and validate power, mining, and manufacturing behavior on the larger sandbox.
