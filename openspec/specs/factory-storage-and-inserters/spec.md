# factory-storage-and-inserters Specification

## Purpose
Define buffered storage, storage inspection, and inserter-based handoff rules for the factory logistics sandbox.

## Requirements
### Requirement: Storage buffers discrete factory items
The game SHALL provide a storage structure that accepts discrete factory items, keeps them buffered while capacity remains available, and rejects additional items when its capacity is full.

#### Scenario: Storage accepts incoming logistics items
- **WHEN** a compatible upstream structure sends an item into an empty or partially filled storage structure
- **THEN** the storage structure records the item in its buffer and the item remains available for later downstream transfer

#### Scenario: Storage rejects overflow deterministically
- **WHEN** a storage structure has reached its configured capacity and another item is offered to it
- **THEN** the storage structure refuses the item and the upstream structure remains blocked rather than silently deleting or teleporting the item

### Requirement: Storage can act as a downstream item source
The game SHALL allow buffered storage items to be withdrawn by compatible downstream logistics so storage can serve as an active supply node instead of only a terminal sink.

#### Scenario: Inserter drains buffered storage
- **WHEN** a storage structure contains at least one buffered item and an adjacent compatible inserter requests an item from its output side
- **THEN** the storage structure yields one buffered item for transfer and keeps any remaining buffered items available for later requests

#### Scenario: Storage output preserves deterministic order
- **WHEN** multiple items have been buffered in storage and downstream logistics withdraw them one at a time
- **THEN** the game emits items in a deterministic order for repeated runs of the same layout

### Requirement: Storage exposes an inspection panel
The game SHALL allow the player to inspect a storage structure and view its current buffered contents through a dedicated detail panel with an inventory-style slot grid while in interaction mode.

#### Scenario: Selecting storage opens its contents panel
- **WHEN** the player is in interaction mode and selects a storage structure in the world
- **THEN** the game opens a storage detail panel bound to that structure and displays its buffered contents in inventory slots instead of a text-only list

#### Scenario: Storage panel stays synchronized with the selected structure
- **WHEN** the storage inspection panel is open and items are added to or removed from the selected storage
- **THEN** the panel updates to reflect the storage's current buffered contents without requiring the player to reopen it

#### Scenario: Storage contents can be rearranged by slot
- **WHEN** the player drags a buffered item to another valid slot in the selected storage panel
- **THEN** the storage keeps the same buffered item count while updating the moved item's slot position

### Requirement: Inserters bridge adjacent compatible logistics structures
The game SHALL provide a mechanical inserter structure that transfers one item at a time between its pickup side and drop side when both adjacent structures participate in buffered transfer.

#### Scenario: Inserter picks from a belt and places into storage
- **WHEN** a belt exposes an item at the inserter pickup side and the storage on the inserter drop side can accept it
- **THEN** the inserter removes exactly one item from the belt-side provider and delivers it into the storage buffer on its transfer cycle

#### Scenario: Inserter picks from storage and places onto downstream logistics
- **WHEN** a storage structure has a buffered item on the inserter pickup side and the structure on the drop side can receive it
- **THEN** the inserter transfers exactly one buffered item to the drop-side structure without duplicating or skipping the item

### Requirement: Inserters stall cleanly under blockage or incompatibility
The game SHALL keep inserter behavior deterministic when the source is empty, the destination is blocked, or either side does not support the requested transfer.

#### Scenario: Empty source pauses the inserter
- **WHEN** an inserter's pickup-side structure has no transferable item available
- **THEN** the inserter waits idle and no item appears at the drop side

#### Scenario: Blocked destination prevents item loss
- **WHEN** an inserter has a valid pickup source but the drop-side structure cannot currently receive the item
- **THEN** the inserter does not delete, duplicate, or teleport the item and waits until the destination becomes available
