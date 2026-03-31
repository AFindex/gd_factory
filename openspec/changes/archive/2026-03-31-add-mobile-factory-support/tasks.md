## 1. Host Model Refactor

- [x] 1.1 Introduce a shared factory site/host abstraction for structure lookup, cell-to-world conversion, and placement context while keeping current world placement behavior unchanged
- [x] 1.2 Update `FactoryStructure` and related structure scripts to bind to a site/local-cell model instead of assuming a single permanent world-grid cell
- [x] 1.3 Refactor `FactoryStructureFactory` into a registry-driven creation path that can instantiate structures for either world placement or mobile-factory interiors

## 2. Grid Reservations And Preview

- [x] 2.1 Expand `GridManager` from single-cell occupancy to reservation records that support static structures, mobile footprints, and deployment ports
- [x] 2.2 Add validation helpers for reserving, querying, and releasing all cells owned by a mobile factory deployment
- [x] 2.3 Update build/deploy preview rendering so mobile factories show full footprint, port cells, and invalid overlaps before confirmation

## 3. Mobile Factory Lifecycle

- [x] 3.1 Implement a `MobileFactoryInstance` data/model layer that preserves factory identity, internal layout, and lifecycle state across deploy, recall, and redeploy
- [x] 3.2 Implement a `MobileFactorySite` plus deployment-port bridge objects that expose the mobile factory interior to the world only while deployed
- [x] 3.3 Add deploy, recall, and redeploy commands that enforce valid footprints, block movement while deployed, and cleanly release reservations on recall
- [x] 3.4 Allow the mobile factory interior editor to open in both deployed and in-transit states while preserving the same shared layout data

## 4. Simulation And Demo Integration

- [x] 4.1 Update `SimulationController` to rebuild cross-boundary topology at deploy/recall boundaries without duplicating or silently dropping items
- [x] 4.2 Create a dedicated `MobileFactoryDemo` scene and HUD flow for deploy/recall/redeploy interactions while keeping the existing `FactoryDemo` behavior largely unchanged
- [x] 4.3 Create a narrow mobile-demo content setup where a mobile factory's internal production chain connects to an external belt/sink loop after deployment
- [x] 4.4 Add a side-sliding split-view editor to the mobile factory demo that keeps an approximate `1:5` world-to-editor ratio when open
- [x] 4.5 Give the split-view editor its own dedicated camera, render surface, and build controls modeled after the factory demo interaction style
- [x] 4.6 Route mouse input by hover so the world strip and interior editor can be manipulated independently without explicit focus toggles
- [x] 4.7 Show mobile factory ports inside the editor with direction and external connection state overlays
- [x] 4.8 Add a synchronized world miniature presentation of the mobile factory that mirrors the shared interior layout and visible item flow

## 5. Verification And Documentation

- [x] 5.1 Extend smoke coverage to verify valid deployment, blocked deployment, recall cleanup, and successful post-redeploy item delivery in the dedicated mobile demo
- [x] 5.2 Update `docs/factory-demo-notes.md` with the distinction between the baseline static demo and the new mobile-factory demo, plus any new controls or debug behaviors
- [x] 5.3 Extend verification to cover split-view opening, hover-based input ownership, port-state overlays, and synchronization between the interior editor and the world miniature
