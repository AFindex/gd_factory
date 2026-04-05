## MODIFIED Requirements

### Requirement: Storage buffers stackable factory items
The game SHALL provide storage structures that accept factory items into slot-based stacks, merge incoming items into compatible non-full stacks before using empty slots, and reject additional items only when no slot can accept the offered item.

#### Scenario: Storage merges incoming items into an existing stack
- **WHEN** a compatible upstream structure sends an item into a storage structure that already contains a non-full stack of the same item kind
- **THEN** the storage increments that stack without consuming another slot and keeps the buffered items available for later downstream transfer

#### Scenario: Storage opens a new stack in a deterministic slot
- **WHEN** a compatible upstream structure sends an item that cannot join any existing stack and the storage still has an empty slot
- **THEN** the storage creates a new stack in the next deterministic slot position

#### Scenario: Storage rejects overflow deterministically
- **WHEN** every storage slot is either occupied by an incompatible stack or already at the configured stack limit for its stored item kind and another item is offered
- **THEN** the storage refuses the item and the upstream structure remains blocked rather than silently deleting or teleporting the item

### Requirement: Storage can act as a downstream item source
The game SHALL allow buffered storage items to be withdrawn one item at a time from stacked slots so storage can serve as an active supply node while preserving deterministic output order.

#### Scenario: Inserter drains one item from a buffered stack
- **WHEN** a storage structure contains a stacked slot and an adjacent compatible inserter requests an item from its output side
- **THEN** the storage yields exactly one buffered item from the earliest eligible stack and keeps the remaining items in that stack available for later requests

#### Scenario: Storage output preserves deterministic order across stacks
- **WHEN** multiple items have been buffered across one or more storage stacks and downstream logistics withdraw them one at a time
- **THEN** the game emits items in deterministic slot order and, within a stack, in deterministic arrival order for repeated runs of the same layout

### Requirement: Storage exposes an inspection panel
The game SHALL allow the player to inspect a storage structure and view its stacked buffered contents through a dedicated detail panel with an inventory-style slot grid while in interaction mode.

#### Scenario: Selecting storage opens its stacked contents panel
- **WHEN** the player is in interaction mode and selects a storage structure in the world
- **THEN** the game opens a storage detail panel bound to that structure and displays each slot's current item kind and stack count

#### Scenario: Storage panel stays synchronized with stack count changes
- **WHEN** the storage inspection panel is open and items are added to or removed from the selected storage
- **THEN** the panel updates to reflect the selected storage's current slot occupancy and per-slot stack counts without requiring the player to reopen it

#### Scenario: Storage contents can be rearranged by slot and merged by stack
- **WHEN** the player drags a buffered slot stack to another valid slot in the selected storage panel
- **THEN** the storage moves the stack into an empty slot or merges it into a compatible non-full stack without losing, duplicating, or retyping buffered items
