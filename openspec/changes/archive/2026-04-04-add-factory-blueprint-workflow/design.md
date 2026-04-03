## Context

The project already has two editable factory surfaces with overlapping but separate control flows:

- `FactoryDemo` + `FactoryHud` manage the static world sandbox on top of `GridManager`.
- `MobileFactoryDemo` + `MobileFactoryHud` manage the split-view interior editor on top of `MobileFactoryInstance` and `MobileFactorySite`.

Both surfaces already support structure placement, removal, orientation, detail windows, and smoke-tested authored layouts, but neither has a reusable layout abstraction. The closest existing authored layout model is `MobileFactoryInteriorPreset`, which is code-authored, profile-specific, and applied only at factory construction time. The static sandbox starter layout is even less reusable because it is assembled procedurally inside `FactoryDemo`.

This change introduces a new cross-cutting concept: reusable blueprints captured from live scenes and applied back through gameplay tooling. That affects scene UI, site validation, structure serialization, mobile attachment rules, and smoke-test coverage. Factorio's blueprint UX is a good reference point here: capture is selection-based, saved blueprints live in a library distinct from the active placement cursor, and blueprints preserve structural intent/configuration rather than transient runtime contents.

## Goals / Non-Goals

**Goals:**
- Provide one shared blueprint model that works for the world sandbox and mobile factory interiors.
- Let players capture a layout from a live scene, name it, save it into a reusable library entry, preview it, and apply it back into a compatible target site.
- Preserve the structure data that defines a layout, including kind, local cell offset, facing, recipe selection, and boundary-attachment placement where applicable.
- Exclude transient runtime state such as buffered items, delivery counters, combat damage, or in-flight simulation objects from blueprint payloads.
- Keep blueprint application testable by generating an explicit validation/apply plan before mutating the scene.
- Add sandbox-facing UI so the workflow can be exercised end to end without editor-only tooling.

**Non-Goals:**
- Full ghost-building, partial placement, or deferred construction queues like a finished factory game would use.
- Cross-device/cloud synchronization or a polished long-term persistence UX in this change.
- Arbitrary text editing of blueprint payloads by the player.
- A generic undo/redo framework for all build actions.
- Blueprinting world enemy state, item buffers, ammo counts, or other volatile simulation data.

## Decisions

### 1. Represent blueprints as site-agnostic serialized records with explicit compatibility metadata

Add a shared `FactoryBlueprintRecord` model built from plain serializable DTOs rather than scene nodes. Each record should include:

- blueprint id and display name,
- source site type (`world-grid` or `mobile-interior`),
- normalized bounds and anchor offset,
- a list of structure entries containing `BuildPrototypeKind`, local cell offset, facing, and stable config payload,
- compatibility metadata such as interior bounds requirements and required attachment mounts for mobile blueprints.

Stable config should include settings that define layout behavior, such as selected production recipe or boundary attachment kind/orientation. Transient state such as buffered items, current HP, active deliveries, enemy aggro, or rendered preview state must not be serialized.

This is preferred over reusing live `FactoryStructure` instances or `MobileFactoryInteriorPreset` directly because blueprints need to be captured from either site at runtime, transferred between scenes, and revalidated against a new target before instantiation.

### 2. Use selection-based capture with normalized local coordinates

Capture should operate on a selected rectangle plus an optional site-wide shortcut:

- In the static sandbox, the player enters blueprint capture mode and drags a selection rectangle over the world grid.
- In the mobile interior editor, the player can use the same selection workflow inside the editor viewport, with an additional "capture full interior" shortcut for authored preset-sized layouts.

The capture service filters structures whose occupied cells intersect the selected bounds, converts them to blueprint-local coordinates using the minimum selected occupied cell as origin, and stores that normalized layout in the blueprint record.

This is preferred over absolute world-space saves because normalized coordinates make blueprints portable, previewable from a new anchor cell, and easy to validate for both the static grid and mobile interiors.

### 3. Introduce a shared blueprint site adapter instead of hard-coding world/mobile branches throughout controllers

Add a shared adapter interface around editable sites, for example `IFactoryBlueprintSite`, that exposes the operations the blueprint pipeline needs:

- enumerate structures inside a selection,
- report bounds and cell occupancy,
- validate whether a structure entry can be placed at a translated target cell/facing,
- apply a batch of placements,
- surface site-specific compatibility information.

`GridManager` and `MobileFactorySite` become the concrete implementations or collaborators behind world/interior adapters. `MobileFactoryInstance` remains responsible for attachment/runtime refresh when interior placements change, but the blueprint planner should not need to know scene-specific controller details.

This is preferred over letting `FactoryDemo` and `MobileFactoryDemo` each invent their own serialization/apply logic because those code paths would drift quickly and make future blueprint fixes twice as expensive.

