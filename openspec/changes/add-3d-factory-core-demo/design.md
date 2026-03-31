## Context

The repository is currently a Godot 4.6.1 .NET project whose main scene is a UI showcase built on `Control`. There is no 3D gameplay scene, no input map for factory controls, no world interaction layer, and no gameplay simulation loop. This change establishes the first gameplay-oriented slice by introducing a dedicated 3D demo scene and the minimum set of systems needed to navigate, build, and observe automated item flow.

The design needs to stay lightweight enough for a first implementation pass while still setting a clean foundation for future buildings, recipes, logistics, and progression. The project already uses C#, so the gameplay stack should stay in .NET rather than mixing scripting languages for core systems.

## Goals / Non-Goals

**Goals:**
- Create a playable 3D demo scene that loads as the primary game experience.
- Support a fixed-angle factory camera with bounded navigation and predictable controls.
- Support mouse hover, grid targeting, placement preview, placement, and removal on a buildable floor.
- Implement a minimal automation loop with a source, transport, and sink so item movement is visible and testable.
- Keep the architecture modular enough to add more structures and recipes without reworking the core loop.

**Non-Goals:**
- Full Factorio-scale logistics, crafting trees, research, enemies, or power management.
- Final art, polished UI, save/load, procedural world generation, or multiplayer.
- General free-look camera behavior or highly customizable keybinding UX in this first slice.

## Decisions

### Use a dedicated `FactoryDemo` 3D scene as the new main entry point

The project should introduce a new `Node3D`-based root scene for gameplay instead of trying to retrofit 3D behavior into the existing UI showcase. This keeps the prototype focused and allows the old UI scene to remain available as a reference or debug scene if needed.

Alternative considered: embedding 3D content inside the current `Control` scene. This was rejected because it would blur responsibilities and make startup, input routing, and world hierarchy harder to reason about.

### Model camera behavior as a rig with locked pitch and discrete rotation

The camera should be implemented as a hierarchy such as `CameraRig -> Pivot -> Camera3D`, with the pitch fixed to an isometric-style angle and yaw rotating only in explicit steps if rotation is supported. Pan moves the rig target within map bounds, zoom adjusts distance within min/max limits, and the camera always looks toward the build plane.

Alternative considered: a free orbit camera. This was rejected because free-look introduces drift and misalignment that conflicts with readable grid building.

### Use raycast-to-plane targeting plus an integer grid manager

Mouse interaction should resolve the cursor onto the build plane, then convert the hit position into integer grid coordinates owned by a `GridManager` or equivalent service. The grid manager will own occupancy, bounds, and structure lookup so camera/input code stays separate from placement rules.

Alternative considered: placing directly based on raw world coordinates. This was rejected because consistent snapping, occupancy checks, and future routing logic all benefit from an authoritative grid abstraction.

### Represent prototype structures as scene instances with lightweight simulation components

Each buildable structure should be a small scene or node composition with a companion C# script describing its type, footprint, facing, and simulation endpoints. A central simulation controller can tick registered structures and conveyors at a fixed cadence, while visual interpolation remains in `_Process`.

Alternative considered: simulating everything directly through scene tree polling without a controller. This was rejected because it would make item flow ordering and future expansion harder to control.

### Keep the first production loop intentionally narrow

The initial demo should include only the pieces needed to prove the loop: a resource source or miner, straight belt transport, and a storage/sink destination. Items can be represented as simple data tokens plus lightweight visuals moving along belt segments. Restricting the content set keeps the first implementation tractable while still exercising placement, orientation, transport, and throughput feedback.

Alternative considered: starting with assemblers, inserters, and branching belts. This was rejected because it would expand scope before the core interaction loop is stable.

### Add a small in-game overlay for build mode and demo telemetry

The demo should include a minimal `CanvasLayer` UI showing the current build selection, placement hints, and sink throughput or delivered item count. This helps verify the systems during implementation without depending on heavy interface work.

Alternative considered: no UI. This was rejected because debugging placement state and demo success would be much slower.

## Risks / Trade-offs

- [Rigid fixed-angle camera may feel too restrictive] -> Mitigation: allow bounded pan, zoom, and optional step rotation so players still have situational control without losing readability.
- [Central tick simulation can feel over-engineered for a prototype] -> Mitigation: keep the API minimal and only register the few prototype structures required for the demo.
- [3D placement feedback may be hard to read with placeholder art] -> Mitigation: use strong color-coded ghost previews, grid highlights, and simple high-contrast blockout meshes.
- [Switching the main scene may hide the existing UI work] -> Mitigation: keep the current UI showcase scene in the repository and only change startup routing.
- [Belt/item movement can become visually jittery if tied directly to simulation cadence] -> Mitigation: separate logical item advancement from per-frame visual interpolation.

## Migration Plan

1. Add the new gameplay scenes, scripts, and input actions without deleting the existing UI showcase assets.
2. Change `run/main_scene` to the gameplay demo once the scene loads and basic controls work.
3. Verify the project boots directly into the demo and that the full source-to-sink loop can be observed.
4. If rollback is needed, restore `run/main_scene` to `res://scenes/ui_showcase.tscn` and keep the gameplay assets disabled but available.

## Open Questions

- Should camera rotation ship in the first pass, or should the view stay locked to a single cardinal presentation until belts/buildings are stable?
- Should the initial source structure be a passive spawner or a mineable resource node plus miner building pair?
- How much debug visualization should remain enabled in the demo after the initial implementation lands?
