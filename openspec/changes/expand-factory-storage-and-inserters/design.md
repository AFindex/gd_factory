## Context

The project is a Godot 4.6 .NET factory prototype with a static `factory_demo` scene whose startup content is currently authored in code inside `FactoryDemo.CreateStarterLayout()`. The existing logistics layer is centered on `FactoryStructure`, `FlowTransportStructure`, `GridManager`, and `SimulationController`, with most transfers modeled as direct front-to-back sends between adjacent structures.

That architecture is enough for belts, splitters, mergers, bridges, loaders, unloaders, producers, and sinks, but it does not yet expose a general concept of "this structure can offer buffered items" versus "this structure can receive buffered items." As a result, any new logistics primitive would otherwise require more pairwise exceptions like the current loader/unloader transport-only rules.

The requested change extends the bottom-layer factory sandbox with storage and a mechanical inserter inspired by Dyson Sphere Program sorters. The DSP research is most useful as design guidance rather than a literal clone: sorters are directional, bridge distinct provider and receiver types, and become part of the factory planning puzzle because range, throughput, filtering, and blockage all matter. For this prototype, the important transferable ideas are explicit source-to-target transfer, visible arm cadence, compatibility across belts and machine-like buildings, and deterministic blocking behavior.

## Goals / Non-Goals

**Goals:**
- Add a storage structure that can buffer multiple items and participate in both input and output logistics.
- Add a storage inspection panel that exposes the current buffered contents of a selected storage structure.
- Add an inserter-style arm that transfers items between adjacent compatible structures on the simulation step instead of teleporting them.
- Introduce a reusable transfer abstraction so storage, producers, sinks, loaders/unloaders, belts, and future machines can exchange items without adding one-off rules for every pair.
- Split the demo input flow into an interaction mode and an explicit build mode so clicks can inspect buildings without risking accidental placement.
- Expand the authored factory demo layout so the default scene showcases storage buffering, storage discharge, inserter-fed handoff, and recovery/recycling-style routes.
- Keep the implementation lightweight enough to fit the current single-grid, placeholder-art demo without requiring a full recipe or inventory overhaul.

**Non-Goals:**
- Full Dyson Sphere Program parity such as multi-tier sorter research, arbitrary reach lengths, power consumption, stack-size upgrades, or per-slot storage filters.
- Replacing the current belt transport simulation with lane-based or spline-authored conveyor logic.
- Adding a full crafting machine inventory model, recipe UI, or save/load support in this change.
- Reworking `factory_demo.tscn` into a manually authored node-heavy scene; the startup layout can remain script-authored if the authored content becomes richer.
- Building a full inventory management window system with drag/drop slot editing or multi-building selection in this change.

## Decisions

### Introduce provider/receiver transfer interfaces for buffered exchange

Add a pair of logistics-facing contracts for structures that can expose or receive discrete items outside of the belt-only push path. The exact names can follow project style, but the design assumes concepts equivalent to:

- an item provider interface that can report whether it has an item available for a requesting cell and atomically hand one out
- an item receiver interface that can report whether it can accept a pending item from a source cell and then store or consume it

`SimulationController.TrySendItem()` remains the right abstraction for flow structures that push forward into another structure, but inserters and storage need a pull-then-push pattern. The controller should therefore gain a small set of helper methods to resolve adjacent structures as providers/receivers and keep the actual item ownership transition deterministic.

Alternative considered: extending `FactoryStructure.TryAcceptItem()` until it also handled extraction. This was rejected because "accept an incoming item" and "offer an outgoing item" have different preconditions and would keep coupling new behaviors to ad hoc source-kind checks.

### Model storage as a buffered machine-like structure with explicit capacity and forward output participation

Add a dedicated storage structure backed by a small item queue or ring buffer. It should:

- accept incoming items through its configured input side or inserter-compatible receiver path
- expose a stable output side so downstream belts or inserters can pull from it
- reject new items when capacity is full
- surface fill state visually and in HUD/demo telemetry where useful

This gives the prototype a reusable buffer primitive that can sit between producers and belts, act as a short-term reservoir ahead of sinks, or feed other machines later. A single logical storage type is enough for now; larger boxes, stacking, filtering, and slot limits can be future upgrades once the basic transfer contract exists.

Alternative considered: treating storage as a special sink with counters only. This was rejected because the user explicitly needs storage output, and a terminal sink model would not exercise bidirectional factory flow.

### Expose storage contents through a lightweight inspection panel

Storage needs a simple panel that opens when the player is in interaction mode and selects a storage building. The panel should display the buffered contents in a concise list or grid, tied to the selected structure instance rather than a global abstract total.

To keep scope contained, the first panel can be HUD-driven and read-only:

- it opens only for structures that support inspection
- it refreshes live while the selected storage remains valid
- it closes when the player clears selection, switches into build mode, or removes the structure

Alternative considered: embedding storage contents directly in the always-visible HUD. This was rejected because buffered contents are building-specific, and the user explicitly asked for panel-style inspection in interaction mode.

### Make the inserter a one-cell, one-range transfer bridge with DSP-inspired behavior

The first inserter should stay compatible with the current grid and placement model by occupying one cell and using its opposite sides as pickup and drop directions. In practice:

- the cell behind the inserter is the pickup side
- the cell in front of the inserter is the drop side
- the inserter swings on the fixed simulation step, holding at most one item at a time
- it waits if the source has nothing or the destination is blocked

This keeps placement simple, preserves deterministic topology, and still captures the key DSP design pattern: an explicit arm bridges two neighbors that may be different structure classes. It also creates a clean upgrade path for later features such as side-pick variants, longer reach tiers, filters, or faster arm classes.

