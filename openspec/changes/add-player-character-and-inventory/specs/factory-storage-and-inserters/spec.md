## ADDED Requirements

### Requirement: Player inventory can exchange items with structure inventories
The game SHALL allow the player's backpack and hotbar to act as manual transfer endpoints for storage and other building inventories that expose slot-based contents.

#### Scenario: Dragging from storage into the backpack transfers the stack
- **WHEN** the player drags an occupied slot from an open storage panel into a valid player backpack slot
- **THEN** the transferred items move into the player inventory using the same empty-slot and compatible-stack merge rules as other slotted inventories

#### Scenario: Dragging from the backpack into a compatible building slot transfers the item
- **WHEN** the player drags a compatible item stack from the backpack into an open building inventory slot such as fuel, ammo, or buffered storage
- **THEN** the target building inventory accepts the moved items and the player inventory updates to reflect the transferred amount

### Requirement: Cross-container transfer remains deterministic and lossless
The game SHALL keep player-to-structure inventory transfers deterministic and reject invalid moves without deleting, duplicating, or retyping items.

#### Scenario: Incompatible target rejects a player transfer
- **WHEN** the player drops a backpack item onto a structure slot that cannot accept that item kind
- **THEN** the transfer is rejected and both the player inventory and the target structure inventory remain unchanged

#### Scenario: Open panels stay synchronized after a transfer
- **WHEN** the player completes a valid transfer between the backpack and an open structure inventory
- **THEN** both visible panels refresh to show the same post-transfer slot occupancy and stack counts without reopening either window
