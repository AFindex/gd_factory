## Why

The current `factory_demo` proves basic production, belts, splitters, mergers, bridges, and sink delivery, but it still lacks the richer transfer primitives needed to prototype an actual factory floor. We need storage-backed buffering and a Dyson Sphere Program-inspired inserter so the logistics layer can move beyond fixed one-cell input/output chains and support more representative layouts before further content is added.

## What Changes

- Add a storage structure that can buffer multiple items, visually expose its fill state, and accept input from logistics nodes or direct machine transfers.
- Add a storage output mode so a storage structure can actively serve items to downstream logistics, instead of acting only as a passive endpoint.
- Add a storage inspection panel so players can open a storage building in interaction mode and review its buffered contents.
- Add an inserter-style mechanical arm structure that picks up from any compatible adjacent source, including belts, storage, producers, sinks configured as collection targets, and future machine-like structures.
- Introduce a shared logistics transfer contract for structures that can offer or receive items through buffers, so belts, storage, and machines can interoperate without hard-coded pairwise rules.
- Change the factory demo interaction flow so the player defaults to a non-build interaction mode and only places structures after explicitly selecting a buildable prototype.
- Expand the authored startup content in `factory_demo.tscn` so the scene includes storage-fed lines, inserter-fed exchanges, and more logistics instances that stress the new behavior from the first frame.

## Capabilities

### New Capabilities
- `factory-storage-and-inserters`: Storage buffers, storage output behavior, and generalized inserter pickup/drop-off across belts and machine-like structures.

### Modified Capabilities
- `factory-grid-building`: Placement becomes an explicit build-mode action instead of the default click behavior, preserving a separate interaction mode for selecting structures.
- `factory-production-demo`: The default static factory demo includes authored storage and inserter layouts that exercise the new logistics layer without manual setup.

## Impact

- Affected systems include `FactoryTypes`, `FactoryStructureFactory`, `SimulationController`, and the existing structure hierarchy under `scripts/factory/structures/`.
- The change will likely introduce new structure scripts for storage and inserters plus a shared transfer interface or buffer helper for non-belt item exchange.
- `FactoryDemo` and the demo HUD/build selection flow will need updates so the new structures can be placed, inspected, observed, and smoke-tested in the default startup scene.
