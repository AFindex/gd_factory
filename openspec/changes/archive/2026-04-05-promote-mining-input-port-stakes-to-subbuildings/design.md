## Context

The current mining input port already distinguishes between hard deployment blockers and soft mining connectivity, but its mining stakes still exist only as payload visuals built inside [MobileFactoryBoundaryAttachmentStructure.cs](D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs). That means stake presence, health, destruction, and deployment counts are inferred from presentation instead of authoritative gameplay state. The result is the exact mismatch the user called out: mixed mineral coverage creates confusing stake presentation, destroyed stakes cannot be modeled correctly, and the detail panel cannot report or rebuild real stake capacity.

This change crosses multiple modules:

- Attachment data and deployment evaluation in [MobileFactoryBoundaryAttachments.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryBoundaryAttachments.cs) and [MobileFactoryInstance.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryInstance.cs)
- Mining input attachment runtime and world presentation in [MobileFactoryBoundaryAttachmentStructure.cs](D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs)
- Reusable structure health / combat behavior in [FactoryStructure.cs](D:/Godot/projs/net-factory/scripts/factory/structures/FactoryStructure.cs) and cleanup in [SimulationController.cs](D:/Godot/projs/net-factory/scripts/factory/SimulationController.cs)
- Detail-model plumbing and UI interaction in [FactoryStructureDetails.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetails.cs), [FactoryStructureDetailWindow.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetailWindow.cs), and [MobileFactoryDemo.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs)

The design must preserve existing hard deployment validation and non-mining attachments while moving mining stakes from scene-only visuals into authoritative simulation state.

## Goals / Non-Goals

**Goals:**

- Make each deployed mining stake a real gameplay child structure with its own health, destruction path, and owner relationship to the mining input port.
- Keep current hull deployment rules, footprint reservation, and non-mining boundary attachment behavior stable.
- Deploy mining stakes only onto compatible mineral cells, never onto empty projected cells.
- Track persistent built-stake stock on the mining input port so destroyed stakes reduce later deployment capacity until the player rebuilds them.
- Expose mining stake counts and rebuild actions through the mining input port detail window.
- Remove the standalone mining relay / transfer-station payload model.

**Non-Goals:**

- Redesign the broader mobile-factory deployment UX or attachment mount system.
- Change ordinary input/output port behavior or their world payload models.
- Introduce a new crafting economy for stake rebuilding in this change; the first pass can use a direct rebuild action to avoid disturbing existing business logic.
- Change footprint or projection reservation semantics for mining-input attachments.

## Decisions

### 1. Represent deployed mining stakes as `FactoryStructure`-based child structures

Add a world-side child-structure type for mining stakes, owned by `MobileFactoryMiningInputPortStructure` and registered in the normal structure/combat simulation. Each child structure records its owning port, projected world cell, and deposit type, and uses the shared structure health/damage pipeline.

Why this approach:

- It reuses the existing health bar, damage, destruction, and cleanup systems instead of re-implementing combat rules inside payload visuals.
- It gives mining stake loss an authoritative gameplay consequence that other systems can query.

Alternative considered:

- Keep stakes as `Node3D` payload meshes and manually track HP/state in the attachment. Rejected because it duplicates combat logic and keeps stake existence disconnected from the structure simulation.

### 2. Split mining-input deployment into eligibility and actual child deployment

Deployment evaluation continues to compute the full projected world cells for reservation and hard validity checks. After that, mining input attachments classify projected cells into:

- Projected cells: the current full mining stencil used for hard validation/reservations
- Eligible stake cells: projected cells that overlap compatible deposits
- Deployed stake cells: eligible cells that actually receive a stake because the port has available built-stake stock

The factory can still deploy when the footprint and projected cells are valid, even if some projected cells are empty or some eligible mineral cells cannot receive stakes because stock is short.

Why this approach:

- It preserves current deployment/business logic, which the user explicitly asked us not to disrupt.
- It turns stake shortage into an attachment deployment-state concern instead of a global hull-deploy blocker.

