## MODIFIED Requirements

### Requirement: Inventory-bearing structures expose positioned slot grids
The game SHALL represent internal storage in the detail window as a slot grid with explicit slot positions, visible stack counts, and stack-aware drag targets rather than as text-only inspection lines.

#### Scenario: Storage-bearing structures show occupied slots with stack counts
- **WHEN** the player opens the detail window for a structure with internal storage such as storage or a gun turret ammo rack
- **THEN** the detail window shows a grid of slots, marks which slots are occupied, and displays each occupied slot's item and current stack count at its slot coordinates

#### Scenario: Moving an inventory item updates slot position or merges a stack
- **WHEN** the player drags an occupied inventory slot onto another valid slot within the selected structure's grid
- **THEN** the structure updates the stored slot arrangement by moving the stack into an empty slot or merging it into a compatible non-full stack without losing the buffered items

#### Scenario: Empty slots cannot start a drag interaction
- **WHEN** the player presses on an inventory slot that currently has no item
- **THEN** the detail window does not enter an item-drag state and does not emit an inventory move request
