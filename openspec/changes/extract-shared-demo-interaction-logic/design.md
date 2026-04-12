## Context

`FactoryDemo` and `MobileFactoryDemo` already share a first-pass runtime composition layer (`FactoryDemoSceneScaffold`, preview helpers, inventory/detail bridge helpers), but the overlapping interaction behavior is still spread across both scene controllers. The static demo now represents the baseline factory interaction subset that also exists inside the mobile-factory experience: shared HUD-facing state, structure selection and detail publishing, player inventory placement arming, build/delete/blueprint transitions, and a large amount of preview/input gating.

That leaves the project in an awkward middle state:

- the shared runtime layer exists, but the interaction layer is still controller-owned;
- `FactoryDemo` and the world-facing branch of `MobileFactoryDemo` continue to drift whenever a baseline interaction rule changes;
- the mobile demo has legitimate extra logic for deploy anchors, lifecycle, editor sessions, and interior-only interaction, so we cannot solve the problem by flattening everything into one monolithic controller.

The design needs to extract the overlapping baseline interaction flow into one reusable layer while preserving mobile-factory-specific gameplay branches as local code.

## Goals / Non-Goals

**Goals:**
- Extract one reusable baseline factory interaction layer from the current `FactoryDemo` interaction flow and let both demos consume it.
- Centralize overlapping state transitions for selection, detail publication, placement arming, interaction modes, blueprint mode entry/exit, preview messaging, and shared input gating.
- Provide a shared HUD projection shape for overlapping interaction state so both demos stop duplicating controller-to-HUD translation.
- Reduce interaction-related code inside `FactoryDemo.cs` and `MobileFactoryDemo.cs` so each controller keeps mostly scenario-specific orchestration.
- Preserve current gameplay behavior in both demos while making later interaction changes cheaper to implement once.

**Non-Goals:**
- Do not merge the static sandbox and mobile-factory demos into one scene, one HUD class, or one gameplay mode.
- Do not force deploy-preview logic, world-anchor validation, editor-session routing, or interior-only editing into the first-pass shared interaction shell.
- Do not rewrite the simulation, authored maps, structure hierarchy, or save/runtime systems.
- Do not require every preview primitive in the mobile demo to use the same abstraction if it has no meaningful static-demo equivalent.

## Decisions

### Decision 1: Extract a baseline interaction host centered on the `FactoryDemo` subset

The shared layer should model the overlapping interaction loop that already exists in `FactoryDemo` and is also present in the world-facing portion of `MobileFactoryDemo`. Concretely, that shared host owns:

- active baseline interaction mode (`Interact`, `Build`, `Delete`, blueprint-related transitions);
- selected or hovered structure and the resulting detail/inspection payload;
- selected placement source and player inventory placement arming;
- common preview status text and preview-visibility gates;
- reusable helper hooks for pointer-over-UI suppression and inventory-driven interaction blocking.

The host is intentionally not a generic “everything the demos ever do” state machine. Mobile-only deploy anchors, lifecycle modes, editor session state, and interior interaction remain outside it.

Why this direction:

- It matches the user-visible reality of the codebase: `factory_demo.tscn` is already the common subset.
- It gives the team one authoritative place to evolve the baseline factory interaction rules.
- It avoids building an abstraction around the most specialized mobile-only branches before the shared shape is stable.

Alternative considered: design a completely generic multi-surface interaction engine that handles static, world, deploy, and interior flows from day one. Rejected because it would overfit the mobile demo’s special cases and likely recreate the same complexity inside a larger abstraction.

### Decision 2: Split the shared layer into state host, adapters, and demo-owned rule hooks

The extraction should not become one giant helper class. The shared interaction layer should be decomposed into three roles:

- a shared interaction host/state container for overlapping baseline state and transitions;
- small adapters for HUD projection, detail publishing, inventory endpoint resolution, and preview/input guard checks;
- demo-owned rule hooks for validation and side effects that differ per scene.

In practice, the shared layer should expose narrow methods such as:

- enter or exit baseline interaction modes;
- update hovered cell/structure and selected structure;
- arm or clear placement source from player inventory;
- start, cancel, or confirm baseline blueprint workflow;
- project overlapping interaction state into a shared HUD view model;
- ask the concrete demo whether a pointer/UI state should block world-facing interactions.

The concrete demos remain responsible for answering questions like:

- “Can this world cell be built on here?”
- “What extra preview overlays are needed for this demo?”
- “What deploy/editor/lifecycle state changes must happen around the shared interaction transitions?”

Alternative considered: extend `FactoryDemoInteractionBridge` into one static utility bag. Rejected because the current problem is no longer only about pure helpers; it is about state ownership and transition flow.

### Decision 3: Share one baseline HUD projection contract before attempting HUD-class unification

Both demos still need different HUD surfaces, but the overlapping interaction state should be projected through one shared model or adapter contract. That contract should cover baseline state such as:

- current baseline interaction mode;
- selected structure summary/detail payload;
- active placement source or selected buildable;
- blueprint workflow state;
- preview status message and baseline availability flags.

