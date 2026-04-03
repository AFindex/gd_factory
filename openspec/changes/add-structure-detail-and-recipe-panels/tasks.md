## 1. Shared Detail Data Model

- [x] 1.1 Add shared structure-detail interfaces and models for summary content, inventory grids, and recipe sections under `scripts/factory/`.
- [x] 1.2 Add slot-based inventory utilities that store explicit slot coordinates, support deterministic iteration, and allow validated item moves between slots.

## 2. Structure Integration

- [x] 2.1 Convert `StorageStructure` and `GunTurretStructure` to expose structured inventory data through the new detail-provider path.
- [x] 2.2 Update `ProducerStructure` and `AmmoAssemblerStructure` to expose recipe metadata, active recipe selection, and buffered output details through the shared detail model.
- [x] 2.3 Keep legacy compact inspection text aligned with the richer detail data so existing HUD summaries continue to read correctly during migration.

## 3. Shared Detail Window UI

- [x] 3.1 Implement a reusable draggable structure-detail window that can render summary data, inventory slots, and recipe selection controls.
- [x] 3.2 Wire detail-window actions for inventory movement and recipe selection back to scene-controller callbacks instead of mutating structures directly in HUD code.

## 4. Sandbox And Mobile Editor Integration

- [x] 4.1 Update `FactoryDemo` and `FactoryHud` so interaction-mode left clicks open or focus the detail window while build-mode placement and removal stay unchanged.
- [x] 4.2 Update `MobileFactoryDemo` and `MobileFactoryHud` so interior interaction-mode clicks open or focus the same detail-window pattern without breaking split-view hover ownership or build-mode placement.

## 5. Verification

- [x] 5.1 Smoke test the static sandbox flow for storage contents sync, gun turret ammo display, inventory drag-and-drop, and production recipe switching.
- [x] 5.2 Smoke test the mobile factory editor flow for interaction-mode detail opening, draggable panel behavior, and recipe/inventory updates while the split view remains usable.
