## Why

Mining input ports currently treat mining stakes as world-payload visuals instead of real gameplay objects, so partial mineral coverage, stake destruction, and rebuilt stake capacity are not represented consistently. We need to move stake deployment into the logic layer now so mining input ports can report accurate deployment counts, survive combat interactions, and stay compatible with the existing mobile-factory deployment flow.

## What Changes

- Promote mining stakes from mining-input payload meshes into real mobile-factory child structures with their own health, destruction, and simulation cleanup.
- Evaluate mining-input deployment per projected mineral cell and only deploy stakes onto compatible deposit cells that also have available built-stake stock; empty projected cells no longer create fake stake presentation.
- Keep current hard deployment blockers for footprint, reservation, and out-of-bounds checks so mobile-factory deployment/business rules stay intact while mining stake shortages become a deployment-state concern instead of a hull-deploy blocker.
- Let mining input port details show built stake stock, currently deployed stake count, and the number of eligible mining cells at the current deployment site, plus controls to build replacement stakes.
- Remove the mining-input relay / transfer-station world payload model and derive mining-world presentation from the actual deployed stakes and their attachment-owned connectors only.
- Preserve existing behavior for non-mining boundary attachments and ordinary deployment previews.

## Capabilities

### New Capabilities
- `mobile-factory-child-structures`: world-side child buildings owned by deployed mobile-factory attachments, including durability, destruction, and owner-state synchronization.

### Modified Capabilities
- `mobile-factory-boundary-attachments`: mining input attachments need cell-by-cell stake deployment, no fake stake visuals on empty cells, and no standalone relay payload model.
- `mobile-factory-lifecycle`: deployment evaluation needs to preserve current hard blockers while supporting partial mining-stake deployment based on mineral coverage and available built stakes.
- `factory-resource-extraction`: mobile-factory mining needs to depend on deployed, surviving mining stakes instead of a payload-only mining visual.
- `factory-structure-detail-panels`: mining input port detail windows need to show mining stake deployment status and provide stake-building actions.

## Impact

Affected areas include [MobileFactoryBoundaryAttachmentStructure.cs](D:/Godot/projs/net-factory/scripts/factory/structures/MobileFactoryBoundaryAttachmentStructure.cs), [MobileFactoryBoundaryAttachments.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryBoundaryAttachments.cs), [MobileFactoryInstance.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryInstance.cs), [FactoryStructure.cs](D:/Godot/projs/net-factory/scripts/factory/structures/FactoryStructure.cs), [FactoryStructureDetails.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetails.cs), [FactoryStructureDetailWindow.cs](D:/Godot/projs/net-factory/scripts/factory/FactoryStructureDetailWindow.cs), [MobileFactoryDemo.cs](D:/Godot/projs/net-factory/scripts/factory/MobileFactoryDemo.cs), and combat / simulation cleanup paths in [SimulationController.cs](D:/Godot/projs/net-factory/scripts/factory/SimulationController.cs). The change also updates the related OpenSpec capability specs for attachment deployment, lifecycle validation, structure details, and resource extraction.
