## Why

The current factory runtime assumes every structure is permanently anchored to one grid cell, which makes the simulation simple but prevents us from introducing vehicles, carriers, or relocatable production platforms. We need a mobile-factory model now so future gameplay can support factories that travel, deploy into the map, reconnect to logistics, and keep a coherent identity across those state changes.

## What Changes

- Add a mobile factory lifecycle that treats a factory as a persistent gameplay entity with transport, deployed, docking, and redeploy states instead of a one-time static placement.
- Extend the structure model so production nodes can belong either to the world grid or to a mobile factory interior, while sharing the same simulation contracts for item flow and ticking.
- Introduce a deployment boundary between a mobile factory and the world grid, including rules for reserving footprint cells, exposing connection ports, and blocking movement while the factory is actively deployed.
- Add an interior-editing interaction for mobile factories that can be opened whether the factory is deployed or in transit, using a side-sliding split view instead of a full scene handoff.
- Keep the world visible while editing by reserving roughly one sixth of the screen for the world view and five sixths for the interior editing view, with mouse focus determined by which side the pointer is hovering.
- Show a synchronized miniature representation of the mobile factory's internal layout and item flow in the world view so the player can understand the factory at a glance even when not editing.
- Update the simulation and placement layers so logistics connections can be rebuilt when a mobile factory deploys, moves, docks, or is recalled without duplicating item-processing logic.
- Keep the existing static `FactoryDemo` flow largely unchanged as the baseline automation sandbox, and add a separate demo scene dedicated to explaining the mobile-factory concept.

## Capabilities

### New Capabilities
- `mobile-factory-lifecycle`: Persistent mobile factory entities, deployment state transitions, docking rules, and world connection ports.
- `mobile-factory-interior-editing`: Split-view interior editing, hover-based input focus, connection overlays, and synchronized world miniatures for mobile factories.
- `mobile-factory-demo`: A separate demo scene that showcases deploying, recalling, and redeploying a mobile factory without replacing the current static factory demo.

### Modified Capabilities
- `factory-grid-building`: Placement and occupancy rules must support reservable multi-cell deployment footprints and connection points owned by a mobile factory instead of only standalone static structures.

## Impact

- Affects the factory runtime in `scripts/factory/`, especially `FactoryStructureFactory`, `FactoryStructure`, `GridManager`, and `SimulationController`.
- Adds a dedicated mobile-factory demo scene, split-view editor UI, and synchronized miniature presentation alongside the existing `FactoryDemo` instead of replacing it.
- Establishes the architecture needed for later features such as carrier vehicles, deployable outposts, moving harvesters, or faction bases that can relocate without rebuilding their internals.
