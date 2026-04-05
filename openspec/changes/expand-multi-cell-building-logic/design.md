## Context

The project already has a useful split between shared structure logic (`FactoryStructure`, `FactoryStructureFactory`, `IFactorySite`) and site-specific placement surfaces (`GridManager` for the world, mobile-factory interior/runtime site types for moving factories). The world grid also already reserves multiple cells for mobile-factory deployment footprints, and structures expose `GetOccupiedCells()`, but most authored building logic, previews, build validation, and interaction flows still assume that a placeable structure is effectively a `1x1` anchor.

This change needs to expand that shared contract without rewriting unrelated business systems such as recipes, item transport, power balancing, or blueprint workflows. The safest path is to keep `1x1` structures working through defaults while introducing a first-class footprint description that both the static sandbox and mobile-factory editors can consume.

Stakeholders are the static sandbox build loop, mobile-factory interior editing, tower-defense combat content, and authored demo/smoke scenes that must continue to run after the change.

## Goals / Non-Goals

**Goals:**
- Introduce one shared way for structures to describe occupied cells, anchor semantics, and rotated interaction points.
- Make placement, previews, reservation, hover/selection, and deletion operate on full multi-cell footprints in both world and mobile-factory contexts.
- Add concrete example content that proves the foundation works: a large turret with independent projectiles and a non-combat multi-cell utility structure.
- Preserve existing `1x1` structure behavior unless a prototype explicitly opts into a larger footprint.
- Keep the implementation narrow enough that unaffected production, power, and logistics structures do not need feature rewrites.

**Non-Goals:**
- Reworking every existing structure into Factorio-authentic dimensions in the same change.
- Redesigning belt lanes, inserter animation, fluid systems, or broader RTS/pathfinding behavior.
- Introducing save migration, generic vehicle AI, or a new blueprint schema beyond what is required for footprint-aware placement.
- Replacing current combat pacing with a fully physical ballistics sandbox; only the new heavy turret needs explicit projectile entities.

## Decisions

### Decision: Add explicit footprint metadata instead of hardcoding shape logic per structure

We will introduce a shared footprint description for buildable prototypes and/or structures that defines:
- local occupied-cell offsets relative to an anchor cell
- optional rotated interaction points such as input/output or firing origin cells
- preview bounds and center offsets for visuals and overlays

`1x1` structures will use a default single-cell footprint so existing gameplay keeps working. Multi-cell structures will supply additional offsets and any special connection markers through the same contract.

Why this approach:
- It centralizes rotation and occupancy math instead of scattering custom `GetOccupiedCells()` implementations across many structures.
- It gives previews, placement validation, hover resolution, and blueprint capture a common source of truth.
- It lets later content add new large structures with data/metadata changes instead of more site-specific logic branches.

Alternative considered:
- Override `GetOccupiedCells()` and related accessors independently per structure class. This was rejected because it duplicates rotation rules, makes previews harder to standardize, and increases regression risk when the same structure must behave consistently in the world grid and mobile-factory interiors.

### Decision: Keep site reservation keyed per occupied cell, but resolve every occupied cell back to one owner structure

The existing reservation maps are already cell-based, which is a good fit for preventing overlap. We will extend placement and interaction queries so every occupied cell of a multi-cell structure points at the same owner/reservation record. Removing, selecting, or inspecting any occupied cell will resolve the owning structure and clear or focus the full footprint.

Why this approach:
- It minimizes churn in `GridManager` and other site implementations because the underlying reservation model already stores cell ownership.
- It keeps collision/overlap checks simple and deterministic.
- It ensures combat and click interaction do not depend on the player hitting the anchor cell exactly.

Alternative considered:
- Reserve only the anchor and derive shape occupancy lazily during checks. This was rejected because it complicates overlap validation, breaks combat/selection on non-anchor cells, and makes site queries slower and more error-prone.

### Decision: Share the same footprint validation and preview pipeline between world building and mobile-factory interiors

