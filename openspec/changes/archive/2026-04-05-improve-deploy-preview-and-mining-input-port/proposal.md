## Why

Factory-facing deployment feedback is currently misleading in two places: build/deploy preview arrows are placeholder markers that do not always read like the actual selected facing, and mining input ports hard-fail deployment unless their mining stencil sits on deposits. This makes previews harder to trust and prevents players from staging a mobile factory near a resource patch before the mining side is aligned.

## What Changes

- Replace the placeholder facing markers in both structure-build previews and mobile-factory deployment previews with real arrow-shaped indicators that always follow the selected facing.
- Fix mobile factory deployment preview so the world-facing arrow rotates and positions consistently with the actual deployment orientation.
- Introduce an intermediate deploy-preview state for mining input ports that is visually distinct from blocked and valid deployment.
- Allow mobile factory deployment to proceed when a mining input port's projected mining cells are buildable but not currently covering any mineral cells.
- Show mining piles or mining stake visuals only on projected mining cells that actually overlap deposits; hide them on non-mineral cells even when deployment remains allowed.
- Preserve green as the fully valid deploy state, use yellow for the optional/disconnected mining-input state, and keep red for blocked deployment.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `factory-grid-building`: placement and deployment previews need to communicate real facing direction and richer preview validity feedback.
- `mobile-factory-lifecycle`: deployment validation needs an optional mining-input state that allows deployment without deposit coverage when all required cells remain reservable.
- `mobile-factory-boundary-attachments`: mining input attachments need conditional world-side visuals and attachment behavior that distinguish blocked, optional, and fully connected mining projections.

## Impact

Affected areas include [FactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\FactoryDemo.cs), [MobileFactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryDemo.cs), [MobileFactoryInstance.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryInstance.cs), [MobileFactoryBoundaryAttachments.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryBoundaryAttachments.cs), preview mesh creation in the factory demos, and the related OpenSpec capability specs for deployment preview, lifecycle validation, and boundary attachments.
