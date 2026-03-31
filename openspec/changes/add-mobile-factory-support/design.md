## Context

The current gameplay slice is built around static world placement. `FactoryStructureFactory` creates a `FactoryStructure`, configures it with a single world cell, and `GridManager` stores occupancy in a `Dictionary<Vector2I, FactoryStructure>`. `SimulationController` then ticks those placed structures assuming their topology changes only when a structure is added or removed from the map.

That model works for the current demo, but it breaks down as soon as a "factory" needs to move as a persistent object. A mobile factory needs to preserve its identity, internal layout, and buffers while switching between transport and deployed states. It also needs to expose world-facing ports only when deployed, reserve more than one cell, and reconnect to external logistics without duplicating every existing structure type into separate static/mobile implementations.

The design should stay compatible with the current lightweight prototype. We want the first implementation to prove the mechanism in a dedicated mobile-factory demo without forcing a total rewrite of the existing static `FactoryDemo` scene or its baseline loop.

## Goals / Non-Goals

**Goals:**
- Introduce a mobile factory concept with a persistent identity and explicit lifecycle states such as transport, deploying, deployed, and recalling.
- Allow existing structure logic to run either on the world grid or inside a mobile factory interior without duplicating simulation code.
- Support deploy/recall/redeploy flows that reserve a multi-cell footprint, expose world connection ports, and rebuild logistics links deterministically.
- Allow the player to edit a mobile factory's internal layout whether the factory is deployed or in transit without losing awareness of the surrounding world.
- Refactor the current factory creation path so new structure kinds and mobile-factory-contained structures can be instantiated through the same extension point.
- Keep the existing static factory demo largely intact while adding a second demo scene focused on the mobile-factory concept.
- Keep the initial implementation demo-friendly and scoped to one playable proof of relocation.

**Non-Goals:**
- Full vehicle navigation, autonomous world pathfinding, combat, or free-roaming physics for the mobile factory.
- Save/load migration, multiplayer synchronization, or a final UX for production management.
- Arbitrary nested factories, concurrent movement of many deployed factories, or a full docking network in the first slice.

## Decisions

### Separate factory identity from deployment state

Introduce a persistent gameplay object such as `MobileFactoryInstance` that owns the mobile factory's state, internal structure graph, buffers, and deployment metadata. World placement becomes one state of that instance, not the definition of the factory itself.

This keeps recall/redeploy behavior coherent because the same mobile factory survives state transitions instead of being destroyed and recreated each time it moves.

Alternative considered: treat a mobile factory as a single `Node3D` that physically moves while all internal logic stays world-bound. This was rejected because current structures bind directly to world cells, so moving the node would not solve occupancy, connection ports, or deterministic reconnection.

### Generalize structure hosting with a site abstraction

Refactor structure placement so `FactoryStructure` knows its local cell and owning site, while world-space transforms come from that site. A lightweight abstraction such as `IFactorySite` can provide cell bounds, local-to-world conversion, structure lookup, and port exposure.

The first two site implementations should be:
- `WorldFactorySite`: backed by the current world grid and used by ordinary placed structures.
- `MobileFactorySite`: backed by a mobile factory's internal layout and activated through deployment ports when the factory is deployed.

This allows belts, producers, sinks, and future machines to keep one simulation contract while becoming portable between the world and a mobile interior.

Alternative considered: create separate `MobileBeltStructure`, `MobileProducerStructure`, and similar subclasses. This was rejected because behavior would diverge quickly and every logistics fix would need to be implemented twice.

### Replace single-cell occupancy with reservation records

`GridManager` should evolve from a `cell -> structure` dictionary into reservation records keyed by cell, with an owner id and reservation type such as `StaticStructure`, `MobileFootprint`, or `MobilePort`. Static 1x1 placement still works through the same manager, but mobile factories can now reserve multiple cells plus explicit connection cells during deployment.

This makes deployment validation predictable and gives one authoritative source for collision checks, recall cleanup, and future multi-tile structures.

Alternative considered: keep the existing occupancy dictionary and special-case mobile factories outside it. This was rejected because placement, preview, and removal would then need two conflicting validation systems.

### Model deployment ports as explicit bridge nodes

A deployed mobile factory should expose one or more port bridges that connect its internal site to the world site. The bridge is the only place where items cross the boundary, and it is activated or deactivated during deploy/recall transitions.

This creates a clean seam for the simulation: internal structures continue to talk to their local site, world structures continue to talk to the world site, and the port bridge translates between them.

Alternative considered: let internal structures query the world grid directly whenever the factory is deployed. This was rejected because it would leak world knowledge into portable structures and make recall behavior fragile.

### Apply topology changes only at controlled lifecycle boundaries

`SimulationController` should treat deploy, recall, and redeploy as explicit topology rebuild events. On those events it updates registered bridge nodes, refreshes affected structure routing, and preserves or flushes edge buffers according to a single rule set.

For the first slice, the safe rule should be:
- Internal buffers remain owned by the mobile factory across recall.
- World-bound transit items that have not entered the factory stay in world structures.
- Port bridge buffers are emptied or handed back before the bridge is removed, so items are neither duplicated nor lost silently.

Alternative considered: recompute cross-boundary connectivity opportunistically every tick. This was rejected because it increases the chance of race-like bugs and item duplication during movement.

### Turn `FactoryStructureFactory` into a registry-driven creation boundary

The current switch-based `FactoryStructureFactory` is fine for a tiny static demo, but mobile factories need the same structure types to be created in different hosts. Replace the hard-coded switch with a registry of structure definitions that can create a structure and configure it for a target site/context.

The registry should capture:
- Prototype kind and display metadata
- Footprint and port metadata
- A creator delegate or constructor binding
- Optional flags for "world placeable", "mobile interior allowed", or "requires deployment port"

