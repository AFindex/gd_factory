# factory-structure-detail-panels Specification

## Purpose
Define standalone structure detail windows for factory interaction mode, including draggable inspection UIs for inventory-bearing and recipe-capable structures.

## Requirements
### Requirement: Interaction mode opens a standalone structure detail window
The game SHALL open a standalone structure detail window when the player left-clicks a structure while the current interaction mode is not a build-placement mode.

#### Scenario: Static sandbox click opens details
- **WHEN** the player is in the factory sandbox interaction mode and left-clicks an existing structure
- **THEN** the game opens or focuses a standalone detail window for that structure instead of entering placement behavior

#### Scenario: Mobile editor click opens details
- **WHEN** the player is in the mobile factory interior editor interaction mode and left-clicks an existing internal structure
- **THEN** the game opens or focuses a standalone detail window for that structure while keeping the split-view editor visible

### Requirement: Structure detail windows are independently draggable
The game SHALL allow an open structure detail window to be repositioned independently from the main HUD so the player can move it away from the current work area.

#### Scenario: Player drags the detail window
- **WHEN** the player presses and drags the detail window's draggable region
- **THEN** the window moves with the pointer and remains clamped to the visible viewport

### Requirement: Inventory-bearing structures expose positioned slot grids
The game SHALL represent internal storage in the detail window as a slot grid with explicit slot positions, visible stack counts, and stack-aware drag targets rather than as text-only inspection lines.

#### Scenario: Storage-bearing structures show occupied slots with stack counts
- **WHEN** the player opens the detail window for a structure with internal storage such as storage or a gun turret ammo rack
- **THEN** the detail window shows a grid of slots, marks which slots are occupied, and displays each occupied slot's item and current stack count at its slot coordinates

#### Scenario: Moving an inventory item updates slot position or merges a stack
- **WHEN** the player drags an item from one valid inventory slot to another within the selected structure's grid
- **THEN** the structure updates the stored slot arrangement by moving the stack into an empty slot or merging it into a compatible non-full stack without losing the buffered items

#### Scenario: Empty slots cannot start a drag interaction
- **WHEN** the player presses on an inventory slot that currently has no item
- **THEN** the detail window does not enter an item-drag state and does not emit an inventory move request

### Requirement: Recipe-capable structures expose recipe details and recipe selection
The game SHALL show recipe information for recipe-capable production structures and allow the player to switch the active recipe from the detail window.

#### Scenario: Production structure shows recipe options
- **WHEN** the player opens the detail window for a recipe-capable production structure
- **THEN** the window shows the current recipe, the available recipe choices, and the selected recipe's production summary

#### Scenario: Selecting a recipe updates the structure
- **WHEN** the player selects a different valid recipe in the detail window for the chosen production structure
- **THEN** the structure's active recipe changes and subsequent production behavior follows the newly selected recipe

### Requirement: Mining input port detail windows expose mining stake status and rebuild actions
The game SHALL show mining-input-specific deployment counts in the structure detail window and provide direct controls to build replacement mining stakes from that same window.

#### Scenario: Detail window shows deployed and available stake counts
- **WHEN** the player opens the detail window for a mining input port
- **THEN** the window shows that port's built stake stock, currently deployed stake count, and eligible mining-cell count for the current deployment state

#### Scenario: Player rebuilds a stake from the detail window
- **WHEN** the player uses a mining-stake build action from the mining input port detail window and the port has remaining stake capacity
- **THEN** the port increases its built stake stock and the detail window refreshes to show the updated count without requiring unrelated editor actions
