## Context

The static factory sandbox and the mobile factory interior editor both rely on lightweight HUD labels plus `IFactoryInspectable.GetInspectionLines()` for structure status. That works for hover and quick reads, but it does not provide an independent details surface, cannot express positioned inventory slots, and cannot expose recipe-changing controls for production structures. The affected logic is cross-cutting: world demo input, mobile editor input, structure data models, and two separate HUD classes all need to cooperate.

## Goals / Non-Goals

**Goals:**
- Provide one shared structure-details interaction pattern in the static sandbox and the mobile factory editor.
- Open a standalone draggable details window when the player left-clicks a structure in interaction mode.
- Represent internal storage with slot coordinates so inventory grids can show occupied positions and support drag-and-drop relocation.
- Expose production recipe details and recipe switching for recipe-capable structures without leaving the current scene.
- Keep build-mode placement, rotation, and removal flows intact.

**Non-Goals:**
- Introducing a full save/load persistence layer for panel positions or inventory layouts across application restarts.
- Adding a desktop-style multi-window stack with multiple simultaneous structure inspectors.
- Reworking unrelated logistics rules, enemy balance, or mobile factory deployment behavior beyond what recipe-capable structures require.

## Decisions

### 1. Introduce a richer structure-details contract instead of expanding the string-only inspection API

Add a new shared detail provider model for structures, for example a `FactoryStructureDetailModel` returned by a new interface such as `IFactoryStructureDetailProvider`. The model should carry structured sections for summary data, inventory grids, recipe options, and structure-specific actions. The existing `IFactoryInspectable` string API can remain as a lightweight fallback for compact HUD text and transitional compatibility.

This is preferred over stretching `GetInspectionLines()` because strings are already at their limit: they cannot express draggable inventory slots, recipe selection state, or UI commands without pushing parsing logic into the HUD.

### 2. Replace queue-only or scalar storage state with slot-based inventory data where positioned storage matters

Structures that expose internal storage in the detail window should store contents in explicit slot coordinates rather than only as a queue or integer count. In practice this means:
- `StorageStructure` moves from queue snapshots to a grid-backed inventory container.
- `GunTurretStructure` replaces the scalar ammo count with discrete ammo slots in a compact rack layout.
- Single-buffer production structures such as `ProducerStructure` and `AmmoAssemblerStructure` expose their ready output as a one-slot inventory section when relevant.

The inventory container should support:
- querying occupied slots for rendering,
- moving an item from one slot to another during drag-and-drop,
- deterministic iteration order for extraction or consumption.

This is preferred over deriving fake coordinates from a queue at render time because the user specifically wants movable item positions. Synthetic coordinates would make drag-and-drop cosmetic only and would quickly diverge from simulation state.

### 3. Use one shared draggable detail window control per scene, reused for the currently selected structure

Each demo scene should host a single reusable detail window control that is independent from the existing build/info HUD panels. Clicking a structure in interaction mode selects it and binds that window to the structure's live detail model; clicking another structure retargets the same window. The window must support mouse dragging, viewport clamping, and per-scene default placement so it does not cover critical controls by default.

This is preferred over building separate sandbox/mobile implementations because the requested behavior is the same in both places and the scenes already share structure concepts. Reusing one control reduces drift between demos and keeps follow-up UI changes cheaper.

### 4. Keep scene controllers responsible for mutations; the detail window only emits intent

The detail window should emit high-level intents such as `InventoryMoveRequested` and `RecipeSelected` back to the owning scene controller (`FactoryDemo` or `MobileFactoryDemo`). The scene/controller layer validates the request against the selected structure and then mutates structure state.

This is preferred over letting HUD code mutate structures directly because the existing demos already centralize click mode, selection, and build behavior in their controller scripts. Keeping mutation there avoids UI code becoming a second gameplay authority.

### 5. Model recipes as structure-owned definitions with defaults matching current behavior

Recipe-capable structures should expose an active recipe and a list of allowed recipes through shared recipe definitions. Initial implementation should keep the current demo outputs as defaults so existing layouts still behave the same when the feature lands:
- `ProducerStructure` starts on its current generic cargo output recipe.
- `AmmoAssemblerStructure` starts on its current ammo magazine recipe.

This is preferred over keeping recipes purely in HUD state because simulation code needs an authoritative recipe source for spawning, buffering, and inspection.

## Risks / Trade-offs

- [Inventory refactors could destabilize logistics behavior] -> Migrate storage-bearing structures one by one and verify deterministic extraction/consumption with focused tests or smoke scenarios.
- [A new window can overwhelm smaller viewports] -> Reuse a single inspector window, clamp drag bounds, and keep the default open position away from build palettes and world-strip controls.
- [Recipe changes can accidentally break authored demo layouts] -> Preserve current recipes as defaults and keep the first recipe set deliberately small and compatibility-checked.
- [Two inspection systems may coexist temporarily] -> Treat string inspection as fallback-only and route both HUDs to the richer detail model as soon as equivalent coverage exists.

## Migration Plan

1. Add shared detail-model and inventory-slot primitives while preserving existing structure behavior.
2. Convert storage-bearing and recipe-capable structures to expose structured detail data with current behavior preserved as defaults.
3. Integrate the reusable detail window into the static factory sandbox and route interaction-mode clicks to it.
4. Integrate the same detail window pattern into the mobile factory interior editor without changing build-mode hover ownership.
5. Remove or demote redundant inline inspection content once the new detail window is live and synchronized.

Rollback is straightforward because the change is local to demo scenes and structure state models: if the inspector flow proves unstable, the scenes can temporarily fall back to the existing text inspection path while keeping the rest of the demo playable.

## Open Questions

- Should drag-and-drop slot positions directly determine extraction order for every inventory-bearing structure, or should some structures keep a structure-specific priority rule while still exposing visible positions?
- What is the smallest useful initial recipe set for `ProducerStructure` that demonstrates recipe switching without forcing a broad new item taxonomy in the same change?
