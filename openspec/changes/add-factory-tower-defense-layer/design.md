## Context

The project is a Godot 4.6 .NET prototype whose `Factory Sandbox` scene (`res://scenes/factory_demo.tscn`) is assembled almost entirely in code by `FactoryDemo`. The current runtime already supports deterministic logistics on a fixed simulation step, buildable world structures, storage/inserter item transfer, and a compact HUD, but it does not yet model combat, structure durability, enemy actors, or item typing beyond broad source-structure identity.

That matters for this request because the desired tower-defense layer cuts across several existing abstractions at once:

- structures need persistent health, damage intake, destruction, and attack feedback
- logistics needs at least one dedicated ammunition item flow rather than treating every item as interchangeable
- the sandbox scene needs authored defense cases that are stable enough for regression testing
- enemies and turrets need deterministic update order so the prototype remains debuggable

Factorio is the best design reference for the requested feel, but not as a one-to-one clone. The transferable ideas for this first pass are:

- turrets are only as good as the ammo flow that feeds them
- walls and sacrificial perimeter pieces buy time for production to keep defenses stocked
- enemies create pressure by attacking reachable factory assets along readable approach lanes
- damaged defenses must be visible at a glance so the player understands when attrition is winning

The parts we should explicitly avoid for this change are full pollution simulation, evolution scaling, large tech trees, or freeform pathfinding across the whole map.

## Goals / Non-Goals

**Goals:**
- Add a lightweight combat framework that makes world structures damageable and destructible.
- Add a small tower-defense building roster that fits the current grid and logistics model.
- Add ammo-fed combat so at least one turret depends on the existing factory supply chain.
- Add simple enemy units that can pressure authored lanes and attack walls, turrets, or production buildings.
- Add in-world combat readability, including health bars and visible under-attack state.
- Expand `Factory Sandbox` with authored defense validation cases that showcase successful resupply and intentional failure.
- Keep simulation deterministic and compatible with the current code-built sandbox workflow.

**Non-Goals:**
- Full Factorio parity such as pollution clouds, enemy evolution tiers, nests spreading across the map, or roboport-based repair automation.
- A deep combat tech tree with many ammo calibers, upgrades, resistances, or status effects.
- Freeform navigation/pathfinding across arbitrary factory layouts in the first implementation.
- Rebuilding the sandbox scene as a manually authored node hierarchy; scripted layout generation remains acceptable.
- Extending combat into the mobile-factory demo or test scenario in this change.

## Decisions

### Extend the existing fixed-step simulation with a dedicated combat phase

Combat should run inside `SimulationController` on the same deterministic physics cadence already used for logistics. Instead of creating an unrelated real-time combat loop in `_Process`, add lightweight registration and update hooks for hostile actors and combat-capable structures, then execute them on the fixed step in a stable order.

This keeps debugging sane: ammo consumption, attack cooldowns, damage exchange, and destruction all happen on the same timeline as belts and inserters. It also makes authored sandbox cases repeatable enough for future smoke coverage.

Alternative considered: updating enemies and turrets in `_Process` or per-node timers. This was rejected because it would decouple combat from the logistics tick and make ammo starvation, cooldowns, and destruction timing harder to reason about.

### Add durability and damage directly to `FactoryStructure` via a small combat contract

Every world-placeable structure should expose a combat-facing profile with max health, current health, destroyed state, and optional attackability metadata. The simplest shape is to extend `FactoryStructure` itself with shared health handling and add an interface for things that can receive damage or provide combat state to HUD/visual systems.

This is preferable to a separate component framework because the current codebase uses inheritance-heavy placeholder structures and builds most visuals at runtime. Centralizing the base durability contract in `FactoryStructure` makes destruction, health bars, and removal from the grid/simulation consistent across belts, storage, turrets, and walls.

Alternative considered: adding a separate child node component for health on only selected buildings. This was rejected because the user explicitly asked for every building to have health and attack behavior, and duplicating destroy/unregister logic across many components would add unnecessary indirection.

### Introduce explicit item typing so ammunition is a real logistics payload

`FactoryItem` currently only communicates which structure produced it. That is not enough once turrets must consume ammo instead of arbitrary logistics items. Add a lightweight item kind or payload type, such as `FactoryItemKind`, so the sandbox can distinguish generic cargo from ammunition magazines and any future specialized payloads.

Ammo-producing structures emit ammo items, storage buffers them unchanged, inserters move them like any other discrete item, and ammo-fed turrets accept only supported ammo kinds into an internal magazine buffer.

Alternative considered: treating ammunition as "any item produced by a certain structure kind." This was rejected because it would bake combat rules into source-building checks, make ammo UI unclear, and limit future extension to multiple payload categories.

### Start with a small defensive roster that maps cleanly to the current grid

The first-pass roster should be intentionally small and legible:

- a perimeter blocker such as `Wall`, with high health and no ammo usage
- an `AmmoAssembler` or equivalent producer that generates ammunition items into the logistics network
- an ammo-fed `GunTurret` that targets one enemy at a time within range and stops firing when empty

This roster is enough to capture the requested loop: production creates ammunition, logistics moves it, walls buy time, turrets spend it, enemies create pressure, and damaged structures expose the consequences. If we want a second turret variant later, it can reuse the same ammo and targeting foundations.

