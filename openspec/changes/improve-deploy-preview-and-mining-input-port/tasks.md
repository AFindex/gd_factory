## 1. Deployment Evaluation Model

- [x] 1.1 Add a structured mobile-factory deployment evaluation result that separates blocked, warning, and fully valid states while keeping `CanDeployAt(...)` as a compatibility wrapper.
- [x] 1.2 Update mining input attachment evaluation so occupancy and bounds remain hard requirements, deposit coverage becomes optional, and connected mining cells are tracked separately from disconnected cells.

## 2. Preview Rendering

- [x] 2.1 Replace the placeholder facing markers in [FactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\FactoryDemo.cs) and [MobileFactoryDemo.cs](D:\Godot\projs\net-factory\scripts\factory\MobileFactoryDemo.cs) with real arrow geometry that follows the selected facing.
- [x] 2.2 Update mobile-factory world preview colors, helper messaging, and projected-cell rendering so blocked targets stay red, optional mining targets turn yellow, valid targets stay green, and mining stakes only appear on deposit-backed deployable cells.

## 3. Deployment Behavior And Verification

- [x] 3.1 Update deployed mining-input behavior so off-deposit deployments remain allowed but keep the attachment disconnected and visually inactive until a later deployment overlaps deposits.
- [x] 3.2 Refresh the relevant smoke checks and deployment-preview assertions to cover true facing arrows, yellow warning previews, off-deposit deployment success, and deposit-backed mining activation.
