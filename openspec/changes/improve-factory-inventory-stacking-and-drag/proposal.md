## Why

The factory game's current slot inventories only allow one `FactoryItem` per slot, which makes storage fill too quickly and prevents warehouse-style capacity tuning by item type. At the same time, the detail panel drag flow only supports moving from occupied slots into empty slots, so we need a clear inventory contract for stack counts, per-item stack limits, and safer drag behavior before expanding warehouse management further.

## What Changes

- Add a shared stackable slot-inventory capability so one slot can hold multiple items of the same kind up to a configurable per-item maximum stack size.
- Define per-item stack-size configuration in the factory item catalog so different goods such as ore, plates, wires, and ammo can use different limits without hardcoding per-structure rules.
- Update storage and other slotted structures to accept, count, emit, and inspect stacked items while preserving deterministic transfer order and lossless overflow handling.
- Update structure detail inventory panels to display stack counts, support moving into compatible partial stacks as well as empty slots, and refuse drag start from empty slots.
- Add validation and regression coverage for stacked storage behavior, drag restrictions, and panel synchronization in the factory demo flows.

## Capabilities

### New Capabilities
- `factory-item-stacking`: Shared rules for stackable slotted inventories, per-item stack-size configuration, deterministic add/remove behavior, and stack-aware inventory moves.

### Modified Capabilities
- `factory-storage-and-inserters`: Storage capacity, buffering, inspection, and inserter handoff rules change from one-item-per-slot behavior to stack-aware slot management.
- `factory-structure-detail-panels`: Inventory detail panels change to show stack counts, merge into compatible destination stacks, and block drag interactions that start from empty slots.

## Impact

- Affected code will include shared inventory logic and item metadata under `scripts/factory/`, structure implementations that use `FactorySlottedItemInventory`, and the detail window interaction code for both static and mobile factory HUD flows.
- Existing storage and machine capacity calculations will need to distinguish slot capacity from total item count so stack-aware inventories remain deterministic and debuggable.
- Demo smoke checks and UI verification paths will need new assertions for stack counts, per-item limits, and invalid drag attempts without adding external dependencies.
