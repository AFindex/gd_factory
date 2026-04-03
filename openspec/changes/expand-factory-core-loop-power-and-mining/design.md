## Context

The static sandbox currently demonstrates deterministic logistics, buffered transfer, structure detail panels, combat pressure, and blueprint workflows, but its economy remains synthetic. `ProducerStructure` and `AmmoAssemblerStructure` act as timed item spawners with lightweight recipe switching, `SimulationController` has no concept of resource fields or power state, and `GridManager` treats the entire map as generic build floor bounded by `FactoryConstants.GridMin/GridMax` at `-16..16`. That is good enough for logistics prototyping, but it is not a stable foundation for a deeper factory game because nothing in the world constrains where items come from, whether machines have supply, or how far a layout can scale.

This change is cross-cutting across world authoring, simulation state, placement validation, structure definitions, HUD/detail surfaces, and smoke coverage. It also needs to preserve the project's existing strengths: deterministic fixed-step simulation, reusable structure detail windows, and a readable authored starter layout that can be regression-tested headlessly.

## Goals / Non-Goals

**Goals:**
- Establish a real world-level input -> production -> output loop in the static sandbox using mineable deposits, powered machines, and recipe-driven manufacturing.
- Reuse one shared simulation model for mining, smelting, and assembly so future production structures do not each invent their own buffering and recipe logic.
- Introduce a deterministic power network that can express powered, underpowered, and disconnected machine states without abandoning the existing fixed-step simulation.
- Expand the sandbox to an approximately three-times-wider footprint so the authored starter layout can stage separated mining, power, manufacturing, and regression districts on one map.
- Keep existing build, inspection, combat, and blueprint workflows functional while the new machines slot into them.

**Non-Goals:**
- Reproducing Factorio feature-for-feature, including fluids, trains, split belt lanes, circuit logic, pollution, or tech progression.
- Adding procedural terrain generation or a save-system-grade resource map format in this change.
- Making resource deposits exhaustible in the first implementation; long-running authored demo stability matters more than depletion economics right now.
- Retrofitting the mobile factory interior with full power-grid or mining rules in the same change.

## Decisions

### 1. Replace ad-hoc machine spawning with one shared recipe-process model

Introduce a shared manufacturing model that extends today's recipe catalog from "output label plus cycle time" into proper process definitions: machine class, required inputs, produced outputs, cycle time, and power demand. Mining drills, smelters, assemblers, and upgraded versions of existing production structures should all consume that same recipe/process contract, even if some structures expose only a fixed recipe in the first pass.

This is preferred over continuing to hard-code behavior per structure because the current `ProducerStructure` and `AmmoAssemblerStructure` already show the duplication cost. A shared process model keeps details windows, smoke assertions, buffers, and future machine additions aligned.

Alternative considered: keep separate bespoke rules for miners, assemblers, and generators. Rejected because it would multiply edge-case handling for starvation, blockage, power loss, and recipe inspection almost immediately.

### 2. Model deposits as authored world overlays instead of ordinary structures

Add a resource-layer model keyed by grid cell or deposit patch, owned by the world site rather than by `FactoryStructure`. Deposits should expose resource type, occupied cells, and extraction compatibility while rendering as authored environment overlays on the enlarged map. Placement validation checks that only compatible extractor structures may claim those cells.

This is preferred over implementing ore patches as regular structures because deposits are terrain constraints, not logistics actors. Treating them as overlays keeps them out of combat, out of blueprint capture unless explicitly supported later, and out of structure-topology code paths that assume a placed machine.

Alternative considered: represent deposits as hidden structures in `GridManager`. Rejected because it overloads reservations and complicates any cell that must distinguish terrain metadata from an actual placed building.

### 3. Build a connected power graph with per-network supply satisfaction

Introduce power producers, relays, and consumers through a lightweight power-node contract. The simulation rebuilds connected power components whenever powered structure topology changes, then computes network supply, demand, and a deterministic satisfaction ratio each simulation step. Consumers use that ratio to scale or stall their work progress, and their detail lines/HUD summaries expose whether they are fully powered, underpowered, or disconnected.

This is preferred over binary on/off allocation or explicit wire-placement UI because the project already relies on simple deterministic adjacency rules. A shared network satisfaction model is closer to the intended "Factorio-like" feel than arbitrary priority ordering, while still being practical in the current fixed-step simulation.