### 4. Generate a transactional apply plan before mutating the scene

Blueprint application should be a two-step pipeline:

1. Build a `FactoryBlueprintApplyPlan` from the chosen blueprint, target anchor, and destination site.
2. Commit that plan only if all required entries validate successfully.

The plan should include translated target cells, per-entry validation state, a summarized issue list, and any site-specific blockers such as occupied cells, out-of-bounds cells, or missing mobile attachment mounts. The UI renders this plan as the preview overlay and only enables confirm when the plan is valid.

This is preferred over placing one structure at a time during confirm because the current project does not have a ghost/deferred-construction model. Transactional apply gives us deterministic smoke tests, clean failure semantics, and easier rollback if validation rules change.

### 5. Keep the blueprint library session-scoped in gameplay, but back it with serializable models

Add a shared `FactoryBlueprintLibrary` service that lives for the app session and is accessible from both demo scenes. The first implementation can keep blueprint entries in memory while the user is in the running project, but the records themselves should remain plain serializable data so file persistence can be layered in later without redesigning capture/apply APIs.

The library should own:

- blueprint collection and ordering,
- active selection for apply mode,
- create/rename/delete operations,
- lightweight summaries for UI lists.

This is preferred over scene-local storage because the user explicitly wants a complete save-to-apply workflow, and a blueprint saved in one demo should remain available when testing in another demo during the same session.

### 6. Use a shared blueprint panel UI pattern, but tailor entry points per scene

Implement one reusable blueprint panel/control that can show:

- current mode (`capture`, `library`, `apply preview`),
- selection summary,
- save form for naming a new blueprint,
- blueprint library list with summary cards,
- apply issues and confirm/cancel actions.

Scene entry points differ:

- `FactoryHud` exposes the full blueprint panel as part of the static sandbox controls and selection workflow.
- `MobileFactoryHud` exposes the same panel inside the editor sidebar/overlay so split-view editing remains intact.

The panel emits high-level intents back to scene controllers, just like the existing detail window does, and the controllers remain authoritative for mode switching, hover/drag input, and final placement commits.

This is preferred over bespoke UI implementations because the workflow is conceptually the same even though the surrounding camera and pane layout differ.

### 7. Blueprint compatibility must be rule-based, not profile-id locked

Mobile blueprints should not require an exact `MobileFactoryProfile.Id` match. Instead, a mobile blueprint records the minimum interior bounds it needs and any required attachment mounts. Application succeeds if the destination interior has enough room and satisfies every required attachment mount/orientation constraint.

World blueprints can only target `GridManager` sites. Mobile-interior blueprints can only target mobile interior sites. Cross-site application is rejected before placement planning.

This is preferred over exact-profile matching because it lets authored layouts travel between compatible mobile factory variants and keeps the underlying system more reusable than today's preset-only flow.

## Risks / Trade-offs

- [Blueprint serialization drifts from live structure behavior] -> Centralize stable config extraction/application per structure type and cover recipes plus attachments in smoke tests.
- [Selection and preview controls collide with existing build/delete modes] -> Keep blueprint capture/apply as explicit controller modes with clear HUD labels and dedicated cancel paths.
- [Mobile attachment rules become difficult to reason about] -> Make attachment compatibility part of the apply-plan issue list and block commit unless every required mount validates.
- [Session-only library may feel limited] -> Keep the first release gameplay-complete in-session while using serializable records so disk persistence can be added later without breaking blueprint ids or UI assumptions.
- [Large previews can clutter smaller viewports] -> Render compact overlays, clamp the blueprint panel, and summarize blockers instead of flooding the screen with text.

## Migration Plan

1. Add shared blueprint DTOs, library state, capture helpers, and apply-plan generation while leaving existing build modes unchanged.
2. Add site adapters for `GridManager` and `MobileFactorySite`, plus structure-specific config serializers for recipes and boundary attachments.
3. Integrate capture/apply modes and the reusable blueprint panel into the static sandbox.
4. Integrate the same blueprint pipeline into the mobile factory interior editor, including compatibility checks for attachment mounts and interior bounds.
5. Add smoke coverage for capture, library save/select, valid apply, and invalid apply paths in both demos.

Rollback is localized: if the blueprint workflow proves unstable, the new UI entry points can be disabled and the demos fall back to their existing direct build/edit flows while leaving shared structure logic intact.

## Open Questions

- Should the first implementation allow rotating an entire blueprint at apply time, or should it start with source-facing preservation only and add rotated apply later?
- Do we want a small amount of per-structure config remapping on apply, such as recipe substitution for missing items, or should mismatched config always be treated as invalid in v1?
- Is the mobile editor better served by exposing both rectangle capture and full-interior capture on day one, or should full-interior capture be the only supported mobile save path initially?
