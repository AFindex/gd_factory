## 1. Shared Logistics Contracts

- [x] 1.1 Add new build prototype definitions, labels, and factory registration entries for storage and inserter structures.
- [x] 1.2 Introduce shared item-provider and item-receiver transfer contracts plus any small buffer/helper types needed for deterministic buffered exchange.
- [x] 1.3 Extend `SimulationController` and related base structure helpers so inserters, storage, belts, and legacy loader/unloader logic can resolve compatible providers and receivers without pairwise type checks.

## 2. Storage And Inserter Structures

- [x] 2.1 Implement the storage structure with bounded buffering, deterministic output order, and placeholder visuals that communicate fill state.
- [x] 2.2 Add structure inspection support and a storage contents panel that binds to the currently selected storage instance and refreshes while it is open.
- [x] 2.3 Implement the inserter structure with one-item pickup/hold/drop behavior, adjacency-based source and destination resolution, and visible transfer cadence.
- [x] 2.4 Adapt belts and any affected legacy transport helpers so they can participate in the new buffered transfer contracts at their logical endpoints.

## 3. Demo Integration

- [x] 3.1 Update `FactoryDemo` input flow to support a default interaction mode plus an explicit build mode that starts only after a prototype is selected.
- [x] 3.2 Update build selection, HUD copy, and selection state so storage and inserter prototypes can be placed, while interaction mode can select structures for inspection.
- [x] 3.3 Expand the scripted starter layout in `factory_demo` with multiple storage and inserter clusters, including storage fill/drain lines and mixed transport handoff routes.
- [x] 3.4 Refresh demo telemetry or status text as needed so storage/inserter activity and storage inspection are observable without obscuring the playfield.

## 4. Verification

- [x] 4.1 Add or update smoke/regression coverage to verify storage accepts and drains items, storage inspection can be opened from interaction mode, and inserters do not duplicate, delete, or teleport blocked transfers.
- [x] 4.2 Run the relevant project validation path for the factory demo and confirm the default startup layout still boots into a playable scene with active deliveries.