Placement validation will move toward a shared “resolved footprint” step that accepts a prototype, anchor cell, facing, and site. That step will return occupied cells, connection markers, and blocking reasons. Both the static sandbox and mobile-factory interior editor will use that result to color previews, confirm placement, and handle deletion.

Why this approach:
- The user specifically wants mobile factories to support the same multi-cell logic.
- It avoids a world-only implementation that later has to be duplicated for interior editing.
- It supports the “do not splash other business logic” constraint by isolating most new logic into shared placement helpers instead of spreading behavior changes everywhere.

Alternative considered:
- Ship world-grid support first and add mobile factories later. This was rejected because the feature would immediately diverge across two build surfaces and make the new foundation less trustworthy.

### Decision: Add one combat example and one utility example for the first content pass

The initial example set will be:
- a `2x2` heavy gun turret for the world sandbox that consumes ammo and fires independent projectile entities
- a `2x2` large storage depot that can be placed in the static sandbox and in compatible mobile-factory interiors

Why this approach:
- The heavy turret directly exercises multi-cell combat, previews, occupancy, and projectile simulation.
- The large storage depot proves that non-combat utility structures can reuse the same footprint model without adding new business subsystems.
- Together they provide visible Factorio-like examples while keeping the scope implementable in one change.

Alternative considered:
- Converting splitters/mergers into multi-cell shapes immediately. This was rejected for now because those structures also touch transport topology and would create unnecessary scope overlap with logistics behavior.

### Decision: Model heavy-turret shots as independent simulation-owned projectile actors

The heavy turret will spawn projectile entities with explicit origin, target selection, travel time, and hit resolution instead of using pure hitscan damage. Projectiles will be lightweight and deterministic so they can run inside the existing simulation/update loops without introducing a full physics dependency.

Why this approach:
- The user explicitly asked for independent shells/projectiles.
- A separate projectile actor gives clearer combat readability and a reusable foundation for future artillery or enemy shots.
- A lightweight simulation-owned projectile avoids coupling the feature to Godot physics bodies or nondeterministic collisions.

Alternative considered:
- Fake the projectile with an instant hit plus a visual effect. This was rejected because it would not satisfy the requested gameplay behavior and would be harder to extend later.

## Risks / Trade-offs

- [Risk] Existing placement code still contains hidden `1x1` assumptions outside the main reservation path. → Mitigation: audit the build, preview, removal, combat targeting, and authored layout helpers before implementation; default all untouched structures to the single-cell footprint contract.
- [Risk] Mobile-factory interiors have tighter bounds, so a large structure may fit in the world but not in a small factory. → Mitigation: validate footprints against the active site dimensions and only expose compatible utility examples in interior build menus or presets.
- [Risk] Independent projectiles add another simulation entity type that could create update noise. → Mitigation: keep projectile logic minimal, pool or cap instances if needed, and limit the first rollout to the heavy turret example.
- [Risk] Example content could accidentally change the balance or regression behavior of unrelated demos. → Mitigation: add dedicated authored example lanes/layouts and preserve existing legacy lines unless a footprint-aware replacement is required for test coverage.
- [Risk] Blueprint capture/apply and detail inspection may behave inconsistently if they only store anchor cells. → Mitigation: reuse the shared occupied-cell metadata when capturing or resolving structures and add targeted smoke/test coverage for multi-cell examples.

## Migration Plan

1. Introduce the shared footprint contract with default single-cell behavior.
2. Update site validation, previews, selection, and removal to consume resolved footprints.
3. Retrofit existing world and mobile-factory build flows so they call the shared helper without changing unchanged prototype behavior.
4. Add heavy turret projectiles and the large storage depot example content.
5. Refresh authored demo layouts and smoke tests to cover at least one world and one mobile-factory multi-cell case.

Rollback is straightforward because the feature is additive: disable the new prototypes and revert the shared footprint helpers to the default single-cell path if regressions appear.

## Open Questions

- None for proposal readiness. The change will proceed with a `2x2` heavy gun turret and a `2x2` large storage depot as the initial example set.
