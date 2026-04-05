## Context

The current HUD implementation is split between two scene-specific control trees:

- `FactoryHud` builds one tall static sidebar for the sandbox demo and keeps the blueprint workflow as a separate floating panel.
- `MobileFactoryHud` builds a world info panel plus a large editor panel, with blueprint and structure detail tools layered on top as separate overlays.

That arrangement worked while the demos only needed a few HUD sections, but recent features have increased the amount of UI that must remain reachable at runtime. Blueprints, structure detail windows, command-mode feedback, scenario diagnostics, and mobile-factory state now compete for space. The user's request is not just cosmetic: they want the runtime tools grouped into formal categories, surfaced by a top menu bar, and reorganized so static sandbox testing panels and mobile-factory detail views are available without dumping every control onto the screen at once.

The change crosses multiple modules (`FactoryHud`, `MobileFactoryHud`, `FactoryDemo`, `MobileFactoryDemo`, and existing panel controls), so we should introduce one shared workspace-navigation pattern rather than hand-building a different menu system inside each HUD.

## Goals / Non-Goals

**Goals:**
- Add one reusable workspace-menu pattern that can be used by the static sandbox demo, the focused mobile demo, and the large mobile scenario.
- Reorganize existing HUD content into named workspace panels instead of one always-expanded stack.
- Keep scene controllers authoritative for tool state, build mode, blueprint state, and mobile-factory lifecycle state while the HUD only handles presentation and UI intents.
- Make blueprint actions and new mobile-factory detail content reachable from the workspace menu without breaking split-view editing.
- Preserve the current interaction model: world/editor input routing, draggable structure detail windows, and existing blueprint workflow commands must keep working after the UI reshuffle.
- Leave room for scene-specific panel composition, especially the large scenario's sandbox/testing panels.

**Non-Goals:**
- Rewriting the simulation, build-placement, or blueprint domain logic.
- Replacing the existing structure detail window with a full inspector framework.
- Introducing persistence for HUD layout, draggable workspace docking, or a full window manager.
- Producing final-polish art direction for every panel state in this change.

## Decisions

### 1. Introduce a shared workspace shell instead of duplicating menu logic in each HUD

Add a reusable HUD-side workspace shell concept that includes:

- a top workspace menu row,
- an active workspace id,
- a content host for the selected workspace panel,
- scene-provided descriptors for label, availability, and panel builder.

In code terms, this can be implemented as a shared control/helper pair under `scripts/factory/` such as `FactoryWorkspaceShell` plus simple workspace descriptor/state DTOs. `FactoryHud` and `MobileFactoryHud` will each compose the shell rather than rebuilding the menu row manually.

This is preferred over hard-coding separate button rows inside each HUD because the user wants the same interaction model across sandbox, focused mobile demo, and scenario UI. Shared chrome keeps menu behavior, switching rules, and layout math consistent.

### 2. Keep scene controllers authoritative and make workspaces presentation-driven

`FactoryDemo` and `MobileFactoryDemo` should continue owning:

- current interaction mode,
- selected build prototype,
- blueprint workflow state,
- scenario telemetry,
- mobile-factory lifecycle and attachment summaries.

The HUD layer should expose workspace-selection events and render scene-provided workspace state, but it should not move gameplay logic into UI controls. Workspace switching updates which panel is visible; it does not directly mutate build state unless the selected panel invokes an existing action such as "capture blueprint" or "toggle deploy mode."

This is preferred over storing gameplay state inside the workspace shell because both demos already centralize interaction logic in scene controllers, and moving that authority into the HUD would make smoke testing and future refactors harder.

### 3. Rehost existing panel content into workspace bodies before inventing new bespoke controls

Most of the requested UX can be met by reorganizing existing content:

- The static sandbox's build palette, telemetry, combat readout, notes, and blueprint panel become individual workspace bodies.
- The mobile demo's command controls, world summary, editor tools, blueprint panel, and new factory detail content become workspace bodies.
- The large scenario reuses the same shell but adds scenario-specific workspaces such as build testing and observation/diagnostics.

`FactoryBlueprintPanel` should remain the blueprint workflow control, but it should be embeddable as a workspace body instead of only behaving like a floating sibling window. `FactoryStructureDetailWindow` should remain a separate draggable overlay because it represents target-specific inspection, not a top-level workspace.

