## 1. Item Metadata And Shared Inventory Core

- [x] 1.1 Add per-item max stack size metadata and lookup helpers to the shared factory item catalog definitions.
- [x] 1.2 Refactor `FactorySlottedItemInventory` and its snapshot/state models to store per-slot stacks, merge incoming items into compatible partial stacks, and preserve deterministic single-item peek/take behavior.
- [x] 1.3 Implement stack-aware inventory move rules for empty-slot moves, compatible-stack merges, and invalid destination rejection without allowing drag operations to start from empty source slots.

## 2. Structure Integration

- [x] 2.1 Update storage structures to use stack-aware capacity checks, stacked inspection summaries, and deterministic one-item output from buffered stacks.
- [x] 2.2 Update other slotted structures that share the inventory system, including machine inputs/outputs, generator fuel, and turret ammo, so their counts and acceptance logic remain correct with stacked slots.

## 3. Detail Panel And Drag UX

- [x] 3.1 Extend structure detail models and signatures to include stack count and stack-limit data for occupied slots.
- [x] 3.2 Update the shared structure detail window to render stack counts, highlight valid merge targets, and ignore drag attempts on empty slots while preserving existing panel movement behavior.

## 4. Verification

- [x] 4.1 Expand factory demo smoke coverage to verify stacked storage buffering, deterministic withdrawal, and stack-aware panel synchronization.
- [x] 4.2 Add regression checks for invalid empty-slot drag attempts and valid stack merge moves in both shared detail-window entry points that use the inventory panel flow.
