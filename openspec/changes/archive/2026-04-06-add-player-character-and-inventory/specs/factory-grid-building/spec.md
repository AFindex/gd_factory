## MODIFIED Requirements

### Requirement: Placement requires explicit build selection
The game SHALL default the static factory demo to a non-build interaction mode and only place structures when the player has explicitly selected a buildable prototype or a placeable building item from the player hotbar or backpack.

#### Scenario: Default clicks interact instead of placing
- **WHEN** the player has not selected a buildable prototype and has not armed a placeable building item, then clicks on the factory floor or an existing structure
- **THEN** the game stays in interaction mode and does not place a new structure

#### Scenario: Selecting a prototype or building item enables placement
- **WHEN** the player explicitly selects a buildable prototype from the build UI or a placeable building item from the player inventory and targets a valid empty cell
- **THEN** the game enters build mode and allows the selected structure to be placed according to the normal snapped placement rules

## ADDED Requirements

### Requirement: Placing from player inventory consumes the selected structure item
The game SHALL resolve hotbar-driven building placement against the selected player inventory stack so successful placements consume the held structure item and failed placements do not.

#### Scenario: Valid placement removes one structure item from the selected stack
- **WHEN** the player left-clicks a valid target cell while a placeable structure item is selected in the hotbar
- **THEN** the world places that structure and the selected inventory stack is reduced by exactly one item

#### Scenario: Invalid placement preserves the selected structure item
- **WHEN** the player attempts to place a selected structure item onto an invalid or occupied target cell
- **THEN** no structure is created and the selected inventory stack remains unchanged