This is preferred over rewriting every panel from scratch because the user asked for a stronger information architecture, not a replacement of the underlying workflows.

### 4. Use scene-specific workspace sets with one shared navigation model

Each scene should register only the workspaces it actually needs:

- Static sandbox: overview/build, blueprints, telemetry, combat, testing.
- Focused mobile demo: command, editor/build, blueprints, mobile factory details.
- Large mobile scenario: overview, build test, diagnostics/observation, blueprints if enabled for that surface.

The shared shell handles selection and visibility, but workspace registration stays scene-specific. This avoids forcing empty tabs onto scenes that do not need them, while still giving the user one consistent mental model: choose a category from the top menu, then work inside that panel.

This is preferred over a universal fixed tab list because the static sandbox and large scenario do not expose exactly the same tool groups as the focused mobile demo.

### 5. In the mobile split-view editor, keep the top menu global and route workspaces to the correct host area

The mobile demo already has two visible surfaces: a narrow world strip and a large editor surface. To preserve that split-view while still adding the requested top menu bar:

- place the workspace menu in shared HUD chrome near the top edge,
- keep a compact always-visible summary strip for critical status (mode, lifecycle, anchor/preview feedback),
- route world-oriented workspaces to the world/info host,
- route editor-oriented workspaces such as build tools and blueprints to the editor sidebar host.

The selected workspace determines which host expands, but selecting a workspace must not collapse the editor viewport itself. The mobile-factory detail workspace can render in the info host while the interior editor remains active on the right.

This is preferred over forcing every workspace into the editor sidebar because the user explicitly wants additional mobile-factory detail information, and that information belongs to the whole mobile factory rather than only the interior-building surface.

### 6. Add explicit workspace state hooks for smoke coverage

Both `FactoryDemo` and `MobileFactoryDemo` already include smoke-oriented code paths. Extend that pattern with testable workspace-state methods so smoke coverage can assert:

- expected workspace labels exist,
- selecting a workspace makes the right panel visible,
- critical tools such as build controls and blueprint actions remain reachable after switching,
- scenario-specific panels such as the sandbox build test panel are exposed in the large scenario.

This is preferred over only visually verifying the menu because the change is primarily a UI reorganization, which is exactly the sort of work that benefits from lightweight regression assertions around visibility and availability.

## Risks / Trade-offs

- [Workspace content becomes fragmented across too many tabs] -> Keep a compact default summary visible and choose a small, curated tab set per scene instead of exposing every internal subsection as its own workspace.
- [Embedding the existing blueprint panel creates layout regressions] -> Preserve `FactoryBlueprintPanel` as a reusable control, but add a docked mode or host wrapper rather than depending on its current floating-window assumptions everywhere.
- [Mobile split-view menu placement fights for vertical space] -> Keep the menu row shallow, preserve critical status in a compact strip, and move verbose text into the selected workspace body.
- [Scene-specific workspace definitions drift apart] -> Centralize the workspace shell behavior and only vary descriptors/content builders per scene.
- [UI-only refactors are hard to trust without interaction tests] -> Extend smoke checks to verify workspace switching and reachability for build, blueprint, and scenario test tools.

## Migration Plan

1. Add shared workspace-shell types and basic menu rendering without removing existing HUD content yet.
2. Refactor `FactoryHud` to move its current sections into workspace bodies and replace the one tall stack with the shared workspace shell.
3. Refactor `MobileFactoryHud` to add the top menu, compact summary strip, contextual workspace hosts, and a docked mobile-factory detail workspace.
4. Update `FactoryDemo` and `MobileFactoryDemo` to publish workspace state, respond to workspace-selection events, and expose scenario-specific panels.
5. Update smoke coverage for the static sandbox, focused mobile demo, and large scenario to verify workspace availability and critical-tool reachability.

Rollback is straightforward: the workspace shell can be disabled per HUD and the previous always-expanded panel construction can be restored while leaving underlying gameplay systems untouched.

## Open Questions

- Should the focused mobile demo default to a command/overview workspace or reopen the last editor-oriented workspace whenever the player toggles the split-view editor?
- Do we want the blueprint panel to support both docked and floating presentation modes, or should this change standardize it as docked-only inside the workspace shell?
- Should the large mobile scenario expose blueprint tools by default, or keep its first pass focused on build test and observation panels only?
