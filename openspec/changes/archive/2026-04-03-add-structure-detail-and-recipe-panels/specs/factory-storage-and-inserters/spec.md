## MODIFIED Requirements

### Requirement: Storage exposes an inspection panel
The game SHALL allow the player to inspect a storage structure and view its current buffered contents through a dedicated detail panel with an inventory-style slot grid while in interaction mode.

#### Scenario: Selecting storage opens its contents panel
- **WHEN** the player is in interaction mode and selects a storage structure in the world
- **THEN** the game opens a storage detail panel bound to that structure and displays its buffered contents in inventory slots instead of a text-only list

#### Scenario: Storage panel stays synchronized with the selected structure
- **WHEN** the storage detail panel is open and items are added to or removed from the selected storage
- **THEN** the panel updates to reflect the storage's current buffered contents without requiring the player to reopen it

#### Scenario: Storage contents can be rearranged by slot
- **WHEN** the player drags a buffered item to another valid slot in the selected storage panel
- **THEN** the storage keeps the same buffered item count while updating the moved item's slot position
