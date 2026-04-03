## Why

The factory sandbox and mobile factory editor currently only show lightweight inline inspection text, which is enough for quick status checks but not for deeper management. We need a dedicated interaction-mode details surface so players can inspect buffered structures, understand slot positions, and change production recipes without overloading the main HUD or accidentally entering build actions.

## What Changes

- Add a shared standalone structure details window that opens from left-clicking a building while the player is in non-build interaction mode.
- Make the details window independent from the main HUD so it can be shown, hidden, and repositioned without changing the build sidebar layout.
- Show grid-based inventory contents for structures with internal storage, including slot occupancy and per-slot positional information needed for drag-and-drop movement.
- Show production recipe information and recipe selection controls for production-oriented structures instead of only static inspection text.
- Reuse the same structure-detail interaction pattern in both the static factory sandbox and the mobile factory interior editor so click behavior stays consistent between demos.

## Capabilities

### New Capabilities
- `factory-structure-detail-panels`: Shared detail windows for clicked structures, including draggable panel behavior, inventory grids, and recipe controls.

### Modified Capabilities
- `factory-storage-and-inserters`: Storage inspection changes from a text-only contents readout to a synchronized inventory-style detail panel.
- `mobile-factory-interior-editing`: Interior interaction mode changes so left-clicking a structure opens or focuses an independent detail window while preserving the split-view editor workflow.

## Impact

- Affected code will include the static factory HUD/demo flow, the mobile factory editor HUD/demo flow, and structure inspection/data interfaces under `scripts/factory/`.
- Structures with buffered contents or configurable production output will need richer inspection data than the current string-list inspection model.
- The change introduces shared UI/state management for draggable windows and detail payloads, but does not require external dependencies.