Alternative considered: full DSP-style arbitrary source/target port selection per placement. This was rejected for the first pass because the current world interaction model only supports single-click placement plus facing, and adding two-endpoint selection would inflate both UX and implementation scope.

### Treat belts as compatible providers/receivers only at their logical endpoints

The inserter does not need to grab from arbitrary positions on a moving belt segment to satisfy the requested prototype. Instead, belts should expose buffered exchange only where their current logic already has clear meaning:

- a belt can provide an item to an adjacent inserter from the belt's output-facing side when its leading item has reached the dispatch threshold
- a belt can receive an item from an adjacent inserter on the belt's input-facing side using the same capacity checks already used for upstream transport

This preserves the existing `FlowTransportStructure` cadence, avoids spatial ambiguity around side-grabbing from the middle of a segment, and still makes belts, storage, producers, and sinks interoperable through the same arm abstraction.

Alternative considered: allowing inserters to sample any adjacent belt regardless of the belt's direction or item position. This was rejected because it would add hidden rules, weaken visual readability, and make throughput/debugging harder to reason about.

### Keep loaders/unloaders as legacy directional helpers, but align them to the new transfer contracts

Loaders and unloaders already represent specialized one-cell bridges between transport nodes and machine-like nodes. They should remain placeable so existing layouts do not regress, but their logic should be implemented on top of the new provider/receiver abstractions where possible.

That alignment keeps old demo patterns working while reducing duplicated compatibility logic. Over time, inserters may subsume some loader/unloader use cases, but this change should not force an immediate removal or rename.

Alternative considered: deprecating loaders/unloaders immediately in favor of inserters. This was rejected because the current demo, smoke tests, and player affordances already use them, and removing them would turn an additive logistics change into a migration-heavy redesign.

### Default the demo to interaction mode and require explicit build selection for placement

The current demo assumes clicks place or remove structures as long as a prototype is selected. That is no longer sufficient once structures also need to be selected and inspected. The input model should therefore separate:

- interaction mode: clicking selects an existing structure and opens any supported panel content
- build mode: entered only after the player explicitly chooses a buildable prototype, with placement preview and confirm/remove behavior

Returning to interaction mode should be cheap and obvious, such as via a cancel action, selecting an already active tool off, or completing a user-visible deselect flow. This keeps structure inspection reliable and reduces accidental placement while preserving the current fast iteration loop.

Alternative considered: keeping build mode always active and using modifier keys for inspection. This was rejected because it hides a core interaction behind mode-less shortcuts and conflicts with the requested "only selected building types should build" behavior.

### Expand the default factory demo as a scripted logistics proving ground

`factory_demo.tscn` is currently just a script entry point, so "adding more instances" should be interpreted as expanding the startup layout and visible system count rather than manually placing scene children. The new authored layout should include at least:

- a producer-to-storage buffer that drains back onto belts
- a belt-to-storage and storage-to-belt inserter pair
- a storage-fed recycling or collection corridor that ends in sinks
- at least one denser zone where multiple inserters and storage boxes operate at once

The demo should remain immediately playable on boot, and the new instances should coexist with the earlier splitter/merger/bridge patterns so the scene becomes a broader regression bed instead of replacing one narrow example with another.

Alternative considered: creating a second demo scene just for storage/inserter behavior. This was rejected because the user asked to expand `factory_demo.tscn`, and the existing static demo is already the startup regression slice.

## Risks / Trade-offs

- [Generalized transfer interfaces may add indirection to a currently simple simulation] -> Mitigation: keep the interfaces narrowly scoped to single-item availability/acceptance and use them only where buffered exchange is needed.
- [Storage can hide throughput bugs by buffering over transient stalls] -> Mitigation: keep capacity explicit, reject overflow deterministically, and add demo/test scenarios that observe both fill and drain behavior.
- [A front/back one-range inserter may feel more limited than DSP sorters] -> Mitigation: document it as the baseline tier and leave a clear upgrade path for reach, speed, and filter extensions once the core contract is stable.
- [Mixing belts, storage, and arms in one scene can increase visual clutter] -> Mitigation: give storage and inserters strong silhouette contrast and organize the startup layout into distinct topology clusters.
- [Legacy loader/unloader logic could drift from inserter behavior if both remain] -> Mitigation: refactor them onto shared transfer helpers so compatibility checks live in one place.
- [Adding inspection UI and mode switching can make demo input feel heavier] -> Mitigation: keep interaction mode as the default, keep build mode entry explicit but one-click, and ensure panel/open-close behavior is immediate and minimal.

## Migration Plan

1. Add the new enums, definitions, interfaces, and shared buffer/helper types needed for storage and inserter simulation.
2. Implement storage and inserter structures, then adapt belts and legacy bridge structures to the shared transfer rules without breaking existing starter layouts.
3. Extend `FactoryDemo` selection/HUD wiring with interaction mode, build mode, and storage inspection support, then enlarge the scripted startup layout so storage/inserter clusters appear on first load.
4. Update smoke or regression coverage to assert that storage can fill and drain and that inserters stall gracefully instead of teleporting or dropping items.
5. If rollback is needed, remove the new structure kinds from build selection and starter layout while leaving the underlying helper abstractions isolated from unrelated mobile-factory systems.

## Open Questions

- Should the first storage structure accept and output through only its facing-defined sides, or should inserters be allowed to access storage from any orthogonally adjacent cell for a more box-like feel?
- Should sinks remain pure consumers, or is it worth exposing a limited provider mode so an inserter can also pull back items from designated collection nodes in the prototype?
- Does the team want the first inserter tier to expose an item filter immediately, or is source/target compatibility alone enough for the initial implementation?
- Should the inspection panel pause placement previews entirely while open, or is closing it automatically on build-mode entry enough for the first pass?