Alternative considered: launching with several turret classes, armor types, and repair buildings at once. This was rejected because the current prototype benefits more from one clean end-to-end combat loop than from a broad but shallow roster.

### Represent enemies as lightweight lane-based hostile actors instead of full pathfinding agents

The sandbox should use scripted spawners and authored waypoints or lanes. Enemies move along those lanes until they reach a blocking structure or a preferred target, then attack on cooldown. The first pass should include at least:

- a melee raider that must close distance and chew through walls or buildings
- a ranged raider that pauses at stand-off distance and pressures turrets or logistics pieces behind the wall

This approach captures the key Factorio-like readable pressure pattern, where a perimeter matters and breaching has visible consequences, without paying the complexity cost of global navigation meshes or adaptive routing around arbitrary builds.

Alternative considered: full grid pathfinding around player-built obstacles. This was rejected for the first pass because it would dominate scope, and the requested sandbox use cases can be satisfied by deterministic authored lanes.

### Treat turrets as logistics receivers with their own ammo buffer

Ammo-fed turrets should participate in the existing provider/receiver system rather than inventing a bespoke reload mechanic. A turret can accept ammo items from adjacent inserters, belts, storage, or direct forward sends if they satisfy its receiver rules, then store a small internal ammo buffer. Firing consumes buffered ammo and spawns damage against a valid enemy target.

This keeps combat connected to the factory rather than to magical UI refills. It also reuses the strongest part of the current prototype: deterministic item movement between structures.

Alternative considered: allowing the player to click turrets to reload them directly. This was rejected because the request specifically asks for defenses that require ammunition, and the most meaningful version of that in this project is a logistics-driven supply chain.

### Render health bars and attack state as lightweight world overlays, not heavy UI windows

Each structure should own a compact world-space health bar or billboard that becomes visible when the structure is damaged, hovered, selected, or under attack. Under-attack feedback can reuse tint pulsing or emissive flashes. The HUD can summarize combat pressure with aggregate counters, but the primary readability should live in the world near the affected building.

This matches the user's ask for "a health bar and attacked state" while keeping the sandbox readable during moment-to-moment observation. It also avoids forcing the player to inspect a side panel just to understand which turret is collapsing.

Alternative considered: exposing health only in the left HUD inspection panel. This was rejected because combat information needs to be legible at a glance during active attacks.

### Expand the scripted `Factory Sandbox` into authored logistics-plus-combat case studies

`FactoryDemo.CreateStarterLayout()` should gain a few clearly separated tower-defense subareas rather than one giant mixed battlefield. The initial authored cases should include:

- a stocked defense lane where ammo production and delivery keep a turret alive behind walls
- a starvation lane where the turret starts with or quickly reaches no ammo, letting enemies breach
- a mixed-pressure lane where melee and ranged enemies damage different structure types

These setups make the new systems visible immediately on load and provide future smoke-test anchors. They also mirror the existing style of the project, where important gameplay slices are composed as authored test beds inside the default demo.

Alternative considered: adding combat only as manually spawned debug content. This was rejected because the user explicitly asked to enrich the sandbox scene with relevant use cases.

## Risks / Trade-offs

- [Adding health to every structure increases state management across many existing classes] -> Mitigation: keep the shared health contract in `FactoryStructure` and centralize destruction/unregister behavior.
- [Introducing item typing could ripple through storage, inserters, and HUD text] -> Mitigation: keep the item-kind model minimal and default generic cargo behavior so existing logistics structures do not need bespoke rewrites.
- [Lane-based enemies are less flexible than true pathfinding] -> Mitigation: frame authored lanes as the sandbox baseline and leave adaptive routing as a later extension if combat remains valuable.
- [Turret reload rules could become opaque if adjacent logistics feeds are inconsistent] -> Mitigation: expose turret ammo count in inspection text and show visible "empty" or "reloading" state in the world.
- [Combat clutter could reduce sandbox readability] -> Mitigation: keep the defensive roster small, separate use-case clusters spatially, and only show health bars when useful instead of permanently for every full-health building.
- [Destruction may break existing starter layouts in surprising ways] -> Mitigation: add authored combat lanes in isolated regions and keep current non-combat logistics showcase clusters intact.

## Migration Plan

1. Extend shared data and simulation primitives: item typing, structure health/damage, hostile actor registration, and deterministic combat update hooks.
2. Implement the first defensive structures and ammo flow, then hook them into placement, visuals, HUD inspection, and logistics transfer.
3. Add hostile units plus scripted spawners/lanes and verify that enemies can damage, destroy, and be stopped by defenses.
4. Expand `Factory Sandbox` with authored combat validation lanes and update HUD summaries or smoke probes around the new combat state.
5. If rollback is needed, remove combat structures from the sandbox palette and starter layout first, then disable hostile spawners while keeping harmless data-model additions isolated.

## Open Questions

- Should full-health bars stay hidden until hover/selection/damage, or should defensive buildings always show a slim readiness bar for easier combat monitoring?
- Do we want the first ranged enemy to prioritize turrets specifically, or simply attack the first valid structure in range along its lane?
- Should ammo assemblers produce only one generic magazine type in the first pass, or is there value in reserving space for a second ammo flavor now even if only one is implemented?
