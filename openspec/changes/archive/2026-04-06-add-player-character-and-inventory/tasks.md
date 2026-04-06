## 1. Player Runtime Foundation

- [x] 1.1 Create the shared player-character runtime for factory demos, including a capsule-bodied placeholder scene/node setup and common movement state
- [x] 1.2 Add player movement input handling for `WASD` and hook both the static sandbox and focused mobile-factory demo to spawn and update the player controller
- [x] 1.3 Extend `FactoryCameraRig` with player-follow support and input gating so player-facing UI can suppress world movement and placement input

## 2. Player Inventory And Structure Items

- [x] 2.1 Introduce player-owned inventory containers for backpack and hotbar using the existing stack-aware slot model instead of a separate inventory system
- [x] 2.2 Expand item/catalog definitions so buildable structures exist as placeable inventory items with icons, names, stack limits, and prototype-mapping metadata
- [x] 2.3 Add selection state for hotbar-driven placement so the active quick slot can arm or clear a placeable structure item

## 3. Player Panels And Shared Container UI

- [x] 3.1 Build standalone player windows for backpack, item information, and personal stats using the existing draggable detail-window style
- [x] 3.2 Refactor inventory-slot UI plumbing so player inventories and structure inventories can share slot rendering, selection, and drag state
- [x] 3.3 Implement cross-container transfer commands between player inventory endpoints and open structure inventories, including merge, split, and rejection behavior

## 4. Sandbox Interaction Integration

- [x] 4.1 Update sandbox input priority so UI interaction and drag-drop win before structure inspection or world placement
- [x] 4.2 Change static sandbox placement flow to accept explicit hotbar/backpack structure selection as a valid build source while preserving normal snapped placement validation
- [x] 4.3 Consume one selected structure item on successful placement and leave the stack unchanged on invalid placement attempts
- [x] 4.4 Keep structure inspection behavior available when no placeable item is armed and ensure open player/structure panels stay synchronized after transfers

## 5. Mobile Factory Demo Integration

- [x] 5.1 Rework the focused mobile-factory demo so player control is the default top-level mode and existing factory command / deploy / observer flows become explicit entered contexts
- [x] 5.2 Restore player-controlled movement and follow-camera behavior whenever the user exits factory command, deploy preview, or observer mode
- [x] 5.3 Keep mobile-factory HUD/workspace affordances understandable under the new player-first control model, including a clear way to enter and exit factory contexts

## 6. Verification And Documentation

- [x] 6.1 Add or update smoke coverage for player spawn, player movement, camera follow, hotbar selection, structure-item placement, and cross-container inventory transfer
- [x] 6.2 Extend mobile-demo validation to cover default player mode, switching into factory command mode, and returning to player control without losing current demo state
- [x] 6.3 Update project/demo notes to document the new player-first interaction flow, hotbar/backpack behavior, and panel controls in sandbox and mobile demo
