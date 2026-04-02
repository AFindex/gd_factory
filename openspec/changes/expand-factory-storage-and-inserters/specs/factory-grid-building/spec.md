## MODIFIED Requirements

### Requirement: Placement preview communicates build state
The game SHALL show a live preview for the currently selected structure or deployment action so the player can understand what will happen before placement.

#### Scenario: Preview follows hovered cell during build mode
- **WHEN** the player has explicitly selected a build or deployment tool and moves the cursor across the factory floor
- **THEN** a preview moves to the hovered snapped cell and updates to reflect current orientation and validity

#### Scenario: Mobile factory preview shows footprint and ports
- **WHEN** the player is targeting a mobile factory deployment in the dedicated mobile-factory demo
- **THEN** the preview highlights the full footprint and world-facing port cells so blocked deployment cells are visible before confirmation

### Requirement: Placement requires explicit build selection
The game SHALL default the static factory demo to a non-build interaction mode and only place structures when the player has explicitly selected a buildable prototype or deployment action.

#### Scenario: Default clicks interact instead of placing
- **WHEN** the player has not selected a buildable prototype and clicks on the factory floor or an existing structure
- **THEN** the game stays in interaction mode and does not place a new structure

#### Scenario: Selecting a prototype enables placement
- **WHEN** the player explicitly selects a buildable prototype and targets a valid empty cell
- **THEN** the game enters build mode and allows the selected structure to be placed according to the normal snapped placement rules