Alternative considered: make machines either fully powered or fully stalled based on a greedy sorted allocation pass. Rejected because it produces harsher behavior, more tuning burden, and harder-to-explain results in a prototype sandbox.

### 4. Introduce a small but real early factory chain instead of a broad content dump

The first authored chain should focus on a compact set of meaningful structures and item kinds:
- fuel resource and a fuel-fed generator for bootstrap power,
- ore resource and a mining drill,
- a smelter/refinery step that turns raw resource into an intermediate,
- a true assembler that consumes one or more intermediates to produce a higher-tier part or ammo output.

Existing `ProducerStructure` should be removed from the default sandbox starter layout and treated as legacy/debug content if it remains in code during migration. Existing recipe-capable detail UI should be reused so the new assembler and smelter can expose recipe state immediately.

This is preferred over adding many resources and machines at once because the goal is to prove the core loop, not to front-load content explosion. A narrower chain gives clearer authored use cases and smoke coverage.

Alternative considered: jump straight to many resource types, burner miners, and several assembly tiers. Rejected because the simulation and authored-map changes are already broad enough without layering on a large balance matrix.

### 5. Expand the world bounds and author named sandbox districts

Increase the static sandbox grid from the current `-16..16` world to an approximately triple-width footprint and reorganize the starter layout into named districts: extraction, power, refining, assembly/output, and a preserved regression lane or combat strip. Keep at least one intentionally open probe area for smoke tests and fast manual iteration.

This is preferred over keeping the current dense map and squeezing new systems into it because mining patches, power routing, and longer conveyor corridors need real spatial separation to be readable. It also lets us preserve some of the existing regression layouts instead of deleting all old coverage.

Alternative considered: generate a random larger map at startup. Rejected because authored determinism is more valuable than variety for this project's current test-heavy workflow.

### 6. Reuse existing detail windows and smoke harness instead of building parallel tooling

Powered structures, miners, and manufacturing machines should report state through the existing detail-window pattern and HUD summary lines. The static sandbox smoke flow should be extended rather than replaced: it can still place temporary structures, but now it also verifies resource extraction, generator fuel consumption, power satisfaction, recipe progression, and final delivery on the authored map.

This is preferred over adding bespoke overlays or a separate verification scene because the current demo already succeeds by keeping observability and regression in one place.

Alternative considered: add a dedicated power/mining test scene. Rejected for now because it would fragment the user-facing sandbox and duplicate authored content.

## Risks / Trade-offs

- [Power simulation touches many structures at once] -> Introduce opt-in power interfaces so unpowered legacy structures keep their current behavior until explicitly migrated.
- [Larger bounds can hurt camera readability and manual navigation] -> Expand camera constraints, preserve obvious district landmarks, and keep the compact HUD plus detail-window workflow instead of introducing heavier overlays.
- [Resource overlays may conflict with blueprint/build assumptions] -> Keep deposits world-authored and exclude them from blueprint capture in this phase, while validating placement compatibility explicitly.
- [Keeping legacy producer code during migration can confuse behavior] -> Remove it from the default starter layout and clearly separate "real chain" structures from any compatibility-only debug prototypes.
- [Underpowered ratio-based simulation can be harder to tune than binary states] -> Keep the first pass simple, expose satisfaction values in debug/detail text, and seed smoke tests around both fully powered and intentionally strained networks.

## Migration Plan

1. Extend the shared item and recipe domain so structures can declare real inputs, outputs, fuel, and power demand while preserving current deterministic item transport.
2. Add world resource overlays, extraction validation, and the first mining/generation/manufacturing structure kinds behind the existing placement and detail-window workflows.
3. Add power-graph rebuild and per-network satisfaction calculations to the simulation, then migrate the new powered structures to respect that state.
4. Expand the static sandbox bounds, author the new multi-district starter layout, and remove placeholder producer-driven starter content from the default experience.
5. Update HUD/detail presentation and smoke coverage to verify the powered closed loop end-to-end.

Rollback remains feasible because the change is demo-scene-local: the old bounded starter layout and legacy machine set can temporarily be restored while keeping the new domain classes dormant if the integrated loop proves unstable.

## Open Questions

- Should underpowered networks reduce machine progress continuously, or should some structures snap to idle below a minimum satisfaction threshold for clearer player feedback?
- How much of the new powered-production rules should be exposed to blueprint validation immediately, versus treating power and deposits as external world assumptions in the first implementation?
