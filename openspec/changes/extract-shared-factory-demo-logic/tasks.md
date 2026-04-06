## 1. Shared Scaffold And Primitives

- [x] 1.1 Add shared factory-demo runtime/scaffold types for common bootstrap concerns such as environment, floor/grid, root-node creation, simulation/camera/player support, and launcher navigation wiring
- [x] 1.2 Move duplicated preview constants and reusable preview helpers such as common materials, facing-arrow setup, and power-link-style primitive creation into shared support code

## 2. Shared Interaction Bridges

- [x] 2.1 Extract shared structure-detail and player-inventory bridge logic so both demos publish inspection/detail models and backpack-driven placement state through common helpers
- [x] 2.2 Extract shared blueprint workspace/panel-state coordination helpers while keeping world-grid and mobile-interior placement-plan generation in demo-specific adapters

## 3. Demo Migration

- [x] 3.1 Refactor `FactoryDemo` to consume the shared runtime/scaffold and shared interaction bridges while preserving static sandbox authored layouts, HUD behavior, and testing workspace flow
- [x] 3.2 Refactor `MobileFactoryDemo` to consume the shared runtime/scaffold and shared interaction bridges while preserving deploy modes, interior editing, large-scenario options, and mobile HUD behavior
- [x] 3.3 Trim the remaining duplicated controller glue from both demos and keep only scenario-specific orchestration, validation, and authored smoke logic in the concrete controllers

## 4. Verification

- [x] 4.1 Verify the static sandbox still supports build/detail/inventory/blueprint flows after extraction and update any affected smoke checks or regression helpers
- [x] 4.2 Verify the focused and large mobile demos still support deploy, editor, preview, detail, inventory, and blueprint flows after extraction and update any affected smoke checks or regression helpers