This makes the factory class the extensibility boundary for both static and mobile content instead of a static helper that only knows how to spawn world-anchored nodes.

Alternative considered: keep adding constructor overloads and new switch branches. This was rejected because the factory would become the place where every hosting rule leaks in ad hoc form.

### Prove the feature with one mobile outpost demo loop

The first playable proof should live in a dedicated `MobileFactoryDemo` scene rather than being folded into `FactoryDemo`. That new scene should use a single mobile factory that contains a small internal production chain and exposes one output port to the world. The player deploys it on a valid footprint, watches items feed into an external belt/sink chain, recalls it, and redeploys it elsewhere to restore the loop.

This demo is narrow enough to implement quickly while still proving the architecture that future moving harvesters or carrier bases will rely on, and it preserves the current demo as a stable comparison point.

Alternative considered: retrofit the current `FactoryDemo` so it becomes the mobile-factory showcase. This was rejected because it would blur the boundary between the existing static baseline and the new concept demo, making regressions harder to spot.

### Use a side-sliding split view for interior editing instead of a full-screen transition

When the player opens a mobile factory for editing, the game should keep the world view visible as a narrow strip and slide in a dedicated interior editor that occupies most of the screen. The target layout for the first pass should be approximately a `1:5` world-to-editor ratio so the player keeps world context without sacrificing interior build readability.

The interior side should behave like a self-contained build surface with its own camera, controls, and overlays, rather than trying to zoom the world camera into the factory hull.

Alternative considered: a pure camera zoom into the world model. This was rejected because it makes internal cell picking, occlusion, and readability much harder than a dedicated editor surface.

### Route mouse ownership by hover between world and editor panes

The split view should not require the player to explicitly toggle "which side is active." Instead, mouse input should belong to whichever pane the pointer is currently hovering. Hovering the world strip means clicks and wheel input affect the world camera or world selections; hovering the interior editor means they affect internal building tools.

This keeps the interaction lightweight and makes the split screen feel like one cohesive workspace instead of two separately armed modes.

Alternative considered: explicit focus switching buttons or hotkeys. This was rejected because it adds friction to an interaction the player may repeat very often.

### Share one interior layout model across the editor and the world miniature

The mobile factory should expose the same underlying interior layout to two different presentations:
- a full-size editable internal view in the split editor
- a miniature, read-only world representation on the factory model itself

The world miniature should mirror the arrangement and item flow of the internal layout, while still allowing visual simplification for readability at small scale.

Alternative considered: a decorative world-shell model unrelated to the editable interior. This was rejected because it would make the editor feel disconnected from the factory the player is operating in the world.

### Show ports and external connection state directly in the editor

The interior editor should treat world-facing ports as first-class build context. Port cells should stay visible in the editor and show direction plus whether they are currently connected to an external world line.

This lets the player understand how internal layout choices affect the deployed factory without needing to guess from the outside.

Alternative considered: only show ports in the world view. This was rejected because it forces the player to mentally map edge cells between two views without enough feedback.

## Risks / Trade-offs

- [The host/site abstraction adds indirection to a currently simple prototype] -> Mitigation: keep the interface minimal and migrate existing world structures onto it before adding mobile-only behavior.
- [Deploy/recall could duplicate or drop items at the world boundary] -> Mitigation: centralize bridge teardown rules in `SimulationController` and cover them with focused smoke tests.
- [Reservation records make `GridManager` more complex] -> Mitigation: preserve a simple query API (`CanPlace`, `TryGetOccupant`, `ReserveCells`, `ReleaseOwner`) so callers do not need to understand storage details.
- [A mobile factory interior may tempt the project into building a second whole map] -> Mitigation: scope the first slice to a tiny internal layout and one visible external connection.
- [Registry-based creation can feel heavy for eight prototype kinds] -> Mitigation: start with a thin registry wrapper around the existing constructors and expand only where mobile hosting needs metadata.
- [Two demo scenes can drift apart in controls or presentation] -> Mitigation: share common runtime systems and keep the new scene focused on the mobile-factory lifecycle rather than cloning all static-demo content.
- [Split view can feel visually crowded] -> Mitigation: keep the world strip intentionally narrow and reserve most of the screen for the interior editor.
- [A world miniature could become unreadable at small size] -> Mitigation: keep the data synchronized but allow the miniature presentation to exaggerate key structures and item flow for readability.

## Migration Plan

1. Introduce the site/host abstractions and move current world structures to the new placement API without changing visible behavior.
2. Expand `GridManager` to support reservation records and adapt static placement/preview to the new validation path.
3. Refactor `FactoryStructureFactory` into a registry-based creator that can instantiate structures for either the world site or a mobile site.
4. Add `MobileFactoryInstance`, deployment ports, and lifecycle commands for deploy/recall/redeploy.
5. Add a side-sliding split-view editor for the mobile factory demo with an approximately `1:5` world-to-editor layout and hover-based input ownership.
6. Add a synchronized world miniature that mirrors the editable interior layout and item flow of the mobile factory.
7. Extend smoke coverage to include successful deploy, blocked deploy, recall cleanup, post-redeploy item delivery, and the editor/world synchronization rules.

Rollback strategy: disable the dedicated mobile-factory demo flow and keep all placements on `WorldFactorySite`, while leaving the host abstraction in place if it has already replaced the old direct-world API. The original `FactoryDemo` remains available as the unaffected fallback experience.

## Open Questions

- Should a mobile factory's internal simulation continue while the factory is in transport, or should transport pause all production until redeployment?
- Should the first mobile factory footprint be a simple rectangle, or do we need asymmetric footprints and rotated port layouts immediately?
- When a factory redeploys next to an already built belt line, should ports auto-connect if aligned, or should the player explicitly rebuild the world-side connector each time?