`FactoryHud` and `MobileFactoryHud` can each translate that shared projection into their own presentation layers, then append demo-specific information:

- the static demo adds sandbox-specific workspace/testing context;
- the mobile demo adds deploy-preview, control-mode, editor-session, and interior-specific context.

This lets the code stop duplicating “take overlapping scene state and compute HUD fields” logic without forcing a premature shared HUD widget hierarchy.

Alternative considered: unify `FactoryHud` and `MobileFactoryHud` behind one big interface immediately. Rejected because their surface areas are still meaningfully different, and a forced common interface would either hide necessary differences or become too broad to be useful.

### Decision 4: Reuse the shared host for overlapping mobile world interaction, not the entire interior editor

The first implementation pass should wire the shared interaction host into:

- `FactoryDemo` as the primary baseline consumer;
- the overlapping world-facing factory interaction branch inside `MobileFactoryDemo`.

The interior editor can still reuse lower-level primitives such as inventory/detail helpers, preview utilities, and selected-structure publication where appropriate, but it should not be forced into the same host if the fit is poor. The interior editor has extra concerns that the static demo does not share:

- editor session open/close lifecycle;
- viewport focus routing;
- interior-only blueprint site rules;
- attachment and hull-boundary previews.

This decision keeps the first pass tractable while still delivering the user’s requested benefit: the baseline interaction logic that `factory_demo.tscn` represents becomes the reusable foundation shared by both demos.

Alternative considered: require both the mobile world layer and the interior editor to adopt the shared host in one pass. Rejected because it increases migration risk and would blur the boundary between “shared baseline interaction” and “mobile-only editing product logic.”

### Decision 5: Keep preview and input sharing focused on overlapping gates, not every rendering branch

The shared layer should centralize only the overlapping preview and input gate rules:

- pointer-over-UI blocking;
- active inventory drag or interaction blocking;
- baseline preview enable/disable rules tied to mode or selection state;
- shared preview messaging and common visibility transitions.

Preview rendering that is truly shared can stay in existing helper/support types. But demo-specific preview branches remain local:

- static-demo-only sandbox overlays stay with `FactoryDemo`;
- deploy anchors, mining links, editor viewport previews, and interior-only boundary previews stay with `MobileFactoryDemo`.

This prevents the shared interaction shell from becoming a second rendering subsystem while still eliminating the duplicated control flow around when previews should be live.

Alternative considered: centralize all preview rendering inside the new interaction layer. Rejected because the shared problem is mostly state gating and coordination, not identical geometry.

## Risks / Trade-offs

- [The shared host grows into another god object] → Mitigation: keep it limited to overlapping baseline interaction state and transitions; move scene-specific rules back behind adapters or demo-owned hooks.
- [Mobile demo integration stalls because world and interior logic are intertwined] → Mitigation: make the first migration target only the world-facing overlapping subset and reuse lower-level helpers elsewhere opportunistically.
- [HUD projection extraction misses important mobile-only context] → Mitigation: define the shared projection as additive; each HUD can extend it locally without weakening the shared baseline model.
- [Refactor introduces subtle input regressions] → Mitigation: migrate input guards in small seams, keep existing smoke coverage, and preserve demo-specific blocking rules where the shared shell has no equivalent.
- [Developers misread the shared layer as the home for all future demo logic] → Mitigation: document and enforce the boundary that authored scenarios, deploy/editor product rules, and mobile-only flows stay in concrete demo code.

## Migration Plan

1. Identify the baseline interaction state in `FactoryDemo` that should become the authoritative shared host state: selection, modes, placement source, preview messaging, blueprint state, and shared input guards.
2. Introduce the shared interaction host and HUD projection contract alongside the existing controllers, initially wiring `FactoryDemo` through it while preserving current behavior.
3. Replace remaining controller-owned baseline helpers in `FactoryDemo` with host/adapters, leaving static-demo-specific placement validation, testing workspace behavior, and authored flow local.
4. Integrate the same host into the overlapping world-facing interaction branch of `MobileFactoryDemo`, adapting mobile-only control mode, deploy, and editor-session logic around the shared transitions.
5. Opportunistically move any still-duplicated shared guards or preview state gates into the shared layer, then trim obsolete interaction code from both controllers and update smoke coverage if needed.

Rollback strategy:

- If the shared host becomes too rigid, keep the shared HUD projection and helper adapters but temporarily re-inline a problematic transition in the affected controller.
- If mobile integration proves too disruptive, land the static-demo extraction first and defer the mobile-side adoption behind a follow-up seam while keeping the new contract intact.

## Open Questions

- Should the shared interaction host be an owned object used by both controllers, or a partial base-class layer that already has direct access to common node references? The design prefers composition, but the implementation should choose the option that keeps Godot scene code readable.
- How much of blueprint workflow should move in the first pass? It may be enough to centralize mode/state transitions and HUD projection first, while leaving some plan-generation details in demo-owned adapters.
- Is there a clean shared view model for “selected placeable source” that covers both hotbar-driven placement and mobile-demo world build selection without leaking demo-specific state names into the shared contract?
