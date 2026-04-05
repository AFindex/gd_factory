## ADDED Requirements

### Requirement: Item definitions configure stack limits
The game SHALL define a maximum stack size for each `FactoryItemKind`, and all slotted inventories SHALL use that configured limit when deciding whether an item can join an existing slot stack.

#### Scenario: Inventory reads an item's configured stack limit
- **WHEN** a slotted inventory evaluates whether an incoming item can be merged into an occupied slot
- **THEN** it uses the maximum stack size configured for that item's kind instead of a structure-specific hardcoded constant

### Requirement: Slotted inventories merge compatible items before consuming new slots
The game SHALL let a slotted inventory place multiple same-kind items into one slot up to that kind's configured stack limit, preferring compatible non-full stacks before consuming an empty slot.

#### Scenario: Incoming item joins a compatible partial stack
- **WHEN** an incoming item matches a slot whose stack contains the same item kind and that stack is below the configured maximum
- **THEN** the inventory adds the item to that existing stack and does not consume another empty slot

#### Scenario: Incoming item opens a new stack only when needed
- **WHEN** an incoming item cannot join any compatible partial stack and the inventory still has an empty slot
- **THEN** the inventory creates a new stack in the next deterministic slot position

### Requirement: Stack-aware inventory reads and moves stay deterministic
The game SHALL keep stackable inventory operations deterministic for repeated runs of the same layout, including item withdrawal order and slot-to-slot moves.

#### Scenario: Taking an item decrements the earliest eligible stack
- **WHEN** a consumer requests the next available item from a slotted inventory
- **THEN** the inventory removes exactly one item from the earliest occupied slot in inventory order and preserves the remaining items in that slot for later requests

#### Scenario: Moving onto a compatible stack merges up to the limit
- **WHEN** the player moves an occupied source slot onto another slot holding the same item kind and the destination stack is not full
- **THEN** the inventory fills the destination stack up to its configured maximum and leaves any overflow items in the source slot without deleting or duplicating items

#### Scenario: Incompatible or full destination stacks reject the move
- **WHEN** the player moves an occupied source slot onto a destination slot that contains a different item kind or is already at its configured stack limit
- **THEN** the move is rejected and both stacks remain unchanged
