## Why

The project currently ships a UI showcase scene, but it does not yet establish the gameplay foundation for a 3D factory builder. We need a first playable slice now so the project can move from visual experimentation into a concrete game loop with camera, placement, and production behaviors that future systems can build on.

## What Changes

- Replace the current "UI showcase as main experience" direction with a 3D factory demo scene that becomes the project's gameplay baseline.
- Add a fixed-angle 3D camera rig with constrained pan, zoom, and rotation behavior suitable for a factory-building perspective without allowing free-look drift.
- Add mouse-driven world interaction, including ground hover feedback, grid-aware selection, and build/remove actions for prototype structures.
- Add a minimal factory simulation skeleton with placeable machines, belt transport, resource spawning, and delivery to a sink so players can observe an end-to-end automation loop.
- Add a demo-friendly content set and scene wiring so the core systems can be tested immediately inside a single map.

## Capabilities

### New Capabilities
- `factory-camera-and-input`: Fixed-angle camera controls and mouse interaction for navigating and targeting the 3D factory space.
- `factory-grid-building`: Grid-based world, placement rules, build preview, and structure lifecycle for prototype factory entities.
- `factory-production-demo`: Minimal producer-to-belt-to-consumer simulation loop and a playable demo scene that showcases the core factory flow.

### Modified Capabilities
- None.

## Impact

- Affects the project's main gameplay scene, input map, and startup flow in [project.godot](D:/Godot/projs/net-factory/project.godot).
- Introduces new 3D scenes, .NET scripts, and prototype content under the existing scene/script structure.
- Establishes the baseline architecture for future logistics, building catalogs, and progression systems.