Alternative considered:

- Fail deployment whenever eligible mineral cells exceed built-stake stock. Rejected because it would introduce a new hard blocker and change existing deployment expectations.

### 3. Store persistent built-stake stock on the mining input port

`MobileFactoryMiningInputPortStructure` will own persistent counts such as:

- Maximum stake capacity, derived from the mining attachment stencil slots that can host stakes
- Built stake stock, representing how many stakes the port currently owns and can deploy
- Currently deployed stake count, derived from active child structures at the current world anchor

Destroyed stakes reduce built stock permanently until rebuilt from the detail window. Recalling or redeploying despawns surviving child structures from the world but preserves the owning port's built stock so surviving stakes can be deployed again later.

Why this approach:

- It matches the requested "can build mining stakes" loop and makes destruction meaningful beyond the current deployment.
- It avoids inventing a second inventory system in world space.

Alternative considered:

- Recreate all missing stakes automatically on each deploy. Rejected because it removes the user-requested rebuild pressure after stakes are destroyed.

### 4. Remove the mining relay payload model and drive world visuals from real stakes

The mining input attachment stops creating the standalone relay / transfer-station payload meshes. World presentation for mining input comes from the deployed child stakes themselves plus any lightweight connector geometry that references those deployed stakes, so no visual component exists without matching logic state.

Why this approach:

- It removes the current source of truth mismatch between payload visuals and gameplay state.
- It directly addresses the request to delete the relay model.

Alternative considered:

- Keep the relay as cosmetic decoration while stakes become logical. Rejected because the user explicitly wants that model logic removed.

### 5. Extend detail windows with attachment actions instead of overloading recipe UI

The detail-model pipeline gains a small action section for structure-specific commands. Mining input ports use it to show stake counts and provide one or more rebuild actions, such as building a single replacement stake or filling to capacity. The first implementation can make rebuild instant and non-resource-gated to avoid interfering with current business logic.

Why this approach:

- The current detail window supports summaries, inventories, and recipes, but building stakes is neither an inventory move nor a recipe selection.
- A generic action section stays reusable for future attachment-specific controls.

Alternative considered:

- Encode stake rebuilding as a fake recipe toggle. Rejected because it would misrepresent the operation and complicate the recipe UI contract.

## Risks / Trade-offs

- [Risk] Child structures add owner/cleanup bookkeeping across deployment, recall, and combat cleanup. → Mitigation: make the parent port the single source of truth for active child registrations and route destruction callbacks through one owner API.
- [Risk] Keeping projection reservations unchanged while some stake slots are empty may look conservative to players. → Mitigation: surface built/deployed/eligible counts clearly in the detail UI and status text so the partial state is visible.
- [Risk] Detail-window action support touches shared UI plumbing. → Mitigation: add a minimal generic action model instead of special-casing mining ports directly inside the window scene code.
- [Risk] Deterministic stake deployment order matters when stock is short. → Mitigation: use a stable world-cell ordering and show the resulting deployed count instead of implying every eligible cell always activates.

## Migration Plan

1. Add the child-structure ownership model and the mining stake world structure.
2. Refactor mining input deployment evaluation so projected, eligible, and actually deployed stake cells are computed separately while hard deploy validation stays intact.
3. Replace mining relay payload creation with child-structure spawning/cleanup and route mining state through surviving stakes.
4. Extend the detail model/window with action buttons, then add mining input specific status and rebuild commands.
5. Refresh demo/smoke coverage for mixed mineral coverage, destroyed stakes, rebuild actions, and non-mining attachment regression checks.

Rollback is localized: the child-structure deployment path can be removed and the mining input attachment can fall back to payload-only stake visuals, while the shared detail action additions can remain dormant if unused.

## Open Questions

- None that block implementation. This design intentionally assumes stake rebuilding is an immediate port action with no new material cost so we can preserve current business logic and economy rules in the first pass.
