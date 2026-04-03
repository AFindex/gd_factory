## ADDED Requirements

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
The game SHALL represent internal storage in the detail window as a slot grid with explicit slot positions rather than as text-only inspection lines.

#### Scenario: Storage-bearing structures show occupied slots
- **WHEN** the player opens the detail window for a structure with internal storage such as storage or a gun turret ammo rack
- **THEN** the detail window shows a grid of slots, marks which slots are occupied, and displays the items at their current slot coordinates

#### Scenario: Moving an inventory item updates its slot position
- **WHEN** the player drags an item from one valid inventory slot to another within the selected structure's grid
- **THEN** the structure updates the item's stored slot position and the detail window reflects the new arrangement without losing the item

### Requirement: Recipe-capable structures expose recipe details and recipe selection
The game SHALL show recipe information for recipe-capable production structures and allow the player to switch the active recipe from the detail window.

#### Scenario: Production structure shows recipe options
- **WHEN** the player opens the detail window for a recipe-capable production structure
- **THEN** the window shows the current recipe, the available recipe choices, and the selected recipe's production summary

#### Scenario: Selecting a recipe updates the structure
- **WHEN** the player selects a different valid recipe in the detail window for the chosen production structure
- **THEN** the structure's active recipe changes and subsequent production behavior follows the newly selected recipe
