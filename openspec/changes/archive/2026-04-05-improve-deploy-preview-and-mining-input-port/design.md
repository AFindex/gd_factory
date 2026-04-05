## Context

The current deployment preview pipeline mixes two separate concerns into a single boolean check: whether deployment is blocked and whether a world-side attachment is fully useful after deployment. That works for ordinary input/output ports, but it breaks down for mining input ports because deposit coverage is currently treated like a hard deployment requirement. At the same time, the static factory build preview and the mobile-factory world deployment preview still use placeholder-facing markers that make the selected facing look detached from the actual oriented footprint.

This change touches multiple modules:

- Build and deployment preview rendering in [FactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\FactoryDemo.cs) and [MobileFactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryDemo.cs)
- Deployment validation and attachment projection evaluation in [MobileFactoryInstance.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryInstance.cs)
- Boundary attachment definitions and mining-specific world binding in [MobileFactoryBoundaryAttachments.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryBoundaryAttachments.cs)

The design needs to preserve the existing deployment flow, avoid breaking ordinary attachment types, and make the preview state directly reusable for future UI and smoke tests.

## Goals / Non-Goals

**Goals:**

- Make both the static build preview and the world deployment preview use real arrow meshes or mesh groups whose transforms always match the selected facing.
- Replace the deploy-preview boolean with an evaluation model that can distinguish blocked, optional, and fully connected mining-input results.
- Keep occupancy and out-of-bounds checks strict while treating mineral coverage for mining input projections as a soft requirement.
- Drive preview colors, helper text, and mining stake visibility from the same deployment evaluation result so the visuals stay consistent with behavior.
- Preserve existing deployment behavior for ordinary output and input attachments.

**Non-Goals:**

- Redesign the whole mobile factory HUD or input scheme.
- Change the mining drill rules in the static factory sandbox.
- Introduce new resource types, mining recipes, or attachment mounts.
- Remove projection reservation checks for occupied or out-of-bounds mining cells.

## Decisions

### 1. Add a structured deployment evaluation result instead of expanding `CanDeployAt`

`MobileFactoryInstance` will gain a deployment-evaluation path that returns:

- Overall deployment status: `Blocked`, `DeployableWithWarnings`, or `Deployable`
- Per-attachment evaluation entries
- Per-projection world cells grouped into hard-valid, soft-valid, and blocked buckets

`CanDeployAt(...)` can remain as a compatibility wrapper that returns `true` only for deployable states, but preview rendering and helper messaging should use the richer result directly.

Why this approach:

- It keeps existing callers stable while giving the preview enough detail for colors and conditional world meshes.
- It prevents mining-input-specific exceptions from spreading as ad hoc conditionals across the demo scene.

Alternative considered:

- Keep `CanDeployAt(...)` as the only API and add separate mining-preview helper methods. Rejected because it would duplicate cell classification and make smoke tests depend on scene-specific logic instead of gameplay evaluation.

### 2. Split hard deployment blockers from soft mining coverage checks

Attachment projection evaluation will treat these as hard blockers for every attachment type:

- Any footprint or reserved world cell is out of bounds
- Any required reserved world cell overlaps an occupied or reserved location
- The attachment cannot resolve its mount/projection shape

Mining input attachments will additionally compute a soft mining-coverage result:

- `Connected` when one or more projected mining cells overlap valid deposits
- `Optional` when projected cells remain buildable but none overlap deposits
- `Blocked` when hard deployment checks fail

Deployment remains allowed for both `Connected` and `Optional`, but only `Connected` cells contribute active mining visuals and resource collection.

Why this approach:

- It matches the user requirement that “has mineral” is optional while still preserving spatial correctness.
- It allows yellow feedback without weakening collision and reservation rules.

Alternative considered:

- Stop reserving mining projection cells when they do not overlap deposits. Rejected for now because it changes footprint semantics and could allow world objects to overlap the deployed mining array in confusing ways.

### 3. Drive world preview colors from the highest-severity deployment state

The world deployment preview will use one evaluation result per hovered anchor and facing:

- Red when any hard blocker exists
- Yellow when deployment is allowed but at least one mining input attachment is optional/disconnected
- Green when deployment is allowed and all active attachments are fully connected

Footprint cells, world port cells, helper text, and any per-cell mining stake preview will read from that same state. This keeps the preview legible and avoids cases where the arrow, footprint, and port visuals disagree.

Why this approach:

- The current preview already centralizes color choice in `UpdateWorldPreview`, so introducing one richer status there is low-risk.
- It gives the player immediate feedback about “can deploy but not mining yet,” which is the key missing state.

Alternative considered:

- Color only the mining cells yellow while leaving the footprint green. Rejected because the requested new state is about deployment readiness and needs to be visible at a glance before inspecting individual cells.

### 4. Replace placeholder facing indicators with geometry-backed arrow helpers

The preview facing markers in both demos will be rebuilt as actual geometry, for example a shaft mesh plus an arrowhead mesh under a shared root. Their positions can still be derived from the preview footprint center, but the visible head must point in the exact selected build or deployment direction.

The helper can be shared across the static factory sandbox and the mobile-factory demo so both previews derive orientation from the same transform rules. Interior editor preview arrows can keep their current placeholder treatment unless the shared helper is cheap to adopt there as well.

Why this approach:

- A mesh arrow reads correctly from more camera angles than a `Label3D` glyph or a box-shaped placeholder.
- Using transform-driven geometry ensures the displayed arrow cannot drift from the selected-facing rotation logic.

Alternative considered:

- Keep the current placeholder markers and only adjust their position/rotation math. Rejected because the current complaint is not just drift, but that the preview still does not feel like a real direction arrow.

## Risks / Trade-offs

- [Risk] Preview-state plumbing touches deploy validation and world rendering at the same time. → Mitigation: keep `CanDeployAt(...)` as a wrapper around the new evaluation API so existing command and smoke-test paths continue to work while preview code migrates.
- [Risk] Partial mining coverage could create unclear player expectations about whether the attachment is “working.” → Mitigation: treat green as fully connected, yellow as deployable-but-unconnected, and render mining stakes only on deposit-backed cells.
- [Risk] A new arrow mesh could look oversized or clip into the factory footprint. → Mitigation: build it from simple primitives with size derived from `FactoryConstants.CellSize` and validate from the mobile-factory camera angles already used by the demo.
- [Risk] Existing smoke checks assume off-deposit mining deployment is invalid. → Mitigation: update the smoke scenarios to assert the new yellow deployable state and verify that off-deposit deployment does not connect mining visuals or flow.

## Migration Plan

1. Add the deployment evaluation types and adapt mining-input projection checks to return hard/soft states.
2. Switch world preview rendering to consume the evaluation result and emit red/yellow/green visuals plus conditional mining stakes.
3. Replace the static build and world-deployment preview markers with geometry-backed arrows and align them to the selected facing.
4. Update smoke checks and any scenario helpers that currently expect off-deposit mining deployment to fail.

Rollback is straightforward because the change is local to the mobile-factory demo and instance logic: the new evaluation API can be bypassed by reverting the preview back to the old boolean path and restoring strict mining deposit checks.

## Open Questions

- None that block implementation. The main assumption in this design is that non-mineral mining projection cells remain spatially reserved when deployment succeeds, even though they do not render mining stakes or produce resources.
