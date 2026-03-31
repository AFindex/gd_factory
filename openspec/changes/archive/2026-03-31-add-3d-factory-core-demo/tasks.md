## 1. Gameplay Scene Foundation

- [x] 1.1 Create a new `FactoryDemo` `Node3D` scene and supporting scene/script folders for gameplay systems and prototype structures
- [x] 1.2 Add or update input actions for camera pan, zoom, optional step rotation, build confirm, remove, and build selection controls
- [x] 1.3 Switch `project.godot` startup to the factory demo scene while keeping the existing UI showcase scene available in the repository

## 2. Camera And World Targeting

- [x] 2.1 Implement a fixed-angle camera rig with bounded pan and clamped zoom behavior for the playable map
- [x] 2.2 Add cursor-to-world targeting that resolves the hovered build-plane position into grid coordinates
- [x] 2.3 Add hover visualization and UI-input guarding so world actions do not trigger through interface controls
- [x] 2.4 Implement constrained camera rotation behavior if included in the first pass, otherwise lock the demo to a single valid facing

## 3. Grid Building Loop

- [x] 3.1 Implement a grid manager that defines build bounds, cell size, and occupancy tracking for placed structures
- [x] 3.2 Create prototype placeable structures and metadata for at least a producer, belt, and sink footprint/orientation
- [x] 3.3 Implement build preview visuals that follow the hovered cell and indicate valid versus invalid placement states
- [x] 3.4 Implement structure placement on valid cells and rejection on blocked or out-of-bounds cells
- [x] 3.5 Implement structure removal so occupied cells are released and layouts can be iterated during the demo

## 4. Factory Simulation Demo

- [x] 4.1 Implement a lightweight simulation controller with a deterministic tick for prototype factory entities
- [x] 4.2 Implement producer behavior that emits prototype items into an available downstream connection
- [x] 4.3 Implement straight belt transport with item progression and simple visual interpolation
- [x] 4.4 Implement sink behavior that accepts delivered items and records throughput or total delivered count
- [x] 4.5 Assemble a starter demo layout and default content so the automation loop is observable immediately on load

## 5. Demo UX And Verification

- [x] 5.1 Add a minimal overlay showing current build selection, controls, and delivery telemetry for the demo
- [x] 5.2 Add placeholder meshes, materials, and grid highlights that make structures and interaction states easy to read in 3D
- [x] 5.3 Test the full loop from startup to camera control, placement, removal, and source-to-sink delivery inside the demo scene
- [x] 5.4 Clean up obvious demo issues and document any intentional limitations left for later changes
