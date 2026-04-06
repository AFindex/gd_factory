## Context

The current project has two large scene controllers, `FactoryDemo` and `MobileFactoryDemo`, that both orchestrate the same factory-domain runtime (`GridManager`, `SimulationController`, `FactoryCameraRig`, `FactoryPlayerController`, blueprint/detail/inventory plumbing, and preview visuals) but do so through separate imperative scripts. The duplication is no longer limited to small helpers: both files now repeat scene bootstrap, player HUD wiring, detail-window synchronization, blueprint panel coordination, preview material helpers, and similar scene-graph assembly patterns, while also carrying their own authored scenario logic.

That shape creates two constraints:

1. We cannot simply collapse the demos into one controller because the static sandbox and the mobile-factory experience still have different state models, HUD surfaces, preview topologies, and authored scenario flows.
2. We also cannot keep duplicating controller glue because new workspaces, item interactions, detail actions, blueprint tweaks, and preview fixes now tend to require touching both demo files in parallel.

The design therefore needs to extract shared controller structure without flattening the distinct gameplay surfaces that each demo represents.

## Goals / Non-Goals

**Goals:**
- Reduce repeated controller code between `FactoryDemo` and `MobileFactoryDemo` for bootstrap, player/detail/inventory routing, blueprint coordination, and shared preview utilities.
- Define clear extension seams so each demo can provide its own authored layout, placement validation, HUD mapping, and control-mode behavior without reimplementing the same base plumbing.
- Keep current user-visible behavior stable while making future demo-facing feature work cheaper to land once.
- Preserve or improve smoke-testability by keeping scenario-specific verification code near the relevant demo while extracting only the reusable infrastructure.

**Non-Goals:**
- Merge the static sandbox and mobile-factory experience into one unified scene or one unified HUD.
- Rewrite the underlying factory simulation, structure hierarchy, or placement rules.
- Fully genericize every preview path or every input branch in the first pass; the change should extract stable common logic first, not force abstraction over every special case.
- Replace existing OpenSpec gameplay capabilities with a new gameplay flow; this change is architectural and maintainability-oriented.

## Decisions

### Decision 1: Use a thin shared runtime base plus focused helpers, not a monolithic inheritance tree

Create a shared demo runtime layer that owns the repeated lifecycle skeleton:

- input action registration
- common scene bootstrap order
- shared node/service creation for simulation, camera, player HUD, and launcher navigation
- helper accessors for selected structure detail, player inventory transfers, and common cleanup-safe guards

This layer should be intentionally small. It can be a base class such as `FactoryDemoRuntimeBase` or an equivalent composition root plus adapter contract, but it must expose narrow abstract/virtual hooks for demo-specific behavior instead of forcing every branch through a huge inherited state machine.

Each concrete demo remains responsible for:

- authored world/mobile scenario content
- control-mode state that exists only in that demo
- site-specific placement validation and selection state
- HUD-specific presentation mapping
- smoke routines that verify demo-specific authored cases

Why this over a giant abstract base class:

- A giant base would quickly accumulate dozens of virtual methods for mobile-only deploy/editor behavior and static-only build/testing behavior, which would hide duplication behind fragile override coupling instead of actually simplifying the design.
- A thin runtime base gives us one place for the stable skeleton while still allowing divergence where the two demos legitimately differ.

Alternative considered: extract only static utility methods. Rejected because most duplication now lives in coordination flow, event wiring, and state-to-HUD translation rather than in tiny pure helpers.

### Decision 2: Introduce shared bootstrap/builders for common scene graph pieces

Both demos currently build the same low-level scene ingredients in slightly different forms:

- environment and directional light
- floor and grid lines
- simulation, combat root, camera rig
- player HUD and launcher overlay
- preview roots and helper meshes

Move these repeated building blocks into shared bootstrap utilities that accept configuration input rather than duplicating imperative creation. For example:

- a `FactoryDemoSceneBootstrap` or `FactoryDemoWorldScaffold` that builds the common world nodes from a config object
- a reusable root-node descriptor model for optional roots such as resource overlays, structure roots, enemy roots, preview roots, and blueprint roots
- shared floor/grid helpers that already exist conceptually at the bottom of both files, but are still duplicated

The mobile demo can still add large-scenario landmarks, editor viewport camera, and separate world/interior preview roots through its adapter config. The static demo can still request the simpler world-grid variant.

Alternative considered: keep scene bootstrap inline and only extract trailing helpers. Rejected because `BuildSceneGraph()` and `ConfigureGameplay()` are among the largest repeated seams, and leaving them duplicated would preserve too much coupling.

### Decision 3: Extract shared interaction bridges for detail, inventory, and blueprint plumbing

The strongest duplication seam is not simulation logic; it is controller glue:

- structure selection to inspection/detail model publishing
- player inventory move/transfer/slot-selection behavior
- recipe/detail action routing
- blueprint workspace activation, panel-state construction, and active-blueprint cleanup rules

These concerns should move into focused shared collaborators rather than staying embedded in the scene controller. Likely pieces:

- a detail presenter/bridge that accepts the selected structure and pushes inspection/detail data into either HUD flavor
- a player interaction bridge that centralizes backpack selection, hotbar-to-placement arming, and transfer routing
- a shared blueprint workflow presenter for the common library-facing state and workspace cleanup rules, while concrete demos keep their own placement-plan calculation and site adapter

This keeps the shared layer responsible for "how scene controllers talk to shared UI/domain systems" while leaving the demo responsible for "what is currently selected and what actions are valid here."

Alternative considered: fully unify both HUD APIs under one interface immediately. Rejected because `FactoryHud` and `MobileFactoryHud` expose genuinely different presentation surfaces; a forced interface would either be too weak to help or too broad to stay readable. A presenter plus small HUD adapters is safer.

### Decision 4: Standardize preview rendering inputs instead of copying preview helper state

Both demos duplicate preview constants and helper routines for:

- facing arrows
- preview material coloring
- power-link dash rendering
- hint/ghost/blueprint preview mesh containers

The preview topology is not identical between demos, so the right shared seam is not "one preview controller for everything." Instead, standardize around reusable preview primitives and renderer helpers:

- common preview color/material helpers
- shared mesh/arrow factory helpers
- a reusable renderer for repeated patterns such as power-link dash segments and blueprint ghost roots
- declarative preview input structs so a demo can describe footprint cells, blocked cells, arrows, or port hints and let helpers materialize the visuals

The mobile demo can keep its extra world-preview footprint, mining markers, and interior boundary rendering on top of those primitives. The static demo can keep the simpler world-grid preview.

Alternative considered: leave preview code in each demo because the topology differs. Rejected because the repeated styling constants and mesh/material creation paths are already drifting even before behavior diverges.

### Decision 5: Keep authored scenarios and smoke orchestration local to each demo

Not all duplication should be abstracted. The authored world layout, large-scenario setup, deploy lifecycle checks, and sandbox regression cases are what make each demo distinct. Those should remain in the concrete demo scripts or in dedicated scenario-library helpers owned by that demo family.

The shared layer may expose tiny utility helpers such as wait/retry guards or common smoke assertion helpers if they are obviously reused, but authored placements, scenario landmarks, mobile-factory presets, and static sandbox districts should not move into a shared "do-everything demo" module.

This is the main guardrail that prevents the refactor from erasing the conceptual boundary between the two experiences.

## Risks / Trade-offs

- [Shared base grows into an override-heavy god object] → Mitigation: keep the base responsible only for lifecycle skeleton and shared bridges; require new demo-specific state machines to stay in concrete controllers or narrowly scoped helpers.
- [HUD extraction introduces awkward adapter layers] → Mitigation: extract presenters around existing HUD methods incrementally, starting with inspection/detail/player inventory and blueprint workspace state where duplication is highest.
- [Preview abstraction hides demo-specific rendering needs] → Mitigation: share primitives and material/mesh builders first, then move only obviously repeated topology loops into helpers after both demos still read clearly.
- [Refactor churn breaks one demo while fixing the other] → Mitigation: preserve current smoke flows and add or retain demo-specific verification entry points during each extraction phase.
- [Common bootstrap utilities become too configurable to understand] → Mitigation: use small config records with explicit fields and optional feature toggles rather than a single loose bag of booleans.

## Migration Plan

1. Introduce the shared runtime/composition types alongside the existing demos without changing behavior.
2. Move the bottom-most pure helpers first: floor/grid/environment creation, preview material helpers, and stable shared constants.
3. Extract bootstrap and bridge layers next, converting `FactoryDemo` and `MobileFactoryDemo` one seam at a time so the project always has compiling concrete controllers.
4. Move blueprint/detail/player inventory coordination into shared presenters or adapters once both demos are using the new runtime skeleton.
5. Keep scenario authoring and smoke entry points local, then trim the remaining duplication in the concrete demo scripts.

Rollback strategy: if the shared runtime starts obscuring behavior or causes regressions, concrete demo scripts can temporarily inline the affected seam again while keeping harmless shared helpers such as scene scaffold builders and preview primitives. Because the gameplay capability remains the same, rollback does not require data migration.

## Open Questions

- Should the shared lifecycle skeleton be implemented as a true abstract base class, or as a composition root object that the concrete demos own? The design supports either, but the implementation should choose the option that keeps Godot scene scripting readable.
- How far should the first pass go on blueprint workflow extraction? It may be enough to share panel-state and cleanup rules first, leaving apply-plan generation separate until the common shape is clearer.
- Should player inventory/detail bridging be one helper or two smaller helpers? This depends on whether the current selection and transfer code reveals a clean shared state object during implementation.
